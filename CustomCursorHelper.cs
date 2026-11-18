using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkyrimAutoDiag
{
    public static class CustomCursorHelper
    {
        public static Cursor? RuneArrowCursor { get; private set; }
        public static Cursor? RuneHandCursor { get; private set; }

        /// <summary>
        /// Erzeugt die Design-Mauszeiger im Arbeitsspeicher und wendet sie automatisch 
        /// auf das übergebene Fenster sowie alle darin enthaltenen Buttons, Tabs & Textfelder an.
        /// </summary>
        public static void ApplyToWindow(Window window)
        {
            RuneArrowCursor ??= CreateArrowCursor();
            RuneHandCursor ??= CreateHandCursor();

            // 1. Standard-Mauszeiger für das gesamte Fenster setzen
            window.Cursor = RuneArrowCursor;

            // 2. Sobald das Fenster geladen ist, alle "Hand"-Cursor durch den Runen-Fokus ersetzen
            window.Loaded += (_, _) => ReplaceHandCursorsRecursive(window);
        }

        private static void ReplaceHandCursorsRecursive(DependencyObject parent)
        {
            if (RuneHandCursor == null) return;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is FrameworkElement fe)
                {
                    // Alle Elemente mit Cursor="Hand" oder typische Klick-Steuerelemente anpassen
                    if (fe.Cursor == Cursors.Hand || fe is ButtonBase || fe is TabItem || fe is ComboBox)
                    {
                        fe.Cursor = RuneHandCursor;
                    }
                }

                ReplaceHandCursorsRecursive(child);
            }
        }

        // 1. Standard-Zeiger: Dunkle Skyrim-Runenklinge (#181A1F) mit #61AFEF Rand & #E5C07B Gold-Kern
        private static Cursor CreateArrowCursor()
        {
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                Brush darkBody = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#181A1F"));
                Pen cyanEdge = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#61AFEF")), 1.8)
                {
                    LineJoin = PenLineJoin.Round
                };

                // Äußere Pfeil-Klinge
                StreamGeometry bladeGeo = new StreamGeometry();
                using (StreamGeometryContext ctx = bladeGeo.Open())
                {
                    ctx.BeginFigure(new Point(2, 2), true, true);
                    ctx.LineTo(new Point(2, 24), true, false);
                    ctx.LineTo(new Point(8, 19), true, false);
                    ctx.LineTo(new Point(12.5, 27.5), true, false);
                    ctx.LineTo(new Point(16, 25.5), true, false);
                    ctx.LineTo(new Point(11.5, 17), true, false);
                    ctx.LineTo(new Point(19, 17), true, false);
                }
                dc.DrawGeometry(darkBody, cyanEdge, bladeGeo);

                // Goldene Runen-Intarsie im Inneren (#E5C07B)
                Brush goldRune = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5C07B"));
                StreamGeometry runeGeo = new StreamGeometry();
                using (StreamGeometryContext ctx = runeGeo.Open())
                {
                    ctx.BeginFigure(new Point(5, 7), true, true);
                    ctx.LineTo(new Point(5, 16), true, false);
                    ctx.LineTo(new Point(11.5, 13.5), true, false);
                }
                dc.DrawGeometry(goldRune, null, runeGeo);
            }

            // Hotspot exakt auf der Klingen-Spitze (2, 2)
            return ConvertVisualToCursor(visual, 32, 32, 2, 2);
        }

        // 2. Klick-Zeiger für Buttons & Tabs: Leuchtendes Diagnose-Fadenkreuz mit Runen-Diamant
        private static Cursor CreateHandCursor()
        {
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                Brush darkCenter = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D8181A1F"));
                Pen cyanPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#61AFEF")), 1.7);
                Pen goldPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5C07B")), 1.5);
                Brush goldFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5C07B"));

                // Äußerer Diagnose-Ring
                dc.DrawEllipse(darkCenter, cyanPen, new Point(16, 16), 9.5, 9.5);

                // 4 Fadenkreuz-Segmente (wie im Logo/Drachenauge)
                dc.DrawLine(cyanPen, new Point(16, 2), new Point(16, 7.5));
                dc.DrawLine(cyanPen, new Point(16, 24.5), new Point(16, 30));
                dc.DrawLine(cyanPen, new Point(2, 16), new Point(7.5, 16));
                dc.DrawLine(cyanPen, new Point(24.5, 16), new Point(30, 16));

                // Goldener Runen-Diamant in der Mitte
                StreamGeometry diamond = new StreamGeometry();
                using (StreamGeometryContext ctx = diamond.Open())
                {
                    ctx.BeginFigure(new Point(16, 11.5), true, true);
                    ctx.LineTo(new Point(20.5, 16), true, false);
                    ctx.LineTo(new Point(16, 20.5), true, false);
                    ctx.LineTo(new Point(11.5, 16), true, false);
                }
                dc.DrawGeometry(goldFill, goldPen, diamond);
            }

            // Hotspot exakt in der Mitte des Fadenkreuzes (16, 16)
            return ConvertVisualToCursor(visual, 32, 32, 16, 16);
        }

        // Baut im RAM einen validen Windows-.CUR-Binärheader um das gerenderte PNG
        private static Cursor ConvertVisualToCursor(Visual visual, int width, int height, ushort hotspotX, ushort hotspotY)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);

            PngBitmapEncoder png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));

            using MemoryStream pngStream = new MemoryStream();
            png.Save(pngStream);
            byte[] pngBytes = pngStream.ToArray();

            MemoryStream curStream = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(curStream, System.Text.Encoding.Default, leaveOpen: true))
            {
                // ICONDIR Header (Typ 2 = Cursor)
                bw.Write((ushort)0);
                bw.Write((ushort)2);
                bw.Write((ushort)1);

                // ICONDIRENTRY
                bw.Write((byte)width);
                bw.Write((byte)height);
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write(hotspotX); // X-Koordinate der Klickspitze
                bw.Write(hotspotY); // Y-Koordinate der Klickspitze
                bw.Write((uint)pngBytes.Length);
                bw.Write((uint)22); // Offset der Bilddaten

                bw.Write(pngBytes);
            }

            curStream.Position = 0;
            return new Cursor(curStream);
        }
    }
}