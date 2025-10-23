namespace SecretsManagement.AzSecretsVault;

using System.Collections.ObjectModel;
using System.Net;
using Abstractions;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

public class AzureKeyVaultSecretsVault : AbstractSecretsVault<AzureKeyVaultSecretsVault>
{
    private readonly SecretClient _secretClient;

    public AzureKeyVaultSecretsVault(IDictionary<string, object> vaultParameters) : base(vaultParameters)
    {
        if (vaultParameters == null) throw new ArgumentNullException(nameof(vaultParameters));
        if (!vaultParameters.TryGetValue("vaultUri", out var vaultUriObj) || vaultUriObj is not string vaultUri)
            throw new ArgumentException("The vaultParameters dictionary must contain a 'vaultUri' key with a string value.");

        _secretClient = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
    }

    public override async Task<bool> SetSecretAsync(string name, object secret)
    {
        try
        {
            await _secretClient.SetSecretAsync(name, secret.ToString());
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public override async Task<object?> GetSecretAsync(string name)
    {
        try
        {
            var secret = await _secretClient.GetSecretAsync(name);
            return secret.Value.Value;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override async Task<bool> RemoveSecretAsync(string name)
    {
        try
        {
            await _secretClient.StartDeleteSecretAsync(name);
            var r = _secretClient.PurgeDeletedSecretAsync(name);

            return r.Result.Status == 200;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public override async Task<IEnumerable<SecretInformation>> GetSecretInfoAsync(string? name = null)
    {
        try
        {
            var secrets = _secretClient.GetPropertiesOfSecretsAsync();
            var secretInfos = new List<SecretInformation>();

            await foreach (var secret in secrets)
            {
                if (name == null || secret.Name == name)
                {

                    var metadata = new Dictionary<string, object>()
                    {
                        ["Id"] = secret.Id,
                        ["ContentType"] = secret.ContentType,
                        ["CreatedOn"] = secret.CreatedOn!,
                        ["Enabled"] = secret.Enabled!,
                        ["KeyId"] = secret.KeyId,
                        ["Managed"] = secret.Managed,
                        ["Name"] = secret.Name,
                        ["NotBefore"] = secret.NotBefore!,
                        ["RecoverableDays"] = secret.RecoverableDays!,
                        ["RecoveryLevel"] = secret.RecoveryLevel!,
                        ["Tags"] = secret.Tags,
                        ["UpdatedOn"] = secret.UpdatedOn!,
                        ["VaultUri"] = secret.VaultUri,
                        ["Version"] = secret.Version,
                    };

                    var secretInfo = new SecretInformation(
                        secret.Name,
                        SecretType.String,
                        VaultName,
                        new ReadOnlyDictionary<string, object>(metadata)
                    );
                    secretInfos.Add(secretInfo);
                }
            }

            return secretInfos;
        }
        catch (Exception)
        {
            return [];
        }
    }

    public override async Task<bool> SetSecretInfoAsync(string name, IDictionary<string, object> secretMetadata)
    {
        // Azure Key Vault does not support storing additional metadata directly with secrets.
        return await Task.FromResult(false);
    }

    public override async Task<bool> TestVaultAsync()
    {
        try
        {
            // Simple test to check if Azure Key Vault is accessible
            await _secretClient.GetPropertiesOfSecretsAsync().GetAsyncEnumerator().MoveNextAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public override async Task<bool> UnlockVaultAsync(NetworkCredential credential)
    {
        // Azure Key Vault uses Azure AD for authentication, so this method is not applicable.
        return await Task.FromResult(true);
    }

    public override async Task<bool> UnregisterSecretVaultAsync()
    {
        // No specific cleanup required for Azure Key Vault
        return await Task.FromResult(true);
    }
}