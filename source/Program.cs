using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ZlhrsHelper {
    internal static class Program {
        [STAThread]
        static int Main(string[] args) {
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            if(args.Length>0&&args[0]=="--self-test")return SelfTests.Run(args.Length>1?args[1]:"test-report.txt");
            if(args.Length>1&&args[0]=="--render-cursors"){
                using(var bitmap=new Bitmap(7*140,190))using(var g=Graphics.FromImage(bitmap)){
                    g.Clear(Color.FromArgb(241,235,245));int i=0;
                    foreach(var palette in Palette.All)using(var cursor=CharacterCursor.Create(palette.Id,96)){
                        using(var fill=new SolidBrush(palette.Background))g.FillRectangle(fill,i*140+8,8,124,154);
                        cursor.Value.DrawStretched(g,new Rectangle(i*140+46,24,48,48));
                        cursor.Value.DrawStretched(g,new Rectangle(i*140+22,76,96,96));
                        using(var font=new Font("Segoe UI",9))g.DrawString(palette.Name,font,Brushes.Black,new PointF(i*140+8,174));i++;
                    }
                    bitmap.Save(Path.GetFullPath(args[1]));
                }
                return 0;
            }
            if(args.Length>1&&args[0]=="--render-key-editor"){
                using(var form=new KeyEditor("Gemini",new[]{"sample","","sample"},0,false,true,Palette.Find("pink","green"))){
                    form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);
                    form.Show();form.PerformLayout();Application.DoEvents();
                    using(var bitmap=new Bitmap(form.Width,form.Height)){
                        form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.GetFullPath(args[1]));
                    }
                }
                return 0;
            }
            if(args.Length>1&&args[0]=="--render-preview"){
                using(var form=new MainForm(true)){
                    if(args.Length>2)((ComboBox)form.Controls["MainTheme"]).SelectedItem=Palette.Find(args[2],"green");
                    if(args.Length>3)((ComboBox)form.Controls["AnswerTheme"]).SelectedItem=Palette.Find(args[3],"light");
                    form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);form.Show();
                    int previewWidth,previewHeight;
                    if(args.Length>6&&Int32.TryParse(args[5],out previewWidth)&&Int32.TryParse(args[6],out previewHeight)&&previewWidth>=300&&previewHeight>=240)
                        form.ClientSize=new Size(SettingsLayout.Scale(previewWidth,form.DeviceDpi),SettingsLayout.Scale(previewHeight,form.DeviceDpi));
                    int previewPage;
                    if(args.Length>7&&Int32.TryParse(args[7],out previewPage))form.SelectPage(previewPage);
                    form.PerformLayout();Application.DoEvents();
                    using(var bitmap=new Bitmap(form.Width,form.Height)){
                        form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));
                        if(args.Length>4&&args[4]=="cursor")using(var g=Graphics.FromImage(bitmap))form.Cursor.DrawStretched(g,new Rectangle(655,320,48,48));
                        bitmap.Save(Path.GetFullPath(args[1]));
                    }
                    form.Mini.WantedVisible=false;
                    form.Mini.SetAnswer("1. Сколько будет 2 + 2?\n✓ Б) 4\n\n2. Выберите простые числа.\n✓ А) 2\n✓ В) 5","Снимок 14:32:08 · ответ ИИ");
                    form.Mini.Location=new Point(-30000,-30000);form.Mini.Show();form.Mini.PerformLayout();Application.DoEvents();
                    using(var bitmap=new Bitmap(form.Mini.Width,form.Mini.Height)){form.Mini.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Mini.Size));bitmap.Save(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1])),"mini-answer.png"));}
                    form.Mini.Hide();form.Close();
                }
                return 0;
            }
            bool first;
            using(var mutex=new Mutex(true,"Local\\ZlhrsHelper-Desktop-1",out first)) {
                if(!first)return 0;
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException+=(s,e)=>{
                    using(var error=new PrivateForm{Text=Branding.Name,ClientSize=new Size(480,180),TopMost=true}){
                        error.Controls.Add(new Label{Text="Ошибка приложения: "+e.Exception.Message,Dock=DockStyle.Fill,Padding=new Padding(16)});error.ShowDialog();
                    }
                };
                Application.Run(new MainForm(false));
            }
            return 0;
        }
    }
}
