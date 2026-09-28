using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcanaDuel {
public partial class Engine {
 public ActionMessage SuggestChoice(bool maximize=true){
  var c=PendingChoice;if(c==null)return null;
  int[] selected=null;int split=0;
  if(c.Kind=="inspect")selected=new int[0];
  else if(c.Kind=="scry" || c.Kind=="order"){
   selected=c.Options.OrderByDescending(o=>o.Card==null?0:o.Card.Rank).Select(o=>o.Id).ToArray();split=selected.Length;
  }else if(c.Kind=="option")selected=new[]{maximize?c.Options.Last().Id:c.Options.First().Id};
  else{
   var ids=c.Options.OrderByDescending(o=>o.Card==null?0:o.Card.Rank).Select(o=>o.Id).ToArray();
   var counts=Enumerable.Range(c.Min,c.Max-c.Min+1);if(maximize)counts=counts.Reverse();
   foreach(int n in counts){selected=FindChoice(ids,n,0,new List<int>());if(selected!=null)break;}
  }
  if(selected==null)throw new InvalidOperationException("No valid choice: "+c.Title);
  return new ActionMessage{Type="choose",Revision=Revision,ChoiceId=c.Id,Values=selected,Split=split};
 }
 int[] FindChoice(int[] ids,int count,int start,List<int> chosen){
  if(chosen.Count==count){var result=chosen.ToArray();return validateAnswer==null || validateAnswer(result,0)==null?result:null;}
  for(int i=start;i<=ids.Length-(count-chosen.Count);i++){chosen.Add(ids[i]);var result=FindChoice(ids,count,i+1,chosen);chosen.RemoveAt(chosen.Count-1);if(result!=null)return result;}return null;
 }
 public ActionMessage SuggestAction(int actor,Random rng){
  if(Phase=="ended")return null;
  if(PendingChoice!=null)return PendingChoice.Owner==actor?SuggestChoice():null;
  if(PendingTokens>=0)return PendingTokens==actor?new ActionMessage{Type="discardToken",Value=Enumerable.Range(0,4).OrderByDescending(i=>Players[actor].Tokens[i]).First(),Revision=Revision}:null;
  if(Active!=actor)return null;
  if(Phase=="draft")return new ActionMessage{Type="draft",Value=Available[rng.Next(Available.Count)],Revision=Revision};
  var p=Players[actor];bool up=Upright(actor);int total=p.Mana+p.Crystals;
  bool wantFlip=!up && p.Hand.Count>=3 && (p.Tokens.Sum()>=2 || p.EffectDraws>=1 || p.OwnFlips==0) || up && p.Hand.Count<=2 && p.OwnFlips==0;
  int flipCost=p.Confused?2:1;
  if(wantFlip && p.OwnFlips<2 && total>=flipCost+1){int cry=Math.Max(0,flipCost-p.Mana);return new ActionMessage{Type="flip",Crystals=cry,Revision=Revision};}
  var playable=new List<ActionMessage>();
  foreach(var card in p.Hand.OrderByDescending(x=>x.Rank).ThenBy(x=>rng.Next())){
   var rule=Rules.Get(card);
   for(int cry=0;cry<=rule.Cost;cry++)if(CanPlay(actor,card,cry)==null){
    if(!up && card.Arcana==1 && card.Rank==2 && p.Mana>=2)continue;
    if(!up && card.Arcana==12 && (actor==0?Position:RoadEnd-Position)<=(card.Rank==2?2:1))continue;
    playable.Add(new ActionMessage{Type="play",Value=card.Id,Crystals=cry,Revision=Revision});break;
   }
  }
  if(playable.Count>0)return playable[0];
  return new ActionMessage{Type="end",Revision=Revision};
 }
}
}
