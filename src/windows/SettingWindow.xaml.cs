using System.Windows;
// Removed System.Windows.Controls import to avoid ambiguity, or qualify Button usages
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using System.Collections.Generic; // Added for Dictionary

namespace LiveCaptionsTranslator
{
    public partial class SettingWindow : FluentWindow
    {
        private Wpf.Ui.Controls.Button currentSelected; // Explicitly qualify Button
        private Dictionary<string, FrameworkElement> sectionReferences;

        public SettingWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            DataContext = Translator.Setting;
            // Reset background for all buttons, including the new GeminiButton
            GeneralButton.Background = new SolidColorBrush(Colors.Transparent);
            PromptButton.Background = new SolidColorBrush(Colors.Transparent);
            OllamaButton.Background = new SolidColorBrush(Colors.Transparent);
            OpenAIButton.Background = new SolidColorBrush(Colors.Transparent);
            OpenRouterButton.Background = new SolidColorBrush(Colors.Transparent);
            DeepLButton.Background = new SolidColorBrush(Colors.Transparent);
            YoudaoButton.Background = new SolidColorBrush(Colors.Transparent);
            GeminiButton.Background = new SolidColorBrush(Colors.Transparent); // Add GeminiButton
            MTranServerButton.Background = new SolidColorBrush(Colors.Transparent);

            Loaded += (sender, args) =>
            {
                SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);

                InitializeSectionReferences();

                SelectButton(GeneralButton); // Select General by default
                 // Ensure the correct section is visible on load based on the selected button
                 ShowSectionForButton(GeneralButton);
            };
        }

        private void InitializeSectionReferences()
        {
            // Use Panel or FrameworkElement as the value type if sections are Panels
            sectionReferences = new Dictionary<string, FrameworkElement>
            {
                // Map button Tags to their corresponding section Panel elements
                { "General", AllSettings }, // Assuming "General" shows all settings initially or a specific general panel
                { "Prompt", PromptSection },
                { "Ollama", OllamaSection },
                { "OpenAI", OpenAISection },
                { "OpenRouter", OpenRouterSection },
                { "DeepL", DeepLSection },
                { "Youdao", YoudaoSection },
                { "Gemini", GeminiSection }, // Add Gemini Section
                { "MTranServer", MTranServerSection }
            };
             // Hide all specific sections initially
             foreach (var kvp in sectionReferences)
             {
                 // Check if the key corresponds to a specific section panel (not the main container if "General" maps to it)
                 // This assumes 'AllSettings' is the container and specific sections are direct children or named elements.
                 // Adjust this logic if 'General' should show a specific panel like 'PromptSection'.
                 if (kvp.Value != AllSettings) // Hide specific sections, keep container potentially visible
                 {
                    kvp.Value.Visibility = Visibility.Collapsed;
                 }
             }
             // Ensure the container itself is visible if needed, or handle General's initial view in ShowSectionForButton
             AllSettings.Visibility = Visibility.Visible;
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Button button) // Explicitly qualify Button
            {
                SelectButton(button);
                ShowSectionForButton(button);
            }
        }

         private void ShowSectionForButton(Wpf.Ui.Controls.Button button) // Explicitly qualify Button
         {
             string targetTag = button.Tag?.ToString();
             if (targetTag == null) return;

             // Hide all specific sections first
             foreach (var kvp in sectionReferences)
             {
                 // Only hide specific sections, not the main container if "General" points to it
                 if (kvp.Value != AllSettings)
                 {
                    kvp.Value.Visibility = Visibility.Collapsed;
                 }
             }

             // Show the target section
             if (sectionReferences.TryGetValue(targetTag, out FrameworkElement sectionToShow))
             {
                 // If the target is the main container (e.g., for "General"), ensure it's visible
                 // and potentially show a default section within it.
                 if (sectionToShow == AllSettings)
                 {
                     AllSettings.Visibility = Visibility.Visible;
                     // Optionally show a default section like Prompt when General is clicked
                     if (sectionReferences.TryGetValue("Prompt", out var promptSection))
                     {
                         promptSection.Visibility = Visibility.Visible;
                     }
                 }
                 else // Otherwise, show the specific section panel
                 {
                     sectionToShow.Visibility = Visibility.Visible;
                 }

                 // Optional: Scroll to the top of the section
                 ContentScrollViewer.ScrollToTop();
             }
         }

        private void SelectButton(Wpf.Ui.Controls.Button button) // Explicitly qualify Button
        {
            if (currentSelected != null)
                currentSelected.Background = new SolidColorBrush(Colors.Transparent);

            // Use FindResource for theme-aware brush
            button.Background = FindResource("ControlFillColorSecondaryBrush") as Brush ?? new SolidColorBrush(Colors.LightGray); // Fallback color
            currentSelected = button;
        }
    }
}