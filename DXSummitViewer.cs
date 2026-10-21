// DX Summit Viewer - lista spot autoaggiornante da https://new.dxsummit.fi/api/v1/spots
// Compilare con: build.bat (usa il csc.exe di .NET Framework 4.x, nessuna dipendenza esterna)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

// Proprietà mostrate da Windows (tasto destro -> Proprietà -> Dettagli)
[assembly: System.Reflection.AssemblyTitle("Dx Summit Viewer by IU0SKX")]
[assembly: System.Reflection.AssemblyProduct("Dx Summit Viewer by IU0SKX")]
[assembly: System.Reflection.AssemblyCompany("IU0SKX")]
[assembly: System.Reflection.AssemblyCopyright("Copyright © 2026 IU0SKX")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]

namespace DXSummitViewer
{
    public class Spot
    {
        public long Id;
        public DateTime Time;
        public double Frequency; // kHz
        public string DxCall;
        public string DeCall;
        public string Country;
        public string Info;
        public string Band;
        public string Mode;
    }

    public static class HamUtil
    {
        static readonly object[,] Bands = {
            { "2200m", 135.7, 137.8 }, { "630m", 472.0, 479.0 }, { "160m", 1800.0, 2000.0 },
            { "80m", 3500.0, 4000.0 }, { "60m", 5250.0, 5450.0 }, { "40m", 7000.0, 7300.0 },
            { "30m", 10100.0, 10150.0 }, { "20m", 14000.0, 14350.0 }, { "17m", 18068.0, 18168.0 },
            { "15m", 21000.0, 21450.0 }, { "12m", 24890.0, 24990.0 }, { "10m", 28000.0, 29700.0 },
            { "6m", 50000.0, 54000.0 }, { "4m", 70000.0, 71000.0 }, { "2m", 144000.0, 148000.0 },
            { "70cm", 430000.0, 440000.0 }, { "23cm", 1240000.0, 1300000.0 }
        };

        public static readonly string[] BandNames = {
            "2200m", "630m", "160m", "80m", "60m", "40m", "30m", "20m", "17m",
            "15m", "12m", "10m", "6m", "4m", "2m", "70cm", "23cm", "Altro"
        };

        public static readonly string[] ModeNames = { "CW", "SSB", "FT8", "FT4", "RTTY", "PSK", "Digi", "FM", "AM", "SSTV" };

        // Paesi che includono altre entità DXCC (nomi come li restituisce DX Summit)
        public static readonly Dictionary<string, string[]> CountryGroups =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Italy", new[] { "Italy", "Sicily", "Sardinia", "African Italy" } }
            };

