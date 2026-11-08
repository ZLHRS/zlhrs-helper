using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ZlhrsHelper {
    internal sealed class KeyEditor : PrivateForm {
        readonly TextBox[] fields=new TextBox[Settings.SlotCount];
        readonly RadioButton[] slots=new RadioButton[Settings.SlotCount];
        readonly CheckBox rotate;
        internal string[] Keys {get{return fields.Select(f=>f.Text.Trim()).ToArray();}}
        internal int SelectedSlot {get{return Array.FindIndex(slots,r=>r.Checked);}}
        internal bool AutoRotate {get{return rotate.Checked;}}
        internal KeyEditor(string provider,string[] keys,int active,bool autoRotate){
            SuspendLayout();Text="Ключи "+provider;ClientSize=new Size(490,238);StartPosition=FormStartPosition.CenterParent;
            FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=false;
            Font=new Font("Segoe UI",10);
            Controls.Add(new Label{Text="Отметьте активный слот и введите ключи.",Location=new Point(16,13),Size=new Size(455,24)});
            for(int i=0;i<fields.Length;i++){
                slots[i]=new RadioButton{Text="Слот "+(i+1),Location=new Point(16,44+35*i),Size=new Size(86,25),Checked=i==active};
                fields[i]=new TextBox{UseSystemPasswordChar=true,AccessibleName="Ключ слота "+(i+1),Text=keys[i],Location=new Point(106,44+35*i),Width=366,MaxLength=4096};
                Controls.Add(slots[i]);Controls.Add(fields[i]);
            }
            rotate=new CheckBox{Text="Автопереключение при временном лимите",Checked=autoRotate,Location=new Point(16,154),Size=new Size(455,27)};
            Controls.Add(rotate);
            var save=new Button{Text="Сохранить ключи",DialogResult=DialogResult.OK,Location=new Point(176,194),Size=new Size(167,31)};
            var cancel=new Button{Text="Отмена",DialogResult=DialogResult.Cancel,Location=new Point(352,194),Size=new Size(120,31)};
            Controls.Add(save);Controls.Add(cancel);AcceptButton=save;CancelButton=cancel;
            AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ResumeLayout(true);
        }
    }
}
