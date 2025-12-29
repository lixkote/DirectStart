using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static B8TAM.TileColor;
using Color = System.Drawing.Color;

namespace B8TAM
{
    class TileColorCalculator
    {
        public static string CalculateRightGradient(ImageSource imageSource, String Tilename, string customtilecolor)
        {
            Bitmap bitmap = ConvertToBitmap(imageSource);
            System.Drawing.Color averageColor = CalculateAverageColor(bitmap);

            DefaultTileColors closestColor = FindClosestColor(averageColor, Tilename, customtilecolor);

            TileColor selectedTileColor = GetTileColor(closestColor);
            return selectedTileColor.RightGradient;
        }

        public static string CalculateLeftGradient(ImageSource imageSource, String Tilename, string customtilecolor)
        {
            Bitmap bitmap = ConvertToBitmap(imageSource);
            System.Drawing.Color averageColor = CalculateAverageColor(bitmap);

            DefaultTileColors closestColor = FindClosestColor(averageColor, Tilename, customtilecolor);

            TileColor selectedTileColor = GetTileColor(closestColor);
            return selectedTileColor.LeftGradient;
        }

        public static string CalculateBorder(ImageSource imageSource, String Tilename, string customtilecolor)
        {
            Bitmap bitmap = ConvertToBitmap(imageSource);
            System.Drawing.Color averageColor = CalculateAverageColor(bitmap);

            DefaultTileColors closestColor = FindClosestColor(averageColor, Tilename, customtilecolor);

            TileColor selectedTileColor = GetTileColor(closestColor);
            return selectedTileColor.Border;
        }


        private static Bitmap ConvertToBitmap(ImageSource imageSource)
        {
            BitmapImage bitmapImage = (BitmapImage)imageSource;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(outStream);
                return new Bitmap(outStream);
            }
        }
        private static double CalculateColorDistance(System.Drawing.Color color1, System.Drawing.Color color2)
        {
            // Convert colors to HSL space
            var hsl1 = RGBToHSL(color1);
            var hsl2 = RGBToHSL(color2);

            // Calculate Euclidean distance between two colors in HSL space
            double hDiff = hsl1.Item1 - hsl2.Item1;
            double sDiff = hsl1.Item2 - hsl2.Item2;
            double lDiff = hsl1.Item3 - hsl2.Item3;

            return Math.Sqrt(hDiff * hDiff + sDiff * sDiff + lDiff * lDiff);
        }

        private static (double, double, double) RGBToHSL(System.Drawing.Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h, s, l;

            // Hue calculation
            if (delta == 0)
                h = 0;
            else if (max == r)
                h = 60 * (((g - b) / delta) % 6);
            else if (max == g)
                h = 60 * (((b - r) / delta) + 2);
            else
                h = 60 * (((r - g) / delta) + 4);

            // Saturation calculation
            s = delta == 0 ? 0 : delta / (1 - Math.Abs(2 * (max - 0.5)));

            // Lightness calculation
            l = (max + min) / 2;

            return (h, s, l);
        }


        private static System.Drawing.Color CalculateAverageColor(Bitmap bitmap)
        {
            long red = 0;
            long green = 0;
            long blue = 0;
            int totalPixels = 0;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    System.Drawing.Color pixel = bitmap.GetPixel(x, y);
                    red += pixel.R;
                    green += pixel.G;
                    blue += pixel.B;
                    totalPixels++;
                }
            }

            red /= totalPixels;
            green /= totalPixels;
            blue /= totalPixels;

            return System.Drawing.Color.FromArgb((int)red, (int)green, (int)blue);
        }

        private static DefaultTileColors FindClosestColor(System.Drawing.Color averageColor, string tilename, string customtilecolor)
        {
            // Calculate distance to each predefined color and find the closest one
            double minDistance = double.MaxValue;
            DefaultTileColors closestColor = DefaultTileColors.Blue;

            foreach (DefaultTileColors color in Enum.GetValues(typeof(DefaultTileColors)))
            {
                TileColor tileColor = GetTileColor(color);
                System.Drawing.Color predefinedColor = System.Drawing.ColorTranslator.FromHtml(tileColor.RightGradient);
                double distance = CalculateColorDistance(averageColor, predefinedColor);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestColor = color;
                }
            }

            // Overrides for specific tile names:
            if (new[] { "GitHub Desktop" }.Contains(tilename))
            {
                return DefaultTileColors.Pink;
            }
            if (new[] { "Snipping Tool" }.Contains(tilename))
            {
                return DefaultTileColors.Red;
            }
            if (new[] { "winspy", "Core Temp" }.Contains(tilename))
            {
                return DefaultTileColors.Purple;
            }
            if (new[] { "Downloads", "Videos", "File Explorer", "Windows Explorer" }.Contains(tilename))
            {
                return DefaultTileColors.Teal;
            }
            if (new[] { "Pictures", "Music", "Desktop" }.Contains(tilename))
            {
                return DefaultTileColors.Blue;
            }
            if (new[] { "Documents", "Command Prompt", "cmd" }.Contains(tilename))
            {
                return DefaultTileColors.Gray;
            }
            if (new[] { "Calculator", "Task Manager", "Paint", "Notepad" }.Contains(tilename))
            {
                return DefaultTileColors.Teal;
            }

            switch (customtilecolor.ToLower())
            {
                case "pink":
                    return DefaultTileColors.Pink;
                case "purple":
                    return DefaultTileColors.Purple;
                case "teal":
                    return DefaultTileColors.Teal;
                case "blue":
                    return DefaultTileColors.Blue;
                case "gray":
                    return DefaultTileColors.Gray;
                case "black":
                    return DefaultTileColors.Black;
				case "red":
                    return DefaultTileColors.Red;
                case "pumpkin":
                    return DefaultTileColors.Pumpkin;
                case "palegreen":
                    return DefaultTileColors.PaleGreen;
                case "lightgreen":
                    return DefaultTileColors.LightGreen;
                default:
                    return closestColor;
            }
        }

    }
}