        // Nomi italiani accettati nel filtro paese
        public static readonly Dictionary<string, string> CountryAliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Italia", "Italy" }, { "Sicilia", "Sicily" }, { "Sardegna", "Sardinia" }
            };

        public static string BandOf(double khz)
        {
            for (int i = 0; i < Bands.GetLength(0); i++)
                if (khz >= (double)Bands[i, 1] && khz <= (double)Bands[i, 2]) return (string)Bands[i, 0];
            return "Altro";
        }

        // Frequenze FT8/FT4 standard (kHz), tolleranza +-3 kHz
        static readonly double[] Ft8 = { 1840, 3573, 5357, 7074, 10136, 14074, 18100, 21074, 24915, 28074, 50313, 50323, 144174 };
        static readonly double[] Ft4 = { 3575, 7047.5, 10140, 14080, 18104, 21140, 24919, 28180, 50318 };

        public static string ModeOf(string info, double khz)
        {
            string u = " " + (info ?? "").ToUpperInvariant() + " ";
            // 1) Parole chiave nel commento
            string[][] keys = {
                new[] { "FT8", "FT8" }, new[] { "FT4", "FT4" }, new[] { "CW", "CW" },
                new[] { "RTTY", "RTTY" }, new[] { "PSK", "PSK" }, new[] { "SSTV", "SSTV" },
                new[] { "SSB", "SSB" }, new[] { "USB", "SSB" }, new[] { "LSB", "SSB" },
                new[] { " FM", "FM" }, new[] { " AM ", "AM" },
                new[] { "JS8", "Digi" }, new[] { "JT65", "Digi" }, new[] { "JT9", "Digi" },
                new[] { "MSK144", "Digi" }, new[] { "Q65", "Digi" }, new[] { "OLIVIA", "Digi" },
                new[] { "VARAC", "Digi" }, new[] { "FST4", "Digi" }, new[] { "WSPR", "Digi" }
            };
            foreach (var k in keys)
                if (u.Contains(k[0])) return k[1];

            // 2) Frequenze digitali note
            foreach (var f in Ft8) if (Math.Abs(khz - f) <= 3) return "FT8";
            foreach (var f in Ft4) if (Math.Abs(khz - f) <= 1.5) return "FT4";

            // 3) Band plan IARU (approssimato)
            string b = BandOf(khz);
            switch (b)
            {
                case "160m": return khz < 1838 ? "CW" : "SSB";
                case "80m": return khz < 3570 ? "CW" : (khz < 3600 ? "Digi" : "SSB");
                case "60m": return "SSB";
                case "40m": return khz < 7040 ? "CW" : (khz < 7060 ? "Digi" : "SSB");
                case "30m": return khz < 10130 ? "CW" : "Digi";
                case "20m": return khz < 14070 ? "CW" : (khz < 14100 ? "Digi" : "SSB");
                case "17m": return khz < 18095 ? "CW" : (khz < 18111 ? "Digi" : "SSB");
                case "15m": return khz < 21070 ? "CW" : (khz < 21150 ? "Digi" : "SSB");
                case "12m": return khz < 24915 ? "CW" : (khz < 24931 ? "Digi" : "SSB");
                case "10m": return khz < 28070 ? "CW" : (khz < 28300 ? "Digi" : (khz < 29500 ? "SSB" : "FM"));
                case "6m": return khz < 50100 ? "CW" : (khz < 50300 ? "SSB" : (khz < 50500 ? "Digi" : "FM"));
                case "2m": return khz < 144150 ? "CW" : (khz < 144400 ? "SSB" : "FM");
                case "2200m":
                case "630m": return "CW";
                default: return "";
            }
        }
    }

    // Impostazioni minime salvate in %APPDATA%\DXSummitViewer\<nome>.txt
    public static class Settings
    {
        static string FilePath(string name)
        {
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DXSummitViewer", name + ".txt");
        }

        static string Read(string name)
        {
            try { return System.IO.File.Exists(FilePath(name)) ? System.IO.File.ReadAllText(FilePath(name)).Trim() : ""; }
            catch { return ""; }
        }

        static void Write(string name, string value)
        {
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath(name)));
                System.IO.File.WriteAllText(FilePath(name), value ?? "");
            }
            catch { }
        }

        public static string MyCall
        {
            get { return Read("mycall"); }
            set { Write("mycall", value); }
        }

        public static bool DarkTheme
        {
            get { return Read("theme") == "dark"; }
            set { Write("theme", value ? "dark" : "light"); }
        }
    }

    // Tema chiaro / scuro applicato ricorsivamente ai controlli
    public static class Theme
    {
        public static bool Dark;

        static readonly Color DarkBack = Color.FromArgb(32, 32, 32);
        static readonly Color DarkText = Color.FromArgb(230, 230, 230);
        static readonly Color DarkGridAlt = Color.FromArgb(40, 40, 44);
        static readonly Color DarkGridLine = Color.FromArgb(60, 60, 60);

        static Color Pick(Color light, Color dark) { return Dark ? dark : light; }

        // Palette usata dai controlli personalizzati (UiControls.cs)
        public static Color Back { get { return Pick(SystemColors.Control, DarkBack); } }
        public static Color Bar { get { return Pick(Color.White, Color.FromArgb(38, 38, 41)); } }
        public static Color Field { get { return Pick(Color.White, Color.FromArgb(50, 50, 54)); } }
        public static Color FieldHover { get { return Pick(Color.FromArgb(243, 244, 246), Color.FromArgb(60, 60, 65)); } }
        public static Color FieldPressed { get { return Pick(Color.FromArgb(229, 231, 235), Color.FromArgb(70, 70, 76)); } }
        public static Color Border { get { return Pick(Color.FromArgb(209, 213, 219), Color.FromArgb(72, 72, 78)); } }
        public static Color BorderHover { get { return Pick(Color.FromArgb(156, 163, 175), Color.FromArgb(110, 110, 118)); } }
        public static Color Accent { get { return Pick(Color.FromArgb(15, 110, 86), Color.FromArgb(29, 158, 117)); } }
        public static Color AccentHover { get { return Pick(Color.FromArgb(12, 92, 72), Color.FromArgb(38, 176, 131)); } }
        public static Color AccentPressed { get { return Pick(Color.FromArgb(9, 75, 59), Color.FromArgb(24, 135, 100)); } }
        public static Color Text { get { return Pick(Color.FromArgb(31, 41, 55), DarkText); } }
        public static Color Muted { get { return Pick(Color.FromArgb(107, 114, 128), Color.FromArgb(155, 155, 163)); } }
        public static Color Danger { get { return Pick(Color.FromArgb(185, 28, 28), Color.FromArgb(248, 113, 113)); } }

        public static Color NewSpot
        {
            get { return Dark ? Color.FromArgb(90, 75, 20) : Color.FromArgb(255, 248, 200); }
        }

        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);

        // Funzioni non documentate di uxtheme (Windows 10 1903+) che abilitano le barre di scorrimento scure
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", EntryPoint = "#135")]
        static extern int SetPreferredAppMode(int mode);

        [System.Runtime.InteropServices.DllImport("uxtheme.dll", EntryPoint = "#133")]
        static extern bool AllowDarkModeForWindow(IntPtr hwnd, bool allow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        static bool darkModeAllowed;

        static void DarkScrollBar(Control sb)
        {
            try
            {
                if (!darkModeAllowed) { SetPreferredAppMode(1); darkModeAllowed = true; }
                AllowDarkModeForWindow(sb.Handle, Dark);
                SetWindowTheme(sb.Handle, Dark ? "DarkMode_Explorer" : "Explorer", null);
                SendMessage(sb.Handle, 0x031A, IntPtr.Zero, IntPtr.Zero); // WM_THEMECHANGED
                sb.Invalidate();
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        public static void Apply(Form f)
        {
            f.BackColor = Back;
            f.ForeColor = Text;
            ApplyChildren(f);
            f.Invalidate(true);
            if (f.IsHandleCreated) ApplyTitleBar(f);
            else f.HandleCreated += delegate { ApplyTitleBar(f); };
        }

        // Barra del titolo scura (Windows 10 20H1+ / Windows 11)
        static void ApplyTitleBar(Form f)
        {
            int v = Dark ? 1 : 0;
            try
            {
                if (DwmSetWindowAttribute(f.Handle, 20, ref v, 4) != 0)
                    DwmSetWindowAttribute(f.Handle, 19, ref v, 4);
                // ridisegna subito la barra del titolo (NOSIZE|NOMOVE|NOZORDER|FRAMECHANGED)
                SetWindowPos(f.Handle, IntPtr.Zero, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0004 | 0x0020);
            }
            catch { }
        }

        static void ApplyChildren(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                ApplyOne(c);
                ApplyChildren(c);
            }
        }

        static void ApplyOne(Control c)
        {
            var grid = c as DataGridView;
            if (grid != null) { ApplyGrid(grid); return; }

            if (c is FilterBar)
            {
                c.BackColor = Bar;
                c.ForeColor = Color.Empty;
                return;
            }

            if (c is CaptionLabel)
            {
                c.BackColor = Color.Empty;
                c.ForeColor = Muted;
                return;
            }

            // si disegnano da soli leggendo la palette
            if (c is ModernButton || c is ToggleSwitch || c is FieldBox)
            {
                c.Invalidate();
                return;
            }

            if (c is ComboBox || c is TextBoxBase)
            {
                c.BackColor = Field;
                c.ForeColor = Text;
                c.Invalidate();
                return;
            }

            var ss = c as StatusStrip;
            if (ss != null)
            {
                ss.RenderMode = Dark ? ToolStripRenderMode.System : ToolStripRenderMode.ManagerRenderMode;
                ss.BackColor = Dark ? Color.FromArgb(45, 45, 45) : SystemColors.Control;
                ss.ForeColor = Dark ? DarkText : SystemColors.ControlText;
                return;
            }

            // Label, CheckBox, pannelli: ereditano i colori dal form
            c.BackColor = Color.Empty;
            c.ForeColor = Color.Empty;
        }

        static void ApplyGrid(DataGridView g)
        {
            g.EnableHeadersVisualStyles = !Dark;
            g.BackgroundColor = Dark ? DarkBack : SystemColors.Window;
            g.GridColor = Dark ? DarkGridLine : SystemColors.ControlDark;
            g.DefaultCellStyle.BackColor = Dark ? DarkBack : SystemColors.Window;
            g.DefaultCellStyle.ForeColor = Dark ? DarkText : SystemColors.ControlText;
            g.DefaultCellStyle.SelectionBackColor = Dark ? Color.FromArgb(0, 90, 160) : SystemColors.Highlight;
            g.DefaultCellStyle.SelectionForeColor = Dark ? Color.White : SystemColors.HighlightText;
            g.AlternatingRowsDefaultCellStyle.BackColor = Dark ? DarkGridAlt : Color.FromArgb(245, 247, 250);
            g.ColumnHeadersDefaultCellStyle.BackColor = Dark ? Color.FromArgb(45, 45, 45) : SystemColors.Control;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Dark ? DarkText : SystemColors.WindowText;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = g.ColumnHeadersDefaultCellStyle.BackColor;
            g.ColumnHeadersBorderStyle = Dark ? DataGridViewHeaderBorderStyle.Single : DataGridViewHeaderBorderStyle.Raised;

            // barre di scorrimento scure (Windows 10 1809+), ignorato dove non supportato
            foreach (Control sb in g.Controls)
                if (sb is ScrollBar) DarkScrollBar(sb);
        }
    }

    // Finestra per inviare un nuovo spot a DX Summit (POST /api/v1/spots, come fa il sito)
    public class SpotForm : Form
    {
        const string PostUrl = "https://new.dxsummit.fi/api/v1/spots";

        readonly FieldBox fbDe = new FieldBox("IU0SKX");
        readonly FieldBox fbDx = new FieldBox("");
        readonly FieldBox fbFreq = new FieldBox("14025.0");
        readonly FieldBox fbInfo = new FieldBox("CW 599");
        readonly ModernButton btnSend = new ModernButton();
        readonly ModernButton btnCancel = new ModernButton();
        readonly Label lblMsg = new Label();

        TextBox tbDe { get { return fbDe.TextBox; } }
        TextBox tbDx { get { return fbDx.TextBox; } }
        TextBox tbFreq { get { return fbFreq.TextBox; } }
        TextBox tbInfo { get { return fbInfo.TextBox; } }

        public SpotForm(string dxCall, string freq)
        {
            Text = "Invia spot a DX Summit";
            Icon = MainForm.AppIcon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(400, 250);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            tbDe.CharacterCasing = CharacterCasing.Upper;
            tbDx.CharacterCasing = CharacterCasing.Upper;
            tbInfo.MaxLength = 60;
            tbDe.Text = Settings.MyCall;
            tbDx.Text = dxCall ?? "";
            tbFreq.Text = freq ?? "";

            AddRow(layout, "Mio nominativo", fbDe);
            AddRow(layout, "Nominativo DX", fbDx);
            AddRow(layout, "Frequenza (kHz)", fbFreq);
            AddRow(layout, "Commento", fbInfo);

            lblMsg.AutoSize = true;
            layout.Controls.Add(lblMsg);
            layout.SetColumnSpan(lblMsg, 2);

            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true };
            btnCancel.Text = "Annulla";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnSend.Text = "Invia spot";
            btnSend.Accent = true;
            btnSend.Click += delegate { Send(); };
            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(btnSend);
            layout.Controls.Add(buttons);
            layout.SetColumnSpan(buttons, 2);

            AcceptButton = btnSend;
            CancelButton = btnCancel;
            Controls.Add(layout);
            Theme.Apply(this);

            Shown += delegate
            {
                if (tbDe.Text == "") tbDe.Focus();
                else if (tbDx.Text == "") tbDx.Focus();
                else if (tbFreq.Text == "") tbFreq.Focus();
                else tbInfo.Focus();
            };
        }

        static void AddRow(TableLayoutPanel layout, string label, Control c)
        {
            layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 10, 0) });
            c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c.Margin = new Padding(0, 4, 0, 4);
            layout.Controls.Add(c);
        }

        void Send()
        {
            string de = tbDe.Text.Trim();
            string dx = tbDx.Text.Trim();
            string info = tbInfo.Text.Trim();
            double khz;
            lblMsg.ForeColor = Theme.Danger;
            if (de.Length < 3 || dx.Length < 3)
            {
                lblMsg.Text = "Inserisci il tuo nominativo e quello del DX.";
                return;
            }
            if (!double.TryParse(tbFreq.Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out khz) || khz <= 0)
            {
                lblMsg.Text = "Frequenza non valida.";
                return;
            }
            if (khz < 100) khz *= 1000; // inserita in MHz
            string freq = khz.ToString("0.0", CultureInfo.InvariantCulture);

            var confirm = MessageBox.Show(this,
                "Inviare questo spot pubblico a DX Summit?\n\n" +
                "DE " + de + "   DX " + dx + "   " + freq + " kHz\n" + info,
                "Conferma invio", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            Settings.MyCall = de;
            var body = new JavaScriptSerializer().Serialize(new Dictionary<string, string>
            {
                { "de_call", de }, { "dx_call", dx }, { "frequency", freq }, { "info", info }
            });

            btnSend.Enabled = false;
            lblMsg.ForeColor = Theme.Text;
            lblMsg.Text = "Invio in corso...";

            var wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers[HttpRequestHeader.UserAgent] = "DXSummitViewer/1.0";
            wc.Headers[HttpRequestHeader.ContentType] = "application/json;charset=utf-8";
            wc.UploadStringCompleted += (s, e) =>
            {
                wc.Dispose();
                btnSend.Enabled = true;
                lblMsg.ForeColor = Theme.Danger;
                if (e.Error != null)
                {
                    lblMsg.Text = "Errore: " + e.Error.Message;
                    return;
                }
                string status = null;
                try
                {
                    var res = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(e.Result);
                    object v;
                    if (res != null && res.TryGetValue("status", out v)) status = Convert.ToString(v);
                }
                catch { }
                if (status == "ok")
                {
                    MessageBox.Show(this, "Spot inviato.", "DX Summit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                }
                else
                {
                    lblMsg.Text = "Spot rifiutato: " + (status ?? e.Result);
                }
            };
            wc.UploadStringAsync(new Uri(PostUrl), "POST", body);
        }
    }

    // Finestra Informazioni: icona, nome del software e versione (letti dagli attributi dell'assembly)
    public class AboutForm : Form
    {
        public AboutForm()
        {
            var asm = typeof(AboutForm).Assembly;
            var product = (System.Reflection.AssemblyProductAttribute)Attribute.GetCustomAttribute(
                asm, typeof(System.Reflection.AssemblyProductAttribute));
            var copyright = (System.Reflection.AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(
                asm, typeof(System.Reflection.AssemblyCopyrightAttribute));
            string name = product != null ? product.Product : "Dx Summit Viewer";

            Text = "Informazioni";
            Icon = MainForm.AppIcon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(340, 250);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                Padding = new Padding(20, 22, 20, 16)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var pic = new PictureBox
            {
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.Zoom,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 0, 12)
            };
            if (MainForm.AppIcon != null)
                pic.Image = new Icon(MainForm.AppIcon, 64, 64).ToBitmap();

            var lblName = new Label
            {
                Text = name,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Font = new Font("Segoe UI Semibold", 12F)
            };
            var lblVersion = new CaptionLabel("Versione " + asm.GetName().Version.ToString(3))
            {
                Anchor = AnchorStyles.None,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 4, 0, 0)
            };
            var lblCopy = new CaptionLabel(copyright != null ? copyright.Copyright : "")
            {
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 2, 0, 0)
            };
            var btnClose = new ModernButton
            {
                Text = "Chiudi",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 16, 0, 0)
            };

            layout.Controls.Add(pic);
            layout.Controls.Add(lblName);
            layout.Controls.Add(lblVersion);
            layout.Controls.Add(lblCopy);
            layout.Controls.Add(btnClose);
            Controls.Add(layout);

            AcceptButton = btnClose;
            CancelButton = btnClose;
            Theme.Apply(this);
        }
    }

    public class MainForm : Form, IMessageFilter
    {
        const string ApiUrl = "https://new.dxsummit.fi/api/v1/spots?limit=";
        const int WM_MOUSEWHEEL = 0x020A;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        readonly DataGridView grid = new DataGridView();
        static readonly int[] IntervalSeconds = { 15, 30, 60, 120, 300, 600 };
        static readonly string[] IntervalNames = { "15 s", "30 s", "1 min", "2 min", "5 min", "10 min" };
        static readonly int[] SpotLimits = { 100, 200, 500, 1000 };

        readonly ThemedComboBox cbCountry = new ThemedComboBox();
        readonly FieldBox fbCall = new FieldBox("Cerca nominativo");
        readonly ToggleSwitch chkSpotter = new ToggleSwitch();
        readonly ThemedComboBox cbBand = new ThemedComboBox();
        readonly ThemedComboBox cbMode = new ThemedComboBox();
        readonly ThemedComboBox cbInterval = new ThemedComboBox();
        readonly ThemedComboBox cbLimit = new ThemedComboBox();
        readonly ToggleSwitch chkAuto = new ToggleSwitch();
        readonly ModernButton btnRefresh = new ModernButton();
        readonly ModernButton btnClear = new ModernButton();
        readonly ModernButton btnSpot = new ModernButton();
        readonly ModernButton btnTheme = new ModernButton();
        readonly ModernButton btnInfo = new ModernButton();
        readonly ToolTip toolTip = new ToolTip();

        TextBox tbCall { get { return fbCall.TextBox; } }
        readonly ToolStripStatusLabel lblStatus = new ToolStripStatusLabel();
        readonly ToolStripStatusLabel lblCount = new ToolStripStatusLabel();
        readonly Timer timer = new Timer();

        List<Spot> spots = new List<Spot>();
        readonly HashSet<long> seenIds = new HashSet<long>();
        readonly HashSet<long> newIds = new HashSet<long>();
        bool loading;
        bool firstLoad = true;

        public MainForm()
        {
            Text = "Dx Summit Viewer by IU0SKX";
            Width = 1440;
            Height = 750;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            Icon = AppIcon;

            BuildFilters();
            BuildGrid();

            var status = new StatusStrip();
            lblStatus.Spring = true;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            status.Items.Add(lblStatus);
            status.Items.Add(lblCount);
            Controls.Add(status);
            grid.BringToFront(); // la griglia (Fill) va docked per ultima

            Theme.Dark = Settings.DarkTheme;
            Theme.Apply(this);
            UpdateThemeButton();

            Application.AddMessageFilter(this);
            FormClosed += delegate { Application.RemoveMessageFilter(this); };

            timer.Tick += delegate { LoadSpots(); };
            ApplyTimer();
            Shown += delegate { LoadSpots(); };
        }

        void BuildFilters()
        {
            var panel = new FilterBar
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12, 8, 12, 10),
                WrapContents = true
            };

            cbCountry.DropDownStyle = ComboBoxStyle.DropDown;
            cbCountry.Width = 160;
            cbCountry.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cbCountry.AutoCompleteSource = AutoCompleteSource.ListItems;
            cbCountry.Items.Add("(Tutti)");
            cbCountry.SelectedIndex = 0;
            cbCountry.TextChanged += delegate { ApplyFilter(); };

            fbCall.Width = 140;
            // maiuscolo solo sul testo digitato: CharacterCasing.Upper renderebbe maiuscolo anche il segnaposto
            tbCall.TextChanged += delegate
            {
                string up = tbCall.Text.ToUpperInvariant();
                if (up != tbCall.Text)
                {
                    int sel = tbCall.SelectionStart;
                    tbCall.Text = up; // rilancia TextChanged, che applica il filtro
                    tbCall.SelectionStart = sel;
                    return;
                }
                ApplyFilter();
            };

            chkSpotter.Text = "anche spotter";
            chkSpotter.CheckedChanged += delegate { ApplyFilter(); };

            cbBand.DropDownStyle = ComboBoxStyle.DropDownList;
            cbBand.Width = 90;
            cbBand.Items.Add("(Tutte)");
            cbBand.Items.AddRange(HamUtil.BandNames);
            cbBand.SelectedIndex = 0;
            cbBand.SelectedIndexChanged += delegate { ApplyFilter(); };

            cbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cbMode.Width = 90;
            cbMode.Items.Add("(Tutti)");
            cbMode.Items.AddRange(HamUtil.ModeNames);
            cbMode.SelectedIndex = 0;
            cbMode.SelectedIndexChanged += delegate { ApplyFilter(); };

            btnClear.Text = "Azzera filtri";
            btnClear.Click += delegate
            {
                cbCountry.SelectedIndex = 0;
                tbCall.Text = "";
                cbBand.SelectedIndex = 0;
                cbMode.SelectedIndex = 0;
                chkSpotter.Checked = false;
            };

            chkAuto.Checked = true;
            chkAuto.CheckedChanged += delegate { ApplyTimer(); };

            cbInterval.DropDownStyle = ComboBoxStyle.DropDownList;
            cbInterval.Width = 76;
            cbInterval.Items.AddRange(IntervalNames);
            cbInterval.SelectedIndex = 1;
            cbInterval.SelectedIndexChanged += delegate { ApplyTimer(); };

            cbLimit.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLimit.Width = 76;
            foreach (var n in SpotLimits) cbLimit.Items.Add(n.ToString());
            cbLimit.SelectedIndex = 2;
            cbLimit.SelectedIndexChanged += delegate { LoadSpots(); };

            btnRefresh.Text = "Aggiorna ora";
            btnRefresh.Click += delegate { LoadSpots(); };

            btnSpot.Text = "Invia spot...";
            btnSpot.Accent = true;
            btnSpot.Font = new Font(Font, FontStyle.Bold);
            btnSpot.Click += delegate { OpenSpotForm(null, null); };

            btnInfo.IconPainter = Ui.DrawInfoIcon;
            toolTip.SetToolTip(btnInfo, "Informazioni");
            btnInfo.Click += delegate
            {
                using (var f = new AboutForm()) f.ShowDialog(this);
            };

            btnTheme.Font = new Font("Segoe UI Symbol", 9F); // contiene i simboli sole/luna
            btnTheme.Click += delegate
            {
                Theme.Dark = !Theme.Dark;
                Settings.DarkTheme = Theme.Dark;
                Theme.Apply(this);
                UpdateThemeButton();
                ApplyFilter(); // ricolora gli spot nuovi
            };

            panel.Controls.Add(Group("Paese", cbCountry));
            panel.Controls.Add(Group("Nominativo", fbCall, chkSpotter));
            panel.Controls.Add(Group("Banda", cbBand));
            panel.Controls.Add(Group("Modo", cbMode));
            panel.Controls.Add(Group(" ", btnClear));
            var auto = Group("Aggiorna ogni", chkAuto, cbInterval);
            auto.Margin = new Padding(12, 0, 10, 4); // stacco tra filtri e opzioni di aggiornamento
            panel.Controls.Add(auto);
            panel.Controls.Add(Group("Numero Spot", cbLimit));
            panel.Controls.Add(Group(" ", btnRefresh, btnSpot, btnTheme, btnInfo));
            Controls.Add(panel);
        }

        // Etichetta piccola sopra uno o più controlli affiancati
        static Control Group(string caption, params Control[] controls)
        {
            var g = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 0, 10, 4)
            };
            g.Controls.Add(new CaptionLabel(caption));
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
            foreach (var c in controls)
            {
                // i menu a tendina hanno 1 pixel invisibile per lato: gli altri controlli si allineano
                c.Margin = c is ThemedComboBox ? new Padding(0, 0, 4, 0) : new Padding(1, 1, 7, 1);
                row.Controls.Add(c);
            }
            g.Controls.Add(row);
            return g;
        }

        void BuildGrid()
        {
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.BackgroundColor = SystemColors.Window;
            grid.BorderStyle = BorderStyle.None;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.EnableHeadersVisualStyles = true;
            typeof(DataGridView).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(grid, true, null);

            AddCol("Time", "Ora UTC", 75);
            AddCol("Freq", "Freq (kHz)", 85).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            var dx = AddCol("DX", "DX", 110);
            dx.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            AddCol("Country", "Paese", 150);
            AddCol("Band", "Banda", 60);
            AddCol("Mode", "Modo", 55);
            AddCol("Spotter", "Spotter", 100);
            var info = AddCol("Info", "Commento", 300);
            info.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var call = Convert.ToString(grid.Rows[e.RowIndex].Cells["DX"].Value);
                if (string.IsNullOrEmpty(call)) return;
                try
                {
                    Clipboard.SetText(call);
                    lblStatus.Text = "Nominativo " + call + " copiato negli appunti";
                }
                catch { }
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Copia nominativo DX", null, delegate { CopySelected("DX"); });
            menu.Items.Add("Copia frequenza", null, delegate { CopySelected("Freq"); });
            menu.Items.Add("Apri su QRZ.com", null, delegate
            {
                var call = SelectedValue("DX");
                if (!string.IsNullOrEmpty(call))
                    System.Diagnostics.Process.Start("https://www.qrz.com/db/" + Uri.EscapeDataString(call.Split('/')[0]));
            });
            menu.Items.Add("Spotta questo DX...", null, delegate
            {
                OpenSpotForm(SelectedValue("DX"), SelectedValue("Freq"));
            });
            menu.Items.Add("Filtra per questo paese", null, delegate
            {
                var c = SelectedValue("Country");
                if (!string.IsNullOrEmpty(c)) cbCountry.Text = c;
            });
            grid.ContextMenuStrip = menu;
            grid.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                    grid.CurrentCell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex >= 0 ? e.ColumnIndex : 0];
            };

            Controls.Add(grid);
        }

        DataGridViewTextBoxColumn AddCol(string name, string header, int width)
        {
            var c = new DataGridViewTextBoxColumn { Name = name, HeaderText = header, Width = width, SortMode = DataGridViewColumnSortMode.Automatic };
            grid.Columns.Add(c);
            return c;
        }

        string SelectedValue(string col)
        {
            if (grid.CurrentRow == null) return null;
            return Convert.ToString(grid.CurrentRow.Cells[col].Value);
        }

        void CopySelected(string col)
        {
            var v = SelectedValue(col);
            if (!string.IsNullOrEmpty(v)) try { Clipboard.SetText(v); } catch { }
        }

        // La rotella del mouse scorre sempre la lista, anche se il fuoco è su un filtro
        // (menu a tendina, casella numerica...). Fa eccezione un menu a tendina aperto.
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WM_MOUSEWHEEL) return false;
            var c = Control.FromChildHandle(m.HWnd);
            if (c == null || c.FindForm() != this) return false;
            if (c == grid || grid.Contains(c)) return false;
            foreach (var cb in new[] { cbCountry, cbBand, cbMode, cbInterval, cbLimit })
                if (cb.DroppedDown) return false;
            SendMessage(grid.Handle, WM_MOUSEWHEEL, m.WParam, m.LParam);
            return true;
        }

        // Icona incorporata come risorsa (contiene tutte le dimensioni, nitida anche a 16 px)
        static Icon appIcon;
        public static Icon AppIcon
        {
            get
            {
                if (appIcon == null)
                    try
                    {
                        using (var s = typeof(MainForm).Assembly.GetManifestResourceStream("DXSummitViewer.app.ico"))
                            if (s != null) appIcon = new Icon(s);
                    }
                    catch { }
                return appIcon;
            }
        }

        void UpdateThemeButton()
        {
            btnTheme.Text = Theme.Dark ? "☀ Tema chiaro" : "☾ Tema scuro";
        }

        void OpenSpotForm(string dxCall, string freq)
        {
            using (var f = new SpotForm(dxCall, freq))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadSpots();
            }
        }

        void ApplyTimer()
        {
            timer.Interval = IntervalSeconds[Math.Max(0, cbInterval.SelectedIndex)] * 1000;
            timer.Enabled = chkAuto.Checked;
        }

        void LoadSpots()
        {
            if (loading) return;
            loading = true;
            btnRefresh.Enabled = false;
            lblStatus.Text = "Aggiornamento in corso...";

            var wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers[HttpRequestHeader.UserAgent] = "DXSummitViewer/1.0";
            wc.DownloadStringCompleted += (s, e) =>
            {
                loading = false;
                btnRefresh.Enabled = true;
                wc.Dispose();
                if (e.Error != null)
                {
                    lblStatus.Text = "Errore: " + e.Error.Message + "  (" + DateTime.Now.ToString("HH:mm:ss") + ")";
                    return;
                }
                try
                {
                    var list = Parse(e.Result);
                    int added = 0;
                    newIds.Clear();
                    foreach (var sp in list)
                    {
                        if (seenIds.Add(sp.Id))
                        {
                            added++;
                            if (!firstLoad) newIds.Add(sp.Id);
                        }
                    }
                    spots = list;
                    firstLoad = false;
                    UpdateCountryList();
                    ApplyFilter();
                    lblStatus.Text = "Ultimo aggiornamento: " + DateTime.Now.ToString("HH:mm:ss") +
                                     "  -  nuovi spot: " + added;
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Errore nella lettura dei dati: " + ex.Message;
                }
            };
            wc.DownloadStringAsync(new Uri(ApiUrl + SpotLimits[Math.Max(0, cbLimit.SelectedIndex)]));
        }

        static List<Spot> Parse(string json)
        {
            var ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var arr = ser.Deserialize<List<Dictionary<string, object>>>(json);
            var result = new List<Spot>(arr.Count);
            foreach (var d in arr)
            {
                var sp = new Spot
                {
                    Id = Convert.ToInt64(Get(d, "id") ?? 0, CultureInfo.InvariantCulture),
                    Frequency = Convert.ToDouble(Get(d, "frequency") ?? 0, CultureInfo.InvariantCulture),
                    DxCall = Convert.ToString(Get(d, "dx_call") ?? ""),
                    DeCall = Convert.ToString(Get(d, "de_call") ?? ""),
                    Country = Convert.ToString(Get(d, "dx_country") ?? ""),
                    Info = Convert.ToString(Get(d, "info") ?? "")
                };
                DateTime t;
                if (DateTime.TryParse(Convert.ToString(Get(d, "time")), CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out t))
                    sp.Time = t;
                sp.Band = HamUtil.BandOf(sp.Frequency);
                sp.Mode = HamUtil.ModeOf(sp.Info, sp.Frequency);
                result.Add(sp);
            }
            return result.OrderByDescending(x => x.Time).ThenByDescending(x => x.Id).ToList();
        }

        static object Get(Dictionary<string, object> d, string key)
        {
            object v;
            return d.TryGetValue(key, out v) ? v : null;
        }

        void UpdateCountryList()
        {
            var current = cbCountry.Text;
            var countries = spots.Select(s => s.Country).Where(c => c != "").Distinct()
                                 .Concat(cbCountry.Items.Cast<object>().Skip(1).Select(o => o.ToString()))
                                 .Distinct().OrderBy(c => c).ToArray();
            if (countries.Length == cbCountry.Items.Count - 1) return;
            cbCountry.BeginUpdate();
            cbCountry.Items.Clear();
            cbCountry.Items.Add("(Tutti)");
            cbCountry.Items.AddRange(countries);
            cbCountry.Text = current;
            cbCountry.EndUpdate();
        }

        void ApplyFilter()
        {
            string country = cbCountry.Text.Trim();
            if (country == "(Tutti)") country = "";
            string alias;
            if (HamUtil.CountryAliases.TryGetValue(country, out alias)) country = alias;
            // entità incluse nei gruppi il cui nome corrisponde al testo (es. Italy -> Sicily, Sardinia)
            var groupMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (country != "")
                foreach (var g in HamUtil.CountryGroups)
                    if (g.Key.IndexOf(country, StringComparison.OrdinalIgnoreCase) >= 0)
                        groupMembers.UnionWith(g.Value);
            string call = tbCall.Text.Trim().ToUpperInvariant();
            string band = cbBand.SelectedIndex > 0 ? (string)cbBand.SelectedItem : null;
            string mode = cbMode.SelectedIndex > 0 ? (string)cbMode.SelectedItem : null;
            bool spotter = chkSpotter.Checked;

            var filtered = spots.Where(s =>
                (country == "" || s.Country.IndexOf(country, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    groupMembers.Contains(s.Country)) &&
                (call == "" || s.DxCall.ToUpperInvariant().Contains(call) ||
                    (spotter && s.DeCall.ToUpperInvariant().Contains(call))) &&
                (band == null || s.Band == band) &&
                (mode == null || s.Mode == mode)).ToList();

            // conserva ordinamento e spot selezionato; la lista riparte dall'inizio per mostrare gli spot nuovi
            var sortCol = grid.SortedColumn;
            var sortDir = grid.SortOrder;
            object selectedId = grid.CurrentRow != null ? grid.CurrentRow.Tag : null;
            int selectedCol = grid.CurrentCell != null ? grid.CurrentCell.ColumnIndex : 0;

            grid.SuspendLayout();
            grid.Rows.Clear();
            foreach (var s in filtered)
            {
                int i = grid.Rows.Add(
                    s.Time.ToString("HH:mm:ss"),
                    s.Frequency.ToString("0.0", CultureInfo.InvariantCulture),
                    s.DxCall, s.Country, s.Band, s.Mode, s.DeCall, s.Info);
                grid.Rows[i].Tag = s.Id;
                if (newIds.Contains(s.Id))
                    grid.Rows[i].DefaultCellStyle.BackColor = Theme.NewSpot;
            }
            if (sortCol != null && sortDir != SortOrder.None)
                grid.Sort(sortCol, sortDir == SortOrder.Ascending
                    ? System.ComponentModel.ListSortDirection.Ascending
                    : System.ComponentModel.ListSortDirection.Descending);

            DataGridViewRow selected = null;
            if (selectedId != null)
                foreach (DataGridViewRow r in grid.Rows)
                    if (selectedId.Equals(r.Tag)) { selected = r; break; }

            if (selected != null)
                grid.CurrentCell = selected.Cells[selectedCol]; // riseleziona lo stesso spot
            else if (selectedId != null)
                grid.ClearSelection(); // lo spot selezionato non è più nella lista
            if (grid.Rows.Count > 0)
                try { grid.FirstDisplayedScrollingRowIndex = 0; } catch { }
            grid.ResumeLayout();

            lblCount.Text = "Visualizzati " + filtered.Count + " di " + spots.Count + " spot";
        }

        [STAThread]
        static void Main()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
