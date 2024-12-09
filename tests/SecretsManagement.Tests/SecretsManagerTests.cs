namespace SecretsManagement.Tests;

using System.Net;
using CredentialManagerVault;
using KeePassVault;

[TestFixture]
public class SecretsManagerTests
{
    private const string DatabasePath = "c:/temp/working/database.kdbx";
    private const string MasterPassword = "your-master-password";
    private SecretsManager _secretsManager;

    [SetUp]
    public void Setup()
    {
        // Create an instance of SecretsManager
        _secretsManager = new SecretsManager();
    }

    [Test]
    public async Task TestKeePassSecretsVault()
    {
        // Create vault parameters
        var vaultParameters = new Dictionary<string, object>
        {
            { "databasePath", DatabasePath },
            { "masterPassword", MasterPassword }
        };

        // Register the KeePass vault
        bool registerResult = await _secretsManager.RegisterSecretVaultAsync(
            moduleName: typeof(KeePassSecretsVault).AssemblyQualifiedName,
            vaultName: "KeePassVault",
            vaultParameters: vaultParameters,
            isDefaultVault: true
        );
        Assert.IsTrue(registerResult, "Failed to register KeePass vault");

        // Set a secret
        bool setSecretResult = await _secretsManager.SetSecretAsync("MySecret", "MySecretValue");
        Assert.IsTrue(setSecretResult, "Failed to set secret");

        // Get the secret
        var secret = await _secretsManager.GetSecretAsync("MySecret", asPlainText: true);
        Assert.AreEqual("MySecretValue", secret, "Failed to retrieve secret");

        // Remove the secret
        bool removeSecretResult = await _secretsManager.RemoveSecretAsync("MySecret", "KeePassVault");
        Assert.IsTrue(removeSecretResult, "Failed to remove secret");
    }


    [Test]
    public async Task TestCredentialManagerSecretsVault()
    {
        var vaultParameters = new Dictionary<string, object>();


        var vaultName = typeof(CredentialManagerSecretsVault).Name;

        // Register the KeePass vault
        bool registerResult = await _secretsManager.RegisterSecretVaultAsync(
            moduleName: typeof(CredentialManagerSecretsVault).AssemblyQualifiedName,
            vaultName: vaultName,
            vaultParameters: vaultParameters,
            isDefaultVault: true
        );
        Assert.IsTrue(registerResult, "Failed to register KeePass vault");

        // Set a secret
        bool setSecretResult = await _secretsManager.SetSecretAsync("MySecret", new NetworkCredential(string.Empty, "MySecretValue"));
        Assert.IsTrue(setSecretResult, "Failed to set secret");

        // Get the secret
        var secret = await _secretsManager.GetSecretAsync("MySecret", asPlainText: true) as NetworkCredential;
        Assert.AreEqual("MySecretValue", secret?.Password, "Failed to retrieve secret");

        // Remove the secret
        bool removeSecretResult = await _secretsManager.RemoveSecretAsync("MySecret", vaultName);
        Assert.IsTrue(removeSecretResult, "Failed to remove secret");

       var information =  await _secretsManager.GetSecretInfoAsync();
       foreach (var secretInformation in information)
       {
           Console.WriteLine(secretInformation.Name);
       }

       var credential = (_secretsManager.GetSecretAsync("THI|QlikServer|qa-thi.cw-eng-test.com|d.mccord").Result) as NetworkCredential;
       Console.WriteLine($"THI|QlikServer|qa-thi.cw-eng-test.com|d.mccord = {credential.UserName}");
    }
}