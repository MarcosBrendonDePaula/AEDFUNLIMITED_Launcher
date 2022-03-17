using CmlLib.Core;
using CmlLib.Core.Auth;
using LauncherV1;
using LauncherV1.Properties;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ZipFile = System.IO.Compression.ZipFile;


namespace ML
{
    class Core_Event : EventArgs
    {
        public string message;
        public int status;
    }
    class MinecraftLauncher
    {
        private static MinecraftLauncher _obj = null;

        public static MinecraftLauncher Make()
        {
            if (MinecraftLauncher._obj == null)
            {
                MinecraftLauncher._obj = new MinecraftLauncher();
            }

            return MinecraftLauncher._obj;
        }

        //events
        public event EventHandler HprogressBar;
        public event EventHandler HCoreUpdate;
        public event EventHandler HConsoleAppend;
        private static readonly HttpClient client = new HttpClient();

        protected virtual void OnConsoleAppend(Core_Event e)
        {
            EventHandler handler = HConsoleAppend;
            handler?.Invoke(this, e);
        }

        private void CoreLogg(string message)
        {
            var log = new Core_Event();
            log.message = message;
            OnConsoleAppend(log);
        }
        protected virtual void OnCoreUpdate(Core_Event e)
        {
            EventHandler handler = HCoreUpdate;
            handler?.Invoke(this, e);
        }
        protected virtual void OnDownloadProgressChange(object sender, DownloadProgressChangedEventArgs e)
        {
            EventHandler handler = HprogressBar;
            handler?.Invoke(this, e);
        }

        private void ProgressUpdate(object sender, EventArgs e)
        {
            DownloadProgressChangedEventArgs args = (DownloadProgressChangedEventArgs)e;
            OnDownloadProgressChange(sender, args);
        }

        public static string BasePath = Directory.GetCurrentDirectory() + @"/AEDFCLIENT";
        public static string BaseIp = Resources.IPHOST;

        //public static string base_ip = "aedfunlimited.ddns.net";
        public static string BaseSite = String.Format("http://{0}:25569", BaseIp);
        public static string User = "";
        public static string Token = "";
        public static string Version = "1.2.1.2";
        public static bool X86 = false, X64 = false;

        public static bool crashed = false;
        public static string seslog = "";

        public static bool CoreReady = false;

        public Process MinecraftProcess;
        public static MinecraftPath Mpath;

        public static string Jpath = MinecraftLauncher.BasePath + "/runtime/bin/java.exe";
        private CMLauncher _launcher;

        private void CheckExistWebview()
        {
            string version = null;
            try
            {
                version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch (Exception e)
            {
                CoreLogg("Web view não localizado:");
            }

            try
            {

                var temp = (string)Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}").GetValue("versionInfo");
                if (version == null)
                    version = temp;
            }
            catch (Exception) { }

            try
            {
                var temp = (string)Registry.LocalMachine.OpenSubKey(
                    @"\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}").GetValue("versionInfo");
                if (version == null)
                    version = temp;
            }
            catch (Exception ) { }


            if (System.Environment.Is64BitOperatingSystem)
            {
                MinecraftLauncher.X64 = true;
            }
            else
            {
                MinecraftLauncher.X86 = true;
            }

            if (version == null)
            {
                try
                {
                    OnCoreUpdate(new Core_Event()
                    {
                        message = "Downloading WebView2",
                        status = -900
                    });
                    var webClient = new WebClient();
                    webClient.DownloadProgressChanged += ProgressUpdate;
                    webClient.DownloadFileTaskAsync(new Uri(String.Format("{0}/EDGRUNTIME.exe", MinecraftLauncher.BaseSite)), String.Format("{0}/EDGRUNTIME.exe", MinecraftLauncher.BasePath)).Wait();

                    var installer = new Process();
                    installer.StartInfo.FileName = BasePath + "/EDGRUNTIME.exe";
                    installer.StartInfo.Arguments = "/silent /install";
                    installer.StartInfo.UseShellExecute = false;
                    OnCoreUpdate(new Core_Event()
                    {
                        message = "Instalando o WebView2 1/2\n\tAGUARDE...",
                        status = -900
                    });

                    installer.Start();
                    installer.WaitForExit();
                    Process.Start(Environment.CurrentDirectory + "\\" + Process.GetCurrentProcess().ProcessName + ".exe");
                    Process.GetCurrentProcess().Kill();
                }
                catch (Exception e)
                {
                    MessageBox.Show("ERRO ao Intstalar o webview2\n tente novamente".ToUpper(), "INFO!", MessageBoxButton.OK, MessageBoxImage.Error);
                    CoreLogg("ERR:" + e.ToString());
                }
            }
            var log = new Core_Event();
            log.message = "WEBVIEW v." + version;
            OnConsoleAppend(log);

        }

