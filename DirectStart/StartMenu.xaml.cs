using AFSM;
using B8TAM.FrequentHelpers;
using BlurryControls.Controls;
using ControlzEx.Standard;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using static B8TAM.TilesLoader;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using Color = System.Windows.Media.Color;
using File = System.IO.File;
using MessageBox = System.Windows.MessageBox;

namespace B8TAM
{
	/// <summary>
	/// Interaction logic for StartMenu.xaml
	/// </summary>
	public partial class StartMenu : System.Windows.Window
	{
		LegacyStartMenuIntercepter _listener;
		FrequentAppsHelper frequentHelper;
		ObservableCollection<StartMenuEntry> Programs = new ObservableCollection<StartMenuEntry>();
		ObservableCollection<StartMenuEntry> Pinned = new ObservableCollection<StartMenuEntry>();
		ObservableCollection<StartMenuEntry> Recent = new ObservableCollection<StartMenuEntry>();
		ObservableCollection<StartMenuLink> Results = new ObservableCollection<StartMenuLink>();
		ObservableCollection<Tile> Tiles = new ObservableCollection<Tile>();
		public SourceType SelectedSourceType { get; set; }
		private List<SourceType> sourceTypes;
		private ObservableCollection<CountEntry> countEntries;

		bool Force10BetaStartButton;
        bool RetroBarFix;
        public static IntPtr MessageWindowHandle;

        bool RoundedUserProfileShape;
        bool DisableTiles;
        bool EnableMetroAppsLoad;
        bool UseLegacyMenuIntercept;

        public const int WH_START_TRIGGERED = 0x8001;

        [DllImport("user32.dll")]
        private static extern bool LockWorkStation();


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);


