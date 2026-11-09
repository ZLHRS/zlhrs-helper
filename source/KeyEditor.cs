using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ZlhrsHelper {
    internal sealed class KeyEditor : PrivateForm {
        readonly TextBox[] fields=new TextBox[Settings.SlotCount];
        readonly RadioButton[] slots=new RadioButton[Settings.SlotCount];
        readonly CheckBox rotate,rotateEach;
        internal string[] Keys {get{return fields.Select(f=>f.Text.Trim()).ToArray();}}
        internal int SelectedSlot {get{return Array.FindIndex(slots,r=>r.Checked);}}
        internal bool AutoRotate {get{return rotate.Checked;}}
        internal bool RotateEachRequest {get{return rotateEach.Checked;}}
        internal KeyEditor(string provider,string[] keys,int active,bool autoRotate,bool rotateEachRequest,Palette palette){
            SuspendLayout();Text="Ключи "+provider;ClientSize=new Size(510,312);StartPosition=FormStartPosition.CenterParent;
            FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=false;
            Font=new Font("Segoe UI",10);
            Controls.Add(SettingsTheme.Label("Слоты ключей",20,17,470,34,20,SettingsTheme.Text));
            Controls.Add(SettingsTheme.Label(provider+"  ·  Отметьте слот, с которого начнётся запрос.",22,57,466,23,10,SettingsTheme.Muted));
            for(int i=0;i<fields.Length;i++){
                slots[i]=new RadioButton{Text="Слот "+(i+1),FlatStyle=FlatStyle.Flat,Location=new Point(22,87+35*i),Size=new Size(87,25),Checked=i==active};
                fields[i]=new TextBox{UseSystemPasswordChar=true,AccessibleName="Ключ слота "+(i+1),Text=keys[i],Location=new Point(117,87+35*i),Width=371,MaxLength=4096,BorderStyle=BorderStyle.FixedSingle};
                Controls.Add(slots[i]);Controls.Add(fields[i]);
            }
            rotate=new CheckBox{Text="Автопереключение при временном лимите",Checked=autoRotate,FlatStyle=FlatStyle.Flat,Location=new Point(22,200),Size=new Size(466,27)};
            Controls.Add(rotate);
            rotateEach=new CheckBox{Text="Менять ключ после каждого запроса",Checked=rotateEachRequest,FlatStyle=FlatStyle.Flat,Location=new Point(22,229),Size=new Size(466,27)};
            Controls.Add(rotateEach);
            var save=SettingsTheme.Button("Сохранить ключи",171,265,184,true);
            save.Height=32;save.Tag="primary";save.DialogResult=DialogResult.OK;
            var cancel=SettingsTheme.Button("Отмена",365,265,123,false);
            cancel.Height=32;cancel.DialogResult=DialogResult.Cancel;
            Controls.Add(save);Controls.Add(cancel);AcceptButton=save;CancelButton=cancel;
            if(palette!=null)palette.Apply(this);
            AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ResumeLayout(true);
        }
    }
}
