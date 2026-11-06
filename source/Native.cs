using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZlhrsHelper {
    internal static class Native {
        internal const int WM_HOTKEY=0x0312;
        [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr h,int id,uint mods,uint key);
        [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr h,int id);
        [DllImport("user32.dll",SetLastError=true)] internal static extern bool SetWindowDisplayAffinity(IntPtr h,uint affinity);
        [DllImport("user32.dll",SetLastError=true)] internal static extern bool GetWindowDisplayAffinity(IntPtr h,out uint affinity);
        [DllImport("user32.dll")] internal static extern bool ReleaseCapture();
        [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr wp,IntPtr lp);
        internal static void Drag(IntPtr handle){ReleaseCapture();SendMessage(handle,0xA1,new IntPtr(2),IntPtr.Zero);}
        internal static bool Exclude(IntPtr handle){uint value;return Environment.OSVersion.Version.Build>=19041&&SetWindowDisplayAffinity(handle,0x11)&&GetWindowDisplayAffinity(handle,out value)&&value==0x11;}
    }
    internal class PrivateForm : Form {
        internal PrivateForm(){Icon=Branding.Icon;Text=Branding.Name;}
        internal bool CaptureExcluded { get; private set; }
        protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);CaptureExcluded=Native.Exclude(Handle);}
    }
}
