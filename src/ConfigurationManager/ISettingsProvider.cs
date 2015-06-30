using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.ConfigurationManager
{
    public interface ISettingsProvider
    {
        string Name { get; }
       
        bool TryGetSetting(string settingName, out string settingValue);
    }
    public class SettingChangedEventArgs : EventArgs
    {
        // class members
        public string SettingName { get; set; }
        public ISettingsProvider Provider { get; set; }
    }
    public interface IObservableSettingProvider
    {
        event EventHandler<SettingChangedEventArgs> SettingHasBeenUpdated;
    }
}
