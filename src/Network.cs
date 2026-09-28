using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ArcanaDuel {
public class Packet { public string Kind; public string Code; public string Name; public string Payload; public int Version=3; }
public sealed class Room : IDisposable {
 public event Action<string> Connected; public event Action<Packet> Received; public event Action<string> Failed;
 TcpListener listener; TcpClient client; StreamReader reader; StreamWriter writer; object sendLock=new object(); volatile bool stopped; Timer timer;
 public bool IsHost; public const int Port=27841;
 public static string Json(object x){return new JavaScriptSerializer{MaxJsonLength=524288}.Serialize(x);}
 public static T Parse<T>(string x){return new JavaScriptSerializer{MaxJsonLength=524288}.Deserialize<T>(x);}
 static string ReadLine(StreamReader r){var b=new StringBuilder();while(true){int c=r.Read();if(c<0)throw new IOException("对方已断开连接。");if(c==10)break;if(c!=13)b.Append((char)c);if(b.Length>524288)throw new IOException("网络数据超过限制。");}return b.ToString();}
 void Setup(TcpClient c){client=c;c.NoDelay=true;c.ReceiveTimeout=16000;c.SendTimeout=4000;reader=new StreamReader(c.GetStream(),new UTF8Encoding(false));writer=new StreamWriter(c.GetStream(),new UTF8Encoding(false)){AutoFlush=true};}
 public void Host(string code,string name){
  IsHost=true;listener=new TcpListener(IPAddress.Any,Port);listener.Start(4);
  Task.Run(()=>{try{while(!stopped){var c=listener.AcceptTcpClient();c.ReceiveTimeout=6000;try{
   Setup(c);var p=Parse<Packet>(ReadLine(reader));if(p==null || p.Kind!="hello" || p.Version!=3 || p.Code!=code){Send(new Packet{Kind="reject",Payload="房间口令不正确或版本不一致。"});c.Close();continue;}
   Send(new Packet{Kind="welcome",Name=name});listener.Stop();listener=null;StartHeartbeat();if(Connected!=null)Connected(CleanName(p.Name,"旅人 II"));ReadLoop();break;
  }catch{c.Close();if(stopped)break;}}}catch(Exception e){Fail(e.Message);}});
 }
 public void Join(string host,string code,string name){
  Task.Run(()=>{try{var c=new TcpClient();client=c;var ar=c.BeginConnect(host,Port,null,null);using(ar.AsyncWaitHandle){if(!ar.AsyncWaitHandle.WaitOne(6000))throw new IOException("连接超时，请检查 IP、同一网络及房主端防火墙。");}c.EndConnect(ar);if(stopped){c.Close();return;}Setup(c);
   Send(new Packet{Kind="hello",Code=code,Name=name});var p=Parse<Packet>(ReadLine(reader));if(p==null || p.Kind!="welcome" || p.Version!=3)throw new IOException(p==null?"连接失败。":p.Payload ?? "版本不一致。");StartHeartbeat();if(Connected!=null)Connected(CleanName(p.Name,"旅人 I"));ReadLoop();
  }catch(Exception e){Fail(e.Message);}});
 }
 void StartHeartbeat(){timer=new Timer(o=>{try{Send(new Packet{Kind="ping"});}catch(Exception e){Fail(e.Message);}},null,3000,3000);}
 void ReadLoop(){try{while(!stopped){var p=Parse<Packet>(ReadLine(reader));if(p==null)throw new IOException("无效网络数据。");if(p.Kind!="ping" && Received!=null)Received(p);}}catch(Exception e){Fail(e.Message);}}
 public void Send(Packet p){lock(sendLock){if(stopped)return;if(writer==null)throw new IOException("尚未建立连接。");writer.WriteLine(Json(p));}}
 void Fail(string msg){if(stopped)return;Dispose();if(Failed!=null)Failed(msg);}
 public static string CleanName(string n,string fallback){if(String.IsNullOrWhiteSpace(n))return fallback;n=n.Replace("\r","").Replace("\n","").Trim();return n.Substring(0,Math.Min(16,n.Length));}
 public void Dispose(){stopped=true;if(timer!=null)timer.Dispose();try{if(listener!=null)listener.Stop();}catch{}try{if(client!=null)client.Close();}catch{}}
}
}

