namespace SecretsManagement.Abstractions;

using System.Collections.ObjectModel;

/// <summary>
/// Represents information about a registered secret vault.
/// </summary>
public class SecretsVaultInfo
{
    /// <summary>
    /// Gets the name of the vault.
    /// </summary>
    public string VaultName { get; }

    /// <summary>
    /// Gets the name of the module implementing the vault.
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    /// Gets a value indicating whether this vault is the default vault.
    /// </summary>
    public bool IsDefaultVault { get; }

    /// <summary>
    /// Gets the optional parameters associated with the vault.
    /// </summary>
    public IReadOnlyDictionary<string, object> VaultParameters { get; }

    /// <summary>
    /// Gets the description of the vault.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretsVaultInfo"/> class.
    /// </summary>
    /// <param name="vaultName">The name of the vault.</param>
    /// <param name="moduleName">The name of the module implementing the vault.</param>
    /// <param name="isDefaultVault">A value indicating whether this vault is the default vault.</param>
    /// <param name="vaultParameters">The optional parameters associated with the vault.</param>
    /// <param name="description">A description string that is included in the vault registry information.</param>
    public SecretsVaultInfo(string vaultName, string moduleName, bool isDefaultVault, IDictionary<string, object>? vaultParameters = null, string? description = null)
    {
        VaultName = vaultName;
        ModuleName = moduleName;
        IsDefaultVault = isDefaultVault;
        VaultParameters = vaultParameters != null ? new ReadOnlyDictionary<string, object>(vaultParameters) : new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
        Description = description;
    }
}