using System;
using System.Drawing;

namespace ZlhrsHelper {
    // All design coordinates are in 96-DPI units. Painting and control bounds share this layout.
    internal sealed class SettingsLayout {
        internal static readonly Size DesignSize=new Size(990,646);
        readonly int width,height,settingsWidth,dpi;
        internal readonly bool HasSideArt;
        internal int ScrollHeight {get{return Scale(DesignSize.Height,dpi);}}
        internal SettingsLayout(Size viewport,int dpi){
            this.dpi=Math.Max(1,dpi);
            width=Math.Max(1,(int)Math.Floor(viewport.Width*96f/this.dpi));
            height=Math.Max(1,(int)Math.Floor(viewport.Height*96f/this.dpi));
            HasSideArt=width>=850;
            settingsWidth=HasSideArt?width*654/990:width;
        }
        internal static int Scale(int value,int dpi){return (int)Math.Round(value*dpi/96f);}
        int X(int value){return (int)Math.Round(value*settingsWidth/654f);}
        internal Rectangle LogicalBounds(Rectangle design){
            return Rectangle.FromLTRB(X(design.Left),design.Top,X(design.Right),design.Bottom);
        }
        internal Rectangle Place(Rectangle design){
            Rectangle r=LogicalBounds(design);
            return Rectangle.FromLTRB(Scale(r.Left,dpi),Scale(r.Top,dpi),Scale(r.Right,dpi),Scale(r.Bottom,dpi));
        }
        internal Rectangle Portrait(Size image){
            int artHeight=Math.Max(height,1);
            int artWidth=Math.Max(1,(int)Math.Round(artHeight*image.Width/(float)image.Height));
            return new Rectangle(width-artWidth,0,artWidth,artHeight);
        }
    }
}
