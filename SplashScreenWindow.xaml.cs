using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SkyrimAutoDiag
{
    public partial class SplashScreenWindow : Window
    {
        public bool IsConfirmed { get; private set; } = false;

        public SplashScreenWindow()
        {
            InitializeComponent();
            LoadLogoImageSafely();
            CustomCursorHelper.ApplyToWindow(this);
        }

        private void LoadLogoImageSafely()
        {
            try
            {
                // 1. Versuch: Aus dem Programmverzeichnis laden (Logo.png)
                string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logo.png");
                if (File.Exists(localPath))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(localPath, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    SplashLogoImage.Source = bmp;
                    return;
                }

                // 2. Versuch: Als eingebettete WPF-Pack-Ressource laden
                SplashLogoImage.Source = new BitmapImage(
                    new Uri("pack://application:,,,/Logo.png", UriKind.Absolute));
            }
            catch
            {
                // Falls das Bild nicht gefunden wird, startet der SplashScreen trotzdem sicher
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }
    }
}