using ML;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Navigation;
using Microsoft.VisualBasic.Devices;
using MessageBox = System.Windows.MessageBox;

namespace LauncherV1
{
    /// <summary>
    /// 
    /// </summary>
    public partial class MainWindow : Window
    {
        static LauncherConfig config = LauncherConfig.Make();
        private static ComputerInfo Comp_info = new ComputerInfo();

        public MainWindow()
        {
            InitializeComponent();

            if (config.Email != "")
            {
                CGuardar.IsChecked = true;
                FEmail.Text = config.Email;
                FSenha.Password = config.Senha;
            }

            Slider_Ram.Maximum = Comp_info.TotalPhysicalMemory / (1024 * 1024);
            Ram_Value.Content = config.MAX_RAM.ToString() + " MB";

            var mine = MinecraftLauncher.Make();
            mine.HCoreUpdate += UpdateCore;
            mine.HprogressBar += ProgressUpdate;
            mine.HConsoleAppend += ConsoleAppend;

            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadData(MinecraftLauncher.base_site);
                OFFLINE_MESSAGE.Visibility = Visibility.Hidden;
                Painel_Direito.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {
                Painel_Direito.Visibility = Visibility.Hidden;
                OFFLINE_MESSAGE.Visibility = Visibility.Visible;
            }
            

            new Thread(mine.Basic_Check_Download).Start();
        }

        private void ConsoleAppend(object sender, EventArgs e)
        {
            Core_Event ev = (Core_Event)e;
            Console.WriteLine(ev.message);
            Action atualiza = () =>
            {
                if (ConsoleText.LineCount == config.MaxLogs)
                    ConsoleText.Clear();
                ConsoleText.AppendText(ev.message + "\n");
                ConsoleText.ScrollToEnd();
            };
            Dispatcher.Invoke(atualiza);
        }

        private void ProgressUpdate(object sender, EventArgs e)
        {
            DownloadProgressChangedEventArgs args = (DownloadProgressChangedEventArgs)e;
            Action atualiza = () =>
            {
                if (ConsoleText.LineCount == config.MaxLogs)
                    ConsoleText.Clear();
                BProgress.Value = args.ProgressPercentage;
                Pencent_Label.Content = args.ProgressPercentage.ToString() + "%";
            };
            Dispatcher.Invoke(atualiza);
        }

        private void UpdateCore(object sender, EventArgs e)
        {
            Core_Event ev = (Core_Event)e;
            Action atualiza = () =>
            {
                if (ConsoleText.LineCount == config.MaxLogs)
                    ConsoleText.Clear();
                if (ev.status == -900)
                {
                    IWEBVIEW_LABEL.Content = ev.message;
                    IWEBVIEW_LABEL.Visibility = Visibility.Visible;
                    WebBrowserMain.Visibility = Visibility.Hidden;
                }
                Bplay.Content = ev.message;
                ConsoleText.AppendText(ev.message + "\n");
                ConsoleText.ScrollToEnd();
            };
            Dispatcher.Invoke(atualiza);
        }
        
        //play button function
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if ((string)Bplay.Content != "Play")
            {
                MessageBox.Show("Aguarde A instalação terminar", "INFO");
                return;
            }

            ConsoleText.Clear();
            Tcontrol.SelectedIndex = 1;
            var launch = MinecraftLauncher.Make();
            Bplay.Content = "Sincronizando Mods";

            new Thread(launch.SyncFilesStart).Start();
            Bplay.Content = "Iniciando";
        }

        //login button function
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            User temp = new User(FEmail.Text, FSenha.Password);
            try
            {
                var response = await temp.Login();
                if ((int)response["status"] == -100)
                {
                    MessageBox.Show("Seu Computador foi Banido!", "Mano o que você fez?", MessageBoxButton.OK, MessageBoxImage.Stop);
                    Bplay.Content = "PC BANIDO!";
                    BLogar.Content = "PC BANIDO!";
                    BLogar.Visibility = Visibility.Hidden;
                }
                else
                if ((int)response["status"] == 1)
                {

                    MinecraftLauncher.User = (string)response["username"];
                    MinecraftLauncher.Token = (string)response["token"];
                    WebBrowserMain.Source = new Uri(String.Format("{0}/launcher/auth/{1}/{2}", MinecraftLauncher.base_site,
                        MinecraftLauncher.Token, MinecraftLauncher.User));
                    BLogar.Content = "LOGADO!";
                    UserBtn.Visibility = Visibility.Visible;
                    UserBtn.Content = MinecraftLauncher.User;
                    Bplay.IsEnabled = true;

                    LOGINPAINEL.Visibility = Visibility.Hidden;
                    LOGGEDPAINEL.Visibility = Visibility.Visible;

                    if ((bool)CGuardar.IsChecked)
                    {

                        config.Email = FEmail.Text;
                        config.Senha = FSenha.Password;
                        config.Save();
                    }
                }
                else
                {
                    MessageBox.Show(((string)response["msg"]).ToUpper(), "INFO!", MessageBoxButton.OK, MessageBoxImage.Information);
                    Bplay.IsEnabled = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show(@"Ocorreu um erro ao tentar conectar tente novamente.".ToUpper(), "OPS!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BProcurarJava_Click(object sender, RoutedEventArgs e)
        {
            var fdialog = new OpenFileDialog();
            fdialog.InitialDirectory = "c:\\";
            fdialog.Filter = "java|java.exe";
            fdialog.FilterIndex = 1;
            fdialog.RestoreDirectory = true;

            fdialog.ShowDialog();
            if (fdialog.FileName != "")
            {
                CTJAVA.Text = fdialog.FileName.Replace('\\', '/');
                config.Jpath = CTJAVA.Text;
                config.Save();
            }
        }

        private void Tcontrol_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TabConfig.IsSelected)
            {
                config.Load();
                CTJAVA.Text = config.Jpath.Replace('\\', '/');
                CTARGS.Text = config.Args;
                Slider_Ram.Value = config.MAX_RAM;
                //CTRAM.Text = config.MAX_RAM.ToString();
            }

        }

        private void CTJAVA_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (CTJAVA.Text != "")
            {
                config.Jpath = CTJAVA.Text.Replace('\\', '/');
                config.Save();
            }
        }

