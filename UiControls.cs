// Controlli disegnati a mano che seguono il tema chiaro/scuro (vedi Theme in DXSummitViewer.cs)
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DXSummitViewer
{
    static class Ui
    {
        public static GraphicsPath Round(RectangleF r, float radius)
        {
            float d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        // Rettangolo del bordo allineato ai pixel (linea netta, non sfumata su due pixel)
        public static RectangleF BorderRect(int x, int y, int w, int h)
        {
            return new RectangleF(x, y, w - 1, h - 1);
        }

        public static Color ParentBack(Control c)
        {
            return c.Parent != null ? c.Parent.BackColor : Theme.Back;
        }

        // Icona "info" in stile Lucide (cerchio, asta e punto) centrata in r
        public static void DrawInfoIcon(Graphics g, Rectangle r, Color color)
        {
            float s = 16f / 24;                       // icona da 16 px sulla griglia Lucide 24x24
            float ox = r.X + (r.Width - 16) / 2f, oy = r.Y + (r.Height - 16) / 2f;
            using (var pen = new Pen(color, 1.5f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawEllipse(pen, ox + 2 * s, oy + 2 * s, 20 * s, 20 * s);
                g.DrawLine(pen, ox + 12 * s, oy + 16 * s, ox + 12 * s, oy + 11.5f * s);
            }
            using (var b = new SolidBrush(color))
                g.FillEllipse(b, ox + 12 * s - 1.1f, oy + 8 * s - 1.1f, 2.2f, 2.2f);
        }

        public static void DrawChevron(Graphics g, float cx, float cy)
        {
            using (var pen = new Pen(Theme.Muted, 1.6f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                g.DrawLines(pen, new[] { new PointF(cx - 4, cy - 2), new PointF(cx, cy + 2), new PointF(cx + 4, cy - 2) });
            }
        }
    }

    // Barra dei filtri: sfondo dedicato e linea di separazione in basso
    public class FilterBar : FlowLayoutPanel
    {
        public FilterBar()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Theme.Border))
                e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        }
    }

    // Etichetta piccola sopra ogni filtro
    public class CaptionLabel : Label
    {
        public CaptionLabel(string text)
        {
            Text = text;
            AutoSize = true;
            Font = new Font("Segoe UI", 8F);
            Margin = new Padding(1, 0, 0, 3);
        }
    }

    // Menu a tendina con bordo arrotondato, freccia e voci disegnate secondo il tema.
    // Windows disegna una linea chiara sul primo pixel in alto e a sinistra anche dopo il nostro
    // disegno: la cornice di 1 pixel viene esclusa con Region e il controllo è alto 2 pixel in più.
    public class ThemedComboBox : ComboBox
    {
        const int WM_PAINT = 0x000F;
        const int ButtonWidth = 20;
        const int Inset = 1;
        bool hover;

        public ThemedComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            FlatStyle = FlatStyle.Flat;
            ItemHeight = 22;
            MaxDropDownItems = 16;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct COMBOBOXINFO
        {
            public int cbSize;
            public RECT rcItem, rcButton;
            public int stateButton;
            public IntPtr hwndCombo, hwndItem, hwndList;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool GetComboBoxInfo(IntPtr hwnd, ref COMBOBOXINFO info);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool MoveWindow(IntPtr hwnd, int x, int y, int w, int h, bool repaint);

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Region = new Region(new Rectangle(Inset, Inset, Width - 2 * Inset, Height - 2 * Inset));
        }

        // Nel menu modificabile (DropDown) il testo sta in una casella di Windows posizionata in alto:
        // la si sposta per centrarla in verticale e allinearla al testo degli altri menu.
        // Restituisce la posizione della casella, Rectangle.Empty se il menu non è modificabile.
        Rectangle CenterEdit()
        {
            if (DropDownStyle != ComboBoxStyle.DropDown) return Rectangle.Empty;
            var info = new COMBOBOXINFO();
            info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
            if (!GetComboBoxInfo(Handle, ref info) || info.hwndItem == IntPtr.Zero) return Rectangle.Empty;
            int h = TextRenderer.MeasureText("Ag", Font).Height;
            var target = new Rectangle(Inset + 5, (Height - h) / 2, Width - 2 * Inset - 5 - ButtonWidth - 4, h);
            RECT cur;
            GetWindowRect(info.hwndItem, out cur);
            var p = PointToClient(new Point(cur.Left, cur.Top));
            if (p.X != target.X || p.Y != target.Y || cur.Right - cur.Left != target.Width || cur.Bottom - cur.Top != target.Height)
                MoveWindow(info.hwndItem, target.X, target.Y, target.Width, target.Height, true);
            return target;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool edit = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool selected = !edit && (e.State & DrawItemState.Selected) != 0;
            using (var b = new SolidBrush(selected ? Theme.Accent : Theme.Field))
                e.Graphics.FillRectangle(b, e.Bounds);
            var r = new Rectangle(e.Bounds.X + (edit ? 4 : 8), e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, GetItemText(Items[e.Index]), Font, r,
                selected ? Color.White : Theme.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_PAINT) PaintChrome();
            else if (m.Msg == 0x0005 && IsHandleCreated) CenterEdit(); // WM_SIZE
        }

        // Ridisegna bordo e freccia sopra il disegno nativo, lasciando intatta l'area del testo
        void PaintChrome()
        {
            if (!IsHandleCreated || Width < ButtonWidth + 8) return;
            var edit = CenterEdit();
            using (var g = Graphics.FromHwnd(Handle))
            {
                var r = new Rectangle(0, 0, Width, Height);
                // si lascia intatta solo l'area del testo (la casella modificabile, o il testo disegnato in OnDrawItem)
                var inner = edit != Rectangle.Empty ? edit : new Rectangle(3, 3, Width - 6 - ButtonWidth, Height - 6);
                using (var clip = new Region(r))
                {
                    clip.Exclude(inner);
                    g.SetClip(clip, CombineMode.Replace);
                }
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var b = new SolidBrush(Ui.ParentBack(this))) g.FillRectangle(b, r);
                var rf = Ui.BorderRect(Inset, Inset, Width - 2 * Inset, Height - 2 * Inset);
                using (var path = Ui.Round(rf, 5))
                {
                    using (var b = new SolidBrush(Theme.Field)) g.FillPath(b, path);
                    var border = Focused || ContainsFocus || DroppedDown ? Theme.Accent : hover ? Theme.BorderHover : Theme.Border;
                    using (var pen = new Pen(border)) g.DrawPath(pen, path);
                }
                Ui.DrawChevron(g, Width - Inset - 3 - ButtonWidth / 2f, Height / 2f);
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnDropDownClosed(EventArgs e) { base.OnDropDownClosed(e); Invalidate(); }
    }

    // Campo di testo con bordo arrotondato e testo segnaposto
    public class FieldBox : Panel
    {
        const int EM_SETCUEBANNER = 0x1501;

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        public readonly TextBox TextBox = new TextBox();
        bool hover;

        public FieldBox(string placeholder)
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            Height = 26;
            Cursor = Cursors.IBeam;
            TextBox.BorderStyle = BorderStyle.None;
            Controls.Add(TextBox);
            TextBox.Enter += delegate { Invalidate(); };
            TextBox.Leave += delegate { Invalidate(); };
            TextBox.MouseEnter += delegate { hover = true; Invalidate(); };
            TextBox.MouseLeave += delegate { hover = false; Invalidate(); };
            if (!string.IsNullOrEmpty(placeholder))
                TextBox.HandleCreated += delegate { SendMessage(TextBox.Handle, EM_SETCUEBANNER, (IntPtr)1, placeholder); };
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            TextBox.Location = new Point(8, (Height - TextBox.Height) / 2);
            TextBox.Width = Math.Max(10, Width - 16);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Ui.ParentBack(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Ui.Round(Ui.BorderRect(0, 0, Width, Height), 5))
            {
                using (var b = new SolidBrush(Theme.Field)) g.FillPath(b, path);
                var border = TextBox.Focused ? Theme.Accent : hover ? Theme.BorderHover : Theme.Border;
                using (var pen = new Pen(border)) g.DrawPath(pen, path);
            }
        }

        protected override void OnClick(EventArgs e) { base.OnClick(e); TextBox.Focus(); }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
    }

    // Interruttore on/off al posto della casella di spunta
    public class ToggleSwitch : Control
    {
        const int TrackW = 32, TrackH = 18;
        bool isChecked, hover;

        public event EventHandler CheckedChanged;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Cursor = Cursors.Hand;
            Height = 26;
            Width = TrackW + 2;
        }

        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value) return;
                isChecked = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        void UpdateWidth()
        {
            Width = TrackW + 2 + (string.IsNullOrEmpty(Text) ? 0 : 6 + TextRenderer.MeasureText(Text, Font).Width);
        }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); UpdateWidth(); Invalidate(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); UpdateWidth(); }
        protected override void OnClick(EventArgs e) { base.OnClick(e); Focus(); Checked = !Checked; }
        protected override void OnKeyUp(KeyEventArgs e) { base.OnKeyUp(e); if (e.KeyCode == Keys.Space) Checked = !Checked; }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Ui.ParentBack(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var track = new RectangleF(1, (Height - TrackH) / 2f, TrackW, TrackH);
            using (var path = Ui.Round(track, TrackH / 2f))
            {
                var fill = isChecked ? (hover ? Theme.AccentHover : Theme.Accent) : (hover ? Theme.BorderHover : Theme.Border);
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                if (Focused && ShowFocusCues)
                    using (var pen = new Pen(Theme.Accent, 1.5f))
                    using (var ring = Ui.Round(RectangleF.Inflate(track, 1.5f, 1.5f), TrackH / 2f + 1.5f))
                        g.DrawPath(pen, ring);
            }
            float d = TrackH - 4;
            float x = isChecked ? track.Right - d - 2 : track.X + 2;
            using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, x, track.Y + 2, d, d);
            if (!string.IsNullOrEmpty(Text))
                TextRenderer.DrawText(g, Text, Font, new Rectangle(TrackW + 8, 0, Width - TrackW - 8, Height), Theme.Text,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }
    }

    // Pulsante arrotondato; Accent = pulsante principale colorato
    public class ModernButton : Control, IButtonControl
    {
        bool hover, pressed;

        public bool Accent { get; set; }
        public DialogResult DialogResult { get; set; }

        // Se impostato, il pulsante è quadrato e mostra solo l'icona disegnata da questa funzione
        Action<Graphics, Rectangle, Color> iconPainter;
        public Action<Graphics, Rectangle, Color> IconPainter
        {
            get { return iconPainter; }
            set { iconPainter = value; UpdateWidth(); Invalidate(); }
        }

        public ModernButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Cursor = Cursors.Hand;
            Height = 26;
        }

        public void NotifyDefault(bool value) { }

        public void PerformClick()
        {
            if (Enabled && Visible) OnClick(EventArgs.Empty);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            var f = FindForm();
            if (DialogResult != DialogResult.None && f != null) f.DialogResult = DialogResult;
        }

        void UpdateWidth()
        {
            Width = iconPainter != null ? Height : TextRenderer.MeasureText(Text, Font).Width + 26;
        }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); UpdateWidth(); Invalidate(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); UpdateWidth(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { pressed = true; Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); pressed = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) PerformClick();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Ui.ParentBack(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill, text, border;
            if (!Enabled)
            {
                fill = Theme.Field; border = Theme.Border; text = Theme.Muted;
            }
            else if (Accent)
            {
                fill = pressed ? Theme.AccentPressed : hover ? Theme.AccentHover : Theme.Accent;
                border = fill; text = Color.White;
            }
            else
            {
                fill = pressed ? Theme.FieldPressed : hover ? Theme.FieldHover : Theme.Field;
                border = hover ? Theme.BorderHover : Theme.Border; text = Theme.Text;
            }
            if (Focused && ShowFocusCues) border = Theme.Accent;
            using (var path = Ui.Round(Ui.BorderRect(0, 0, Width, Height), 6))
            {
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                using (var pen = new Pen(border)) g.DrawPath(pen, path);
            }
            if (iconPainter != null)
                iconPainter(g, ClientRectangle, text);
            else
                TextRenderer.DrawText(g, Text, Font, ClientRectangle, text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }
}
