using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;

namespace ArcanaDuel {
public static class Tests {
 static int passed;static List<string> lines=new List<string>();
 static void Check(bool yes,string name){if(!yes)throw new Exception("FAIL: "+name);passed++;lines.Add("PASS: "+name);}
 static void OK(string error,string label){Check(error==null,label+(error==null?"":" · "+error));}
 static string Act(Engine e,int p,string type,int value=0,int cry=0){return e.Apply(p,new ActionMessage{Type=type,Value=value,Crystals=cry,Revision=e.Revision});}
 public static Engine Fixture(int arc,int rank,bool up,bool prepared=false){
  var e=new Engine("Alpha","Beta",new Rules(),42){Phase="battle",Active=0,UprightPlayer=up?0:1,Position=prepared?9:10};
  var p=e.Players[0];p.Mana=3;p.Crystals=3;p.Picks.AddRange(new[]{arc,0,5,19});
  p.Hand.Add(new Card{Id=1,Arcana=arc,Rank=rank});p.Hand.Add(new Card{Id=2,Arcana=0,Rank=0});p.Hand.Add(new Card{Id=3,Arcana=1,Rank=1});
  for(int i=4;i<=20;i++){var c=new Card{Id=i,Arcana=0,Rank=(i-4)%3};if(i<=(prepared?11:6)){if(prepared){c.Arcana=new[]{5,5,0,13,20,5,0,13}[i-4];c.Rank=(i-4)%3;}p.Discard.Add(c);}else p.Deck.Add(c);}
  if(prepared){p.Tokens=new[]{3,3,3,3};p.Played.AddRange(new[]{new Card{Arcana=5,Rank=0},new Card{Arcana=5,Rank=1},new Card{Arcana=19,Rank=0}});p.DamageThisTurn=3;p.EffectDraws=2;p.Scries=1;p.VoluntaryDiscards=1;}
  var other=e.Players[1];other.Mana=0;other.Crystals=prepared?3:0;other.Tokens=prepared?new[]{3,3,3,3}:new int[4];other.Shield=prepared?1:0;other.Confused=prepared;
  for(int i=21;i<=40;i++){var c=new Card{Id=i,Arcana=19,Rank=i%3};if(i<=23)other.Hand.Add(c);else other.Deck.Add(c);}return e;
 }
 public static void Settle(Engine e,bool maximum=true,bool trim=true){
  int n=0;while(e.PendingChoice!=null){if(++n>80)throw new Exception("Choice loop");int actor=e.PendingChoice.Owner;var action=e.SuggestChoice(maximum);var err=e.Apply(actor,action);if(err!=null)throw new Exception(err);}
  if(trim)while(e.PendingTokens>=0){if(++n>100)throw new Exception("Overflow loop");int p=e.PendingTokens;Act(e,p,"discardToken",Enumerable.Range(0,4).OrderByDescending(i=>e.Players[p].Tokens[i]).First());}
 }
 static void Play(Engine e,bool max=true){var c=e.Players[e.Active].Hand[0];var rule=e.Rules.Get(c);int pay=Enumerable.Range(0,rule.Cost+1).First(x=>e.CanPlay(e.Active,c,x)==null);string err=Act(e,e.Active,"play",c.Id,pay);if(err!=null)throw new Exception(err);Settle(e,max);}
 static void Conservation(Engine e,string label){
  int total=e.Players.Sum(p=>p.Hand.Count+p.Deck.Count+p.Discard.Count)+e.ResolvingCount;
  var cards=e.Players.SelectMany(p=>p.Hand.Concat(p.Deck).Concat(p.Discard)).ToList();
  Check(total==40 && cards.Select(c=>c.Id).Distinct().Count()==cards.Count,label+" card conservation");
  Check(e.Players.All(p=>p.Hand.Count<=5 && p.Mana>=0 && p.Mana<=3 && p.Crystals>=0 && p.Crystals<=3 && p.Shield>=0 && p.Shield<=3 && p.Tokens.All(t=>t>=0) && p.Tokens.Sum()<=13),label+" resource bounds");
 }
 static Engine Draft(){var e=new Engine("Alpha","Beta",new Rules(),31);for(int i=0;i<10;i++){int expected=Engine.DraftActors[i]==0?e.First:1-e.First;Check(e.Active==expected,"draft actor "+i);Check(Act(e,1-e.Active,"draft",e.Available[0])!=null,"draft rejects wrong actor "+i);OK(Act(e,e.Active,"draft",e.Available[0]),"draft accepts step "+i);}return e;}
 public static int Run(){
  passed=0;lines.Clear();try{
   var rules=new Rules();Check(rules.Definitions.Length==63,"63 distinct v0.2 card definitions");Check(rules.Definitions.Select(c=>c.Name).Distinct().Count()==63,"63 unique card names");Check(rules.Definitions.All(c=>!String.IsNullOrEmpty(c.Upright) && !String.IsNullOrEmpty(c.Reversed)),"126 nonempty card effects");
   var e=Draft();Check(e.Phase=="battle" && e.Banned.Count==4,"draft enters battle after four bans");Check(e.Position==10 && Engine.RoadLength==21,"21 cells and ten spaces to each goal");
   Check(e.Players.All(p=>p.Picks.Count==4 && p.Picks.Contains(0) && p.Hand.Count==5 && p.Deck.Count==15),"four decks including Fool, 20 cards, opening five");
   Check(e.Players[0].Picks.Intersect(e.Players[1].Picks).SequenceEqual(new[]{0}),"exclusive major picks");
   Check(e.Players.All(p=>p.Hand.Concat(p.Deck).GroupBy(c=>c.Arcana).All(g=>g.Count(c=>c.Rank==0)==2 && g.Count(c=>c.Rank==1)==2 && g.Count(c=>c.Rank==2)==1)),"AABBC in every drafted deck");
   int actor=e.Active;OK(Act(e,actor,"end"),"end turn");Check(e.Players[actor].Crystals==3 && e.Players[actor].Mana==0 && e.Players[e.Active].Hand.Count==5 && e.Players[e.Active].Discard.Count==1,"bank cap and start draw overflow");
   Check(e.Apply(e.Active,new ActionMessage{Type="end",Revision=e.Revision-1})!=null,"stale revision rejected");

   // Every face is exercised both with no optional investment and with prepared synergies.
   for(int a=0;a<21;a++)for(int r=0;r<3;r++)foreach(bool up in new[]{true,false})foreach(bool prepared in new[]{false,true}){
    e=Fixture(a,r,up,prepared);Play(e,prepared);string name=Engine.Names[a]+" "+r+" "+(up?"up":"reversed")+" prepared="+prepared;
    Check(e.PendingChoice==null && e.ResolvingCount==0,name+" completes");Conservation(e,name);
    Check(e.Players[0].Discard.Any(c=>c.Id==1),name+" resolving card enters discard");
   }
   // Baseline damage oracle, independent of the resolver's switch implementation.
   int[][] damage={new[]{1,2,3},new[]{1,2,2},new[]{1,2,3},new[]{1,0,3},new[]{1,2,3},new[]{1,2,3},new[]{1,2,3},new[]{1,2,3},new[]{1,2,3},new[]{1,2,3},new[]{1,3,3},new[]{1,2,3},new[]{1,2,2},new[]{1,2,3},new[]{1,2,2},new[]{3,2,7},new[]{1,2,3},new[]{1,2,3},new[]{1,2,3},new[]{1,2,4},new[]{1,2,3}};
   for(int a=0;a<21;a++)for(int r=0;r<3;r++){e=Fixture(a,r,true);Play(e,false);Check(e.Position==10+damage[a][r],"damage oracle "+Engine.Names[a]+" "+r);}
   int[][] preparedDamage={new[]{0,1,3},new[]{0,2,3},new[]{0,2,4},new[]{0,0,5},new[]{0,1,4},new[]{0,2,4},new[]{0,2,5},new[]{0,2,4},new[]{1,2,5},new[]{0,1,3},new[]{1,2,4},new[]{0,2,2},new[]{0,2,4},new[]{1,2,4},new[]{0,2,3},new[]{2,3,6},new[]{0,1,3},new[]{0,1,4},new[]{0,2,3},new[]{0,2,4},new[]{0,2,4}};
   for(int a=0;a<21;a++)for(int r=0;r<3;r++){e=Fixture(a,r,true,true);Play(e,true);Check(e.Position==9+preparedDamage[a][r],"prepared damage oracle "+Engine.Names[a]+" "+r);}
   string[] draws={"112","011","112","012","001","100","110","012","000","102","124","102","122","200","011","012","002","023","102","012","200"};
   string[] hands={"334","233","334","234","223","322","333","234","222","334","344","324","344","332","233","233","224","245","324","234","332"};
   string[][] tokens={new[]{"0000","1100","1100"},new[]{"2000","0000","0000"},new[]{"0000","0100","0100"},new[]{"1100","0200","2100"},new[]{"0001","0004","0001"},new[]{"0000","0002","0001"},new[]{"0000","1100","1000"},new[]{"1010","2000","1020"},new[]{"0020","0030","0040"},new[]{"0000","0200","0000"},new[]{"0000","0100","0002"},new[]{"0002","0003","0000"},new[]{"0000","0001","0000"},new[]{"0000","0110","0000"},new[]{"1100","0000","1100"},new[]{"0020","0000","0020"},new[]{"2000","0040","0030"},new[]{"0100","0100","0100"},new[]{"0100","0100","0000"},new[]{"0020","0020","0020"},new[]{"0000","0110","0000"}};
   for(int a=0;a<21;a++)for(int r=0;r<3;r++){
    e=Fixture(a,r,false);Play(e,false);var p=e.Players[0];string label=Engine.Names[a]+" "+r;
    Check(p.EffectDraws==draws[a][r]-'0' && p.Hand.Count==hands[a][r]-'0',"reversed draw/hand oracle "+label);
    Check(String.Join("",p.Tokens.Select(n=>n.ToString()))==tokens[a][r],"reversed token-suit oracle "+label);
    int shield=a==4?(r==0?1:r==2?3:0):a==8?r:a==11?(r==1?1:r==2?2:0):a==18?(r>0?2:0):0;
    Check(p.Shield==shield && e.Position==(a==12?(r==2?8:9):10),"reversed shield/road oracle "+label);
   }

   e=Fixture(1,2,false);e.Players[0].Mana=0;e.Players[0].Crystals=2;Play(e,false);Check(e.Players[0].Mana==3 && e.Players[0].Crystals==0 && e.Players[0].Hand.Count==3,"Magician C fills to three: net +1 resource, cycles card");
   e=Fixture(1,2,false);e.Players[0].Mana=2;e.Players[0].Crystals=2;Play(e,false);Check(e.Players[0].Mana==3,"Magician fill respects existing mana");
   e=Fixture(14,2,false);e.Players[0].Mana=0;e.Players[0].Crystals=2;Play(e,false);Check(e.Players[0].Mana==1 && e.Players[0].Tokens.Sum()==2,"Temperance C refund and two distinct tokens");
   e=Fixture(12,0,false);e.Players[0].Crystals=0;Play(e,false);Check(e.Position==9 && e.Players[0].Mana==2 && e.Players[0].Crystals==2 && e.Players[0].Hand.Count==3,"Hanged A pays retreat and grants two crystals plus cycle");
   e=Fixture(12,2,false);e.Players[0].Crystals=0;Play(e,false);Check(e.Position==8 && e.Players[0].Mana==1 && e.Players[0].Crystals==3 && e.Players[0].Hand.Count==4,"Hanged C retreat two, three crystals, draw two");
   e=Fixture(12,2,false);e.Position=1;e.Players[0].Crystals=0;e.Players[0].Shield=3;Play(e,false);Check(e.Winner==1 && e.Phase=="ended" && e.Players[0].Crystals==0 && e.Players[0].Hand.Count==2 && e.Players[0].Shield==3,"retreat ignores shield; terminal loss stops rewards");
   e=Fixture(19,0,true);e.Position=19;Play(e,false);Check(e.Winner==0 && e.Players[0].Tokens[2]==0,"terminal damage stops later token reward");
   e=Fixture(8,2,true,true);Play(e,true);Check(e.Position==14 && e.Players[0].Tokens[2]==0 && e.Players[1].Shield==0,"Strength C six damage less one shield, pays three swords");
   e=Fixture(3,2,true,true);Play(e,true);Check(e.Position==14 && e.Players[0].Tokens[0]==1 && e.Players[0].Tokens[1]==1,"Empress C optional four tokens pays for six damage");
   e=Fixture(15,2,true);Play(e,false);Check(e.Position==17 && e.Players[0].Crystals==0 && e.Players[0].Mana==0,"Devil C consumes all three remaining crystals for seven");
   e=Fixture(15,0,true);e.Players[0].Hand.RemoveRange(1,2);string snapshot=Room.Json(e.View(0,true));Check(Act(e,0,"play",1)!=null && snapshot==Room.Json(e.View(0,true)),"mandatory discard rejected atomically");
   e=Fixture(15,2,true);snapshot=Room.Json(e.View(0,true));Check(Act(e,0,"play",1,3)!=null && snapshot==Room.Json(e.View(0,true)),"Devil C rejects fixed payment leaving zero crystals");
   e=Fixture(10,1,false);Play(e,true);Check(e.Players[0].Hand.Count==4 && e.Players[0].EffectDraws==4,"Wheel B discard two draw four: net one hand card");
   e=Fixture(15,2,false);Play(e,false);Check(e.Players[0].Hand.Count==3 && e.Players[0].Tokens[2]==2,"Devil C extra discard plus draw two keeps hand count");
   e=Fixture(17,0,false);OK(Act(e,0,"play",1,1),"Star A can use crystal");Check(e.Players[0].Crystals==3 && e.Players[0].Tokens[1]==1 && e.Players[0].Hand.Count==2,"Star A equal refund gains cup and consumes a card");

   ChoiceAndPrivacyChecks();StatusChecks();TurnConditions();
   int actions=0;
   for(int seed=0;seed<35;seed++){
    e=new Engine("A","B",new Rules(),seed);var rng=new Random(seed+902);
    for(int n=0;n<2500 && e.Phase!="ended";n++){
     actor=e.PendingChoice!=null?e.PendingChoice.Owner:e.PendingTokens>=0?e.PendingTokens:e.Active;var next=e.SuggestAction(actor,rng);if(next==null)throw new Exception("Bot stalled");string error=e.Apply(actor,next);if(error!=null)throw new Exception("Bot illegal: "+error);
     if(e.Players.Any(p=>p.Hand.Count>5 || p.Mana<0 || p.Mana>3 || p.Crystals<0 || p.Crystals>3 || p.Tokens.Any(t=>t<0)))throw new Exception("Simulation resource invariant");
     if(e.Players.Sum(p=>p.Hand.Count+p.Deck.Count+p.Discard.Count)+e.ResolvingCount!=(e.Phase=="draft"?0:40))throw new Exception("Simulation conservation");actions++;
    }
    if(e.Phase!="ended")File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"long-match-"+seed+".json"),Room.Json(e.View(0,true)));
    Check(e.Phase=="ended","complete v0.2 simulated match "+seed);Conservation(e,"match "+seed);
   }
   NetworkChecks();lines.Add("TOTAL: "+passed+" passed; 252 face scenarios; "+actions+" complete-match actions.");Save();return 0;
  }catch(Exception ex){lines.Add(ex.ToString());Save();return 1;}
 }
 static void Save(){File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results.txt"),lines);}
 static void ChoiceAndPrivacyChecks(){
  var e=Fixture(8,2,true,true);int mana=e.Players[0].Mana;OK(Act(e,0,"play",1),"declare optional cost");Check(e.PendingChoice!=null && e.PendingChoice.Preparing && e.Players[0].Mana==mana && e.Players[0].Hand.Count==3,"no payment before extra cost confirmation");
  Check(e.View(1,false).Choice==null && e.View(1,false).Event.Card==null,"uncommitted card and cost choices private");
  string before=Room.Json(e.View(0,true));var invalid=e.SuggestChoice();invalid.Values=new[]{999999};Check(e.Apply(0,invalid)!=null && before==Room.Json(e.View(0,true)),"forged choice rejected atomically");
  Check(e.Apply(1,e.SuggestChoice())!=null,"wrong choice owner rejected");OK(Act(e,0,"cancel"),"cancel uncommitted spell");Check(e.Players[0].Mana==mana && e.Players[0].Hand.Count==3 && e.PendingChoice==null,"cancel restores untouched state");
  e=Fixture(2,0,false);var first=e.Players[0].Deck.Take(2).ToList();OK(Act(e,0,"play",1),"Priestess begins private scry");Check(e.PendingChoice.Kind=="scry" && !e.PendingChoice.Preparing && e.View(1,false).Choice==null,"scry payload only in entitled snapshot");
  Check(e.View(1,false).Event.Card.Id==1 && e.View(1,false).ResolvingCard.Id==1,"opponent sees the committed card while waiting");
  before=Room.Json(e.View(0,true));Check(Act(e,0,"cancel")!=null && before==Room.Json(e.View(0,true)),"cannot cancel committed spell");
  var ch=e.PendingChoice;OK(e.Apply(0,new ActionMessage{Type="choose",Revision=e.Revision,ChoiceId=ch.Id,Values=new[]{first[1].Id,first[0].Id},Split=1}),"scry reorders and divides top from bottom");
  Check(e.Players[0].Hand.Any(c=>c.Id==first[1].Id) && e.Players[0].Deck.Last().Id==first[0].Id && e.Players[0].EffectDraws==1,"scry order controls next draw; bottom preserved");
  e=Fixture(18,2,false);OK(Act(e,0,"play",1,3),"Moon C enters authorized hand inspection");Check(e.View(0,false).Choice.Options.Count==3 && e.View(0,false).Choice.Options.All(o=>o.Card!=null) && e.View(1,false).Choice==null,"inspection list delivered only to caster");Settle(e,false);Check(e.View(0,false).Choice==null && e.View(0,false).Players[1].Hand.Count==0,"hand inspection closes without permanent exposure");
  e=Fixture(16,2,true,true);OK(Act(e,0,"play",1),"Tower C demands opponent resource choice");Check(e.PendingChoice.Owner==1 && e.View(0,false).Choice==null && e.View(1,false).Choice!=null,"opponent selects destroyed tokens off turn");Settle(e,true);Check(e.Active==0 && e.Players[1].Tokens.Sum()==9,"caster retains turn after opponent choice");
  e=Fixture(14,2,false,true);OK(Act(e,0,"play",1,2),"Temperance resource generation");Settle(e,true,false);Check(e.PendingTokens==0 && e.Players[0].Tokens.Sum()==14,"overflow waits until complete card resolution");Check(Act(e,0,"end")!=null,"overflow blocks ending turn");Settle(e,true);Check(e.Players[0].Tokens.Sum()==13,"owner discards down to thirteen");
  e=Fixture(13,0,false);var card=e.Players[0].Hand[0];e.Players[0].Hand=new List<Card>{card};e.Players[0].Deck.Clear();e.Players[0].Discard.Clear();Play(e,false);Check(e.Players[0].Hand.Count==0 && e.Players[0].Discard.Single().Id==1,"resolving card cannot draw itself from empty zones");
  e=Fixture(13,2,false);e.Players[0].Discard.Clear();OK(Act(e,0,"play",1,3),"Death C with no recovery targets");Check(e.PendingChoice==null && e.Players[0].Discard.Single().Id==1,"resolving card cannot recover itself");
 }
 static void StatusChecks(){
  var e=Fixture(19,2,true);e.Players[1].Shield=3;Play(e,false);Check(e.Position==12 && e.Players[1].Shield==0 && e.Players[0].DamageThisTurn==2,"Sun C clears one shield before damage, tracks unblocked damage");
  e=Fixture(4,2,false);e.Players[0].Shield=2;Play(e,false);Check(e.Players[0].Shield==3,"shield cap three");Act(e,0,"end");Check(e.Players[0].Shield==3,"shield persists through opponent turn");Act(e,1,"end");Check(e.Players[0].Shield==0,"shield expires at owner's next turn start");
  e=Fixture(18,0,true);Play(e,false);Check(e.Players[1].Confused,"Moon A applies confusion");Act(e,0,"end");e.Players[1].Mana=1;e.Players[1].Crystals=1;OK(Act(e,1,"flip",0,1),"confusion supports mixed flip payment");Check(!e.Players[1].Confused && e.Players[1].Mana==0 && e.Players[1].Crystals==0,"first generic flip consumes surcharge once");
  e=Fixture(18,1,false);e.Players[0].Confused=true;Play(e,true);Check(e.Upright(0) && e.Players[0].Confused && e.Players[0].Tokens[1]==0 && e.Players[0].Mana==1,"Moon B spends newly gained cup, bypasses confusion and retains tax");
  Act(e,0,"end");Check(!e.Players[0].Confused,"unused confusion expires at affected turn end");
  e=Fixture(16,2,false);e.Players[1].Crystals=2;Play(e,false);Check(e.Players[0].Crystals==0 && e.Players[1].Crystals==0 && e.Players[0].Tokens[2]==3 && e.Players[0].EffectDraws==2,"Tower symmetric clear counts actual losses, sword cap three");
 }
 static void TurnConditions(){
  var e=Fixture(4,0,true);e.Position=10;Play(e,false);Check(e.Position==11 && e.Players[0].Tokens[3]==0,"advantage checked before current damage");
  e=Fixture(4,0,true);e.Position=11;Play(e,false);Check(e.Players[0].Tokens[3]==1,"existing advantage awards Emperor token");
  e=Fixture(7,2,true);e.Players[0].DamageThisTurn=3;e.Players[0].Tokens[2]=1;Play(e,true);Check(e.Position==15,"Chariot prior actual damage plus optional sword");
  e=Fixture(20,2,true,true);Play(e,true);Check(e.Position==13,"Judgement four discard arcana plus one optional token");
  e=Fixture(11,2,true);e.Players[1].Tokens[0]=3;Play(e,false);Check(e.Position==15,"Justice checks three-token deficit");
  e=Fixture(9,0,false);e.Players[0].Hand.RemoveRange(1,2);Play(e,false);Check(e.Players[0].Hand.Count==2,"Hermit empty-hand condition excludes current card");
  e=Fixture(19,0,true);e.Players[0].OwnFlips=1;Play(e,false);Check(e.Players[0].Tokens[2]==0,"Sun no-flip bonus counts owner's flip actions");
  e=Fixture(17,2,true);e.Players[0].EffectDraws=2;e.Players[0].Tokens[1]=1;Play(e,true);Check(e.Position==15,"Star completed draw setup discounts optional cup");
  e=Fixture(2,0,true);Play(e,false);Check(e.Players[0].Scries==1 && e.Players[0].EffectDraws==0,"scry counts as preparation but not effect draw");
  e=Fixture(13,1,false);Play(e,false);Check(e.Players[0].EffectDraws==0,"recovery is not effect draw");
  e=Fixture(5,2,true,true);Play(e,true);Check(e.Position==13,"Hierophant ritual sees earlier A and B; five damage minus shield");
 }
 static void NetworkChecks(){
  using(var host=new Room())using(var guest=new Room()){
   var connected=new ManualResetEvent(false);var finished=new ManualResetEvent(false);var lost=new ManualResetEvent(false);string error=null;var e=Fixture(2,0,false);var swap=e.Players[0];e.Players[0]=e.Players[1];e.Players[1]=swap;e.Active=1;e.UprightPlayer=0;Act(e,1,"play",1);
   bool privateChoice=false,hiddenHand=false;host.Connected+=name=>{connected.Set();host.Send(new Packet{Kind="state",Payload=Room.Json(e.View(1,false))});};
   guest.Received+=p=>{try{if(p.Kind!="state")return;var view=Room.Parse<Snapshot>(p.Payload);hiddenHand=view.Players[0].Hand.Count==0;if(view.Choice!=null){privateChoice=view.Choice.Kind=="scry";guest.Send(new Packet{Kind="action",Payload=Room.Json(new ActionMessage{Type="choose",Revision=view.Revision,ChoiceId=view.Choice.Id,Values=view.Choice.Options.Select(o=>o.Id).Reverse().ToArray(),Split=1})});}else finished.Set();}catch(Exception ex){error=ex.ToString();finished.Set();}};
   host.Received+=p=>{try{if(p.Kind=="action"){error=e.Apply(1,Room.Parse<ActionMessage>(p.Payload));host.Send(new Packet{Kind="state",Payload=Room.Json(e.View(1,false))});}}catch(Exception ex){error=ex.ToString();finished.Set();}};
   host.Failed+=s=>lost.Set();guest.Failed+=s=>{error=s;finished.Set();};host.Host("123456","Host");guest.Join("127.0.0.1","123456","Guest");
   Check(connected.WaitOne(7000),"TCP protocol v3 handshake");Check(finished.WaitOne(5000) && error==null && privateChoice,"TCP authorized private scry request and response");Check(hiddenHand && e.View(0,false).Players[1].Hand.Count==0,"TCP full effect snapshots hide other hand");Check(e.PendingChoice==null && e.Players[1].EffectDraws==1,"TCP choice resolves authoritative draw");guest.Dispose();Check(lost.WaitOne(4000),"TCP disconnect detected");
  }
  using(var host=new Room())using(var guest=new Room()){var rejected=new ManualResetEvent(false);bool accepted=false;host.Connected+=s=>accepted=true;guest.Failed+=s=>rejected.Set();host.Host("123456","Host");guest.Join("127.0.0.1","999999","Guest");Check(rejected.WaitOne(7000) && !accepted,"wrong room code rejected");}
 }
}
}
