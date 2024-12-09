namespace SecretsManagement.Abstractions;

using System.Collections.Immutable;
using System.Net;

/// <summary>
/// Interface for managing secrets and vaults, providing methods to register, retrieve, and manage secrets.
/// </summary>
public interface ISecretsManager
{
    /// <summary>
    /// Finds and returns a secret by name from registered vaults.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="vaultName">Optional name of the registered vault to retrieve the secret from.</param>
    /// <param name="asPlainText">When true, returns the secret as plain text if it is a string or SecureString.</param>
    /// <returns>The secret value.</returns>
    /// <remarks>
    /// This method finds and returns the first secret that matches the provided name.
    /// If a vault name is specified, then only that vault will be searched.
    /// Otherwise, all vaults are searched and the first found result is returned.
    /// If a default vault is specified, then search that vault before any other registered vault.
    /// Secrets that are string or SecureString types return as SecureString objects by default.
    /// Unless the <paramref name="asPlainText"/> parameter is true, in which case the return the secret as a string in plain text.
    /// </remarks>
    Task<object?> GetSecretAsync(string name, string? vaultName = null, bool asPlainText = false);

    /// <summary>
    /// Adds a secret to a SecretManagement registered vault.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="secret">The secret value.</param>
    /// <param name="vaultName">Optional name of the vault to which the secret is added.</param>
    /// <param name="metadata">Optional metadata associated with the secret.</param>
    /// <param name="noClobber">When true, this command will not overwrite an existing secret and emit an error instead.</param>
    /// <returns>A boolean indicating success.</returns>
    /// <remarks>
    /// This method adds a secret value by name to SecretManagement.
    /// If no vault name is specified, then the secret will be added to the default vault.
    /// If an existing secret by the same name exists, it will be overwritten with the new value unless the <paramref name="noClobber"/> parameter is true.
    /// Additional data can be included with the secret through the <paramref name="metadata"/> parameter, if supported by the extension vault.
    /// If the extension vault does not support metadata then an error will be generated and the operation will fail.
    /// Metadata is not required to be stored securely, and should not contain sensitive information.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when noClobber is true and the secret already exists.</exception>
    Task<bool> SetSecretAsync(string name, object secret, string? vaultName = null, IDictionary<string, object>? metadata = null, bool noClobber = false);

    /// <summary>
    /// Removes a secret from a specified registered extension vault.
    /// </summary>
    /// <param name="name">The name of the secret to remove.</param>
    /// <param name="vaultName">The name of the vault from which the secret is to be removed.</param>
    /// <returns>A boolean indicating success.</returns>
    /// <remarks>
    /// This method removes a secret by name from a registered extension vault.
    /// Both the secret name and extension vault name must be provided.
    /// </remarks>
    Task<bool> RemoveSecretAsync(string name, string vaultName);

    /// <summary>
    /// Finds and returns secret metadata information of one or more secrets.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="vaultName">Optional name of the registered vault to retrieve the secret from.</param>
    /// <returns>The secret information.</returns>
    /// <remarks>
    /// This method finds and returns secret metadata for secrets with names that match the provided <paramref name="name"/>.
    /// If no <paramref name="name"/> parameter argument is provided then metadata for all secrets is returned.
    /// The search is performed over all registered vaults, unless a specific vault name is specified.
    /// Secret metadata consists of the secret name, secret type, vault name, and optional additional user data if supported by the extension vault.
    /// </remarks>
    Task<IEnumerable<SecretInformation>> GetSecretInfoAsync(string name, string? vaultName = null);

    /// <summary>
    /// Adds or replaces additional secret metadata to a secret currently stored in a vault.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="metadata">The metadata to associate with the secret.</param>
    /// <param name="vaultName">Optional name of the vault to which the secret is added.</param>
    /// <returns>A boolean indicating success.</returns>
    /// <remarks>
    /// This method adds additional secret metadata to an existing secret.
    /// Metadata support is an optional feature for an extension vault.
    /// An error will be thrown if a vault does not support secret metadata.
    /// Metadata is a Hashtable object containing Name/Value pairs.
    /// The value type is restricted to string, int, or DateTime.
    /// Metadata is not stored securely in a vault and should not contain sensitive information.
    /// </remarks>
    Task<bool> SetSecretInfoAsync(string name, IDictionary<string, object> metadata, string? vaultName = null);

    /// <summary>
    /// Finds and returns registered vault information.
    /// </summary>
    /// <param name="vaultNames">An array of vault names to retrieve information for. If null or empty, retrieves all registered vaults.</param>
    /// <returns>A list of registered vaults.</returns>
    /// <remarks>
    /// This method finds and returns information of registered vaults.
    /// It takes an array of vault name strings, which can contain wildcard characters.
    /// If no <paramref name="vaultNames"/> parameter is specified, all registered vault information is returned.
    /// The registered vault information includes the vault name, vault implementing module name, and optional default parameters.
    /// </remarks>
    Task<IEnumerable<SecretsVaultInfo>> GetSecretVaultsAsync(params string[] vaultNames);

