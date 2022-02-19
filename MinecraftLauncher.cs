using CmlLib.Core;
using CmlLib.Core.Auth;
using LauncherV1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LauncherV1.Properties;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;


namespace ML
{
    class Core_Event : EventArgs
    {
        public string message;
        public int status;
    }
    class MinecraftLauncher
    {
        private static MinecraftLauncher obj = null;

        public static MinecraftLauncher Make()
        {
            if (MinecraftLauncher.obj == null)
            {
                MinecraftLauncher.obj = new MinecraftLauncher();
            }

            return MinecraftLauncher.obj;
        }

        //events
        public event EventHandler HprogressBar;
        public event EventHandler HCoreUpdate;
        public event EventHandler HConsoleAppend;

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

        public static string base_Path = Directory.GetCurrentDirectory() + @"/AEDFCLIENT";
        public static string base_ip   = Resources.IPHOST;
        
        
        //public static string base_ip = "aedfunlimited.ddns.net";
        public static string base_site = String.Format("http://{0}:25569", base_ip);
        public static string User = "";
        public static string Token = "";
        public static bool x86 = false, x64 = false;


        public static bool CoreReady = false;

        public Process minecraft_process;
        public static MinecraftPath mpath;
        
        public static string jpath = MinecraftLauncher.base_Path + "/runtime/bin/java.exe";
        private CMLauncher launcher;

