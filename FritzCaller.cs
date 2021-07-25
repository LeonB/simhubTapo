using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net;
using System.Security.Cryptography;
using System.IO;

namespace Argo.FritzCall
{
    [PluginDescription("Fritz! DECT200 Plugin")]
    [PluginAuthor("Argo")]
    [PluginName("FritzCall")]
    public class FritzCaller : IPlugin, IDataPlugin, IWPFSettings
    {

        public DataPluginDemoSettings Settings;


        /// <summary>
        /// Instance of the current plugin manager
        /// </summary>
        public PluginManager PluginManager { get; set; }

        /// <summary>
        /// Called one time per game data update, contains all normalized game data, 
        /// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        /// 
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        /// 
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data"></param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            return;
            //// Define the value of our property (declared in init)
            //pluginManager.SetPropertyValue("CurrentDateTime", this.GetType(), DateTime.Now);

            //if (data.GameRunning)
            //{
            //    if (data.OldData != null && data.NewData != null)
            //    {
            //        if (data.OldData.SpeedKmh < Settings.SpeedWarningLevel && data.OldData.SpeedKmh >= Settings.SpeedWarningLevel)
            //        {
            //            // Trigger an event
            //            pluginManager.TriggerEvent("SpeedWarning", this.GetType());
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here ! 
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            // Save settings
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControl(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting plugin");

            // Load settings
            Settings = this.ReadCommonSettings<DataPluginDemoSettings>("GeneralSettings", () => new DataPluginDemoSettings());
            pluginManager.AddAction("CallFritz", this.GetType(), CallFritz);

        }

        private void CallFritz(PluginManager arg1, string arg2)
        {
            /// Big thanks to https://sirmark.de/computer/fritzdect-200-schaltsteckdose-mit-c-steuern-1895.html for the used example
            string username = Settings.Username;
            string password = Settings.Password;

            string sain = Settings.sAIN;
            string sid = GetSessionId(username, password);

            string sURL = string.Empty;

            //switchOn
            //sURL = string.Format("http://fritz.box/webservices/homeautoswitch.lua?switchcmd=setswitchon&sid={0}&ain={1}", sid, sAIN);

            //switchOff
            //sURL = string.Format("http://fritz.box/webservices/homeautoswitch.lua?switchcmd=setswitchoff&sid={0}&ain={1}", sid, sAIN);

            //toggle
            sURL = string.Format("http://fritz.box/webservices/homeautoswitch.lua?switchcmd=setswitchtoggle&sid={0}&ain={1}", sid, sain);

            //Call
            string sResult = readUrl(sURL);

            //Logout
            sURL = string.Format("http://fritz.box/login.lua?page=/home/home.lua&logout=1&sid={0}", sid);

            sResult = readUrl(sURL);
        }

        public static string GetSessionId(string benutzername, string kennwort)
        {

            XDocument doc = XDocument.Load(@"http://fritz.box/login_sid.lua");
            string sid = fl_Get_Value_of_Node_in_XDocument_by_NodeName(doc, "SID");

            if (sid == "0000000000000000")
            {
                string challenge = fl_Get_Value_of_Node_in_XDocument_by_NodeName(doc, "Challenge");
                string sResponse = fl_GetResponse_by_TempUser_Passwort(challenge, kennwort);

                string uri = @"http://fritz.box/login_sid.lua?username=" + benutzername + @"&response=" + sResponse;
                doc = XDocument.Load(uri);

                sid = fl_Get_Value_of_Node_in_XDocument_by_NodeName(doc, "SID");
            }
            return sid;

        }

        public static string readUrl(string url)
        {
            //read page with sid access, by webrequest
            Uri uri = new Uri(url);
            HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string str = reader.ReadToEnd();
            return str;
        }

        public static string fl_Get_Value_of_Node_in_XDocument_by_NodeName(XDocument doc, string name)
        {
            XElement info = doc.FirstNode as XElement;
            return info.Element(name).Value;
        }

        public static string fl_GetResponse_by_TempUser_Passwort(string challenge, string kennwort)
        {            
            return challenge + "-" + fl_Get_MD5Hash_of_String(challenge + "-" + kennwort);
         
        }

        public static string fl_Get_MD5Hash_of_String(string input)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(Encoding.Unicode.GetBytes(input));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("x2"));
            }
            return sb.ToString();
        }

    }
}