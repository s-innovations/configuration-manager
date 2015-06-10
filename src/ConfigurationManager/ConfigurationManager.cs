using SInnovations.ConfigurationManager.Configuration;
using SInnovations.ConfigurationManager.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.ConfigurationManager
{

  

    public class ConfigurationManager
    {
        private static ILog Logger = LogProvider.GetCurrentClassLogger();

        private Dictionary<string, object> overrides = new Dictionary<string, object>();
        private readonly ConcurrentDictionary<string, ResetLazy<object>> _lazies = new ConcurrentDictionary<string,ResetLazy<object>>();
        private SortedList<int, ISettingsProvider> _providers = new SortedList<int, ISettingsProvider>(new DuplicateKeyComparer<int>());

        public static Func<String, object> StringConverter = (s) => s;
        public static Func<String, object> IntConverter = (s) => int.Parse(s);

        public ConfigurationManager(params ISettingsProvider[] providers)
        {
            foreach(var provider in providers)
                AddSettingsProvider(provider);
        }

        /// <summary>
        /// Get all registered providers except those named in <see cref="excludes"/>
        /// </summary>
        /// <param name="excludes">The providers to exclude</param>
        /// <returns>Array of providernames</returns>
        public string[] GetProviders(params string[] excludes)
        {
            return _providers.Values.Select(s=>s.Name)
                .Where(s => !excludes.Any(e => e == s)).ToArray();
        }
        /// <summary>
        /// Add a setting provider after construction time
        /// </summary>
        /// <param name="provider">The <seealso cref="ISettingsProvider"/> provider to be registered </param>
        /// <param name="order">The loading order of the given provider. First found value is returned among providers.</param>
        public void AddSettingsProvider(ISettingsProvider provider, int order = 0 )
        {
            _providers.Add(order, provider);
        }

        /// <summary>
        /// Register a setting for retriviel.
        /// 
        /// </summary>
        /// <param name="key"> Key is a local name for the setting that it can be queried on. If the key is already registered, it will be ignored</param>
        /// <param name="name">The setting name to query providers for, if null the key is used.</param>
        /// <param name="converter">A converter that takes the setting as a string and convert it to its object type.</param>
        /// <param name="defaultvalue">The default value for the value.</param>
        /// <param name="acceptnull">If true the setting can be returned as null if not found, otherwise exception will be trown that the setting is not found</param>
        /// <param name="providers">The provider names to be used for looking up the setting</param>
        public void RegisterSetting(string key, Func<string> name = null, Func<string, object> converter = null, string defaultvalue = null, bool acceptnull = false, params string[] providers)
        {
            if (_lazies.ContainsKey(key))
                return;
            int tries = 10;
            while (!_lazies.TryAdd(key, CreateLazy(name ?? (() => key), converter, defaultvalue, acceptnull, providers)) && tries-- > 0) ;

        }
      
        /// <summary>
        /// Override any setting with a fixed value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void RegisterOverride<T>(string key, T value)
        {
            overrides.Add(key, value);
        }
        /// <summary>
        /// Return the setting, first in the overrideable set of settings , then from setting providers.
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetSetting<T>(string key)
        {
            if (overrides.ContainsKey(key))
                return (T)overrides[key];

            if (!_lazies.ContainsKey(key))
                throw new KeyNotFoundException(key);

            return (T)_lazies[key].Value;
        }

        /// <summary>
        /// Try to get the setting and return true if it was found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetSetting<T>(string key, out T value)
        {

            if (overrides.ContainsKey(key))
            {
                value = (T)overrides[key];
                return true;
            }

            if (!_lazies.ContainsKey(key))
            {
                value = default(T);
                return false;
            }

            value = (T)_lazies[key].Value;
            return true;
        }


        #region Helpers
        private ResetLazy<object> CreateLazy(Func<string> settingnameFunc, Func<string, object> converter = null, string defaultvalue = null, bool acceptnull = false, params string[] providers)
        {
            converter = converter ?? StringConverter;
            return new ResetLazy<object>(() =>
            {
                var settingname = settingnameFunc();
                var value = GetValue(settingname, defaultvalue, providers);

                if (string.IsNullOrEmpty(value))
                {
                    if (acceptnull)
                        return null;


                    throw new ArgumentNullException(settingname, string.Format("No Default value for the setting {0}, please create a new deployment with this setting specified.", settingname));

                }

                return converter(value);

            });

        }
        private string GetValue(string settingName, string defaultvalue = null, params string[] providers)
        {
            return GetValue(settingName, () => defaultvalue, providers);

        }
        private string GetValue(string settingName, Func<string> defaultvalue, params string[] providersNames)
        {

            IEnumerable<ISettingsProvider> providers = _providers.Values;
            if (providersNames.Any())
                providers = providers.Where(p => providersNames.Contains(p.Name));

            
            string val = string.Empty;
            foreach (var provider in providers)
            {
                if (provider.TryGetSetting(settingName, out val))
                    return val;                
            }
            if(string.IsNullOrWhiteSpace(val))
            {
                var d = defaultvalue();
                if (d != null)
                    Logger.InfoFormat("Failed to retrieve setting '{0}' from settings providers: '{1}', it do not exist. Defaulting to : '{2}'",
                        settingName,string.Join(", ",providers.Select(p=>p.Name)), d);
                val = d;
            }

            return val;
        }
        #endregion
    }
}
