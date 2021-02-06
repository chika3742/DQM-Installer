using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace DQM_Installer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        string mcPath = Environment.GetEnvironmentVariable("appdata") + "\\.minecraft";
        string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string tempPath = Path.Combine(Environment.GetEnvironmentVariable("temp"), "extract-dqm");

        string procedure = "";
        public MainWindow()
        {
            InitializeComponent();

            window.Title = "DQM 半自動インストーラー " + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;
            Skin.Text = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "assets\\steve.png");
        }

        private void YouTubeButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://youtu.be/Dgy8i-YAH48");
        }

        private void ReferPremiseMOD_Click(object sender, RoutedEventArgs e)
        {
            var path = ShowFilePicker("DQM 前提MOD|*.zip", "DQM 前提MODの選択");
            if (path != null)
            {
                PremiseMod.Text = path;
            }
        }

        private void ReferBodyMOD_Click(object sender, RoutedEventArgs e)
        {
            var path = ShowFilePicker("DQM 本体MOD|*.zip", "DQM 本体MODの選択");
            if (path != null)
            {
                BodyMod.Text = path;
            }
        }

        private void ReferDQMSound_Click(object sender, RoutedEventArgs e)
        {
            var path = ShowFilePicker("DQM 音声・BGM|*.zip", "DQM 音声・BGMの選択");
            if (path != null)
            {
                Sound.Text = path;
            }
        }

        private void ReferForge_Click(object sender, RoutedEventArgs e)
        {
            var path = ShowFilePicker("Forge 1.5.2|*.zip", "Forgeの選択");
            if (path != null)
            {
                Forge.Text = path;
            }
        }

        private void ReferLib_Click(object sender, RoutedEventArgs e)
        {
            var path = ShowFilePicker("Forge ライブラリファイル|*.zip", "Forge libファイルの選択");
            if (path != null)
            {
                ForgeLib.Text = path;
            }
        }

        private string ShowFilePicker(string filter, string title)
        {
            var picker = new OpenFileDialog();
            picker.Filter = filter;
            picker.Title = title;
            if (picker.ShowDialog() == true)
            {
                return picker.FileName;
            }
            else
            {
                return null;
            }
        }

        private void ChangeEnabled(bool to)
        {
            PremiseMod.IsEnabled = to;
            ReferPremiseMod.IsEnabled = to;
            BodyMod.IsEnabled = to;
            ReferBodyMod.IsEnabled = to;
            Sound.IsEnabled = to;
            ReferSound.IsEnabled = to;
            Forge.IsEnabled = to;
            ReferForge.IsEnabled = to;
            ForgeLib.IsEnabled = to;
            ReferForgeLib.IsEnabled = to;
            Skin.IsEnabled = to;
            ReferSkin.IsEnabled = to;
            DisplayVersion.IsEnabled = to;
            InstallBtn.IsEnabled = to;
            ModeComboBox.IsEnabled = to;

            var mode = ModeComboBox.SelectedIndex;
            TextSkinFile.Content = "スキンファイル(オプション)";
            TextVersionName.Content = "ランチャーでのバージョン表記";
            switch (mode)
            {
                case 1:
                    TextVersionName.Content = "バージョン名";
                    Sound.IsEnabled = false;
                    ReferSound.IsEnabled = false;
                    Forge.IsEnabled = false;
                    ReferForge.IsEnabled = false;
                    ForgeLib.IsEnabled = false;
                    ReferForgeLib.IsEnabled = false;
                    Skin.IsEnabled = false;
                    ReferSkin.IsEnabled = false;
                    break;
                case 2:
                    TextSkinFile.Content = "スキンファイル";
                    TextVersionName.Content = "バージョン名";
                    PremiseMod.IsEnabled = false;
                    ReferPremiseMod.IsEnabled = false;
                    BodyMod.IsEnabled = false;
                    ReferBodyMod.IsEnabled = false;
                    Sound.IsEnabled = false;
                    ReferSound.IsEnabled = false;
                    Forge.IsEnabled = false;
                    ReferForge.IsEnabled = false;
                    ForgeLib.IsEnabled = false;
                    ReferForgeLib.IsEnabled = false;
                    break;
            }

            if (to)
            {
                InstallBtn.Content = "インストール";
                BeginStoryboard(FindResource("BtnRestoreAnimation") as Storyboard);
            }
            else
            {
                InstallBtn.Content = "インストール中";
                BeginStoryboard(FindResource("BtnAnimation") as Storyboard);
            }

        }

        private void Install_Click(object sender, RoutedEventArgs e)
        {
            ChangeEnabled(false);
            ProgressBar.Maximum = 9;
            ProgressBar.Value = 0;

            if (ModeComboBox.SelectedIndex == 2)
            {
                var strBuilder = new StringBuilder();
                
                if (Skin.Text == "") strBuilder.AppendLine("スキンのパスを入力してください。");
                else if (!File.Exists(Skin.Text)) strBuilder.AppendLine("スキンに選択されたファイルは存在しません。");
                if (DisplayVersion.Text == "") strBuilder.AppendLine("バージョンIDを入力してください。");
                else if (!File.Exists(Path.Combine(mcPath, "versions", DisplayVersion.Text, $"{DisplayVersion.Text}.jar")))
                {
                    strBuilder.AppendLine("入力されたバージョン名を持つバージョンは見つかりませんでした。");
                }
                if (strBuilder.ToString() != "")
                {
                    MessageBox.Show(strBuilder.ToString(), "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    ChangeEnabled(true);
                }
                else
                {
                    OnlySetSkin();
                }

                return;
            } else if (ModeComboBox.SelectedIndex == 1)
            {
                var msgBuilder2 = new StringBuilder();
                if (PremiseMod.Text == "") msgBuilder2.AppendLine("前提MODを選択してください。");
                else if (!File.Exists(PremiseMod.Text)) msgBuilder2.AppendLine("前提MODに選択されたファイルは存在しません。");
                if (BodyMod.Text == "") msgBuilder2.AppendLine("本体MODを選択してください。");
                else if (!File.Exists(BodyMod.Text)) msgBuilder2.AppendLine("本体MODに選択されたファイルは存在しません。");
                if (DisplayVersion.Text == "") msgBuilder2.AppendLine("バージョンIDを入力してください。");
                else if (!File.Exists(Path.Combine(mcPath, "versions", DisplayVersion.Text, $"{DisplayVersion.Text}.jar")))
                {
                    msgBuilder2.AppendLine("入力されたバージョン名を持つバージョンは見つかりませんでした。");
                }
                if (msgBuilder2.ToString() != "")
                {
                    MessageBox.Show("入力に不備があります。\n\n" + msgBuilder2.ToString(), "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    ChangeEnabled(true);
                    return;
                }
                ProgressBar.Maximum = 3;
                Update();
                return;
            }

            var msgBuilder = new StringBuilder();
            if (PremiseMod.Text == "") msgBuilder.AppendLine("前提MODを選択してください。");
            else if (!File.Exists(PremiseMod.Text)) msgBuilder.AppendLine("前提MODに選択されたファイルは存在しません。");
            if (BodyMod.Text == "") msgBuilder.AppendLine("本体MODを選択してください。");
            else if (!File.Exists(BodyMod.Text)) msgBuilder.AppendLine("本体MODに選択されたファイルは存在しません。");
            if (Sound.Text == "") msgBuilder.AppendLine("DQM音声・BGMを選択してください。");
            else if (!File.Exists(Sound.Text)) msgBuilder.AppendLine("DQM音声・BGMに選択されたファイルは存在しません。");
            if (Forge.Text == "") msgBuilder.AppendLine("Forgeを選択してください。");
            else if (!File.Exists(Forge.Text)) msgBuilder.AppendLine("Forgeに選択されたファイルは存在しません。");
            if (ForgeLib.Text == "") msgBuilder.AppendLine("Forge libファイルを選択してください。");
            else if (!File.Exists(ForgeLib.Text)) msgBuilder.AppendLine("Forge libファイルに選択されたファイルは存在しません。");
            if (Skin.Text == "") msgBuilder.AppendLine("スキンを選択してください。");
            else if (!File.Exists(Skin.Text)) msgBuilder.AppendLine("スキンに選択されたファイルは存在しません。");

            if (DisplayVersion.Text == "") msgBuilder.AppendLine("バージョン表示名を入力してください。");

            if (msgBuilder.ToString() != "")
            {
                MessageBox.Show("入力に不備があります。\n\n" + msgBuilder.ToString(), "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                ChangeEnabled(true);
                return;
            }

            Install();
        }

        private async void Update()
        {
            var premisePath = PremiseMod.Text;
            var displayVersion = DisplayVersion.Text;
            var jarPath = $"{mcPath}\\versions\\{displayVersion}\\{displayVersion}.jar";
            var bodyPath = BodyMod.Text;
            await Task.Run(() =>
            {
                ExtractPreAndBody(premisePath, jarPath, bodyPath, true);
                MessageBox.Show("アップデートが完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                CancelInstalling();
            });
        }

        private async void Install()
        {
            await Task.Run(() => {
                try
                {

                    SendProgress("DQM用バージョンを作成しています。");

                    string displayVersion = "";
                    string forgePath = "";
                    string premisePath = "";
                    string bodyPath = "";
                    string libPath = "";
                    string sePath = "";
                    string skinPath = "";
                    Dispatcher.Invoke(() =>
                    {
                        displayVersion = DisplayVersion.Text;
                        forgePath = Forge.Text;
                        premisePath = PremiseMod.Text;
                        bodyPath = BodyMod.Text;
                        libPath = ForgeLib.Text;
                        sePath = Sound.Text;
                        skinPath = Skin.Text;
                    });

                    if (!File.Exists($"{mcPath}\\versions\\1.5.2\\1.5.2.jar"))
                    {
                        ShowErrorMessage("Minecraft 1.5.2 実行ファイルが見つかりません。Minecraft 1.5.2を1回も起動していない可能性があります。もう一度動画を見てやり直してみてください。");
                        CancelInstalling();
                        return;
                    }

                    // DQM用バージョンディレクトリ作成
                    Directory.CreateDirectory($"{mcPath}\\versions\\{displayVersion}");
                    var jarPath = $"{mcPath}\\versions\\{displayVersion}\\{displayVersion}.jar";
                    SendProgress("準備中");
                    if (File.Exists(jarPath))
                    {
                        if (MessageBox.Show("指定されたバージョンはすでに存在します。上書きしますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                        {
                            CancelInstalling();
                            return;
                        }
                    }
                    // JARファイルコピー
                    File.Copy($"{mcPath}\\versions\\1.5.2\\1.5.2.jar", jarPath, true);

                    SendProgress("DQM用バージョンを作成しています。");

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    // JSONの書き込み

                    var vJsonPath = Path.Combine(exePath, "assets", "dqm4.json");
                    if (!File.Exists(vJsonPath))
                    {
                        MessageBox.Show("dqm4.jsonが見つかりません。実行ファイルだけ別の場所に移動していませんか？", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        CancelInstalling();
                        return;
                    }
                    using (var vJsonReader = new StreamReader(vJsonPath))
                    {
                        var vJson = JObject.Parse(vJsonReader.ReadToEnd());
                        vJson["id"] = displayVersion;
                        // JSONの書き込み
                        File.WriteAllText($"{mcPath}\\versions\\{displayVersion}\\{displayVersion}.json", vJson.ToString());

                        UpdateProgressBar(1);
                        using (var libArchive = ZipFile.OpenRead(libPath))
                        {
                            SendProgress("libファイルの展開中です。");
                            libArchive.ExtractToDirectory(mcPath + "\\lib", true);
                        }

                        SendProgress("プロファイルの登録中です。");
                        var launcherProfilePath = mcPath + "\\launcher_profiles.json";
                        if (!File.Exists(launcherProfilePath))
                        {
                            ShowErrorMessage("プロファイル一覧ファイルが見つかりません。ランチャーを1回も起動していない可能性があります。もう一度動画を見てやり直してみてください。");
                            CancelInstalling();
                            return;
                        }
                        var reader = new StreamReader(launcherProfilePath, Encoding.GetEncoding("UTF-8"));
                        var json = reader.ReadToEnd();
                        reader.Close();

                        var jObject = JObject.Parse(json);
                        var profile = new JObject();
                        profile["created"] = DateTime.Now.ToString();
                        profile["lastVersionId"] = displayVersion;
                        profile["name"] = displayVersion;
                        profile["type"] = "custom";
                        jObject["profiles"][Membership.GeneratePassword(32, 0)] = profile;

                        File.WriteAllText(launcherProfilePath, jObject.ToString());

                        SendProgress("クリーンアップ中です。");
                        if (Directory.Exists(tempPath))
                        {
                            try
                            {
                                Directory.Delete(Path.Combine(tempPath, "forge"), true);
                                Directory.Delete(Path.Combine(tempPath, "premise"), true);
                                Directory.Delete(Path.Combine(tempPath, "skin"), true);
                            } catch(Exception e) { }
                        }

                        UpdateProgressBar(2);
                        procedure = "xf";
                        SendProgress("Forgeの展開中です。");
                        var szPath = Path.Combine(exePath, "bin/7za.exe");
                        ExtractToDirectoryWithSevenZip(forgePath, $"{tempPath}\\forge");
                        UpdateProgressBar(3);
                        procedure = "aj";
                        SendProgress("JARファイルの作成中です。");
                        AddEntryToZipFileWithSevenZip(jarPath, $"{tempPath}\\forge\\*");
                        DeleteEntryFromZipFile(jarPath, "META-INF");
                        UpdateProgressBar(4);

                        ExtractPreAndBody(premisePath, jarPath, bodyPath, false);

                        using (var seArchive = ZipFile.OpenRead(sePath))
                        {
                            SendProgress("DQM SE/BGMの展開中です。");
                            seArchive.ExtractToDirectory(mcPath, true);
                        }
                        UpdateProgressBar(8);

                        
                        if (!File.Exists(Path.Combine(tempPath, "resources.zip")))
                        {
                            SendProgress("バニラSEのダウンロード中です。");

                            var soundClient = new WebClient();
                            soundClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(MCSoundDownloadProgressChanged);
                            soundClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(OnMCSoundDownloadCompleted);

                            soundClient.DownloadFileAsync(new Uri("https://app.chikach.net/dist/resources.zip"), Path.Combine(tempPath, "resources.zip"));
                        }
                        else
                        {
                            OnMCSoundDownloadCompleted(null, null);
                        }
                    }
                }
                catch (Exception e)
                {
                    ShowErrorMessage(e.Message);
                    CancelInstalling();
                }
                
               
            });
        }

        private void ExtractPreAndBody(string premisePath, string jarPath, string bodyPath, bool isUpdate)
        {
            int baseNum;
            if (isUpdate) baseNum = 0; else baseNum = 4;
            procedure = "xp";
            SendProgress("前提MODの展開中です。");
            ExtractToDirectoryWithSevenZip(premisePath, $"{tempPath}\\premise");
            UpdateProgressBar(baseNum + 1);
            procedure = "aj";
            SendProgress("JARファイルの作成中です。");
            AddEntryToZipFileWithSevenZip(jarPath, $"{tempPath}\\premise\\*");
            UpdateProgressBar(baseNum + 2);

            SendProgress("本体MODのコピー中です。");
            Directory.CreateDirectory(Path.Combine(mcPath, "mods"));
            File.Copy(bodyPath, mcPath + $"\\mods\\{bodyPath.Split('\\').Last()}", true);
            UpdateProgressBar(baseNum + 3);
        }

        private void CancelInstalling()
        {
            Dispatcher.Invoke(() =>
            {
                ChangeEnabled(true);
            });
        }

        private void SendProgress(string text)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressLabel.Content = text;
            });
        }

        private void UpdateProgressBar(int percent)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = percent;
            });
        }

        private void OnDropped(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null) return;

            if (files.Count() != 1)
            {
                ShowErrorMessage("複数のファイルを指定することはできません。");
            }
            else if (Directory.Exists(files[0]))
            {
                ShowErrorMessage("フォルダーを指定することはできません。");
            }
            else
            {
                (sender as TextBox).Text = files[0];
            }
        }

        public void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// 7-Zipを用いてディレクトリにアーカイブを展開します。
        /// </summary>
        /// <param name="zipFilePath">展開するZipファイル</param>
        /// <param name="targetDirectory">展開先ディレクトリ</param>
        void ExtractToDirectoryWithSevenZip(string zipFilePath, string targetDirectory)
        {
            using (var p = new Process())
            {
                p.StartInfo.FileName = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "bin/7za.exe");
                p.StartInfo.Arguments = $"x -y -bsp1 -o\"{targetDirectory}\" \"{zipFilePath}\"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardInput = false;
                p.OutputDataReceived += SevenZipOutputDataReceived;

                p.Start();

                p.BeginOutputReadLine();

                p.WaitForExit();
            }
        }

        private void SevenZipOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            var result = e.Data.Trim();
            if (!result.Contains("%")) return;
            var percent = result.Substring(0, e.Data.Trim().IndexOf("%"));
            switch (procedure)
            {
                case "xf":
                    SendProgress($"Forgeの展開中です。({percent}%)");
                    break;
                case "aj":
                    SendProgress($"JARファイルの作成中です。({percent}%)");
                    break;
                case "xp":
                    SendProgress($"前提MODの展開中です。({percent}%)");
                    break;
            }
        }

        private void MCSoundDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var percent = Math.Round(((float)e.BytesReceived / (float)e.TotalBytesToReceive * 100), 1);
            SendProgress($"バニラSEをダウンロードしています。({percent}%)");
        }

        private async void OnMCSoundDownloadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            await Task.Run(() =>
            {
                if (e != null && e.Error != null)
                {
                    ShowErrorMessage($"Minecraft SEファイルのダウンロードに失敗しました。({e.Error.Message})");
                    return;

                }
                SendProgress("バニラSEの展開中です。");
                using (var f = ZipFile.OpenRead(Path.Combine(Environment.GetEnvironmentVariable("temp"), "extract-dqm\\resources.zip")))
                {
                    f.ExtractToDirectory(Environment.GetEnvironmentVariable("appdata") + "\\.minecraft", true);
                }

                SetSkin();
            });
        }

        private void SetSkin()
        {
            SendProgress("スキンの設定中です。");
            UpdateProgressBar(9);

            //Directory.Delete(Path.Combine(Environment.GetEnvironmentVariable("temp"), "extract-dqm"), true);

            var skinPath = "";
            var displayVersion = "";
            Dispatcher.Invoke(() =>
            {
                skinPath = Skin.Text;
                displayVersion = DisplayVersion.Text;
            });

            var launcherAccountsPath = Environment.GetEnvironmentVariable("appdata") + "\\.minecraft\\launcher_accounts.json";
            if (!File.Exists(launcherAccountsPath))
            {
                ShowErrorMessage("アカウントデータファイルが見つかりません。ランチャーを1回も起動していない可能性があります。もう一度動画を見てやり直してみてください。");
                CancelInstalling();
                return;
            }
            var reader = new StreamReader(launcherAccountsPath, Encoding.GetEncoding("UTF-8"));
            var json = reader.ReadToEnd();
            reader.Close();

            var jObject = JObject.Parse(json);

            var x = ((JObject)jObject["accounts"]);
            if (x == null)
            {
                MessageBox.Show($"Minecraftランチャーにログインされているアカウントが存在しません。そのため、スキン設定ができませんでした。", "スキン設定", MessageBoxButton.OK, MessageBoxImage.Error);
                CancelInstalling();
                return;
            }
            var properties = x.Descendants().OfType<JProperty>();
            var accountList = new List<string>();
            
            foreach (var prop in properties)
            {
                if (prop.Name == "minecraftProfile") accountList.Add((string)((JObject)prop.Value)["name"]);
            }
            if (accountList.Count() > 1)
            {
                var s = new StringBuilder();
                foreach (var item in accountList)
                {
                    s.AppendLine(item);
                }
                MessageBox.Show($"Minecraftランチャーにログインされているアカウントにプレイヤーが複数存在するため、以下のすべてのプレイヤーに対してスキンを設定します。\n\n{s.ToString()}", "スキン設定", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (accountList.Count() == 0)
            {
                MessageBox.Show($"Minecraftランチャーにログインされているアカウントが存在しません。そのため、スキン設定ができませんでした。", "スキン設定", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            var skinTempPath = Path.Combine(Environment.GetEnvironmentVariable("temp"), "extract-dqm", "skin");
            Directory.CreateDirectory(Path.Combine(skinTempPath, "mob"));
            foreach (var item in accountList)
            {
                File.Copy(skinPath, Path.Combine(skinTempPath, $"mob\\{item}.png"), true);
            }

            AddEntryToZipFileWithSevenZip(Path.Combine(Environment.GetEnvironmentVariable("appdata"), ".minecraft\\versions", displayVersion, displayVersion + ".jar"), Path.Combine(skinTempPath, "*"));

            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("インストールが完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                CancelInstalling();
            });
        }

        private async void OnlySetSkin()
        {
            await Task.Run(() =>
            {
                SetSkin();
            });
        }

        /// <summary>
        /// 既存のアーカイブにファイルを追加します。
        /// </summary>
        /// <param name="zipFilePath">追加先アーカイブ</param>
        /// <param name="toAddFile">追加するファイル</param>
        void AddEntryToZipFileWithSevenZip(string zipFilePath, string toAddFile)
        {
            using (var p = new Process())
            {
                p.StartInfo.FileName = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "bin/7za.exe");
                p.StartInfo.Arguments = $"a -y -bsp1 \"{zipFilePath}\" \"{toAddFile}\"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardInput = false;
                p.OutputDataReceived += SevenZipOutputDataReceived;

                p.Start();

                p.BeginOutputReadLine();

                p.WaitForExit();
            }
        }

        /// <summary>
        /// 既存のアーカイブ書庫からファイルを削除します。
        /// </summary>
        /// <param name="zipFilePath">削除先アーカイブ</param>
        /// <param name="toDeleteFile">削除するファイルの相対パス</param>
        void DeleteEntryFromZipFile(string zipFilePath, string toDeleteFile)
        {
            using (var p = new Process())
            {
                p.StartInfo.FileName = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "bin/7za.exe");
                p.StartInfo.Arguments = $"d -y -bsp1 \"{zipFilePath}\" \"{toDeleteFile}\"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardInput = false;
                p.OutputDataReceived += SevenZipOutputDataReceived;

                p.Start();

                p.BeginOutputReadLine();

                p.WaitForExit();
            }
        }

        private void ReferSkin_Click(object sender, RoutedEventArgs e)
        {
            var path = ShowFilePicker("スキンファイル|*.png", "スキンの選択");
            if (path != null)
            {
                Skin.Text = path;
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            ChangeEnabled(true);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChangeEnabled(true);
        }

        private void DLMod_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://dqm4mod.wixsite.com/home");
        }

        private void DLForge_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://adfoc.us/serve/?id=27122854926913");
        }

        private void DLForgeLib_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://files.minecraftforge.net/fmllibs/fml_libs15.zip");
        }
    }

    /// <summary>
    /// ZIP拡張クラス。
    /// </summary>
    public static class MyZipFileExtensions
    {
        /// <summary>
        /// エントリーがディレクトリかどうか取得する。
        /// </summary>
        /// <param name="entry">ZIPアーカイブエントリー</param>
        /// <returns></returns>
        public static bool IsDirectory(this ZipArchiveEntry entry)
        {
            return string.IsNullOrEmpty(entry.Name);
        }

        /// <summary>
        /// ZIPアーカイブ内のすべてのファイルを特定のフォルダに解凍する。
        /// </summary>
        /// <param name="source">ZIPアーカイブ</param>
        /// <param name="destinationDirectoryName">解凍先ディレクトリ。</param>
        /// <param name="overwrite">上書きフラグ。ファイルの上書きを行う場合はtrue。</param>
        public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName, bool overwrite)
        {
            foreach (var entry in source.Entries)
            {
                try
                {
                    var fullPath = System.IO.Path.Combine(destinationDirectoryName, entry.FullName);
                    if (entry.IsDirectory())
                    {
                        if (!Directory.Exists(fullPath))
                        {
                            Directory.CreateDirectory(fullPath);
                        }
                    }
                    else
                    {
                        if (overwrite)
                        {
                            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));
                            try
                            {
                                entry.ExtractToFile(fullPath, true);
                            }
                            catch (NotSupportedException)
                            {
                                entry.ExtractToFile("\\\\.\\" + fullPath, true);
                            }

                        }
                        else
                        {
                            if (!File.Exists(fullPath))
                            {
                                entry.ExtractToFile(fullPath, true);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
