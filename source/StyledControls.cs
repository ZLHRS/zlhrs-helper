using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZlhrsHelper {
    internal static class Shapes {
        internal static GraphicsPath Round(RectangleF bounds,float radius){
            float d=Math.Min(radius*2,Math.Min(bounds.Width,bounds.Height));var path=new GraphicsPath();
            path.AddArc(bounds.X,bounds.Y,d,d,180,90);path.AddArc(bounds.Right-d,bounds.Y,d,d,270,90);
            path.AddArc(bounds.Right-d,bounds.Bottom-d,d,d,0,90);path.AddArc(bounds.X,bounds.Bottom-d,d,d,90,90);path.CloseFigure();return path;
        }
    }
    internal sealed class RoundedButton : Button {
        bool hover,pressed;
        internal RoundedButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
        protected override void OnResize(EventArgs e){
            base.OnResize(e);if(Width<=0||Height<=0)return;
            using(var path=Shapes.Round(new RectangleF(0,0,Width,Height),10*DeviceDpi/96f)){var previous=Region;Region=new Region(path);if(previous!=null)previous.Dispose();}
        }
        protected override void OnMouseEnter(EventArgs e){hover=true;base.OnMouseEnter(e);Invalidate();}
        protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;base.OnMouseLeave(e);Invalidate();}
        protected override void OnMouseDown(MouseEventArgs e){pressed=e.Button==MouseButtons.Left;base.OnMouseDown(e);Invalidate();}
        protected override void OnMouseUp(MouseEventArgs e){pressed=false;base.OnMouseUp(e);Invalidate();}
        protected override void OnPaint(PaintEventArgs e){
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            Color fill=pressed?FlatAppearance.MouseDownBackColor:hover?FlatAppearance.MouseOverBackColor:BackColor;
            using(var path=Shapes.Round(new RectangleF(.5f,.5f,Width-1,Height-1),10*DeviceDpi/96f)){
                using(var brush=new SolidBrush(fill))e.Graphics.FillPath(brush,path);
                if(!Equals(Tag,"primary"))using(var pen=new Pen(FlatAppearance.BorderColor))e.Graphics.DrawPath(pen,path);
            }
            TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);
            if(Focused&&ShowFocusCues){var bounds=ClientRectangle;bounds.Inflate(-5,-5);ControlPaint.DrawFocusRectangle(e.Graphics,bounds,ForeColor,fill);}
        }
    }
    internal sealed class PaletteSlider : Control {
        int value;
        internal int Minimum=0,Maximum=100;
        internal Color Accent=Color.HotPink,Track=Color.Pink;
        internal event EventHandler ValueChanged;
        internal int Value {
            get{return value;}
            set{int next=Math.Max(Minimum,Math.Min(Maximum,value));if(this.value==next)return;this.value=next;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}
        }
        internal PaletteSlider(){Height=38;TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.Slider;SetStyle(ControlStyles.Selectable|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
        protected override bool IsInputKey(Keys keyData){return keyData==Keys.Left||keyData==Keys.Right||keyData==Keys.Up||keyData==Keys.Down||keyData==Keys.Home||keyData==Keys.End||base.IsInputKey(keyData);}
        protected override void OnKeyDown(KeyEventArgs e){
            base.OnKeyDown(e);
            if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down)Value--;
            else if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Up)Value++;
            else if(e.KeyCode==Keys.Home)Value=Minimum;
            else if(e.KeyCode==Keys.End)Value=Maximum;
            else return;
            e.Handled=true;e.SuppressKeyPress=true;
        }
        void SetFromMouse(int x){float pad=12*DeviceDpi/96f;Value=Minimum+(int)Math.Round((Maximum-Minimum)*Math.Max(0,Math.Min(1,(x-pad)/Math.Max(1,Width-2*pad))));}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();Capture=true;SetFromMouse(e.X);}}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture)SetFromMouse(e.X);}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left)Capture=false;}
        protected override void OnPaint(PaintEventArgs e){
            base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            float scale=DeviceDpi/96f,pad=12*scale,y=Height/2f,x=pad+(Width-2*pad)*(Value-Minimum)/(float)Math.Max(1,Maximum-Minimum);
            using(var rail=new Pen(Track,5*scale)){rail.StartCap=rail.EndCap=LineCap.Round;e.Graphics.DrawLine(rail,pad,y,Width-pad,y);}
            using(var active=new Pen(Accent,5*scale)){active.StartCap=active.EndCap=LineCap.Round;e.Graphics.DrawLine(active,pad,y,x,y);}
            using(var shadow=new SolidBrush(Color.FromArgb(35,Accent)))e.Graphics.FillEllipse(shadow,x-12*scale,y-12*scale,24*scale,24*scale);
            using(var thumb=new SolidBrush(Accent))e.Graphics.FillEllipse(thumb,x-8*scale,y-8*scale,16*scale,16*scale);
            if(Focused&&ShowFocusCues){var bounds=ClientRectangle;bounds.Inflate(-2,-2);ControlPaint.DrawFocusRectangle(e.Graphics,bounds);}
        }
    }
}
