using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.ConfigurationManager.CryptoExtensions
{
    public static class EncryptionHelper
    {
        public static string Encrypt(this X509Certificate2 certificate,string settingValue, bool fOAEP)
        {


            if (settingValue == null)
            {
                throw new ArgumentNullException("settingValue");
            }
            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }

            byte[] plainData = Encoding.UTF8.GetBytes(settingValue);

            using (RSACryptoServiceProvider provider = new RSACryptoServiceProvider())
            {
                // Note that we use the public key to encrypt
                provider.FromXmlString(GetPublicKey(certificate));
                return Convert.ToBase64String(provider.Encrypt(plainData, fOAEP));
            }
        }
        public static string GetPublicKey(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }

            return certificate.PublicKey.Key.ToXmlString(false);
        }

        public static string Decrypt(this X509Certificate2 certificate, string encryptedBase64String, bool fOAEP)
                               
        {

            if (encryptedBase64String == null)
            {
                throw new ArgumentNullException("encryptedBase64String");
            }
            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }
            var encryptedData = Convert.FromBase64String(encryptedBase64String);

            using (RSACryptoServiceProvider provider = new RSACryptoServiceProvider())
            {
                // Note that we use the private key to decrypt
                provider.FromXmlString(GetXmlKeyPair(certificate));

                return Encoding.UTF8.GetString(provider.Decrypt(encryptedData, fOAEP));
            }
        }
        public static string GetXmlKeyPair(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }

            if (!certificate.HasPrivateKey)
            {
                throw new ArgumentException("certificate does not have a private key");
            }
            else
            {
                return certificate.PrivateKey.ToXmlString(true);
            }
        }
    }
}
