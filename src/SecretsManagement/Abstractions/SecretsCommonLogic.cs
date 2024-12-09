namespace SecretsManagement.Abstractions;
public static class SecretsCommonLogic
{
    public static SecretType GetSecretType(object secret)
    {
        return secret switch
        {
            byte[] => SecretType.ByteArray,
            string => SecretType.String,
            System.Security.SecureString => SecretType.SecureString,
            System.Net.NetworkCredential => SecretType.NetworkCredential,
            System.Collections.Hashtable => SecretType.Hashtable,
            Dictionary<string, object> => SecretType.IDictionaryStringObject,
            _ => SecretType.Unknown
        };
    }

    public static T? IfPresent<T>(this T? source, Action<T?> actionIfNotNull)
    {
        if (source is not null)
        {
            actionIfNotNull(source);
        }
        return source;
    }
}

