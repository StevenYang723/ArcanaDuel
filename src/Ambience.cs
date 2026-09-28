using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ArcanaDuel {
// One reusable, non-interactive layer per window. Detaching or minimizing stops its clock.
public sealed class SceneAtmosphere : FrameworkElement {
 readonly DispatcherTimer timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(33)};
 readonly Stopwatch clock=new Stopwatch();
 readonly Random random=new Random(912);
 readonly Point[] motes=new Point[64];
 readonly DrawingImage[] cards=new DrawingImage[3];
 readonly Brush gold=Ink.B("#E4C98D"),blue=Ink.B("#AAD9EB");
 readonly Brush warm=Glow("#C79645"),cool=Glow("#647ACE"),violet=Glow("#884AAA");
 readonly Pen ring=new Pen(Ink.B("#C9A567"),1),fine=new Pen(Ink.B("#778BB0"),.7);
 Window owner;double previous,phase,offsetX,offsetY;bool motion=true;
 public bool MenuScene;public Point Pointer;
 public int Frames {get;private set;}
 public double Phase {get{return phase;}}
 public bool ClockRunning {get{return timer.IsEnabled;}}
 public bool Motion {get{return motion;}set{motion=value;if(!value){offsetX=offsetY=0;}RefreshClock();InvalidateVisual();}}
 public SceneAtmosphere(){
  IsHitTestVisible=false;ClipToBounds=true;
  for(int i=0;i<motes.Length;i++)motes[i]=new Point(random.NextDouble()*1440,random.NextDouble()*900);
  cards[0]=MakeCard(18);cards[1]=MakeCard(19);cards[2]=MakeCard(2);
  Loaded+=(s,e)=>{owner=Window.GetWindow(this);if(owner!=null)owner.StateChanged+=WindowChanged;RefreshClock();};
  Unloaded+=(s,e)=>{timer.Stop();clock.Stop();if(owner!=null)owner.StateChanged-=WindowChanged;owner=null;};
  IsVisibleChanged+=(s,e)=>RefreshClock();
  timer.Tick+=(s,e)=>{double now=clock.Elapsed.TotalSeconds;phase+=Math.Min(.12,Math.Max(0,now-previous));previous=now;offsetX+=(Pointer.X-offsetX)*.065;offsetY+=(Pointer.Y-offsetY)*.065;Frames++;InvalidateVisual();};
 }
 void WindowChanged(object sender,EventArgs args){RefreshClock();}
 void RefreshClock(){
  bool run=motion && IsLoaded && IsVisible && (owner==null || owner.WindowState!=WindowState.Minimized);
  if(run){clock.Start();previous=clock.Elapsed.TotalSeconds;timer.Start();}else{timer.Stop();clock.Stop();}
 }
 static Brush Glow(string color){var c=Ink.C(color);var b=new RadialGradientBrush();b.GradientStops.Add(new GradientStop(c,0));b.GradientStops.Add(new GradientStop(Color.FromArgb(0,c.R,c.G,c.B),1));b.Freeze();return b;}
 static DrawingImage MakeCard(int arc){
  var group=new DrawingGroup();using(var d=group.Open()){
   d.DrawRoundedRectangle(Ink.B("#070B17"),new Pen(Ink.B("#D2B784"),2),new Rect(-113,-179,226,358),5,5);
   d.DrawRectangle(null,new Pen(Ink.B(Assets.Colors[arc]),2),new Rect(-103,-169,206,338));
   d.PushClip(new RectangleGeometry(new Rect(-96,-132,192,242)));TarotArt.Draw(d,arc,-96,-132,192,242);d.Pop();
   Ink.Text(d,Engine.Romans[arc],0,-166,19,Ink.Gold,true,"Georgia");
   Ink.Line(d,-96,121,96,121,Assets.Colors[arc]);
   Ink.Text(d,Engine.Names[arc],0,132,22,Ink.White,true);Ink.Text(d,Engine.English[arc].ToUpperInvariant(),0,158,7,Ink.Gold,true,"Georgia");
   Ink.Star(d,-98,-155,4);Ink.Star(d,98,-155,4);
  }group.Freeze();var image=new DrawingImage(group);image.Freeze();return image;
 }
 protected override void OnRender(DrawingContext d){
  d.PushTransform(new ScaleTransform(ActualWidth/1440,ActualHeight/900));
  double t=phase;
  if(MenuScene)DrawSanctum(d,t);else DrawBoardLight(d,t);
  // A few bright grains at the perimeter; subdued dust across the playing surface.
  for(int i=0;i<motes.Length;i++){
   var seed=motes[i];double x=(seed.X+Math.Sin(t*.13+i)*19+1440)%1440,y=(seed.Y-t*(4+i%5)+18000)%900;
   double alpha=(MenuScene?.22:.12)+.25*(.5+.5*Math.Sin(t*.7+i*2.1));
   if(!MenuScene && x>205 && x<1270 && y>260 && y<710)alpha*=.22;
   d.PushOpacity(alpha);d.DrawEllipse(i%4==0?blue:gold,null,new Point(x,y),i%7==0?1.8:.8,i%7==0?1.8:.8);d.Pop();
  }
  d.Pop();
 }
 void DrawSanctum(DrawingContext d,double t){
  d.PushTransform(new TranslateTransform(offsetX*.55,offsetY*.55));
  d.PushOpacity(.43+.05*Math.Sin(t*.45));d.DrawEllipse(violet,null,new Point(1080,415),440,470);d.Pop();
  d.PushOpacity(.38);d.DrawEllipse(cool,null,new Point(846+Math.Sin(t*.17)*70,300),360,310);d.Pop();
  d.PushOpacity(.15+.04*Math.Sin(t*.7));d.DrawEllipse(warm,null,new Point(1010,705),370,125);d.Pop();
  d.PushTransform(new RotateTransform(t*1.2,1015,443));
  d.PushOpacity(.51);d.DrawEllipse(null,ring,new Point(1015,443),308,308);d.DrawEllipse(null,fine,new Point(1015,443),294,294);
  for(int i=0;i<72;i++){double a=i*Math.PI/36;double r=i%6==0?277:302;d.DrawLine(ring,new Point(1015+r*Math.Cos(a),443+r*Math.Sin(a)),new Point(1015+308*Math.Cos(a),443+308*Math.Sin(a)));}
  for(int i=0;i<12;i++){double a=i*Math.PI/6;Ink.Star(d,1015+326*Math.Cos(a),443+326*Math.Sin(a),i%3==0?7:3,"#DFC08D");}d.Pop();d.Pop();
  d.PushOpacity(.24);d.PushTransform(new RotateTransform(-t*1.7,1015,443));
  d.DrawEllipse(null,fine,new Point(1015,443),248,248);
  for(int i=0;i<6;i++){double a=i*Math.PI/3,b=a+Math.PI*2/3;d.DrawLine(ring,new Point(1015+247*Math.Cos(a),443+247*Math.Sin(a)),new Point(1015+247*Math.Cos(b),443+247*Math.Sin(b)));}d.Pop();d.Pop();
  DrawCard(d,0,817,481+Math.Sin(t*.65)*8,-19+Math.Sin(t*.32)*1.5,.9);
  DrawCard(d,1,1213,481+Math.Sin(t*.62+2)*9,19+Math.Sin(t*.29)*1.5,.9);
  DrawCard(d,2,1015,415+Math.Sin(t*.55+1)*11,Math.Sin(t*.24)*1.3,1.13);
  d.Pop();
  // Slow orbiting glints stay outside the illustrations.
  for(int i=0;i<3;i++){double a=t*.09+i*2.1;double x=1015+312*Math.Cos(a),y=443+312*Math.Sin(a);d.PushOpacity(.7);d.DrawEllipse(warm,null,new Point(x,y),25,25);Ink.Star(d,x,y,4,Ink.White);d.Pop();}
 }
 void DrawCard(DrawingContext d,int index,double x,double y,double angle,double scale){
  d.PushTransform(new TranslateTransform(x+offsetX*.5,y+offsetY*.5));d.PushTransform(new RotateTransform(angle));d.PushTransform(new ScaleTransform(scale,scale));
  d.PushOpacity(.8);d.DrawRoundedRectangle(Ink.B("#60040913"),null,new Rect(-121,-171,248,375),8,8);d.Pop();
  d.DrawImage(cards[index],new Rect(-114,-180,228,360));d.Pop();d.Pop();d.Pop();
 }
 void DrawBoardLight(DrawingContext d,double t){
  d.PushOpacity(.10+.025*Math.Sin(t*.38));d.DrawEllipse(cool,null,new Point(1290,168),350,250);d.DrawEllipse(violet,null,new Point(118,171),285,245);d.Pop();
  Point[] candles={new Point(34,527),new Point(39,675),new Point(169,603),new Point(1401,358),new Point(1337,543)};
  for(int i=0;i<candles.Length;i++){
   var c=candles[i];double pulse=.17+.035*Math.Sin(t*3.3+i)+.025*Math.Sin(t*6.1+i*2);
   d.PushOpacity(pulse);d.DrawEllipse(warm,null,c,84,110);d.Pop();
   for(int j=0;j<3;j++){double rise=(t*13+j*31+i*17)%91;d.PushOpacity((1-rise/91)*.45);d.DrawEllipse(gold,null,new Point(c.X+Math.Sin(t*.8+j+i)*9,c.Y-rise),1,1.5);d.Pop();}
  }
  d.PushOpacity(.09+.025*Math.Sin(t*.6));d.PushTransform(new RotateTransform(t*.5,720,410));
  d.DrawEllipse(null,ring,new Point(720,410),236,236);
  for(int i=0;i<12;i++){double a=i*Math.PI/6;Ink.Star(d,720+236*Math.Cos(a),410+236*Math.Sin(a),4);}d.Pop();d.Pop();
 }
}
}
