using System;
using System.Drawing;
using System.Reflection;

namespace ZlhrsHelper {
    internal sealed class Wallpaper {
        internal readonly string Id,Character,Series;
        Wallpaper(string id,string character,string series){Id=id;Character=character;Series=series;}
        internal static readonly Wallpaper[] All={
            new Wallpaper("green","C.C.","Code Geass"),
            new Wallpaper("light","Сора Касугано","Связанные небом"),
            new Wallpaper("black","Май Сакурадзима","Bunny Girl Senpai"),
            new Wallpaper("gray","Микаса Аккерман","Атака титанов"),
            new Wallpaper("blue","Рем","Re:Zero"),
            new Wallpaper("purple","Синобу Кочо","Клинок, рассекающий демонов"),
            new Wallpaper("pink","Zero Two · 02","Милый во Франксе")
        };
        internal static Wallpaper Find(string id){foreach(var item in All)if(item.Id==id)return item;return All[0];}
        internal Image Load(){
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Wallpaper."+Id)){
                if(stream==null)throw new InvalidOperationException("Не найден фон темы: "+Id);
                using(var image=Image.FromStream(stream))return new Bitmap(image);
            }
        }
    }
}
