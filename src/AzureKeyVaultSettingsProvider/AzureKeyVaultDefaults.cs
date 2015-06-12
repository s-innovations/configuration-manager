using Microsoft.Azure.KeyVault;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.ConfigurationManager.Providers
{
    public static class AzureKeyVaultDefaults
    {
        public const string DefaultKeyVaultUriKey = "Azure.KeyVault.Uri";
        public const string DefaultAzureADClientIdKey = "Microsoft.Azure.AD.Application.ClientId";
        public const string DefaultAzureADClientSecretKey = "Microsoft.Azure.AD.Application.ClientSecret";


        public static void RegisterAzureKeyVaultSecret(this ConfigurationManager config, string name, Uri secretUri)
        {
            config.RegisterSetting(name, () => secretUri.AbsoluteUri, (str) => JsonConvert.DeserializeObject<Secret>(str));
        }

        public static void RegisterAzureKeyVaultSecret(this ConfigurationManager config, string name, string secretName,string secretVersion=null)
        {
            config.RegisterSetting(name, () => string.Format("{0}/secrets/{1}/{2}",
                config.GetSetting<string>(AzureKeyVaultDefaults.DefaultKeyVaultUriKey).Trim('/'),
                secretName, secretVersion).Trim('/'),
                (str) => JsonConvert.DeserializeObject<Secret>(str));
        }

        public static Secret GetAzureKeyVaultSecret(this ConfigurationManager config, string name)
        {
            return config.GetSetting<Secret>(name);
        }
    }
}