        public void DownloadVenv(bool is32 = false)
        {
            try
            {


                int exist = 1;
                exist = exist * (Directory.Exists(MinecraftLauncher.BasePath + "/runtime") ? 1 : 0);
                if (exist == 1)
                {
                    Directory.Delete(MinecraftLauncher.BasePath + "/runtime", true);
                }
                Directory.CreateDirectory(MinecraftLauncher.BasePath + "/runtime");
                var config = LauncherConfig.Make();
                if (config.Jpath.Length != 0)
                {
                    return;
                }

                OnCoreUpdate(new Core_Event()
                {
                    message = "Obtendo o Java",
                    status = 0
                });

                WebClient webClient = new WebClient();
                webClient.DownloadProgressChanged += ProgressUpdate;

                if (is32)
                {

                    webClient.DownloadFileTaskAsync(new Uri(String.Format("{0}/JAVA32.zip", MinecraftLauncher.BaseSite)), String.Format("{0}/venv.zip", MinecraftLauncher.BasePath)).Wait();
                    //webClient.DownloadFileTaskAsync(new Uri("http://aedfunlimitedfiles.freetzi.com/32.zip"), String.Format("{0}/venv.zip", MinecraftLauncher.BasePath)).Wait();
                }
                else
                {

                    webClient.DownloadFileTaskAsync(new Uri(String.Format("{0}/JAVA64.zip", MinecraftLauncher.BaseSite)), String.Format("{0}/venv.zip", MinecraftLauncher.BasePath)).Wait();
                    //webClient.DownloadFileTaskAsync(new Uri("http://aedfunlimitedfiles.freetzi.com/64.zip"), String.Format("{0}/venv.zip", MinecraftLauncher.BasePath)).Wait();
                }

                OnCoreUpdate(new Core_Event()
                {
                    message = "Instalando venv",
                    status = 0
                });

                ZipFile.ExtractToDirectory(String.Format("{0}/venv.zip", MinecraftLauncher.BasePath), MinecraftLauncher.BasePath + "/runtime");
                File.Delete(String.Format("{0}/venv.zip", MinecraftLauncher.BasePath));

                config.Jpath = MinecraftLauncher.BasePath + "/runtime/bin/java.exe";
                config.Save();
            }
            catch (Exception e)
            {
                CoreLogg("Erro ao instalar o java:" + e.ToString());
            }

        }

        public void Basic_Check_Download()
        {
            try
            {
                CheckExistWebview();
                OnCoreUpdate(new Core_Event()
                {
                    message = "Checking Core",
                    status = -1
                });

                int exist = 1;
                exist = exist * (Directory.Exists(MinecraftLauncher.BasePath) ? 1 : 0);
                if (exist == 1)
                {
                    var dirs = Directory.GetDirectories(MinecraftLauncher.BasePath);
                    if (dirs.Length <= 5)
                    {
                        exist = 0;
                        Directory.Delete(MinecraftLauncher.BasePath, true);
                    }
                }

                if (exist == 0)
                {
                    OnCoreUpdate(new Core_Event()
                    {
                        message = "Downloading Core 2/2",
                        status = 0
                    });

                    Directory.CreateDirectory(MinecraftLauncher.BasePath);
                    WebClient webClient = new WebClient();

                    webClient.DownloadProgressChanged += ProgressUpdate;
                    webClient.DownloadFileTaskAsync(new Uri(String.Format("{0}/versoes/base.zip", MinecraftLauncher.BaseSite)), String.Format("{0}/base.zip", MinecraftLauncher.BasePath)).Wait();

                    OnCoreUpdate(new Core_Event()
                    {
                        message = "Instalando",
                        status = 0
                    });
                    ZipFile.ExtractToDirectory(String.Format("{0}/base.zip", MinecraftLauncher.BasePath), MinecraftLauncher.BasePath);
                    File.Delete(String.Format("{0}/base.zip", MinecraftLauncher.BasePath));

                    DownloadVenv(X86);

                    CoreReady = true;


                    OnCoreUpdate(new Core_Event()
                    {
                        message = "Core OK!",
                        status = 1
                    });

                    OnCoreUpdate(new Core_Event()
                    {
                        message = "JOGAR",
                        status = 1
                    });
                }
                else
                {
                    if (!Directory.Exists(MinecraftLauncher.BasePath + "/runtime/bin") || LauncherConfig.Make().Jpath == "")
                    {
                        DownloadVenv(X86);
                    }
                    OnCoreUpdate(new Core_Event()
                    {
                        message = "JOGAR",
                        status = 0
                    });
                }
            }
            catch (Exception e)
            {
                CoreLogg("ERR:" + e.ToString());
            }

        }

