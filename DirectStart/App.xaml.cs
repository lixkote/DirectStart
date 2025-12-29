using B8TAM;
using ControlzEx.Standard;
using ControlzEx.Theming;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;

namespace AFSM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        static Mutex mutex = new Mutex(true, "{8f7112b5-18a2-4152-896c-97e0fb647681}");
        private StartMenu _mainWindow;
        public App()
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                mutex.ReleaseMutex();
            }
            else
            {
                Current.Shutdown();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DirectStart");
            base.OnStartup(e);

            try
            {
                if (key != null)
                {
                    // Theme string (REG_SZ)
                    string theme = key.GetValue("Theme") as string ?? "Metro";

                    // DWORD (int -> bool)
                    bool roundUserProfile = Convert.ToInt32(key.GetValue("RoundedUserProfileShape", 1)) == 1;
                    bool force10BetaStartButton = Convert.ToInt32(key.GetValue("Force10BetaStartButton", 0)) == 1;
                    bool retroBarFix = Convert.ToInt32(key.GetValue("RetroBarFix", 0)) == 1;
                    bool disableTiles = Convert.ToInt32(key.GetValue("DisableTiles", 0)) == 1;
                    bool enableMetroAppsLoad = Convert.ToInt32(key.GetValue("EnableMetroAppsLoad", 0)) == 1;
                    bool useLegacyStartIntecept = Convert.ToInt32(key.GetValue("UseLegacyMenuIntercept", 0)) == 1;

                    // Apply theme
                    string resourceDictionaryPath = GetResourceDictionaryPath(theme);

                    if (!string.IsNullOrEmpty(resourceDictionaryPath))
                    {
                        ResourceDictionary skinDictionary = new ResourceDictionary
                        {
                            Source = new Uri(resourceDictionaryPath, UriKind.Absolute)
                        };

                        Resources.MergedDictionaries.Add(skinDictionary);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "The " + theme + " theme does not exist in the 'Skins' folder next to the executable.\n" +
                            "Check if '" + theme + ".xaml' is present.",
                            "DirectStart",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }

                    this.Resources["RoundedUserProfileShape"] = roundUserProfile;
                    this.Resources["Force10BetaStartButton"] = force10BetaStartButton;
                    this.Resources["RetroBarFix"] = retroBarFix;
                    this.Resources["DisableTiles"] = disableTiles;
                    this.Resources["EnableMetroAppsLoad"] = enableMetroAppsLoad;
                    this.Resources["UseLegacyMenuIntercept"] = useLegacyStartIntecept;
                }
                else
                {
                    // Registry key doesn't exist, use defaults
                    ApplyDefaultSettings();
                }
            }
            catch
            {
                ApplyDefaultSettings();
            }

            SetLanguageDictionary();
            // key.Close();
            StartMenu mainWindow = new StartMenu();
            mainWindow.Show();
        }
        private void ApplyDefaultSettings()
        {
            const string registryPath = @"SOFTWARE\DirectStart";

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryPath))
            {
                if (key != null)
                {
                    // Load default values
                    SetRegistryDefaultDword(key, "Force10BetaStartButton", 0);
                    SetRegistryDefaultDword(key, "RetroBarFix", 0);
                    SetRegistryDefaultDword(key, "DisableTiles", 0);
                    SetRegistryDefaultDword(key, "RoundedUserProfileShape", 1);
                    SetRegistryDefaultDword(key, "EnableMetroAppsLoad", 0);
                    SetRegistryDefaultDword(key, "UseLegacyMenuIntercept", 1);
                    SetRegistryDefaultString(key, "Theme", "Metro");

                    this.Resources["RoundedUserProfileShape"] = ((int?)key.GetValue("RoundedUserProfileShape") == 1);
                    this.Resources["Force10BetaStartButton"] = ((int?)key.GetValue("Force10BetaStartButton") == 1);
                    this.Resources["RetroBarFix"] = ((int?)key.GetValue("RetroBarFix") == 1);
                    this.Resources["DisableTiles"] = ((int?)key.GetValue("DisableTiles") == 1);
                    this.Resources["EnableMetroAppsLoad"] = ((int?)key.GetValue("DisableTiles") == 1);
                    this.Resources["UseLegacyMenuIntercept"] = ((int?)key.GetValue("UseLegacyMenuIntercept") == 1);

                    string theme = key.GetValue("Theme") as string;
                    string resourceDictionaryPath = GetResourceDictionaryPath(theme);
                    if (!string.IsNullOrEmpty(resourceDictionaryPath))
                    {
                        ResourceDictionary skinDictionary = new ResourceDictionary
                        {
                            Source = new Uri(resourceDictionaryPath, UriKind.RelativeOrAbsolute)
                        };
                        Resources.MergedDictionaries.Add(skinDictionary);
                    }
                }
            }
        }

        private void SetRegistryDefaultDword(RegistryKey key, string name, int defaultValue)
        {
            if (key.GetValue(name) == null)
            {
                key.SetValue(name, defaultValue, RegistryValueKind.DWord);
            }
        }

        private void SetRegistryDefaultString(RegistryKey key, string name, string defaultValue)
        {
            if (key.GetValue(name) == null)
            {
                key.SetValue(name, defaultValue, RegistryValueKind.String);
            }
        }


        private void SetLanguageDictionary()
        {
            ResourceDictionary dict = new ResourceDictionary();
            switch (Thread.CurrentThread.CurrentCulture.ToString())
            {
                case "en-US":
                    dict.Source = new Uri("..\\MultiLang\\StringResources.xaml", UriKind.Relative);
                    break;
                case "pl-PL":
                    dict.Source = new Uri("..\\MultiLang\\StringResources.xaml", UriKind.Relative);
                    break;
                default:
                    dict.Source = new Uri("..\\MultiLang\\StringResources.xaml", UriKind.Relative);
                    break;
            }
            this.Resources.MergedDictionaries.Add(dict);
        }
        private string GetResourceDictionaryPath(string themeName)
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string stylesDir = Path.Combine(exeDir, "Skins");
            string fullPath = Path.Combine(stylesDir, themeName + ".xaml");

            return File.Exists(fullPath) ? fullPath : null;
        }
    }
}
