﻿using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using SInnovations.ConfigurationManager.Configuration;
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


    public class AzureKeyVaultSettingsProvider : ISettingsProvider, IObservableSettingProvider
    {
        private static string NoEncrypter(string s)
        {
            return s;
        }

        private static ILog Logger = LogProvider.GetCurrentClassLogger();
        private Lazy<KeyVaultClient> keyVaultClient;
        private ResetLazy<SecretItem[]> allSecrets;

        private AzureKeyVaultSettingsProviderOptions _options;
        private ConfigurationManager _config;

        private System.Timers.Timer _idleCheckTimer;
        private Dictionary<string, SecretBundle> _loadedSecrets = new Dictionary<string, SecretBundle>();

        public const string AzureKeyVaultSettingsProviderName = "azure.keyvault";
        public string Name
        {
            get { return AzureKeyVaultSettingsProviderName; }
        }
        private void SetIdleCheckTimer()
        {

            _idleCheckTimer = new System.Timers.Timer(TimeSpan.FromMinutes(10).TotalMilliseconds);
            _idleCheckTimer.AutoReset = false;
            _idleCheckTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnIdleCheckTimer);
            _idleCheckTimer.Start();
        }
        private void OnIdleCheckTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            Logger.Trace("Checking for Settings Changes");
            try
            {

                if (_loadedSecrets.Any())
                {
                    var all = _loadedSecrets;
                    _loadedSecrets = new Dictionary<string, SecretBundle>();//Reset the settinglist;

                    allSecrets.Reset();
                    foreach (var name in all.Keys)
                    {
                        var value = all[name];
                        var meta = allSecrets.Value.FirstOrDefault(s => s.Id == value.Id);

                        if (meta != null && meta.Attributes.Updated > value.Attributes.Updated)
                        {
                            Logger.TraceFormat("Found secret {0} that has been updated at {1}", meta.Id, meta.Attributes.Updated);


                            OnSettingHasBeenUpdated(new SettingChangedEventArgs { SettingName = name, Provider = this });
                        }
                    }


                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                lock (objectLock)
                {
                    if (_settingsHasBeenUpdated != null)
                    {
                        SetIdleCheckTimer();
                    }
                    else
                    {

                        _idleCheckTimer = null;
                    }
                }
            }

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


                if (options.UseKeyVaultCredentialsKey)
                {
                    _config.RegisterSetting(_options.KeyVaultCredentialsKey,
                    converter: _options.SecretConverter ?? NoEncrypter,
                    providers: providers);
                }
                else
                {
                    _config.RegisterSetting(_options.AzureApplicationClientIdKey,
                  defaultvalue: string.IsNullOrWhiteSpace(_options.ClientId) ? null : _options.ClientId,
                  providers: providers);
                    _config.RegisterSetting(_options.AzureApplicationClientSecretKey,
                        defaultvalue: string.IsNullOrWhiteSpace(_options.ClientSecret) ? null : _options.ClientSecret,
                        converter: _options.SecretConverter ?? NoEncrypter,
                        providers: providers);
                }
            }

            keyVaultClient = new Lazy<KeyVaultClient>(() =>
            {
                return new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessToken));

            });
            allSecrets = new ResetLazy<SecretItem[]>(() =>
            {
                Logger.Trace("Fetching all settings metadata");
                var secrets = Task.Run(() => this.keyVaultClient.Value.GetSecretsAsync(this.KeyVaultUri)).GetAwaiter().GetResult();
                var secretsValue = secrets.ToArray();
                Logger.TraceFormat("Found {0} settings: {1}", secretsValue.Length, string.Join(", ", secretsValue.Select(s => string.Format("{0}={1}", s.Id, s.Attributes.Updated))));
                return secretsValue;
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
              
                if(_config == null)
                {
                    return new ClientCredential(_options.ClientId, _options.ClientSecret);
                }
                else if (_options.UseKeyVaultCredentialsKey)
                {
                    var both = _config.GetSetting<string>(_options.KeyVaultCredentialsKey).Split(':');
                    return new ClientCredential(both.First(), both.Last());
                }
                else
                {
                    return new ClientCredential(_config.GetSetting<string>(_options.AzureApplicationClientIdKey), _config.GetSetting<string>(_options.AzureApplicationClientSecretKey));

                }
               
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
                Logger.WarnFormat("The setting was not found: {0}", string.Join(", ", allSecrets.Value.Select(s => s.Id)));

                return false;
            }

            try
            {

                var secret = Task.Run(() => keyVaultClient.Value.GetSecretAsync(settingName)).GetAwaiter().GetResult();
                _loadedSecrets[settingName] = secret;
                settingValue = JsonConvert.SerializeObject(secret);

            }
            catch (Exception ex)
            {
                Logger.ErrorException("Failed to get KeyVault Setting", ex);
                return false;
            }

            return true;
        }


        protected virtual void OnSettingHasBeenUpdated(SettingChangedEventArgs e)
        {
            if (_settingsHasBeenUpdated != null)
            {
                _settingsHasBeenUpdated(this, e);
            }
        }
        private event EventHandler<SettingChangedEventArgs> _settingsHasBeenUpdated;
        object objectLock = new Object();
        public event EventHandler<SettingChangedEventArgs> SettingHasBeenUpdated
        {
            add
            {
                lock (objectLock)
                {
                    _settingsHasBeenUpdated += value;
                    if (_idleCheckTimer == null)
                        SetIdleCheckTimer();

                }


            }
            remove
            {
                lock (objectLock)
                {
                    _settingsHasBeenUpdated -= value;

                }
            }
        }
    }
}
