using System;
using ML;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using CmlLib.Core.Files;
using CmlLib.Core.Installer;
using Org.BouncyCastle.Utilities.Encoders;

namespace LauncherV1
{
    class LauncherConfig
    {
        public static MJava jvcheck = new MJava();
        public static LauncherConfig obj = null;

        public static LauncherConfig Make()
        {
            if (LauncherConfig.obj == null)
            {
                LauncherConfig.obj = new LauncherConfig();
            }

            return LauncherConfig.obj;
        }

        public string path = Directory.GetCurrentDirectory();
        public string Email;
        public string Senha;
        public string Jpath;
        public int MAX_RAM;
        public string Args;
        public int MaxLogs;
        private JObject config;


        public void Save()
        {
            this.config["Email"] = Email;
            this.config["Senha"] = Encoding.UTF8.GetString(Base64.Encode(Encoding.UTF8.GetBytes(Senha)));
            this.config["Jpath"] = Jpath;
            this.config["MAX_RAM"] = MAX_RAM;
            this.config["Args"] = Args;
            this.config["Maxlogs"] = MaxLogs;
            File.WriteAllText(path + "/Launcher_Config.json", config.ToString());
        }

        public void Load()
        {

            using (StreamReader reader = File.OpenText(path + "/Launcher_Config.json"))
            {
                config = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
            }


            Email = (string)config["Email"];
            Senha = Encoding.UTF8.GetString(Base64.Decode(Encoding.UTF8.GetBytes((string)config["Senha"])));
            Jpath = (string)config["Jpath"];

            if (!File.Exists((string)config["Jpath"]))
            {
                Jpath = "";
            }

            MAX_RAM = (int)config["MAX_RAM"];
            Args = (string)config["Args"];
            MaxLogs = (int)config["Maxlogs"];

        }

        LauncherConfig()
        {
            this.config = new JObject();
            this.config["Email"] = this.Email = "";
            this.config["Senha"] = this.Senha = "";
            this.config["Jpath"] = this.Jpath = "";
            this.config["MAX_RAM"] = this.MAX_RAM = 1024;
            this.config["Args"] = this.Args = "";
            this.config["Maxlogs"] = this.MaxLogs = 2000;

            if (!File.Exists(path + "/Launcher_Config.json"))
            {
                this.Save();
            }

            this.Load();
        }

    }
}