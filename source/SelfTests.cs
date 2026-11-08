using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZlhrsHelper {
    internal static class SelfTests {
        static readonly List<string> report=new List<string>();
        static int failed;
        static void Check(string name,Action test){try{test();report.Add("PASS "+name);}catch(Exception e){failed++;report.Add("FAIL "+name+": "+e.GetType().Name+" "+e.Message);}}
        static void Assert(bool yes,string message){if(!yes)throw new Exception(message);}
        static void Throws(Action action,string contains){try{action();}catch(InvalidOperationException e){Assert(e.Message.Contains(contains),"Wrong error: "+e.Message);return;}throw new Exception("Expected an error");}
        static AnalysisResult Sample(){
            return new AnalysisResult{note="",questions=new[]{
                new QuestionAnswer{question="2 + 2?",answers=new[]{"Б) 4"},confident=true,explanation="Сумма равна четырём."},
                new QuestionAnswer{question="Простые числа?",answers=new[]{"А) 2","В) 5"},confident=true,explanation="У простого числа два делителя."},
                new QuestionAnswer{question="Неполный вопрос",answers=new[]{"SHOULD NOT DISPLAY"},confident=false,explanation="Не видны варианты."}
            }};
        }
        static string Envelope(AnalysisResult result){return Json.New().Serialize(Json.Obj("candidates",new[]{Json.Obj("finishReason","STOP","content",Json.Obj("parts",new[]{Json.Obj("text",Json.New().Serialize(result))}))}));}
        static string Instructions(Dictionary<string,object> payload){return (string)((Dictionary<string,object>)((object[])((Dictionary<string,object>)payload["systemInstruction"])["parts"])[0])["text"];}
        internal static int Run(string path){
            report.Add("ZlhrsHelper local verification; no live API requests; "+DateTime.UtcNow.ToString("u"));
            Check("Multiple questions, Unicode and answer choices parse",()=>{var r=VisionApi.Parse(Envelope(Sample()));Assert(r.questions.Length==3&&r.questions[0].answers[0]=="Б) 4"&&r.questions[1].answers.Length==2,"Lost data");});
            Check("Uncertain answers are not shown as correct",()=>{var text=VisionApi.Display(Sample());Assert(!text.Contains("SHOULD NOT DISPLAY")&&text.Contains("Недостаточно"),"Uncertain answer leaked");});
            Check("Empty screen guidance",()=>Assert(VisionApi.Display(new AnalysisResult{questions=new QuestionAnswer[0]}).Contains("Вопросы не найдены"),"Missing guidance"));
            Check("Reject incomplete response",()=>Throws(()=>VisionApi.Parse("{\"candidates\":[{\"finishReason\":\"MAX_TOKENS\"}]}"),"не завершил"));
            Check("Reject refusal",()=>Throws(()=>VisionApi.Parse("{\"promptFeedback\":{\"blockReason\":\"SAFETY\"}}"),"отказалась"));
            Check("Reject malformed JSON",()=>Throws(()=>VisionApi.Parse("not-json"),"некорректный"));
            Check("Reject missing content",()=>Throws(()=>VisionApi.Parse("{\"candidates\":[]}"),"пустой"));
            Check("Reject malformed question list",()=>Throws(()=>VisionApi.Parse(Envelope(new AnalysisResult{questions=new QuestionAnswer[]{null}})),"Некорректный"));
            Check("Reject excessive questions",()=>Throws(()=>VisionApi.Parse(Envelope(new AnalysisResult{questions=Enumerable.Repeat(Sample().questions[0],13).ToArray()})),"Некорректный"));
            Check("DPAPI key round trip and clearing",()=>{var s=new Settings();s.SetKey("test-placeholder-not-a-real-key");Assert(s.Key=="test-placeholder-not-a-real-key"&&!s.ProtectedKey.Contains("placeholder"),"Key protection failed");s.SetKey("");Assert(s.Key=="","Key clear failed");});
            CaptureData image=null;
            Check("Screen image size and PNG encoding",()=>{using(var bitmap=new Bitmap(3840,2160)){image=CaptureData.Encode(bitmap);Assert(image.Size==new Size(2400,1350),"Incorrect resize");using(var stream=new MemoryStream(image.Png))using(var decoded=Image.FromStream(stream))Assert(decoded.Size==image.Size,"Bad PNG");}});
            Check("Small screenshot is not enlarged",()=>{using(var bitmap=new Bitmap(800,600))Assert(CaptureData.Encode(bitmap).Size==bitmap.Size,"Unnecessary enlargement");});
            Check("Gemini image request and JSON schema",()=>{
                var request=Json.New().DeserializeObject(VisionApi.Request(Settings.DefaultModel,image)) as Dictionary<string,object>;
                Assert(!request.ContainsKey("model")&&!request.ContainsKey("store")&&!request.ContainsKey("input"),"Legacy fields present");
                var msg=((object[])request["contents"])[0] as Dictionary<string,object>;
                var img=((object[])msg["parts"])[1] as Dictionary<string,object>;var blob=img["inlineData"] as Dictionary<string,object>;
                Assert((string)blob["mimeType"]=="image/png"&&Convert.FromBase64String((string)blob["data"]).SequenceEqual(image.Png),"Image changed");
                var config=request["generationConfig"] as Dictionary<string,object>;
                Assert((string)config["responseMimeType"]=="application/json"&&config.ContainsKey("responseJsonSchema"),"Schema missing");
            });
            Check("Legacy preferences migrate without old API credentials",()=>{
                var old=new Settings{Model="gpt-old",ProtectedKey="old-key",SystemPrompt="Контекст",Opacity=42,HotkeyIndex=2};
                var next=Settings.FromLegacy(Json.New().Serialize(old));
                Assert(next.Model==Settings.DefaultModel&&next.ProtectedKey==""&&next.Opacity==42&&next.SystemPrompt=="Контекст"&&next.GetHotkeys()[0].Key==119,"Migration lost settings or retained old credentials");
                Assert(Settings.FilePath.EndsWith(Path.Combine("ZlhrsHelper","settings.json")),"Uses legacy settings file");
            });
            Check("Model normalization rejects URLs and unrelated models",()=>{
                Assert(VisionApi.NormalizeModel(" models/gemini-3.8-flash ")=="gemini-3.8-flash","Prefix not normalized");
                foreach(string invalid in new[]{"gpt-old","gemini-x?key=secret","https://example.com","gemini-x/../../"})Throws(()=>VisionApi.NormalizeModel(invalid),"имя модели");
            });
            Check("Thoughts are ignored and text parts concatenate",()=>{
                string answer=Json.New().Serialize(Sample());int mid=answer.Length/2;
                string body=Json.New().Serialize(Json.Obj("candidates",new[]{Json.Obj("finishReason","STOP","content",Json.Obj("parts",new[]{Json.Obj("thought",true,"text","private reasoning"),Json.Obj("text",answer.Substring(0,mid)),Json.Obj("text",answer.Substring(mid))}))}));
                Assert(VisionApi.Parse(body).questions.Length==3,"Parts or thoughts handled incorrectly");
            });
            Check("Candidate safety filter and absent finish reason rejected",()=>{
                Throws(()=>VisionApi.Parse("{\"candidates\":[{\"finishReason\":\"SAFETY\"}]}"),"отказалась");
                Throws(()=>VisionApi.Parse("{\"candidates\":[{}]}"),"не завершил");
            });
            Check("HTTP pipeline with fake transport",()=>{
                using(var client=new HttpClient(new FakeHandler(200,Envelope(Sample()),false))){var r=VisionApi.SendAsync(client,"test-placeholder","{}",CancellationToken.None).GetAwaiter().GetResult();Assert(r.questions.Length==3,"Pipeline lost data");}
            });
            Check("HTTP auth error",()=>{using(var client=new HttpClient(new FakeHandler(401,"{}",false)))Throws(()=>VisionApi.SendAsync(client,"test-placeholder","{}",CancellationToken.None).GetAwaiter().GetResult(),"Ключ API");});
            Check("Gemini quota errors preserve server status",()=>{
                string error=VisionApi.Error(429,"{\"error\":{\"code\":429,\"status\":\"RESOURCE_EXHAUSTED\",\"message\":\"Quota exceeded\"}}");
                Assert(error.Contains("квота")&&error.Contains("RESOURCE_EXHAUSTED")&&error.Contains("Quota exceeded"),"Missing quota guidance");
            });
            Check("Gemini auth, model and server errors",()=>{
                Assert(VisionApi.Error(403,"{}").Contains("доступа"),"Missing permissions guidance");
                Assert(VisionApi.Error(404,"{}").Contains("имя модели"),"Missing model guidance");
                Assert(VisionApi.Error(503,"{}").Contains("позже"),"Missing retry guidance");
            });
            Check("Diagnostic secrets are redacted",()=>{
                string data=Json.New().Serialize(Json.Obj("error",Json.Obj("status","INVALID_ARGUMENT","message","Bad AIzaSySECRET123 and custom-secret-token")));
                string error=VisionApi.Error(400,data,"custom-secret-token");
                Assert(!error.Contains("SECRET")&&!error.Contains("custom-secret-token"),"Diagnostic exposed key");
            });
            Check("Missing status preserves server message",()=>Assert(VisionApi.Error(400,"{\"error\":{\"message\":\"Model unavailable\"}}").Contains("Model unavailable"),"Server details discarded"));
            Check("Quota response through HTTP pipeline",()=>{using(var client=new HttpClient(new FakeHandler(429,"{\"error\":{\"status\":\"RESOURCE_EXHAUSTED\"}}",false)))Throws(()=>VisionApi.SendAsync(client,"test-placeholder","{}",CancellationToken.None).GetAwaiter().GetResult(),"квота");});
            Check("Selected model used in URL",()=>{
                using(var client=new HttpClient(new FakeHandler(200,Envelope(Sample()),false,"gemini-4-argon")))Assert(VisionApi.SendAsync(client,"test-placeholder","{}",CancellationToken.None,"models/gemini-4-argon").GetAwaiter().GetResult().questions.Length==3,"Model lost");
            });
            Check("503 retries and succeeds on third attempt",()=>{
                using(var handler=new RetryHandler(2))using(var client=new HttpClient(handler)){
                    Assert(VisionApi.SendAsync(client,"test-placeholder","{}",CancellationToken.None).GetAwaiter().GetResult().questions.Length==3&&handler.Calls==3,"Retry failed");
                }
            });
            Check("503 stops after three attempts with model guidance",()=>{
                using(var handler=new RetryHandler(99))using(var client=new HttpClient(handler)){
                    Throws(()=>VisionApi.SendAsync(client,"test-placeholder","{}",CancellationToken.None).GetAwaiter().GetResult(),"перегружена");
                    Assert(handler.Calls==3,"Unbounded retries");
                }
            });
            Check("Cancellation interrupts retry backoff",()=>{
                using(var handler=new RetryHandler(99))using(var client=new HttpClient(handler))using(var cts=new CancellationTokenSource()){
                    cts.CancelAfter(50);
                    try{VisionApi.SendAsync(client,"test-placeholder","{}",cts.Token).GetAwaiter().GetResult();}
                    catch(OperationCanceledException){Assert(handler.Calls==1,"Retried after cancellation");return;}
                    throw new Exception("Not cancelled");
                }
            });
            Check("Non-503 errors are never retried",()=>{
                using(var handler=new RetryHandler(99,429))using(var client=new HttpClient(handler)){
                    Throws(()=>VisionApi.SendAsync(client,"test-placeholder","{}",CancellationToken.None).GetAwaiter().GetResult(),"квота");
                    Assert(handler.Calls==1,"Quota error retried");
                }
            });
            Check("HTTP cancellation",()=>{
                using(var cts=new CancellationTokenSource())using(var client=new HttpClient(new FakeHandler(200,"",true))){cts.CancelAfter(40);try{VisionApi.SendAsync(client,"test-placeholder","{}",cts.Token).GetAwaiter().GetResult();}catch(OperationCanceledException){return;}throw new Exception("Not cancelled");}
            });
            Check("Mini-window content, hide state and opacity",()=>{using(var mini=new AnswerForm()){mini.WantedVisible=false;mini.CreateControl();mini.SetAnswer("Тестовый ответ","ДЕМО");Assert(mini.BodyText=="Тестовый ответ"&&!mini.Visible,"Missing answer or unexpected show");mini.SetVisible(false);Assert(!mini.WantedVisible&&!mini.Visible,"Hide failed");mini.SetOpacity(75);Assert(Math.Abs(mini.Opacity-.75)<.01,"Opacity failed");}});
            Check("Capture exclusion accepted by Windows",()=>{using(var mini=new AnswerForm()){IntPtr h=mini.Handle;Assert(mini.CaptureExcluded,"WDA_EXCLUDEFROMCAPTURE unavailable");uint affinity;Assert(Native.GetWindowDisplayAffinity(h,out affinity)&&affinity==0x11,"Affinity not retained");}});
            Check("Legacy settings retain all capture presets",()=>{
                uint[] keys={81,87,119};
                for(int i=0;i<3;i++){var cfg=Json.New().Deserialize<Settings>("{\"HotkeyIndex\":"+i+",\"Opacity\":75}");var chords=cfg.GetHotkeys();HotkeyChord.Validate(chords);Assert(chords[0].Key==keys[i]&&chords[0].Modifiers==(i==2?6u:3u)&&chords[1].Key==65,"Migration changed keys");}
            });
            Check("Custom hotkeys and model survive serialization",()=>{
                var cfg=new Settings{Model="gemini-4-argon",Opacity=0,Hotkeys=new[]{new HotkeyChord(6,121),new HotkeyChord(0,120),new HotkeyChord(10,75),new HotkeyChord(7,83)}};
                var restored=Json.New().Deserialize<Settings>(Json.New().Serialize(cfg));HotkeyChord.Validate(restored.GetHotkeys());Assert(restored.Model==cfg.Model&&restored.Opacity==0&&restored.Hotkeys[2].ToString()=="Ctrl + Win + K","Settings not retained");
                restored.Hotkeys[1]=restored.Hotkeys[0];Throws(()=>HotkeyChord.Validate(restored.GetHotkeys()),"своя комбинация");
            });
            Check("Hotkey editor records arbitrary chord",()=>{
                using(var box=new HotkeyBox(new HotkeyChord(3,81))){
                    var method=typeof(HotkeyBox).GetMethod("ProcessCmdKey",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                    object[] args={new Message(),Keys.Control|Keys.Shift|Keys.F10};method.Invoke(box,args);
                    Assert(box.Chord.Key==(uint)Keys.F10&&box.Chord.Modifiers==6,"Captured incorrect chord");
                }
            });
            Check("Zero opacity stays hidden after answers and toggles; slider restores",()=>{
                using(var mini=new AnswerForm()){
                    mini.Location=new Point(-30000,-30000);mini.SetOpacity(0);mini.SetVisible(true);mini.SetAnswer("answer","test");mini.RestoreVisibility();
                    Assert(mini.Opacity==0&&!mini.Visible&&mini.WantedVisible,"Zero opacity revealed window");
                    mini.SetOpacity(1);Assert(mini.Visible&&Math.Abs(mini.Opacity-.01)<.001,"Minimum visible opacity failed");
                    mini.SetVisible(false);mini.SetOpacity(100);Assert(!mini.Visible&&mini.Opacity==1,"Opacity reset hidden state");
                    mini.SetOpacity(0);mini.SetVisible(false);mini.SetVisible(true);Assert(!mini.Visible,"Toggle bypassed zero opacity");
                }
            });
            Check("Gemini models and four independent binding editors",()=>{
                using(var form=new MainForm(true)){var choices=form.Controls.OfType<ComboBox>().Single(c=>c.Name=="Model");Assert(choices.Items.Contains("gemini-4-argon")&&choices.Text==Settings.DefaultModel,"Model list or default changed");Assert(form.Controls.OfType<HotkeyBox>().Count()==5,"Missing binding editors");}
                var payload=Json.New().DeserializeObject(VisionApi.Request("gemini-4-argon",image)) as Dictionary<string,object>;Assert(payload.ContainsKey("contents"),"Gemini request missing");
            });
            Check("System prompt persists multiline Unicode and old settings stay empty",()=>{
                string prompt="Предмет: физика\r\nИспользуй g = 10 м/с².\r\nНазвание: \"опыт\"";
                var cfg=Json.New().Deserialize<Settings>(Json.New().Serialize(new Settings{SystemPrompt=prompt}));
                Assert(cfg.SystemPrompt==prompt,"Prompt changed on save");Assert(Json.New().Deserialize<Settings>("{}").SystemPrompt=="","Legacy prompt not empty");
                var payload=Json.New().DeserializeObject(VisionApi.Request(Settings.DefaultModel,image,cfg.SystemPrompt)) as Dictionary<string,object>;
                Assert(Instructions(payload).Contains(prompt),"Prompt not passed in instructions");
                var empty=Json.New().DeserializeObject(VisionApi.Request(Settings.DefaultModel,image,"")) as Dictionary<string,object>;
                Assert(!Instructions(empty).Contains(prompt),"Cleared prompt still sent");
                Assert(VisionApi.Request(Settings.DefaultModel,image,null)==VisionApi.Request(Settings.DefaultModel,image,""),"Empty prompt changes request");
            });
            Check("Provider switch changes models and retains independent draft keys",()=>{
                using(var form=new MainForm(true)){
                    var provider=form.Controls.OfType<ComboBox>().Single(c=>c.Name=="Provider");
                    var model=form.Controls.OfType<ComboBox>().Single(c=>c.Name=="Model");
                    var key=form.Controls.OfType<TextBox>().Single(c=>c.UseSystemPasswordChar);
                    key.Text="gemini-test-key";model.Text="gemini-custom-model";provider.SelectedItem="OpenAI";
                    Assert(key.Text==""&&model.Text=="gpt-4.1-mini"&&model.Items.Contains("gpt-4.1")&&!model.Items.Contains(Settings.DefaultModel),"OpenAI options or key isolation failed");
                    key.Text="openai-test-key";model.Text="gpt-custom-model";provider.SelectedItem="Gemini";
                    Assert(key.Text=="gemini-test-key"&&model.Text=="gemini-custom-model"&&!model.Items.Contains("gpt-4.1"),"Gemini draft lost");
                    provider.SelectedItem="OpenAI";Assert(key.Text=="openai-test-key"&&model.Text=="gpt-custom-model","OpenAI draft lost");
                }
            });
            Check("Both provider keys and models survive encrypted serialization",()=>{
                var cfg=new Settings();cfg.SetKey("gemini-test-key");cfg.Provider="OpenAI";cfg.SetKey("openai-test-key");cfg.SetModel("gpt-4.1");
                string json=Json.New().Serialize(cfg);Assert(!json.Contains("test-key"),"Plaintext key saved");
                var restored=Json.New().Deserialize<Settings>(json);Assert(restored.Key=="openai-test-key"&&restored.ActiveModel=="gpt-4.1","OpenAI values lost");
                restored.SetKey("");restored.Provider="Gemini";Assert(restored.Key=="gemini-test-key"&&restored.ActiveModel==Settings.DefaultModel,"Gemini values lost when OpenAI cleared");
                Assert(Json.New().Deserialize<Settings>("{}").Provider=="Gemini","Existing Gemini settings changed provider");
            });
            Check("OpenAI image request and structured output schema",()=>{
                var payload=Json.New().DeserializeObject(OpenAiApi.Request("gpt-4.1-mini",image,"Тестовый промпт")) as Dictionary<string,object>;
                Assert((string)payload["model"]=="gpt-4.1-mini"&&!(bool)payload["store"]&&((string)payload["instructions"]).Contains("Тестовый промпт"),"OpenAI configuration lost");
                var msg=(Dictionary<string,object>)((object[])payload["input"])[0];var img=(Dictionary<string,object>)((object[])msg["content"])[1];
                Assert(((string)img["image_url"]).StartsWith("data:image/png;base64,"),"Missing OpenAI image");
                var format=(Dictionary<string,object>)((Dictionary<string,object>)payload["text"])["format"];Assert((bool)format["strict"]&&(string)format["type"]=="json_schema","Missing schema");
                Throws(()=>OpenAiApi.NormalizeModel(Settings.DefaultModel),"OpenAI");Throws(()=>VisionApi.NormalizeModel("gpt-4.1"),"Gemini");
            });
            Check("OpenAI HTTP transport and completed response",()=>{
                string response=Json.New().Serialize(Json.Obj("status","completed","output",new[]{Json.Obj("type","message","content",new[]{Json.Obj("type","output_text","text",Json.New().Serialize(Sample()))})}));
                using(var client=new HttpClient(new OpenAiHandler(200,response))){var result=OpenAiApi.SendAsync(client,"openai-test-key","{}",CancellationToken.None).GetAwaiter().GetResult();Assert(result.questions.Length==3&&result.questions[0].answers[0]=="Б) 4","OpenAI response lost");}
                using(var client=new HttpClient(new OpenAiHandler(401,"{}")))Throws(()=>OpenAiApi.SendAsync(client,"openai-test-key","{}",CancellationToken.None).GetAwaiter().GetResult(),"Ключ OpenAI");
            });
            Check("OpenAI refuses partial malformed and blocked output",()=>{
                Throws(()=>OpenAiApi.Parse("{\"status\":\"incomplete\",\"output\":[]}"),"не завершил");
                Throws(()=>OpenAiApi.Parse("not-json"),"некорректный");
                Throws(()=>OpenAiApi.Parse("{\"status\":\"completed\",\"output\":[]}"),"пустой");
                Throws(()=>OpenAiApi.Parse("{\"status\":\"completed\",\"output\":[{\"content\":[{\"type\":\"refusal\"}]}]}"),"отказалась");
                string error=OpenAiApi.Error(429,"{\"error\":{\"type\":\"insufficient_quota\",\"message\":\"openai-test-key\"}}","openai-test-key");Assert(error.Contains("квота")&&!error.Contains("openai-test-key"),"OpenAI diagnostics leaked secret or lost quota");
            });
            Check("Renamed app loads previous settings and prefers its own settings",()=>{
                string dir=Path.Combine(Path.GetTempPath(),"ZlhrsHelper-test-"+Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(dir);
                string current=Path.Combine(dir,"new-settings.json"),previous=Path.Combine(dir,"gemini-settings.json");
                try{
                    var cfg=new Settings{Provider="OpenAI",OpenAiModel="gpt-4.1",SystemPrompt="Контекст",Opacity=42};
                    cfg.SetKey("migration-test-key");File.WriteAllText(previous,Json.New().Serialize(cfg));
                    var migrated=Settings.LoadFrom(current,dir);
                    Assert(migrated.Provider=="OpenAI"&&migrated.Key=="migration-test-key"&&migrated.ActiveModel=="gpt-4.1"&&migrated.Opacity==42&&migrated.SystemPrompt=="Контекст","Previous settings lost");
                    File.WriteAllText(current,Json.New().Serialize(new Settings{Opacity=77}));
                    Assert(Settings.LoadFrom(current,dir).Opacity==77,"Old settings override current settings");
                    Assert(File.Exists(previous),"Migration removed previous settings");
                }finally{if(File.Exists(current))File.Delete(current);if(File.Exists(previous))File.Delete(previous);Directory.Delete(dir);}
            });
            Check("Window themes change independently without changing provider drafts",()=>{
                using(var form=new MainForm(true)){
                    var main=form.Controls.OfType<ComboBox>().Single(c=>c.Name=="MainTheme");
                    var answer=form.Controls.OfType<ComboBox>().Single(c=>c.Name=="AnswerTheme");
                    var model=form.Controls.OfType<ComboBox>().Single(c=>c.Name=="Model");
                    var key=form.Controls.OfType<TextBox>().Single(c=>c.UseSystemPasswordChar);
                    key.Text="theme-test-key";model.Text="gemini-custom";form.Mini.SetAnswer("Тестовый ответ","test");form.Mini.SetOpacity(75);
                    main.SelectedItem=Palette.Find("light","green");
                    answer.SelectedItem=Palette.Find("black","light");
                    Assert(form.BackColor.ToArgb()==Color.White.ToArgb()&&form.Mini.BackColor.ToArgb()==Color.Black.ToArgb(),"Window themes coupled");
                    main.SelectedItem=Palette.Find("purple","green");
                    Assert(form.Mini.BackColor.ToArgb()==Color.Black.ToArgb()&&form.BackColor==Palette.Find("purple","green").Background,"Main theme changed answer");
                    answer.SelectedItem=Palette.Find("blue","light");
                    Assert(form.BackColor==Palette.Find("purple","green").Background&&form.Mini.BodyText=="Тестовый ответ"&&Math.Abs(form.Mini.Opacity-.75)<.01,"Answer theme changed content or opacity");
                    Assert(key.Text=="theme-test-key"&&model.Text=="gemini-custom","Theme reset provider draft");
                    Assert(form.Mini.Controls.OfType<Button>().Count()==1&&form.Mini.Controls.Count==2,"Theme added extra answer controls");
                }
            });
            Check("Every palette applies to inputs buttons and answer text",()=>{
                using(var form=new MainForm(true))foreach(var palette in Palette.All){
                    form.Controls.OfType<ComboBox>().Single(c=>c.Name=="MainTheme").SelectedItem=palette;
                    form.Controls.OfType<ComboBox>().Single(c=>c.Name=="AnswerTheme").SelectedItem=palette;
                    var key=form.Controls.OfType<TextBox>().Single(c=>c.UseSystemPasswordChar);
                    var save=form.Controls.OfType<Button>().Single(c=>c.Text=="Сохранить");
                    var panel=form.Mini.Controls.OfType<Panel>().Single();var body=panel.Controls.OfType<Label>().Single();
                    Assert(form.BackColor==palette.Background&&key.BackColor==palette.Surface&&key.ForeColor==palette.Text,"Input palette missing: "+palette.Id);
                    Assert(save.BackColor==palette.Accent&&save.ForeColor==palette.AccentText,"Button palette missing: "+palette.Id);
                    Assert(body.ForeColor==palette.Text&&panel.BackColor==palette.Background,"Answer palette missing: "+palette.Id);
                }
            });
            Check("Separate theme preferences persist and older settings keep defaults",()=>{
                var cfg=Json.New().Deserialize<Settings>(Json.New().Serialize(new Settings{MainTheme="blue",AnswerTheme="black"}));
                Assert(cfg.MainTheme=="blue"&&cfg.AnswerTheme=="black","Theme preferences lost");
                var old=Json.New().Deserialize<Settings>("{}");Assert(old.MainTheme=="green"&&old.AnswerTheme=="light","Legacy colors changed");
                Assert(Palette.Find("unknown","green").Id=="green"&&Palette.Find(null,"light").Id=="light","Invalid theme fallback failed");
            });
            Check("All seven themes contain their embedded character wallpapers",()=>{
                Assert(Palette.All.Length==7&&Wallpaper.All.Length==7,"Missing pink theme or wallpaper");
                foreach(var palette in Palette.All){var wallpaper=Wallpaper.Find(palette.Id);Assert(wallpaper.Id==palette.Id,"Wallpaper mapping missing: "+palette.Id);using(var art=wallpaper.Load())Assert(art.Width>=512&&art.Height>=768,"Low-resolution wallpaper: "+palette.Id);}
                Assert(Wallpaper.Find("light").Character.Contains("Сора")&&Wallpaper.Find("pink").Character.Contains("02"),"Requested characters missing");
            });
            Check("Character wallpapers follow only the main theme",()=>{
                using(var form=new MainForm(true)){
                    var main=form.Controls.OfType<ComboBox>().Single(c=>c.Name=="MainTheme");var answer=form.Controls.OfType<ComboBox>().Single(c=>c.Name=="AnswerTheme");
                    main.SelectedItem=Palette.Find("light","green");Assert(form.WallpaperId=="light","Sora background missing");
                    answer.SelectedItem=Palette.Find("pink","light");Assert(form.WallpaperId=="light","Answer theme changed main wallpaper");
                    main.SelectedItem=Palette.Find("pink","green");Assert(form.WallpaperId=="pink"&&form.Mini.BackColor==Palette.Find("pink","light").Background,"Pink background missing");
                    foreach(var palette in Palette.All){main.SelectedItem=palette;Assert(form.WallpaperId==palette.Id,"Wallpaper did not switch: "+palette.Id);}
                    Assert(form.Mini.Controls.Count==2,"Wallpaper added controls to the answer window");
                }
                var cfg=Json.New().Deserialize<Settings>(Json.New().Serialize(new Settings{MainTheme="pink",AnswerTheme="black"}));Assert(cfg.MainTheme=="pink"&&cfg.AnswerTheme=="black","Pink theme preferences lost");
            });
            Check("Styled opacity slider controls the answer by keyboard and mouse",()=>{
                using(var form=new MainForm(true)){
                    var slider=form.Controls.OfType<PaletteSlider>().Single();
                    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                    var keyMethod=typeof(PaletteSlider).GetMethod("OnKeyDown",flags);
                    var down=typeof(PaletteSlider).GetMethod("OnMouseDown",flags);var up=typeof(PaletteSlider).GetMethod("OnMouseUp",flags);
                    keyMethod.Invoke(slider,new object[]{new KeyEventArgs(Keys.Home)});Assert(slider.Value==0&&form.Mini.Opacity==0,"Keyboard cannot hide answer");
                    keyMethod.Invoke(slider,new object[]{new KeyEventArgs(Keys.End)});keyMethod.Invoke(slider,new object[]{new KeyEventArgs(Keys.Left)});
                    Assert(slider.Value==99&&Math.Abs(form.Mini.Opacity-.99)<.01,"Keyboard cannot adjust answer opacity");
                    form.Controls.OfType<ComboBox>().Single(c=>c.Name=="MainTheme").SelectedItem=Palette.Find("pink","green");
                    Assert(slider.Value==99&&slider.Accent==Palette.Find("pink","green").Accent,"Theme changed slider value or lost color");
                    var left=new MouseEventArgs(MouseButtons.Left,1,0,slider.Height/2,0);down.Invoke(slider,new object[]{left});up.Invoke(slider,new object[]{left});
                    Assert(slider.Value==0&&form.Mini.Opacity==0,"Mouse cannot set zero opacity");
                    var right=new MouseEventArgs(MouseButtons.Left,1,slider.Width-1,slider.Height/2,0);down.Invoke(slider,new object[]{right});up.Invoke(slider,new object[]{right});
                    Assert(slider.Value==100&&form.Mini.Opacity==1,"Mouse cannot set full opacity");
                }
            });
            Check("Character cursor follows main theme on all controls and stays out of answer window",()=>{
                using(var outside=new Form())using(var form=new MainForm(true)){
                    IntPtr outsideCursor=outside.Cursor.Handle,answerCursor=form.Mini.Cursor.Handle;
                    var main=form.Controls.OfType<ComboBox>().Single(c=>c.Name=="MainTheme");var answer=form.Controls.OfType<ComboBox>().Single(c=>c.Name=="AnswerTheme");
                    foreach(var palette in Palette.All){
                        main.SelectedItem=palette;Assert(form.CursorThemeId==palette.Id,"Cursor theme did not switch");
                        IntPtr expected=form.Cursor.Handle;Assert(expected!=Cursors.Default.Handle,"Default cursor still selected");
                        Action<Control> check=null;check=control=>{Assert(control.Cursor.Handle==expected,"Control missed custom cursor: "+control.Name);foreach(Control child in control.Controls)check(child);};check(form);
                        answer.SelectedItem=Palette.Find(palette.Id=="pink"?"blue":"pink","light");
                        Assert(form.Cursor.Handle==expected&&form.Mini.Cursor.Handle==answerCursor&&outside.Cursor.Handle==outsideCursor,"Cursor affected another window or followed answer theme");
                    }
                }
            });
            Check("All character cursor handles have click hotspots and are released",()=>{
                foreach(var palette in Palette.All)foreach(int dpi in new[]{96,192}){
                    var cursor=CharacterCursor.Create(palette.Id,dpi);IntPtr handle=cursor.Value.Handle;CharacterCursor.IconInfo info;
                    try{
                        Assert(CharacterCursor.GetIconInfo(handle,out info),"Invalid cursor handle");
                        try{Assert(!info.IsIcon&&info.XHotspot==(dpi==96?1u:2u)&&info.YHotspot==info.XHotspot,"Cursor hotspot missing");Assert(info.Color!=IntPtr.Zero,"Cursor has no color bitmap");}
                        finally{if(info.Mask!=IntPtr.Zero)CharacterCursor.DeleteObject(info.Mask);if(info.Color!=IntPtr.Zero)CharacterCursor.DeleteObject(info.Color);}
                    }finally{cursor.Dispose();}
                    Assert(!CharacterCursor.GetIconInfo(handle,out info),"Native cursor handle was not released");
                }
            });
            Check("Cursor sprites have real transparency",()=>{
                foreach(var palette in Palette.All)using(var stream=typeof(MainForm).Assembly.GetManifestResourceStream("Cursor."+palette.Id))using(var bitmap=new Bitmap(stream)){
                    Assert(bitmap.GetPixel(0,0).A==0&&bitmap.GetPixel(bitmap.Width-1,bitmap.Height-1).A==0,"Opaque cursor sprite background: "+palette.Id);
                }
            });
            Check("Settings resize on short and narrow screens without losing controls",()=>{
                using(var form=new MainForm(true)){
                    form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);form.Show();
                    var key=form.Controls.OfType<TextBox>().Single(c=>c.UseSystemPasswordChar);
                    var save=form.Controls.OfType<Button>().Single(c=>c.Text=="Сохранить");
                    var model=(ComboBox)form.Controls["Model"];
                    key.Text="resize-test-placeholder";model.Text="gemini-resize-test";
                    foreach(var logical in new[]{new Size(990,810),new Size(990,630),new Size(620,550),new Size(430,350),new Size(990,810)}){
                        form.AutoScrollPosition=Point.Empty;
                        form.ClientSize=new Size(SettingsLayout.Scale(logical.Width,form.DeviceDpi),SettingsLayout.Scale(logical.Height,form.DeviceDpi));
                        form.PerformLayout();Application.DoEvents();
                        Assert(!form.HorizontalScroll.Visible,"Horizontal scrolling on "+logical);
                        var items=form.Controls.Cast<Control>().Where(c=>c.Visible&&(!String.IsNullOrEmpty(c.Text)||c is PaletteSlider)).ToArray();
                        foreach(Control item in items)Assert(item.Left>=0&&item.Right<=form.ClientSize.Width,"Control clipped on "+logical+": "+item.GetType().Name);
                        for(int a=0;a<items.Length;a++)for(int b=a+1;b<items.Length;b++)
                            Assert(!items[a].Bounds.IntersectsWith(items[b].Bounds),"Controls overlap on "+logical+": "+items[a].GetType().Name+"/"+items[b].GetType().Name);
                        if(logical.Height>=630)Assert(form.ClientRectangle.Contains(save.Bounds),"Save is hidden at laptop height");
                        form.ScrollControlIntoView(save);Application.DoEvents();
                        Assert(form.ClientRectangle.Contains(save.Bounds),"Save cannot be reached on "+logical);
                        Assert(key.Text=="resize-test-placeholder"&&model.Text=="gemini-resize-test","Resize lost typed settings");
                    }
                    form.Close();
                }
            });
            Check("Full HD work area fits settings at common DPI scales",()=>{
                foreach(int dpi in new[]{96,120,144,192}){
                    Size viewport=new Size(Math.Min(SettingsLayout.Scale(990,dpi),1870),Math.Min(SettingsLayout.Scale(810,dpi),946));
                    var layout=new SettingsLayout(viewport,dpi);
                    Rectangle save=layout.Place(new Rectangle(30,700,240,40));
                    Rectangle settings=layout.Place(new Rectangle(20,78,616,140));
                    Assert(save.Width>0&&save.Right<viewport.Width&&settings.Right<viewport.Width,"Fields overflow at DPI "+dpi);
                    Assert(save.Bottom<=layout.ScrollHeight,"Save lies outside scrollable content at DPI "+dpi);
                    if(dpi<=144)Assert(save.Bottom<=viewport.Height,"Save not immediately visible at DPI "+dpi);
                    Rectangle art=layout.Portrait(new Size(1024,1536));
                    Assert(Math.Abs(art.Width*1.5-art.Height)<2,"Wallpaper aspect ratio changed at DPI "+dpi);
                }
            });
            Check("Key slots preserve legacy encryption and stay separate by provider",()=>{
                var settings=Json.New().Deserialize<Settings>("{\"Provider\":\"OpenAI\"}");
                settings.SetKey("legacy-slot-test");settings.SetKey(1,"second-slot-test");settings.SetKey(2,"third-slot-test");
                settings.KeySlot=1;settings.AutoRotate=true;settings.RotateEachRequest=true;
                settings.Provider="Gemini";settings.SetKey(2,"gemini-slot-test");settings.KeySlot=2;
                string json=Json.New().Serialize(settings);
                Assert(!json.Contains("slot-test"),"Plaintext key reached settings");
                var loaded=Json.New().Deserialize<Settings>(json);
                Assert(loaded.Key=="gemini-slot-test"&&!loaded.AutoRotate&&!loaded.RotateEachRequest,"Gemini preferences lost");
                loaded.Provider="OpenAI";
                Assert(loaded.Key=="second-slot-test"&&loaded.AutoRotate&&loaded.RotateEachRequest&&loaded.GetKey(0)=="legacy-slot-test","OpenAI preferences lost");
                Assert(ApiThrottle.NextSlot(loaded.GetKeys(),1)==2&&ApiThrottle.NextSlot(loaded.GetKeys(),2)==0,"Slot cycle incorrect");
            });
            Check("Rate limit fallback tries distinct slots in order and waits",()=>{
                var used=new List<string>();var waits=new List<TimeSpan>();
                int slot=ApiThrottle.WithSlots(new[]{"one","two","three"},0,true,(key,token)=>{
                    used.Add(key);if(key!="three")throw new TemporaryRateLimit("test",TimeSpan.FromSeconds(3));
                    return Task.FromResult(0);
                },CancellationToken.None,(delay,token)=>{waits.Add(delay);return Task.FromResult(0);}).GetAwaiter().GetResult();
                Assert(slot==2&&used.SequenceEqual(new[]{"one","two","three"})&&waits.SequenceEqual(new[]{TimeSpan.FromSeconds(3),TimeSpan.FromSeconds(3)}),"Retry order or server delay lost");
                int attempts=0;
                Throws(()=>ApiThrottle.WithSlots(new[]{"one","one",""},0,true,(key,token)=>{attempts++;throw new TemporaryRateLimit("limited",null);},CancellationToken.None).GetAwaiter().GetResult(),"limited");
                Assert(attempts==1,"Duplicate key retried");
                Throws(()=>ApiThrottle.WithSlots(new[]{"one","two","three"},0,false,(key,token)=>{attempts++;throw new TemporaryRateLimit("disabled",null);},CancellationToken.None).GetAwaiter().GetResult(),"disabled");
                Assert(attempts==2,"Rotation ran while disabled");
            });
            Check("429 classifier separates temporary throttling from quota and billing",()=>{
                using(var response=new HttpResponseMessage((HttpStatusCode)429)){
                    response.Headers.RetryAfter=new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(12));
                    var transient=ApiThrottle.Error(response,"{\"error\":{\"code\":\"rate_limit_exceeded\"}}","test",true) as TemporaryRateLimit;
                    Assert(transient!=null&&transient.RetryAfter==TimeSpan.FromSeconds(12),"OpenAI delay missing");
                    Assert(!(ApiThrottle.Error(response,"{\"error\":{\"code\":\"insufficient_quota\",\"type\":\"rate_limit_error\"}}","test",true) is TemporaryRateLimit),"Billing misclassified");
                    Assert(!(ApiThrottle.Error(response,"{\"error\":{\"status\":\"RESOURCE_EXHAUSTED\"}}","test",false) is TemporaryRateLimit),"Unknown Gemini quota misclassified");
                    var minute="{\"error\":{\"status\":\"RESOURCE_EXHAUSTED\",\"details\":[{\"violations\":[{\"quotaId\":\"GenerateRequestsPerMinutePerProject\",\"quotaValue\":\"10\"}]}]}}";
                    Assert(ApiThrottle.Error(response,minute,"test",false) is TemporaryRateLimit,"Gemini RPM not identified");
                    Assert(!(ApiThrottle.Error(response,minute.Replace("PerMinute","PerDay"),"test",false) is TemporaryRateLimit),"Daily quota misclassified");
                }
            });
            Check("Request rotation cycles active credentials and skips gaps",()=>{
                var keys=new[]{"first","","third"};
                int slot=ApiThrottle.FirstSlot(keys,1);
                var attempted=new List<int>();
                for(int i=0;i<4;i++){
                    int used=ApiThrottle.WithSlots(keys,slot,false,(key,token)=>Task.FromResult(0),CancellationToken.None,
                        onAttempt:n=>attempted.Add(n)).GetAwaiter().GetResult();
                    slot=ApiThrottle.NextSlot(keys,used);
                }
                Assert(attempted.SequenceEqual(new[]{2,0,2,0}),"Rotation lost order or used an empty slot");
            });
            Check("Existing hotkeys gain an optional next-key binding without changing old chords",()=>{
                var settings=new Settings{Hotkeys=new[]{new HotkeyChord(3,81),new HotkeyChord(3,65),new HotkeyChord(3,88),new HotkeyChord(3,83)}};
                var migrated=settings.GetHotkeys();
                Assert(migrated.Length==5&&migrated[4]==null&&migrated.Take(4).SequenceEqual(settings.Hotkeys),"Existing binds changed");
                HotkeyChord.Validate(migrated);
                migrated[4]=new HotkeyChord(3,75);HotkeyChord.Validate(migrated);
                migrated[4]=migrated[0];Throws(()=>HotkeyChord.Validate(migrated),"своя комбинация");
            });
            report.Add("RESULT: "+(report.Count-1-failed)+" passed, "+failed+" failed.");
            report.Add("Live API access, answer accuracy, and third-party recording/streaming compatibility are NOT verified.");
            File.WriteAllLines(Path.GetFullPath(path),report,Encoding.UTF8);return failed==0?0:1;
        }
        sealed class OpenAiHandler : HttpMessageHandler {
            readonly int code;readonly string body;
            internal OpenAiHandler(int code,string body){this.code=code;this.body=body;}
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){
                Assert(request.RequestUri.AbsoluteUri==OpenAiApi.Endpoint&&request.Method==HttpMethod.Post,"Wrong OpenAI destination");
                Assert(request.Headers.Authorization.Scheme=="Bearer"&&request.Headers.Authorization.Parameter=="openai-test-key"&&!request.Headers.Contains("x-goog-api-key"),"Wrong provider credentials");
                return Task.FromResult(new HttpResponseMessage((HttpStatusCode)code){Content=new StringContent(body)});
            }
        }
        sealed class RetryHandler : HttpMessageHandler {
            internal int Calls;readonly int failures,code;
            internal RetryHandler(int failures,int code=503){this.failures=failures;this.code=code;}
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){
                Calls++;token.ThrowIfCancellationRequested();
                Assert(request.Headers.GetValues("x-goog-api-key").Single()=="test-placeholder","Key missing on retry");
                Assert(request.Content!=null,"Payload missing on retry");
                return Task.FromResult(new HttpResponseMessage(Calls<=failures?(HttpStatusCode)code:HttpStatusCode.OK){Content=new StringContent(Calls<=failures?"{}":Envelope(Sample()))});
            }
        }
        sealed class FakeHandler : HttpMessageHandler {
            readonly int code;readonly string body;readonly bool wait;readonly string model;
            internal FakeHandler(int code,string body,bool wait,string model=null){this.code=code;this.body=body;this.wait=wait;this.model=model??Settings.DefaultModel;}
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){
                Assert(request.Method==HttpMethod.Post&&request.RequestUri.AbsoluteUri==VisionApi.Endpoint+model+":generateContent","Unexpected destination");Assert(request.Headers.Authorization==null&&request.Headers.GetValues("x-goog-api-key").Single()=="test-placeholder"&&!request.RequestUri.Query.Contains("key"),"Authorization missing");
                if(wait)await Task.Delay(10000,token);
                return new HttpResponseMessage((HttpStatusCode)code){Content=new StringContent(body)};
            }
        }
    }
}
