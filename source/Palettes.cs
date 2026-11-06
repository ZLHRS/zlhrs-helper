using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZlhrsHelper {
    internal sealed class Palette {
        internal readonly string Id,Name;
        internal readonly Color Background,Surface,Text,Muted,Accent,AccentText,Border;
        Palette(string id,string name,int background,int surface,int text,int muted,int accent,int accentText,int border){
            Id=id;Name=name;Background=Color.FromArgb(background|unchecked((int)0xff000000));Surface=Color.FromArgb(surface|unchecked((int)0xff000000));
            Text=Color.FromArgb(text|unchecked((int)0xff000000));Muted=Color.FromArgb(muted|unchecked((int)0xff000000));
            Accent=Color.FromArgb(accent|unchecked((int)0xff000000));AccentText=Color.FromArgb(accentText|unchecked((int)0xff000000));Border=Color.FromArgb(border|unchecked((int)0xff000000));
        }
        internal static readonly Palette[] All={
            new Palette("green","Зелёная",0x080d0a,0x111b14,0xcbefd5,0x9cbea5,0x59f787,0x080d0a,0x30633e),
            new Palette("light","Светлая",0xffffff,0xf1f4f7,0x19232e,0x4e6070,0x2563eb,0xffffff,0xb8c5d1),
            new Palette("black","Чёрная",0x000000,0x151515,0xf5f5f5,0xb5b5b5,0xe5e5e5,0x101010,0x505050),
            new Palette("gray","Графитовая",0x202329,0x2c3038,0xf2f4f8,0xb5bdcb,0x9cbaff,0x14213b,0x535c6c),
            new Palette("blue","Синяя",0x0c1528,0x152540,0xe2edff,0xa3badb,0x66b7ff,0x071b30,0x375a87),
            new Palette("purple","Фиолетовая",0x1a1228,0x2a1d3e,0xf0e7ff,0xc2acd9,0xc19aff,0x241136,0x654b83),
            new Palette("pink","Розовая",0xf5a6cd,0xffd3e8,0x551432,0x752345,0xc21b69,0xffffff,0xdb77a7)
        };
        public override string ToString(){return Name;}
        internal static Palette Find(string id,string fallback){
            foreach(var palette in All)if(palette.Id==id)return palette;
            foreach(var palette in All)if(palette.Id==fallback)return palette;
            return All[0];
        }
        internal void Apply(Control root){
            root.BackColor=Background;root.ForeColor=Text;
            foreach(Control control in root.Controls){
                Apply(control);
                var label=control as Label;
                if(label!=null){label.BackColor=Color.Transparent;label.ForeColor=Equals(label.Tag,"accent")?Accent:Equals(label.Tag,"text")?Text:Muted;}
                var button=control as Button;
                if(button!=null){
                    bool primary=Equals(button.Tag,"primary");button.UseVisualStyleBackColor=false;
                    button.BackColor=primary?Accent:Surface;button.ForeColor=primary?AccentText:Text;
                    button.FlatAppearance.BorderColor=Border;
                    button.FlatAppearance.MouseOverBackColor=ControlPaint.Light(button.BackColor,.12f);
                    button.FlatAppearance.MouseDownBackColor=ControlPaint.Dark(button.BackColor,.12f);
                }
                if(control is TextBoxBase||control is ComboBox){control.BackColor=Surface;control.ForeColor=Text;}
                var combo=control as ComboBox;
                if(combo!=null){combo.FlatStyle=FlatStyle.Flat;combo.ItemHeight=24*combo.DeviceDpi/96;combo.DrawMode=DrawMode.OwnerDrawFixed;combo.DrawItem-=DrawCombo;combo.DrawItem+=DrawCombo;}
                var slider=control as PaletteSlider;
                if(slider!=null){slider.BackColor=Color.FromArgb((Background.R+Surface.R)/2,(Background.G+Surface.G)/2,(Background.B+Surface.B)/2);slider.Accent=Accent;slider.Track=Border;slider.Invalidate();}
            }
            root.Invalidate(true);
        }
        static void DrawCombo(object sender,DrawItemEventArgs e){
            var combo=(ComboBox)sender;var palette=combo.Tag as Palette;
            if(palette==null)return;
            bool selected=(e.State&DrawItemState.Selected)!=0;
            using(var brush=new SolidBrush(selected?palette.Accent:palette.Surface))e.Graphics.FillRectangle(brush,e.Bounds);
            string text=e.Index>=0?combo.GetItemText(combo.Items[e.Index]):combo.Text;
            var bounds=e.Bounds;bounds.X+=6;bounds.Width-=6;
            var choice=e.Index>=0?combo.Items[e.Index] as Palette:null;
            if(choice!=null){using(var dot=new SolidBrush(choice.Accent))e.Graphics.FillEllipse(dot,bounds.X+1,bounds.Y+(bounds.Height-10)/2,10,10);bounds.X+=21;bounds.Width-=21;}
            TextRenderer.DrawText(e.Graphics,text,e.Font,bounds,selected?palette.AccentText:palette.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);
            e.DrawFocusRectangle();
        }
        internal void SetComboPalettes(Control root){
            foreach(Control control in root.Controls){var combo=control as ComboBox;if(combo!=null)combo.Tag=this;SetComboPalettes(control);}
        }
    }
}
