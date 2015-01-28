using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.KeyVault.Client;
using Newtonsoft.Json;
using SInnovations.ConfigurationManager.Logging;
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


        public static void RegisterAzureKeyVaultSecret(this ConfigurationManager config, string name, string secretUri)
        {
            config.RegisterSetting(name, () => secretUri, (str) => JsonConvert.DeserializeObject<Secret>(str));
        }
        public static Secret GetAzureKeyVaultSecret(this ConfigurationManager config, string name)
        {
            return config.GetSetting<Secret>(name);
        }
    }
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
        public ConfigurationManager ConfigurationManager{get;set;}

    }
    public class AzureKeyVaultSettingsProvider : ISettingsProvider
    {
        private static ILog Logger = LogProvider.GetCurrentClassLogger();
        private Lazy<KeyVaultClient> keyVaultClient;
        private AzureKeyVaultSettingsProviderOptions _options;
        private ConfigurationManager _config;
      

        public AzureKeyVaultSettingsProvider(AzureKeyVaultSettingsProviderOptions options) 
        {
            _options = options;
            _config = _options.ConfigurationManager;
            if (_config != null)
            {
                var providers = _config.GetProviders(Name);
                _config.RegisterSetting(_options.KeyVaultUriKey,
                    defaultvalue: string.IsNullOrWhiteSpace(_options.KeyVaultUri) ? null : _options.KeyVaultUri,
                    providers: providers);   
                _config.RegisterSetting(_options.AzureApplicationClientIdKey,
                    defaultvalue: string.IsNullOrWhiteSpace(_options.ClientId) ? null : _options.ClientId,
                    providers: providers);
                _config.RegisterSetting(_options.AzureApplicationClientSecretKey,
                    defaultvalue: string.IsNullOrWhiteSpace(_options.ClientSecret) ? null : _options.ClientSecret,
                    providers: providers);
            }

            keyVaultClient = new Lazy<KeyVaultClient>(() =>
            {
                
                
                return new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessToken));

            });
           
        }
        private string GetAccessToken(string authority, string resource, string scope)
        {
            var context = new AuthenticationContext(authority, null);
            var result = context.AcquireToken(resource, ClientCredentials);

            return result.AccessToken;
        }

        protected virtual ClientCredential ClientCredentials
        {
            get
            {
                return new ClientCredential(_config == null ?
                    _options.ClientId : _config.GetSetting<string>(_options.AzureApplicationClientIdKey),
                    _config == null ?
                    _options.ClientSecret : _config.GetSetting<string>(_options.AzureApplicationClientSecretKey)
                    );
            }
        }
        protected virtual string KeyVaultUri
        {
            get
            {
                return _config == null ?
                    _options.KeyVaultUri : _config.GetSetting<string>(_options.KeyVaultUriKey);
            }
        }

        private const string name = "azure.keyvault";
        public string Name
        {
            get { return name; }
        }

        public bool TryGetSetting(string settingName, out string settingValue)
        {
            settingValue = null;
            if(!settingName.StartsWith(KeyVaultUri))
                return false;
            try
            {
                var secret = keyVaultClient.Value.GetSecretAsync(settingName).GetAwaiter().GetResult();
                settingValue = JsonConvert.SerializeObject(secret);

            }
            catch(Exception ex)
            {
                Logger.ErrorException("Failed to get KeyVault Setting", ex);
                return false;
            }

            return true;
        }
    }
}
