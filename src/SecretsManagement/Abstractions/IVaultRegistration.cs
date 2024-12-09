namespace SecretsManagement.Abstractions;

using System.Security;

/// <summary>
/// Interface for managing vault registration and related operations.
/// </summary>
public interface IVaultRegistration
{
    /// <summary>
    /// Registers a single extension vault to the current user.
    /// </summary>
    /// <param name="moduleName">The name of the module implementing the vault.</param>
    /// <param name="vaultName">The name of the vault.</param>
    /// <param name="vaultParameters">The optional parameters associated with the vault.</param>
    /// <param name="isDefaultVault">A value indicating whether this vault is the default vault.</param>
    /// <param name="allowClobber">When true, this command will overwrite an existing vault with the same name.</param>
    /// <param name="description">A description string that is included in the vault registry information.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> RegisterSecretVaultAsync(string moduleName, string? vaultName = null, IDictionary<string, object>? vaultParameters = null, bool isDefaultVault = false, bool allowClobber = false, string? description = null);

    /// <summary>
    /// Retrieves information about one or more registered extension vaults.
    /// </summary>
    /// <param name="vaultNames">An array of vault names to retrieve information for. If null or empty, retrieves all registered vaults.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of registered vaults.</returns>
    Task<IList<SecretsVaultInfo>> GetSecretVaultsAsync(params string[] vaultNames);

    /// <summary>
    /// Unregisters a single extension vault.
    /// </summary>
    /// <param name="vaultName">The name of the vault to unregister.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> UnregisterSecretVaultAsync(string vaultName);

    /// <summary>
    /// Sets one registered extension vault as the default vault.
    /// </summary>
    /// <param name="vaultName">The name of the vault to set as default.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> SetSecretVaultDefaultAsync(string vaultName);

    /// <summary>
    /// Runs the Test-SecretVault function provided by the extension vault.
    /// </summary>
    /// <param name="vaultNames">An array of vault names to test. If null or empty, tests all registered vaults.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> TestSecretVaultAsync(params string[] vaultNames);

    /// <summary>
    /// Unlocks the extension vault through a passed in SecureString password.
    /// </summary>
    /// <param name="vaultName">The name of the vault to unlock.</param>
    /// <param name="password">The password to unlock the vault.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> UnlockSecretVaultAsync(string vaultName, SecureString password);
}