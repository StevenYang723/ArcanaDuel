using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Globalization;

namespace ArcanaDuel {
public partial class GameWindow {
 static void Verify(bool value,string name,List<string> output){if(!value)throw new Exception("Playback check failed: "+name);output.Add("PASS: "+name);}
 static IEnumerable<T> Descendants<T>(DependencyObject parent) where T:DependencyObject {for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T)yield return (T)child;foreach(var descendant in Descendants<T>(child))yield return descendant;}}
 void Click(Button b){if(b==null)throw new Exception("Missing test button");b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
 Button NamedButton(string name){stage.UpdateLayout();return Descendants<Button>(modal).FirstOrDefault(b=>Object.Equals(b.Content,name));}
 Button OptionButton(int id){stage.UpdateLayout();var b=Descendants<Button>(modal).FirstOrDefault(x=>x.Tag is int && (int)x.Tag==id);if(b==null)throw new Exception("Missing option "+id+" in "+(state.Choice==null?"no choice":state.Choice.Title));return b;}
 async Task Idle(){for(int i=0;i<350;i++){if(!busy && pendingSnapshots.Count==0)return;await Task.Delay(40);}throw new Exception("Presentation timed out");}
 void PresentFixture(int arc,int rank,bool up,bool prepared=false,string runMode="test"){
  Cleanup();mode=runMode;engine=Tests.Fixture(arc,rank,up,prepared);viewer=0;state=engine.View(viewer,mode=="test");Render();
 }
 void SelectPaymentForCheck(int mana,int crystals){
  ClearPaymentSelection();Render();
  for(int i=0;i<mana;i++)Click(Descendants<Button>(page).Single(b=>Object.Equals(b.Tag,"pay-mana-"+i)));
  for(int i=0;i<crystals;i++)Click(Descendants<Button>(page).Single(b=>Object.Equals(b.Tag,"pay-crystal-"+i)));
 }
 async Task CheckNewInteractions(string folder,List<string> results){
  PresentFixture(11,2,true);var card=state.Players[0].Hand[0];int revision=engine.Revision;
  var unusedKey=new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(this),0,System.Windows.Input.Key.R){RoutedEvent=Keyboard.KeyDownEvent};Key(this,unusedKey);Verify(!unusedKey.Handled,"R is not intercepted without an active card preview",results);
  BeginPlay(card);Verify(engine.Revision==revision && !busy && modal.Children.Count==0,"no selection cannot spend automatically",results);
  SelectPaymentForCheck(2,1);
  stage.UpdateLayout();DependencyObject resourceHit=stage.InputHitTest(new Point(1144,751)) as DependencyObject;while(resourceHit!=null && !(resourceHit is Button))resourceHit=VisualTreeHelper.GetParent(resourceHit);
  Verify(resourceHit is Button && Object.Equals(((Button)resourceHit).Tag,"pay-mana-0"),"scene resource is a real clickable hit target",results);
  Verify(SelectedMana==2 && SelectedCrystals==1 && engine.Players[0].Mana==3 && engine.Players[0].Crystals==3,"click resources selects exact 2 mana plus 1 crystal without spending",results);
  Click(Descendants<Button>(page).Single(b=>Object.Equals(b.Tag,"pay-mana-1")));
  Verify(SelectedMana==1 && SelectedCrystals==1,"click selected resource again cancels only that resource",results);
  BeginPlay(card);Verify(engine.Revision==revision && SelectedMana==1,"underpayment retains selection and card",results);
  SelectPaymentForCheck(3,1);BeginPlay(card);Verify(engine.Revision==revision && SelectedMana==3 && SelectedCrystals==1,"overpayment never silently chooses a cheaper subset",results);
  SelectPaymentForCheck(2,1);Capture(Path.Combine(folder,"16-selected-payment.png"));
  var handBox=page.Children.OfType<Border>().First(b=>b.Child is CardFace && ((CardFace)b.Child).Card.Id==card.Id);
  handBox.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,0,MouseButton.Right){RoutedEvent=Mouse.MouseUpEvent});
  Verify(NamedButton("预览逆位")!=null,"right-click hand opens interactive card preview",results);
  Click(NamedButton("预览逆位"));var face=Descendants<CardFace>(modal).Single();
  Verify(face.Upright==false && engine.UprightPlayer==0 && engine.Revision==revision && engine.Players[0].Mana==3 && SelectedMana==2 && SelectedCrystals==1,"other-face preview leaves orientation resources and selected payment unchanged",results);
  Capture(Path.Combine(folder,"17-other-face-preview.png"));Click(NamedButton("预览正位"));Verify(face.Upright==true,"preview toggles back independently",results);Click(NamedButton("返回"));
  ShowPreview(card,570,188,false);Key(this,new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(this),0,System.Windows.Input.Key.R){RoutedEvent=Keyboard.KeyDownEvent});
  Verify(preview.Upright==false && engine.UprightPlayer==0 && engine.Revision==revision,"hover R previews opposite effect without a game action",results);previews.Children.Clear();
  BeginPlay(card);Verify(busy && modal.Children.Count==0 && engine.Players[0].Mana==1 && engine.Players[0].Crystals==2,"selected 2 mana plus 1 crystal pays a 3 cost mixed card without payment dialog",results);await Idle();
  Verify(state.Position==13 && SelectedMana==0 && SelectedCrystals==0,"previewing reverse does not change cast effect; successful play clears selection",results);
  Verify(!Descendants<Button>(page).Single(b=>Object.Equals(b.Tag,"pay-mana-1")).IsEnabled,"spent mana cannot be selected",results);
  ToggleResource(false,1);Verify(SelectedMana==0,"unavailable resource cannot enter selection",results);
  PresentFixture(2,1,true);card=state.Players[0].Hand[0];revision=engine.Revision;SelectPaymentForCheck(1,1);BeginPlay(card);
  Verify(engine.Revision==revision && !busy,"mana-only card rejects crystals in selection",results);SelectPaymentForCheck(2,0);Verify(Payment(card)==0,"mana-only card accepts exact mana selection",results);
  PresentFixture(1,2,false);card=state.Players[0].Hand[0];revision=engine.Revision;SelectPaymentForCheck(1,1);BeginPlay(card);
  Verify(engine.Revision==revision && !busy,"crystal-only card rejects mana in selection",results);SelectPaymentForCheck(0,2);BeginPlay(card);await Idle();
  Verify(state.Players[0].Crystals==1 && state.Players[0].Mana==3,"crystal-only card consumes only selected crystals",results);
  Discard(0);stage.UpdateLayout();var discardBox=Descendants<Border>(modal).First(b=>b.Child is CardFace);discardBox.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,0,MouseButton.Left){RoutedEvent=Mouse.MouseUpEvent});
  Verify(NamedButton("预览正位")!=null,"discard card opens preview in owner's actual reverse orientation",results);Click(NamedButton("预览正位"));Click(NamedButton("返回"));stage.UpdateLayout();Verify(Descendants<ScrollViewer>(modal).Any(),"discard preview returns to discard browser",results);modal.Children.Clear();
  PresentFixture(8,2,true,true);SelectPaymentForCheck(2,1);BeginPlay(state.Players[0].Hand[0]);Verify(state.Choice!=null && state.Choice.Preparing,"selected mixed payment still offers extra card cost",results);
  Action("cancel");await Idle();Verify(state.Players[0].Mana==3 && state.Players[0].Crystals==3 && state.Players[0].HandCount==3,"cancel extra cost does not spend selected payment",results);
  SelectPaymentForCheck(1,1);viewer=1;Render();Verify(SelectedMana==0 && SelectedCrystals==0,"test-mode view switch clears former player's payment selection",results);
  ToggleResource(false,0);Verify(SelectedMana==0,"off-turn resource selection is blocked",results);
  viewer=0;Render();SelectPaymentForCheck(1,0);Action("end");await Idle();Verify(viewer==1 && SelectedMana==0 && SelectedCrystals==0,"turn changes clear local selection",results);
  PresentFixture(19,0,true);revision=engine.Revision;BeginFlip(false);Verify(engine.Revision==revision,"orientation change also requires selected payment",results);SelectPaymentForCheck(0,1);BeginFlip(false);await Idle();Verify(state.UprightPlayer==1 && state.Players[0].Mana==3 && state.Players[0].Crystals==2,"orientation change consumes the selected resource",results);
 }
 public async Task RunPlaybackChecks(string folder){
  ShowActivated=false;ShowInTaskbar=false;WindowStartupLocation=WindowStartupLocation.Manual;Left=-20000;Top=-20000;Show();
  Directory.CreateDirectory(folder);var results=new List<string>();sound=false;await CheckAmbience(folder,results);mode="test";engine=new Engine("测试玩家 I","测试玩家 II",rules,72);
  viewer=engine.Active;state=engine.View(viewer,true);Render();Capture(Path.Combine(folder,"01-draft.png"));
  var target=stage.InputHitTest(new Point(325,305)) as Border;Verify(target!=null && target.Child is CardFace,"draft hit areas",results);
  target.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,0,MouseButton.Left){RoutedEvent=Mouse.MouseUpEvent});Click(NamedButton("确认禁用"));Verify(engine.DraftIndex==1,"draft confirmation advances operator",results);

  PresentFixture(19,0,true);Capture(Path.Combine(folder,"02-battle.png"));
  var hand=stage.InputHitTest(new Point(530,735)) as Border;Verify(hand!=null && hand.Child is CardFace,"horizontal hand receives input",results);
  SelectPaymentForCheck(1,0);int rev=state.Revision,pos=state.Position;BeginPlay(state.Players[0].Hand[0]);Verify(busy && state.Revision==rev && state.Position==pos,"cast animation holds old visible state",results);
  await Task.Delay(600);Capture(Path.Combine(folder,"03-cast.png"));await Idle();
  Verify(state.Position==11 && state.Players[0].Tokens[2]==1 && effects.Children.Count==0,"damage and token steps settle after animation",results);
  SelectPaymentForCheck(1,0);BeginFlip(false);Verify(busy,"generic flip animates",results);await Idle();Verify(state.UprightPlayer==1,"flip changes relative orientation",results);

  PresentFixture(15,2,true);ShowPreview(state.Players[0].Hand[0],570,188,false);Capture(Path.Combine(folder,"04-long-card-preview.png"));
  Verify(preview!=null && preview.Width==330,"large current-face preview",results);previews.Children.Clear();
  SelectPaymentForCheck(3,0);Verify(Payment(state.Players[0].Hand[0])==0,"selected three mana preserves crystals for Devil extra cost",results);Capture(Path.Combine(folder,"05-payment.png"));

  PresentFixture(8,2,true,true);SelectPaymentForCheck(3,0);BeginPlay(state.Players[0].Hand[0]);
  Verify(state.Choice!=null && state.Choice.Preparing && engine.Players[0].Mana==3,"optional-cost overlay precedes payment",results);Capture(Path.Combine(folder,"06-extra-cost.png"));
  Click(OptionButton(3));Click(NamedButton("确认选择"));await Idle();Verify(state.Position==14 && state.Players[0].Tokens[2]==0,"extra-cost buttons execute six-damage Strength core",results);

  PresentFixture(2,0,false);Action("play",1,0);await Idle();Verify(state.Choice!=null && state.Choice.Kind=="scry","private scry opens after cast presentation",results);
  Verify(page.Children.OfType<CardFace>().Any(f=>f.Card.Id==1) && !state.Players[0].Discard.Any(c=>c.Id==1),"unresolved card stays on altar outside discard",results);
  var options=state.Choice.Options.ToList();Click(OptionButton(options[1].Id));Click(OptionButton(options[0].Id));Descendants<ComboBox>(modal).Single().SelectedIndex=1;Capture(Path.Combine(folder,"07-scry.png"));
  Verify(Math.Abs(OptionButton(options[0].Id).TransformToAncestor(stage).Transform(new Point()).Y-OptionButton(options[1].Id).TransformToAncestor(stage).Transform(new Point()).Y)<1,"small scry selection remains side by side without vertical overflow",results);
  Click(NamedButton("确认选择"));await Idle();
  Verify(state.ChoiceOwner==-1 && state.Players[0].Hand.Any(c=>c.Id==options[1].Id) && engine.Players[0].Deck.Last().Id==options[0].Id,"scry UI order and top-count drive authoritative draw",results);

  PresentFixture(18,2,false,false,"solo");Action("play",1,3);await Idle();Verify(state.Choice!=null && state.Choice.Kind=="inspect" && state.Players[1].Hand.Count==0,"authorized peek uses temporary inspect overlay",results);Capture(Path.Combine(folder,"08-inspection.png"));Click(NamedButton("继续结算"));await Idle();Verify(state.Choice==null && state.Players[1].Hand.Count==0 && modal.Children.Count==0,"inspection clears after acknowledgement",results);

  PresentFixture(16,2,true,true);Action("play",1,0);await Idle();Verify(viewer==1 && state.Active==0 && state.Choice.Owner==1,"test mode switches to opponent token-loss choice",results);Capture(Path.Combine(folder,"09-opponent-choice.png"));
  foreach(int id in engine.SuggestChoice().Values)Click(OptionButton(id));Click(NamedButton("确认选择"));await Idle();Verify(viewer==0 && state.Active==0 && state.Position==12,"test mode returns to caster after off-turn choice",results);

  PresentFixture(14,2,false,true);Action("play",1,2);await Idle();foreach(int id in engine.SuggestChoice().Values)Click(OptionButton(id));Click(NamedButton("确认选择"));await Idle();
  Verify(state.PendingTokens==0 && state.Players[0].Tokens.Sum()==14,"overflow overlay waits for whole card",results);Capture(Path.Combine(folder,"10-overflow.png"));Action("discardToken",0);Verify(state.PendingTokens==-1 && state.Players[0].Tokens.Sum()==13,"overflow choice resumes turn",results);

  PresentFixture(19,0,true);Action("play",1,0);engine.Apply(0,new ActionMessage{Type="end",Revision=engine.Revision});Receive(engine.View(0,true));Verify(pendingSnapshots.Count==1,"new snapshot queues behind current animation",results);await Idle();Verify(state.Revision==engine.Revision && viewer==1 && !busy,"queue drains without losing operator change",results);

  PresentFixture(19,0,true,false,"host");var observer=new GameWindow();observer.sound=false;observer.mode="guest";observer.viewer=1;observer.engine=null;observer.state=engine.View(1,false);observer.ShowActivated=false;observer.ShowInTaskbar=false;observer.WindowStartupLocation=WindowStartupLocation.Manual;observer.Left=-22000;observer.Top=-20000;observer.Show();observer.Render();
  Action("play",1,0);observer.Receive(engine.View(1,false));Verify(busy && observer.busy && state.Position==10 && observer.state.Position==10,"both client views animate before settlement",results);
  await Task.Delay(580);Capture(Path.Combine(folder,"11-host-cast.png"));observer.Capture(Path.Combine(folder,"12-guest-cast.png"));await Idle();await observer.Idle();
  Verify(state.Position==11 && observer.state.Position==11 && Math.Abs(Canvas.GetLeft(pawn)-(224+11*48))<1 && Math.Abs(Canvas.GetLeft(observer.pawn)-(224+9*48))<1,"both clients finish with mirrored road positions",results);
  Verify(state.Players[1].Hand.Count==0 && observer.state.Players[0].Hand.Count==0,"both client presentations retain hidden hands",results);observer.Close();

  await CheckNewInteractions(folder,results);
  Library();Capture(Path.Combine(folder,"13-library.png"));DeckDetails(15);Capture(Path.Combine(folder,"14-deck-details.png"));
  for(int a=0;a<21;a++)for(int r=0;r<3;r++)foreach(bool up in new[]{true,false}){
   var rule=rules.Get(new Card{Arcana=a,Rank=r});double size=12;FormattedText text;do{text=new FormattedText(up?rule.Upright:rule.Reversed,CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface("Microsoft YaHei UI"),size,Ink.B(Ink.White),1){MaxTextWidth=184};if(text.Height<=85 || size<=8.5)break;size-=.25;}while(true);
   Verify(text.Height<=85,"card text fits: "+rule.Name+" "+(up?"up":"reversed"),results);
  }
  modal.Children.Clear();previews.Children.Clear();effects.Children.Clear();page.Children.Clear();
  Label(page,"费用徽章 · 左上角",104,35,30,Ink.White);
  string[] payments={"mana","crystal","any"};
  string[] captions={"仅回合费用","仅法力水晶","通用费用 · 可任选 / 混合"};
  for(int i=0;i<3;i++){
   var def=rules.Definitions.First(rule=>rule.Payment==payments[i] && rule.Cost==2);
   var card=new Card{Arcana=def.Arcana,Rank=def.Rank};double x=104+i*435;
   Label(page,captions[i],x,93,21,Ink.White,420);
   Put(page,new CardFace(card,rules){Upright=true},x,140,275,425);
   Put(page,new CardFace(card,rules){Upright=true},x,612,167,258);
  }
  Label(page,"手牌尺寸",104,580,15,Ink.Muted);
  Capture(Path.Combine(folder,"15-payment-badges.png"));
  File.WriteAllLines(Path.Combine(folder,"playback-test-results.txt"),results.Concat(new[]{"TOTAL: "+results.Count+" passed"}));Close();
 }
}
}
