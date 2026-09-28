using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ArcanaDuel {
public partial class GameWindow {
 void Submit(ActionMessage action){
  if(busy || state==null || disconnected)return;action.Revision=state.Revision;
  if(mode=="guest"){try{room.Send(new Packet{Kind="action",Payload=Room.Json(action)});}catch(Exception ex){Toast(ex.Message);}return;}ApplyHost(viewer,action);
 }
 List<int> Payments(Card card){
  var me=state.Players[viewer];var r=rules.Get(card);var result=new List<int>();
  for(int cry=0;cry<=r.Cost;cry++){
   if(cry>me.Crystals || r.Cost-cry>me.Mana || r.Payment=="mana" && cry!=0 || r.Payment=="crystal" && cry!=r.Cost)continue;
   bool up=state.UprightPlayer==viewer;
   if(card.Arcana==15 && card.Rank==2 && up && me.Crystals-cry<1)continue;
   if(card.Arcana==15 && (card.Rank==0 && up || card.Rank==2 && !up) && me.HandCount<2)continue;
   result.Add(cry);
  }
  return result;
 }
 void BeginPlay(Card card){
  if(!MyTurn() || state.Phase!="battle" || modal.Children.Count>0)return;
  int pay=Payment(card);if(pay<0){Toast(PaymentHint(card));return;}
  Action("play",card.Id,pay);
 }
 void BeginFlip(bool preferCrystal){
  if(!MyTurn() || modal.Children.Count>0)return;SyncPaymentSelection();int cost=state.Players[viewer].Confused?2:1;
  if(SelectedMana+SelectedCrystals!=cost){Toast("请点选合计 "+cost+" 点资源，再点击星盘转位。");return;}
  Action("flip",0,SelectedCrystals);
 }
 void ChoiceModal(){
  var choice=state.Choice;if(choice==null || choice.Owner!=viewer)return;
  if(choice.Kind=="option"){OptionModal(choice);return;}
  modal.Children.Clear();previews.Children.Clear();
  Put(modal,new Border{Background=Ink.B("#E0090C15")},0,0,1440,900);
  Put(modal,Panel(1220,756,"#151822","#9F8A68"),110,72);
  Label(modal,choice.Title,150,108,24,Ink.Gold,1120);
  string instruction=choice.Kind=="scry" || choice.Kind=="order"?"依次点击排序；再次点击可撤回。":choice.Kind=="inspect"?"查看完毕后继续结算。":"选择 "+choice.Min+"–"+choice.Max+" 项"+(choice.Min==0?"，可直接跳过。":"。");
  Label(modal,instruction,150,151,15,Ink.Muted,1110);
  var selected=new List<int>();var ordered=choice.Kind=="scry" || choice.Kind=="order";
  var buttons=new Dictionary<int,Button>();var captions=new Dictionary<int,TextBlock>();
  bool cardChoices=choice.Options.Any(o=>o.Card!=null);double cardWidth=choice.Options.Count<=4?220:171;
  var wrap=new WrapPanel{Orientation=Orientation.Horizontal};if(cardChoices)wrap.Width=Math.Min(1118,choice.Options.Count*(cardWidth+56));
  var scroll=new ScrollViewer{Content=wrap,HorizontalContentAlignment=HorizontalAlignment.Center,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
  Put(modal,scroll,148,204,1146,465);
  var status=T("",17,Ink.White);Put(modal,status,150,692,920);
  ComboBox topCount=null;
  if(choice.Kind=="scry"){
   Label(modal,"前几张留在牌顶",790,687,15,Ink.Muted);topCount=new ComboBox{Width=90,Height=31,FontSize=17,ItemsSource=Enumerable.Range(0,choice.Options.Count+1).ToArray(),SelectedIndex=choice.Options.Count};Put(modal,topCount,956,682);
  }
  Action update=()=>{
   foreach(var opt in choice.Options){bool on=selected.Contains(opt.Id);buttons[opt.Id].BorderBrush=Ink.B(on?"#F6D785":"#4C4354");buttons[opt.Id].BorderThickness=new Thickness(on?3:1);captions[opt.Id].Text=(on?(ordered?(selected.IndexOf(opt.Id)+1)+" · ":"✓ "):"")+opt.Text;}
   status.Text=ordered?"排序："+String.Join(" → ",selected.Select(id=>(choice.Options.FindIndex(o=>o.Id==id)+1).ToString())):"已选 "+selected.Count+" / "+choice.Max;
  };
  foreach(var opt in choice.Options){
   var item=opt;var panel=new StackPanel{Margin=new Thickness(7)};
   bool faceUp=choice.Kind=="inspect"?state.UprightPlayer==1-viewer:choice.Upright;
   if(item.Card!=null)panel.Children.Add(new CardFace(item.Card,rules){Width=cardWidth,Height=cardWidth*340/220,Upright=faceUp});
   else if(item.Suit>=0)panel.Children.Add(new Relic("seal",new[]{"♧","♜","†","◇"}[item.Suit]){Width=58,Height=58,Color=new[]{"#A9BC75","#7BC7D8","#B6C2D4","#E1BE68"}[item.Suit]});
   var caption=T(item.Text,item.Card!=null?12:17,Ink.White);caption.Width=item.Card!=null?cardWidth+4:225;caption.TextAlignment=TextAlignment.Center;panel.Children.Add(caption);
   var button=new Button{Content=panel,Tag=item.Id,Background=Ink.B("#171E2C"),BorderBrush=Ink.B("#4C4354"),BorderThickness=new Thickness(1),Margin=new Thickness(7),Cursor=Cursors.Hand,Padding=new Thickness(3)};
   button.Click+=(s,e)=>{if(choice.Kind=="inspect")return;if(selected.Contains(item.Id))selected.Remove(item.Id);else if(selected.Count<choice.Max)selected.Add(item.Id);else if(choice.Max==1){selected.Clear();selected.Add(item.Id);}update();};
   if(item.Card!=null){button.ToolTip=new TextBlock{Text=faceUp?rules.Get(item.Card).Upright:rules.Get(item.Card).Reversed,Width=360,FontSize=15,TextWrapping=TextWrapping.Wrap};}
   buttons.Add(item.Id,button);captions.Add(item.Id,caption);wrap.Children.Add(button);
  }
  update();
  Put(modal,Btn(choice.Kind=="inspect"?"继续结算":"确认选择",()=>{
   if(selected.Count<choice.Min || selected.Count>choice.Max){Toast("请选择规定数量的目标。");return;}
   var a=new ActionMessage{Type="choose",ChoiceId=choice.Id,Values=selected.ToArray(),Split=topCount==null?0:topCount.SelectedIndex};modal.Children.Clear();Submit(a);
  },225,true),1063,750);
  if(choice.Preparing)Put(modal,Btn("取消施放",()=>{modal.Children.Clear();Action("cancel");},165),150,750);
 }
 void OptionModal(Choice choice){
  modal.Children.Clear();previews.Children.Clear();Put(modal,new Border{Background=Ink.B("#CA080D19")},0,0,1440,900);Put(modal,Panel(890,552,"#151822","#9F8A68"),275,174);
  if(choice.Source!=null)Put(modal,new CardFace(choice.Source,rules){Upright=choice.Upright},323,232,236,365);
  Label(modal,choice.Title,607,217,23,Ink.Gold,494);int selected=-1,line=0;var options=new List<Button>();
  foreach(var option in choice.Options){int id=option.Id;var button=Btn(option.Text,()=>{selected=id;foreach(var b in options){bool on=(int)b.Tag==id;b.BorderBrush=Ink.B(on?Ink.Gold:"#49546A");b.Background=Ink.B(on?"#403526":"#1A2940");}},467);button.Tag=id;button.Height=49;options.Add(button);Put(modal,button,607,306+line++*59);}
  Put(modal,Btn("确认选择",()=>{if(selected<0){Toast("请先选择一个选项。");return;}modal.Children.Clear();Submit(new ActionMessage{Type="choose",ChoiceId=choice.Id,Values=new[]{selected}});},226,true),848,650);
  if(choice.Preparing)Put(modal,Btn("取消施放",()=>{modal.Children.Clear();Action("cancel");},165),323,650);
 }
 void DeckDetails(int arc,bool up=true){
  ModalBase(1250,790);Label(modal,Engine.Romans[arc]+" · "+Engine.Names[arc],134,87,28,Assets.Colors[arc]);
  Put(modal,Btn("正位",()=>DeckDetails(arc,true),100,up),851,87);Put(modal,Btn("逆位",()=>DeckDetails(arc,false),100,!up),965,87);Put(modal,Btn("← 图鉴",()=>Library(),128),1084,87);
  for(int r=0;r<3;r++){
   var card=new Card{Arcana=arc,Rank=r};var rule=rules.Get(card);double x=184+r*370;
   Put(modal,new CardFace(card,rules){Upright=up},x,171,270,417);
   Label(modal,(r<2?"×2":"×1")+"   "+CardFace.PayName(rule.Payment),x,600,14,Assets.Colors[arc],300);
   Label(modal,up?"↓ 逆位":"↑ 正位",x,638,14,Ink.Gold,310);Label(modal,up?rule.Reversed:rule.Upright,x,666,14,Ink.White,310);
  }
 }
 void RulesHelp(){
  ModalBase(1090,760);Label(modal,"星途规则 · v0.2",214,103,30,Ink.Gold);
  var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto};scroll.Content=T(
   "禁选与胜负\n20 副大阿尔卡纳参与禁选，不含愚者、世界。双方各选 3 副，再加入愚者；每副 A×2、B×2、C×1，共 20 张。道路 21 格，中央出发，向自己右方推进 10 格到终点获胜。\n\n回合与手牌\n每回合 3 费用；剩余费用在结束时转成水晶，上限 3。初始手牌 5，先手首回合不抽，其后回合开始抽 1。手牌上限 5，新增牌溢出入弃牌区；牌库空时洗回弃牌。结算中的牌不能抽回或回收自己。\n\n正逆位与出牌\n双方正逆位相反。先点选资源，再点击星盘支付并转位；通常 1 点，迷惑时 2 点。点击自己的费用或水晶选中，再点取消；选择的总点数与牌费一致时，拖牌到场中按该组合支付。选多、选少或类型不符时不出牌。悬停手牌按 R 查看另一牌面，单击或右键手牌打开可切换正逆位的预览；预览不改变实际局面。牌面左上角：圆形仅用回合费用，菱形仅用水晶，上尖下圆的水滴形为通用费用，可任选一种或混合支付；徽章颜色随卡牌，数字是总点数。额外代价在施放前选择，确认前可取消；施放后必须完成结算选择。\n\n四类资源\n权杖、圣杯、宝剑、星币合计最多 13。资源仅按卡牌效果获得、消耗或调色，没有通用兑换。整张牌结算完才处理超限，由拥有者选择弃置。\n\n屏障与迷惑\n屏障最多 3，等量抵消对手伤害，在自己的下回合开始时清除。后退不受屏障保护，到达对手终点立即失败。迷惑使下回合首次通用转位额外消耗 1 点费用或水晶，不叠加；回合结束失效。卡牌效果转位不受迷惑影响。\n\n预知、展示与回收\n预知可重排牌库顶，并选择任意数量放到牌库底；按点击顺序排序，再选择顶部保留数。预知结果仅自己可见，展示牌顶双方可见。回收从弃牌区选牌加入手牌，仍遵守 5 张上限。月蚀的对手手牌仅在本次查看窗口显示。\n\n条件与时点\n占优/劣势以中央格为界。牌面条件在支付固定与额外代价、卡牌离手后记录；“此前”不含本牌。效果抽牌包含溢出的抽牌，但不含回合开始抽牌、预知、回收和直接挑选。到达终点立即停止其余效果。\n\n所有 63 种牌的正逆位效果均可在图鉴查看。联机由房主验证操作；双方播放同一出牌与分段效果。双人测试会自动切换到需要行动或选择的玩家。",17,"#D8D0C4");Put(modal,scroll,216,164,1000,630);
 }
 async Task AnimateCardEvent(Event ev,bool entering,bool awaitingChoice=false){
  int gen=session;effects.Children.Clear();previews.Children.Clear();
  CardFace face=null;Border shade=null;
  if(entering && ev.Card!=null){
   foreach(var source in page.Children.OfType<Border>()){var cardFace=source.Child as CardFace;if(cardFace!=null && cardFace.Card.Id==ev.Card.Id && ev.Actor==viewer)source.Opacity=0;}
   if(ev.Actor!=viewer){var source=page.Children.OfType<CardFace>().LastOrDefault(f=>f.Back || f.Card.Id==ev.Card.Id);if(source!=null)source.Opacity=0;}
   shade=new Border{Background=Ink.B("#AE0A0E18"),Width=1440,Height=900};Put(effects,shade,0,0);Animate(shade,OpacityProperty,0,1,130);
   face=new CardFace(ev.Card,rules){Upright=ev.Upright};face.Effect=new System.Windows.Media.Effects.DropShadowEffect{Color=Ink.C(Assets.Colors[ev.Card.Arcana]),BlurRadius=34,ShadowDepth=0,Opacity=.9};
   Put(effects,face,ev.Actor==viewer?610:740,ev.Actor==viewer?650:95,248,383);Animate(face,Canvas.LeftProperty,ev.Actor==viewer?610:740,596,360);Animate(face,Canvas.TopProperty,ev.Actor==viewer?650:95,180,360);
   var title=T(state.Players[ev.Actor].Name+" · "+(ev.Upright?"正位":"逆位"),21,Ink.Gold);title.TextAlignment=TextAlignment.Center;Put(effects,title,495,126,450);
   await Task.Delay(780);if(gen!=session)return;
   Animate(shade,OpacityProperty,1,.2,160);Animate(face,OpacityProperty,1,.28,160);
  }
  else if(ev.Card!=null && state.ResolvingCard!=null){
   face=new CardFace(ev.Card,rules){Upright=ev.Upright,Opacity=.35};Put(effects,face,596,180,248,383);
  }
  foreach(var step in ev.Steps??new List<EffectEvent>()){
   if(gen!=session)return;
   var text=T(step.Text,25,step.Kind=="damage"?Ink.Gold:"#B8D6E4");text.TextAlignment=TextAlignment.Center;Put(effects,text,410,569,620);Animate(text,OpacityProperty,0,1,120);
   bool local=step.Actor==viewer;
   if(step.Kind=="damage" || step.Kind=="retreat"){
    int before=viewer==0?step.Before:Engine.RoadEnd-step.Before,after=viewer==0?step.After:Engine.RoadEnd-step.After;
    if(pawn!=null)Animate(pawn,Canvas.LeftProperty,224+before*48,224+after*48,480);
    for(int i=0;i<14;i++){double t=i*Math.PI*2/14;var spark=new Ellipse{Width=5,Height=5,Fill=Ink.B(step.Kind=="damage"?Ink.Gold:"#BD9DEE")};Put(effects,spark,720,392);Animate(spark,Canvas.LeftProperty,720,720+Math.Cos(t)*210,460);Animate(spark,Canvas.TopProperty,392,392+Math.Sin(t)*120,460);Animate(spark,OpacityProperty,1,0,460);}
    if(sound)System.Media.SystemSounds.Asterisk.Play();await Task.Delay(520);
   }else if(step.Kind=="draw" || step.Kind=="recover"){
    for(int i=0;i<Math.Min(5,step.Amount);i++){var back=new CardFace(new Card(),rules){Back=true};Put(effects,back,1210,local?540:130,62,96);Animate(back,Canvas.LeftProperty,1210,local?510+i*91:575+i*62,440,i*60);Animate(back,Canvas.TopProperty,local?540:130,local?655:111,440,i*60);Animate(back,OpacityProperty,1,0,170,330+i*60);}
    await Task.Delay(480+Math.Min(5,step.Amount)*60);
   }else if(step.Kind=="reveal" && step.Cards!=null){
    var revealed=new List<CardFace>();int count=step.Cards.Count;
    for(int i=0;i<count;i++){var card=new CardFace(step.Cards[i],rules){Upright=ev.Upright};Put(effects,card,720-count*100+i*200,255,180,278);revealed.Add(card);}await Task.Delay(1050);foreach(var card in revealed)effects.Children.Remove(card);
   }else if(step.Kind=="flip"){await AnimateFlip(ev);}
   else{
    var rune=new Relic("seal",step.Kind=="shield"?"▱":step.Kind=="confuse"?"?":step.Kind=="mana"?"●":step.Kind=="crystal"?"◆":"✦"){Color=step.Kind=="shield"?"#85D5CB":"#CBB5EE"};Put(effects,rune,local?651:660,local?588:45,90,90);Animate(rune,OpacityProperty,0,1,140);await Task.Delay(350);effects.Children.Remove(rune);
   }
   effects.Children.Remove(text);
  }
  if(gen!=session)return;
  if(face!=null && !awaitingChoice){Animate(face,Canvas.LeftProperty,596,1250,210);Animate(face,Canvas.TopProperty,180,ev.Actor==viewer?550:140,210);Animate(face,OpacityProperty,face.Opacity,0,210);await Task.Delay(230);}
 }
 async void ScheduleBot(){
  if(mode!="solo" || state==null || state.Phase=="ended" || busy || aiScheduled || engine==null)return;
  int owner=engine.PendingChoice!=null?engine.PendingChoice.Owner:engine.PendingTokens>=0?engine.PendingTokens:engine.Active;if(owner!=1)return;
  aiScheduled=true;int gen=session;await Task.Delay(620);aiScheduled=false;if(gen!=session || mode!="solo" || busy)return;
  var next=engine.SuggestAction(1,rng);if(next!=null)ApplyHost(1,next);
 }
}
}