        private void CheckExistWebview()
        {
            string version = null;
            try
            {
                version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch (Exception e)
            {
                CoreLogg("ERR:" + e.Message);
            }

            //detect if system is 64bits
            try
            { 
                
                var temp = (string) Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}").GetValue("versionInfo");
                MinecraftLauncher.x64 = true;
                if(version == null)
                    version = temp;
            }
            catch (Exception e)
            {
                CoreLogg("ERR:" + e.Message);
            }
            
            //detect if system is 32bits
            try
            {
                var temp = (string) Registry.LocalMachine.OpenSubKey(
                    @"\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}").GetValue("versionInfo");
                MinecraftLauncher.x86 = true;
                if (version == null)
                    version = temp;
            }
            catch (Exception e)
            {
                CoreLogg("ERR:" + e.Message);
            }

            if (version == null && !MinecraftLauncher.x86 && !MinecraftLauncher.x64)
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
                    webClient.DownloadFileTaskAsync(new Uri(String.Format("{0}/EDGRUNTIME.exe", MinecraftLauncher.base_site)), String.Format("{0}/EDGRUNTIME.exe", MinecraftLauncher.base_Path)).Wait();
                    
                    var installer = new Process();
                    installer.StartInfo = new ProcessStartInfo(base_Path + "/EDGRUNTIME.exe", "/silent /install");
                    OnCoreUpdate(new Core_Event()
                    {
                        message = "Instalando o WebView2 1/2\n\tAGUARDE...",
                        status = -900
                    });

                    installer.Start();
                    installer.WaitForExit();
                    Process.Start(Environment.CurrentDirectory + "\\"+ Process.GetCurrentProcess().ProcessName+".exe");
                    Process.GetCurrentProcess().Kill();
                }
                catch (Exception e)
                {
                    MessageBox.Show("ERRO ao Intstalar o webview2\n tente novamente".ToUpper(), "INFO!", MessageBoxButton.OK, MessageBoxImage.Error);
                    Console.WriteLine("Erro ao instalar browser");
                    CoreLogg("ERR:"+e.Message);
                }
            }
            var log = new Core_Event();
            log.message = "WEBVIEW v." + version;
            OnConsoleAppend(log);

        }

        public void DownloadVenv(bool is_32 = false)
        {
            try
            {
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


                if (is_32)
                {
                    webClient.DownloadFileTaskAsync(new Uri(String.Format("{0}/_addons/venv/32.zip", MinecraftLauncher.base_site)), String.Format("{0}/venv.zip", MinecraftLauncher.base_Path)).Wait();
                }
                else
                {
                    webClient.DownloadFileTaskAsync(new Uri(String.Format("{0}/_addons/venv/64.zip", MinecraftLauncher.base_site)), String.Format("{0}/venv.zip", MinecraftLauncher.base_Path)).Wait();
                }
                
                OnCoreUpdate(new Core_Event()
                {
                    message = "Instalando venv",
                    status = 0
                });

                ZipFile.ExtractToDirectory(String.Format("{0}/venv.zip", MinecraftLauncher.base_Path), MinecraftLauncher.base_Path + "/runtime");
                File.Delete(String.Format("{0}/venv.zip", MinecraftLauncher.base_Path));
                
                config.Jpath = MinecraftLauncher.base_Path + "/runtime/bin/java.exe";
                config.Save();
            }
            catch (Exception e)
            {
                CoreLogg("Erro ao instalar o java:" + e.Message);
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
                exist = exist * (Directory.Exists(MinecraftLauncher.base_Path) ? 1 : 0);
                if (exist == 1)
                {
                    var dirs = Directory.GetDirectories(MinecraftLauncher.base_Path);
                    if (dirs.Length <= 5)
                    {
                        exist = 0;
                        Directory.Delete(MinecraftLauncher.base_Path, true);
                    }
                }

                if (exist == 0 )
                {
                    OnCoreUpdate(new Core_Event()
                    {
                        message = "Downloading Core 2/2",
                        status = 0
                    });

                    Directory.CreateDirectory(MinecraftLauncher.base_Path);
                    WebClient webClient = new WebClient();

                    webClient.DownloadProgressChanged += ProgressUpdate;
                    webClient.DownloadFileTaskAsync(new Uri(String.Format("{0}/versoes/base.zip", MinecraftLauncher.base_site)), String.Format("{0}/base.zip", MinecraftLauncher.base_Path)).Wait();

                    OnCoreUpdate(new Core_Event()
                    {
                        message = "Instalando",
                        status = 0
                    });
                    ZipFile.ExtractToDirectory(String.Format("{0}/base.zip", MinecraftLauncher.base_Path), MinecraftLauncher.base_Path);
                    File.Delete(String.Format("{0}/base.zip", MinecraftLauncher.base_Path));

                    DownloadVenv(x86);

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
                    if (!Directory.Exists(MinecraftLauncher.base_Path + "/runtime/bin"))
                    {
                        DownloadVenv(x86);
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
                CoreLogg("ERR:" + e.Message);
            }
            
        }

        public MinecraftLauncher()
        {
            MinecraftLauncher.mpath = new MinecraftPath(MinecraftLauncher.base_Path, MinecraftLauncher.base_Path);
            Console.WriteLine(MinecraftLauncher.mpath.Resource);
            launcher = new CMLauncher(MinecraftLauncher.mpath);
            launcher.FileChanged += (e) =>
            {
                Console.WriteLine("[{0}] {1} - {2}/{3}", e.FileKind.ToString(), e.FileName, e.ProgressedFileCount, e.TotalFileCount);
            };
            launcher.ProgressChanged += (s, e) =>
            {
                Console.WriteLine("{0}%", e.ProgressPercentage);
            };
            System.Net.ServicePointManager.DefaultConnectionLimit = 256;
        }

        public async void SyncFilesStart()
        {
            try
            {
                if (!File.Exists(MinecraftLauncher.base_Path + "/versions.json"))
                {
                    var versionA = File.Create(MinecraftLauncher.base_Path + "/versions.json");
                    string temp = "{\"mods\":\"0.0\",\"coremods\":\"0.0\",\"config\":\"0.0\"}";
                    var array = Encoding.UTF8.GetBytes(temp);
                    versionA.Write(array, 0, array.Length);
                    versionA.Close();
                    Directory.CreateDirectory(MinecraftLauncher.base_Path + "/mods");
                    Directory.CreateDirectory(MinecraftLauncher.base_Path + "/coremods");
                    Directory.CreateDirectory(MinecraftLauncher.base_Path + "/config");
                }

                JObject versaoAtual;
                using (StreamReader reader = File.OpenText(MinecraftLauncher.base_Path + "/versions.json"))
                {
                    versaoAtual = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                }

                HttpClient client = new HttpClient();
                var response = await client.GetAsync(MinecraftLauncher.base_site + "/mod");
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
                        Directory.Delete(MinecraftLauncher.base_Path + "/mods", true);
                        WebClient webClient = new WebClient();
                        webClient.DownloadProgressChanged += ProgressUpdate;
                        webClient.DownloadFileTaskAsync(new Uri(String.Format("{0}/mods.zip", MinecraftLauncher.base_site)), String.Format("{0}/mods.zip", MinecraftLauncher.base_Path)).Wait();
                        ZipFile.ExtractToDirectory(String.Format("{0}/mods.zip", MinecraftLauncher.base_Path), MinecraftLauncher.base_Path);
                        versaoAtual["mods"] = (string)nova["mods"];
                        File.WriteAllText(MinecraftLauncher.base_Path + "/versions.json", versaoAtual.ToString());
                        File.Delete(String.Format("{0}/mods.zip", MinecraftLauncher.base_Path));
                    }
                    catch (Exception e)
                    {
                        CoreLogg("ERR:" + e.Message);
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
                        Directory.Delete(MinecraftLauncher.base_Path + "/coremods", true);
                        WebClient webClient = new WebClient();
                        webClient.DownloadProgressChanged += ProgressUpdate;
                        webClient.DownloadFile(String.Format("{0}/coremods.zip", MinecraftLauncher.base_site), String.Format("{0}/coremods.zip", MinecraftLauncher.base_Path));
                        ZipFile.ExtractToDirectory(String.Format("{0}/coremods.zip", MinecraftLauncher.base_Path), MinecraftLauncher.base_Path);
                        versaoAtual["coremods"] = (string)nova["coremods"];
                        File.WriteAllText(MinecraftLauncher.base_Path + "/versions.json", versaoAtual.ToString());
                        File.Delete(String.Format("{0}/coremods.zip", MinecraftLauncher.base_Path));
                    }
                    catch (Exception e)
                    {
                        CoreLogg("ERR:" + e.Message);
                    }

                }

                if ((string)nova["config"] != (string)versaoAtual["config"])
                {
                    CoreLogg("Download: Config");
                    try
                    {
                        Directory.Delete(MinecraftLauncher.base_Path + "/config", true);
                        var webClient = new WebClient();
                        webClient.DownloadProgressChanged += ProgressUpdate;
                        webClient.DownloadFile(String.Format("{0}/config.zip", MinecraftLauncher.base_site), String.Format("{0}/config.zip", MinecraftLauncher.base_Path));
                        ZipFile.ExtractToDirectory(String.Format("{0}/config.zip", MinecraftLauncher.base_Path), MinecraftLauncher.base_Path);
                        versaoAtual["config"] = (string)nova["config"];
                        File.WriteAllText(MinecraftLauncher.base_Path + "/versions.json", versaoAtual.ToString());
                        File.Delete(String.Format("{0}/config.zip", MinecraftLauncher.base_Path));
                    }
                    catch (Exception e)
                    {
                        CoreLogg("ERR:" + e.Message);
                    }
                    
                }

                OnCoreUpdate(new Core_Event()
                {
                    message = "Iniciando..",
                    status = 0
                });

                await start();
                minecraft_process.WaitForExit();

                OnCoreUpdate(new Core_Event()
                {
                    message = "Play",
                    status = 0
                });
            }
            catch (Exception e)
            {
                CoreLogg("ERR:"+e.Message);
            }
            
        }

        private async Task<bool> start()
        {
            try
            {
                var config = LauncherConfig.Make();
                var pathJava = config.Jpath;
                var Bpath = base_Path;

                if (jpath.Contains(" "))
                {
                    pathJava = "\"" + pathJava + "\"";
                    Bpath = "\"" + Bpath + "\"";
                }
                
                CoreLogg("B_PATH:" + Bpath);
                CoreLogg("J_PATH:"+pathJava);
                

                var args = new string[4];
                args[0] = "-DmyArgument=" + MinecraftLauncher.Token + "::" + MinecraftLauncher.base_ip + ":25569" + "::" + MinecraftLauncher.User;
                args[1] = "-Dminecraft.applet.TargetDirectory=" + Bpath;
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

                this.minecraft_process = await launcher.CreateProcessAsync("1.5.2-Forge7.8.1.738", launchOption);

                var processUtil = new CmlLib.Utils.ProcessUtil(this.minecraft_process);
                processUtil.OutputReceived += (s, e) =>
                {
                    CoreLogg(e);
                };
                processUtil.StartWithEvents();

                return true;
            }
            catch (Exception e)
            {
                CoreLogg("Erro : " + e.Message);
                return false;
            }
        }
    }
}
