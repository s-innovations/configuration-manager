using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.ConfigurationManager.Providers
{
    public class RoleEnvironmentSettingsProvider : ISettingsProvider
    {
        private const string name = "azure.roleenvironment";
        public string Name
        {
            get { return name; }
        }

        public bool TryGetSetting(string settingName, out string settingValue)
        {
            try
            {
                settingValue = RoleEnvironment.GetConfigurationSettingValue(settingName);
                return true;
            }
            catch (RoleEnvironmentException)
            {
                 settingValue= null;
                 return false;
            }

        }
    }
}
