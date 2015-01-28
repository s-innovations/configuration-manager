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

    /// <summary>
    /// Comparer for comparing two keys, handling equality as beeing greater
    /// Use this Comparer e.g. with SortedLists or SortedDictionaries, that don't allow duplicate keys
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class DuplicateKeyComparer<TKey>
                    :
                 IComparer<TKey> where TKey : IComparable
    {
        #region IComparer<TKey> Members

        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);

            if (result == 0)
                return 1;   // Handle equality as beeing greater
            else
                return result;
        }

        #endregion
    }

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

        public string[] GetProviders(params string[] excludes)
        {
            return _providers.Values.Select(s=>s.Name)
                .Where(s => !excludes.Any(e => e == s)).ToArray();
        }

        public void AddSettingsProvider(ISettingsProvider provider, int order = 0 )
        {
            _providers.Add(order, provider);
        }

        public void RegisterSetting(string key, Func<string> name = null, Func<string, object> converter = null, string defaultvalue = null, bool acceptnull = false, params string[] providers)
        {
            if (_lazies.ContainsKey(key))
                return;
            int tries = 10;
            while (!_lazies.TryAdd(key, CreateLazy(name ?? (() => key), converter, defaultvalue, acceptnull, providers)) && tries-- > 0) ;

        }
      
        public void RegisterOverride<T>(string key, T value)
        {
            overrides.Add(key, value);
        }
        public T GetSetting<T>(string key)
        {
            if (overrides.ContainsKey(key))
                return (T)overrides[key];

            if (!_lazies.ContainsKey(key))
                throw new KeyNotFoundException(key);

            return (T)_lazies[key].Value;
        }

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
            if(val==string.Empty)
            {
                var d = defaultvalue();
                if (d != null)
                    Logger.InfoFormat("Failed to retrieve setting '{0}' from settings providers: '{1}', it do not exist. Defaulting to : '{2}'",
                        settingName,string.Join(", ",providers.Select(p=>p.Name)), d);
                val = d;
            }

            return val;
        }
    }
}
