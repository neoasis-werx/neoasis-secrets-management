namespace SecretsManagement.Abstractions
{
    using System.Collections;
    using System.Net;
    using System.Security;

    public class SecretVariant
    {
        /// <summary>
        /// Initializes a new instance of the SecretVariant class.
        /// </summary>
        /// <param name="secret">The secret.</param>
        /// <param name="secretType">Type of the secret.</param>
        private SecretVariant(object secret, SecretType secretType)
        {
            Secret = secret;
            SecretType = secretType;
        }

        /// <summary>
        /// Initializes a new instance of the SecretVariant class.
        /// </summary>
        /// <param name="secret">The secret.</param>
        public SecretVariant(byte[] secret) : this(secret, SecretType.ByteArray) { }

        /// <summary>
        /// Initializes a new instance of the SecretVariant class.
        /// </summary>
        /// <param name="secret">The secret.</param>
        public SecretVariant(string secret) : this(secret, SecretType.String) { }

        /// <summary>
        /// Initializes a new instance of the SecretVariant class.
        /// </summary>
        /// <param name="secret">The secret.</param>
        public SecretVariant(SecureString secret) : this(secret, SecretType.SecureString) { }

        /// <summary>
        /// Initializes a new instance of the SecretVariant class.
        /// </summary>
        /// <param name="secret">The secret.</param>
        public SecretVariant(NetworkCredential secret) : this(secret, SecretType.NetworkCredential) { }

        /// <summary>
        /// Initializes a new instance of the SecretVariant class.
        /// </summary>
        /// <param name="secret">The secret.</param>
        public SecretVariant(Hashtable secret) : this(secret, SecretType.Hashtable) { }

        /// <summary>
        /// Initializes a new instance of the SecretVariant class.
        /// </summary>
        /// <param name="secret">The secret.</param>
        public SecretVariant(IDictionary<string, object> secret) : this(secret, SecretType.IDictionaryStringObject) { }

        public object Secret { get; }
        public SecretType SecretType { get; }

        public T? TryCast<T>()
        {
            try
            {
                return (T)Secret;
            }
            catch (Exception)
            {
                return default;
            }
        }

        private static T? CastSecret<T>(SecretVariant? source, SecretType expectedType)
        {
            if (source?.Secret == null) return default;
            return source.SecretType == expectedType ? (T)source.Secret : throw new InvalidCastException($"SecretVariant is not a {typeof(T).Name}");
        }

        public static implicit operator SecretVariant(byte[] source) => new SecretVariant(source, SecretType.ByteArray);
        public static explicit operator byte[]?(SecretVariant? source) => CastSecret<byte[]>(source, SecretType.ByteArray);

        public static implicit operator SecretVariant(string source) => new SecretVariant(source, SecretType.String);
        public static explicit operator string?(SecretVariant? source) => CastSecret<string>(source, SecretType.String);

        public static implicit operator SecretVariant(SecureString? source) => new SecretVariant(source, SecretType.SecureString);
        public static explicit operator SecureString?(SecretVariant? source) => CastSecret<SecureString>(source, SecretType.SecureString);

        public static implicit operator SecretVariant(NetworkCredential? source) => new SecretVariant(source, SecretType.NetworkCredential);
        public static explicit operator NetworkCredential?(SecretVariant? source) => CastSecret<NetworkCredential>(source, SecretType.NetworkCredential);

        public static implicit operator SecretVariant(Hashtable source) => new SecretVariant(source, SecretType.Hashtable);
        public static explicit operator Hashtable?(SecretVariant? source) => CastSecret<Hashtable>(source, SecretType.Hashtable);

        public static implicit operator SecretVariant(Dictionary<string, object>? source) => new SecretVariant(source, SecretType.IDictionaryStringObject);
        public static explicit operator Dictionary<string, object>?(SecretVariant? source) => CastSecret<Dictionary<string, object>>(source, SecretType.IDictionaryStringObject);
    }
}
