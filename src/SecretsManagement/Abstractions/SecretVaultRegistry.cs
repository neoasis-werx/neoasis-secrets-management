namespace SecretsManagement.Abstractions
{
    using System.Collections.Concurrent;

    public class SecretVaultRegistry
    {
        private readonly ConcurrentDictionary<string, SecretsVaultInfo> _vaultInfos = new();
        private readonly ConcurrentDictionary<string, ISecretsVault> _vaults = new();

        public bool TryGetValue(string vaultName, out ISecretsVault? vault)
        {
            return _vaults.TryGetValue(vaultName, out vault);
        }

        public bool TryGetVaultInfo(string vaultName, out SecretsVaultInfo? vaultInfo)
        {
            return _vaultInfos.TryGetValue(vaultName, out vaultInfo);
        }

        public bool RegisterVault(string vaultName, ISecretsVault vault, SecretsVaultInfo vaultInfo)
        {
            if (_vaults.ContainsKey(vaultName))
            {
                return false;
            }

            _vaults[vaultName] = vault;
            _vaultInfos[vaultName] = vaultInfo;
            return true;
        }

        public bool UnregisterVault(string vaultName)
        {
            if (_vaults.TryRemove(vaultName, out _) && _vaultInfos.TryRemove(vaultName, out _))
            {
                return true;
            }

            return false;
        }

        public IEnumerable<SecretsVaultInfo> GetAllVaultInfos()
        {
            return _vaultInfos.Values;
        }
    }
}