    /// <summary>
    /// Registers a SecretManagement extension vault module for the current user.
    /// </summary>
    /// <param name="moduleName">The name of the module implementing the vault.</param>
    /// <param name="vaultName">The name of the vault.</param>
    /// <param name="vaultParameters">The optional parameters associated with the vault.</param>
    /// <param name="isDefaultVault">A value indicating whether this vault is the default vault.</param>
    /// <param name="allowClobber">When true, this command will overwrite an existing vault with the same name.</param>
    /// <param name="description">A description string that is included in the vault registry information.</param>
    /// <returns>A boolean indicating success.</returns>
    /// <remarks>
    /// This method adds a provided SecretManagement extension vault module to the current user vault registry.
    /// An extension vault module is a PowerShell module that conforms to the required extension vault format.
    /// This method will first verify that the provided module meets conformance requirements, and then add it to the extension vault registry.
    /// Extension vaults are registered to the current user and do not affect other user vault registrations.
    /// </remarks>
    Task<bool> RegisterSecretVaultAsync(string moduleName, string? vaultName = null, IDictionary<string, object>? vaultParameters = null, bool isDefaultVault = false, bool allowClobber = false, string? description = null);

    /// <summary>
    /// Registers a SecretManagement extension vault module for the current user using a SecretsVaultInfo object.
    /// </summary>
    /// <param name="vaultInfo">The SecretsVaultInfo object containing vault registration details.</param>
    /// <param name="allowClobber">When true, this command will overwrite an existing vault with the same name.</param>
    /// <returns>A boolean indicating success.</returns>
    /// <remarks>
    /// This method adds a provided SecretManagement extension vault module to the current user vault registry using a SecretsVaultInfo object.
    /// It delegates to the other RegisterSecretVaultAsync method.
    /// </remarks>
    async Task<bool> RegisterSecretVaultAsync(SecretsVaultInfo vaultInfo, bool allowClobber = false)
    {
        return await RegisterSecretVaultAsync(
            vaultInfo.ModuleName,
            vaultInfo.VaultName,
            vaultInfo.VaultParameters.ToImmutableDictionary(),
            vaultInfo.IsDefaultVault,
            allowClobber: allowClobber,
            vaultInfo.Description
        );
    }

    /// <summary>
    /// Un-registers an extension vault from SecretManagement for the current user.
    /// </summary>
    /// <param name="vaultName">The name of the vault to un-register.</param>
    /// <returns>A boolean indicating success.</returns>
    /// <remarks>
    /// This method un-registers the specified extension vault.
    /// Once un-registered, the vault is no longer available to SecretManagement for the current user.
    /// </remarks>
    Task<bool> UnregisterSecretVaultAsync(string vaultName);

    /// <summary>
    /// Sets the provided vault name as the default vault for the current user.
    /// </summary>
    /// <param name="vaultName">The name of the vault to set as default.</param>
    /// <returns>A boolean indicating success.</returns>
    /// <remarks>
    /// This method updates the vault registry to designate the provided vault name as the default vault.
    /// Only one registered vault can be the default vault.
    /// If this method is run without specifying the <paramref name="vaultName"/> parameter, then no registered vault is designated as the default vault.
    /// </remarks>
    Task<bool> SetSecretVaultDefaultAsync(string vaultName);

    /// <summary>
    /// Runs an extension vault self test.
    /// </summary>
    /// <param name="vaultNames">An array of vault names to test. If null or empty, tests all registered vaults.</param>
    /// <returns>A boolean indicating success.</returns>
    /// <remarks>
    /// This method runs an extension vault self test by running the internal vault 'Test-SecretVault' command.
    /// It will return true if all tests succeeded, and false otherwise.
    /// Information on failing tests will be written to the error stream as error records.
    /// </remarks>
    Task<bool> TestSecretVaultAsync(params string[] vaultNames);

    /// <summary>
    /// Unlocks an extension vault so that it can be accessed in the current session.
    /// </summary>
    /// <param name="vaultName">The name of the vault to unlock.</param>
    /// <param name="credential">The credential to unlock the vault.</param>
    /// <returns>A boolean indicating success.</returns>
    /// <remarks>
    /// This method unlocks an extension vault using the provided credential parameter argument.
    /// This allows a vault that requires password authentication to operate without first having to prompt the user.
    /// Not all extension vaults require password authentication, in which case this method has no effect.
    /// A warning will be emitted if the extension vault does not support unlocking via password.
    /// </remarks>
    Task<bool> UnlockSecretVaultAsync(string vaultName, NetworkCredential credential);
}
