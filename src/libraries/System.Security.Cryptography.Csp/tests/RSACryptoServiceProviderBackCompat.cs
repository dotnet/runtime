using System.Collections.Generic;
using System.Security.Cryptography.Rsa.Tests;
using Xunit;

namespace System.Security.Cryptography.Csp.Tests
{
    public class RSACryptoServiceProviderBackCompat
    {
        [Theory]
        [MemberData("AlgorithmIdentifiers")]
        public static void AlgorithmLookups(string primaryId, object halg)
        {
            byte[] data = { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(TestData.RSA2048Params);

                byte[] primary = rsa.SignData(data, primaryId);
                byte[] lookup = rsa.SignData(data, halg);

                Assert.Equal(primary, lookup);
            }
        }

        public static IEnumerable<object[]> AlgorithmIdentifiers()
        {
            return new[]
            {
                new object[] { "MD5", MD5.Create() },
                new object[] { "MD5", typeof(MD5) },
                new object[] { "MD5", "1.2.840.113549.2.5" },
                new object[] { "SHA1", SHA1.Create() },
                new object[] { "SHA1", typeof(SHA1) },
                new object[] { "SHA1", "1.3.14.3.2.26" },
                new object[] { "SHA256", SHA256.Create() },
                new object[] { "SHA256", typeof(SHA256) },
                new object[] { "SHA256", "2.16.840.1.101.3.4.2.1" },
                new object[] { "SHA384", SHA384.Create() },
                new object[] { "SHA384", typeof(SHA384) },
                new object[] { "SHA384", "2.16.840.1.101.3.4.2.2" },
                new object[] { "SHA512", SHA512.Create() },
                new object[] { "SHA512", typeof(SHA512) },
                new object[] { "SHA512", "2.16.840.1.101.3.4.2.3" },
            };
        }
    }
}
