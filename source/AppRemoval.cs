using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ZlhrsHelper {
    internal static class AppRemoval {
        static string Literal(string value){return "'"+value.Replace("'","''")+"'";}
        internal static string Script(string executable,string settings,int processId){
            executable=Path.GetFullPath(executable);
            settings=Path.GetFullPath(settings);
            // Delete only these three exact files; never recurse into the project directory.
            return "$ErrorActionPreference='Stop'; Wait-Process -Id "+processId+" -ErrorAction SilentlyContinue; "+
                "$targets=@("+Literal(executable)+","+Literal(executable+".config")+","+Literal(settings)+"); "+
                "foreach($target in $targets){ for($attempt=0;$attempt -lt 30;$attempt++){ try { if(Test-Path -LiteralPath $target -PathType Leaf){Remove-Item -LiteralPath $target -Force}; break } catch { Start-Sleep -Milliseconds 500 } } }";
        }
        internal static Process Start(string executable,string settings,int processId){
            string script=Script(executable,settings,processId);
            var info=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe"),
                "-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand "+Convert.ToBase64String(Encoding.Unicode.GetBytes(script))){
                UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,
                WorkingDirectory=Path.GetTempPath()
            };
            var process=Process.Start(info);
            if(process==null)throw new InvalidOperationException("Removal helper did not start.");
            return process;
        }
    }
}
