using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ArcanaDuel {
public partial class GameWindow : Window {
 Canvas stage=new Canvas{Width=1440,Height=900,ClipToBounds=true}; Canvas page=new Canvas{Width=1440,Height=900}; Canvas effects=new Canvas{IsHitTestVisible=false}; Canvas previews=new Canvas{IsHitTestVisible=false}; Canvas modal=new Canvas{Width=1440,Height=900};
 Rules rules; Engine engine; Snapshot state; Room room; string mode="menu"; int viewer; bool busy; bool disconnected; bool revealBoth; bool sound=true; int session; bool aiScheduled; string myName="旅人"; string roomCode; string networkStatus="";
 Card dragging; FrameworkElement dragSource; CardFace ghost; Point dragStart; bool dragMoved; CardFace preview; Border pawn; TextBox nameBox,ipBox,codeBox; TextBlock lobbyStatus;
 Queue<Snapshot> pendingSnapshots=new Queue<Snapshot>();
 Random rng=new Random();
 public GameWindow(){
  Title="星途对决 · ARCANA DUEL 1.4";Width=1360;Height=890;MinWidth=1000;MinHeight=670;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=Ink.B(Ink.Night);FontFamily=new FontFamily("Microsoft YaHei UI");
  rules=Rules.Load(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"cards.json"));
  var vb=new Viewbox{Stretch=Stretch.Uniform,Child=stage};Content=vb;stage.Children.Add(page);stage.Children.Add(effects);stage.Children.Add(previews);stage.Children.Add(modal);
  Closed+=(s,e)=>{atmosphere.Motion=false;session++;if(room!=null)room.Dispose();};KeyDown+=Key;MouseMove+=(s,e)=>{if(mode=="menu"){var point=e.GetPosition(stage);atmosphere.Pointer=new Point((point.X-720)/65,(point.Y-450)/65);}};MouseLeave+=(s,e)=>atmosphere.Pointer=new Point();
  Menu();
 }
 void Key(object s,KeyEventArgs e){if(e.Key==System.Windows.Input.Key.R && !e.IsRepeat){e.Handled=ToggleHoverPreview();}if(e.Key==System.Windows.Input.Key.Escape){if(state!=null && (state.ChoiceOwner>=0 || state.PendingTokens>=0)){if(state.Choice!=null && state.Choice.Preparing && state.Choice.Owner==viewer)Action("cancel");return;}if(dragging!=null)CancelDrag();else if(modal.Children.Count>0)modal.Children.Clear();else if(mode!="menu")AskLeave();}if(e.Key==System.Windows.Input.Key.F11)WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;}
 void Put(Canvas c,UIElement e,double x,double y,double w=Double.NaN,double h=Double.NaN){var f=e as FrameworkElement;if(f!=null){if(!Double.IsNaN(w))f.Width=w;if(!Double.IsNaN(h))f.Height=h;}Canvas.SetLeft(e,x);Canvas.SetTop(e,y);c.Children.Add(e);}
 TextBlock T(string text,double size=16,string color=Ink.White){return new TextBlock{Text=text,FontSize=size,Foreground=Ink.B(color),TextWrapping=TextWrapping.Wrap};}
 void Label(Canvas c,string text,double x,double y,double size=16,string color=Ink.White,double width=Double.NaN){Put(c,T(text,size,color),x,y,width);}
 Border Panel(double w,double h,string bg="#121D30",string border="#334055"){return new Border{Width=w,Height=h,Background=Ink.B(bg),BorderBrush=Ink.B(border),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(10)};}
 Button Btn(string text,Action click,double w=160,bool primary=false){
  var b=new Button{Content=text,Width=w,Height=44,FontSize=15,Cursor=Cursors.Hand,Foreground=Ink.B(primary?"#111A2A":Ink.White),Background=Ink.B(primary?Ink.Gold:"#1A2940"),BorderBrush=Ink.B(primary?"#F0D399":"#49546A"),BorderThickness=new Thickness(1),Padding=new Thickness(12,5,12,5)};
  var factory=new FrameworkElementFactory(typeof(Border));factory.SetValue(Border.CornerRadiusProperty,new CornerRadius(2));factory.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));factory.SetValue(Border.BorderBrushProperty,new TemplateBindingExtension(Control.BorderBrushProperty));factory.SetValue(Border.BorderThicknessProperty,new TemplateBindingExtension(Control.BorderThicknessProperty));
  var cp=new FrameworkElementFactory(typeof(ContentPresenter));cp.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);cp.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);factory.AppendChild(cp);b.Template=new ControlTemplate(typeof(Button)){VisualTree=factory};
  b.MouseEnter+=(s,e)=>{if(b.IsEnabled)b.Opacity=.8;};b.MouseLeave+=(s,e)=>b.Opacity=1;b.Click+=(s,e)=>click();b.IsEnabledChanged+=(s,e)=>b.Opacity=b.IsEnabled?1:.35;return b;
 }
 TextBox Input(string text,double w=270){return new TextBox{Text=text,Width=w,Height=44,FontSize=17,Background=Ink.B("#0B1527"),Foreground=Ink.B(Ink.White),BorderBrush=Ink.B("#58657A"),Padding=new Thickness(12,9,12,5),MaxLength=64};}
 void Base(bool menu=false){page.Children.Clear();previews.Children.Clear();Put(page,new SceneBoard(),0,0,1440,900);Put(page,new Border{Background=Ink.B("#B0080E19"),IsHitTestVisible=false},0,0,1440,900);AddAtmosphere(false);Label(page,"✦  ARCANA DUEL",34,24,18,Ink.Gold);Label(page,"星 途 对 决",34,53,11,Ink.Muted);var help=Btn("玩法指南",()=>Help(),108);Put(page,help,1164,25);Put(page,Btn(sound?"♫ 开":"♫ 关",()=>{sound=!sound;Render();},75),1284,25);}
 void Footer(string text){Label(page,text,36,869,12,Ink.Muted);Label(page,"ARCANA  /  01",1277,869,12,Ink.Gold);}
 void Menu(){mode="menu";state=null;engine=null;busy=false;disconnected=false;modal.Children.Clear();effects.Children.Clear();DrawHome();}
 void Cleanup(){ClearPaymentSelection();session++;CancelDrag();if(room!=null){room.Dispose();room=null;}effects.Children.Clear();modal.Children.Clear();pendingSnapshots.Clear();busy=false;aiScheduled=false;disconnected=false;}
 void StartLocal(bool ai){Cleanup();mode=ai?"solo":"test";engine=new Engine("旅人 I",ai?"星盘守卫":"旅人 II",rules,rng.Next());viewer=ai?0:engine.Active;state=engine.View(viewer,mode=="test");Render();ScheduleAI();}
 void Lobby(bool host){Cleanup();mode=host?"hostLobby":"joinLobby";Base(true);Put(page,Btn("← 返回",()=>{Cleanup();Menu();},100),34,99);Label(page,host?"开启一场命运之约":"赴约星途",142,166,38);Label(page,host?"另一位玩家使用同版本游戏，输入你的 IP 与房间口令。":"连接同一局域网的房主，或通过可达的 VPN / 公网 IP 连接。",145,228,17,Ink.Muted);
  Put(page,Panel(665,475),135,281);Label(page,"你的称呼",169,311,14,Ink.Gold);nameBox=Input(myName,290);nameBox.MaxLength=16;Put(page,nameBox,169,341);
  if(host){
   roomCode=rng.Next(100000,999999).ToString();Label(page,"本机 IPv4",169,416,14,Ink.Gold);string ips="";try{ips=String.Join("  /  ",Dns.GetHostAddresses(Dns.GetHostName()).Where(x=>x.AddressFamily==AddressFamily.InterNetwork).Select(x=>x.ToString()));}catch{}if(ips.Length==0)ips="未检测到网络；本机测试可使用 127.0.0.1";Label(page,ips,169,449,19,Ink.White,570);
   Label(page,"房间口令",169,509,14,Ink.Gold);Label(page,roomCode,169,537,36,Ink.Gold);Put(page,Btn("开始等待玩家",()=>HostRoom(),210,true),169,624);
  }else{Label(page,"房主 IP / 主机名",169,416,14,Ink.Gold);ipBox=Input("127.0.0.1",380);Put(page,ipBox,169,447);Label(page,"六位房间口令",169,510,14,Ink.Gold);codeBox=Input("",210);codeBox.MaxLength=6;Put(page,codeBox,169,540);Put(page,Btn("连接房间  →",()=>JoinRoom(),210,true),169,624);}
  lobbyStatus=T("",14,Ink.Gold);Put(page,lobbyStatus,169,692,585);Put(page,new CardFace(new Card{Arcana=17},rules,true),934,294,240,371);
  Label(page,"连接提示",916,706,17,Ink.Gold);Label(page,"TCP 端口 27841。房主需允许游戏通过专用网络防火墙。\n异地网络需要 VPN 或端口转发；本版不含公共中继服务器。",916,740,14,Ink.Muted,390);Footer("联机仅交换本局游戏状态；对手手牌与牌库顺序不会发送。");
 }
 void WireRoom(Room r){int generation=session;r.Failed+=msg=>Dispatcher.BeginInvoke(new Action(()=>{if(generation!=session)return;disconnected=true;networkStatus=msg;if(state==null){if(lobbyStatus!=null)lobbyStatus.Text="连接失败："+msg;}else{Render();Toast("连接已断开："+msg);}}));r.Received+=p=>Dispatcher.BeginInvoke(new Action(()=>{if(generation!=session)return;try{
  if(p.Kind=="state" && mode=="guest"){var next=Room.Parse<Snapshot>(p.Payload);if(next!=null && (state==null || next.Revision>state.Revision))Receive(next);}
  else if(p.Kind=="action" && mode=="host"){if(busy){r.Send(new Packet{Kind="error",Payload="正在播放出牌，请稍候。"});return;}var a=Room.Parse<ActionMessage>(p.Payload);ApplyHost(1,a);}
  else if(p.Kind=="error"){Render();Toast(p.Payload);}
 }catch(Exception ex){Toast("网络数据处理失败："+ex.Message);}}));}
 void HostRoom(){if(room!=null && !disconnected)return;try{if(room!=null)room.Dispose();disconnected=false;myName=Room.CleanName(nameBox.Text,"旅人 I");room=new Room();WireRoom(room);int generation=session;room.Connected+=n=>Dispatcher.BeginInvoke(new Action(()=>{if(generation!=session)return;mode="host";viewer=0;engine=new Engine(myName,n,rules,rng.Next());state=engine.View(0,false);SendState();Render();}));room.Host(roomCode,myName);lobbyStatus.Text="✦ 房间已开启，正在等待另一位旅人…";}catch(Exception ex){if(room!=null)room.Dispose();room=null;lobbyStatus.Text="无法创建房间："+ex.Message;}}
 void JoinRoom(){if(room!=null && !disconnected)return;if(String.IsNullOrWhiteSpace(ipBox.Text) || codeBox.Text.Trim().Length!=6){Toast("请输入房主 IP 和六位口令。");return;}if(room!=null)room.Dispose();disconnected=false;myName=Room.CleanName(nameBox.Text,"旅人 II");room=new Room();WireRoom(room);int generation=session;room.Connected+=n=>Dispatcher.BeginInvoke(new Action(()=>{if(generation!=session)return;mode="guest";viewer=1;lobbyStatus.Text="已连接，正在同步星盘…";}));room.Join(ipBox.Text.Trim(),codeBox.Text.Trim(),myName);lobbyStatus.Text="正在连接…";}
 void SendState(){if(room!=null && mode=="host")try{room.Send(new Packet{Kind="state",Payload=Room.Json(engine.View(1,false))});}catch(Exception ex){disconnected=true;networkStatus=ex.Message;}}
 void Action(string type,int value=0,int crystals=0){if(busy || state==null || disconnected)return;var a=new ActionMessage{Type=type,Value=value,Crystals=crystals,Revision=state.Revision};if(mode=="guest"){try{room.Send(new Packet{Kind="action",Payload=Room.Json(a)});}catch(Exception ex){Toast(ex.Message);}return;}ApplyHost(viewer,a);}
 void ApplyHost(int actor,ActionMessage a){if(engine==null)return;string error=engine.Apply(actor,a);if(error!=null){if(mode=="host" && actor==1)room.Send(new Packet{Kind="error",Payload=error});else {Render();Toast(error);}return;}SendState();Receive(engine.View(viewer,mode=="test"));}
 async void Receive(Snapshot next){
  if(busy){pendingSnapshots.Enqueue(next);return;}previews.Children.Clear();CancelDrag();var old=state;if(old==null || next.Revision>old.Revision)ClearPaymentSelection();
  if(old!=null && next.Event!=null && next.Revision>old.Revision && (next.Event.Type=="play" || next.Event.Type=="resolve" && (next.Event.Steps.Count>0 || next.ResolvingCard==null && next.Event.Card!=null))){
   busy=true;int generation=session;try{await AnimateCardEvent(next.Event,next.Event.Type=="play",next.ResolvingCard!=null);}catch{}if(generation!=session)return;effects.Children.Clear();busy=false;
  }else if(old!=null && next.Event!=null && next.Event.Type=="flip"){
   busy=true;int generation=session;await AnimateFlip(next.Event);if(generation!=session)return;effects.Children.Clear();busy=false;
  }
  state=next;rules=next.Rules;
  if(mode=="test" && state.Phase!="ended")viewer=state.ChoiceOwner>=0?state.ChoiceOwner:state.PendingTokens>=0?state.PendingTokens:state.Active;
  Render();if(pendingSnapshots.Count>0){var pending=pendingSnapshots.Dequeue();if(pending.Revision>state.Revision)Receive(pending);}else ScheduleAI();
 }
 void Render(){if(mode=="menu"){Menu();return;}if(state==null)return;Base();Put(page,Btn("退出对局",()=>AskLeave(),108),1044,25);
  if(state.Phase=="draft")Draft();else Battle();
  if(disconnected){var b=Panel(760,93,"#351F31","#B57682");Put(page,b,340,84);Label(page,"连接中断 · 本局已暂停",363,96,20,"#F2B9BF");Label(page,networkStatus,363,130,14,Ink.White,710);}
 }
 bool MyTurn(){return state!=null && state.Active==viewer && !busy && !disconnected && state.Phase!="ended" && state.ChoiceOwner<0 && state.PendingTokens<0 && state.ResolvingCard==null;}
 string Who(int p){return p==viewer?"你":state.Players[p].Name;}
 void Draft(){
  bool ban=Engine.DraftBan[state.DraftIndex];bool my=MyTurn();Label(page,"命运选集",36,111,31);Label(page,"BAN / PICK",37,155,12,Ink.Gold);
  Label(page,(my?"请":"等待 "+Who(state.Active)+" ")+(ban?"禁用 1 副牌组":"选择 1 副牌组"),285,112,27,my?Ink.Gold:Ink.White);
  Label(page,"禁用和选取均为全局独占 · 双方最终各选 3 副，自动加入愚者",286,155,14,Ink.Muted);
  for(int i=0;i<10;i++){int actor=Engine.DraftActors[i]==0?state.First:1-state.First;string s=(actor==viewer?"你":"对手")+" "+(Engine.DraftBan[i]?"禁":"选");var p=Panel(104,38,i==state.DraftIndex?"#B79A65":i<state.DraftIndex?"#182437":"#0D1727",i==state.DraftIndex?Ink.Gold:"#334055");p.Child=T(s,13,i==state.DraftIndex?Ink.Night:i<state.DraftIndex?Ink.Muted:Ink.White);((TextBlock)p.Child).HorizontalAlignment=HorizontalAlignment.Center;((TextBlock)p.Child).VerticalAlignment=VerticalAlignment.Center;Put(page,p,285+i*110,194);}
  Label(page,"你的星群",53,287,16,Ink.Gold);Label(page,state.Players[viewer].Name,53,315,12,Ink.Muted);int line=0;foreach(int a in state.Players[viewer].Picks)Label(page,Engine.Romans[a]+"   "+Engine.Names[a],53,350+line++*32,17);Label(page,"＋ 愚者（自动加入）",53,453,13,Ink.Muted);
  Label(page,"对手的星群",53,520,16,"#B9B1D8");line=0;foreach(int a in state.Players[1-viewer].Picks)Label(page,Engine.Romans[a]+"   "+Engine.Names[a],53,560+line++*32,17);Label(page,"每副：A × 2 + B × 2 + C × 1",53,745,12,Ink.Muted,177);
  for(int a=1;a<=20;a++){
   int arc=a;int col=(a-1)%10,row=(a-1)/10;double x=284+col*110,y=260+row*268;bool available=state.Available.Contains(a);
   var card=new CardFace(new Card{Arcana=a},rules,true);var b=new Border{Width=103,Height=159,Background=Brushes.Transparent,Child=card,Cursor=available && my?Cursors.Hand:Cursors.Arrow,Opacity=available?1:.31};Put(page,b,x,y);
   b.MouseEnter+=(s,e)=>ShowPreview(new Card{Arcana=arc,Rank=2},x+105,y,true);b.MouseLeave+=(s,e)=>previews.Children.Clear();
   b.MouseLeftButtonUp+=(s,e)=>{if(!MyTurn() || !state.Available.Contains(arc))return;ConfirmDraft(arc);};
   Label(page,Engine.Names[a],x,y+169,15,available?Ink.White:Ink.Muted,103);
   string tag=available?"5 张 · 3 种":state.Banned.Contains(a)?"已禁用":state.Players[viewer].Picks.Contains(a)?"你的选择":"对手选择";Label(page,tag,x,y+196,11,state.Banned.Contains(a)?"#B87D86":Ink.Gold,103);
  }
  if(mode=="test"){var quick=Btn("随机完成剩余禁选",()=>{while(engine.Phase=="draft")engine.Apply(engine.Active,new ActionMessage{Type="draft",Value=engine.Available[rng.Next(engine.Available.Count)],Revision=engine.Revision});viewer=engine.Active;state=engine.View(viewer,true);Render();},204);quick.Height=32;quick.FontSize=12;Put(page,quick,1141,816);}
  Footer(mode=="test"?"双人测试  ·  自动切换到当前操作者  |  当前："+state.Players[viewer].Name:"随机先手："+state.Players[state.First].Name+"  ·  点击牌组后确认禁选");
 }
 void ConfirmDraft(int arc){previews.Children.Clear();bool ban=Engine.DraftBan[state.DraftIndex];ModalBase(650,555);Put(modal,new CardFace(new Card{Arcana=arc},rules,true),441,220,180,278);Label(modal,Engine.Names[arc],663,245,30);Label(modal,Engine.English[arc],665,291,12,Ink.Gold);Label(modal,"A × 2 / B × 2 / C × 1",665,341,16,Ink.Muted,330);Put(modal,Btn("查看三张牌",()=>{DeckDetails(arc);Put(modal,Btn(ban?"确认禁用":"确认选择",()=>{modal.Children.Clear();Action("draft",arc);},180,true),620,788);},173),665,393);Put(modal,Btn(ban?"确认禁用":"确认选择",()=>{modal.Children.Clear();Action("draft",arc);},173,true),665,459);Put(modal,Btn("重新考虑",()=>modal.Children.Clear(),173),665,517);}
 int Payment(Card c){if(state==null)return -1;SyncPaymentSelection();return SelectedMana+SelectedCrystals==rules.Get(c).Cost && Payments(c).Contains(SelectedCrystals)?SelectedCrystals:-1;}
 void ShowPreview(Card c,double x,double y,bool compact){previews.Children.Clear();if(busy || modal.Children.Count>0)return;double px=Math.Min(1110,Math.Max(235,x+75)),py=compact?Math.Min(415,y):118;preview=new CardFace(c,rules,compact){Upright=state==null?true:state.UprightPlayer==viewer};Put(previews,preview,px,py,330,510);preview.Effect=new System.Windows.Media.Effects.DropShadowEffect{Color=Colors.Black,BlurRadius=28,ShadowDepth=10,Opacity=.8};if(!compact){Put(previews,new Border{Background=Ink.B("#EE101827"),CornerRadius=new CornerRadius(4)},px,py-60,330,54);Label(previews,PaymentHint(c),px+10,py-54,13,Ink.Gold,312);Label(previews,"R 查看另一牌面 · 单击 / 右键打开预览",px+10,py-31,12,Ink.White,312);}}

 void BeginDrag(Card c,FrameworkElement source,MouseButtonEventArgs e){if(!MyTurn() || state.Phase!="battle" || modal.Children.Count>0)return;dragging=c;dragSource=source;dragStart=e.GetPosition(stage);dragMoved=false;previews.Children.Clear();source.CaptureMouse();castRune=new CastSigil{Opacity=.6,IsHitTestVisible=false};Put(effects,castRune,382,443,660,162);e.Handled=true;}
 void MoveDrag(object sender,MouseEventArgs e){if(dragging==null)return;var p=e.GetPosition(stage);if(!dragMoved && (p-dragStart).Length<7)return;dragMoved=true;if(ghost==null){ghost=new CardFace(dragging,rules){Upright=state.UprightPlayer==viewer};Put(effects,ghost,p.X-80,p.Y-100,160,247);ghost.Opacity=.94;dragSource.Opacity=.15;}Canvas.SetLeft(ghost,p.X-80);Canvas.SetTop(ghost,p.Y-100);if(castRune!=null)castRune.Opacity=p.X>=235 && p.X<=1165 && p.Y>=285 && p.Y<=625?1:.35;}
 void EndDrag(object sender,MouseButtonEventArgs e){if(dragging==null)return;var c=dragging;bool moved=dragMoved;var p=e.GetPosition(stage);int pay=Payment(c);CancelDrag();if(moved && p.X>=235 && p.X<=1165 && p.Y>=285 && p.Y<=625){BeginPlay(c);}else if(!moved)InspectCard(c,state.UprightPlayer==viewer);e.Handled=true;}
 void CancelDrag(){dragging=null;if(castRune!=null){effects.Children.Remove(castRune);castRune=null;}if(dragSource!=null){dragSource.Opacity=1;dragSource.ReleaseMouseCapture();dragSource=null;}if(ghost!=null){effects.Children.Remove(ghost);ghost=null;}previews.Children.Clear();}
 void ModalBase(double w,double h){modal.Children.Clear();previews.Children.Clear();Put(modal,new Border{Background=Ink.B("#D9080E1B")},0,0,1440,900);Put(modal,Panel(w,h,"#142035","#8A795C"),(1440-w)/2,(900-h)/2);Put(modal,Btn("×",()=>modal.Children.Clear(),40),(1440+w)/2-57,(900-h)/2+16);}
 void Help(){RulesHelp();}
 void Library(){ModalBase(1280,774);Label(modal,"大阿尔卡纳图鉴",117,85,28);Label(modal,"63 种卡牌 · 126 个正逆位效果 · 世界不参与本版",118,130,13,Ink.Muted);
  for(int a=0;a<=20;a++){int arc=a;double x=120+a%11*110,y=176+a/11*310;var b=new Border{Width=99,Height=153,Background=Brushes.Transparent,Child=new CardFace(new Card{Arcana=a},rules,true)};Put(modal,b,x,y);Label(modal,Engine.Names[a],x,y+163,16,Ink.White,101);Label(modal,"A×2  B×2  C×1",x,y+192,11,Ink.Gold,102);b.MouseLeftButtonUp+=(s,e)=>{DeckDetails(arc);};}
 }
 void Discard(int p){ModalBase(1110,710);Label(modal,state.Players[p].Name+" 的弃牌区",205,125,27);var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};var wrap=new WrapPanel();foreach(var c in state.Players[p].Discard.AsEnumerable().Reverse()){var f=new CardFace(c,rules){Upright=state.UprightPlayer==p,Width=144,Height=223,Margin=new Thickness(8)};var box=new Border{Child=f,Background=Brushes.Transparent,Cursor=Cursors.Hand,ToolTip="点击预览正逆位效果"};box.MouseLeftButtonUp+=(sender,args)=>InspectCard(c,state.UprightPlayer==p,()=>Discard(p));wrap.Children.Add(box);}if(state.Players[p].Discard.Count==0)wrap.Children.Add(T("星盘尚无弃牌。",20,Ink.Muted));scroll.Content=wrap;Put(modal,scroll,204,191,1020,580);}
 void TokenModal(){if(state.PendingTokens!=viewer)return;ModalBase(660,380);Label(modal,"小阿尔卡纳超出上限",440,306,26);Label(modal,"请选择弃置，直到合计不超过 13。",440,360,16,Ink.Muted);for(int i=0;i<4;i++){int suit=i;var b=Btn(new[]{"权杖","圣杯","宝剑","星币"}[i]+" "+state.Players[viewer].Tokens[i],()=>{modal.Children.Clear();Action("discardToken",suit);},129);b.IsEnabled=state.Players[viewer].Tokens[i]>0;Put(modal,b,440+i*140,432);}}
 void Victory(){ModalBase(660,450);bool won=state.Winner==viewer;Label(modal,"✦",681,251,58,Ink.Gold);Label(modal,won?"命运归于你":"星途已落定",527,336,37,Ink.Gold);Label(modal,state.Players[state.Winner].Name+" 抵达胜利终点",527,399,19,Ink.White,430);Put(modal,Btn("返回主菜单",()=>{Cleanup();Menu();},208,true),491,487);Put(modal,Btn("查看终局",()=>modal.Children.Clear(),208),724,487);}
 void AskLeave(){ModalBase(620,300);Label(modal,"离开本局？",456,341,28);Label(modal,"联机时会断开连接；当前对局不会保存。",456,396,16,Ink.Muted);Put(modal,Btn("继续对局",()=>modal.Children.Clear(),192,true),456,473);Put(modal,Btn("返回主菜单",()=>{Cleanup();Menu();},192),673,473);}
 async void Toast(string text){var p=Panel(660,54,"#293448","#B89D70");p.Child=T(text,15,Ink.White);((TextBlock)p.Child).VerticalAlignment=VerticalAlignment.Center;((TextBlock)p.Child).Margin=new Thickness(18,0,18,0);Put(previews,p,390,83);await Task.Delay(3000);previews.Children.Remove(p);}
 void Animate(UIElement target,DependencyProperty prop,double from,double to,int ms,int delay=0){var a=new DoubleAnimation(from,to,TimeSpan.FromMilliseconds(ms)){BeginTime=TimeSpan.FromMilliseconds(delay),EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};target.BeginAnimation(prop,a);}
 async Task AnimatePlay(Event ev){await AnimateCardEvent(ev,true);}
 async Task AnimateFlip(Event ev){var orb=new Border{Width=210,Height=210,CornerRadius=new CornerRadius(105),BorderBrush=Ink.B(Ink.Gold),BorderThickness=new Thickness(2),Background=Ink.B("#E018263E"),RenderTransformOrigin=new Point(.5,.5)};orb.Child=T("☾   ✦",42,Ink.Gold);((TextBlock)orb.Child).HorizontalAlignment=HorizontalAlignment.Center;((TextBlock)orb.Child).VerticalAlignment=VerticalAlignment.Center;Put(effects,orb,615,273);var rot=new RotateTransform();orb.RenderTransform=rot;rot.BeginAnimation(RotateTransform.AngleProperty,new DoubleAnimation(0,180,TimeSpan.FromMilliseconds(650)));Animate(orb,OpacityProperty,0,1,150);await Task.Delay(720);}
 void ScheduleAI(){ScheduleBot();}
 // Offscreen artifact rendering also exercises every custom illustration and screen layout.
 public void RenderGallery(string folder){
  Directory.CreateDirectory(folder);Menu();Capture(System.IO.Path.Combine(folder,"01-menu.png"));
  StartLocal(false);Capture(System.IO.Path.Combine(folder,"02-draft.png"));
  while(engine.Phase=="draft")engine.Apply(engine.Active,new ActionMessage{Type="draft",Value=engine.Available[0],Revision=engine.Revision});viewer=engine.Active;state=engine.View(viewer,true);Render();Capture(System.IO.Path.Combine(folder,"03-battle.png"));
  ShowPreview(state.Players[viewer].Hand[0],580,218,false);Capture(System.IO.Path.Combine(folder,"04-preview.png"));previews.Children.Clear();
  Library();Capture(System.IO.Path.Combine(folder,"05-gallery.png"));Help();Capture(System.IO.Path.Combine(folder,"06-rules.png"));modal.Children.Clear();
  engine.GrantTokens(viewer,0,14);state=engine.View(viewer,true);Render();Capture(System.IO.Path.Combine(folder,"07-tokens.png"));modal.Children.Clear();
  engine.PendingTokens=-1;engine.Players[viewer].Tokens[0]=13;engine.Phase="ended";engine.Winner=viewer;engine.Position=viewer==0?Engine.RoadEnd:0;state=engine.View(viewer,true);Render();Capture(System.IO.Path.Combine(folder,"08-victory.png"));
 }
 void Capture(string path){stage.Measure(new Size(1440,900));stage.Arrange(new Rect(0,0,1440,900));stage.UpdateLayout();var bitmap=new RenderTargetBitmap(1440,900,96,96,PixelFormats.Pbgra32);bitmap.Render(stage);var enc=new PngBitmapEncoder();enc.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(path))enc.Save(stream);}
}
public static class Program {
 [STAThread] public static int Main(string[] args){
  if(args.Length>0 && args[0]=="--test")return Tests.Run();
  try{var app=new Application();if(args.Length>1 && args[0]=="--playback-test"){app.ShutdownMode=ShutdownMode.OnExplicitShutdown;app.Startup+=async(s,e)=>{try{await new GameWindow().RunPlaybackChecks(args[1]);app.Shutdown(0);}catch(Exception ex){File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"playback-error.txt"),ex.ToString());app.Shutdown(1);}};return app.Run();}if(args.Length>1 && args[0]=="--render-gallery"){new GameWindow().RenderGallery(args[1]);return 0;}app.DispatcherUnhandledException+=(s,e)=>{MessageBox.Show("发生错误："+e.Exception.Message,"星途对决");e.Handled=true;};app.Run(new GameWindow());return 0;}catch(Exception e){if(args.Length>0){File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"error.txt"),e.ToString());return 1;}MessageBox.Show(e.Message,"星途对决 · 启动失败");return 1;}
 }
}
}




