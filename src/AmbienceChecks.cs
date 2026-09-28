using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ArcanaDuel {
public partial class GameWindow {
 Button HomeControl(string tag){stage.UpdateLayout();return Descendants<Button>(page).Single(b=>Object.Equals(b.Tag,tag));}
 async Task CheckAmbience(string folder,List<string> results){
  Menu();stage.UpdateLayout();await Task.Delay(130);
  Verify(atmosphere.IsLoaded && !atmosphere.IsHitTestVisible && atmosphere.MenuScene,"home ambience is loaded below interactive controls",results);
  Verify(new[]{"home-host","home-join","home-solo","home-test","home-library","home-guide","home-motion"}.All(tag=>HomeControl(tag)!=null),"all home entry points remain available",results);
  DependencyObject hit=stage.InputHitTest(new Point(280,450)) as DependencyObject;while(hit!=null && !(hit is Button))hit=VisualTreeHelper.GetParent(hit);
  Verify(hit is Button && Object.Equals(((Button)hit).Tag,"home-host"),"animated home art does not intercept primary button",results);
  int frames=atmosphere.Frames;double time=atmosphere.Phase;Capture(Path.Combine(folder,"18-home.png"));await Task.Delay(380);
  Verify(atmosphere.Frames>frames && atmosphere.Phase>time,"home starfield and floating-card clock advances",results);Capture(Path.Combine(folder,"19-home-motion.png"));
  Click(HomeControl("home-motion"));await Task.Delay(70);time=atmosphere.Phase;frames=atmosphere.Frames;await Task.Delay(150);
  Verify(!atmosphere.Motion && !atmosphere.ClockRunning && atmosphere.Phase==time && atmosphere.Frames==frames,"motion toggle freezes scene and stops idle redraws",results);
  Click(HomeControl("home-motion"));await Task.Delay(120);Verify(atmosphere.ClockRunning && atmosphere.Phase>time,"home motion can resume",results);
  WindowState=WindowState.Minimized;await Task.Delay(90);time=atmosphere.Phase;await Task.Delay(100);Verify(!atmosphere.ClockRunning && atmosphere.Phase==time,"minimized window suspends ambient animation",results);WindowState=WindowState.Normal;await Task.Delay(90);
  Click(HomeControl("home-host"));Verify(mode=="hostLobby" && !atmosphere.MenuScene,"home host entry opens room setup",results);Cleanup();Menu();
  Click(HomeControl("home-join"));Verify(mode=="joinLobby" && ipBox!=null,"home join entry opens connection setup",results);Cleanup();Menu();
  Click(HomeControl("home-library"));Verify(Descendants<CardFace>(modal).Count()==21,"home library opens all arcana",results);modal.Children.Clear();
  Click(HomeControl("home-guide"));Verify(Descendants<ScrollViewer>(modal).Any(),"home guide remains accessible",results);modal.Children.Clear();
  Click(HomeControl("home-test"));Verify(mode=="test" && state.Phase=="draft","home local two-player entry starts draft",results);Cleanup();Menu();
  Click(HomeControl("home-solo"));Verify(mode=="solo" && state.Phase=="draft","home solo entry starts draft",results);Cleanup();
  PresentFixture(11,2,true);SelectPaymentForCheck(2,1);await Task.Delay(90);int revision=engine.Revision;time=atmosphere.Phase;
  Capture(Path.Combine(folder,"20-battle-ambience.png"));await Task.Delay(180);
  Verify(!atmosphere.MenuScene && atmosphere.Phase>time && engine.Revision==revision && SelectedMana==2 && SelectedCrystals==1,"battle ambience runs without mutating game or selected payment",results);
  SceneMenu();Click(NamedButton("场景动效：开"));Verify(!atmosphere.Motion && !atmosphere.ClockRunning && SelectedMana==2,"battle menu motion toggle preserves payment selection",results);
  Click(NamedButton("场景动效：关"));Click(NamedButton("继续对局"));
  Verify(page.Children.OfType<SceneAtmosphere>().Count()==1,"rerendering uses one ambient layer per window",results);
  Cleanup();Menu();await CaptureAmbientClip(Path.Combine(folder,"home-motion.gif"));
 }
 async Task CaptureAmbientClip(string path){
  var encoder=new GifBitmapEncoder();
  for(int i=0;i<32;i++){
   stage.UpdateLayout();var drawing=new DrawingVisual();using(var d=drawing.RenderOpen())d.DrawRectangle(new VisualBrush(stage),null,new Rect(0,0,864,540));
   var bitmap=new RenderTargetBitmap(864,540,96,96,PixelFormats.Pbgra32);bitmap.Render(drawing);
   var metadata=new BitmapMetadata("gif");metadata.SetQuery("/grctlext/Delay",(ushort)15);metadata.SetQuery("/grctlext/Disposal",(byte)1);
   encoder.Frames.Add(BitmapFrame.Create(bitmap,null,metadata,null));await Task.Delay(150);
  }
  using(var file=File.Create(path))encoder.Save(file);
 }
}
}
