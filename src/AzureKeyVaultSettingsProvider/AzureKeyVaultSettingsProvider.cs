using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using SInnovations.ConfigurationManager.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.ConfigurationManager.Providers
{


    public class AzureKeyVaultSettingsProvider : ISettingsProvider
    {

        private static ILog Logger = LogProvider.GetCurrentClassLogger();
        private Lazy<KeyVaultClient> keyVaultClient;
        private Lazy<SecretItem[]> allSecrets;

        private AzureKeyVaultSettingsProviderOptions _options;
        private ConfigurationManager _config;
       
        public const string AzureKeyVaultSettingsProviderName = "azure.keyvault";
        public string Name
        {
            get { return AzureKeyVaultSettingsProviderName; }
        }

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
                    converter: _options.SecretConverter,
                    providers: providers);
            }

            keyVaultClient = new Lazy<KeyVaultClient>(() =>
            {
                return new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessToken));

            });
            allSecrets = new Lazy<SecretItem[]>(() =>
            {
                var secrets = Task.Run(()=> this.keyVaultClient.Value.GetSecretsAsync(this.KeyVaultUri)).GetAwaiter().GetResult();
                return secrets.Value.ToArray();
            });

        }
        private ConcurrentDictionary<string, Lazy<AuthenticationContext>> _authCache = new ConcurrentDictionary<string, Lazy<AuthenticationContext>>();
        private Lazy<AuthenticationContext> CreateContext(string key)
        {
            return new Lazy<AuthenticationContext>(() =>
            {
                var parts = key.Split(',');
                var context = new AuthenticationContext(parts[0], new TokenCache());
                //var result = await context.AcquireTokenAsync(parts[1], ClientCredentials);
                return context;
            });
        }
        private async Task<string> GetAccessToken(string authority, string resource, string scope)
        {
          
            try
            {

                var context = _authCache.GetOrAdd(string.Join(",", authority, resource), CreateContext).Value;
                var task = context.AcquireTokenAsync(resource, ClientCredentials);
                task.Wait(5000);
                var result = await task;

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Failed to get access token:", ex);
                throw;
            }
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



        public bool TryGetSetting(string settingName, out string settingValue)
        {
            Logger.InfoFormat("Trying to get setting {0}", settingName);
            settingValue = null;

            if (!allSecrets.Value.Any(s => settingName.StartsWith(s.Id)))
            {
                Logger.WarnFormat("The setting was not found: {0}", string.Join(", ",allSecrets.Value.Select(s=>s.Id)));

                return false;
            }

            if (!settingName.StartsWith(KeyVaultUri))
            {
                Logger.WarnFormat("The setting did not start with the keyvault uri: {0}", KeyVaultUri);

                return false;
            }
            try
            {

                var secret =  Task.Run(()=> keyVaultClient.Value.GetSecretAsync(settingName)).GetAwaiter().GetResult();
                settingValue = JsonConvert.SerializeObject(secret);

            }
            catch (Exception ex)
            {
                Logger.ErrorException("Failed to get KeyVault Setting", ex);
                return false;
            }

            return true;
        }

    }
}
