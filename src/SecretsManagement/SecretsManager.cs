namespace SecretsManagement;

using Abstractions;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using System.Security;

/// <summary>
/// Manages secrets and vaults, providing methods to register, retrieve, and manage secrets.
/// </summary>
public class SecretsManager : ISecretsManager
{
    private readonly ConcurrentDictionary<string, ISecretsVault> _vaults = new();
    private string? _defaultVaultName;

    /// <inheritdoc />
    public async Task<object?> GetSecretAsync(string name, string? vaultName = null, bool asPlainText = false)
    {
        if (vaultName != null)
        {
            if (_vaults.TryGetValue(vaultName, out var vault))
            {
                var secret = await vault.GetSecretAsync(name);
                return asPlainText && secret is SecureString secureString ? new NetworkCredential(string.Empty, secureString).Password : secret;
            }
        }
        else
        {
            foreach (var vault in _vaults.Values)
            {
                var secret = await vault.GetSecretAsync(name);
                if (secret != null)
                {
                    return asPlainText && secret is SecureString secureString ? new NetworkCredential(string.Empty, secureString).Password : secret;
                }
            }
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<bool> SetSecretAsync(string name, object secret, string? vaultName = null, IDictionary<string, object>? metadata = null, bool noClobber = false)
    {
        if (vaultName == null)
        {
            vaultName = _defaultVaultName  ?? throw new ArgumentException("vaultName was null and no default vault was set");
        }

        if (_vaults.TryGetValue(vaultName, out var vault))
        {
            if (noClobber)
            {
                var existingSecret = await vault.GetSecretAsync(name);
                if (existingSecret != null)
                {
                    throw new InvalidOperationException($"The secret '{name}' already exists in the vault '{vaultName}'.");
                }
            }

            var success = await vault.SetSecretAsync(name, secret);
            if (success && metadata != null)
            {
                await vault.SetSecretInfoAsync(name, metadata.ToImmutableDictionary());
            }
            return success;
        }
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveSecretAsync(string name, string vaultName)
    {
        if (_vaults.TryGetValue(vaultName, out var vault))
        {
            return await vault.RemoveSecretAsync(name);
        }
        return false;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SecretInformation>> GetSecretInfoAsync(string? name = null, string? vaultName = null)
    {
        if (vaultName != null)
        {
            if (_vaults.TryGetValue(vaultName, out var vault))
            {
                return await vault.GetSecretInfoAsync(name);
            }
        }
        else
        {
            var infos = new List<SecretInformation>();
            foreach (var vault in _vaults.Values)
            {
                var secretInfo = await vault.GetSecretInfoAsync(name);
                infos.AddRange(secretInfo);
            }

            return infos;
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<bool> SetSecretInfoAsync(string name, IDictionary<string, object>? metadata, string? vaultName = null)
    {
        vaultName ??= _defaultVaultName ?? throw new ArgumentException("vaultName was null and no default vault was set");

        if (_vaults.TryGetValue(vaultName, out var vault))
        {
            return metadata != null && await vault.SetSecretInfoAsync(name, metadata);
        }
        return false;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SecretsVaultInfo>> GetSecretVaultsAsync(params string[] vaultNames)
    {
        var result = new List<SecretsVaultInfo>();

        foreach (var kvp in _vaults)
        {
            if (vaultNames == null || vaultNames.Contains(kvp.Key))
            {
                var vaultInfo = new SecretsVaultInfo(kvp.Key, kvp.Value.GetType().Name, kvp.Key == _defaultVaultName);
                result.Add(vaultInfo);
            }
        }

        return await Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<bool> RegisterSecretVaultAsync(string moduleName, string? vaultName = null, IDictionary<string, object>? vaultParameters = null, bool isDefaultVault = false, bool allowClobber = false, string? description = null)
    {
        vaultName ??= moduleName;
        if (!allowClobber && _vaults.ContainsKey(vaultName))
        {
            return await Task.FromResult(false);
        }

        var type = Type.GetType(moduleName);
        if (type == null)
        {
            return false;
        }

        vaultParameters ??= new Dictionary<string, object>();
        if (Activator.CreateInstance(type, vaultParameters) is not ISecretsVault vault)
        {
            return false;
        }

        _vaults[vaultName] = vault;
        if (isDefaultVault)
        {
            _defaultVaultName = vaultName;
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> UnregisterSecretVaultAsync(string vaultName)
    {
        if (_vaults.TryRemove(vaultName, out var vault))
        {
            await vault.UnregisterSecretVaultAsync();
            if (vaultName == _defaultVaultName)
            {
                _defaultVaultName = null;
            }
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> SetSecretVaultDefaultAsync(string vaultName)
    {
        if (_vaults.ContainsKey(vaultName))
        {
            _defaultVaultName = vaultName;
            return await Task.FromResult(true);
        }
        return await Task.FromResult(false);
    }

    /// <inheritdoc />
    public async Task<bool> TestSecretVaultAsync(params string[] vaultNames)
    {
        var result = true;

        foreach (var kvp in _vaults)
        {
            if (vaultNames == null || vaultNames.Contains(kvp.Key))
            {
                result &= await kvp.Value.TestVaultAsync();
            }
        }

        return await Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<bool> UnlockSecretVaultAsync(string vaultName, NetworkCredential credential)
    {
        if (_vaults.TryGetValue(vaultName, out var vault))
        {
            return await vault.UnlockVaultAsync(credential);
        }
        return false;
    }
}