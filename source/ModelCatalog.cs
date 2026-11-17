using System;

namespace ZlhrsHelper {
    internal static class ModelCatalog {
        internal const string GeminiDefault="gemini-3.5-flash-lite",OpenAiDefault="gpt-6-luna";
        static readonly string[] Gemini={GeminiDefault};
        static readonly string[] OpenAi={OpenAiDefault,"gpt-6.1-terra","gpt-6.1-sol"};
        static string[] Models(string provider){return provider=="OpenAI"?OpenAi:Gemini;}
        internal static string[] Choices(string provider){return (string[])Models(provider).Clone();}
        internal static string Resolve(string provider,string saved){
            var models=Models(provider);
            saved=(saved??"").Trim();
            if(provider=="Gemini"&&saved.StartsWith("models/",StringComparison.Ordinal))saved=saved.Substring(7);
            return Array.IndexOf(models,saved)>=0?saved:models[0];
        }
        internal static string Next(string provider,string current){
            var models=Models(provider);
            int index=Array.IndexOf(models,Resolve(provider,current));
            return models[(index+1)%models.Length];
        }
        internal static void Validate(string provider,string model){
            if(Array.IndexOf(Models(provider),model)<0)throw new InvalidOperationException("Выберите модель из списка.");
        }
    }
}
