using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.ConfigurationManager.CryptoExtensions
{
    public static class ConfigurationManagerCryptoExtensions
    {
      
        public static string GetAndDecryptSetting(this ConfigurationManager manager,string key,string CertKey)
        {
            var certData = Convert.FromBase64String(manager.GetSetting<string>(CertKey));
            var cert = new X509Certificate2(certData,new SecureString(),X509KeyStorageFlags.Exportable);
            return cert.Decrypt(manager.GetSetting<string>(key),true);

        }
    }
}
