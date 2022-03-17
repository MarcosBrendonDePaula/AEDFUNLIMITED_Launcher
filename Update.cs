using ML;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;

namespace LauncherV1
{
    class Update
    {
        public static Update obj = null;

        private string actual_version = null;

        public static Update Make(string version = null)
        {
            if (Update.obj == null)
            {
                if (version != null)
                {
                    Update.obj = new Update(version);
                }
                else
                {
                    Update.obj = new Update(MinecraftLauncher.Version);
                }
            }
            return Update.obj;
        }

        public Update(string version)
        {
            this.actual_version = version;
        }

        public async void checkUpdate()
        {
            var BasePath = Directory.GetCurrentDirectory();
            string[] arguments = Environment.GetCommandLineArgs();
            try
            {

                if (arguments.Length > 2)
                {
                    if (arguments[1] == "update")
                    {
                        var webclient = new WebClient();
                        webclient.DownloadFile(MinecraftLauncher.BaseSite + "/Launcher.zip", arguments[2] + "\\temp\\temp.zip");

                        var files = Directory.GetFiles(arguments[2], "*");
                        foreach (var file in files)
                        {
                            var file_inf = file.Split('\\');
                            File.Delete(arguments[2] + "\\" + file_inf[file_inf.Length - 1]);
                        }

                        if (Directory.Exists(arguments[2] + "\\runtimes"))
                            Directory.Delete(arguments[2] + "\\runtimes", true);

                        ZipFile.ExtractToDirectory(arguments[2] + "\\temp\\temp.zip", arguments[2]);

                        Process process = new Process();
                        process.StartInfo.FileName = (arguments[2] + "\\" + Process.GetCurrentProcess().ProcessName + ".exe");
                        process.StartInfo.UseShellExecute = false;
                        process.Start();

                        Process.GetCurrentProcess().Kill();
                    }
                }
                else
                {
                    if (Directory.Exists(BasePath + "\\temp"))
                    {
                        Directory.Delete(BasePath + "\\temp", true);
                    }

                    try
                    {

                        HttpClient client = new HttpClient();
                        var response = await client.GetAsync(MinecraftLauncher.BaseSite + "/Launcher");
                        var responseString = await response.Content.ReadAsStringAsync();
                        JObject vers = JObject.Parse(responseString);

                        if ((string)vers["version"] != this.actual_version)
                        {
                            if (Directory.Exists(BasePath + "\\temp"))
                            {
                                Directory.Delete(BasePath + "\\temp", true);
                            }
                            Directory.CreateDirectory(BasePath + "\\temp");

                            var files = Directory.GetFiles(BasePath, "*");
                            foreach (var file in files)
                            {
                                var file_inf = file.Split('\\');
                                File.Copy(file, BasePath + "\\temp\\" + file_inf[file_inf.Length - 1]);
                            }

                            Process process = new Process();
                            process.StartInfo.FileName = BasePath + "\\temp\\" + Process.GetCurrentProcess().ProcessName + ".exe";
                            process.StartInfo.Arguments = "update " + BasePath;
                            process.StartInfo.UseShellExecute = false;

                            process.Start();

                            Process.GetCurrentProcess().Kill();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }


        }
    }
}