        double taskbarheightinpx;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        [DllImport("user32.dll")]
        static extern byte MapVirtualKey(byte wCode, int wMap);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hWnd, uint Msg);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        public SolidColorBrush PressedBackground
        {
            get
            {
                IntPtr pElementName = Marshal.StringToHGlobalUni(ImmersiveColors.ImmersiveStartSelectionBackground.ToString());
                System.Windows.Media.Color color = GetColor(pElementName);
                return new SolidColorBrush(color);
            }
        }


        public ICommand Run => new RunCommand(RunCommand);


        private static Color GetColor(IntPtr pElementName)
		{
			var colourset = DUIColorHelper.GetImmersiveUserColorSetPreference(false, false);
			uint type = DUIColorHelper.GetImmersiveColorTypeFromName(pElementName);
			Marshal.FreeCoTaskMem(pElementName);
			uint colourdword = DUIColorHelper.GetImmersiveColorFromColorSetEx((uint)colourset, type, false, 0);
			byte[] colourbytes = new byte[4];
			colourbytes[0] = (byte)((0xFF000000 & colourdword) >> 24); // A
			colourbytes[1] = (byte)((0x00FF0000 & colourdword) >> 16); // B
			colourbytes[2] = (byte)((0x0000FF00 & colourdword) >> 8); // G
			colourbytes[3] = (byte)(0x000000FF & colourdword); // R
			Color color = Color.FromArgb(colourbytes[0], colourbytes[3], colourbytes[2], colourbytes[1]);
			return color;
		}

		public StartMenu()
		{
			PreparePinnedStartMenu();
			string programs = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs");
			GetPrograms(programs);
            programs = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
			GetPrograms(programs);



            if (EnableMetroAppsLoad == true)
            {
                // Searches and loads metro apps from these folders:
                string metroAppsDirectory = @"C:\Program Files\WindowsApps";
                LoadMetroApps(metroAppsDirectory);
                string immersiveSet = @"C:\Windows\ImmersiveControlPanel";
                LoadMetroApps(immersiveSet);

                // Warning: Metro apps load function is unfinished and can cause crashes!
                // Blame M$ for not giving any useful way to load them in WPF
            }


            this.sourceTypes = new List<SourceType>()
			{
				new SourceType("Program", @"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist\{CEBFF5CD-ACE2-4F4F-9178-9926F41749EA}\Count"),
				new SourceType("Shortcut", @"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist\{F4E57C4B-2036-45F0-A9AB-443BCFE33D9F}\Count")
			};
			this.SelectedSourceType = sourceTypes[0];
			GetFrequentsNew();

			string pinned = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				@"Microsoft\Internet Explorer\Quick Launch\User Pinned\StartMenu\");
			GetPinned(pinned);

			Programs = new ObservableCollection<StartMenuEntry>(Programs.OrderBy(x => x.Title));

            var DefaultPinnedOrder = new Dictionary<string, int>
            {
                { "Documents", 1 },
                { "Pictures", 2 },
                { "Control Panel", 3 },
                { "This PC", 4 }
            };

            Pinned = new ObservableCollection<StartMenuEntry>(
                Pinned
                    .OrderBy(x => DefaultPinnedOrder.ContainsKey(x.Title) ? DefaultPinnedOrder[x.Title] : int.MaxValue)
            );


            try
            {
                InitializeComponent();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.ToString() + "Make sure you have the skins folder aside the main DirectStart exe", "We couldn't load the start menu."); }



			UserImageButton.Tag = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			userpfp.Source = IconHelper.GetUserTile(Environment.UserName).ToBitmapImage();
			PinnedItems.ItemsSource = Pinned;
			ProgramsList.ItemsSource = Programs;
			RecentApps.ItemsSource = Recent;

            PropertyGroupDescription startGroupDesc = new PropertyGroupDescription("Alph");
            SearchGlyph.Source = Properties.Resources.searchglyph.ToBitmapImage();

            // Try to get the username from WMI, will return the proper username if the user changed it in cpl
            var searcher = new ManagementObjectSearcher("SELECT FullName FROM Win32_UserAccount WHERE Name='" + Environment.UserName + "'");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["FullName"]?.ToString() == null)
                {
                    // WMI Username was empty, so falling back to classic method of getting username from directstart 2.x
                    UserNameText.Text = Environment.UserName;
                }
                else if (obj["FullName"]?.ToString() == "")
                {
                    // WMI Username was empty, so falling back to classic method of getting username from directstart 2.x
                    UserNameText.Text = Environment.UserName;
                }
                else if (obj["FullName"]?.ToString() == " ")
                {
                    // WMI Username was empty, so falling back to classic method of getting username from directstart 2.x
                    UserNameText.Text = Environment.UserName;
                }
                else
                {
                    UserNameText.Text = obj["FullName"]?.ToString();
                }
            }

            DUIColorize();
            LoadTiles();
			AdjustToTaskbarReworked();
            bool RoundedUserProfileShape = (bool)System.Windows.Application.Current.Resources["RoundedUserProfileShape"];
            bool Force10BetaStartButton = (bool)System.Windows.Application.Current.Resources["Force10BetaStartButton"];
            bool UseLegacyMenuIntercept = (bool)System.Windows.Application.Current.Resources["UseLegacyMenuIntercept"];
            bool DisableTiles = (bool)System.Windows.Application.Current.Resources["DisableTiles"];

            if (UseLegacyMenuIntercept == true)
            {
                // Load old intercepter if manually enabled
                _listener = new LegacyStartMenuIntercepter();
                _listener.StartTriggered += OnStartTriggered;
            }
            if (RoundedUserProfileShape)
            {
                // Make user avatar rounded
                UserRounderer.CornerRadius = new CornerRadius(999);
            }
            else
            {
                // or not 
                UserRounderer.CornerRadius = new CornerRadius(0);
            }
            if (TilesHost.Items.Count == 0)
            {
                Debug.WriteLine("[StartMenu.xaml.cs] No pinned tiles were detected or tiles were disabled in registry. Hiding tiles section.");
                Menu.Width = 273;
                StartMenuBackground.Width = 273;
            }
        }

        private void DUIColorize()
		{
            if (IsSkinSupportDuiBackgroundColor.Text == "True" || IsSkinSupportDuiBackgroundColor.Text == "true")
            {
                // DUI Colors for the main start menu grid:
                IntPtr pElementName = Marshal.StringToHGlobalUni(ImmersiveColors.ImmersiveStartBackground.ToString());
                System.Windows.Media.Color color = GetColor(pElementName);
                StartMenuBackground.Background = new SolidColorBrush(color);
                StartLogoTop.Background = new SolidColorBrush(color);
                StartLogoLeft.Background = new SolidColorBrush(color);
                StartLogoBottom.Background = new SolidColorBrush(color);
                StartLogoRight.Background = new SolidColorBrush(color);
            }
        }

        private double GetTaskbarHeight(Screen screen)
        {
            try
            {
                Rectangle bounds = screen.Bounds;
                Rectangle working = screen.WorkingArea;

                int heightDiff = bounds.Height - working.Height;
                int widthDiff = bounds.Width - working.Width;

                if (heightDiff > 0)
                    return heightDiff;

                if (widthDiff > 0)
                    return widthDiff;

                return 0;
            }
            catch
            {
                return 0;
            }
        }


        // P/Invoke declarations
        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("shell32.dll")]
        private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        private const uint ABM_GETTASKBARPOS = 5;

        private double GetTaskbarWidth()
        {
            // First, try to get taskbar width using Screen class
            double taskbarWidth = GetTaskbarWidthUsingScreenClass();

            // If the taskbar width obtained is non-negative, return it
            if (taskbarWidth >= 0)
                return taskbarWidth;

            // If the taskbar width obtained is negative, try an alternate method
            taskbarWidth = GetTaskbarWidthUsingShell32();

            return taskbarWidth;
        }

        private double GetTaskbarWidthUsingScreenClass()
        {
            try
            {
                // Get the working area of the screen (excluding the taskbar)
                Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;

                // Get the total area of the screen
                Rectangle screenArea = Screen.PrimaryScreen.Bounds;

                // Calculate the taskbar width by subtracting the working area width from the total screen width
                double taskbarWidth = screenArea.Width - workingArea.Width;

                return taskbarWidth;
            }
            catch
            {
                // Handle any exceptions gracefully
                return -1;
            }
        }

        private double GetTaskbarWidthUsingShell32()
        {
            try
            {
                APPBARDATA appBarData = new APPBARDATA();
                appBarData.cbSize = (uint)Marshal.SizeOf(appBarData);
                IntPtr result = SHAppBarMessage(ABM_GETTASKBARPOS, ref appBarData);
                if (result == IntPtr.Zero)
                    return -1;

                RECT taskbarRect = appBarData.rc;
                return taskbarRect.Right - taskbarRect.Left;
            }
            catch
            {
                return -1;
            }
        }
        private double _baseMenuHeight = -1;
        private double _baseMenuWidth = -1;


        private void AdjustToTaskbarReworked()
        {
            Screen screen = Screen.FromPoint(System.Windows.Forms.Cursor.Position);

            if (_baseMenuHeight < 0)
                _baseMenuHeight = Menu.Height;

            if (_baseMenuWidth < 0)
                _baseMenuWidth = Menu.Width;

            Menu.Height = _baseMenuHeight;
            // Menu.Width = _baseMenuWidth;
            Menu.Margin = new Thickness(0);
            Menu.Left = 0.0;

            base.Left = screen.WorkingArea.Left;
            base.Top = screen.WorkingArea.Top;

            StartLogoTop.Visibility = Visibility.Hidden;
            StartLogoBottom.Visibility = Visibility.Hidden;
            StartLogoLeft.Visibility = Visibility.Hidden;
            StartLogoRight.Visibility = Visibility.Hidden;

            taskbarheightinpx = GetTaskbarHeight(screen);
            double taskbarwidthinpx = GetTaskbarWidth();
            var taskbarPosition = GetTaskbarPosition.Taskbar.Position;
            Version osVersion = Environment.OSVersion.Version;

            bool showWin8Logo =
                (osVersion.Major == 6 && osVersion.Minor == 3) || Force10BetaStartButton;

            switch (taskbarPosition)
            {
                case GetTaskbarPosition.TaskbarPosition.Top:
                    StartMenuBackground.VerticalAlignment = VerticalAlignment.Bottom;
                    Menu.Height = _baseMenuHeight + taskbarheightinpx;

                    StartLogoTop.Visibility = showWin8Logo ? Visibility.Visible : Visibility.Hidden;
                    StartLogoTop.Height = taskbarheightinpx + 1;
                    Menu.Margin = new Thickness(0, taskbarheightinpx, 0, 0);
                    break;

                case GetTaskbarPosition.TaskbarPosition.Bottom:
                    StartMenuBackground.VerticalAlignment = VerticalAlignment.Top;
                    Menu.Height = _baseMenuHeight + taskbarheightinpx;
                    base.Top = screen.WorkingArea.Bottom - base.Height + taskbarheightinpx;

                    StartLogoBottom.Visibility = showWin8Logo ? Visibility.Visible : Visibility.Hidden;

                    StartLogoBottom.Height = taskbarheightinpx + 1;
                    Menu.Margin = new Thickness(0, 0, 0, taskbarheightinpx);
                    break;

                case GetTaskbarPosition.TaskbarPosition.Left:
                    Menu.Width = _baseMenuWidth + taskbarwidthinpx;

                    StartLogoLeft.Width = taskbarwidthinpx;
                    StartLogoLeft.Visibility = showWin8Logo ? Visibility.Visible : Visibility.Hidden;
                    break;

                case GetTaskbarPosition.TaskbarPosition.Right:
                    Menu.Width = _baseMenuWidth + taskbarwidthinpx;

                    StartLogoRight.Width = taskbarwidthinpx;
                    StartLogoRight.Visibility = showWin8Logo ? Visibility.Visible : Visibility.Hidden;
                    break;

                case GetTaskbarPosition.TaskbarPosition.Unknown:
                    StartMenuBackground.VerticalAlignment = VerticalAlignment.Top;
                    Menu.Height = _baseMenuHeight + taskbarheightinpx;
                    base.Top = screen.WorkingArea.Bottom - base.Height + taskbarheightinpx;

                    StartLogoBottom.Visibility = showWin8Logo ? Visibility.Visible : Visibility.Hidden;
                    StartLogoBottom.Height = taskbarheightinpx + 1;
                    Menu.Margin = new Thickness(0, 0, 0, taskbarheightinpx);
                    break;
            }
        }

        int maxfrequent = 5;
		int startfrequent = 0;

        private static string SmartCapitalizeFirstLetter(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            if (char.IsUpper(text[0]))
                return text;

            return char.ToUpper(text[0]) + text.Substring(1);
        }


        private static readonly string[] WindowsNameExceptions =
        {
            "Windows Media Center",
            "Windows Movie Maker",
            "Windows Media Player"
        };

        private string GetFrequentEntryName(string exePath)
        {
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                return string.Empty;

            try
            {
                var info = FileVersionInfo.GetVersionInfo(exePath);

                string name = null;

                if (!string.IsNullOrWhiteSpace(info.FileDescription))
                    name = info.FileDescription.Trim();

                else if (!string.IsNullOrWhiteSpace(info.ProductName))
                    name = info.ProductName.Trim();

                else if (!string.IsNullOrWhiteSpace(info.InternalName))
                    name = info.InternalName.Trim();

                else if (!string.IsNullOrWhiteSpace(info.OriginalFilename))
                    name = Path.GetFileNameWithoutExtension(info.OriginalFilename);

                else
                    name = Path.GetFileNameWithoutExtension(exePath);

                bool isException = WindowsNameExceptions.Any(e =>
                    string.Equals(e, name, StringComparison.OrdinalIgnoreCase));

                if (!isException &&
                    name.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    name = name.Replace("Windows", "").Replace("windows", "").Trim();
                }

                return SmartCapitalizeFirstLetter(name);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetFrequentEntryName error: {ex.Message}");
            }

            // Okay no other options, so we'll just use the exe filename...
            return SmartCapitalizeFirstLetter(Path.GetFileName(exePath));
        }

        private string GetExeName(string path)
        {
            try
            {
                return System.IO.Path.GetFileNameWithoutExtension(path);
            }
            catch
            {
                return path;
            }
        }


        private void GetFrequentsNew()
        {
            if (startfrequent > maxfrequent)
                return;

            try
            {
                RegistryKey reg = Registry.CurrentUser.OpenSubKey(SelectedSourceType.Key);
                List<CountEntry> sortedEntries = new List<CountEntry>();
                HashSet<string> addedTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (string valueName in reg.GetValueNames())
                {
                    CountEntry entry = new CountEntry
                    {
                        Name = valueName,
                        Value = (byte[])reg.GetValue(valueName),
                        RegKey = reg.ToString()
                    };

                    if (File.Exists(entry.DecodedName) || Directory.Exists(entry.DecodedName))
                    {
                        sortedEntries.Add(entry);
                    }
                }

                sortedEntries.Sort((a, b) => b.ExecutionCount.CompareTo(a.ExecutionCount));

                foreach (CountEntry entry in sortedEntries)
                {
                    if (startfrequent >= maxfrequent)
                        break;

                    string title = GetFrequentEntryName(entry.DecodedName);

                    if (string.IsNullOrWhiteSpace(title))
                        continue;

                    if (title.Equals("Command Processor", StringComparison.OrdinalIgnoreCase))
                        title = "Command Prompt";

                    if (title.Equals("Diskmgmt", StringComparison.OrdinalIgnoreCase))
                        title = "Disk Management";

                    if (title.Equals("ResourceHacker", StringComparison.OrdinalIgnoreCase))
                        title = "Resource Hacker";

                    if (title.Equals("Explorer", StringComparison.OrdinalIgnoreCase))
                        title = "File Explorer";

                    if (title.Length > 30)
                        title = GetExeName(entry.DecodedName);

                    string[] TSCNoFlyList =
                    {
                "Secondsystem",
                "Version Reporter Applet",
                "CompMgmtLauncher",
                "Control Panel",
                "DirectStart",
                "Start Menu",
                "ColorSync",
                "Setup",
                "Installer",
                "installation",
                "MMixConfig",
                "Notifications",
                "StartMenu",
                "Start menu"
            };

                    if (TSCNoFlyList.Any(b =>
                        title.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0))
                        continue;

                    if (!addedTitles.Add(title))
                        continue;

                    Recent.Add(new StartMenuLink
                    {
                        Title = title,
                        Icon = IconHelper.GetFileIcon(entry.DecodedName),
                        Link = entry.DecodedName
                    });

                    startfrequent++;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error reading UserAssist entries: " + ex.Message);
            }
        }



        private void UIElement_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			e.Handled = false;
		}


		private void Item_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (sender is StackPanel panel)
			{
				// Change the background color or any other visual properties
				panel.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 255, 255, 255));
			}
		}

		private void Item_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (sender is StackPanel panel)
			{
				// Revert back to the original background color or visual properties
				panel.Background = System.Windows.Media.Brushes.Transparent; // or any other color you desire
			}
		}


		private void PreparePinnedStartMenu() 
		{
			// Specify the directory path
			string targetDirectory = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				@"Microsoft\Internet Explorer\Quick Launch\User Pinned\");

			// Specify the folder name to check
			string folderToCheck = "StartMenu";

			// Check if the folder exists
			if (!Directory.Exists(Path.Combine(targetDirectory, folderToCheck)))
			{
				// If not, create the folder
				try
				{
					Directory.CreateDirectory(Path.Combine(targetDirectory, folderToCheck));
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Error Creatin Folder :C" + ex.ToString());
				}
			}
			else
			{
			}
		}

		public void LoadTiles()
		{
			TilesLoader TileAppHelper = new TilesLoader();
			Tiles = new ObservableCollection<Tile>();
			TileAppHelper.LoadTileGroups(Tiles);
			TilesHost.ItemsSource = Tiles;
		}

        public void OnStartTriggered(object sender, EventArgs e)
        {
            OnStartTriggeredNoArgs();
        }
        public void OnStartTriggeredNoArgs()
        {
            ToggleStartMenu();
        }

        private void Menu_Deactivated(object sender, EventArgs e)
        {
            if (IsVisible)
                HideMenu();
        }


        // Improved toggle logic with a "dirty" flicker fix, but if it works, it works i guess lmao

        private DateTime _lastHideTime;
        private const int HideCooldownMs = 100;

        private void HideMenu()
        {
            _lastHideTime = DateTime.UtcNow;
            Results.Clear();
            SearchText.Text = string.Empty;

            Hide();
            CloseProgramslist();
        }

        public static void ForceForegroundWindow(IntPtr hwnd)
        {
            uint windowThreadProcessId = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            uint currentThreadId = GetCurrentThreadId();
            uint CONST_SW_SHOW = 5;
            AttachThreadInput(windowThreadProcessId, currentThreadId, true);
            BringWindowToTop(hwnd);
            ShowWindow(hwnd, CONST_SW_SHOW);
            AttachThreadInput(windowThreadProcessId, currentThreadId, false);
        }

        public void ToggleStartMenu()
        {
            if ((DateTime.UtcNow - _lastHideTime).TotalMilliseconds < HideCooldownMs)
                return;
            if (IsVisible)
            {
                HideMenu();
                return;
            }
            DUIColorize();
            AdjustToTaskbarReworked();
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            ForceForegroundWindow(hwnd);
            this.Show();
            this.Focus();
            ProgramsList.Focus();
        }

        private void HandleCheck(object sender, RoutedEventArgs e)
		{
			GridPrograms.Visibility = Visibility.Visible;
			GridTogglable.Visibility = Visibility.Collapsed;
            ToggleButtonText.Visibility = Visibility.Collapsed;
            ToggleButtonTextBack.Visibility = Visibility.Visible;
            ToggleButtonGlyph.Text = "";
			ToggleButtonGlyph.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Symbol");
        }

		private void HandleUnchecked(object sender, RoutedEventArgs e)
		{
            CloseProgramslist();
        }

        private void CloseProgramslist()
        {
            GridPrograms.Visibility = Visibility.Collapsed;
            GridTogglable.Visibility = Visibility.Visible;
            ToggleButtonText.Visibility = Visibility.Visible;
            ToggleButtonTextBack.Visibility = Visibility.Collapsed;
            ToggleButtonGlyph.Text = "";
            ToggleButtonGlyph.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Symbol");
        }

        // Method to load Metro apps into the Programs list
        private void LoadMetroApps(string metroAppsDirectory)
        {
            // Assuming GetMetroApps is a static method and takes the directory where metro apps are stored
            var metroApps = MetroAppHelper.GetMetroApps(metroAppsDirectory);

            // Add each metro app to the ObservableCollection
            foreach (var metroApp in metroApps)
            {
                Programs.Add(new MetroApp
                {
                    Name = metroApp.Name,
					Path = metroApp.Path,
                    Icon = metroApp.Icon,  // You can display an icon, or handle it as needed
                    Identity = metroApp.Identity
                });
            }
        }


        // Blacklist of useless shitty entries
        private static readonly HashSet<string> useless = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
        {
            "Desktop",
            "DirectStart",
            "StartMenu",
            "Immersive Control Panel",
            "Search"
        };

        // replace dumb internal system titles
        private static readonly Dictionary<string, string> dumbtitlereplacements =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Windows.Defender", "Windows Defender" },
            { "computer", "This PC" },
            { "StartUp", "Startup" }
        };

        private void GetPrograms(string directory)
        {
            foreach (string f in Directory.GetFiles(directory))
            {
                if (Path.GetExtension(f) == ".ini")
                    continue;

                string title = Path.GetFileNameWithoutExtension(f);
                if (useless.Contains(title))
                    continue;
                if (dumbtitlereplacements.TryGetValue(title, out string replacedTitle))
                {
                    title = replacedTitle;
                }

                Programs.Add(new StartMenuLink
                {
                    Title = title,
                    Icon = IconHelper.GetFileIcon(f),
                    Link = f
                });
            }

            GetProgramsRecurse(directory);
        }


        private void GetPinned(string directory)
		{
			foreach (string f in Directory.GetFiles(directory))
			{
				if (System.IO.Path.GetExtension(f) != ".ini")
				{
					Pinned.Add(new StartMenuLink
					{
						Title = System.IO.Path.GetFileNameWithoutExtension(f),
						Icon = IconHelper.GetFileIcon(f),
						Link = f
					});
				}
			}
			GetProgramsRecurse(directory);
		}
        private void GetProgramsRecurse(string directory, StartMenuDirectory parent = null)
        {
            bool hasParent = parent != null;

            foreach (string d in Directory.GetDirectories(directory))
            {
                string dirTitle = new DirectoryInfo(d).Name;
                if (useless.Contains(dirTitle))
                    continue;
                if (dumbtitlereplacements.TryGetValue(dirTitle, out string replacedDirTitle))
                {
                    dirTitle = replacedDirTitle;
                }

                StartMenuDirectory folderEntry = null;

                if (!hasParent)
                {
                    folderEntry = Programs
                        .FirstOrDefault(x => x.Title.Equals(dirTitle, StringComparison.OrdinalIgnoreCase))
                        as StartMenuDirectory;
                }

                if (folderEntry == null)
                {
                    folderEntry = new StartMenuDirectory
                    {
                        Title = dirTitle,
                        Links = new ObservableCollection<StartMenuLink>(),
                        Directories = new ObservableCollection<StartMenuDirectory>(),
                        Link = d,
                        Icon = IconHelper.GetFolderIcon(d)
                    };
                }
                GetProgramsRecurse(d, folderEntry);

                foreach (string f in Directory.GetFiles(d))
                {
                    if (Path.GetExtension(f) == ".ini")
                        continue;

                    string fileTitle = Path.GetFileNameWithoutExtension(f);
                    if (useless.Contains(fileTitle))
                        continue;
                    if (dumbtitlereplacements.TryGetValue(fileTitle, out string replacedFileTitle))
                    {
                        fileTitle = replacedFileTitle;
                    }

                    folderEntry.HasChildren = true;

                    folderEntry.Links.Add(new StartMenuLink
                    {
                        Title = fileTitle,
                        Icon = IconHelper.GetFileIcon(f),
                        Link = f
                    });
                }

                if (!hasParent)
                {
                    if (!Programs.Contains(folderEntry))
                    {
                        Programs.Add(folderEntry);
                    }
                }
                else
                {
                    parent.Directories.Add(folderEntry);
                }
            }
        }


        private void Link_Click(object sender, RoutedEventArgs e)
		{
			Link_Click(sender, null);
		}

		private void Tile_Click(object sender, RoutedEventArgs e)
		{
			Tile_Click(sender, null);
		}
		private void Tile_Click(object sender, MouseButtonEventArgs e)
		{
			Tile data = (sender as FrameworkElement).DataContext as Tile;
			this.Hide();
            try
            {
                Process.Start(data.Path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "This app couldn't be started", MessageBoxButton.OK);
            }
        }

		private void BrowseLink_Click(object sender, RoutedEventArgs e)
		{
			StartMenuLink data = (sender as FrameworkElement).DataContext as StartMenuLink;
			this.Hide();
			Process.Start("explorer.exe", $"/select, \"{data.Link}\"");
		}

		private void Link_Click(object sender, MouseButtonEventArgs e)
		{
			StartMenuLink data = (sender as FrameworkElement).DataContext as StartMenuLink;
			this.Hide();
            try
            {
                Process.Start(data.Link);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "This app couldn't be started", MessageBoxButton.OK);
            }
		}
        private void LinkRunAsAdmin(object sender, MouseButtonEventArgs e)
        {
            StartMenuLink data = (sender as FrameworkElement).DataContext as StartMenuLink;
            this.Hide();
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = data.Link,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "This app couldn't be started", MessageBoxButton.OK);
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var searchTerm = SearchText.Text;
			Search(searchTerm);
		}
		Thread _searchThread = null;
		private void Search(string searchTerm)
		{
			if(_searchThread != null)
			{
				_searchThread.Abort();
			}
			Results.Clear();
			if (searchTerm.Length == 0)
			{
				return;
			}
			_searchThread = new Thread(() => { 
				var matches = Programs.Search(searchTerm);
				foreach(StartMenuLink link in matches)
				{
					Dispatcher.InvokeAsync(() => { 
						Results.Add(new StartMenuSearchResult
						{
							Icon = link.Icon,
							Link = link.Link,
							Title = link.Title,
							ResultType = ResultType.Apps
						});
					});
				}
				SearchDocuments(searchTerm);
			});
			_searchThread.Start();
		}

		private void Folder_Click(object sender, RoutedEventArgs e)
		{
			string folder = (sender as System.Windows.Controls.Control).Tag as String;
			if(folder == string.Empty)
			{
				folder = "explorer.exe";
			}
			Process.Start(folder);
		}

		private void SearchDocuments(string searchTerm)
		{
			using (var connection = new OleDbConnection(@"Provider=Search.CollatorDSO;Extended Properties=""Application=Windows"""))
			{
				var query = @"SELECT TOP 15 System.ItemName, System.ItemUrl, System.ItemType FROM SystemIndex " +
				$@"WHERE scope ='file:{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}' {(searchTerm.Length < 3 ? "AND System.ItemType = 'Directory'" : "")} AND System.ItemName LIKE '%{searchTerm}%'";
				connection.Open();
				using (var command = new OleDbCommand(query, connection))
				using (var r = command.ExecuteReader())
				{
					while (r.Read())
					{
						string fileName = r[0] as string;
						string filePath = r[1] as string;
						string itemType = r[2] as string;
						filePath = filePath.Remove(0, 5).Replace("/", "\\");
						Dispatcher.InvokeAsync(() =>
						{
							Results.Add(new StartMenuSearchResult
							{
								Title = itemType == "Directory" ? fileName : System.IO.Path.GetFileName(fileName),
								Icon = IconHelper.GetFileIcon(filePath),//need cache for performance
								Link = filePath,
								AllowOpenLocation = itemType == "Directory" ? false : true,
								ResultType = ResultType.Files
							});
						});
					}
				}
			}
		}

		private void PowerButton_Click(object sender, RoutedEventArgs e)
		{
			PowerMenu.PlacementTarget = sender as UIElement;
			PowerMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
			PowerMenu.IsOpen = true;
		}
		public void RunCommand(string command)
		{
			Task.Factory.StartNew(() => { 

				ProcessStartInfo processStartInfo;

				processStartInfo = new ProcessStartInfo(command.Split(' ')[0]);
				processStartInfo.Arguments = command.Remove(0, command.Split(' ')[0].Length);
				processStartInfo.CreateNoWindow = true;
				processStartInfo.UseShellExecute = true;

				try
				{
					Process.Start(processStartInfo);
				}
				catch(Exception ex)
				{
					Dispatcher.Invoke(() => { System.Windows.MessageBox.Show(ex.Message); });
				}
			});
		}

		private void Shutdown(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("shutdown.exe", "-s -t 0");
		}

		private void Sleep(object sender, RoutedEventArgs e)
		{
			System.Windows.Forms.Application.SetSuspendState(PowerState.Suspend, true, true);
		}

		private void Restart(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("shutdown.exe", "-r -t 0");
		}

		private void Hibernate(object sender, RoutedEventArgs e)
		{
			System.Windows.Forms.Application.SetSuspendState(PowerState.Hibernate, true, true);
		}

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private void UserImageButton_Click(object sender, RoutedEventArgs e)
        {
			UserMenu.PlacementTarget = sender as UIElement;
			UserMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
			UserMenu.IsOpen = true;
		}

        private void sleepPC_Click(object sender, RoutedEventArgs e)
        {
			System.Windows.Forms.Application.SetSuspendState(PowerState.Suspend, true, true);
		}

		private void shutdownPC_Click(object sender, RoutedEventArgs e)
        {
			System.Diagnostics.Process.Start("shutdown.exe", "-s -t 0");
		}

		private void restartPC_Click(object sender, RoutedEventArgs e)
        {
			System.Diagnostics.Process.Start("shutdown.exe", "-r -t 0");
		}

		private void ExitDS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get all processes named "StartMenu.exe"
                var processes = Process.GetProcessesByName("StartMenu");

                if (processes.Length == 0)
                {
                    return;
                }

                // Kill each found process
                foreach (var process in processes)
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void Menu_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowText(hwnd, "StartMenu"); 
            GridPrograms.Visibility = Visibility.Collapsed;
		}

        private void StartLogo_Click(object sender, RoutedEventArgs e)
        {
			OnStartTriggered(sender, e);
        }

        private void TileResizerDynamic(object sender, string sizetoresizeto)
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var tile = menuItem?.DataContext as Tile;
            if (tile == null) return;

            tile.Size = sizetoresizeto; // Update in memory

            // Update XML
            string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "PinnedTilesDS.xml");
            if (File.Exists(configFile))
            {
                try
                {
                    XDocument doc = XDocument.Load(configFile);

                    // Find matching Tile node by path
                    var tileElement = doc.Descendants("Tile")
                        .FirstOrDefault(x => x.Element("Path")?.Value == tile.Path);

                    if (tileElement != null)
                    {
                        tileElement.SetElementValue("Size", sizetoresizeto); // Update or create <Size> element
                        doc.Save(configFile); // Save changes
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"(ResizeTileSmall_Click) Error updating XML: {ex.Message}");
                }
            }

            // Reload UI
            TilesHost.ItemsSource = null;
            LoadTiles();
            TilesHost.ItemsSource = Tiles;
        }


        private void ResizeTileSmall_Click(object sender, RoutedEventArgs e)
        {
			TileResizerDynamic(sender, "Small");
        }

        private void ResizeTileNormal_Click(object sender, RoutedEventArgs e)
        {
            TileResizerDynamic(sender, "Normal");
        }

        private void ResizeTileWide_Click(object sender, RoutedEventArgs e)
        {
            TileResizerDynamic(sender, "Wide");
        }

        private void ResizeTileLarge_Click(object sender, RoutedEventArgs e)
        {
            TileResizerDynamic(sender, "Large");
        }

        private void SearchText_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Check if any alphanumeric key is pressed
            if (char.IsLetterOrDigit((char)e.Key))
            {
                string rundll32Path = Environment.ExpandEnvironmentVariables(@"%windir%\system32\rundll32.exe");
                string command = @"-sta {C90FB8CA-3295-4462-A721-2935E83694BA}";

                ProcessStartInfo startInfo = new ProcessStartInfo(rundll32Path, command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                try
                {
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("An error occurred: " + ex.Message);
                }
            }
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            string rundll32Path = Environment.ExpandEnvironmentVariables(@"%windir%\system32\rundll32.exe");
            string command = @"-sta {C90FB8CA-3295-4462-A721-2935E83694BA}";

            ProcessStartInfo startInfo = new ProcessStartInfo(rundll32Path, command)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("An error occurred: " + ex.Message);
            }
        }

        private void OpenUserAccountSettings()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Packages\windows.immersivecontrolpanel_cw5n1h2txyewy\LocalState\Indexed\Settings\en-US\AAA_SettingsPageAccountsPicture.settingcontent-ms"
            );

            Process.Start("explorer.exe", path);
        }


        private void UserImageChangeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenUserAccountSettings();
        }

        private void LockMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LockWorkStation();
        }

        const uint EWX_LOGOFF = 0x00000000;
        private void SignOutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Log off the current user
            ExitWindowsEx(EWX_LOGOFF, 0);
        }

        private void UnpinTile_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var tile = menuItem?.DataContext as Tile;
            if (tile == null) return;

            string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "PinnedTilesDS.xml");

            if (File.Exists(configFile))
            {
                try
                {
                    XDocument doc = XDocument.Load(configFile);

                    // Find and remove the matching tile node
                    var tileElement = doc.Descendants("Tile")
                        .FirstOrDefault(x => x.Element("Path")?.Value == tile.Path);

                    if (tileElement != null)
                    {
                        tileElement.Remove();
                        doc.Save(configFile);
                        Debug.WriteLine($"(UnpinTile_Click) Tile '{tile.Title}' unpinned and removed from XML.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"(UnpinTile_Click) Error updating XML: {ex.Message}");
                }
            }

            // Remove from the in-memory collection
            Tiles.Remove(tile);

            // Refresh the ItemsControl
            TilesHost.ItemsSource = null;
            TilesHost.ItemsSource = Tiles;
            if (TilesHost.Items.Count == 0)
            {
                Debug.WriteLine("(StartMenu.xaml.cs) No pinned tiles were detected. Hiding tiles section.");
                Menu.Width = 273;
                StartMenuBackground.Width = 273;
            }
        }

        private void TileRunAsAdmin_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var tile = menuItem?.DataContext as Tile;

            if (tile == null || string.IsNullOrEmpty(tile.Path))
                return;

            try
            {
                this.Hide();

                var startInfo = new ProcessStartInfo
                {
                    FileName = tile.Path,
                    UseShellExecute = true,
                    Verb = "runas" 
                };

                Process.Start(startInfo);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                MessageBox.Show(ex.ToString(), "This tile couldn't be launched", MessageBoxButton.OK);
            }
        }
        private void OpenFileLocationTile_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var tile = menuItem?.DataContext as Tile;
            this.Hide();
            Process.Start("explorer.exe", $"/select, \"{tile.Path}\"");
        }

        private void PinTaskbarTile_Click(object sender, RoutedEventArgs e)
        {


        }


        string GetShortcutTarget(string shortcutPath)
        {
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            return shortcut.TargetPath;
        }
        private void pinstart_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var link = menuItem?.DataContext as StartMenuLink;
            if (link == null || string.IsNullOrWhiteSpace(link.Link)) return;

            string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "PinnedTilesDS.xml");

            try
            {
                XDocument doc;

                // Load or initialize XML
                if (File.Exists(configFile))
                {
                    doc = XDocument.Load(configFile);
                }
                else
                {
                    doc = new XDocument(new XElement("Tiles"));
                }

                // Check if already pinned
                var alreadyPinned = doc.Descendants("Tile")
                    .Any(x => x.Element("Path")?.Value == link.Link);

                if (alreadyPinned)
                {
                    Debug.WriteLine($"(pinstart_Click) Tile already pinned: {link.Title}");
                    return;
                }

                // Create new <Tile> element
                XElement newTile = new XElement("Tile",
                    new XElement("Path", link.Link),
                    new XElement("PathMetro", ""), // Optional for Metro links
                    new XElement("Size", "Normal"),
                    new XElement("IsLiveTileEnabled", "false"),
                    new XElement("TileColor", "default") // generate dynamically
                );

                doc.Root.Add(newTile);
                doc.Save(configFile);

                // Build Tile object to display immediately
                Tile pinnedTile = new Tile
                {
                    Title = System.IO.Path.GetFileNameWithoutExtension(link.Link),
                    Path = link.Link,
                    EXEPath = GetShortcutTarget(link.Link),
                    PathMetro = "",
                    Size = "Normal",
                    Icon = ExtractArbitrarySizeIcon(GetShortcutTarget(link.Link), 2),
                    IsLiveTileEnabled = false,
                    LeftGradient = TileColorCalculator.CalculateLeftGradient(
                        IconHelper.GetLargeFileIcon(link.Link),
                        link.Title, "default"),
                    RightGradient = TileColorCalculator.CalculateRightGradient(
                        IconHelper.GetLargeFileIcon(link.Link),
                        link.Title, "default"),
                    Border = TileColorCalculator.CalculateBorder(
                        IconHelper.GetLargeFileIcon(link.Link),
                        link.Title, "default")
                };

                // Add to tiles list and refresh UI
                Tiles.Add(pinnedTile);
                TilesHost.ItemsSource = null;
                TilesHost.ItemsSource = Tiles;

                Debug.WriteLine($"(pinstart_Click) Pinned new tile: {pinnedTile.Title}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"(pinstart_Click) Error pinning tile: {ex.Message}");
            }
            if (TilesHost.Items.Count > 0)
            {
                Debug.WriteLine("(StartMenu.xaml.cs) A tile was pinned. Unhiding tiles section.");
                Menu.Width = 533;
                StartMenuBackground.Width = 533;
            }
        }
        private void pinstart_Click_fromuserassistlist(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var link = menuItem?.DataContext as StartMenuLink;
            if (link == null || string.IsNullOrWhiteSpace(link.Link)) return;

            string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "PinnedTilesDS.xml");

            try
            {
                XDocument doc;

                // Load or initialize XML
                if (File.Exists(configFile))
                {
                    doc = XDocument.Load(configFile);
                }
                else
                {
                    doc = new XDocument(new XElement("Tiles"));
                }

                // Check if already pinned
                var alreadyPinned = doc.Descendants("Tile")
                    .Any(x => x.Element("Path")?.Value == link.Link);

                if (alreadyPinned)
                {
                    Debug.WriteLine($"(pinstart_Click) Tile already pinned: {link.Title}");
                    return;
                }

                // Create new <Tile> element
                XElement newTile = new XElement("Tile",
                    new XElement("Path", link.Link),
                    new XElement("PathMetro", ""), // Optional for Metro links
                    new XElement("Size", "Normal"),
                    new XElement("IsLiveTileEnabled", "false"),
                    new XElement("TileColor", "default") // generate dynamically
                );

                doc.Root.Add(newTile);
                doc.Save(configFile);

                // Build Tile object to display immediately
                Tile pinnedTile = new Tile
                {
                    Title = System.IO.Path.GetFileNameWithoutExtension(link.Link),
                    Path = link.Link,
                    EXEPath = GetShortcutTarget(link.Link),
                    PathMetro = "",
                    Size = "Normal",
                    Icon = IconHelper.GetFileIcon(link.Link),
                    IsLiveTileEnabled = false,
                    LeftGradient = TileColorCalculator.CalculateLeftGradient(
                        IconHelper.GetLargeFileIcon(link.Link),
                        link.Title, "default"),
                    RightGradient = TileColorCalculator.CalculateRightGradient(
                        IconHelper.GetLargeFileIcon(link.Link),
                        link.Title, "default"),
                    Border = TileColorCalculator.CalculateBorder(
                        IconHelper.GetLargeFileIcon(link.Link),
                        link.Title, "default")
                };

                // Add to tiles list and refresh UI
                Tiles.Add(pinnedTile);
                TilesHost.ItemsSource = null;
                TilesHost.ItemsSource = Tiles;

                Debug.WriteLine($"(pinstart_Click) Pinned new tile: {pinnedTile.Title}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"(pinstart_Click) Error pinning tile: {ex.Message}");
            }
            if (TilesHost.Items.Count > 0)
            {
                Debug.WriteLine("(StartMenu.xaml.cs) A tile was pinned. Unhiding tiles section.");
                Menu.Width = 533;
                StartMenuBackground.Width = 533;
            }
        }


        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WH_START_TRIGGERED)
            {
                OnStartTriggeredNoArgs();
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void Menu_SourceInitialized(object sender, EventArgs e)
        {
            if (UseLegacyMenuIntercept == false)
            {
                HwndSource source = (HwndSource)HwndSource.FromVisual(this);
                source.AddHook(WndProc);
            }
        }

        private void Menu_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                OnStartTriggered(sender, e);
                e.Handled = true;
            }
        }

        private void runasadmin_Click(object sender, RoutedEventArgs e)
        {
            LinkRunAsAdmin(sender, null);
        }
    }

    public class RunCommand : ICommand
	    {
		public delegate void ExecuteMethod();
		private Action<string> func;
		public RunCommand(Action<string> exec)
		{
			func = exec;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public event EventHandler CanExecuteChanged;

		public void Execute(object parameter)
		{
			func(parameter as string);
		}
	}
}