        private void CTARGS_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            config.Args = CTARGS.Text;
            config.Save();
        }

        private void LOGOFFBTN_Click(object sender, RoutedEventArgs e)
        {
            LOGINPAINEL.Visibility = Visibility.Visible;
            LOGGEDPAINEL.Visibility = Visibility.Hidden;
            BLogar.Content = "Logar";
            Bplay.IsEnabled = false;
        }

        private void Slider_Ram_Change(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CTJAVA.Text != "")
            {
                try
                {
                    int ram = Convert.ToInt32(Slider_Ram.Value);
                    config.MAX_RAM = ram;
                    config.Save();
                    Ram_Value.Content = config.MAX_RAM.ToString() + " MB";
                }
                catch (Exception)
                {
                    config.Load();
                    Slider_Ram.Value = config.MAX_RAM;
                }
            }
        }

        //addons functions
        private void BTexturePackDownload_Click(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {

                if (!Directory.Exists(MinecraftLauncher.base_Path + "/texturepacks"))
                {
                    Directory.CreateDirectory(MinecraftLauncher.base_Path + "/texturepacks");
                }

                WebClient webClient = new WebClient();
                webClient.DownloadProgressChanged += (object send, DownloadProgressChangedEventArgs ee) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        PAddonDownloader.Visibility = Visibility.Visible;
                        LAddonStatus.Visibility = Visibility.Visible;
                        PAddonDownloader.Value = ee.ProgressPercentage;
                        LAddonStatus.Content = ee.BytesReceived + " - " + ee.TotalBytesToReceive;
                    });
                };

                webClient.DownloadFileCompleted += (object snd, AsyncCompletedEventArgs ee) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        PAddonDownloader.Visibility = Visibility.Hidden;
                        LAddonStatus.Visibility = Visibility.Hidden;
                    });
                };

                webClient.DownloadFileTaskAsync(MinecraftLauncher.base_site + "/textures/Faithful.zip", MinecraftLauncher.base_Path + "/texturepacks/Faithful.zip").Wait();

                Dispatcher.Invoke(() =>
                {
                    BTexturePackDownload.Content += " OK";
                });

            }).Start();
        }

        private void BShadersInstall_Click(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                string b_path = MinecraftLauncher.base_Path + "/libraries/net/minecraftforge/minecraftforge/7.8.1.738";
                if (!Directory.Exists(b_path))
                {
                    Directory.CreateDirectory(b_path);
                }


                WebClient webClient = new WebClient();
                webClient.DownloadProgressChanged += (object send, DownloadProgressChangedEventArgs ee) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        PAddonDownloader.Visibility = Visibility.Visible;
                        LAddonStatus.Visibility = Visibility.Visible;
                        PAddonDownloader.Value = ee.ProgressPercentage;
                        LAddonStatus.Content = ee.BytesReceived + " - " + ee.TotalBytesToReceive;
                    });
                };

                webClient.DownloadFileCompleted += (object snd, AsyncCompletedEventArgs ee) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        PAddonDownloader.Visibility = Visibility.Hidden;
                        LAddonStatus.Visibility = Visibility.Hidden;
                    });
                };

                webClient.DownloadFileTaskAsync(MinecraftLauncher.base_site + "/shaders.jar", b_path + "/minecraftforge-7.8.1.738.jar").Wait();

                Dispatcher.Invoke(() =>
                {

                });

            }).Start();
        }


        private void Navigate_discord_link(object sender, RequestNavigateEventArgs e)
        {

            Grid_Principal.ColumnDefinitions[0].Width = new GridLength(0, GridUnitType.Star); ;
            Grid_Principal.ColumnDefinitions[1].Width = new GridLength(100, GridUnitType.Star);
            Painel_Direito.Visibility = Visibility.Visible;
            WebBrowserMain.Source = new Uri("https://discord.gg/WsWb56jfBw");
        }
    }
}