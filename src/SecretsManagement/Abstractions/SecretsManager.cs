namespace SecretsManagement.Abstractions;

using System.Collections.Immutable;
using System.Net;
using System.Security;

/// <summary>
/// Manages secrets and vaults, providing methods to register, retrieve, and manage secrets.
/// </summary>
public class SecretsManager : ISecretsManager
{
    private readonly SecretVaultRegistry _vaultRegistry = new();
    private string? _defaultVaultName;

    public async Task<object?> GetSecretAsync(string name, string? vaultName = null, bool asPlainText = false)
    {
        if (vaultName != null)
        {
            if (_vaultRegistry.TryGetValue(vaultName, out var vault))
            {
                var secret = await vault.GetSecretAsync(name);
                return asPlainText && secret is SecureString secureString ? new NetworkCredential(string.Empty, secureString).Password : secret;
            }
        }
        else
        {
            foreach (var vault in _vaultRegistry.GetAllVaultInfos())
            {
                if (_vaultRegistry.TryGetValue(vault.VaultName, out var vaultInstance))
                {
                    var secret = await vaultInstance.GetSecretAsync(name);
                    if (secret != null)
                    {
                        return asPlainText && secret is SecureString secureString ? new NetworkCredential(string.Empty, secureString).Password : secret;
                    }
                }
            }
        }
        return null;
    }

    public async Task<bool> SetSecretAsync(string name, object secret, string? vaultName = null, IDictionary<string, object>? metadata = null, bool noClobber = false)
    {
        vaultName ??= _defaultVaultName ?? throw new ArgumentException("vaultName was null and no default vault was set");

        if (_vaultRegistry.TryGetValue(vaultName, out var vault))
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

    public async Task<bool> RemoveSecretAsync(string name, string vaultName)
    {
        if (_vaultRegistry.TryGetValue(vaultName, out var vault))
        {
            return await vault.RemoveSecretAsync(name);
        }
        return false;
    }

    public async Task<IEnumerable<SecretInformation>> GetSecretInfoAsync(string? name = null, string? vaultName = null)
    {
        if (vaultName != null)
        {
            if (_vaultRegistry.TryGetValue(vaultName, out var vault))
            {
                return await vault.GetSecretInfoAsync(name);
            }
        }
        else
        {
            var infos = new List<SecretInformation>();
            foreach (var vault in _vaultRegistry.GetAllVaultInfos())
            {
                if (_vaultRegistry.TryGetValue(vault.VaultName, out var vaultInstance))
                {
                    var secretInfo = await vaultInstance.GetSecretInfoAsync(name);
                    infos.AddRange(secretInfo);
                }
            }

            return infos;
        }

        return [];
    }

    public async Task<bool> SetSecretInfoAsync(string name, IDictionary<string, object>? metadata, string? vaultName = null)
    {
        vaultName ??= _defaultVaultName ?? throw new ArgumentException("vaultName was null and no default vault was set");

        if (_vaultRegistry.TryGetValue(vaultName, out var vault))
        {
            return metadata != null && await vault.SetSecretInfoAsync(name, metadata);
        }
        return false;
    }

    public async Task<IEnumerable<SecretsVaultInfo>> GetSecretVaultsAsync(params string[] vaultNames)
    {
        var result = new List<SecretsVaultInfo>();

        foreach (var vaultInfo in _vaultRegistry.GetAllVaultInfos())
        {
            if (vaultNames == null || vaultNames.Contains(vaultInfo.VaultName))
            {
                result.Add(vaultInfo);
            }
        }

        return await Task.FromResult(result);
    }

    public async Task<bool> RegisterSecretVaultAsync(string moduleName, string? vaultName = null, IDictionary<string, object>? vaultParameters = null, bool isDefaultVault = false, bool allowClobber = false, string? description = null)
    {
        vaultName ??= moduleName;
        if (!allowClobber && _vaultRegistry.TryGetValue(vaultName, out _))
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

        var vaultInfo = new SecretsVaultInfo(vaultName, moduleName, isDefaultVault, vaultParameters, description);
        if (!_vaultRegistry.RegisterVault(vaultName, vault, vaultInfo))
        {
            return false;
        }

        if (isDefaultVault)
        {
            _defaultVaultName = vaultName;
        }

        return true;
    }

    public async Task<bool> UnregisterSecretVaultAsync(string vaultName)
    {
        if (_vaultRegistry.UnregisterVault(vaultName))
        {
            if (vaultName == _defaultVaultName)
            {
                _defaultVaultName = null;
            }
            return true;
        }
        return false;
    }

    public async Task<bool> SetSecretVaultDefaultAsync(string vaultName)
    {
        if (_vaultRegistry.TryGetValue(vaultName, out _))
        {
            _defaultVaultName = vaultName;
            return await Task.FromResult(true);
        }
        return await Task.FromResult(false);
    }

    public async Task<bool> TestSecretVaultAsync(params string[] vaultNames)
    {
        var result = true;

        foreach (var vaultInfo in _vaultRegistry.GetAllVaultInfos())
        {
            if (vaultNames == null || vaultNames.Contains(vaultInfo.VaultName))
            {
                if (_vaultRegistry.TryGetValue(vaultInfo.VaultName, out var vault))
                {
                    result &= await vault.TestVaultAsync();
                }
            }
        }

        return await Task.FromResult(result);
    }

    public async Task<bool> UnlockSecretVaultAsync(string vaultName, NetworkCredential credential)
    {
        if (_vaultRegistry.TryGetValue(vaultName, out var vault))
        {
            return await vault.UnlockVaultAsync(credential);
        }
        return false;
    }
}
