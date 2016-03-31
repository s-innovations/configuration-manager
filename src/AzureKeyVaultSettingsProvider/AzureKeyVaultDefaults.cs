using Microsoft.Azure.KeyVault;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Runtime.InteropServices;

namespace SInnovations.ConfigurationManager.Providers
{
    public static class AzureKeyVaultDefaults
    {
        public const string DefaultKeyVaultUriKey = "Azure.KeyVault.Uri";
        public const string DefaultAzureADClientIdKey = "Microsoft.Azure.AD.Application.ClientId";
        public const string DefaultAzureADClientSecretKey = "Microsoft.Azure.AD.Application.ClientSecret";

        public const string DefaultKeyVaultCredentials = "Azure.KeyVault.Credentials";

        public static void RegisterAzureKeyVaultSecret(this ConfigurationManager config, string name, Uri secretUri)
        {
            config.RegisterSetting(name, () => secretUri.AbsoluteUri, (str) => JsonConvert.DeserializeObject<Secret>(str));
        }

        public static void RegisterAzureKeyVaultSecret(this ConfigurationManager config, string secretName,string secretVersion=null,string name=null)
        {
            config.RegisterSetting(name??secretName, () => string.Format("{0}/secrets/{1}/{2}",
                config.GetSetting<string>(AzureKeyVaultDefaults.DefaultKeyVaultUriKey).Trim('/'),
                secretName, secretVersion).Trim('/'),
                (str) => JsonConvert.DeserializeObject<Secret>(str));
        }
        public static void RegisterAzureKeyVaultSecretSecureString(this ConfigurationManager config, string secretName, string secretVersion = null, string name = null)
        {
            config.RegisterSetting(name ?? secretName, () => string.Format("{0}/secrets/{1}/{2}",
                  config.GetSetting<string>(AzureKeyVaultDefaults.DefaultKeyVaultUriKey).Trim('/'),
                  secretName, secretVersion).Trim('/'),
                (str) => convertToSecureString( JsonConvert.DeserializeObject<Secret>(str).Value));
        }
        public static SecureString convertToSecureString(string strPassword)
        {
            var secureStr = new SecureString();
            if (strPassword.Length > 0)
            {
                foreach (var c in strPassword.ToCharArray()) secureStr.AppendChar(c);
            }
            return secureStr;
        }
        public static string ConvertToUnsecureString(this SecureString securePassword)
        {
            if (securePassword == null)
                throw new ArgumentNullException("securePassword");

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        public static Secret GetAzureKeyVaultSecret(this ConfigurationManager config, string name)
        {
            Secret value;
            if(!config.TryGetSetting<Secret>(name, out value))
            {
                config.RegisterAzureKeyVaultSecret(name);
                config.TryGetSetting<Secret>(name, out value);
            }
            if (value == null)
                throw new Exception(string.Format("Could not get secret {0}", name));
            
            return value;
        }
       
        public static SecureString GetAureKeyVaultSecretSecureString(this ConfigurationManager config, string name)
        {
            SecureString value;
            if (!config.TryGetSetting<SecureString>(name, out value))
            {
                config.RegisterAzureKeyVaultSecretSecureString(name);
                config.TryGetSetting<SecureString>(name, out value);
            }
            if (value == null)
                throw new Exception(string.Format("Could not get secret {0}", name));

            return value;
        }
    }
}
