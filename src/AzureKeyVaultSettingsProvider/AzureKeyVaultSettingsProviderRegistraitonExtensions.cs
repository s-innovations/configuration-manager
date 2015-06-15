using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.ConfigurationManager.Providers
{

    public static class AzureKeyVaultSettingsProviderRegistraitonExtensions
    {
        public static ConfigurationManager UseAzureKeyVault(this ConfigurationManager manager, AzureKeyVaultSettingsProviderOptions options)
        {
           // RegisterSetting(AzureKeyVaultDefaults.DefaultAzureADClientSecretKey, converter: CryptoHelper.DecryptEnvelop);

            options.ConfigurationManager = manager;
            manager.AddSettingsProvider(new AzureKeyVaultSettingsProvider(options),options.LoadingOrder);
            return manager;
        }
    }
}
