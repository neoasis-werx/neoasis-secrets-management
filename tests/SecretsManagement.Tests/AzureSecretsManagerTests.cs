
namespace SecretsManagement.Tests;

using AzSecretsVault;

using SecretsManagement.Abstractions;

[TestFixture]
public class AzureSecretsManagerTests
{
    private SecretsManager _secretsManager;

    [SetUp]
    public void Setup()
    {
        // Create an instance of SecretsManager
        _secretsManager = new SecretsManager();
    }

    [Test]
    public async Task TestAzureKeyVaultSecretsVault()
    {

        const string secretName = "THI--QlikServer--dev-thi-qlik01--MySecret";
        const string keyVaultName = "dev-thi-deployment-vault";

        const string kvUri = $"https://{keyVaultName}.vault.azure.net";


        // Create vault parameters
        var vaultParameters = new Dictionary<string, object>
        {
            { "vaultUri", kvUri }
        };

        // Register the Azure Key Vault
        bool registerResult = await _secretsManager.RegisterSecretVaultAsync(
            moduleName: typeof(AzureKeyVaultSecretsVault).AssemblyQualifiedName,
            vaultName: keyVaultName,
            vaultParameters: vaultParameters,
            isDefaultVault: true
        );
        Assert.IsTrue(registerResult, "Failed to register Azure Key Vault");

        // Set a secret
        bool setSecretResult = await _secretsManager.SetSecretAsync(secretName, "MySecretValue");
        Assert.IsTrue(setSecretResult, "Failed to set secret");

        // Get the secret
        var secret = await _secretsManager.GetSecretAsync(secretName, asPlainText: true);
        Assert.AreEqual("MySecretValue", secret, "Failed to retrieve secret");

        // Remove the secret
        bool removeSecretResult = await _secretsManager.RemoveSecretAsync(secretName, keyVaultName);
        Assert.IsTrue(removeSecretResult, "Failed to remove secret");


        var information =  await _secretsManager.GetSecretInfoAsync();
        foreach (var secretInformation in information)
        {
            Console.WriteLine(secretInformation.Dump());
        }
    }
}