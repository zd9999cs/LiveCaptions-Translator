using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Appearance;

using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class SettingPage : Page
    {
        private static SettingWindow? SettingWindow;
        
        public SettingPage()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            
            // Add null check before assigning DataContext
            if (Translator.Setting != null)
            {
                DataContext = Translator.Setting;
            }
            else
            {
                // Handle the case where settings are null, maybe show an error or use default values
                // For now, just prevent setting a null DataContext
                System.Diagnostics.Debug.WriteLine("Warning: Translator.Setting is null in SettingPage constructor.");
            }

            Loaded += (s, e) =>
            {
                // Ensure MainWindow is not null before accessing properties/methods
                var mainWindow = App.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.AutoHeightAdjust(maxHeight: (int)mainWindow.MinHeight);
                }
            };

            // Add null check for Configs as well
            if (Translator.Setting?.Configs != null)
            {
                TranslateAPIBox.ItemsSource = Translator.Setting.Configs.Keys;
                TranslateAPIBox.SelectedIndex = 0; // Consider checking if Keys is empty
            }

            LoadAPISetting();
        }

        private void LiveCaptionsButton_click(object sender, RoutedEventArgs e)
        {
            if (Translator.Window == null)
                return;

            var button = sender as Wpf.Ui.Controls.Button;
            var text = ButtonText.Text;

            bool isHide = Translator.Window.Current.BoundingRectangle == Rect.Empty;
            if (isHide)
            {
                LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);
                ButtonText.Text = "Hide";
            }
            else
            {
                LiveCaptionsHandler.HideLiveCaptions(Translator.Window);
                ButtonText.Text = "Show";
            }
        }

        private void TranslateAPIBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAPISetting();
        }

        private void TargetLangBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Translator.Setting != null && TargetLangBox?.SelectedItem != null)
                Translator.Setting.TargetLanguage = TargetLangBox.SelectedItem.ToString() ?? Translator.Setting.TargetLanguage;
        }

        private void TargetLangBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting != null && TargetLangBox != null)
                Translator.Setting.TargetLanguage = TargetLangBox.Text;
        }
        
        private void APISettingButton_click(object sender, RoutedEventArgs e)
        {
            if (SettingWindow != null && SettingWindow.IsLoaded)
                SettingWindow.Activate();
            else
            {
                SettingWindow = new SettingWindow();
                SettingWindow.Closed += (sender, args) => SettingWindow = null;
                SettingWindow.Show();
            }
        }
        
        private void CaptionLogMax_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Add null checks for Translator.Setting and related properties
            if (Translator.Setting?.OverlayWindow == null || Translator.Setting?.MainWindow == null || Translator.Caption?.LogCards == null)
                return;

            if (Translator.Setting.OverlayWindow.HistoryMax > Translator.Setting.MainWindow.CaptionLogMax)
                Translator.Setting.OverlayWindow.HistoryMax = Translator.Setting.MainWindow.CaptionLogMax;
            
            // Check if LogCards is not null before accessing Count
            while (Translator.Caption.LogCards.Count > Translator.Setting.MainWindow.CaptionLogMax)
                Translator.Caption.LogCards.Dequeue();
            Translator.Caption.OnPropertyChanged("DisplayLogCards");
        }
        
        private void OverlayHistoryMax_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Add null checks for Translator.Setting and related properties
            if (Translator.Setting?.OverlayWindow == null || Translator.Setting?.MainWindow == null)
                return;

            if (Translator.Setting.OverlayWindow.HistoryMax > Translator.Setting.MainWindow.CaptionLogMax)
                Translator.Setting.MainWindow.CaptionLogMax = Translator.Setting.OverlayWindow.HistoryMax;
        }
        
        private void LiveCaptionsInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            LiveCaptionsInfoFlyout.Show();
        }

        private void LiveCaptionsInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            LiveCaptionsInfoFlyout.Hide();
        }
        
        private void FrequencyInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            FrequencyInfoFlyout.Show();
        }

        private void FrequencyInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            FrequencyInfoFlyout.Hide();
        }
        
        private void TranslateAPIInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            TranslateAPIInfoFlyout.Show();
        }

        private void TranslateAPIInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            TranslateAPIInfoFlyout.Hide();
        }

        private void TargetLangInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            TargetLangInfoFlyout.Show();
        }

        private void TargetLangInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            TargetLangInfoFlyout.Hide();
        }

        private void CaptionLogMaxInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            CaptionLogMaxInfoFlyout.Show();
        }

        private void CaptionLogMaxInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            CaptionLogMaxInfoFlyout.Hide();
        }

        public void LoadAPISetting()
        {
            // Add comprehensive null checks
            if (Translator.Setting == null || TargetLangBox == null)
                return;

            // Check CurrentAPIConfig specifically
            var currentConfig = Translator.Setting.CurrentAPIConfig;
            if (currentConfig == null || currentConfig.SupportedLanguages == null)
                return; // Cannot proceed if config or supported languages are missing

            var supportedLanguages = currentConfig.SupportedLanguages;
            TargetLangBox.ItemsSource = supportedLanguages.Keys;

            // Check if Translator.Setting is not null before accessing TargetLanguage
            string targetLang = Translator.Setting.TargetLanguage;
            if (string.IsNullOrEmpty(targetLang)) // Handle case where targetLang might be null or empty initially
            {
                // Optionally set a default language or handle appropriately
                // For now, just prevent potential errors if targetLang is needed later
                return; 
            }

            if (!supportedLanguages.ContainsKey(targetLang))
                supportedLanguages[targetLang] = targetLang;    // add custom language to supported languages
            TargetLangBox.SelectedItem = targetLang;
        }
    }
}