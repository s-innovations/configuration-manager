using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SInnovations.ConfigurationManager.Providers;
using Microsoft.KeyVault.Client;

namespace SInnovations.ConfigurationManager.Tests
{
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
        public void TestMethod1()
        {
            //Add the following to your DI as a singleton.
            
                var config = new ConfigurationManager(new AppSettingsProvider());
                var options = new AzureKeyVaultSettingsProviderOptions
                {
                    ConfigurationManager = config, //Used to get the clientid,secret and uri of vault.
                };
                config.AddSettingsProvider(new AzureKeyVaultSettingsProvider(options));

                config.RegisterAzureKeyVaultSecret("sendgrid_secret",
                    "{vault_uri}/secrets/{scrent_name}/{secret_id}");

            

            //In your application do IoC.Resolve<ConfigurationManager>();
                var secret = config.GetSetting<Secret>("sendgrid_secret");
                var a = config.GetAzureKeyVaultSecret("sendgrid_secret");

            //And there is your secret taht you can use.


        }
    }
}
