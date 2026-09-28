using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace ArcanaDuel {
public partial class GameWindow {
 FrameworkElement castRune;
 void SceneBase(bool dim=false){page.Children.Clear();previews.Children.Clear();Put(page,new SceneBoard{IsHitTestVisible=false},0,0,1440,900);if(dim)Put(page,new Border{Background=Ink.B("#78050A15"),IsHitTestVisible=false},0,0,1440,900);AddAtmosphere(false);}
 TextBlock Inscribe(Canvas on,string text,double x,double y,double size=16,string color=Ink.White,double width=Double.NaN){var t=T(text,size,color);t.Effect=new DropShadowEffect{Color=Colors.Black,BlurRadius=5,ShadowDepth=1,Opacity=1};Put(on,t,x,y,width);return t;}
 Button Artifact(string kind,string caption,Action action,double x,double y,double size,string hint,bool lit=true,string color=Ink.Gold){var b=Btn("",action,size);b.Height=size;b.Background=Brushes.Transparent;b.BorderThickness=new Thickness(0);b.Content=new Relic(kind,caption){Width=size,Height=size,Lit=lit,Color=color};b.ToolTip=hint;b.Effect=new DropShadowEffect{Color=Colors.Black,BlurRadius=14,ShadowDepth=4,Opacity=.7};Put(page,b,x,y);return b;}
 void Battle(){
  SyncPaymentSelection();SceneBase();CardFace.CurrentUpright=state.UprightPlayer==viewer;
  var me=state.Players[viewer];var other=state.Players[1-viewer];bool mine=MyTurn();bool up=CardFace.CurrentUpright;
  // Opponent's zodiac seal and cards sit on the far side of the altar.
  Artifact("seal",state.UprightPlayer==1-viewer?"☀":"☾",()=>Help(),681,7,79,"对手："+other.Name+" · "+(up?"逆位":"正位"),true,"#CCB5E8");
  Inscribe(page,other.Name,498,25,18,"#EEE4D5",172);Inscribe(page,other.HandCount+" / 5",499,53,12,"#B6ADBC");
  for(int i=0;i<3;i++){Put(page,new Relic("mana",""){Lit=i<other.Mana,Color="#66B7D2"},800+i*28,25,25,31);Put(page,new Relic("gem",""){Lit=i<other.Crystals,Color="#BC91ED"},903+i*28,25,25,31);}
  double ox=720-(other.HandCount*64+18)/2.0;
  for(int i=0;i<other.HandCount;i++){Card c=revealBoth && mode=="test"?other.Hand[i]:new Card{Arcana=0};var face=new CardFace(c,rules){Back=!(revealBoth && mode=="test"),Upright=!up};face.RenderTransform=new RotateTransform((i-(other.HandCount-1)/2.0)*4,40,0);Put(page,face,ox+i*66,101,77,119);}
  DrawPile(1-viewer,false,1124,112,.66);DrawPile(1-viewer,true,1220,145,.55);
  // Rules information lives in the book at the edge, rather than in side panels.
  Artifact("seal","☰",()=>SceneMenu(),32,28,51,"菜单、规则、对局记录");
  Artifact("seal",state.Turn.ToString(),()=>{},98,31,43,"第 "+state.Turn+" 回合",false);
  if(mode=="test"){
   Artifact("seal","⇄",()=>{if(!busy){viewer=1-viewer;Render();}},37,107,47,"双人测试：切换操作者");
   Artifact("seal",revealBoth?"◉":"◎",()=>{revealBoth=!revealBoth;Render();},95,107,47,"双人测试：切换双方明牌显示",revealBoth);
  }else Artifact("seal",mode=="solo"?"✦":"∞",()=>{},38,112,42,mode=="solo"?"单人练习":"已连接双人对战",true);
  // 21 cells: ten steps to either goal from the central eleventh cell.
  for(int i=0;i<Engine.RoadLength-1;i++){var link=new Line{X1=260+i*48,Y1=392,X2=272+i*48,Y2=392,Stroke=Ink.B("#9F906A"),StrokeThickness=3,IsHitTestVisible=false};page.Children.Add(link);}
  for(int i=0;i<Engine.RoadLength;i++){var tile=new Relic("tile",i==0?"☾":i==Engine.RoadEnd?"☀":i==Engine.RoadCenter?"✦":i==5 || i==15?"5":"·"){Color=i==0?"#C1A4D9":i==Engine.RoadEnd?"#EAD082":"#B2AB99"};tile.Effect=new DropShadowEffect{Color=Colors.Black,BlurRadius=7,ShadowDepth=5,Opacity=.8};Put(page,tile,218+i*48,350,44,85);}
  int pos=viewer==0?state.Position:Engine.RoadEnd-state.Position;pawn=new Border{Width=32,Height=64,Child=new Relic("gem","✦"){Color="#EDC465"},IsHitTestVisible=false};pawn.Effect=new DropShadowEffect{Color=Ink.C("#F5C97E"),BlurRadius=25,ShadowDepth=0,Opacity=.9};Put(page,pawn,224+pos*48,359);
  var end=Artifact("seal",mine?"▶":"◷",()=>Action("end"),1279,325,106,mine?"结束回合：未使用的回合费用转为水晶，最多保存 3 枚。":"等待对方回合",mine);end.IsEnabled=mine;
  if(mine)end.Effect=new DropShadowEffect{Color=Ink.C("#F8D582"),BlurRadius=24,ShadowDepth=0,Opacity=.7};
  // Four physical token seals with counters; hover explains each resource.
  string[] glyphs={"♧","♜","†","◇"};string[] names={"权杖","圣杯","宝剑","星币"};string[] tokenColors={"#A9BC75","#7BC7D8","#B6C2D4","#E1BE68"};
  for(int i=0;i<4;i++){double x=39+(i%2)*76,y=323+(i/2)*86;Artifact("seal",glyphs[i],()=>{},x,y,61,names[i]+"："+me.Tokens[i]+"。四类资源合计上限 13。",me.Tokens[i]>0,tokenColors[i]);Inscribe(page,me.Tokens[i].ToString(),x+48,y+46,16,Ink.White);}
  Inscribe(page,me.Tokens.Sum()+" / 13",81,501,12,"#C3B698");
  string oppTokens=String.Join("  ",other.Tokens.Select((n,i)=>glyphs[i]+n));Inscribe(page,oppTokens,309,109,12,"#C8BED5",175);
  // The astrolabe consumes the locally selected payment, just like a card.
  var dial=Artifact("seal",up?"☀":"☾",()=>BeginFlip(false),176,606,108,"先点选资源，再点击星盘转位：通常 1 点，迷惑时 2 点。",mine,up?"#F0CC74":"#B5B8FF");
  dial.MouseRightButtonUp+=(s,e)=>{if(MyTurn())BeginFlip(true);e.Handled=true;};
  Inscribe(page,me.Name,182,800,19,Ink.White,150);
  // Costs are luminous stones embedded in a small brass tray beside the hand.
  DrawPaymentResources();
  DrawPile(viewer,false,1117,528,.85);DrawPile(viewer,true,1250,550,.75);
  // A horizontal, slightly overlapping hand: every card remains directly selectable.
  double start=720-(167+Math.Max(0,me.Hand.Count-1)*145)/2;
  for(int i=0;i<me.Hand.Count;i++){
   Card card=me.Hand[i];double x=start+i*145;var face=new CardFace(card,rules){Upright=up};var b=new Border{Width=167,Height=258,Child=face,Background=Brushes.Transparent,Cursor=mine?Cursors.Hand:Cursors.Arrow};Put(page,b,x,638);bool playable=mine && Payment(card)>=0;b.Effect=new DropShadowEffect{Color=Ink.C(playable?"#71E0BB":"#000000"),BlurRadius=playable?13:14,ShadowDepth=playable?0:6,Opacity=playable?.66:.9};
   var lift=new TranslateTransform();face.RenderTransform=lift;
   b.MouseEnter+=(s,e)=>{if(dragging==null && !busy){lift.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(0,-23,TimeSpan.FromMilliseconds(130)));System.Windows.Controls.Panel.SetZIndex(b,15);ShowPreview(card,Math.Min(905,x+22),188,false);}};
   b.MouseLeave+=(s,e)=>{if(dragging==null){lift.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(lift.Y,0,TimeSpan.FromMilliseconds(140)));System.Windows.Controls.Panel.SetZIndex(b,0);previews.Children.Clear();}};
   b.ToolTip="拖动出牌 · 单击或右键预览 · 悬停按 R 查看另一牌面";
   b.MouseRightButtonUp+=(s,e)=>{InspectCard(card,up);e.Handled=true;};
   b.MouseLeftButtonDown+=(s,e)=>{if(MyTurn())BeginDrag(card,b,e);else {InspectCard(card,up);e.Handled=true;}};b.MouseMove+=MoveDrag;b.MouseLeftButtonUp+=EndDrag;b.LostMouseCapture+=(s,e)=>{if(dragging!=null)CancelDrag();};
  }
  if(me.Shield>0)Artifact("seal","▱"+me.Shield,()=>{},303,608,53,"屏障：抵消伤害；下回合开始清除",true,"#88D9D1");
  if(other.Shield>0)Artifact("seal","▱"+other.Shield,()=>{},393,33,49,"对手屏障",true,"#88D9D1");
  if(me.Confused)Artifact("seal","?",()=>{},308,674,44,"迷惑：下次通用转位额外消耗 1 点",true,"#BF96E1");
  if(other.Confused)Artifact("seal","?",()=>{},445,37,43,"对手受到迷惑",true,"#BF96E1");
  if(state.ResolvingCard!=null)Put(page,new CardFace(state.ResolvingCard,rules){Upright=state.ResolvingUpright},632,210,176,272);
  if(state.ChoiceOwner>=0 && state.ChoiceOwner!=viewer)Artifact("seal","◷",()=>{},688,488,48,"等待 "+state.Players[state.ChoiceOwner].Name+" 完成选择",true,"#C5B1DE");
  if(state.Phase=="ended")Victory();else if(state.ChoiceOwner>=0)ChoiceModal();else if(state.PendingTokens>=0)TokenModal();
 }
 void DrawPile(int p,bool discard,double x,double y,double scale){
  var player=state.Players[p];int count=discard?player.Discard.Count:player.DeckCount;var cv=new Canvas{Width=110,Height=172,Background=Brushes.Transparent};
  for(int i=3;i>=0;i--){var c=discard && count>0?player.Discard[count-1]:new Card{Arcana=0};var f=new CardFace(c,rules){Back=!discard || count==0,Upright=state.UprightPlayer==p};Put(cv,f,i*3,i*3,94,145);}
  var coin=new Relic("seal",count.ToString()){Width=43,Height=43};Put(cv,coin,70,124);var box=new Border{Child=cv,Cursor=Cursors.Hand,ToolTip=discard?"弃牌区 · 点击查看":"牌库 · 还剩 "+count+" 张；抽空时洗回弃牌。",RenderTransform=new ScaleTransform(scale,scale)};box.MouseLeftButtonUp+=(s,e)=>{if(discard)Discard(p);else Toast("牌库剩余 "+count+" 张。"+(p==viewer?"你的牌组："+String.Join("、",player.Picks.Select(a=>Engine.Names[a])):""));};box.Effect=new DropShadowEffect{Color=Colors.Black,BlurRadius=8,ShadowDepth=8,Opacity=.9};Put(page,box,x,y,110,172);
 }
 void SceneMenu(){ModalBase(590,510);Label(modal,"星盘菜单",477,233,29,Ink.Gold);Put(modal,Btn("玩法指南",()=>Help(),200),477,303);Put(modal,Btn("命运记录",()=>History(),200),703,303);Put(modal,Btn(sound?"声音：开":"声音：关",()=>{sound=!sound;SceneMenu();},200),477,366);Put(modal,Btn("大阿尔卡纳图鉴",()=>Library(),200),703,366);Put(modal,Btn(atmosphere.Motion?"场景动效：开":"场景动效：关",()=>{ToggleMotion();SceneMenu();},200),477,426);Put(modal,Btn("继续对局",()=>modal.Children.Clear(),200,true),477,490);Put(modal,Btn("离开对局",()=>AskLeave(),200),703,490);}
 void History(){ModalBase(730,620);Label(modal,"命运记录",402,166,28,Ink.Gold);var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto};scroll.Content=T(String.Join("\n\n",state.Log.AsEnumerable().Reverse()),17,"#DDD3BE");Put(modal,scroll,402,226,623,472);}
}
}



