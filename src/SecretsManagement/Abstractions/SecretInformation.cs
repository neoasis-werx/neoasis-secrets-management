namespace SecretsManagement.Abstractions;

using System.Collections;
using System.Collections.ObjectModel;

/// <summary>
///     Represents a secret from a secret vault.
/// </summary>
public sealed class SecretInformation
{
    #region Properties

    /// <summary>
    ///     Gets the name of the secret.
    /// </summary>
    public string Name { get; }


    /// <summary>
    ///     Gets the object type of the secret.
    /// </summary>
    public SecretType Type { get; }

    /// <summary>
    ///     Gets the vault name where the secret resides.
    /// </summary>
    public string VaultName { get; }

    /// <summary>
    ///     Gets metadata of the secret.
    /// </summary>
    public ReadOnlyDictionary<string, object> Metadata { get; }

    #endregion

    #region Constructor

    /// <summary>
    ///     Constructor
    /// </summary>
    public SecretInformation(
        string name,
        SecretType type,
        string vaultName)
    {
        Name = name;
        Type = type;
        VaultName = vaultName;
        Metadata = ReadOnlyDictionary<string, object>.Empty;
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    public SecretInformation(
        string name,
        SecretType type,
        string vaultName,
        ReadOnlyDictionary<string, object> metadata) : this(name, type, vaultName)
    {
        Metadata = metadata;
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    public SecretInformation(
        string name,
        SecretType type,
        string vaultName,
        Hashtable? metadata) : this(name, type, vaultName)
    {
        if (metadata is null)
        {
            return;
        }

        var metaDictionary = new Dictionary<string, object>(metadata.Count);
        foreach (var key in metadata.Keys)
        {
            var value = metadata[key];
            if (value != null) metaDictionary.Add((string)key, value);
        }

        Metadata = new ReadOnlyDictionary<string, object>(metaDictionary);
    }

    #endregion
}