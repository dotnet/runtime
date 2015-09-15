using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public class X509StoreTests
    {
        [Fact]
        public static void OpenMyStore()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
            }
        }

        [Fact]
        public static void ReadMyCertificates()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                int certCount = store.Certificates.Count;

                // This assert is just so certCount appears to be used, the test really
                // is that store.get_Certificates didn't throw.
                Assert.True(certCount >= 0);
            }
        }

        [Fact]
        public static void OpenNotExistant()
        {
            using (X509Store store = new X509Store(Guid.NewGuid().ToString("N"), StoreLocation.CurrentUser))
            {
                Assert.ThrowsAny<CryptographicException>(() => store.Open(OpenFlags.OpenExistingOnly));
            }
        }

        [Fact]
        public static void AddReadOnlyThrows()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                store.Open(OpenFlags.ReadOnly);

                // Add only throws when it has to do work.  If, for some reason, this certificate
                // is already present in the CurrentUser\My store, we can't really test this
                // functionality.
                if (!store.Certificates.Contains(cert))
                {
                    Assert.ThrowsAny<CryptographicException>(() => store.Add(cert));
                }
            }
        }

        [Fact]
        public static void AddReadOnlyThrowsWhenCertificateExists()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);

                X509Certificate2 toAdd = null;

                // Look through the certificates to find one with no private key to call add on.
                // (The private key restriction is so that in the event of an "accidental success"
                // that no potential permissions would be modified)
                foreach (X509Certificate2 cert in store.Certificates)
                {
                    if (!cert.HasPrivateKey)
                    {
                        toAdd = cert;
                        break;
                    }
                }

                if (toAdd != null)
                {
                    Assert.ThrowsAny<CryptographicException>(() => store.Add(toAdd));
                }
            }
        }

        [Fact]
        public static void RemoveReadOnlyThrowsWhenFound()
        {
            // This test is unfortunate, in that it will mostly never test.
            // In order to do so it would have to open the store ReadWrite, put in a known value,
            // and call Remove on a ReadOnly copy.
            //
            // Just calling Remove on the first item found could also work (when the store isn't empty),
            // but if it fails the cost is too high.
            //
            // So what's the purpose of this test, you ask? To record why we're not unit testing it.
            // And someone could test it manually if they wanted.
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                store.Open(OpenFlags.ReadOnly);

                if (store.Certificates.Contains(cert))
                {
                    Assert.ThrowsAny<CryptographicException>(() => store.Remove(cert));
                }
            }
        }

        [Fact]
        public static void EnumerateClosedIsEmpty()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                int count = store.Certificates.Count;
                Assert.Equal(0, count);
            }
        }

        [Fact]
        public static void AddClosedThrows()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.ThrowsAny<CryptographicException>(() => store.Add(cert));
            }
        }

        [Fact]
        public static void RemoveClosedThrows()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.ThrowsAny<CryptographicException>(() => store.Remove(cert));
            }
        }
    }
}
