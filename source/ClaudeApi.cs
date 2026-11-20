using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZlhrsHelper {
    internal static class ClaudeApi {
        internal const string Endpoint="https://api.anthropic.com/v1/messages";
        internal const string Version="2026-10-21";
        internal static string Request(string model,CaptureData image,string systemPrompt=null){
            ModelCatalog.Validate("Claude",model);
            return Json.New().Serialize(Json.Obj("model",model,"max_tokens",16384,
                "system",VisionApi.Instructions(systemPrompt),
                "messages",new[]{Json.Obj("role","user","content",new object[]{
                    Json.Obj("type","image","source",Json.Obj("type","base64","media_type","image/png","data",Convert.ToBase64String(image.Png))),
                    Json.Obj("type","text","text","Найди видимые вопросы и варианты ответа на снимке. Ответь по-русски; текст вариантов сохрани на языке оригинала.")})},
                "output_config",Json.Obj("effort","low","format",Json.Obj("type","json_schema","schema",VisionApi.Schema()))));
        }
        internal static async Task<AnalysisResult> SolveAsync(string key,string payload,CancellationToken token){
            ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
            using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(token))
            using(var client=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false})){
                client.Timeout=TimeSpan.FromSeconds(90);timeout.CancelAfter(TimeSpan.FromSeconds(90));
                return await SendAsync(client,key,payload,timeout.Token).ConfigureAwait(false);
            }
        }
        internal static async Task<AnalysisResult> SendAsync(HttpClient client,string key,string payload,CancellationToken token){
            using(var request=new HttpRequestMessage(HttpMethod.Post,Endpoint)){
                request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
                request.Headers.Add("anthropic-version",Version);
                request.Content=new StringContent(payload,Encoding.UTF8,"application/json");
                using(var response=await client.SendAsync(request,token).ConfigureAwait(false)){
                    string data=await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    if(!response.IsSuccessStatusCode){
                        string type="",detail="";
                        try{
                            var root=Json.New().DeserializeObject(data) as Dictionary<string,object>;object raw;
                            var error=root!=null&&root.TryGetValue("error",out raw)?raw as Dictionary<string,object>:null;
                            if(error!=null){
                                if(error.TryGetValue("type",out raw))type=Convert.ToString(raw);
                                if(error.TryGetValue("message",out raw))detail=Convert.ToString(raw);
                            }
                        }catch(ArgumentException){}catch(InvalidOperationException){}
                        int status=(int)response.StatusCode;
                        string help=status==401?"Ключ Claude API не принят. Проверьте его в настройках.":
                            status==403?"Нет доступа к Claude API. Проверьте права ключа и доступность API.":
                            status==402||type=="billing_error"?"Недоступна квота Claude API. Проверьте баланс проекта.":
                            status==429?"Claude API: достигнут лимит запросов или квота.":
                            status==400||status==404?"Claude API не принял модель или запрос. Проверьте доступ к выбранной модели.":
                            status>=500?"Ошибка на стороне Claude API. Повторите позже.":"Claude API отклонил запрос.";
                        string message=help+"\n\nHTTP "+status+
                            (String.IsNullOrEmpty(type)?"":" · "+VisionApi.SafeDiagnostic(type,key))+
                            (String.IsNullOrEmpty(detail)?"":"\n\nClaude: "+VisionApi.SafeDiagnostic(detail,key));
                        if(status==429&&type=="rate_limit_error"){
                            var retry=response.Headers.RetryAfter;
                            TimeSpan? after=retry==null?null:retry.Delta??(retry.Date-DateTimeOffset.UtcNow);
                            throw new TemporaryRateLimit(message,after);
                        }
                        throw new InvalidOperationException(message);
                    }
                    return Parse(data);
                }
            }
        }
        internal static AnalysisResult Parse(string data){
            Dictionary<string,object> root;
            try{root=Json.New().DeserializeObject(data) as Dictionary<string,object>;}
            catch(ArgumentException){throw new InvalidOperationException("Claude API вернул некорректный ответ.");}
            if(root==null)throw new InvalidOperationException("Claude API вернул некорректный ответ.");
            object raw;
            string reason=root.TryGetValue("stop_reason",out raw)?Convert.ToString(raw):"";
            if(reason=="refusal")throw new InvalidOperationException("Claude отказался отвечать на этот запрос.");
            if(reason!="end_turn")throw new InvalidOperationException("Claude не завершил ответ. Покажите меньше вопросов и повторите снимок.");
            var content=root.TryGetValue("content",out raw)?raw as object[]:null;
            var text=new StringBuilder();
            if(content!=null)foreach(var item in content){
                var part=item as Dictionary<string,object>;
                if(part==null||!part.TryGetValue("type",out raw))continue;
                string type=Convert.ToString(raw);
                if(type=="refusal")throw new InvalidOperationException("Claude отказался отвечать на этот запрос.");
                if(type=="text"&&part.TryGetValue("text",out raw))text.Append(Convert.ToString(raw));
            }
            if(text.Length==0)throw new InvalidOperationException("Claude API вернул пустой ответ.");
            return VisionApi.ParseResult(text.ToString());
        }
    }
}
