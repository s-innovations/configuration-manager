using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography.Pkcs;
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

        public static string Decrypt(string base64EncryptedString)
        {
            var encryptedBytes = Convert.FromBase64String(base64EncryptedString);
            var envelope = new EnvelopedCms();
            envelope.Decode(encryptedBytes);
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            envelope.Decrypt(store.Certificates);
            return Encoding.UTF8.GetString(envelope.ContentInfo.Content);
        }
    }
}
