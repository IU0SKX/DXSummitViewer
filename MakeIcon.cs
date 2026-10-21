// Genera app.ico (icona "antenna" stile Lucide su verde acqua) in più dimensioni.
// Compilare ed eseguire con make_icon.bat
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

static class MakeIcon
{
    static readonly int[] Sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };
    static readonly Color Back = Color.FromArgb(0x0F, 0x6E, 0x56);

    // Linee dell'icona Lucide "antenna" (griglia 24x24)
    static readonly float[][] Lines = {
        new float[] { 2, 12, 7, 2 }, new float[] { 7, 12, 12, 2 }, new float[] { 12, 12, 17, 2 },
        new float[] { 17, 12, 22, 2 }, new float[] { 4.5f, 7, 19.5f, 7 }, new float[] { 12, 16, 12, 22 }
    };

    static Bitmap Render(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            float s = size / 32f;
            float r = 7 * s * 2;
            using (var path = new GraphicsPath())
            {
                path.AddArc(0, 0, r, r, 180, 90);
                path.AddArc(size - r, 0, r, r, 270, 90);
                path.AddArc(size - r, size - r, r, r, 0, 90);
                path.AddArc(0, size - r, r, r, 90, 90);
                path.CloseFigure();
                using (var b = new SolidBrush(Back)) g.FillPath(b, path);
            }

            // alle dimensioni piccole tratto più spesso e un elemento in meno per leggibilità
            bool small = size <= 20;
            float pad = small ? 2.5f : 4;              // margine in unità della griglia 32x32
            float k = s * (32 - 2 * pad) / 24;         // da griglia 24x24 a pixel
            float width = small ? 1.9f : (size <= 24 ? 2.4f * k : 2 * k);
            using (var pen = new Pen(Color.White, width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                for (int i = 0; i < Lines.Length; i++)
                {
                    if (small && (i == 1 || i == 2)) continue;
                    var l = Lines[i];
                    g.DrawLine(pen, pad * s + l[0] * k, pad * s + l[1] * k, pad * s + l[2] * k, pad * s + l[3] * k);
                }
                if (small) // elemento centrale al posto dei due tolti
                    g.DrawLine(pen, pad * s + 9.5f * k, pad * s + 12 * k, pad * s + 14.5f * k, pad * s + 2 * k);
            }
        }
        return bmp;
    }

    // Voce ICO in formato BMP (32 bit BGRA + maschera AND), compatibile con tutto
    static byte[] ToDib(Bitmap bmp)
    {
        int w = bmp.Width, h = bmp.Height;
        int maskStride = ((w + 31) / 32) * 4;
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(40); bw.Write(w); bw.Write(h * 2); bw.Write((short)1); bw.Write((short)32);
            bw.Write(0); bw.Write(w * h * 4 + maskStride * h); bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
            for (int y = h - 1; y >= 0; y--)
                for (int x = 0; x < w; x++)
                {
                    var c = bmp.GetPixel(x, y);
                    bw.Write(c.B); bw.Write(c.G); bw.Write(c.R); bw.Write(c.A);
                }
            for (int y = h - 1; y >= 0; y--)
            {
                var row = new byte[maskStride];
                for (int x = 0; x < w; x++)
                    if (bmp.GetPixel(x, y).A == 0) row[x / 8] |= (byte)(0x80 >> (x % 8));
                bw.Write(row);
            }
            return ms.ToArray();
        }
    }

    static byte[] ToPng(Bitmap bmp)
    {
        using (var ms = new MemoryStream())
        {
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }

    static void Main(string[] args)
    {
        string outPath = args.Length > 0 ? args[0] : "app.ico";
        var images = new List<byte[]>();
        foreach (var size in Sizes)
            using (var bmp = Render(size))
            {
                images.Add(size >= 256 ? ToPng(bmp) : ToDib(bmp));
                if (args.Length > 1 && size == 256) bmp.Save(args[1], ImageFormat.Png);
            }

        using (var fs = File.Create(outPath))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write((short)0); bw.Write((short)1); bw.Write((short)Sizes.Length);
            int offset = 6 + 16 * Sizes.Length;
            for (int i = 0; i < Sizes.Length; i++)
            {
                int s = Sizes[i];
                bw.Write((byte)(s >= 256 ? 0 : s)); bw.Write((byte)(s >= 256 ? 0 : s));
                bw.Write((byte)0); bw.Write((byte)0);
                bw.Write((short)1); bw.Write((short)32);
                bw.Write(images[i].Length); bw.Write(offset);
                offset += images[i].Length;
            }
            foreach (var img in images) bw.Write(img);
        }
        Console.WriteLine("Creato " + outPath);
    }
}
