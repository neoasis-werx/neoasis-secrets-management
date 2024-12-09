namespace SecretsManagement.Abstractions;

/// <summary>
/// Supported secret types
/// </summary>
public enum SecretType
{
    /// <summary>
    /// Specifies the value is of an unknown type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Specifies the value is a <see cref="T:byte[]"/>.
    /// </summary>
    ByteArray,

    /// <summary>
    /// Specifies the value is a <see cref="T:string"/>.
    /// </summary>
    String,

    /// <summary>
    /// Specifies the value is a <see cref="System.Security.SecureString"/>.
    /// </summary>
    SecureString,

    /// <summary>
    /// Specifies the value is a <see cref="System.Net.NetworkCredential"/>.
    /// </summary>
    NetworkCredential,

    /// <summary>
    /// Specifies the value is a <see cref="System.Collections.Hashtable"/>.
    /// </summary>
    Hashtable,

    /// <summary>
    /// Specifies the value is a dictionary with string keys and object values.
    /// </summary>
    IDictionaryStringObject,
}