        public MinecraftLauncher()
        {
            MinecraftLauncher.Mpath = new MinecraftPath(MinecraftLauncher.BasePath, MinecraftLauncher.BasePath);
            Console.WriteLine(MinecraftLauncher.Mpath.Resource);
            _launcher = new CMLauncher(MinecraftLauncher.Mpath);
            _launcher.FileChanged += (e) =>
            {
                Console.WriteLine("[{0}] {1} - {2}/{3}", e.FileKind.ToString(), e.FileName, e.ProgressedFileCount, e.TotalFileCount);
            };
            _launcher.ProgressChanged += (s, e) =>
            {
                Console.WriteLine("{0}%", e.ProgressPercentage);
            };
            System.Net.ServicePointManager.DefaultConnectionLimit = 256;

        }

        public async void SyncFilesStart()
        {
            try
            {
                if (!File.Exists(MinecraftLauncher.BasePath + "/versions.json"))
                {
                    var versionA = File.Create(MinecraftLauncher.BasePath + "/versions.json");
                    string temp = "{\"mods\":\"0.0\",\"coremods\":\"0.0\",\"config\":\"0.0\"}";
                    var array = Encoding.UTF8.GetBytes(temp);
                    versionA.Write(array, 0, array.Length);
                    versionA.Close();
                    Directory.CreateDirectory(MinecraftLauncher.BasePath + "/mods");
                    Directory.CreateDirectory(MinecraftLauncher.BasePath + "/coremods");
                    Directory.CreateDirectory(MinecraftLauncher.BasePath + "/config");
                }

                JObject versaoAtual;
                using (StreamReader reader = File.OpenText(MinecraftLauncher.BasePath + "/versions.json"))
                {
                    versaoAtual = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                }

                HttpClient client = new HttpClient();
                var response = await client.GetAsync(MinecraftLauncher.BaseSite + "/mod");
                var responseString = await response.Content.ReadAsStringAsync();
                JObject nova = JObject.Parse(responseString);

                if ((string)nova["mods"] != (string)versaoAtual["mods"])
                {

                    OnCoreUpdate(new Core_Event()
                    {
                        message = "Downloading Mods",
                        status = 0
                    });

                    try
                    {
                        Directory.Delete(MinecraftLauncher.BasePath + "/mods", true);
                        WebClient webClient = new WebClient();
                        webClient.DownloadProgressChanged += ProgressUpdate;
                        webClient.DownloadFileTaskAsync(new Uri(String.Format("{0}/mods.zip", MinecraftLauncher.BaseSite)), String.Format("{0}/mods.zip", MinecraftLauncher.BasePath)).Wait();
                        ZipFile.ExtractToDirectory(String.Format("{0}/mods.zip", MinecraftLauncher.BasePath), MinecraftLauncher.BasePath);
                        versaoAtual["mods"] = (string)nova["mods"];
                        File.WriteAllText(MinecraftLauncher.BasePath + "/versions.json", versaoAtual.ToString());
                        File.Delete(String.Format("{0}/mods.zip", MinecraftLauncher.BasePath));
                    }
                    catch (Exception e)
                    {
                        CoreLogg("ERR:" + e.ToString());
                    }
                }

                if ((string)nova["coremods"] != (string)versaoAtual["coremods"])
                {
                    OnCoreUpdate(new Core_Event()
                    {
                        message = "Downloading CoreMods",
                        status = 0
                    });
                    try
                    {
                        Directory.Delete(MinecraftLauncher.BasePath + "/coremods", true);
                        WebClient webClient = new WebClient();
                        webClient.DownloadProgressChanged += ProgressUpdate;
                        webClient.DownloadFile(String.Format("{0}/coremods.zip", MinecraftLauncher.BaseSite), String.Format("{0}/coremods.zip", MinecraftLauncher.BasePath));
                        ZipFile.ExtractToDirectory(String.Format("{0}/coremods.zip", MinecraftLauncher.BasePath), MinecraftLauncher.BasePath);
                        versaoAtual["coremods"] = (string)nova["coremods"];
                        File.WriteAllText(MinecraftLauncher.BasePath + "/versions.json", versaoAtual.ToString());
                        File.Delete(String.Format("{0}/coremods.zip", MinecraftLauncher.BasePath));
                    }
                    catch (Exception e)
                    {
                        CoreLogg("ERR:" + e.ToString());
                    }

                }

                if ((string)nova["config"] != (string)versaoAtual["config"])
                {
                    CoreLogg("Download: Config");
                    try
                    {
                        Directory.Delete(MinecraftLauncher.BasePath + "/config", true);
                        var webClient = new WebClient();
                        webClient.DownloadProgressChanged += ProgressUpdate;
                        webClient.DownloadFile(String.Format("{0}/config.zip", MinecraftLauncher.BaseSite), String.Format("{0}/config.zip", MinecraftLauncher.BasePath));
                        ZipFile.ExtractToDirectory(String.Format("{0}/config.zip", MinecraftLauncher.BasePath), MinecraftLauncher.BasePath);
                        versaoAtual["config"] = (string)nova["config"];
                        File.WriteAllText(MinecraftLauncher.BasePath + "/versions.json", versaoAtual.ToString());
                        File.Delete(String.Format("{0}/config.zip", MinecraftLauncher.BasePath));
                    }
                    catch (Exception e)
                    {
                        CoreLogg("ERR:" + e.ToString());
                    }

                }

                OnCoreUpdate(new Core_Event()
                {
                    message = "INICIANDO...",
                    status = 0
                });

                await Start();
                MinecraftProcess.Exited += (sender, args) =>
                {
                    if (MinecraftLauncher.crashed)
                    {
                        sendCrashReport();
                    }

                    OnCoreUpdate(new Core_Event()
                    {
                        message = "JOGAR",
                        status = 0
                    });
                };

                OnCoreUpdate(new Core_Event()
                {
                    message = "ABERTO",
                    status = 0
                });

            }
            catch (Exception e)
            {
                CoreLogg("ERR:" + e.ToString());
            }

        }

