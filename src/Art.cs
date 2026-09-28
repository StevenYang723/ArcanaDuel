using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace ArcanaDuel {
public static class Ink {
 public static Color C(string s){return (Color)ColorConverter.ConvertFromString(s);}
 public static SolidColorBrush B(string s){var b=new SolidColorBrush(C(s));b.Freeze();return b;}
 public const string Gold="#D6B77B", Muted="#8D99B0", White="#EEE8DA", Night="#0C1222";
 public static void Text(DrawingContext d,string s,double x,double y,double size,string color,bool center=false,string font="Microsoft YaHei UI"){
  var f=new FormattedText(s,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(font),size,B(color),1.0);d.DrawText(f,new Point(center?x-f.Width/2:x,y));
 }
 public static void Line(DrawingContext d,double x,double y,double xx,double yy,string c=Gold,double w=1){d.DrawLine(new Pen(B(c),w),new Point(x,y),new Point(xx,yy));}
 public static void Path(DrawingContext d,string g,string fill,string stroke=Gold,double w=1.5){d.DrawGeometry(fill==null?null:B(fill),stroke==null?null:new Pen(B(stroke),w),Geometry.Parse(g));}
 public static void Circle(DrawingContext d,double x,double y,double r,string fill=null,string stroke=Gold,double w=1){d.DrawEllipse(fill==null?null:B(fill),stroke==null?null:new Pen(B(stroke),w),new Point(x,y),r,r);}
 public static void Star(DrawingContext d,double x,double y,double r,string color=Gold){Path(d,String.Format(CultureInfo.InvariantCulture,"M {0},{1} L {2},{3} {4},{1} {2},{5} Z",x-r,y,x,y-r,x+r,y+r),color,null);}
 public static void Moon(DrawingContext d,double x,double y,double r,string bg="#14213A"){Circle(d,x,y,r,Gold,null);Circle(d,x+r*.52,y-r*.25,r*.89,bg,null);}
}
public class TarotArt : FrameworkElement {
 public int Arcana; public TarotArt(int a){Arcana=a;IsHitTestVisible=false;}
 protected override void OnRender(DrawingContext d){Draw(d,Arcana,0,0,ActualWidth,ActualHeight);}
 public static void Draw(DrawingContext d,int a,double x,double y,double w,double h){
  if(Assets.Art(a)!=null){d.DrawImage(Assets.Art(a),new Rect(x,y,w,h));return;}
  d.PushClip(new RectangleGeometry(new Rect(x,y,w,h)));d.PushTransform(new TranslateTransform(x,y));d.PushTransform(new ScaleTransform(w/200,h/230));
  string[] shades={"#20334C","#202A48","#252747","#263D38","#3B2B37","#333044","#3E2A40","#26384D","#45392E","#263637","#313042","#353248","#243A43","#2C3040","#253E44","#3B263C","#3C2F42","#22354D","#252E4D","#443933","#30394B"};
  string bg=shades[a];d.DrawRectangle(new LinearGradientBrush(Ink.C(bg),Ink.C("#101827"),90),null,new Rect(0,0,200,230));
  var rng=new Random(a*13+12);for(int i=0;i<25;i++){double sx=rng.Next(9,191),sy=rng.Next(8,205);Ink.Circle(d,sx,sy,i%4==0?1.3:.55,Ink.Gold,null);}
  Ink.Path(d,"M 19,216 L 19,87 A 81,81 0 0 1 181,87 L 181,216",null,"#786B58",.8);Ink.Path(d,"M 25,217 L 25,88 A 75,75 0 0 1 175,88 L 175,217",null,"#786B58",.5);
  Ink.Circle(d,100,105,63,null,"#6B645B",.65);Ink.Line(d,38,105,162,105,"#655F58",.5);Ink.Line(d,100,42,100,168,"#655F58",.5);
  // Original geometric illustrations: each arcana has its own symbolic composition.
  switch(a){
   case 0: Ink.Circle(d,129,65,21,"#D6B77B",null);Ink.Path(d,"M 28,205 L 77,165 116,191 116,218 28,218 Z","#465970");Ink.Path(d,"M 68,108 Q 88,96 113,116 L 101,164 66,160 Z","#9A788B");Ink.Circle(d,88,91,10,Ink.Gold,null);Ink.Line(d,97,114,145,87,Ink.Gold,3);Ink.Line(d,66,120,50,145,Ink.Gold,3);Ink.Line(d,75,160,84,181,Ink.Gold,3);Ink.Line(d,99,160,116,178,Ink.Gold,3);Ink.Path(d,"M 135,88 Q 157,93 147,109 Q 130,110 135,88", "#7D8D98");break;
   case 1: Figure(d,100,111,"#84728D");Ink.Line(d,69,106,60,58,Ink.Gold,4);Ink.Star(d,60,48,10);Ink.Line(d,132,112,140,156,Ink.Gold,4);Ink.Path(d,"M 44,169 L 156,169 148,181 51,181 Z","#755E54");Ink.Line(d,56,181,51,212);Ink.Line(d,145,181,150,212);Ink.Text(d,"∞",100,30,35,Ink.Gold,true);break;
   case 2: Pillar(d,37,88,110);Pillar(d,148,88,110);Ink.Moon(d,100,59,20,bg);Figure(d,100,120,"#7F8FAF");Ink.Path(d,"M 78,138 L 99,142 121,138 120,159 100,163 80,159 Z","#D6CBB1");break;
   case 3: Figure(d,100,110,"#839685");Crown(d,100,77);for(int i=0;i<5;i++){Ink.Line(d,39+i*6,213,35+i*8,163,Ink.Gold,1.5);Ink.Path(d,"M "+(35+i*8)+",180 q -12,-10 -6,-15 q 13,6 6,15",Ink.Gold,null);}Ink.Path(d,"M 144,166 C 118,139 143,127 151,141 C 160,126 180,140 151,169 Z","#C69C86");break;
   case 4: Ink.Path(d,"M 51,198 L 51,89 149,89 149,198 Z","#685358");Figure(d,100,119,"#A47364");Crown(d,100,83);Ink.Line(d,142,107,142,183,Ink.Gold,3);Ink.Circle(d,142,99,7,Ink.Gold,null);break;
   case 5: Pillar(d,33,71,138);Pillar(d,154,71,138);Figure(d,100,122,"#96827B");Crown(d,100,79);Ink.Line(d,100,66,100,39,Ink.Gold,2);Ink.Line(d,90,46,110,46,Ink.Gold,2);Ink.Path(d,"M 71,190 L 124,211 M 129,190 L 76,211",null,Ink.Gold,3);Ink.Circle(d,66,188,7);Ink.Circle(d,134,188,7);break;
   case 6: Ink.Path(d,"M 100,102 C 45,68 72,41 100,67 C 128,41 155,68 100,102 Z","#B9878D");Figure(d,64,150,"#83979B");Figure(d,136,150,"#9B7E93");Ink.Line(d,83,151,117,151,Ink.Gold,2);break;
   case 7: Ink.Path(d,"M 40,68 L 160,68 145,90 55,90 Z","#8D88A5");Ink.Line(d,49,72,49,157);Ink.Line(d,151,72,151,157);Figure(d,100,117,"#7E8CA5");Ink.Path(d,"M 49,149 L 151,149 145,192 55,192 Z","#44536F");Ink.Circle(d,57,194,18,"#152239");Ink.Circle(d,143,194,18,"#152239");Ink.Star(d,100,167,14);break;
   case 8: Ink.Text(d,"∞",100,34,31,Ink.Gold,true);Ink.Circle(d,106,140,40,"#A88754");Ink.Circle(d,106,138,27,"#C3A273");Ink.Path(d,"M 83,123 L 76,99 99,113 M 113,113 L 137,99 130,126","#C3A273");Ink.Line(d,93,137,99,137);Ink.Line(d,114,137,120,137);Ink.Path(d,"M 102,147 L 111,147 106,154 Z",Ink.Night);Ink.Path(d,"M 58,180 Q 100,211 147,175",null,Ink.Gold,4);break;
   case 9: Ink.Path(d,"M 105,75 Q 70,82 65,128 L 47,205 131,205 120,127 Q 126,94 105,75 Z","#718A8A");Ink.Path(d,"M 95,89 Q 105,104 110,116 L 86,115 Z",Ink.Night);Ink.Line(d,141,103,132,216,Ink.Gold,3);Ink.Path(d,"M 139,74 L 160,74 160,104 139,104 Z",null);Ink.Star(d,150,89,8);Ink.Line(d,113,126,140,102,Ink.Gold,4);break;
   case 10: Ink.Circle(d,100,120,60,"#27334C");Ink.Circle(d,100,120,49);Ink.Circle(d,100,120,18,"#9E895D");for(int i=0;i<8;i++){double t=i*Math.PI/4;Ink.Line(d,100+20*Math.Cos(t),120+20*Math.Sin(t),100+58*Math.Cos(t),120+58*Math.Sin(t));Ink.Star(d,100+72*Math.Cos(t),120+72*Math.Sin(t),5);}break;
   case 11: Ink.Line(d,100,68,100,189,Ink.Gold,4);Ink.Line(d,52,99,148,99,Ink.Gold,3);Ink.Star(d,100,58,12);Scale(d,57,101);Scale(d,143,101);Ink.Path(d,"M 80,191 L 120,191 137,206 63,206 Z","#8F8096");break;
   case 12: Ink.Line(d,37,56,164,56,Ink.Gold,5);Ink.Line(d,100,57,100,89,Ink.Gold,3);Ink.Path(d,"M 99,89 L 127,118 94,118 M 99,89 L 79,127 106,148",null,Ink.Gold,4);Ink.Path(d,"M 79,125 L 114,125 125,164 72,164 Z","#8798AA");Ink.Circle(d,98,181,11,Ink.Gold,null);Ink.Circle(d,98,181,22);break;
   case 13: Ink.Path(d,"M 61,202 Q 46,127 94,102 Q 149,114 141,203 Z","#4B5570");Ink.Circle(d,99,87,25,"#D0CBBB");Ink.Circle(d,90,84,6,Ink.Night,null);Ink.Circle(d,110,84,6,Ink.Night,null);Ink.Path(d,"M 96,95 L 104,95 100,90 Z",Ink.Night,null);Ink.Line(d,143,69,143,210,Ink.Gold,3);Ink.Path(d,"M 143,69 Q 91,36 54,82 Q 88,62 143,84", "#B8B7BA");Ink.Star(d,49,184,10);break;
   case 14: Ink.Path(d,"M 97,102 Q 52,61 36,94 L 71,158 99,133 Q 151,63 169,96 L 133,155 Z","#7D9FA5");Figure(d,100,120,"#BDC1B0");Cup(d,65,140);Cup(d,135,119);Ink.Path(d,"M 127,131 Q 117,159 75,151",null,"#95C7D0",3);break;
   case 15: Ink.Path(d,"M 99,95 L 62,57 68,104 Q 33,107 32,154 L 79,136 66,193 134,193 121,136 168,154 Q 167,107 132,104 L 138,57 Z","#8E6F8E");Ink.Circle(d,87,116,3,Ink.Gold,null);Ink.Circle(d,113,116,3,Ink.Gold,null);Ink.Path(d,"M 91,132 L 100,138 109,132",null);Ink.Circle(d,58,194,18);Ink.Circle(d,142,194,18);break;
   case 16: Ink.Path(d,"M 69,211 L 79,99 128,99 139,211 Z","#8E8198");Ink.Path(d,"M 83,126 L 95,126 95,147 81,147 Z M 113,167 L 125,167 127,188 113,188 Z",Ink.Night,null);Ink.Path(d,"M 123,36 L 88,85 110,85 91,137 141,70 118,70 Z","#E9D199",null);Ink.Path(d,"M 73,83 L 63,62 79,64 86,48 94,66 109,68 98,88 Z","#B48D6D");break;
   case 17: Ink.Star(d,100,90,36);for(int i=0;i<7;i++){double t=i*Math.PI*2/7;Ink.Star(d,100+65*Math.Cos(t),105+65*Math.Sin(t),7);}Ink.Path(d,"M 28,194 Q 66,172 100,194 T 172,194 M 35,209 Q 67,185 102,209 T 166,209",null,"#7BABB4",2);break;
   case 18: Ink.Moon(d,103,86,39,bg);Pillar(d,33,136,74);Pillar(d,148,136,74);Ink.Path(d,"M 92,215 Q 129,181 96,162 Q 79,151 101,137",null,"#B6B7C9",3);Ink.Path(d,"M 61,190 L 62,171 73,181 82,170 81,194 Z M 119,194 L 119,171 130,181 139,171 140,191 Z","#7D889F");break;
   case 19: Ink.Circle(d,100,109,38,"#D6AE66");for(int i=0;i<16;i++){double t=i*Math.PI/8;Ink.Line(d,100+47*Math.Cos(t),109+47*Math.Sin(t),100+65*Math.Cos(t),109+65*Math.Sin(t),Ink.Gold,i%2==0?2.5:1);}Ink.Path(d,"M 82,119 Q 100,134 118,119",null,"#66533F",2);Ink.Circle(d,87,103,2,Ink.Night,null);Ink.Circle(d,113,103,2,Ink.Night,null);Ink.Path(d,"M 31,211 Q 98,168 169,211","#847B63");break;
   case 20: Ink.Path(d,"M 100,79 Q 55,40 28,80 L 77,124 100,100 Q 153,41 172,80 L 125,126 Z","#9BABC0");Ink.Circle(d,100,76,11,Ink.Gold,null);Ink.Path(d,"M 88,108 L 76,143 124,143 112,108 Z","#A8AFBE");Ink.Line(d,100,103,134,126,Ink.Gold,6);Ink.Path(d,"M 128,116 L 143,119 136,136 Z",Ink.Gold);for(int i=0;i<3;i++){Ink.Line(d,75+i*25,164,75+i*25,194);Ink.Star(d,75+i*25,207,8);}break;
  }
  Ink.Line(d,34,221,166,221,"#786B58",.8);Ink.Star(d,100,222,3);d.Pop();d.Pop();d.Pop();
 }
 static void Figure(DrawingContext d,double x,double y,string c){Ink.Circle(d,x,y-24,11,Ink.Gold,null);Ink.Path(d,String.Format(CultureInfo.InvariantCulture,"M {0},{1} Q {2},{3} {4},{1} L {5},{6} {7},{6} Z",x-17,y-7,x,y-18,x+17,x+33,y+67,x-33),c);}
 static void Crown(DrawingContext d,double x,double y){Ink.Path(d,String.Format(CultureInfo.InvariantCulture,"M {0},{1} L {2},{3} {4},{5} {6},{3} {7},{1} {8},{9} {10},{9} Z",x-19,y,x-22,y-18,x-9,y-10,x,y-24,x+9,x+22,y-18,x+19,y),"#BDA472");}
 static void Pillar(DrawingContext d,double x,double y,double h){d.DrawRectangle(Ink.B("#5F6E87"),new Pen(Ink.B(Ink.Gold),.7),new Rect(x,y,14,h));Ink.Line(d,x-4,y,x+18,y,Ink.Gold,3);Ink.Line(d,x-4,y+h,x+18,y+h,Ink.Gold,3);}
 static void Cup(DrawingContext d,double x,double y){Ink.Path(d,String.Format(CultureInfo.InvariantCulture,"M {0},{1} Q {2},{3} {4},{1} Z M {2},{3} L {2},{5} M {6},{5} L {7},{5}",x-13,y,x,y+17,x+13,y+29,x-9,x+9),"#B5A078");}
 static void Scale(DrawingContext d,double x,double y){Ink.Line(d,x,y,x-19,y+40);Ink.Line(d,x,y,x+19,y+40);Ink.Path(d,String.Format(CultureInfo.InvariantCulture,"M {0},{1} Q {2},{3} {4},{1} Z",x-22,y+40,x,y+68,x+22),"#948B9F");}
}
public class CardFace : FrameworkElement {
 public static bool CurrentUpright=true;
 public Card Card; public bool Back; public bool Compact; public bool? Upright; public Rules Rules; public CardFace(Card card,Rules rules,bool compact=false){Card=card;Rules=rules;Compact=compact;IsHitTestVisible=false;}
 protected override void OnRender(DrawingContext d){
  double w=ActualWidth,h=ActualHeight;d.PushTransform(new ScaleTransform(w/220,h/340));
  string accent=Assets.Colors[Card.Arcana];
  d.DrawRoundedRectangle(new LinearGradientBrush(Ink.C("#BCAD87"),Ink.C("#32333B"),40),new Pen(Ink.B("#090B10"),2),new Rect(1,1,218,338),8,8);
  d.DrawRoundedRectangle(Ink.B("#101116"),new Pen(Ink.B(accent),3),new Rect(6,6,208,328),4,4);
  if(Back){Ink.Circle(d,110,163,64);Ink.Circle(d,110,163,55);Ink.Path(d,"M 110,90 L 161,163 110,236 59,163 Z",null);Ink.Star(d,110,163,29);Ink.Text(d,"A R C A N A",110,269,13,Ink.Gold,true,"Georgia");}
  else {
   int a=Card.Arcana;Ink.Text(d,Engine.Romans[a],110,12,14,accent,true,"Georgia");
   TarotArt.Draw(d,a,12,34,196,Compact?233:146);
   Ink.Path(d,"M 12,34 L 23,34 12,47 Z M 208,225 L 197,225 208,212 Z",accent,null);
   if(Compact){Ink.Text(d,Engine.Names[a],110,278,22,Ink.White,true);Ink.Text(d,Engine.English[a],110,311,8.5,accent,true,"Georgia");}
   else {
    var r=Rules.Get(Card);DrawCostBadge(d,r);
    Ink.Path(d,"M 5,184 L 215,177 212,212 10,219 Z","#07090D",accent,1.2);
    Ink.Text(d,r.Name,110,186,17,Ink.White,true);
    Ink.Text(d,new[]{"A","B","C"}[Card.Rank],191,16,12,accent,true,"Georgia");
    bool up=Upright ?? CurrentUpright;
    string effect=up?r.Upright:r.Reversed;double size=12;FormattedText text;
    do{text=new FormattedText(effect,CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface("Microsoft YaHei UI"),size,Ink.B(Ink.White),1.0){MaxTextWidth=184,TextAlignment=TextAlignment.Left};if(text.Height<=85 || size<=8.5)break;size-=.25;}while(true);
    d.DrawText(text,new Point(18,231));
    Ink.Text(d,Engine.Names[a]+(up?" · ↑ 正位":" · ↓ 逆位"),110,320,8.5,accent,true);
   }
  }d.Pop();
 }
 static void DrawCostBadge(DrawingContext d,CardRule rule){
  // Payment is encoded only by silhouette; every badge uses its arcana colour.
  var orb=new EllipseGeometry(new Point(28,29),24,24);
  var gem=Geometry.Parse("M 28,3 L 54,29 28,55 2,29 Z");
  var drop=Geometry.Parse("M 28,2 C 23,13 6,24 6,34 C 6,46 16,55 28,55 C 40,55 50,46 50,34 C 50,24 33,13 28,2 Z");
  Geometry shape=rule.Payment=="mana"?(Geometry)orb:rule.Payment=="crystal"?gem:drop;
  d.DrawGeometry(Ink.B(Assets.Colors[rule.Arcana]),new Pen(Ink.B("#080B13"),6),shape);
  d.DrawGeometry(null,new Pen(Ink.B("#E9E0D4"),1.6),shape);
  var number=new FormattedText(rule.Cost.ToString(),CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Georgia"),FontStyles.Normal,FontWeights.Bold,FontStretches.Normal),27,Ink.B("#FFFFFF"),1);
  var glyph=number.BuildGeometry(new Point(28-number.Width/2,(rule.Payment=="any"?33:29)-number.Height/2));
  d.DrawGeometry(Ink.B("#FFFFFF"),new Pen(Ink.B("#182032"),2.8){LineJoin=PenLineJoin.Round},glyph);
  d.DrawGeometry(Ink.B("#FFFFFF"),null,glyph);
 }
 public static string PayName(string p){return p=="mana"?"仅回合费用":p=="crystal"?"仅法力水晶":"通用费用 · 可混合支付";}
}
public class Cosmos : FrameworkElement {
 public bool Menu; public Cosmos(bool menu){Menu=menu;IsHitTestVisible=false;}
 protected override void OnRender(DrawingContext d){double w=ActualWidth,h=ActualHeight;
  d.DrawRectangle(new LinearGradientBrush(Ink.C("#131F35"),Ink.C("#080E1B"),65),null,new Rect(0,0,w,h));
  var r=new Random(87);for(int i=0;i<155;i++){double x=r.NextDouble()*w,y=r.NextDouble()*h;Ink.Circle(d,x,y,i%9==0?1.6:.7,i%3==0?"#887B63":"#405269",null);}
  d.PushOpacity(.35);double cx=Menu?w*.69:w*.5,cy=Menu?h*.48:h*.42;
  foreach(double radius in new[]{175.0,188.0,250.0,268.0,340.0})Ink.Circle(d,cx,cy,radius,null,"#8B795A",.6);
  for(int i=0;i<60;i++){double t=i*Math.PI/30;Ink.Line(d,cx+250*Math.Cos(t),cy+250*Math.Sin(t),cx+(i%5==0?267:257)*Math.Cos(t),cy+(i%5==0?267:257)*Math.Sin(t),Ink.Gold,.8);}
  for(int i=0;i<12;i++){double t=i*Math.PI/6;Ink.Star(d,cx+340*Math.Cos(t),cy+340*Math.Sin(t),4);}
  d.Pop();
 }
}
}


