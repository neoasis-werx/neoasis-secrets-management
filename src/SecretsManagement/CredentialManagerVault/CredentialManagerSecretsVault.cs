namespace SecretsManagement.CredentialManagerVault;

using Abstractions;

using AdysTech.CredentialManager;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;

public class CredentialManagerSecretsVault : AbstractSecretsVault<CredentialManagerSecretsVault>
{
    public CredentialManagerSecretsVault(IDictionary<string, object> vaultParameters) : base(vaultParameters)
    {
    }

    public override async Task<bool> SetSecretAsync(string name, object secret)
    {
        try
        {
            var secretType = SecretsCommonLogic.GetSecretType(secret);
            if (secretType != SecretType.NetworkCredential)
            {
                throw new ArgumentException($"Unsupported secret type {secretType}");
            }

            var credential = (NetworkCredential)secret;
            return await Task.FromResult(CredentialManager.SaveCredentials(name, credential) is not null);
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
            var credential = CredentialManager.GetCredentials(name);
            return await Task.FromResult(credential);
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
            return await Task.FromResult(CredentialManager.RemoveCredentials(name));
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
            var creds = CredentialManager.EnumerateICredentials(name);

            var results = new List<SecretInformation>();


            foreach (var cred in creds)
            {
                var metaData = new Dictionary<string, object>()
                {
                    ["Comment"] = cred.Comment,
                    ["LastWritten"] = cred.LastWritten,
                    ["CredentialType"] = cred.Type,
                    ["UserName"] = cred.UserName,
                };
                var secretInfo = new SecretInformation(
                    cred.TargetName,
                    SecretType.NetworkCredential,
                    VaultName,
                    new ReadOnlyDictionary<string, object>(metaData)
                );
                results.Add(secretInfo);
            }

            return await Task.FromResult(results);
        }
        catch (Exception)
        {
            return [];
        }
    }

    public override async Task<bool> SetSecretInfoAsync(string name, IDictionary<string, object> secretMetadata)
    {
        // Credential Manager does not support storing metadata, so this method is not applicable.
        return await Task.FromResult(false);
    }

    public override async Task<bool> TestVaultAsync()
    {
        // Simple test to check if Credential Manager is accessible
        try
        {
            var testCredential = new NetworkCredential("TestVault", "TestPassword");
            var saveResult = CredentialManager.SaveCredentials("TestVault", testCredential);
            var removeResult = CredentialManager.RemoveCredentials("TestVault");
            return await Task.FromResult(saveResult is not null && removeResult);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public override async Task<bool> UnlockVaultAsync(NetworkCredential credential)
    {
        // Credential Manager does not require unlocking, so this method is not applicable.
        return await Task.FromResult(true);
    }

    public override async Task<bool> UnregisterSecretVaultAsync()
    {
        // No specific cleanup required for Credential Manager
        return await Task.FromResult(true);
    }

}