        public async void sendCrashReport()
        {
            try
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"crash", MinecraftLauncher.seslog},
                });
                var response = await client.PostAsync(MinecraftLauncher.BaseSite + "/crashreport", content);
            }
            catch (Exception e)
            {
                CoreLogg("Erro : " + e.ToString());
                throw;
            }

        }

        private async Task<bool> Start()
        {
            try
            {
                MinecraftLauncher.crashed = false;
                MinecraftLauncher.seslog = "";

                var config = LauncherConfig.Make();
                var pathJava = config.Jpath;
                var bpath = BasePath;

                if (Jpath.Contains(" "))
                {
                    pathJava = "\"" + pathJava + "\"";
                    bpath = "\"" + bpath + "\"";
                }

                CoreLogg("B_PATH:" + bpath);
                CoreLogg("J_PATH:" + pathJava);


                var args = new string[4];
                args[0] = "-DmyArgument=" + MinecraftLauncher.Token + "::" + MinecraftLauncher.BaseIp + ":25569" + "::" + MinecraftLauncher.User;
                args[1] = "-Dminecraft.applet.TargetDirectory=" + bpath;
                //Ativando fps++
                args[2] = "-Dfml.ignoreInvalidMinecraftCertificates=true -Dfml.ignorePatchDiscrepancies=true";
                args[3] = config.Args;


                System.Net.ServicePointManager.DefaultConnectionLimit = 256;


                var session = MSession.GetOfflineSession(MinecraftLauncher.User);
                var launchOption = new MLaunchOption
                {
                    MaximumRamMb = config.MAX_RAM,
                    Session = session,
                    JVMArguments = args,
                    JavaPath = pathJava,
                    ServerIp = Resources.ServerIp,
                    ServerPort = 25565
                };

                CoreLogg("User: " + MinecraftLauncher.User);

                this.MinecraftProcess = await _launcher.CreateProcessAsync("1.5.2-Forge7.8.1.738", launchOption);

                var processUtil = new CmlLib.Utils.ProcessUtil(this.MinecraftProcess);
                processUtil.OutputReceived += (s, e) =>
                {
                    if (e.Contains("Minecraft has crashed!"))
                    {
                        MinecraftLauncher.crashed = true;
                    }

                    if (MinecraftLauncher.crashed)
                    {
                        MinecraftLauncher.seslog += e;
                    }

                    CoreLogg(e);
                };
                processUtil.StartWithEvents();

                return true;
            }
            catch (Exception e)
            {
                CoreLogg("Erro : " + e.ToString());
                return false;
            }
        }
    }
}
