using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcanaDuel {
public partial class Engine {
 void Q(Action action){steps.Enqueue(action);}
 bool Prior(PlayContext c,int arc,int rank=-1){return c.Previous.Any(x=>x.Arcana==arc && (rank<0 || x.Rank==rank));}
 bool Cross(PlayContext c){return c.Previous.Count>0 && c.Previous.Last().Arcana!=c.Card.Arcana;}
 bool TwoKinds(PlayContext c){return c.Previous.Select(x=>x.Arcana).Distinct().Count()>=2;}
 IEnumerable<ChoiceOption> CardOptions(IEnumerable<Card> cards){return cards.Select(c=>new ChoiceOption{Id=c.Id,Card=c,Text=Names[c.Arcana]+" · "+Rules.Get(c).Name});}
 List<ChoiceOption> TokenOptions(int actor,IEnumerable<int> suits=null){
  var options=new List<ChoiceOption>();foreach(int s in suits??Enumerable.Range(0,4))for(int n=0;n<Players[actor].Tokens[s];n++)options.Add(new ChoiceOption{Id=s*100+n,Suit=s,Text=Suits[s]+" "+(n+1)});return options;
 }
 void Option(int actor,string title,string[] labels,Action<int> done){Ask(actor,title,"option",labels.Select((s,i)=>new ChoiceOption{Id=i,Text=s}),1,1,(ids,split)=>done(ids[0]));}
 void ExtraFixed(PlayContext c,int[] tokens,int crystals=0){
  Q(()=>{var p=Players[c.Actor];if(tokens.Where((n,i)=>n>p.Tokens[i]).Any() || p.Crystals-c.CrystalPay<crystals)return;
   string cost=String.Join(" + ",tokens.Select((n,i)=>n>0?n+" "+Suits[i]:null).Where(s=>s!=null));if(crystals>0)cost+=(cost.Length>0?" + ":"")+crystals+" 水晶";
   Option(c.Actor,"追加代价",new[]{"不追加","消耗 "+cost},value=>{if(value==1){c.Extra=1;c.ExtraCrystals=crystals;c.ExtraTokens=(int[])tokens.Clone();}});
  });
 }
 void ExtraCount(PlayContext c,int suit,int max){Q(()=>{int count=Math.Min(max,Players[c.Actor].Tokens[suit]);if(count==0)return;Option(c.Actor,"追加"+Suits[suit]+" · 每个增加 1 伤害",Enumerable.Range(0,count+1).Select(n=>n==0?"不追加":"消耗 "+n+" "+Suits[suit]).ToArray(),n=>{c.Extra=n;c.ExtraTokens[suit]=n;});});}
 void ExtraAny(PlayContext c,int count,bool different=false,int[] suits=null){Q(()=>{
  var options=TokenOptions(c.Actor,suits);if(options.Count<count || different && options.Select(x=>x.Suit).Distinct().Count()<count)return;
  Ask(c.Actor,"选择额外消耗的资源 · 可不追加","tokens",options,0,count,(ids,split)=>{foreach(int id in ids)c.ExtraTokens[id/100]++;c.Extra=ids.Length>0?1:0;},(ids,split)=>ids.Length!=0 && ids.Length!=count?"请选择 "+count+" 个，或不追加。":different && ids.Select(x=>x/100).Distinct().Count()!=ids.Length?"需要不同种类的资源。":null);
 });}
 void ExtraDiscard(PlayContext c,bool mandatory,int sword=0){Q(()=>{
  var cards=Players[c.Actor].Hand.Where(x=>x.Id!=c.Card.Id).ToList();if(cards.Count==0 || Players[c.Actor].Tokens[2]<sword)return;
  Ask(c.Actor,mandatory?"必须额外弃置 1 张手牌":"可额外弃置 1 张手牌"+(sword>0?"并消耗 1 宝剑":""),"cards",CardOptions(cards),mandatory?1:0,1,(ids,split)=>{c.Discards.AddRange(ids);if(ids.Length>0){c.Extra=1;c.ExtraTokens[2]=sword;}});
 });}
 void ConfigureExtras(PlayContext c){
  int a=c.Card.Arcana,r=c.Card.Rank;bool up=c.Up;
  if(up){switch(a){
   case 0:if(r==2)ExtraAny(c,1);break;
   case 1:if(r==1 && c.Previous.Count>0)ExtraFixed(c,new[]{1,0,0,0});if(r==2)ExtraCount(c,0,2);break;
   case 2:if(r==2 && c.ScryBefore>0)ExtraFixed(c,new[]{0,1,0,0});break;
   case 3:if(r==2)ExtraFixed(c,new[]{2,2,0,0});break;
   case 4:if(r==2)ExtraCount(c,3,2);break;
   case 5:if(r==2 && Prior(c,5,0) && Prior(c,5,1))ExtraFixed(c,new[]{0,0,0,2});break;
   case 6:if(r==2 && TwoKinds(c))ExtraFixed(c,new[]{1,1,0,0});break;
   case 7:if(r==1)ExtraFixed(c,new[]{1,0,0,0});if(r==2)ExtraFixed(c,new[]{0,0,1,0});break;
   case 8:if(r==0)ExtraFixed(c,new[]{0,0,1,0});if(r==2)ExtraCount(c,2,3);break;
   case 9:if(r==2)ExtraFixed(c,new[]{0,1,0,0});break;
   case 10:if(r==2)ExtraFixed(c,new[]{0,0,0,1});break;
   case 12:if(r==2)ExtraFixed(c,new[]{0,0,0,1});break;
   case 13:if(r==0)ExtraDiscard(c,false);if(r==2)ExtraDiscard(c,false,1);break;
   case 14:if(r==1)ExtraAny(c,1);if(r==2)ExtraAny(c,2,true);break;
   case 15:if(r==0)ExtraDiscard(c,true);if(r==1)ExtraFixed(c,new[]{0,0,1,0},1);if(r==2){c.ExtraCrystals=Players[c.Actor].Crystals-c.CrystalPay;c.Extra=c.ExtraCrystals;}break;
   case 16:if(r==0 && Players[1-c.Actor].Tokens[3]>0)ExtraFixed(c,new[]{1,0,0,0});if(r==1)ExtraFixed(c,new[]{0,0,1,0});break;
   case 17:if(r==2 && c.DrawsBefore>=2)ExtraFixed(c,new[]{0,1,0,0});break;
   case 19:if(r==1)ExtraFixed(c,new[]{0,0,1,0});break;
   case 20:if(r==2 && Players[c.Actor].Discard.Select(x=>x.Arcana).Distinct().Count()>=4)ExtraAny(c,1,false,new[]{1,2});break;
  }}else{switch(a){
   case 1:if(r==1)ExtraFixed(c,new[]{1,0,0,0});break;
   case 10:if(r==2)ExtraFixed(c,new[]{0,2,0,0});break;
   case 11:if(r==2)ExtraFixed(c,new[]{0,0,0,1});break;
   case 13:if(r==2)ExtraFixed(c,new[]{0,2,0,0});break;
   case 15:if(r==1)ExtraDiscard(c,false);if(r==2)ExtraDiscard(c,true);break;
  }}
 }
 void Damage(int actor,int amount){
  var other=Players[1-actor];int blocked=Math.Min(other.Shield,amount);other.Shield-=blocked;int actual=amount-blocked,before=Position;
  Position=Math.Max(0,Math.Min(RoadEnd,Position+(actor==0?actual:-actual)));Players[actor].DamageThisTurn+=actual;LastEvent.Amount+=actual;
  Step("damage",actor,actual,"伤害 "+actual+(blocked>0?" · 屏障抵消 "+blocked:""),before);CheckWinner();
 }
 void Retreat(int actor,int amount){int before=Position;Position=Math.Max(0,Math.Min(RoadEnd,Position+(actor==0?-amount:amount)));Step("retreat",actor,amount,"后退 "+amount,before);CheckWinner();}
 void CheckWinner(){if(Position==0 || Position==RoadEnd){Winner=Position==RoadEnd?0:1;Phase="ended";Note(Players[Winner].Name+" 抵达终点，赢得本局！");}}
 void EffectDraw(int actor,int count){int actual=DrawInternal(actor,count,true);Step("draw",actor,actual,"抽取 "+actual+" 张牌");}
 void Gain(int actor,int suit,int n){if(n<=0)return;GrantTokens(actor,suit,n);Step("token",actor,n,"获得 "+n+" "+Suits[suit]);}
 void Shield(int actor,int n){int old=Players[actor].Shield;Players[actor].Shield=Math.Min(3,old+n);Step("shield",actor,Players[actor].Shield-old,"屏障 +"+(Players[actor].Shield-old));}
 void BreakShield(int actor,int n){int actual=Math.Min(Players[1-actor].Shield,n);Players[1-actor].Shield-=actual;Step("break",actor,actual,"清除对手 "+actual+" 屏障");}
 void Mana(int actor,int n){int old=Players[actor].Mana;Players[actor].Mana=Math.Min(3,old+n);Step("mana",actor,Players[actor].Mana-old,"费用 +"+(Players[actor].Mana-old));}
 void Crystal(int actor,int n){int old=Players[actor].Crystals;Players[actor].Crystals=Math.Min(3,old+n);Step("crystal",actor,Players[actor].Crystals-old,"水晶 +"+(Players[actor].Crystals-old));}
 void Flip(int actor){UprightPlayer=1-UprightPlayer;Players[actor].OwnFlips++;Step("flip",actor,1,Upright(actor)?"转为正位":"转为逆位");}
 void Confuse(int actor){Players[1-actor].Confused=true;Step("confuse",actor,1,"对手受到迷惑");}
 void GainDifferent(int actor,int n){
  Ask(actor,"选择获得的"+n+"种小阿尔卡纳","tokens",Enumerable.Range(0,4).Select(i=>new ChoiceOption{Id=i,Suit=i,Text=Suits[i]}),n,n,(ids,split)=>{foreach(int id in ids)Gain(actor,id,1);});
 }
 void DiscardHand(int actor,int max,bool optional,Action<int> done){
  var cards=Players[actor].Hand.ToList();int count=Math.Min(max,cards.Count);if(count==0){done(0);return;}
  Ask(actor,optional?"选择主动弃牌 · 可跳过":"选择主动弃牌","cards",CardOptions(cards),optional?0:count,count,(ids,split)=>{
   foreach(int id in ids){var card=Players[actor].Hand.Single(x=>x.Id==id);Players[actor].Hand.Remove(card);Players[actor].Discard.Add(card);Players[actor].VoluntaryDiscards++;}if(ids.Length>0)Step("discard",actor,ids.Length,"主动弃置 "+ids.Length+" 张牌");done(ids.Length);
  });
 }
 void BottomOne(int actor,Action done){var p=Players[actor];if(p.Hand.Count==0)return;Ask(actor,"可将 1 张手牌放到牌库底","cards",CardOptions(p.Hand),0,1,(ids,split)=>{if(ids.Length==0)return;var c=p.Hand.Single(x=>x.Id==ids[0]);p.Hand.Remove(c);p.Deck.Add(c);Step("bottom",actor,1,"将 1 张手牌置底");done();});}
 void OrderBottom(int actor,List<Card> cards,Action done){
  if(cards.Count<=1){foreach(var card in cards){Players[actor].Deck.Remove(card);Players[actor].Hand.Remove(card);Players[actor].Deck.Add(card);}done();return;}
  Ask(actor,"依次选择牌库底的顺序 · 左侧先被抽到","order",CardOptions(cards),cards.Count,cards.Count,(ids,split)=>{foreach(var card in cards){Players[actor].Deck.Remove(card);Players[actor].Hand.Remove(card);}foreach(int id in ids)Players[actor].Deck.Add(cards.Single(x=>x.Id==id));Step("bottom",actor,cards.Count,"整理牌库底 "+cards.Count+" 张牌");done();});
 }
 void Scry(int actor,int n){
  Players[actor].Scries++;var cards=Players[actor].Deck.Take(n).ToList();if(cards.Count==0){Step("scry",actor,0,"预知：牌库为空");return;}
  Ask(actor,"预知 "+cards.Count+" · 依次排序，再选留在顶部的张数","scry",CardOptions(cards),cards.Count,cards.Count,(ids,top)=>{
   var p=Players[actor];foreach(var card in cards)p.Deck.Remove(card);var ordered=ids.Select(id=>cards.Single(x=>x.Id==id)).ToList();p.Deck.InsertRange(0,ordered.Take(top));p.Deck.AddRange(ordered.Skip(top));Step("scry",actor,cards.Count,"完成预知 "+cards.Count);
  });
 }
 void SelectTop(int actor,int n){
  var p=Players[actor];var cards=p.Deck.Take(n).ToList();if(cards.Count==0)return;
  Ask(actor,"从牌库顶选择 1 张加入手牌","cards",CardOptions(cards),1,1,(ids,split)=>{var selected=cards.Single(x=>x.Id==ids[0]);p.Deck.Remove(selected);AddHand(actor,selected);cards.Remove(selected);Step("recover",actor,1,"从牌库顶选择 1 张加入手牌");OrderBottom(actor,cards,()=>{});});
 }
 void Recover(int actor,int n,Func<Card,bool> filter,bool different=false,bool optional=true){
  var p=Players[actor];var cards=p.Discard.Where(filter).ToList();int count=Math.Min(n,different?cards.Select(x=>x.Arcana).Distinct().Count():cards.Count);if(count==0)return;
  Ask(actor,"回收"+(different?"不同大阿尔卡纳的":"")+"至多 "+count+" 张牌","cards",CardOptions(cards),optional?0:count,count,(ids,split)=>{
   var chosen=ids.Select(id=>cards.Single(x=>x.Id==id)).ToList();foreach(var c in chosen)p.Discard.Remove(c);foreach(var c in chosen)AddHand(actor,c);if(chosen.Count>0)Step("recover",actor,chosen.Count,"从弃牌区回收 "+chosen.Count+" 张牌");
  },(ids,split)=>different && ids.Select(id=>cards.Single(x=>x.Id==id).Arcana).Distinct().Count()!=ids.Length?"需要选择不同大阿尔卡纳的牌。":null);
 }
 void Recolor(int actor,int max){
  var opts=TokenOptions(actor);if(opts.Count==0)return;
  Ask(actor,"选择最多 "+max+" 个资源调色","tokens",opts,0,Math.Min(max,opts.Count),(ids,split)=>RecolorNext(actor,ids,0));
 }
 void RecolorNext(int actor,int[] ids,int at){
  if(at>=ids.Length)return;int from=ids[at]/100;
  Ask(actor,"将 "+Suits[from]+" 转换为","tokens",Enumerable.Range(0,4).Where(i=>i!=from).Select(i=>new ChoiceOption{Id=i,Suit=i,Text=Suits[i]}),1,1,(choice,split)=>{Players[actor].Tokens[from]--;Players[actor].Tokens[choice[0]]++;Step("token",actor,0,Suits[from]+" → "+Suits[choice[0]]);RecolorNext(actor,ids,at+1);});
 }
 List<Card> Reveal(int actor,int n){var cards=Players[actor].Deck.Take(n).ToList();Step("reveal",actor,cards.Count,"展示牌库顶 "+cards.Count+" 张",-1,cards);return cards;}
 void LoseTokens(int actor,int max,Action<int> done){
  var opts=TokenOptions(actor);int n=Math.Min(max,opts.Count);if(n==0){done(0);return;}
  Ask(actor,"高塔 · 选择失去的 "+n+" 个资源","tokens",opts,n,n,(ids,split)=>{foreach(int id in ids)Players[actor].Tokens[id/100]--;Step("destroy",actor,ids.Length,"失去 "+ids.Length+" 个小阿尔卡纳");done(ids.Length);});
 }
 void Peek(int actor){Ask(actor,"月蚀 · 对手当前手牌（仅本次可见）","inspect",CardOptions(Players[1-actor].Hand),0,0,(ids,split)=>Step("inspect",actor,0,"查看了对手当前手牌"));}

 void BuildEffects(PlayContext c){
  int p=c.Actor,a=c.Card.Arcana,r=c.Card.Rank;var me=Players[p];var other=Players[1-p];int extra=c.Extra;
  if(c.Up){switch(a){
   case 0:Q(()=>Damage(p,r+1+(r==2?extra:0)));if(r==1)Q(()=>Recolor(p,2));break;
   case 1:Q(()=>Damage(p,r==0?1:r==1?2+extra:2+extra));if(r==0)Q(()=>Gain(p,0,1));break;
   case 2:
    if(r==0){Q(()=>Damage(p,1));Q(()=>Scry(p,1));}
    if(r==1)Q(()=>{var cards=Reveal(p,1);Damage(p,cards.Count==1 && cards[0].Rank==2?3:2);});
    if(r==2)Q(()=>Damage(p,3+(c.ScryBefore>0?1:0)+extra));break;
   case 3:
    if(r==0){Q(()=>Damage(p,1));Q(()=>Gain(p,1,1));}
    if(r==1){Q(()=>Shield(p,2));Q(()=>Gain(p,0,2));}
    if(r==2)Q(()=>Damage(p,3+extra*3));break;
   case 4:Q(()=>Damage(p,r+1+(r==2?extra:0)));if(r<2 && c.Advantage)Q(()=>Gain(p,3,1));break;
   case 5:Q(()=>Damage(p,r==0?1:r==1?2+(Prior(c,5)?1:0):3+extra*2));if(r==0 && Prior(c,5))Q(()=>Gain(p,3,1));break;
   case 6:Q(()=>Damage(p,r==0?1:r==1?2+(TwoKinds(c)?1:0):3+(TwoKinds(c)?1:0)+extra*2));if(r==0 && Cross(c))Q(()=>Gain(p,0,1));break;
   case 7:Q(()=>Damage(p,r==0?1:r==1?2+extra:3+(c.DamageBefore>=3?1:0)+extra));if(r==0 && c.DamageBefore>=1)Q(()=>Gain(p,0,1));break;
   case 8:if(r==1)Q(()=>BreakShield(p,1));Q(()=>Damage(p,r==0?1+extra:r==1?2:3+extra));break;
   case 9:Q(()=>Damage(p,r==0?1:r==1?2+(c.HandCount<=1?1:0):3+(c.HandCount==0?1:0)+extra));if(r==0 && c.HandCount<=2)Q(()=>Gain(p,1,1));break;
   case 10:
    if(r==0)Q(()=>{var cards=Reveal(p,1);Damage(p,cards.Count==1 && cards[0].Rank==2?2:1);if(Phase!="ended")OrderBottom(p,cards,()=>{});});
    if(r==1)Q(()=>{var cards=Reveal(p,2);Damage(p,cards.Count==2 && cards[0].Rank!=cards[1].Rank?3:2);if(Phase!="ended")OrderBottom(p,cards,()=>{});});
    if(r==2){if(extra>0)Q(()=>Scry(p,3));Q(()=>Option(p,"命运重掷",new[]{"稳定 · 造成 3 点伤害","冒险 · A/B/C 齐全打 5，否则打 2"},n=>{if(n==0)Damage(p,3);else{var cards=Reveal(p,3);Damage(p,cards.Count==3 && cards.Select(x=>x.Rank).Distinct().Count()==3?5:2);if(Phase!="ended")OrderBottom(p,cards,()=>{});}}));}break;
   case 11:if(r==1 && c.Behind)Q(()=>BreakShield(p,1));Q(()=>Damage(p,r==0?1+(c.OtherCrystals>c.Crystals?1:0):r==1?2:c.OtherTokens>=c.Tokens+3?5:3));break;
   case 12:Q(()=>Damage(p,r==0?1:r==1?2+(c.Behind?1:0):2+(c.Behind?2:0)+extra));if(r==0 && c.Behind)Q(()=>Gain(p,3,1));break;
   case 13:Q(()=>Damage(p,r==0?1+extra:r==1?2+(c.DiscardsBefore>0?1:0):3+extra*2));if(r==0 && extra>0)Q(()=>Gain(p,2,1));break;
   case 14:Q(()=>Damage(p,r==0?1:r==1?2+extra:2+extra*2));if(r==0)Q(()=>Recolor(p,1));break;
   case 15:Q(()=>Damage(p,r==0?3:r==1?2+extra*2:4+extra));break;
   case 16:
    if(r==0){Q(()=>Damage(p,1));if(extra>0)Q(()=>{other.Tokens[3]--;Step("destroy",1-p,1,"失去 1 星币");});}
    if(r==1)Q(()=>{bool has=other.Crystals>0;if(has){other.Crystals--;Step("destroy",1-p,1,"失去 1 水晶");}Damage(p,(has?1:2)+extra);});
    if(r==2)Q(()=>LoseTokens(1-p,3,n=>Damage(p,3+(n>=2?1:0))));break;
   case 17:Q(()=>Damage(p,r==0?1:r==1?2:3+(c.DrawsBefore>=2?1:0)+extra));if(r==0 && c.Crystals==0)Q(()=>Gain(p,1,1));if(r==1)Q(()=>Scry(p,2));break;
   case 18:{bool confused=other.Confused;Q(()=>Damage(p,r==0?1:r==1?2+(confused?1:0):3+(confused?1:0)));if(r!=1)Q(()=>Confuse(p));break;}
   case 19:if(r==2)Q(()=>BreakShield(p,1));Q(()=>Damage(p,r==0?1:r==1?2+extra:4));if(r==0 && c.FlipsBefore==0)Q(()=>Gain(p,2,1));break;
   case 20:Q(()=>Damage(p,r==0?1:r==1?2+(c.DiscardKinds>=3?1:0):3+(c.DiscardKinds>=4?1:0)+extra));if(r==0 && c.DiscardCount>=5)Q(()=>Gain(p,2,1));break;
  }}else{switch(a){
   case 0:
    if(r==0){Q(()=>EffectDraw(p,1));Q(()=>DiscardHand(p,1,true,n=>{if(n>0)GainDifferent(p,2);}));}
    else{Q(()=>EffectDraw(p,r==1?1:2));Q(()=>GainDifferent(p,2));}break;
   case 1:
    if(r==0)Q(()=>Gain(p,0,2));
    if(r==1){Q(()=>Mana(p,1));Q(()=>EffectDraw(p,1+extra));}
    if(r==2){Q(()=>Mana(p,3-me.Mana));Q(()=>EffectDraw(p,1));}break;
   case 2:Q(()=>Scry(p,r==2?4:2));Q(()=>EffectDraw(p,r==2?2:1));if(r>0)Q(()=>Gain(p,1,1));break;
   case 3:if(r>0)Q(()=>EffectDraw(p,r==1?1:2));if(r!=1)Q(()=>Gain(p,0,r==0?1:2));Q(()=>Gain(p,1,r==1?2:1));break;
   case 4:if(r==1)Q(()=>Gain(p,3,4));else{Q(()=>Shield(p,r==0?1:3));if(r==2)Q(()=>EffectDraw(p,1));Q(()=>Gain(p,3,1));}break;
   case 5:
    if(r==0){Q(()=>EffectDraw(p,1));if(Prior(c,5))Q(()=>Gain(p,3,1));}
    if(r==1){Q(()=>Recover(p,1,x=>x.Arcana==5 && x.Rank<2,false,false));Q(()=>Gain(p,3,2));}
    if(r==2){Q(()=>Recover(p,1,x=>x.Arcana==5 && x.Rank==0));Q(()=>Recover(p,1,x=>x.Arcana==5 && x.Rank==1));Q(()=>Gain(p,3,1));}break;
   case 6:
    if(r==0){Q(()=>EffectDraw(p,1));if(Cross(c))Q(()=>Gain(p,1,1));}
    if(r==1){Q(()=>Gain(p,0,1));Q(()=>Gain(p,1,1));Q(()=>EffectDraw(p,1));Q(()=>BottomOne(p,()=>EffectDraw(p,1)));}
    if(r==2){Q(()=>Recover(p,2,x=>x.Rank<2,true,false));Q(()=>GainDifferent(p,1));}break;
   case 7:if(r>0)Q(()=>EffectDraw(p,r==1?1:2));Q(()=>Gain(p,0,r==1?2:1));if(r!=1)Q(()=>Gain(p,2,r==0?1:2));break;
   case 8:Q(()=>Gain(p,2,r+2));if(r>0)Q(()=>Shield(p,r));break;
   case 9:
    if(r==0)Q(()=>EffectDraw(p,c.HandCount==0?2:1));
    if(r==1){Q(()=>SelectTop(p,3));Q(()=>Gain(p,1,2));}
    if(r==2)Q(()=>EffectDraw(p,Math.Max(0,4-me.Hand.Count)));break;
   case 10:
    if(r==0){Q(()=>EffectDraw(p,1));Q(()=>BottomOne(p,()=>Gain(p,3,2)));}
    if(r==1){Q(()=>DiscardHand(p,2,true,n=>EffectDraw(p,n+2)));Q(()=>Gain(p,1,1));}
    if(r==2){Q(()=>OrderBottom(p,me.Hand.ToList(),()=>{}));Q(()=>EffectDraw(p,extra>0?5:4));Q(()=>Gain(p,3,2));}break;
   case 11:
    if(r==0){Q(()=>Gain(p,3,2));if(c.HandCount<c.OtherHandCount)Q(()=>EffectDraw(p,1));}
    if(r==1){Q(()=>Gain(p,3,3));Q(()=>Shield(p,1));}
    if(r==2){Q(()=>EffectDraw(p,2+extra));Q(()=>Shield(p,c.Behind?3:2));}break;
   case 12:
    Q(()=>Retreat(p,r==2?2:1));
    if(r==0){Q(()=>Crystal(p,2));Q(()=>EffectDraw(p,1));}
    if(r==1){Q(()=>EffectDraw(p,2));Q(()=>Gain(p,3,1));Q(()=>Option(p,"是否转位？",new[]{"保持逆位","转为正位"},n=>{if(n==1)Flip(p);}));}
    if(r==2){Q(()=>Crystal(p,3));Q(()=>EffectDraw(p,2));}break;
   case 13:
    if(r==0)Q(()=>DiscardHand(p,1,false,n=>EffectDraw(p,n>0?2:1)));
    if(r==1){Q(()=>Recover(p,1,x=>x.Rank<2,false,false));Q(()=>Gain(p,1,1));Q(()=>Gain(p,2,1));}
    if(r==2)Q(()=>Recover(p,extra>0?3:2,x=>true));break;
   case 14:
    if(r==0)Q(()=>GainDifferent(p,2));
    if(r==1){Q(()=>EffectDraw(p,1));Q(()=>Crystal(p,1));Q(()=>Recolor(p,2));}
    if(r==2){Q(()=>Mana(p,1));Q(()=>EffectDraw(p,1));Q(()=>GainDifferent(p,2));}break;
   case 15:
    if(r==0)Q(()=>Gain(p,2,2));
    if(r==1){Q(()=>Crystal(p,1));Q(()=>EffectDraw(p,1));if(extra>0)Q(()=>Gain(p,2,2));}
    if(r==2){Q(()=>Crystal(p,2));Q(()=>Gain(p,2,2));Q(()=>EffectDraw(p,2));}break;
   case 16:
    if(r==0)Q(()=>Gain(p,0,2));
    if(r==1){Q(()=>Gain(p,2,4));Q(()=>BreakShield(p,1));}
    if(r==2){Q(()=>{int count=me.Crystals+other.Crystals;Step("destroy",p,me.Crystals,"失去 "+me.Crystals+" 水晶");Step("destroy",1-p,other.Crystals,"失去 "+other.Crystals+" 水晶");me.Crystals=other.Crystals=0;Gain(p,2,Math.Min(3,count));});Q(()=>EffectDraw(p,2));}break;
   case 17:
    if(r==0){Q(()=>Crystal(p,1));Q(()=>Gain(p,1,1));}
    if(r==1){Q(()=>EffectDraw(p,2));Q(()=>Gain(p,1,1));}
    if(r==2){Q(()=>EffectDraw(p,3));Q(()=>Crystal(p,1));Q(()=>Gain(p,1,1));}break;
   case 18:
    if(r==0){Q(()=>EffectDraw(p,1));Q(()=>Gain(p,1,1));}
    if(r==1){Q(()=>Shield(p,2));Q(()=>Gain(p,1,1));Q(()=>Option(p,"可消耗 1 圣杯转位",new[]{"保持逆位","消耗 1 圣杯并转位"},n=>{if(n==1){me.Tokens[1]--;Flip(p);}}));}
    if(r==2){Q(()=>Peek(p));Q(()=>Shield(p,2));Q(()=>EffectDraw(p,2));}break;
   case 19:if(r>0)Q(()=>EffectDraw(p,r==1?1:2));Q(()=>Gain(p,2,2));break;
   case 20:
    if(r==0)Q(()=>DiscardHand(p,1,false,n=>EffectDraw(p,n>0?2:1)));
    if(r==1){Q(()=>Recover(p,1,x=>x.Rank<2,false,false));Q(()=>Gain(p,2,1));Q(()=>Gain(p,1,1));}
    if(r==2){Q(()=>Recover(p,2,x=>x.Rank<2,true));if(c.DiscardKinds>=4)Q(()=>Crystal(p,1));}break;
  }}
 }
}
}
