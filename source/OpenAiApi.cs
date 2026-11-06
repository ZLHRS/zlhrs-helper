using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ZlhrsHelper {
    internal static class OpenAiApi {
        internal const string Endpoint="https://api.openai.com/v1/responses";
        internal static string NormalizeModel(string model){
            model=(model??"").Trim();
            if(!Regex.IsMatch(model,@"^gpt-[A-Za-z0-9._-]+$"))throw new InvalidOperationException("Введите имя модели OpenAI, например gpt-4.1-mini.");
            return model;
        }
        internal static string Request(string model,CaptureData image,string systemPrompt=null){
            return Json.New().Serialize(Json.Obj("model",NormalizeModel(model),"store",false,
                "instructions",VisionApi.Instructions(systemPrompt),"max_output_tokens",16384,
                "input",new[]{Json.Obj("role","user","content",new object[]{
                    Json.Obj("type","input_text","text","Найди видимые вопросы и варианты ответа на снимке. Ответь по-русски; текст вариантов сохрани на языке оригинала."),
                    Json.Obj("type","input_image","image_url","data:image/png;base64,"+Convert.ToBase64String(image.Png),"detail","high")})},
                "text",Json.Obj("format",Json.Obj("type","json_schema","name","screen_answers","strict",true,"schema",VisionApi.Schema()))));
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
            using(var message=new HttpRequestMessage(HttpMethod.Post,Endpoint)){
                message.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
                message.Content=new StringContent(payload,Encoding.UTF8,"application/json");
                using(var response=await client.SendAsync(message,token).ConfigureAwait(false)){
                    string data=await response.Content.ReadAsStringAsync().ConfigureAwait(false);token.ThrowIfCancellationRequested();
                    if(!response.IsSuccessStatusCode)throw new InvalidOperationException(Error((int)response.StatusCode,data,key));
                    return Parse(data);
                }
            }
        }
        internal static string Error(int status,string data,string key=null){
            string code="",message="",type="";
            try{
                var root=Json.New().DeserializeObject(data) as Dictionary<string,object>;object raw;
                var error=root!=null&&root.TryGetValue("error",out raw)?raw as Dictionary<string,object>:null;
                if(error!=null){if(error.TryGetValue("code",out raw))code=Convert.ToString(raw);if(error.TryGetValue("type",out raw))type=Convert.ToString(raw);if(error.TryGetValue("message",out raw))message=Convert.ToString(raw);}
            }catch{}
            string help=status==401?"Ключ OpenAI API не принят. Проверьте его в настройках.":
                status==403?"Нет доступа к OpenAI API. Проверьте права проекта и доступность API.":
                status==429?(type=="insufficient_quota"||code=="insufficient_quota"||code=="credit_balance_exhausted"?"Недоступна квота OpenAI API. Проверьте баланс и лимиты проекта.":"OpenAI API: достигнут лимит запросов или квота. Проверьте лимиты проекта."):
                status==400||status==404?"OpenAI API не принял модель или запрос. Проверьте модель и её доступность.":
                status>=500?"Ошибка на стороне OpenAI API. Повторите позже.":"OpenAI API отклонил запрос.";
            return help+"\n\nHTTP "+status+(String.IsNullOrEmpty(code)?"":" · "+VisionApi.SafeDiagnostic(code,key))+(String.IsNullOrEmpty(message)?"":"\n\nOpenAI: "+VisionApi.SafeDiagnostic(message,key));
        }
        internal static AnalysisResult Parse(string data){
            Dictionary<string,object> root;
            try{root=Json.New().DeserializeObject(data) as Dictionary<string,object>;}catch{throw new InvalidOperationException("API вернул некорректный ответ.");}
            if(root==null)throw new InvalidOperationException("API вернул некорректный ответ.");
            object raw;
            if(!root.TryGetValue("status",out raw)||Convert.ToString(raw)!="completed")throw new InvalidOperationException("API не завершил ответ. Покажите меньше вопросов и повторите захват.");
            var output=root.TryGetValue("output",out raw)?raw as object[]:null;var text=new StringBuilder();
            if(output!=null)foreach(var item in output){
                var entry=item as Dictionary<string,object>;
                var content=entry!=null&&entry.TryGetValue("content",out raw)?raw as object[]:null;
                if(content==null)continue;
                foreach(var part in content){
                    var p=part as Dictionary<string,object>;if(p==null||!p.TryGetValue("type",out raw))continue;
                    string type=Convert.ToString(raw);
                    if(type=="refusal")throw new InvalidOperationException("Модель отказалась отвечать на этот запрос.");
                    if(type=="output_text"&&p.TryGetValue("text",out raw))text.Append(Convert.ToString(raw));
                }
            }
            if(text.Length==0)throw new InvalidOperationException("API вернул пустой ответ.");
            return VisionApi.ParseResult(text.ToString());
        }
    }
}
