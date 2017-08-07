using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SInnovations.ConfigurationManager.Providers;
using Microsoft.Azure.KeyVault;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.KeyVault.Models;

namespace SInnovations.ConfigurationManager.Tests
{
    public static class CryptoHelper
    {
        public static string GetRandomKey(int length = 256)
        {
            using (var r = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[length];
                r.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }

        public static string DecryptEnvelop(string base64EncryptedString)
        {
            var encryptedBytes = Convert.FromBase64String(base64EncryptedString);
            var envelope = new EnvelopedCms();
            envelope.Decode(encryptedBytes);
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            envelope.Decrypt(store.Certificates);
            return Encoding.Unicode.GetString(envelope.ContentInfo.Content);
        }
    }
    /// <summary>
    /// Summary description for AzureVaultUnitTests
    /// </summary>
    [TestClass]
    public class AzureVaultUnitTests
    {
        public AzureVaultUnitTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion
        [TestMethod]
        public void TestMethod2()
        {
            var a = "MIICPQYJKoZIhvcNAQcDoIICLjCCAioCAQAxggFUMIIBUAIBADA4MCQxIjAgBgNVBAMTGUluZnJhc3RydWN0dXJlQ2VydGlmaWNhdGUCEGFDoHw8IjWWTtug1DBypUAwDQYJKoZIhvcNAQEBBQAEggEAoqy0MBJLCrS3e4//RB8+VsV0UETaeSY+Kh7n1Ml7oKNzvLbgGO0FYECTCKAdrDDArTqiMCfMoIQh16uMdT32IWPDtORrkF6toyFA+LEP/wUuD5thOHTpWpMvEo86edkTxx7ewvju9lCMBtT5InK5dF8YuPuJSu5DkGOYn7l0QfWkMym5xSlZ/aUTgbwn0DLX7HaBpu1edZYCxqlOyXmuQAQTtpCAO2txqSeOybKfSfSKc6fXIMCu/az3/PvcKThG4eBvw3UcUI7SKeudW9Yi+SoZkbItAZzOEtDl0XXkZN5/+uSYQrXke6mFhIuIrHs18Nrs96L11HPEj5zJG2HPrjCBzAYJKoZIhvcNAQcBMBQGCCqGSIb3DQMHBAgRhSap0YWMX4CBqMs839PUS8o2HZGV6xSnL2ywZ6HwuzknUSc+n7fHqMjXGoeIwu7AkfL1Shd2yiRNUJRKZcaHtjziyzfln77yw4u4rbX5ONOY3DijwXXrT/rafH03Td5FPyZtVSh60Rwa1NASJm7kk3cX8hE25+jwiEwOMLIG9p3ZARJvf4jY56+2wfHdx88NkZqEG0IAmcbuP6OpNxHVmMM6xqWaQU/JPiFRIs/MFE5vNg==";

            Console.WriteLine(CryptoHelper.DecryptEnvelop(a));
        }
        [TestMethod]
        public void TestMethod1()
        {
            //Add the following to your DI as a singleton.
            
                var config = new ConfigurationManager(new AppSettingsProvider());
                var options = new AzureKeyVaultSettingsProviderOptions
                {
                    ConfigurationManager = config, //Used to get the clientid,secret and uri of vault.
                    SecretConverter = CryptoHelper.DecryptEnvelop

                };
                config.AddSettingsProvider(new AzureKeyVaultSettingsProvider(options));

                config.RegisterAzureKeyVaultSecret("sendgrid_secret");

                

            //In your application do IoC.Resolve<ConfigurationManager>();
                var secret = config.GetSetting<SecretBundle>("sendgrid_secret");
                var a = config.GetAzureKeyVaultSecret("sendgrid_secret");

            //And there is your secret taht you can use.


        }
    }
}
