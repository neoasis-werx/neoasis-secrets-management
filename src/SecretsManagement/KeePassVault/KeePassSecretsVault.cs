namespace SecretsManagement.KeePassVault;

using Abstractions;

using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;

using System.Collections.ObjectModel;
using System.Net;

public class KeePassSecretsVault : AbstractSecretsVault<KeePassSecretsVault>
{

    private readonly string _databasePath;
    private readonly string _masterPassword;
    private readonly PwDatabase _database;

    public KeePassSecretsVault(IDictionary<string, object> vaultParameters) : base(vaultParameters)
    {
        if (vaultParameters == null) throw new ArgumentNullException(nameof(vaultParameters));
        if (!vaultParameters.TryGetValue("databasePath", out var databasePathObj) || databasePathObj is not string databasePath)
            throw new ArgumentException("The vaultParameters dictionary must contain a 'databasePath' key with a string value.");
        if (!vaultParameters.TryGetValue("masterPassword", out var masterPasswordObj) || masterPasswordObj is not string masterPassword)
            throw new ArgumentException("The vaultParameters dictionary must contain a 'masterPassword' key with a string value.");

        _databasePath = databasePath;
        _masterPassword = masterPassword;
        _database = new PwDatabase();
    }

    private async Task<bool> OpenDatabaseAsync()
    {
        try
        {
            var ioConnectionInfo = new IOConnectionInfo { Path = _databasePath };
            var compositeKey = new CompositeKey();
            compositeKey.AddUserKey(new KcpPassword(_masterPassword));

            if (File.Exists(_databasePath))
            {
                _database.Open(ioConnectionInfo, compositeKey, null);
            }
            else
            {
                // Create a new database
                _database.New(ioConnectionInfo, compositeKey);
                _database.MasterKey = compositeKey;
                _database.Save(null);
            }

            return await Task.FromResult(true);
        }
        catch (Exception)
        {
            return await Task.FromResult(false);
        }
    }

    private async Task<bool> SaveDatabaseAsync()
    {
        try
        {
            _database.Save(null);
            return await Task.FromResult(true);
        }
        catch (Exception)
        {
            return await Task.FromResult(false);
        }
    }



    public override async Task<bool> SetSecretAsync(string name, object secret)
    {
        if (!await OpenDatabaseAsync())
        {
            return false;
        }

        try
        {
            var entry = new PwEntry(true, true);
            entry.Strings.Set(PwDefs.TitleField, new ProtectedString(true, name));
            entry.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, secret.ToString()));
            _database.RootGroup.AddEntry(entry, true);
            return await SaveDatabaseAsync();
        }
        catch (Exception)
        {
            return false;
        }
    }

    public override async Task<object?> GetSecretAsync(string name)
    {
        if (!await OpenDatabaseAsync())
        {
            return null;
        }

        try
        {
            var entry = _database.RootGroup.Entries.FirstOrDefault(e => e.Strings.ReadSafe(PwDefs.TitleField) == name);
            return await Task.FromResult(entry?.Strings.ReadSafe(PwDefs.PasswordField));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override async Task<bool> RemoveSecretAsync(string name)
    {
        if (!await OpenDatabaseAsync())
        {
            return false;
        }

        try
        {
            var entry = _database.RootGroup.Entries.FirstOrDefault(e => e.Strings.ReadSafe(PwDefs.TitleField) == name);
            if (entry != null)
            {
                _database.RootGroup.Entries.Remove(entry);
                return await SaveDatabaseAsync();
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public override async Task<IEnumerable<SecretInformation>> GetSecretInfoAsync(string? name = null)
    {
        if (!await OpenDatabaseAsync())
        {
            return [];
        }

        try
        {
            var entry = _database.RootGroup.Entries.FirstOrDefault(e => e.Strings.ReadSafe(PwDefs.TitleField) == name);
            if (entry == null)
            {
                return [];
            }

            if (name != null)
            {
                var secretInfo = new SecretInformation(
                    name,
                    SecretType.String,
                    "KeePassVault",
                    new ReadOnlyDictionary<string, object>(new Dictionary<string, object>())
                );

                return await Task.FromResult(new List<SecretInformation> { secretInfo });
            }
        }
        catch (Exception)
        {
            return [];
        }

        return [];
    }

    public override async Task<bool> SetSecretInfoAsync(string name, IDictionary<string, object> secretMetadata)
    {
        // KeePass does not support storing metadata, so this method is not applicable.
        return await Task.FromResult(false);
    }

    public override async Task<bool> TestVaultAsync()
    {
        // Simple test to check if KeePass database is accessible
        return await OpenDatabaseAsync();
    }

    public override async Task<bool> UnlockVaultAsync(NetworkCredential credential)
    {
        // KeePass requires a master password, so this method is not applicable.
        return await Task.FromResult(true);
    }

    public override async Task<bool> UnregisterSecretVaultAsync()
    {
        // No specific cleanup required for KeePass
        return await Task.FromResult(true);
    }
}