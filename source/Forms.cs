using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZlhrsHelper {
    internal static class Theme {
        internal static Color Background=Color.White,Surface=Color.FromArgb(244,244,244),Text=Color.Black,Muted=Color.Black,Accent=Color.FromArgb(225,225,225);
        internal static Label Label(string text,int x,int y,int w,int h,float size,Color color){return new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=new Font("Segoe UI",size),ForeColor=color,BackColor=Color.Transparent};}
        internal static Button Button(string text,int x,int y,int w,bool primary){var b=new Button{Text=text,Location=new Point(x,y),Size=new Size(w,40),FlatStyle=FlatStyle.Flat,BackColor=primary?Accent:Surface,ForeColor=Text,Cursor=Cursors.Hand};b.FlatAppearance.BorderSize=1;b.FlatAppearance.BorderColor=Color.FromArgb(200,200,200);return b;}
    }
    internal sealed class AnswerForm : PrivateForm {
        readonly Label body;
        readonly Panel viewport;
        readonly Button hide;
        Palette palette=Palette.Find("light","light");
        internal bool WantedVisible=false;
        internal AnswerForm(){
            SuspendLayout();
            FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;
            ClientSize=new Size(440,292);MinimumSize=new Size(340,200);BackColor=Color.White;Opacity=.88;
            hide=Theme.Button("×",397,5,36,false);hide.Size=new Size(34,29);hide.BackColor=BackColor;hide.Anchor=AnchorStyles.Top|AnchorStyles.Right;hide.Click+=(s,e)=>SetVisible(false);Controls.Add(hide);
            hide.FlatAppearance.BorderSize=0;
            viewport=new Panel{Location=new Point(16,38),Size=new Size(408,238),AutoScroll=true,BackColor=BackColor,Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right};Controls.Add(viewport);
            body=new Label{Location=Point.Empty,AutoSize=true,MaximumSize=new Size(384,0),BackColor=BackColor,ForeColor=Color.Black,Font=new Font("Segoe UI",11),UseMnemonic=false};viewport.Controls.Add(body);
            viewport.SizeChanged+=(s,e)=>ResizeAnswerText();
            MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left)Native.Drag(Handle);};
            viewport.MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left)Native.Drag(Handle);};
            body.MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left)Native.Drag(Handle);};
            body.Text="";
            SetTheme("light");
            AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
            ResumeLayout(true);ResizeAnswerText();
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams { get { var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p; } }
        protected override void WndProc(ref Message m){
            if(m.Msg==0x84){base.WndProc(ref m);Point p=PointToClient(new Point((short)m.LParam.ToInt32(),(short)(m.LParam.ToInt32()>>16)));int grip=Math.Max(14,DeviceDpi*14/96);if(p.X>=ClientSize.Width-grip&&p.Y>=ClientSize.Height-grip)m.Result=new IntPtr(17);return;}
            if(m.Msg==0x21){m.Result=new IntPtr(3);return;}base.WndProc(ref m);
        }
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(palette.Border))e.Graphics.DrawRectangle(p,0,0,ClientSize.Width-1,ClientSize.Height-1);}
        internal void SetTheme(string id){
            palette=Palette.Find(id,"light");BackColor=palette.Background;ForeColor=palette.Text;
            viewport.BackColor=palette.Background;body.BackColor=palette.Background;body.ForeColor=palette.Text;
            hide.BackColor=palette.Background;hide.ForeColor=palette.Text;
            hide.FlatAppearance.MouseOverBackColor=palette.Surface;hide.FlatAppearance.MouseDownBackColor=palette.Border;
            Invalidate(true);
        }
        void ResizeAnswerText(){body.MaximumSize=new Size(Math.Max(1,viewport.ClientSize.Width-24*DeviceDpi/96),0);}
        internal void PlaceOn(Screen screen){
            var r=screen.WorkingArea;int pad=22*DeviceDpi/96;
            Location=new Point(Math.Max(r.Left,r.Right-Width-pad),Math.Max(r.Top,r.Bottom-Height-pad));
        }
        internal void SetVisible(bool visible){WantedVisible=visible;RestoreVisibility();}
        internal void RestoreVisibility(){if(WantedVisible&&Opacity>0){if(!Visible)Show();}else Hide();}
        internal void SetAnswer(string text,string caption){body.Text=text;viewport.AutoScrollPosition=Point.Empty;RestoreVisibility();}
        internal void SetOpacity(int value){Opacity=Math.Max(0,Math.Min(1,value/100.0));RestoreVisibility();}
        internal string BodyText { get { return body.Text; } }
    }
    internal static class SettingsTheme {
        internal static Color Background=Color.FromArgb(8,13,10),Surface=Color.FromArgb(17,27,20),Text=Color.FromArgb(203,239,213),Muted=Color.FromArgb(140,181,150),Accent=Color.FromArgb(89,247,135);
        internal static Label Label(string text,int x,int y,int w,int h,float size,Color color){return new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=new Font("Segoe UI",size,size>=24?FontStyle.Bold:FontStyle.Regular),ForeColor=color,BackColor=Color.Transparent,Tag=color==Accent?"accent":color==Text?"text":"muted"};}
        internal static Button Button(string text,int x,int y,int w,bool primary){var b=new RoundedButton{Text=text,Location=new Point(x,y),Size=new Size(w,40),FlatStyle=FlatStyle.Flat,BackColor=primary?Accent:Surface,ForeColor=primary?Background:Text,Cursor=Cursors.Hand,Font=new Font("Segoe UI",10,FontStyle.Bold)};b.FlatAppearance.BorderSize=1;b.FlatAppearance.BorderColor=Color.FromArgb(48,99,62);b.FlatAppearance.MouseOverBackColor=primary?Color.FromArgb(125,255,161):Color.FromArgb(28,49,34);return b;}
    }
    internal sealed class MainForm : PrivateForm {
        Settings settings;
        Settings draft;
        readonly TextBox key,systemPrompt;
        readonly ComboBox model,provider,mainTheme,answerTheme; readonly HotkeyBox[] hotkeys=new HotkeyBox[4]; readonly Label opacityLabel,keyLabel;
        readonly PaletteSlider opacity;
        readonly Label state,protection;
        readonly NotifyIcon tray;
        readonly AnswerForm mini=new AnswerForm();
        CancellationTokenSource request;
        bool closing,capturing;
        int generation;
        readonly bool preview;
        Wallpaper wallpaper;
        Image wallpaperImage;
        Palette mainPalette;
        CharacterCursor characterCursor;
        Dictionary<Control,Rectangle> designBounds;
        SettingsLayout layout;
        Rectangle lastWorkArea;
        bool applyingLayout, fitting, fitQueued;
        internal string WallpaperId { get { return wallpaper==null?"":wallpaper.Id; } }
        internal string CursorThemeId { get { return characterCursor==null?"":characterCursor.ThemeId; } }
        protected override bool ShowWithoutActivation { get { return preview; } }
        internal MainForm(bool previewOnly){
            preview=previewOnly;SuspendLayout();
            Text=Branding.Name;ClientSize=SettingsLayout.DesignSize;FormBorderStyle=FormBorderStyle.Sizable;StartPosition=FormStartPosition.CenterScreen;
            SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);
            Font=new Font("Segoe UI",10);BackColor=SettingsTheme.Background;ForeColor=SettingsTheme.Text;
            string warning="";
            try{settings=preview?new Settings():Settings.Load();}catch{settings=new Settings();warning="Настройки не удалось прочитать. Введите ключ заново.";}
            draft=Json.New().Deserialize<Settings>(Json.New().Serialize(settings));
            Controls.Add(SettingsTheme.Label("by ZLHRS",28,20,340,45,28,SettingsTheme.Accent));
            Controls.Add(SettingsTheme.Label("Провайдер API",390,12,234,22,9,SettingsTheme.Muted));
            provider=new ComboBox{Name="Provider",Location=new Point(390,38),Width=234,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=SettingsTheme.Surface,ForeColor=SettingsTheme.Text};
            provider.Items.AddRange(new[]{"OpenAI","Gemini"});provider.SelectedItem=settings.Provider=="OpenAI"?"OpenAI":"Gemini";Controls.Add(provider);
            keyLabel=SettingsTheme.Label("",30,88,290,24,9,SettingsTheme.Muted);Controls.Add(keyLabel);
            var editKeys=SettingsTheme.Button("Ключи…",328,82,128,false);editKeys.Height=28;editKeys.Click+=(s,e)=>EditKeys();Controls.Add(editKeys);
            var nextKey=SettingsTheme.Button("Следующий ключ",466,82,158,false);nextKey.Height=28;nextKey.Click+=(s,e)=>NextKey();Controls.Add(nextKey);
            key=new TextBox{Location=new Point(30,115),Size=new Size(594,30),UseSystemPasswordChar=true,BackColor=SettingsTheme.Surface,ForeColor=SettingsTheme.Text,BorderStyle=BorderStyle.FixedSingle};
            try{key.Text=settings.Key;}catch{warning="Сохранённый ключ недоступен. Введите его заново.";}Controls.Add(key);
            Controls.Add(SettingsTheme.Label("Модель",30,155,594,24,10,SettingsTheme.Muted));
            model=new ComboBox{Name="Model",Location=new Point(30,182),Width=594,DropDownStyle=ComboBoxStyle.DropDown,BackColor=SettingsTheme.Surface,ForeColor=SettingsTheme.Text};
            Controls.Add(model);LoadProvider();
            provider.SelectedIndexChanged+=(s,e)=>{StoreProvider();draft.Provider=provider.Text;LoadProvider();};
            string[] names={"Снимок → ответ","Скрыть / показать ответ","Отмена и очистка","Открыть настройки"};
            var chords=settings.GetHotkeys();
            for(int i=0;i<4;i++){
                int x=i%2==0?30:330,y=458+(i/2)*63;
                Controls.Add(SettingsTheme.Label(names[i],x,y,294,22,10,SettingsTheme.Muted));
                var box=new HotkeyBox(chords[i]){Location=new Point(x,y+25),Width=294,BackColor=SettingsTheme.Surface,ForeColor=SettingsTheme.Text,BorderStyle=BorderStyle.FixedSingle};hotkeys[i]=box;Controls.Add(box);
                box.Enter+=(s,e)=>{if(!preview)UnregisterKeys();};
                box.Leave+=(s,e)=>{if(!preview){try{RegisterKeys(settings.GetHotkeys());}catch(Exception ex){state.Text=ex.Message;}}};
            }
            opacityLabel=SettingsTheme.Label("",30,594,594,24,10,SettingsTheme.Muted);Controls.Add(opacityLabel);
            opacity=new PaletteSlider{Location=new Point(25,623),Width=599,Minimum=0,Maximum=100,Value=Math.Max(0,Math.Min(100,settings.Opacity)),BackColor=SettingsTheme.Background};
            opacity.ValueChanged+=(s,e)=>{mini.SetOpacity(opacity.Value);UpdateOpacityLabel();};Controls.Add(opacity);UpdateOpacityLabel();
            protection=SettingsTheme.Label("",30,665,425,24,10,SettingsTheme.Text);Controls.Add(protection);
            var update=SettingsTheme.Button("Обновить",465,665,159,false);update.Height=28;update.Click+=(s,e)=>StartUpdate();Controls.Add(update);
            var save=SettingsTheme.Button("Сохранить",30,700,240,true);save.Tag="primary";save.Click+=(s,e)=>{if(ApplySettings()){Hide();mini.SetVisible(true);}};Controls.Add(save);
            var show=SettingsTheme.Button("Показать ответ",282,700,213,false);show.Click+=(s,e)=>mini.SetVisible(true);Controls.Add(show);
            var demo=SettingsTheme.Button("Демо",507,700,117,false);demo.Click+=(s,e)=>Demo();Controls.Add(demo);
            state=SettingsTheme.Label(warning,30,757,594,45,10,SettingsTheme.Text);Controls.Add(state);
            Controls.Add(SettingsTheme.Label("Системный промпт",30,224,594,24,10,SettingsTheme.Text));
            systemPrompt=new TextBox{Text=settings.SystemPrompt??"",Name="SystemPrompt",Location=new Point(30,252),Size=new Size(594,110),Multiline=true,AcceptsReturn=true,ScrollBars=ScrollBars.Vertical,MaxLength=200000,BackColor=SettingsTheme.Surface,ForeColor=SettingsTheme.Text,BorderStyle=BorderStyle.FixedSingle};Controls.Add(systemPrompt);
            Controls.Add(SettingsTheme.Label("Тема основного окна",30,383,284,22,10,SettingsTheme.Muted));
            Controls.Add(SettingsTheme.Label("Тема окна ответа",330,383,294,22,10,SettingsTheme.Muted));
            mainTheme=CreateThemePicker("MainTheme",30,408,284,settings.MainTheme,"green");
            answerTheme=CreateThemePicker("AnswerTheme",330,408,294,settings.AnswerTheme,"light");
            mainTheme.SelectedIndexChanged+=(s,e)=>{draft.MainTheme=((Palette)mainTheme.SelectedItem).Id;ApplyMainTheme();};
            answerTheme.SelectedIndexChanged+=(s,e)=>{draft.AnswerTheme=((Palette)answerTheme.SelectedItem).Id;mini.SetTheme(draft.AnswerTheme);};
            ApplyMainTheme();mini.SetTheme(((Palette)answerTheme.SelectedItem).Id);
            tray=new NotifyIcon{Icon=Branding.Icon,Text=Branding.Name,Visible=!preview};
            // Menus are separate top-level native windows; protect their handles as well.
            var menu=new ContextMenuStrip{BackColor=Color.White,ForeColor=Color.Black,RenderMode=ToolStripRenderMode.System};menu.Opened+=(s,e)=>Native.Exclude(menu.Handle);
            menu.Items.Add("Показать / скрыть ответ",null,(s,e)=>Toggle());menu.Items.Add("Настройки",null,(s,e)=>ShowSettings());menu.Items.Add("Демо без API",null,(s,e)=>Demo());menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Выход",null,(s,e)=>{closing=true;Close();});tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>Toggle();
            mini.PlaceOn(Screen.PrimaryScreen);mini.SetOpacity(settings.Opacity);
            if(!preview)Shown+=(s,e)=>{
                try{RegisterKeys(settings.GetHotkeys());}catch(Exception ex){state.Text=ex.Message;}
                mini.WantedVisible=true;
                UpdateProtection();
            };
            FormClosing+=(s,e)=>{if(!closing&&!preview&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}else Cleanup();};
            if(!preview){
                Deactivate+=(s,e)=>{if(!closing){try{RegisterKeys(settings.GetHotkeys());}catch(Exception ex){state.Text=ex.Message;}}};
                Activated+=(s,e)=>{foreach(var box in hotkeys)if(box.Focused){UnregisterKeys();break;}};
            }
            designBounds=new Dictionary<Control,Rectangle>();
            foreach(Control control in Controls){
                Rectangle bounds=control.Bounds;
                if(control is ComboBox)bounds.Height=30;
                if(control is TextBox&&!((TextBox)control).Multiline)bounds.Height=25;
                designBounds.Add(control,bounds);
            }
            AutoScroll=true;
            AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
            ResumeLayout(true);Reflow();
        }
        protected override void OnLoad(EventArgs e){
            base.OnLoad(e);
            if(!preview)FitToMonitor(true);
            Reflow();ApplyCharacterCursor();
        }
        protected override void OnClientSizeChanged(EventArgs e){base.OnClientSizeChanged(e);Reflow();}
        protected override void OnScroll(ScrollEventArgs se){base.OnScroll(se);Invalidate(true);}
        protected override void OnLocationChanged(EventArgs e){
            base.OnLocationChanged(e);
            if(!preview&&!fitting&&Visible&&IsHandleCreated&&Screen.FromHandle(Handle).WorkingArea!=lastWorkArea)QueueFit();
        }
        void QueueFit(){
            if(preview||fitQueued||!IsHandleCreated||IsDisposed)return;
            fitQueued=true;
            BeginInvoke((MethodInvoker)delegate{
                fitQueued=false;if(IsDisposed||!IsHandleCreated)return;
                FitToMonitor(false);Reflow();ApplyCharacterCursor();
            });
        }
        void FitToMonitor(bool initial){
            if(fitting||WindowState!=FormWindowState.Normal)return;
            fitting=true;
            try{
                Rectangle work=Screen.FromHandle(Handle).WorkingArea;lastWorkArea=work;
                Size frame=SizeFromClientSize(Size.Empty);
                int margin=SettingsLayout.Scale(12,DeviceDpi);
                Size usable=new Size(Math.Max(1,work.Width-frame.Width-2*margin),Math.Max(1,work.Height-frame.Height-2*margin));
                MinimumSize=new Size(Math.Min(usable.Width,SettingsLayout.Scale(430,DeviceDpi))+frame.Width,
                    Math.Min(usable.Height,SettingsLayout.Scale(320,DeviceDpi))+frame.Height);
                Size target=initial?SettingsLayout.DesignSize:ClientSize;
                if(initial)target=new Size(SettingsLayout.Scale(target.Width,DeviceDpi),SettingsLayout.Scale(target.Height,DeviceDpi));
                ClientSize=new Size(Math.Min(target.Width,usable.Width),Math.Min(target.Height,usable.Height));
                int x=initial?work.Left+(work.Width-Width)/2:Left;
                int y=initial?work.Top+(work.Height-Height)/2:Top;
                Location=new Point(Math.Max(work.Left,Math.Min(x,work.Right-Width)),Math.Max(work.Top,Math.Min(y,work.Bottom-Height)));
            }finally{fitting=false;}
        }
        void Reflow(){
            if(designBounds==null||applyingLayout||IsDisposed||ClientSize.Width<=0||ClientSize.Height<=0)return;
            applyingLayout=true;
            try{
                // Scrollbar appearance can change the available client width. Reflow against the final viewport.
                for(int pass=0;pass<3;pass++){
                    Size before=ClientSize;
                    layout=new SettingsLayout(before,DeviceDpi);
                    SuspendLayout();
                    try{
                        AutoScrollMinSize=new Size(0,layout.ScrollHeight);
                        Point offset=AutoScrollPosition;
                        foreach(var entry in designBounds){
                            Rectangle bounds=layout.Place(entry.Value);
                            bounds.Offset(offset);
                            var combo=entry.Key as ComboBox;
                            int itemHeight=SettingsLayout.Scale(24,DeviceDpi);
                            if(combo!=null&&combo.ItemHeight!=itemHeight)combo.ItemHeight=itemHeight;
                            entry.Key.Bounds=bounds;
                        }
                    }finally{ResumeLayout(true);}
                    if(before==ClientSize)break;
                }
                Invalidate(true);
            }finally{applyingLayout=false;}
        }
        ComboBox CreateThemePicker(string name,int x,int y,int width,string id,string fallback){
            var picker=new ComboBox{Name=name,Location=new Point(x,y),Width=width,DropDownStyle=ComboBoxStyle.DropDownList};
            picker.Items.AddRange(Palette.All);picker.SelectedItem=Palette.Find(id,fallback);Controls.Add(picker);return picker;
        }
        void ApplyMainTheme(){
            var palette=(Palette)mainTheme.SelectedItem;
            mainPalette=palette;
            palette.SetComboPalettes(this);palette.Apply(this);
            if(wallpaper==null||wallpaper.Id!=palette.Id){
                var next=Wallpaper.Find(palette.Id);var image=next.Load();
                var previous=wallpaperImage;wallpaperImage=image;wallpaper=next;if(previous!=null)previous.Dispose();
            }
            ApplyCharacterCursor();
            Invalidate(true);
        }
        void ApplyCharacterCursor(){
            if(mainPalette==null)return;
            if(characterCursor!=null&&characterCursor.ThemeId==mainPalette.Id&&characterCursor.Dpi==DeviceDpi)return;
            var next=CharacterCursor.Create(mainPalette.Id,DeviceDpi);var previous=characterCursor;
            characterCursor=next;CharacterCursor.Apply(this,next.Value);
            if(previous!=null){if(System.Windows.Forms.Cursor.Current!=null&&System.Windows.Forms.Cursor.Current.Handle==previous.Value.Handle)System.Windows.Forms.Cursor.Current=next.Value;previous.Dispose();}
        }
        protected override void OnDpiChanged(DpiChangedEventArgs e){
            base.OnDpiChanged(e);
            if(preview){Reflow();ApplyCharacterCursor();}else QueueFit();
        }
        protected override void OnPaintBackground(PaintEventArgs e){
            base.OnPaintBackground(e);
            if(wallpaperImage==null||mainPalette==null||layout==null)return;
            var saved=e.Graphics.Save();
            try{
                float scale=DeviceDpi/96f;
                e.Graphics.ScaleTransform(scale,scale);
                e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
                Rectangle art=layout.Portrait(wallpaperImage.Size);
                if(layout.HasSideArt){
                    e.Graphics.DrawImage(wallpaperImage,art);
                    int width=Math.Min(280,art.Width);
                    using(var fade=new LinearGradientBrush(new Rectangle(art.Left,0,width,art.Height),mainPalette.Background,Color.FromArgb(0,mainPalette.Background),LinearGradientMode.Horizontal)){
                        fade.InterpolationColors=new ColorBlend{Positions=new[]{0f,.6f,1f},Colors=new[]{mainPalette.Background,Color.FromArgb(245,mainPalette.Background),Color.FromArgb(0,mainPalette.Background)}};
                        e.Graphics.FillRectangle(fade,art.Left,0,width,art.Height);
                    }
                }else{
                    using(var tint=new ImageAttributes()){
                        tint.SetColorMatrix(new ColorMatrix{Matrix33=.22f});
                        e.Graphics.DrawImage(wallpaperImage,art,0,0,wallpaperImage.Width,wallpaperImage.Height,GraphicsUnit.Pixel,tint);
                    }
                }
                var cardColor=Color.FromArgb((mainPalette.Background.R+mainPalette.Surface.R)/2,(mainPalette.Background.G+mainPalette.Surface.G)/2,(mainPalette.Background.B+mainPalette.Surface.B)/2);
                var cards=new[]{new Rectangle(20,78,616,140),new Rectangle(20,220,616,151),new Rectangle(20,377,616,69),new Rectangle(20,452,616,126),new Rectangle(20,586,616,91)};
                e.Graphics.TranslateTransform(AutoScrollPosition.X/scale,AutoScrollPosition.Y/scale);
                using(var fill=new SolidBrush(cardColor))using(var border=new Pen(Color.FromArgb(85,mainPalette.Border)))foreach(var card in cards)
                    using(var shape=Shapes.Round(layout.LogicalBounds(card),13)){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.FillPath(fill,shape);e.Graphics.DrawPath(border,shape);}
            }finally{e.Graphics.Restore(saved);}
        }
        protected override void Dispose(bool disposing){
            if(disposing&&characterCursor!=null){
                CharacterCursor.Apply(this,Cursors.Default);
                if(System.Windows.Forms.Cursor.Current!=null&&System.Windows.Forms.Cursor.Current.Handle==characterCursor.Value.Handle)System.Windows.Forms.Cursor.Current=Cursors.Default;
                characterCursor.Dispose();characterCursor=null;
            }
            if(disposing&&wallpaperImage!=null){wallpaperImage.Dispose();wallpaperImage=null;}
            base.Dispose(disposing);
        }
        void StoreProvider(){
            draft.SetKey(key.Text);
            if(draft.Provider=="OpenAI")draft.OpenAiModel=model.Text.Trim();else draft.Model=model.Text.Trim();
        }
        void LoadProvider(){
            LoadKeyField();
            model.Items.Clear();
            if(draft.Provider=="OpenAI")model.Items.AddRange(new[]{"gpt-4.1-mini","gpt-4.1","gpt-5-mini","gpt-5.2","gpt-6-luna","gpt-6-sol","gpt-6-astra","gpt-6.1-sol"});
            else model.Items.AddRange(new[]{Settings.DefaultModel,"gemini-3.6-flash","gemini-3.8-flash","gemini-4-argon"});
            model.Text=draft.ActiveModel;
        }
        void LoadKeyField(){
            keyLabel.Text=draft.Provider+" · ключ "+(draft.KeySlot+1)+"/"+Settings.SlotCount+(draft.AutoRotate?" · авто":"");
            try{key.Text=draft.Key;}catch{key.Text="";}
        }
        void EditKeys(){
            try{
                draft.SetKey(key.Text);
                using(var editor=new KeyEditor(draft.Provider,draft.GetKeys(),draft.KeySlot,draft.AutoRotate)){
                    if(editor.ShowDialog(this)!=DialogResult.OK)return;
                    var next=Json.New().Deserialize<Settings>(Json.New().Serialize(settings));
                    string providerInUse=next.Provider;next.Provider=draft.Provider;
                    string[] keys=editor.Keys;int chosen=editor.SelectedSlot;
                    if(String.IsNullOrEmpty(keys[chosen])){int available=Array.FindIndex(keys,k=>k.Length>0);if(available>=0)chosen=available;}
                    for(int i=0;i<keys.Length;i++)next.SetKey(i,keys[i]);
                    next.KeySlot=chosen;next.AutoRotate=editor.AutoRotate;
                    next.Provider=providerInUse;if(!preview)next.Save();settings=next;
                    for(int i=0;i<keys.Length;i++)draft.SetKey(i,keys[i]);
                    draft.KeySlot=chosen;draft.AutoRotate=editor.AutoRotate;LoadKeyField();
                    state.Text="Ключи сохранены. Активный слот: "+(chosen+1)+".";
                }
            }catch(Exception){state.Text="Не удалось сохранить ключи. Проверьте настройки Windows.";}
        }
        void NextKey(){
            try{
                var next=Json.New().Deserialize<Settings>(Json.New().Serialize(settings));
                string providerInUse=next.Provider;next.Provider=draft.Provider;
                int slot=ApiThrottle.NextSlot(next.GetKeys(),next.KeySlot);
                if(slot==next.KeySlot){state.Text="Добавьте ещё один ключ через «Ключи…».";return;}
                draft.SetKey(key.Text);
                next.KeySlot=slot;next.Provider=providerInUse;if(!preview)next.Save();settings=next;
                draft.KeySlot=slot;LoadKeyField();state.Text=draft.Provider+" · активный слот "+(slot+1)+".";
            }catch(Exception){state.Text="Не удалось переключить ключ.";}
        }
        void StartUpdate(){
            if(preview)return;
            string root=AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string script=Path.Combine(root,"Update.ps1");
            if((!Directory.Exists(Path.Combine(root,".git"))&&!File.Exists(Path.Combine(root,".git")))||!File.Exists(script)){
                state.Text="Для обновления нужна папка, полученная через git clone.";return;
            }
            if(MessageBox.Show(this,"Приложение закроется, чтобы Git мог заменить EXE. Несохранённые настройки будут потеряны. Продолжить?","Обновить из GitHub",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)return;
            try{
                var info=new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,@"WindowsPowerShell\v1.0\powershell.exe"),
                    "-NoProfile -ExecutionPolicy Bypass -STA -File "+QuoteArgument(script)+" -WaitForProcess "+Process.GetCurrentProcess().Id);
                info.UseShellExecute=false;info.CreateNoWindow=true;info.WorkingDirectory=Environment.SystemDirectory;
                using(var helper=Process.Start(info)){if(helper==null)throw new InvalidOperationException();}
                closing=true;Close();
            }catch(Exception){state.Text="Не удалось запустить обновление.";}
        }
        static string QuoteArgument(string path){return "\""+path+"\"";}
        void UpdateProtection(){
            IntPtr ignored=mini.Handle;
            protection.Text=CaptureExcluded&&mini.CaptureExcluded?"":"Исключение из захвата недоступно.";
            if(!CaptureExcluded||!mini.CaptureExcluded)protection.ForeColor=((Palette)mainTheme.SelectedItem).Text;
        }
        void UpdateOpacityLabel(){opacityLabel.Text="Непрозрачность мини-окна: "+opacity.Value+"%"+(opacity.Value==0?" — полностью скрыто":"");}
        string Bind(int index){return settings.GetHotkeys()[index].ToString();}
        void UnregisterKeys(){for(int i=1;i<=4;i++)Native.UnregisterHotKey(Handle,i);}
        void RegisterKeys(HotkeyChord[] chords){
            HotkeyChord.Validate(chords);UnregisterKeys();
            for(int i=0;i<4;i++)if(!Native.RegisterHotKey(Handle,i+1,chords[i].Modifiers|0x4000,chords[i].Key)){
                UnregisterKeys();throw new InvalidOperationException("Комбинация "+chords[i]+" занята или зарезервирована Windows. Выберите другую.");
            }
        }
        bool ApplySettings(){
            if(String.IsNullOrWhiteSpace(model.Text)){state.Text="Введите имя модели API.";return false;}
            var old=settings;
            try{
                StoreProvider();
                var next=Json.New().Deserialize<Settings>(Json.New().Serialize(draft));
                next.Hotkeys=new[]{hotkeys[0].Chord,hotkeys[1].Chord,hotkeys[2].Chord,hotkeys[3].Chord};
                HotkeyChord.Validate(next.Hotkeys);next.SetModel(model.Text);next.Opacity=opacity.Value;next.SystemPrompt=systemPrompt.Text;
                if(!preview)RegisterKeys(next.Hotkeys);
                next.Save();settings=next;mini.SetOpacity(settings.Opacity);state.Text="Настройки сохранены.";return true;
            }catch(Exception ex){
                settings=old;string message=ex.Message;
                if(!preview){try{RegisterKeys(old.GetHotkeys());}catch{message+=" Старые клавиши тоже недоступны; откройте настройки через трей.";}}
                state.Text=message;return false;
            }
        }
        protected override void WndProc(ref Message m){
            if(m.Msg==0x007E)QueueFit();
            if(m.Msg==Native.WM_HOTKEY){int id=m.WParam.ToInt32();if(id==1)StartCapture();else if(id==2)Toggle();else if(id==3)Cancel(true);else if(id==4)ShowSettings();return;}base.WndProc(ref m);
        }
        async void StartCapture(){await CaptureAsync();}
        void Toggle(){if(capturing)mini.WantedVisible=!mini.WantedVisible;else mini.SetVisible(!mini.WantedVisible);}
        void ShowSettings(){Show();WindowState=FormWindowState.Normal;FitToMonitor(false);Activate();UpdateProtection();}
        void Cancel(bool clear){
            generation++;if(request!=null)request.Cancel();
            if(clear)mini.SetAnswer("Ответ очищен. Сделайте новый снимок экрана.","ZlhrsHelper");
        }
        void Demo(){
            Cancel(false);Hide();mini.PlaceOn(Screen.FromPoint(Cursor.Position));mini.SetVisible(true);
            mini.SetAnswer("1. Сколько будет 2 + 2?\n✓ Б) 4\n\n2. Выберите простые числа.\n✓ А) 2\n✓ В) 5","ДЕМО · без API");
        }
        async Task CaptureAsync(){
            if(capturing)return;
            var snapshot=settings;
            string[] apiKeys;
            try{apiKeys=snapshot.GetKeys();}catch{ShowSettings();state.Text="Проверьте ключи в настройках.";return;}
            if(Array.TrueForAll(apiKeys,String.IsNullOrEmpty)){ShowSettings();state.Text="Сначала добавьте API-ключ.";return;}
            string apiProvider=snapshot.Provider,apiModel=snapshot.ActiveModel,prompt=snapshot.SystemPrompt;
            capturing=true;Cancel(false);int version=generation;
            var cts=new CancellationTokenSource();request=cts;
            try{
                Screen screen=Screen.FromPoint(Cursor.Position);Rectangle bounds=screen.Bounds;
                Hide();mini.Hide();
                await Task.Delay(160,cts.Token); // No flash, selection rectangle or shutter window.
                cts.Token.ThrowIfCancellationRequested();
                CaptureData image;
                using(var bitmap=new Bitmap(bounds.Width,bounds.Height,PixelFormat.Format24bppRgb)){
                    using(var g=Graphics.FromImage(bitmap))g.CopyFromScreen(bounds.Location,Point.Empty,bounds.Size,CopyPixelOperation.SourceCopy);
                    image=CaptureData.Encode(bitmap);
                }
                DateTime capturedAt=DateTime.Now;
                mini.SetAnswer("Читаю вопрос и варианты на снимке…","Обработка · "+capturedAt.ToString("HH:mm:ss"));
                capturing=false;
                bool openAi=apiProvider=="OpenAI";
                string payload=openAi?OpenAiApi.Request(apiModel,image,prompt):VisionApi.Request(apiModel,image,prompt);
                AnalysisResult result=null;int usedSlot;
                using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(cts.Token)){
                    timeout.CancelAfter(TimeSpan.FromSeconds(90));
                    usedSlot=await ApiThrottle.WithSlots(apiKeys,snapshot.KeySlot,snapshot.AutoRotate,async (apiKey,token)=>{
                        result=openAi?await OpenAiApi.SolveAsync(apiKey,payload,token).ConfigureAwait(false)
                            :await VisionApi.SolveAsync(apiKey,payload,token,apiModel).ConfigureAwait(false);
                    },timeout.Token);
                }
                if(version!=generation||cts.IsCancellationRequested||IsDisposed)return;
                if(ReferenceEquals(settings,snapshot)&&usedSlot!=snapshot.KeySlot){
                    int previousSlot=snapshot.KeySlot;
                    snapshot.KeySlot=usedSlot;
                    try{snapshot.Save();}catch(IOException){}catch(UnauthorizedAccessException){}
                    if(draft.Provider==apiProvider&&draft.KeySlot==previousSlot&&key.Text==apiKeys[previousSlot]){
                        draft.KeySlot=usedSlot;LoadKeyField();
                    }
                }
                mini.SetAnswer(VisionApi.Display(result),"Снимок "+capturedAt.ToString("HH:mm:ss")+" · ответ ИИ");
                state.Text="Ответ готов · слот "+(usedSlot+1)+". "+Bind(1)+" открывает мини-окно.";
            }catch(OperationCanceledException){
                if(version==generation&&!IsDisposed)mini.SetAnswer("Время ожидания истекло или запрос отменён. Повторите снимок.","ZlhrsHelper · ожидание завершено");
            }catch(Exception ex){
                if(version==generation&&!IsDisposed){string message=ex is System.Net.Http.HttpRequestException?"Не удалось подключиться к API. Проверьте интернет.":ex.Message;mini.SetAnswer(message,"ZlhrsHelper · ошибка");state.Text=message;}
            }finally{
                if(request==cts){request=null;capturing=false;}cts.Dispose();
                if(version==generation&&!IsDisposed)mini.RestoreVisibility();
            }
        }
        void Cleanup(){
            Cancel(false);for(int i=1;i<=4;i++)Native.UnregisterHotKey(Handle,i);tray.Visible=false;tray.Dispose();mini.Dispose();
        }
        internal AnswerForm Mini { get { return mini; } }
    }
}
