using System;
using System.Collections.Generic;
using System.IO;

using Shadowsocks.Controller;
using Newtonsoft.Json;

namespace Shadowsocks.Model
{
    public struct IPRange {
        public string beginIP;
        public string endIP;
    };

    [Serializable]
    public class Configuration
    {
        public List<Server> configs;
        // when strategy is set, index is ignored
        public string strategy;
        public int index;
        public bool global;
        public bool enabled;
        public bool shareOverLan;
        public bool isDefault;
        public int localPort;
        public string pacUrl;
        public bool useOnlinePac;
        public bool availabilityStatistics;
        public bool autoCheckUpdate;
        public LogViewerConfig logViewer;
        public List<IPRange> ipRangeList;

        private static string CONFIG_FILE = "gui-config.json";
        private static string IP_LIST_FILE = "iplist.txt";

        public Server GetCurrentServer()
        {
            if (index >= 0 && index < configs.Count)
                return configs[index];
            else
                return GetDefaultServer();
        }

        public static void CheckServer(Server server)
        {
            CheckPort(server.server_port);
            CheckPassword(server.password);
            CheckServer(server.server);
        }

        public static List<IPRange> LoadIPRange()
        {
            List<IPRange> ipRangeList = new List<IPRange>();
            string[] ipRanges = File.ReadAllLines(IP_LIST_FILE);
            if (ipRanges == null)
                return null; 
            foreach (string ipRange in ipRanges) {
                if (ipRange.Contains("-")){
                    string[] tmp = ipRange.Split('-');
                    if (tmp.Length > 2)
                        return null;
                    IPRange ip = new IPRange();
                    ip.beginIP = tmp[0];
                    ip.endIP = tmp[1];
                    ipRangeList.Add(ip);
                }
                else {
                    IPRange ip = new IPRange();
                    ip.beginIP = ipRange;
                    ip.endIP = ipRange;
                    ipRangeList.Add(ip);
                }
            }
            return ipRangeList;
        }

        public static Configuration Load()
        {
            try
            {
                string configContent = File.ReadAllText(CONFIG_FILE);
                Configuration config = JsonConvert.DeserializeObject<Configuration>(configContent);
                config.isDefault = false;
                if (config.localPort == 0)
                    config.localPort = 1080;
                if (config.index == -1 && config.strategy == null)
                    config.index = 0;
                return config;
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                    Logging.LogUsefulException(e);
                return new Configuration
                {
                    index = 0,
                    isDefault = true,
                    localPort = 1080,
                    autoCheckUpdate = true,
                    configs = new List<Server>()
                    {
                        GetDefaultServer()
                    }
                };
            }
        }

        public static void Save(Configuration config)
        {
            if (config.index >= config.configs.Count)
                config.index = config.configs.Count - 1;
            if (config.index < -1)
                config.index = -1;
            if (config.index == -1 && config.strategy == null)
                config.index = 0;
            config.isDefault = false;
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(CONFIG_FILE, FileMode.Create)))
                {
                    string jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
                    sw.Write(jsonString);
                    sw.Flush();
                }
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e);
            }
        }

        public static Server GetDefaultServer()
        {
            return new Server();
        }

        private static void Assert(bool condition)
        {
            if (!condition)
                throw new Exception(I18N.GetString("assertion failure"));
        }

        public static void CheckPort(int port)
        {
            if (port <= 0 || port > 65535)
                throw new ArgumentException(I18N.GetString("Port out of range"));
        }

        public static void CheckLocalPort(int port)
        {
            CheckPort(port);
            if (port == 8123)
                throw new ArgumentException(I18N.GetString("Port can't be 8123"));
        }

        private static void CheckPassword(string password)
        {
            if (password.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Password can not be blank"));
        }

        private static void CheckServer(string server)
        {
            if (server.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Server IP can not be blank"));
        }
    }
}
