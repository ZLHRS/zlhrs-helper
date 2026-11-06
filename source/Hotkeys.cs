using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZlhrsHelper {
    public sealed class HotkeyChord {
        public uint Modifiers;
        public uint Key;
        public HotkeyChord(){}
        public HotkeyChord(uint modifiers,uint key){Modifiers=modifiers;Key=key;}
        public override string ToString(){return ((Modifiers&2)!=0?"Ctrl + ":"")+((Modifiers&1)!=0?"Alt + ":"")+((Modifiers&4)!=0?"Shift + ":"")+((Modifiers&8)!=0?"Win + ":"")+((Keys)Key).ToString();}
        internal static void Validate(HotkeyChord[] chords){
            if(chords==null||chords.Length!=4)throw new InvalidOperationException("Задайте четыре горячие клавиши.");
            var seen=new HashSet<string>();
            foreach(var c in chords){
                if(c==null||c.Key<8||c.Key>254||(c.Modifiers&~15u)!=0)throw new InvalidOperationException("Недопустимая горячая клавиша.");
                if(!seen.Add(c.Modifiers+":"+c.Key))throw new InvalidOperationException("У каждого действия должна быть своя комбинация.");
            }
        }
    }
    internal sealed class HotkeyBox : TextBox {
        [DllImport("user32.dll")] static extern short GetKeyState(int key);
        internal HotkeyChord Chord;
        internal HotkeyBox(HotkeyChord chord){ReadOnly=true;ShortcutsEnabled=false;SetChord(chord);}
        internal void SetChord(HotkeyChord chord){Chord=chord;Text=chord.ToString();}
        protected override bool ProcessCmdKey(ref Message msg,Keys data){
            Keys key=data&Keys.KeyCode;
            if(key==Keys.Tab)return base.ProcessCmdKey(ref msg,data);
            if(key==Keys.ControlKey||key==Keys.ShiftKey||key==Keys.Menu||key==Keys.LWin||key==Keys.RWin)return true;
            uint mods=((data&Keys.Control)!=0?2u:0u)|((data&Keys.Alt)!=0?1u:0u)|((data&Keys.Shift)!=0?4u:0u);
            if(GetKeyState((int)Keys.LWin)<0||GetKeyState((int)Keys.RWin)<0)mods|=8;
            SetChord(new HotkeyChord(mods,(uint)key));return true;
        }
    }
}
