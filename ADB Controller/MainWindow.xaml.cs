using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ADB_Controller
{
    public enum Command { devices, kill, reboot, push, pull, install, home, info, clear };
    public enum FindCommand { project, app_auto, app, adb };
    public enum FileFormat { gradle, apk }

    public static class Constants
    {
        public static readonly string DESKTOP_PATH = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public const string APK_FILE_PATH = @"app\build\outputs\apk\debug\";
    }

    public partial class MainWindow : Window
    {
        System.Diagnostics.Process process;

        public MainWindow()
        {
            InitializeComponent();
            ProjectPath.Text = Properties.Settings.Default.defaultPath;
            if(!ProjectPath.Text.Equals("")) { }
                GetAPKName(ProjectPath.Text);
            ADBPath.Text = "vendor/app/";
        }

        private void StartCmd()
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = @"cmd",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };

            process = new System.Diagnostics.Process
            {
                StartInfo = startInfo
            };
            process.Start();
        }

        private void OnClickCommand(object sender, RoutedEventArgs e)
        {
            Command commmand;
            Enum.TryParse(((Button)sender).Tag.ToString(), out commmand);

            switch (commmand)
            {
                case Command.devices: CommandAsync("adb devices"); break;
                case Command.kill: CommandAsync("adb kill-server"); break;
                case Command.reboot: CommandAsync("adb reboot"); break;
                case Command.push:
                    {
                        try
                        {
                            string oldpath  = ProjectPath.Text + Constants.APK_FILE_PATH + APKName.Text;
                            string path     = ProjectPath.Text + Constants.APK_FILE_PATH + APKName.Text.Remove(APKName.Text.IndexOf("_"), APKName.Text.Length - APKName.Text.IndexOf("_") - 4);
                            if (File.Exists(path) && !oldpath.Equals(path))
                                File.Delete(path);
                            File.Move(oldpath, path);
                            GetAPKName(ProjectPath.Text);
                            CommandAsync("adb push " + path + " " + ADBPath.Text);
                        }
                        catch (ArgumentOutOfRangeException ex)
                        {
                            Debug.Write(ex.StackTrace);
                            CommandAsync("adb push " + ProjectPath.Text + Constants.APK_FILE_PATH + APKName.Text + " " + ADBPath.Text);
                        }

                        break;
                    }
                case Command.pull:
                    {
                        //CommandAsync("adb pull");
                        break;
                    }
                case Command.install:
                    {
                        CommandAsync("adb install -r " + ProjectPath.Text + Constants.APK_FILE_PATH + APKName.Text);
                        break;
                    }
                case Command.home:
                    {
                        CommandAsync("adb shell am start com.android.launcher/com.android.launcher2.Launcher");
                        break;
                    }
                case Command.info:
                    {
                        CommandAsync("adb shell \"dumpsys window | grep -E 'mCurrentFocus|mFocusedApp'\"");
                        break;
                    }
                case Command.clear:
                    {
                        ResultBox.Text = "";
                        break;
                    }
            }
            Properties.Settings.Default.defaultPath = ProjectPath.Text;
            Properties.Settings.Default.Save();
        }

        private void CommandAsync(string command_line)
        {
            StartCmd();
            process.StandardInput.Write(command_line + Environment.NewLine);
            process.StandardInput.Close();

            String result = process.StandardOutput.ReadToEnd();
            ResultBox.Text = result.Replace(Environment.CurrentDirectory, "");

            process.Close();
        }


        private void OnClickFindCommand(object sender, RoutedEventArgs e)
        {
            FindCommand commmand;
            Enum.TryParse(((Button)sender).Tag.ToString(), out commmand);

            switch (commmand)
            {
                case FindCommand.project:
                    {
                        ProjectPath.Text = FindFile(FileFormat.gradle);
                        break;
                    }
                case FindCommand.app_auto:
                    {
                        GetAPKName(ProjectPath.Text);
                        break;
                    }
                case FindCommand.app:
                    {
                        APKName.Text = FindFile(FileFormat.apk);
                        break;
                    }
                case FindCommand.adb:
                    {

                        break;
                    }
            }
        }

        private string FindFile(FileFormat format)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                InitialDirectory = Constants.DESKTOP_PATH
            };
            if (format.Equals(FileFormat.apk))
                dlg.Filter = "Android Application files (*.apk)|*.apk";
            else if (format.Equals(FileFormat.gradle))
                dlg.Filter = "Android Gradle files (*.gradle)|*.gradle";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                if (!String.IsNullOrWhiteSpace(dlg.FileName))
                {
                    if (format.Equals(FileFormat.apk))
                        return dlg.SafeFileName;
                    else if (format.Equals(FileFormat.gradle))
                        return dlg.FileName.Replace(dlg.SafeFileName, "");
                }
                return "NOPE";
            }
            else
            {
                return "NOPE";
            }
        }

        private void GetAPKName(string projectPath)
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(projectPath + Constants.APK_FILE_PATH);
                FileInfo[] files = dirInfo.GetFiles("*.apk");
                APKName.Text = files[files.Length - 1].Name;
            }
            catch (DirectoryNotFoundException dex)
            {
                Debug.Write(dex.Data);
                APKName.Text = "DIR Not Exists!";
            }
            catch (IndexOutOfRangeException iex)
            {
                Debug.Write(iex.Data);
                APKName.Text = "APK Not Exists!";
            }
        }
    }
}
