
# S-Innovations Configuration Manager

A small lib that I been using for the past few years for azure solutions that I am making public. 
I am moving the stable parts into this as I get my private repro refactored alittle.

The project is about getting configuration values, and I created this because of having a common way of getting settings, wether it being from app.config,web.config or azure cloud configuration. 

The reason for making this public now is that, another interesting settings provider can be used.

## Azure Key Vault
Instead of configuraing all these secrets and settings in appconfig,service definition or web.config. I am moving all my settings to the Azure Key Vault and then by a settings provider in this lib retrieving them. 

### Benefits 
Ofcause, keys are stored in a secure place on azure with HSMs certified to FIPS 140-2 level 2 and Common Criteria EAL4+ standards support. 

But whats most interesting is that the CSO of the company can now manage the secrets and authorize different applications access to it, all within the Azure AD management system. Consultants or temporate employees should not have access to those secrets.


## Set up Vault

```
1) PS C:\> add-azureaccount
2) PS C:\> Get-AzureSubscription
3) PS C:\> Set-ExecutionPolicy Unrestricted -Scope Process
4) PS C:\> import-module C:\dev\KeyVaultScripts\KeyVaultManager
5) PS C:\> Switch-AzureMode AzureResourceManager
6) PS C:\> New-AzureKeyVault -VaultName 'sinnovations-weu' -ResourceGroupName 'sinnovations-weu' -Location 'West Europe' -SKU 'Premium'
7) PS C:\> $secretvalue = ConvertTo-SecureString 'Z4PfCLrm' -AsPlainText -Force
8) PS C:\> $secret = Set-AzureKeyVaultSecret -VaultName 'sinnovations-weu' -Name 'sendgrid-secret' -SecretValue $secretvalue
9) PS C:\> Set-AzureKeyVaultAccessPolicy -VaultName 'sinnovations-weu' -ServicePrincipalName 889673a3-1fac-4d47-a3c9-755dffa31286 -PermissionsToKeys decrypt,sign -PermissionsToSecrets get
```

## Use ConfigurationManager

In the unit test you can find a small example. It uses a AppSettingsProvider to get the client id, vault uri and client secret. Vault Uri is given when you create your vault. Client Id and Secrets are created in the azure management portal under your AD. Add Application and in the configure tab you will find client id and options to create secrets.

    <add key="Azure.KeyVault.Uri" value="https://sinnovations-weu.vault.azure.net" />
    <add key="Microsoft.Azure.AD.Application.ClientId" value="889673a3-1fac-4d47-a3c9-755dffa31286" />
    <add key="Microsoft.Azure.AD.Application.ClientSecret" value="<secret>" />

```
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
            "https://sinnovations-weu.vault.azure.net/secrets/sendgrid-secret/0469a8ee294e4c7bb82679b6e364c229");

            

    //In your application do IoC.Resolve<ConfigurationManager>();
        var secret = config.GetSetting<Secret>("sendgrid_secret");
        var a = config.GetAzureKeyVaultSecret("sendgrid_secret");

    //And there is your secret taht you can use.


}
```


## Please star the repro
if this kind of configuration is something you would use. I would put it into a nuget than.
