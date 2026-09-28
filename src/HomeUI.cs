using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace ArcanaDuel {
public partial class GameWindow {
 readonly SceneAtmosphere atmosphere=new SceneAtmosphere();
 void AddAtmosphere(bool home){atmosphere.MenuScene=home;Put(page,atmosphere,0,0,1440,900);}
 void ToggleMotion(){atmosphere.Motion=!atmosphere.Motion;}
 void DrawHome(){
  page.Children.Clear();previews.Children.Clear();Put(page,new SceneBoard(),0,0,1440,900);
  var shade=new LinearGradientBrush();shade.StartPoint=new Point(0,0);shade.EndPoint=new Point(1,0);
  shade.GradientStops.Add(new GradientStop(Ink.C("#FA090D19"),0));shade.GradientStops.Add(new GradientStop(Ink.C("#EB0B1020"),.43));shade.GradientStops.Add(new GradientStop(Ink.C("#A00D1120"),.72));shade.GradientStops.Add(new GradientStop(Ink.C("#C20A0D19"),1));
  Put(page,new Border{Background=shade,IsHitTestVisible=false},0,0,1440,900);AddAtmosphere(true);
  Put(page,new HomeOrnament(),0,0,1440,900);
  Label(page,"A R C A N A   D U E L",112,53,13,Ink.Gold,400);
  Label(page,"T H E   A R C A N A   A W A K E N",107,195,11,"#A49484",500);
  var title=T("星途对决",76,"#F2EADD");title.FontFamily=new FontFamily("Microsoft YaHei UI");title.FontWeight=FontWeights.Light;title.Effect=new DropShadowEffect{Color=Ink.C("#AE8B51"),BlurRadius=22,ShadowDepth=0,Opacity=.25};Put(page,title,99,223,490);
  Label(page,"命 运，由 你 翻 转",108,331,20,"#C9B48F",500);
  HomeButton("创建联机房间","HOST A DUEL","✦",()=>Lobby(true),104,413,439,75,true,"home-host");
  HomeButton("加入联机房间","JOIN A DUEL","☾",()=>Lobby(false),104,502,439,67,false,"home-join");
  HomeButton("单人练习","SOLO","Ⅰ",()=>StartLocal(true),104,590,211,78,false,"home-solo");
  HomeButton("双人测试","SANDBOX","Ⅱ",()=>StartLocal(false),332,590,211,78,false,"home-test");
  var library=HomeButton("大阿尔卡纳图鉴","THE MAJOR ARCANA","◇",()=>Library(),104,702,439,63,false,"home-library");library.Opacity=.86;
  var guide=Btn("玩法指南",()=>Help(),103);guide.Tag="home-guide";StyleHomeUtility(guide);Put(page,guide,1218,43);
  var audio=Btn(sound?"声音：开":"声音：关",()=>{sound=!sound;Render();},92);StyleHomeUtility(audio);Put(page,audio,1006,43);
  var motion=Btn(atmosphere.Motion?"动效：开":"动效：关",()=>{ToggleMotion();Render();},92);motion.Tag="home-motion";StyleHomeUtility(motion);Put(page,motion,1112,43);
  Label(page,"XXI   /   THE MAJOR ARCANA",905,776,12,"#B9A184",430);
  Label(page,"正 位   ·   逆 位",960,803,13,"#898AA1",280);
  Label(page,"v1.4",106,847,12,"#747381");Label(page,"F11  最大化",1240,847,12,"#747381");
 }
 void StyleHomeUtility(Button b){b.Background=Brushes.Transparent;b.BorderBrush=Ink.B("#615644");b.FontSize=12;b.Height=34;}
 Button HomeButton(string title,string subtitle,string glyph,Action action,double x,double y,double width,double height,bool primary,string tag){
  var button=Btn("",action,width);button.Tag=tag;button.Height=height;button.Padding=new Thickness(0);button.Background=Brushes.Transparent;button.BorderThickness=new Thickness(0);
  var canvas=new Canvas{Width=width,Height=height,IsHitTestVisible=false};button.Content=canvas;
  string geometry=String.Format(System.Globalization.CultureInfo.InvariantCulture,"M 12,0 L {0},0 {1},12 {1},{2} {3},{4} 0,{4} 0,12 Z",width-13,width,height-13,width-13,height);
  var plate=new System.Windows.Shapes.Path{Data=Geometry.Parse(geometry),Fill=new LinearGradientBrush(Ink.C(primary?"#D4B77C":"#E91A2232"),Ink.C(primary?"#A88A56":"#D9101623"),0),Stroke=Ink.B(primary?"#F0D7A4":"#806D51"),StrokeThickness=1};canvas.Children.Add(plate);
  Label(canvas,glyph,20,(height-28)/2,24,primary?"#30271C":"#D4B98A",32);
  Label(canvas,title,65,primary?11:10,width<250?18:22,primary?"#211D18":"#EEE5D5",width-85);
  Label(canvas,subtitle,67,primary?44:height-24,9,primary?"#55452F":"#8E91A3",width-90);
  if(width>250)Label(canvas,"›",width-39,(height-32)/2,27,primary?"#483821":"#BDA678",30);
  var glow=new DropShadowEffect{Color=Ink.C("#D4B57A"),BlurRadius=20,ShadowDepth=0,Opacity=primary?.17:0};button.Effect=glow;
  var move=new TranslateTransform();button.RenderTransform=move;
  button.MouseEnter+=(s,e)=>{button.Opacity=1;glow.Opacity=.42;plate.Stroke=Ink.B("#F4D9A5");if(atmosphere.Motion)move.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(0,6,TimeSpan.FromMilliseconds(150)));};
  button.MouseLeave+=(s,e)=>{glow.Opacity=primary?.17:0;plate.Stroke=Ink.B(primary?"#F0D7A4":"#806D51");move.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(move.X,0,TimeSpan.FromMilliseconds(atmosphere.Motion?180:1)));};
  Put(page,button,x,y);return button;
 }
}
public sealed class HomeOrnament : FrameworkElement {
 public HomeOrnament(){IsHitTestVisible=false;}
 protected override void OnRender(DrawingContext d){
  Ink.Path(d,"M 67,58 L 78,47 89,58 78,69 Z",null,"#CCAE78");Ink.Star(d,78,58,5,"#CCAE78");
  Ink.Line(d,104,380,474,380,"#776247",.8);Ink.Star(d,490,380,5,"#D4B987");Ink.Line(d,506,380,543,380,"#776247",.8);
  Ink.Line(d,104,823,544,823,"#4D443A",.7);
  Ink.Line(d,624,176,624,735,"#37313B",.7);Ink.Star(d,624,162,4,"#76654C");Ink.Star(d,624,749,4,"#76654C");
 }
}
}
