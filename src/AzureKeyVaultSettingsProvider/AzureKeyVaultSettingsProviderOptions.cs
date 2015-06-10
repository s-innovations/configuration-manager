using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.ConfigurationManager.Providers
{
    public class AzureKeyVaultSettingsProviderOptions
    {

        public AzureKeyVaultSettingsProviderOptions()
        {
            KeyVaultUriKey = AzureKeyVaultDefaults.DefaultKeyVaultUriKey;
            AzureApplicationClientIdKey = AzureKeyVaultDefaults.DefaultAzureADClientIdKey;
            AzureApplicationClientSecretKey = AzureKeyVaultDefaults.DefaultAzureADClientSecretKey;
        }
        public string KeyVaultUriKey { get; set; }
        public string AzureApplicationClientIdKey { get; set; }
        public string AzureApplicationClientSecretKey { get; set; }
        public string KeyVaultUri { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public ConfigurationManager ConfigurationManager { get; set; }

    }
}
