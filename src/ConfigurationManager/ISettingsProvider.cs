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
}
