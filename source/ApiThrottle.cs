using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ZlhrsHelper {
    internal sealed class TemporaryRateLimit : InvalidOperationException {
        internal readonly TimeSpan? RetryAfter;
        internal TemporaryRateLimit(string message,TimeSpan? retryAfter):base(message){RetryAfter=retryAfter;}
    }
    internal static class ApiThrottle {
        static Dictionary<string,object> Object(object value){return value as Dictionary<string,object>;}
        static object Get(Dictionary<string,object> map,string key){object v;return map!=null&&map.TryGetValue(key,out v)?v:null;}
        static string Text(Dictionary<string,object> map,string key){return Convert.ToString(Get(map,key),CultureInfo.InvariantCulture);}
        internal static InvalidOperationException Error(HttpResponseMessage response,string body,string message,bool openAi){
            if((int)response.StatusCode!=429)return new InvalidOperationException(message);
            Dictionary<string,object> error;
            try{error=Object(Get(Object(Json.New().DeserializeObject(body)),"error"));}
            catch(ArgumentException){return new InvalidOperationException(message);}
            catch(InvalidOperationException){return new InvalidOperationException(message);}
            if(error==null)return new InvalidOperationException(message);
            var code=Text(error,"code");var type=Text(error,"type");
            var retry=response.Headers.RetryAfter;
            TimeSpan? after=retry==null?null:retry.Delta??(retry.Date-DateTimeOffset.UtcNow);
            bool temporary;
            if(openAi){
                var billing=new[]{"insufficient_quota","credit_balance_exhausted","organization_spend_limit_exceeded","project_spend_limit_exceeded","organization_usage_limit_exceeded"};
                temporary=!billing.Contains(code)&&!billing.Contains(type)&&(code=="rate_limit_exceeded"||code=="slow_down"||type=="rate_limit_error");
            }else{
                temporary=code=="rate_limit_exceeded"||Text(error,"status")=="RATE_LIMIT_EXCEEDED";
                var violations=new List<Dictionary<string,object>>();
                foreach(var raw in Get(error,"details") as object[]??new object[0]){
                    var detail=Object(raw);
                    var reason=Text(detail,"reason");
                    if(reason=="RATE_LIMIT_EXCEEDED")temporary=true;
                    foreach(var v in Get(detail,"violations") as object[]??new object[0]){var item=Object(v);if(item!=null)violations.Add(item);}
                    if(Text(detail,"@type")=="type.googleapis.com/google.rpc.RetryInfo"){
                        string rawDelay=Text(detail,"retryDelay");double seconds;
                        if(rawDelay.EndsWith("s",StringComparison.Ordinal)&&Double.TryParse(rawDelay.Substring(0,rawDelay.Length-1),NumberStyles.Float,CultureInfo.InvariantCulture,out seconds)&&seconds>=0&&seconds<TimeSpan.MaxValue.TotalSeconds)
                            after=TimeSpan.FromSeconds(Math.Max(seconds,after.HasValue?after.Value.TotalSeconds:0));
                    }
                }
                if(violations.Count>0)temporary=violations.All(v=>{
                    string id=Text(v,"quotaId");
                    return Text(v,"quotaValue")!="0"&&(id.IndexOf("PerMinute",StringComparison.OrdinalIgnoreCase)>=0||id.IndexOf("PerSecond",StringComparison.OrdinalIgnoreCase)>=0)
                        &&id.IndexOf("Spend",StringComparison.OrdinalIgnoreCase)<0;
                });
            }
            return temporary?new TemporaryRateLimit(message,after):new InvalidOperationException(message);
        }
        internal static int FirstSlot(string[] keys,int slot){
            return String.IsNullOrEmpty(keys[slot])?NextSlot(keys,slot):slot;
        }
        internal static int NextSlot(string[] keys,int slot){
            for(int step=1;step<keys.Length;step++){
                int index=(slot+step)%keys.Length;if(!String.IsNullOrEmpty(keys[index])&&keys[index]!=keys[slot])return index;
            }
            return slot;
        }
        internal static async Task<int> WithSlots(string[] keys,int first,bool rotate,Func<string,CancellationToken,Task> send,CancellationToken token,Func<TimeSpan,CancellationToken,Task> delay=null,Action<int> onAttempt=null){
            int[] order=Enumerable.Range(0,keys.Length).Select(i=>(first+i)%keys.Length).Where(i=>!String.IsNullOrEmpty(keys[i])).GroupBy(i=>keys[i],StringComparer.Ordinal).Select(g=>g.First()).ToArray();
            if(order.Length==0)throw new InvalidOperationException("Добавьте API-ключ в настройках.");
            for(int i=0;;i++){
                token.ThrowIfCancellationRequested();
                TimeSpan wait;
                if(onAttempt!=null)onAttempt(order[i]);
                try{await send(keys[order[i]],token).ConfigureAwait(false);return order[i];}
                catch(TemporaryRateLimit ex){
                    if(!rotate||i+1==order.Length)throw;
                    wait=ex.RetryAfter??TimeSpan.FromSeconds(1<<i);
                    if(wait>TimeSpan.FromSeconds(60))throw;
                }
                if(wait>TimeSpan.Zero)await (delay??Task.Delay)(wait,token).ConfigureAwait(false);
            }
        }
    }
}
