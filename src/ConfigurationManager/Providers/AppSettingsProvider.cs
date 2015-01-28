using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.ConfigurationManager.Providers
{
    public class AppSettingsProvider : ISettingsProvider
    {
        public string Name
        {
            get { return "app.config"; }
        }

        public bool TryGetSetting(string settingName, out string settingValue)
        {
            settingValue = System.Configuration.ConfigurationManager.AppSettings[settingName];
            return settingValue != null;
        }
    }
}
