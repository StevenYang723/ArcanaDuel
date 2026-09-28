using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ArcanaDuel {
public partial class GameWindow {
 readonly bool[] selectedMana=new bool[3],selectedCrystals=new bool[3];
 int selectionViewer=-1;
 int SelectedMana {get{return selectedMana.Count(x=>x);}}
 int SelectedCrystals {get{return selectedCrystals.Count(x=>x);}}
 void ClearPaymentSelection(){Array.Clear(selectedMana,0,3);Array.Clear(selectedCrystals,0,3);selectionViewer=viewer;}
 void SyncPaymentSelection(){
  if(selectionViewer!=viewer)ClearPaymentSelection();
  var p=state.Players[viewer];for(int i=0;i<3;i++){if(i>=p.Mana)selectedMana[i]=false;if(i>=p.Crystals)selectedCrystals[i]=false;}
 }
 void ToggleResource(bool crystal,int index){
  if(!MyTurn() || state.Phase!="battle" || modal.Children.Count>0 || dragging!=null)return;
  SyncPaymentSelection();var p=state.Players[viewer];if(index<0 || index>=3 || index>=(crystal?p.Crystals:p.Mana))return;
  var selected=crystal?selectedCrystals:selectedMana;selected[index]=!selected[index];Render();
 }
 void DrawPaymentResources(){
  SyncPaymentSelection();var p=state.Players[viewer];bool mine=MyTurn();
  for(int kind=0;kind<2;kind++)for(int i=0;i<3;i++){
   bool crystal=kind==1;int index=i;bool on=(crystal?selectedCrystals:selectedMana)[i];bool available=i<(crystal?p.Crystals:p.Mana);
   var b=Btn("",()=>ToggleResource(crystal,index),40);b.Height=48;b.Padding=new Thickness(0);b.Tag="pay-"+(crystal?"crystal-":"mana-")+i;
   b.Background=Brushes.Transparent;b.BorderThickness=new Thickness(on?2:0);b.BorderBrush=Ink.B(Ink.White);
   b.Content=new Relic(crystal?"gem":"mana",on?"✓":""){Width=36,Height=44,Lit=available,Color=crystal?"#B786DC":"#53BBCB"};
   b.ToolTip=(crystal?"法力水晶":"回合费用")+" · "+(on?"已选中，点击取消":"点击选中支付");b.IsEnabled=mine && available;
   if(on)b.Effect=new DropShadowEffect{Color=Ink.C(Ink.Gold),BlurRadius=17,ShadowDepth=0,Opacity=.85};
   Put(page,b,1124+i*47,crystal?804:727);
  }
  Inscribe(page,SelectedMana+" / "+p.Mana,1171,780,12,"#B9E3E7");Inscribe(page,SelectedCrystals+" / "+p.Crystals,1171,856,12,"#E0C5F5");
  var total=Artifact("seal",(SelectedMana+SelectedCrystals).ToString(),()=>{if(MyTurn()){ClearPaymentSelection();Render();}},1291,771,52,"已选支付总点数；点击清空选择",SelectedMana+SelectedCrystals>0);
  total.IsEnabled=mine;
 }
 string PaymentHint(Card card){
  var r=rules.Get(card);int pay=Payment(card);
  if(pay>=0)return "拖至场中 · "+SelectedMana+" 费用 + "+SelectedCrystals+" 水晶";
  if(SelectedMana+SelectedCrystals!=r.Cost)return "请点选合计 "+r.Cost+" 点资源（已选 "+(SelectedMana+SelectedCrystals)+"）";
  if(r.Payment=="mana" && SelectedCrystals>0)return "此牌仅能使用回合费用";
  if(r.Payment=="crystal" && SelectedMana>0)return "此牌仅能使用法力水晶";
  return "费用或必需的额外代价不足";
 }
 bool ToggleHoverPreview(){
  if(preview==null || preview.Compact || busy || modal.Children.Count>0 || !previews.Children.Contains(preview))return false;
  preview.Upright=!(preview.Upright ?? CardFace.CurrentUpright);preview.InvalidateVisual();
  return true;
 }
 void InspectCard(Card card,bool upright,Action back=null){
  if(busy || state==null || state.ChoiceOwner>=0 || state.PendingTokens>=0)return;
  ModalBase(780,750);
  var face=new CardFace(card,rules){Upright=upright};Put(modal,face,389,157,330,510);
  Label(modal,"牌面预览",759,206,27,Ink.Gold,280);
  Label(modal,"仅查看效果，不改变当前正逆位。",759,253,15,Ink.Muted,248);
  var status=T("当前局面 · "+(upright?"正位":"逆位"),17,Ink.White);Put(modal,status,759,311,240);
  Button toggle=null;toggle=Btn(upright?"预览逆位":"预览正位",()=>{
   face.Upright=!face.Upright.Value;face.InvalidateVisual();
   toggle.Content=face.Upright.Value?"预览逆位":"预览正位";
   status.Text=(face.Upright.Value==upright?"当前局面 · ":"另一牌面 · ")+(face.Upright.Value?"正位":"逆位");
  },225);toggle.Tag="preview-flip";Put(modal,toggle,759,361);
  Label(modal,CardFace.PayName(rules.Get(card).Payment),759,430,15,Ink.Muted,245);
  Put(modal,Btn("返回",()=>{modal.Children.Clear();if(back!=null)back();},225),759,553);
 }
}
}
