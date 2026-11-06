using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;

namespace ZlhrsHelper {
    internal sealed class CharacterCursor : IDisposable {
        internal readonly string ThemeId;
        internal readonly int Dpi,PixelSize;
        internal readonly Cursor Value;
        readonly CursorHandle nativeHandle;
        CharacterCursor(string id,int dpi,int size,IntPtr handle){ThemeId=id;Dpi=dpi;PixelSize=size;nativeHandle=new CursorHandle(handle);Value=new Cursor(handle);}
        internal static CharacterCursor Create(string id,int dpi){
            var palette=Palette.Find(id,"green");int size=Math.Max(32,Math.Min(128,(int)Math.Round(48*dpi/96f)));
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Cursor."+palette.Id)){
                if(stream==null)throw new InvalidOperationException("Не найден курсор темы: "+palette.Id);
                using(var image=Image.FromStream(stream))using(var sprite=new Bitmap(image))
                using(var canvas=new Bitmap(size,size,PixelFormat.Format32bppArgb)){
                    Rectangle source=VisibleBounds(sprite);
                    using(var g=Graphics.FromImage(canvas)){
                        g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                        float unit=size/48f,ratio=Math.Min(36f/source.Width,42f/source.Height)*unit;
                        float width=source.Width*ratio,height=source.Height*ratio;
                        g.DrawImage(sprite,new RectangleF(10*unit+(36*unit-width)/2,5*unit+(42*unit-height)/2,width,height),source,GraphicsUnit.Pixel);
                        var arrow=new[]{new PointF(unit,unit),new PointF(unit,18*unit),new PointF(6*unit,14*unit),new PointF(10*unit,22*unit),new PointF(14*unit,20*unit),new PointF(10*unit,12*unit),new PointF(18*unit,12*unit)};
                        using(var outline=new Pen(Color.White,3*unit)){outline.LineJoin=LineJoin.Round;g.DrawPolygon(outline,arrow);}
                        using(var fill=new SolidBrush(palette.Accent))g.FillPolygon(fill,arrow);
                        using(var edge=new Pen(Color.FromArgb(45,29,45),unit)){edge.LineJoin=LineJoin.Round;g.DrawPolygon(edge,arrow);}
                    }
                    IntPtr handle=CreateNative(canvas,Math.Max(1,(int)Math.Round(size/48f)));
                    return new CharacterCursor(palette.Id,dpi,size,handle);
                }
            }
        }
        static Rectangle VisibleBounds(Bitmap sprite){
            var bits=sprite.LockBits(new Rectangle(0,0,sprite.Width,sprite.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try{
                int stride=Math.Abs(bits.Stride);var pixels=new byte[stride*sprite.Height];Marshal.Copy(bits.Scan0,pixels,0,pixels.Length);
                int left=sprite.Width,top=sprite.Height,right=-1,bottom=-1;
                for(int y=0;y<sprite.Height;y++)for(int x=0;x<sprite.Width;x++)if(pixels[y*stride+x*4+3]>16){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
                if(right<left)throw new InvalidOperationException("Пустое изображение курсора.");
                return Rectangle.FromLTRB(left,top,right+1,bottom+1);
            }finally{sprite.UnlockBits(bits);}
        }
        static IntPtr CreateNative(Bitmap bitmap,int hotspot){
            IntPtr icon=bitmap.GetHicon();IconInfo info=new IconInfo();
            try{
                if(!GetIconInfo(icon,out info))throw new Win32Exception(Marshal.GetLastWin32Error());
                info.IsIcon=false;info.XHotspot=(uint)hotspot;info.YHotspot=(uint)hotspot;
                IntPtr cursor=CreateIconIndirect(ref info);
                if(cursor==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());
                return cursor;
            }finally{
                if(info.Mask!=IntPtr.Zero)DeleteObject(info.Mask);if(info.Color!=IntPtr.Zero)DeleteObject(info.Color);DestroyIcon(icon);
            }
        }
        internal static void Apply(Control control,Cursor cursor){control.Cursor=cursor;foreach(Control child in control.Controls)Apply(child,cursor);}
        public void Dispose(){Value.Dispose();nativeHandle.Dispose();}
        sealed class CursorHandle : SafeHandleZeroOrMinusOneIsInvalid {
            internal CursorHandle(IntPtr value):base(true){SetHandle(value);}
            protected override bool ReleaseHandle(){return DestroyCursor(handle);}
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct IconInfo { [MarshalAs(UnmanagedType.Bool)] internal bool IsIcon;internal uint XHotspot,YHotspot;internal IntPtr Mask,Color; }
        [DllImport("user32.dll",SetLastError=true)] internal static extern bool GetIconInfo(IntPtr handle,out IconInfo info);
        [DllImport("user32.dll",SetLastError=true)] static extern IntPtr CreateIconIndirect(ref IconInfo info);
        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr handle);
        [DllImport("user32.dll")] static extern bool DestroyCursor(IntPtr handle);
        [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr handle);
    }
}
