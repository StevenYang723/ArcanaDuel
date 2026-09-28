using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web.Script.Serialization;

namespace ArcanaDuel {
public class Card { public int Id; public int Arcana; public int Rank; }
public class CardRule { public int Arcana; public int Rank; public string Name; public int Cost; public string Payment; public string Upright; public string Reversed; }
public class Rules {
 public string Version="0.2";
 public CardRule[] Definitions=CardCatalog.Create();
 public CardRule Get(Card c){return Definitions[c.Arcana*3+c.Rank];}
 public static Rules Load(string path){
  if(!File.Exists(path))return new Rules();
  string json=File.ReadAllText(path);
  if(!json.Contains("Definitions"))return new Rules();
  var r=new JavaScriptSerializer{MaxJsonLength=524288}.Deserialize<Rules>(json);
  if(r==null || r.Version!="0.2" || r.Definitions==null || r.Definitions.Length!=63)throw new Exception("cards.json 必须包含 v0.2 的 63 种卡牌。");
  var canonical=CardCatalog.Create();
  for(int i=0;i<63;i++){
   var c=r.Definitions[i];var d=canonical[i];
   if(c==null || c.Arcana!=i/3 || c.Rank!=i%3 || c.Cost!=d.Cost || c.Payment!=d.Payment || c.Name!=d.Name || c.Upright!=d.Upright || c.Reversed!=d.Reversed)throw new Exception("cards.json 与 v0.2 规则不一致，请恢复随附配置："+d.Name);
  }
  return r;
 }
}
public class Player {
 public string Name; public List<int> Picks=new List<int>(); public List<Card> Hand=new List<Card>();
 public List<Card> Deck=new List<Card>(); public List<Card> Discard=new List<Card>();
 public int Mana; public int Crystals; public int[] Tokens=new int[4]; public int Shield; public bool Confused;
 public List<Card> Played=new List<Card>(); public int DamageThisTurn; public int EffectDraws; public int Scries; public int OwnFlips; public int VoluntaryDiscards;
 public void ResetTurn(){Played.Clear();DamageThisTurn=EffectDraws=Scries=OwnFlips=VoluntaryDiscards=0;Shield=0;Mana=3;}
}
public class EffectEvent { public string Kind; public int Actor; public int Amount; public int Before; public int After; public string Text; public List<Card> Cards; }
public class Event {
 public int Seq; public string Type; public int Actor; public Card Card; public bool Upright; public int Amount; public int Before; public int After; public string Text;
 public List<EffectEvent> Steps=new List<EffectEvent>();
}
public class ChoiceOption { public int Id; public string Text; public Card Card; public int Suit=-1; }
public class Choice {
 public int Id; public int Owner; public string Title; public string Kind; public int Min; public int Max; public bool Preparing;
 public Card Source; public bool Upright;
 public List<ChoiceOption> Options=new List<ChoiceOption>();
}
public class ActionMessage { public string Type; public int Value; public int Crystals; public int Revision; public int ChoiceId; public int[] Values; public int Split; }
public class ViewPlayer {
 public string Name; public List<int> Picks; public List<Card> Hand; public int HandCount; public int DeckCount; public List<Card> Discard;
 public int Mana; public int Crystals; public int[] Tokens; public int Shield; public bool Confused;
}
public class Snapshot {
 public string Phase; public int Revision; public int First; public int Active; public int DraftIndex; public int Position; public int UprightPlayer; public int Turn; public int Winner=-1;
 public List<int> Available; public List<int> Banned; public ViewPlayer[] Players; public List<string> Log; public Event Event; public Rules Rules; public int PendingTokens=-1;
 public Choice Choice; public int ChoiceOwner=-1; public Card ResolvingCard; public int ResolvingActor=-1; public bool ResolvingUpright;
}
class PlayContext {
 public int Actor; public Card Card; public bool Up; public int CrystalPay; public int[] ExtraTokens=new int[4]; public int ExtraCrystals;
 public List<int> Discards=new List<int>(); public int Extra; public bool Committed;
 public int HandCount; public int OtherHandCount; public int Crystals; public int OtherCrystals; public int Tokens; public int OtherTokens; public int DiscardCount; public int DiscardKinds;
 public bool Advantage; public bool Behind; public List<Card> Previous; public int DamageBefore; public int DrawsBefore; public int ScryBefore; public int FlipsBefore; public int DiscardsBefore;
}
public partial class Engine {
 public const int RoadLength=21; public const int RoadCenter=10; public const int RoadEnd=20;
 public static readonly string[] Names={"愚者","魔术师","女祭司","皇后","皇帝","教皇","恋人","战车","力量","隐者","命运之轮","正义","倒吊人","死神","节制","恶魔","高塔","星星","月亮","太阳","审判"};
 public static readonly string[] English={"THE FOOL","THE MAGICIAN","THE HIGH PRIESTESS","THE EMPRESS","THE EMPEROR","THE HIEROPHANT","THE LOVERS","THE CHARIOT","STRENGTH","THE HERMIT","WHEEL OF FORTUNE","JUSTICE","THE HANGED MAN","DEATH","TEMPERANCE","THE DEVIL","THE TOWER","THE STAR","THE MOON","THE SUN","JUDGEMENT"};
 public static readonly string[] Romans={"0","I","II","III","IV","V","VI","VII","VIII","IX","X","XI","XII","XIII","XIV","XV","XVI","XVII","XVIII","XIX","XX"};
 public static readonly string[] Suits={"权杖","圣杯","宝剑","星币"};
 public static readonly int[] DraftActors={0,1,0,1,1,0,1,0,0,1};
 public static readonly bool[] DraftBan={true,true,false,false,false,true,true,false,false,false};
 public Player[] Players; public string Phase="draft"; public int First; public int Active; public int DraftIndex; public int Position=RoadCenter; public int UprightPlayer; public int Turn=1; public int Winner=-1; public int Revision; public int PendingTokens=-1;
 public List<int> Available=Enumerable.Range(1,20).ToList(); public List<int> Banned=new List<int>(); public List<string> Log=new List<string>(); public Event LastEvent; public Rules Rules;
 public Choice PendingChoice {get;private set;}
 public int ResolvingCount {get{return context!=null && context.Committed?1:0;}}
 Random random; int serial; int choiceSerial; PlayContext context; Queue<Action> steps=new Queue<Action>();
 Action<int[],int> answer; Func<int[],int,string> validateAnswer;
 public Engine(string p0,string p1,Rules rules,int seed){Rules=rules;random=new Random(seed);Players=new[]{new Player{Name=p0},new Player{Name=p1}};First=random.Next(2);Active=First;UprightPlayer=First;Note(Players[First].Name+" 掷星为先手，首先禁用一副牌组。");}
 void Note(string t){Log.Add(t);if(Log.Count>50)Log.RemoveAt(0);}
 void BeginEvent(string type,int actor){LastEvent=new Event{Type=type,Actor=actor,Before=Position,After=Position};}
 void Publish(string text){Revision++;LastEvent.Seq=Revision;LastEvent.After=Position;LastEvent.Text=text;if(!String.IsNullOrEmpty(text))Note(text);}
 void Step(string kind,int actor,int amount,string text,int before=-1,List<Card> cards=null){
  if(LastEvent!=null)LastEvent.Steps.Add(new EffectEvent{Kind=kind,Actor=actor,Amount=amount,Text=text,Before=before<0?Position:before,After=Position,Cards=cards});
  Note(Players[actor].Name+" · "+text);
 }
 public bool Upright(int p){return UprightPlayer==p;}
 public void Shuffle(List<Card> cards){for(int i=cards.Count-1;i>0;i--){int j=random.Next(i+1);var c=cards[i];cards[i]=cards[j];cards[j]=c;}}
 public void Draw(int p,int count){DrawInternal(p,count,false);}
 int DrawInternal(int p,int count,bool effect){
  var x=Players[p];int actual=0;
  for(int i=0;i<count;i++){
   if(x.Deck.Count==0){if(x.Discard.Count==0)break;x.Deck.AddRange(x.Discard);x.Discard.Clear();Shuffle(x.Deck);Note(x.Name+" 将弃牌区洗回牌库。");}
   var c=x.Deck[0];x.Deck.RemoveAt(0);AddHand(p,c);actual++;
  }
  if(effect)x.EffectDraws+=actual;return actual;
 }
 void AddHand(int p,Card c){if(Players[p].Hand.Count<5)Players[p].Hand.Add(c);else{Players[p].Discard.Add(c);Note(Players[p].Name+" 新加入的牌超出手牌上限，进入弃牌区。");}}
 void Start(){
  for(int p=0;p<2;p++){Players[p].Picks.Add(0);foreach(int a in Players[p].Picks)foreach(int r in new[]{0,0,1,1,2})Players[p].Deck.Add(new Card{Id=++serial,Arcana=a,Rank=r});Shuffle(Players[p].Deck);Draw(p,5);}
  Phase="battle";Active=First;Players[Active].Mana=3;Note("双方各 20 张牌；v0.2 独立牌面。先手正位，首回合不额外抽牌。");
 }
 public string CanPlay(int actor,Card c,int crystals){
  if(c==null || !Players[actor].Hand.Contains(c))return "手牌不存在。";
  var p=Players[actor];var rule=Rules.Get(c);int mana=rule.Cost-crystals;
  if(crystals<0 || mana<0 || crystals>p.Crystals || mana>p.Mana)return "无法按此方式支付费用。";
  if(rule.Payment=="mana" && crystals!=0 || rule.Payment=="crystal" && mana!=0)return "不符合此牌的支付限制。";
  bool up=Upright(actor);
  if(c.Arcana==15 && c.Rank==2 && up && p.Crystals-crystals<1)return "灵魂抵押须在支付固定费用后至少剩余 1 水晶。";
  if(c.Arcana==15 && (c.Rank==0 && up || c.Rank==2 && !up) && p.Hand.Count<2)return "此效果必须额外弃置另一张手牌。";
  return null;
 }
 public string Apply(int actor,ActionMessage a){
  if(a==null || actor<0 || actor>1)return "无效操作。";
  if(a.Revision!=Revision)return "局面已更新，请重试。";
  if(Phase=="ended")return "本局已经结束。";
  if(a.Type=="surrender"){
   BeginEvent("win",actor);FinishPending();Winner=1-actor;Phase="ended";Publish(Players[actor].Name+" 投降。");return null;
  }
  if(PendingChoice!=null){
   if(actor!=PendingChoice.Owner)return "等待另一位玩家选择。";
   if(a.Type=="cancel" && PendingChoice.Preparing){BeginEvent("cancel",actor);steps.Clear();context=null;PendingChoice=null;answer=null;validateAnswer=null;Publish("取消施放，尚未支付任何代价。");return null;}
   if(a.Type!="choose" || a.ChoiceId!=PendingChoice.Id)return "请先完成当前选择。";
   int[] ids=a.Values??new int[0];var ch=PendingChoice;
   if(ids.Length<ch.Min || ids.Length>ch.Max || ids.Distinct().Count()!=ids.Length || ids.Any(id=>!ch.Options.Any(o=>o.Id==id)))return "选择数量或目标无效。";
   if(ch.Kind=="scry" && (a.Split<0 || a.Split>ids.Length))return "牌顶数量无效。";
   string err=validateAnswer==null?null:validateAnswer(ids,a.Split);if(err!=null)return err;
   BeginEvent("resolve",context==null?actor:context.Actor);
   if(context!=null && context.Committed){LastEvent.Card=context.Card;LastEvent.Upright=context.Up;}
   var callback=answer;PendingChoice=null;answer=null;validateAnswer=null;callback(ids,a.Split);RunSteps();
   Publish(LastEvent.Type=="play"?CastText():"选择已结算。");return null;
  }
  if(PendingTokens>=0){
   if(actor!=PendingTokens || a.Type!="discardToken" || a.Value<0 || a.Value>3 || Players[actor].Tokens[a.Value]==0)return "请先选择弃置超出上限的小阿尔卡纳。";
   BeginEvent("token",actor);Players[actor].Tokens[a.Value]--;CheckOverflow();Publish("弃置 1 个"+Suits[a.Value]+"。");return null;
  }
  if(actor!=Active)return "请等待另一位玩家。";
  if(Phase=="draft"){
   if(a.Type!="draft" || !Available.Contains(a.Value))return "这副牌组不可选择。";
   BeginEvent("draft",actor);bool ban=DraftBan[DraftIndex];Available.Remove(a.Value);if(ban)Banned.Add(a.Value);else Players[actor].Picks.Add(a.Value);
   string message=Players[actor].Name+(ban?" 禁用 ":" 选择 ")+Names[a.Value];DraftIndex++;
   if(DraftIndex==10)Start();else Active=DraftActors[DraftIndex]==0?First:1-First;Publish(message);return null;
  }
  var p=Players[actor];
  if(a.Type=="flip"){
   int cost=p.Confused?2:1,cry=a.Crystals,mana=cost-cry;
   if(cry<0 || mana<0 || p.Crystals<cry || p.Mana<mana)return "转位费用不足。迷惑时需要合计 2 点费用或水晶。";
   BeginEvent("flip",actor);p.Crystals-=cry;p.Mana-=mana;p.Confused=false;Flip(actor);
   Publish(p.Name+" 旋转星盘，现在为"+(Upright(actor)?"正位":"逆位")+"。");return null;
  }
  if(a.Type=="end"){
   BeginEvent("turn",actor);p.Crystals=Math.Min(3,p.Crystals+p.Mana);p.Mana=0;p.Confused=false;Active=1-Active;Players[Active].ResetTurn();Turn++;Draw(Active,1);
   Publish(Players[Active].Name+" 的回合开始，抽 1 张牌。");return null;
  }
  if(a.Type=="play"){
   var c=p.Hand.Find(x=>x.Id==a.Value);string err=CanPlay(actor,c,a.Crystals);if(err!=null)return err;
   BeginEvent("prepare",actor);context=new PlayContext{Actor=actor,Card=c,Up=Upright(actor),CrystalPay=a.Crystals,Previous=new List<Card>(p.Played),DamageBefore=p.DamageThisTurn,DrawsBefore=p.EffectDraws,ScryBefore=p.Scries,FlipsBefore=p.OwnFlips,DiscardsBefore=p.VoluntaryDiscards};
   ConfigureExtras(context);steps.Enqueue(CommitPlay);RunSteps();Publish(LastEvent.Type=="play"?CastText():"选择本牌的额外代价。");return null;
  }
  return "未知操作。";
 }
 string CastText(){return LastEvent.Card==null?"选择已结算。":Players[LastEvent.Actor].Name+" 打出「"+Rules.Get(LastEvent.Card).Name+"」· "+(LastEvent.Upright?"正位":"逆位");}
 void CommitPlay(){
  var c=context;var p=Players[c.Actor];var other=Players[1-c.Actor];var rule=Rules.Get(c.Card);
  p.Mana-=rule.Cost-c.CrystalPay;p.Crystals-=c.CrystalPay+c.ExtraCrystals;
  for(int i=0;i<4;i++)p.Tokens[i]-=c.ExtraTokens[i];
  foreach(int id in c.Discards){var card=p.Hand.Single(x=>x.Id==id);p.Hand.Remove(card);p.Discard.Add(card);p.VoluntaryDiscards++;}
  p.Hand.Remove(c.Card);c.Committed=true;
  c.HandCount=p.Hand.Count;c.OtherHandCount=other.Hand.Count;c.Crystals=p.Crystals;c.OtherCrystals=other.Crystals;c.Tokens=p.Tokens.Sum();c.OtherTokens=other.Tokens.Sum();c.DiscardCount=p.Discard.Count;c.DiscardKinds=p.Discard.Select(x=>x.Arcana).Distinct().Count();
  c.Advantage=c.Actor==0?Position>RoadCenter:Position<RoadCenter;c.Behind=c.Actor==0?Position<RoadCenter:Position>RoadCenter;
  LastEvent.Type="play";LastEvent.Actor=c.Actor;LastEvent.Card=c.Card;LastEvent.Upright=c.Up;
  BuildEffects(c);
 }
 void RunSteps(){
  while(PendingChoice==null && steps.Count>0 && Phase!="ended")steps.Dequeue()();
  if(Phase=="ended"){FinishPending();return;}
  if(PendingChoice==null && steps.Count==0 && context!=null && context.Committed){var c=context;Players[c.Actor].Discard.Add(c.Card);Players[c.Actor].Played.Add(c.Card);context=null;CheckOverflow();}
 }
 void FinishPending(){if(context!=null && context.Committed){Players[context.Actor].Discard.Add(context.Card);Players[context.Actor].Played.Add(context.Card);}context=null;steps.Clear();PendingChoice=null;answer=null;validateAnswer=null;PendingTokens=-1;}
 void Ask(int owner,string title,string kind,IEnumerable<ChoiceOption> options,int min,int max,Action<int[],int> callback,Func<int[],int,string> validator=null){
  PendingChoice=new Choice{Id=++choiceSerial,Owner=owner,Title=title,Kind=kind,Min=min,Max=max,Options=options.ToList(),Preparing=context!=null && !context.Committed,Source=context==null?null:context.Card,Upright=context!=null && context.Up};answer=callback;validateAnswer=validator;
 }
 void CheckOverflow(){PendingTokens=-1;for(int i=0;i<2;i++)if(Players[i].Tokens.Sum()>13){PendingTokens=i;break;}}
 public void GrantTokens(int p,int suit,int amount){if(p<0 || p>1 || suit<0 || suit>3 || amount<0)throw new ArgumentException();Players[p].Tokens[suit]+=amount;if(context==null)CheckOverflow();}
 public static string RankName(int rank){return new[]{"启示 A","秘仪 B","命定 C"}[rank];}
 public Snapshot View(int viewer,bool reveal){return new Snapshot{
  Phase=Phase,Revision=Revision,First=First,Active=Active,DraftIndex=DraftIndex,Position=Position,UprightPlayer=UprightPlayer,Turn=Turn,Winner=Winner,Available=new List<int>(Available),Banned=new List<int>(Banned),Log=new List<string>(Log),Event=LastEvent,Rules=Rules,PendingTokens=PendingTokens,
  Choice=PendingChoice!=null && (PendingChoice.Owner==viewer || reveal)?PendingChoice:null,ChoiceOwner=PendingChoice==null?-1:PendingChoice.Owner,
  ResolvingCard=context!=null && context.Committed?context.Card:null,ResolvingActor=context!=null && context.Committed?context.Actor:-1,ResolvingUpright=context!=null && context.Up,
  Players=Players.Select((p,i)=>new ViewPlayer{Name=p.Name,Picks=new List<int>(p.Picks),Hand=i==viewer || reveal?new List<Card>(p.Hand):new List<Card>(),HandCount=p.Hand.Count,DeckCount=p.Deck.Count,Discard=new List<Card>(p.Discard),Mana=p.Mana,Crystals=p.Crystals,Tokens=(int[])p.Tokens.Clone(),Shield=p.Shield,Confused=p.Confused}).ToArray()
 };}
}
}
