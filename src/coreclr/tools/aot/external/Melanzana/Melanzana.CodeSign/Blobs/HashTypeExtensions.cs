using System.Security.Cryptography;

namespace Melanzana.CodeSign.Blobs
{
    public static class HashTypeExtensions
    {

        public static IncrementalHash GetIncrementalHash(this HashType hashType)
        {
            return hashType switch
            {
                HashType.SHA1 => IncrementalHash.CreateHash(HashAlgorithmName.SHA1),
                HashType.SHA256 => IncrementalHash.CreateHash(HashAlgorithmName.SHA256),
                HashType.SHA256Truncated => IncrementalHash.CreateHash(HashAlgorithmName.SHA256),
                HashType.SHA384 => IncrementalHash.CreateHash(HashAlgorithmName.SHA384),
                HashType.SHA512 => IncrementalHash.CreateHash(HashAlgorithmName.SHA512),
                _ => throw new NotSupportedException()
            };
        }

        public static byte GetSize(this HashType hashType)
        {
            return hashType switch
            {
                HashType.SHA1 => 20,
                HashType.SHA256 => 32,
                HashType.SHA256Truncated => 20,
                HashType.SHA384 => 48,
                HashType.SHA512 => 64,
                _ => throw new NotSupportedException()
            };
        }

        public static string GetOid(this HashType hashType)
        {
            return hashType switch
            {
                HashType.SHA1 => "1.3.14.3.2.26",
                HashType.SHA256 => "2.16.840.1.101.3.4.2.1",
                HashType.SHA256Truncated => "2.16.840.1.101.3.4.2.1",
                HashType.SHA384 => "2.16.840.1.101.3.4.2.2",
                HashType.SHA512 => "2.16.840.1.101.3.4.2.3",
                _ => throw new NotSupportedException()
            };
        }
    }
}
