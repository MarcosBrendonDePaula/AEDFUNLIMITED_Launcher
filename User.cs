using ML;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace LauncherV1
{
    class User
    {
        private Dictionary<string, string> fields;
        public JObject response;
        private static readonly HttpClient client = new HttpClient();

        public User(string email = "", string passw = "")
        {
            var macAddr =
            (
                from nic in NetworkInterface.GetAllNetworkInterfaces()
                where nic.OperationalStatus == OperationalStatus.Up
                select nic.GetPhysicalAddress().ToString()
            ).FirstOrDefault();

            fields = new Dictionary<string, string>
            {
                { "email", email},
                { "passw", passw },
                {"id",macAddr}
            };
        }

        public void setValues(string email = "", string passw = "")
        {
            fields = new Dictionary<string, string>
            {
                { "email", email},
                { "passw", passw }
            };
        }

        public async Task<JObject> Login()
        {

            var content = new FormUrlEncodedContent(this.fields);

            var response = await client.PostAsync(MinecraftLauncher.base_site + "/login", content);

            var responseString = await response.Content.ReadAsStringAsync();
            this.response = JObject.Parse(responseString);
            return this.response;
        }
    }
}
