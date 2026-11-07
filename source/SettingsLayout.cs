using System;
using System.Drawing;

namespace ZlhrsHelper {
    // All design coordinates are in 96-DPI units. Painting and control bounds share this layout.
    internal sealed class SettingsLayout {
        internal static readonly Size DesignSize=new Size(990,810);
        static readonly int[] NormalRows={0,78,220,377,452,586,665,700,757,810};
        static readonly int[] CompactRows={0,70,190,298,368,480,550,574,621,672};
        readonly int width,height,settingsWidth,dpi;
        readonly bool compact;
        internal readonly bool HasSideArt;
        internal int ScrollHeight {get{return Scale(compact?672:810,dpi);}}
        internal SettingsLayout(Size viewport,int dpi){
            this.dpi=Math.Max(1,dpi);
            width=Math.Max(1,(int)Math.Floor(viewport.Width*96f/this.dpi));
            height=Math.Max(1,(int)Math.Floor(viewport.Height*96f/this.dpi));
            compact=height<810;
            HasSideArt=width>=850;
            settingsWidth=HasSideArt?width*654/990:width;
        }
        internal static int Scale(int value,int dpi){return (int)Math.Round(value*dpi/96f);}
        int X(int value){return (int)Math.Round(value*settingsWidth/654f);}
        int Y(int value){
            if(!compact)return value;
            for(int i=1;i<NormalRows.Length;i++){
                if(value<=NormalRows[i])return CompactRows[i-1]+(int)Math.Round((value-NormalRows[i-1])*(CompactRows[i]-CompactRows[i-1])/(float)(NormalRows[i]-NormalRows[i-1]));
            }
            return CompactRows[CompactRows.Length-1]+value-NormalRows[NormalRows.Length-1];
        }
        internal Rectangle LogicalBounds(Rectangle design){
            int top=Y(design.Top);
            // Keep the heading and provider caption at their full line height.
            int bottom=design.Top<NormalRows[1]?top+design.Height:Y(design.Bottom);
            return Rectangle.FromLTRB(X(design.Left),top,X(design.Right),bottom);
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
