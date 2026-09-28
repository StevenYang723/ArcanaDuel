using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Reflection;
namespace ArcanaDuel {
public static class Assets {
 public static readonly string[] Colors={"#C4AF7F","#EA382C","#2470D8","#DB5F8D","#9F233E","#D5AB31","#F07967","#28B7D9","#F08424","#7F9A67","#AC703E","#728EB8","#209986","#A575D1","#9BD5CC","#D62483","#C96520","#6DBBE8","#625FB0","#EDD738","#BE99DA"};
 public static readonly BitmapSource Board=Load("board.png");
 public static readonly BitmapSource Atlas=Load("arcana-atlas.png");
 static BitmapSource[] art=new BitmapSource[21];
 static BitmapSource Load(string name){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)){if(s==null)return null;var b=new BitmapImage();b.BeginInit();b.CacheOption=BitmapCacheOption.OnLoad;b.StreamSource=s;b.EndInit();b.Freeze();return b;}}
 public static BitmapSource Art(int a){if(Atlas==null)return null;if(art[a]!=null)return art[a];double[] xs={0,274,539,810,1101,1371,1648,1916};double[] ys={0,272,541,821};int c=a%7,r=a/7;int x=(int)(xs[c]/1916*Atlas.PixelWidth)+3,y=(int)(ys[r]/821*Atlas.PixelHeight)+3;int right=(int)(xs[c+1]/1916*Atlas.PixelWidth)-3,bottom=(int)(ys[r+1]/821*Atlas.PixelHeight)-3;art[a]=new CroppedBitmap(Atlas,new Int32Rect(x,y,right-x,bottom-y));art[a].Freeze();return art[a];}
}
public class SceneBoard : FrameworkElement {
 protected override void OnRender(DrawingContext d){if(Assets.Board!=null)d.DrawImage(Assets.Board,new Rect(0,0,ActualWidth,ActualHeight));else d.DrawRectangle(Ink.B(Ink.Night),null,new Rect(0,0,ActualWidth,ActualHeight));}
}
public class Relic : FrameworkElement {
 public string Kind;public string Caption;public string Color=Ink.Gold;public bool Lit=true;
 public Relic(string kind,string caption){Kind=kind;Caption=caption;IsHitTestVisible=false;}
 protected override void OnRender(DrawingContext d){double w=ActualWidth,h=ActualHeight;d.PushTransform(new ScaleTransform(w/100,h/100));
  if(Kind=="mana"){
   d.DrawEllipse(new LinearGradientBrush(Ink.C("#B8C8CA"),Ink.C("#26313D"),65),new Pen(Ink.B("#B5C2C5"),2),new Point(50,50),44,44);
   d.DrawEllipse(new RadialGradientBrush(Ink.C(Lit?"#B1F3FF":"#253240"),Ink.C(Lit?"#197088":"#0B1522")),new Pen(Ink.B("#101D28"),3),new Point(50,50),37,37);
   if(Lit){Ink.Path(d,"M 26,45 Q 35,20 57,26",null,"#D8FFFF",5);Ink.Path(d,"M 65,70 Q 72,66 75,57",null,"#73D5E1",2);}Ink.Text(d,Caption,50,34,25,Ink.White,true,"Georgia");
  }else if(Kind=="gem"){
   Ink.Path(d,"M 50,5 L 85,29 85,70 50,96 15,70 15,29 Z","#101923","#A2987B",2.5);
   Ink.Path(d,"M 50,12 L 77,33 77,66 50,87 23,66 23,33 Z",Lit?Color:"#263344",null);
   d.PushOpacity(.5);Ink.Path(d,"M 50,12 L 23,33 50,41 77,33 Z M 23,33 L 23,66 50,87 50,41 Z","#FFFFFF",null);d.Pop();
   Ink.Text(d,Caption,50,35,23,"#FFFFFF",true,"Georgia");
  }else if(Kind=="tile"){
   Ink.Path(d,"M 12,7 L 87,7 96,17 96,81 86,94 13,94 4,81 4,17 Z","#080E18","#B29B67",1.4);
   var b=new LinearGradientBrush(Ink.C("#536372"),Ink.C("#152030"),65);d.DrawGeometry(b,new Pen(Ink.B("#77818A"),1),Geometry.Parse("M 15,12 L 84,12 91,21 91,77 82,87 17,87 9,77 9,21 Z"));
   Ink.Circle(d,50,48,24,null,"#7E7968",.7);Ink.Text(d,Caption,50,32,23,Color,true,"Georgia");Ink.Star(d,50,82,3,"#A29677");
  }else {
   d.DrawEllipse(new RadialGradientBrush(Ink.C("#746143"),Ink.C("#211E1D")),new Pen(Ink.B("#C0A477"),2),new Point(50,50),47,47);
   Ink.Circle(d,50,50,41,null,"#E2C98F",1);d.DrawEllipse(new RadialGradientBrush(Ink.C(Lit?"#345459":"#27313D"),Ink.C("#0D1723")),new Pen(Ink.B("#8A7960"),1.4),new Point(50,50),36,36);
   for(int i=0;i<12;i++){double a=i*Math.PI/6;Ink.Line(d,50+39*Math.Cos(a),50+39*Math.Sin(a),50+44*Math.Cos(a),50+44*Math.Sin(a),"#D4B77E",1);}
   Ink.Text(d,Caption,50,Kind=="turn"?40:32,Kind=="turn"?14:27,Lit?Color:"#8996A6",true);
  }d.Pop();
 }
}
public class CastSigil : FrameworkElement {
 protected override void OnRender(DrawingContext d){double x=ActualWidth/2,y=ActualHeight/2;d.DrawEllipse(new RadialGradientBrush(Ink.C("#502FA99B"),Colors.Transparent),null,new Point(x,y),ActualWidth/2,ActualHeight/2);d.DrawEllipse(null,new Pen(Ink.B("#93D8B8"),2),new Point(x,y),ActualWidth*.43,ActualHeight*.41);d.DrawEllipse(null,new Pen(Ink.B("#E1CC8F"),1),new Point(x,y),ActualWidth*.40,ActualHeight*.36);for(int i=0;i<12;i++){double a=i*Math.PI/6;Ink.Star(d,x+ActualWidth*.45*Math.Cos(a),y+ActualHeight*.43*Math.Sin(a),4,"#C9DBAB");}}
}
}
