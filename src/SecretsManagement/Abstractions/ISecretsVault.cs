namespace SecretsManagement.Abstractions;

using System.Net;

/// <summary>
/// Interface for managing secrets in a vault.
/// </summary>
public interface ISecretsVault
{
    public static string DefaultVaultName => nameof(ISecretsVault);

    public string VaultName { get; set; }


    /// <summary>
    /// Adds a secret to a specific extension vault, or to the default vault if no vault is specified.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="secret">The secret value.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> SetSecretAsync(string name, object secret);

    /// <summary>
    /// Retrieves a secret from a specific extension vault, or first found over all vaults.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the secret value.</returns>
    Task<object?> GetSecretAsync(string name);

    /// <summary>
    /// Removes a secret from a specific vault.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> RemoveSecretAsync(string name);

    /// <summary>
    /// Retrieves information about one or more secrets, but not the secret itself.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the secret information.</returns>
    Task<IEnumerable<SecretInformation>> GetSecretInfoAsync(string? name = null);

    /// <summary>
    /// Adds or replaces additional secret metadata to an existing secret in the vault.
    /// Metadata is not stored securely.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="secretMetadata">The metadata to associate with the secret.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> SetSecretInfoAsync(string name, IDictionary<string, object> secretMetadata);

    /// <summary>
    /// Runs the Test-SecretVault function provided by the extension vault.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> TestVaultAsync();

    /// <summary>
    /// Unlocks the extension vault through a passed in NetworkCredential.
    /// </summary>
    /// <param name="credential">The credentials to unlock the vault.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> UnlockVaultAsync(NetworkCredential credential);

    /// <summary>
    /// Called before the vault is unregistered to allow it to perform any needed cleanup work.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> UnregisterSecretVaultAsync();
}