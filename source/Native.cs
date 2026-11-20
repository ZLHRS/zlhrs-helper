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
        [DllImport("user32.dll",SetLastError=true)] internal static extern bool SetWindowPos(IntPtr handle,IntPtr after,int x,int y,int width,int height,uint flags);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern IntPtr GetWindow(IntPtr handle,uint command);
        internal static bool RaiseWithoutActivation(IntPtr handle){
            return SetWindowPos(handle,new IntPtr(-1),0,0,0,0,0x0001|0x0002|0x0010|0x0200);
        }
        internal static void Drag(IntPtr handle){ReleaseCapture();SendMessage(handle,0xA1,new IntPtr(2),IntPtr.Zero);}
        internal static bool Exclude(IntPtr handle){uint value;return Environment.OSVersion.Version.Build>=19041&&SetWindowDisplayAffinity(handle,0x11)&&GetWindowDisplayAffinity(handle,out value)&&value==0x11;}
    }
    internal class PrivateForm : Form {
        readonly Timer topmostTimer;
        internal PrivateForm(){
            Icon=Branding.Icon;Text=Branding.Name;TopMost=true;
            topmostTimer=new Timer{Interval=250};
            topmostTimer.Tick+=(s,e)=>MaintainTopmost();
        }
        internal void MaintainTopmost(){
            if(IsDisposed||Disposing||!IsHandleCreated||!Visible||WindowState==FormWindowState.Minimized)return;
            // Preserve the ordering of our own settings, dropdowns and modal dialogs.
            if(Form.ActiveForm is PrivateForm&&Form.ActiveForm!=this)return;
            Native.RaiseWithoutActivation(Handle);
        }
        void UpdateTopmost(){
            if(topmostTimer==null)return;
            topmostTimer.Enabled=Visible&&IsHandleCreated&&WindowState!=FormWindowState.Minimized;
            if(topmostTimer.Enabled)MaintainTopmost();
        }
        protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);UpdateTopmost();}
        protected override void OnResize(EventArgs e){base.OnResize(e);UpdateTopmost();}
        protected override void OnHandleDestroyed(EventArgs e){if(topmostTimer!=null)topmostTimer.Stop();base.OnHandleDestroyed(e);}
        protected override void Dispose(bool disposing){if(disposing&&topmostTimer!=null)topmostTimer.Dispose();base.Dispose(disposing);}

        internal bool CaptureExcluded { get; private set; }
        protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);CaptureExcluded=Native.Exclude(Handle);UpdateTopmost();}
    }
}
