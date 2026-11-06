using System.Drawing;
using System.Reflection;

[assembly: AssemblyTitle("NVIDIA Helper")]
[assembly: AssemblyDescription("NVIDIA Helper")]
[assembly: AssemblyProduct("NVIDIA Helper")]
[assembly: AssemblyCompany("ZLHRS")]
[assembly: AssemblyVersion("1.10.0.0")]
[assembly: AssemblyFileVersion("1.10.0.0")]

namespace ZlhrsHelper {
    internal static class Branding {
        internal const string Name="NVIDIA Helper";
        internal static readonly Icon Icon=LoadIcon();
        static Icon LoadIcon(){
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("AppIcon"))
            using(var icon=new Icon(stream,new Size(32,32)))return (Icon)icon.Clone();
        }
    }
}
