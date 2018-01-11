// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using Xunit;

using Test.Cryptography;
using System.Security.Cryptography.Pkcs.Tests;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    public static partial class EdgeCasesTests
    {
        public static bool SupportsCngCertificates { get; } = (!PlatformDetection.IsFullFramework || PlatformDetection.IsNetfx462OrNewer);

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void ImportEdgeCase()
        {
            //
            // Pfx's imported into a certificate collection propagate their "delete on Dispose" behavior to its cloned instances:
            // a subtle difference from Pfx's created using the X509Certificate2 constructor that can lead to premature or
            // double key deletion. Since EnvelopeCms.Decrypt() has no legitimate reason to clone the extraStore certs, this shouldn't
            // be a problem, but this test will verify that it isn't.
            //

            byte[] encodedMessage =
                ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481805e"
                + "bb2d08773594be9ec5d30c0707cf339f2b982a4f0797b74d520a0c973d668a9a6ad9d28066ef36e5b5620fef67f4d79ee50c"
                + "25eb999f0c656548347d5676ac4b779f8fce2b87e6388fbe483bb0fcf78ab1f1ff29169600401fded7b2803a0bf96cc160c4"
                + "96726216e986869eed578bda652855c85604a056201538ee56b6c4302b06092a864886f70d010701301406082a864886f70d"
                + "030704083adadf63cd297a86800835edc437e31d0b70").HexToByteArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.LoadPfxUsingCollectionImport())
            {
                X509Certificate2Collection extraStore = new X509Certificate2Collection(cert);
                ecms.Decrypt(extraStore);

                byte[] expectedContent = { 1, 2, 3 };
                ContentInfo contentInfo = ecms.ContentInfo;
                Assert.Equal<byte>(expectedContent, contentInfo.Content);
            }
        }

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void ImportEdgeCaseSki()
        {
            byte[] encodedMessage =
                ("3081f206092a864886f70d010703a081e43081e10201023181ae3081ab0201028014f2008aa9fa3742e8370cb1674ce1d158"
                + "2921dcc3300d06092a864886f70d01010105000481804336e978bc72ba2f5264cd854867fac438f36f2b3df6004528f2df83"
                + "4fb2113d6f7c07667e7296b029756222d6ced396a8fffed32be838eec7f2e54b9467fa80f85d097f7d1f0fbde57e07ab3d46"
                + "a60b31f37ef9844dcab2a8eef4fec5579fac5ec1e7ee82409898e17d30c3ac1a407fca15d23c9df2904a707294d78d4300ba"
                + "302b06092a864886f70d010701301406082a864886f70d03070408355c596e3e8540608008f1f811e862e51bbd").HexToByteArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.LoadPfxUsingCollectionImport())
            {
                X509Certificate2Collection extraStore = new X509Certificate2Collection(cert);
                ecms.Decrypt(extraStore);

                byte[] expectedContent = { 1, 2, 3 };
                ContentInfo contentInfo = ecms.ContentInfo;
                Assert.Equal<byte>(new byte[] { 1, 2, 3 }, contentInfo.Content);
                Assert.Equal<byte>(expectedContent, contentInfo.Content);
            }
        }

        private static X509Certificate2 LoadPfxUsingCollectionImport(this CertLoader certLoader)
        {
            byte[] pfxData = certLoader.PfxData;
            string password = certLoader.Password;
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(pfxData, password, X509KeyStorageFlags.DefaultKeySet);
            Assert.Equal(1, collection.Count);
            return collection[0];
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Desktop rejects zero length content: corefx#18724")]
        public static void ZeroLengthContent_RoundTrip()
        {
            ContentInfo contentInfo = new ContentInfo(Array.Empty<byte>());
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();
            ValidateZeroLengthContent(encodedMessage);
        }

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void ZeroLengthContent_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082010406092a864886f70d010703a081f63081f30201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004818009"
                + "c16b674495c2c3d4763189c3274cf7a9142fbeec8902abdc9ce29910d541df910e029a31443dc9a9f3b05f02da1c38478c40"
                + "0261c734d6789c4197c20143c4312ceaa99ecb1849718326d4fc3b7fbb2d1d23281e31584a63e99f2c17132bcd8eddb63296"
                + "7125cd0a4baa1efa8ce4c855f7c093339211bdf990cef5cce6cd74302306092a864886f70d010701301406082a864886f70d"
                + "03070408779b3de045826b188000").HexToByteArray();
            ValidateZeroLengthContent(encodedMessage);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "RC4 isn't available via CNG, and CNG is the only library available to UWP")]
        public static void Rc4AndCngWrappersDontMixTest()
        {
            //
            // Combination of RC4 over a CAPI certificate.
            //
            //  This works as long as the PKCS implementation opens the cert using CAPI. If he creates a CNG wrapper handle (by passing CRYPT_ACQUIRE_PREFER_NCRYPT_KEY_FLAG),
            //  the test fails with a NOTSUPPORTED crypto exception inside Decrypt(). The same happens if the key is genuinely CNG.
            //

            byte[] content = { 6, 3, 128, 33, 44 };
            AlgorithmIdentifier rc4 = new AlgorithmIdentifier(new Oid(Oids.Rc4));

            EnvelopedCms ecms = new EnvelopedCms(new ContentInfo(content), rc4);
            CmsRecipientCollection recipients = new CmsRecipientCollection(new CmsRecipient(Certificates.RSAKeyTransferCapi1.GetCertificate()));
            ecms.Encrypt(recipients);
            byte[] encodedMessage = ecms.Encode();

            ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            // In order to actually use the CAPI version of the key, perphemeral loading must be specified.
            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.CloneAsPerphemeralLoader().TryGetCertificateWithPrivateKey())
            {
                if (cert == null)
                    return; // Sorry - CertLoader is not configured to load certs with private keys - we've tested as much as we can.

                X509Certificate2Collection extraStore = new X509Certificate2Collection();
                extraStore.Add(cert);
                ecms.Decrypt(extraStore);
            }

            ContentInfo contentInfo = ecms.ContentInfo;
            Assert.Equal<byte>(content, contentInfo.Content);
        }

        private static void ValidateZeroLengthContent(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.TryGetCertificateWithPrivateKey())
            {
                if (cert == null)
                    return;
                X509Certificate2Collection extraStore = new X509Certificate2Collection(cert);
                ecms.Decrypt(extraStore);
                ContentInfo contentInfo = ecms.ContentInfo;
                byte[] content = contentInfo.Content;

                int expected = PlatformDetection.IsFullFramework ? 6 : 0; // Desktop bug gives 6
                Assert.Equal(expected, content.Length);
            }
        }

        [Fact]
        public static void ReuseEnvelopeCmsEncodeThenDecode()
        {
            // Test ability to encrypt, encode and decode all in one EnvelopedCms instance.

            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }

            byte[] encodedMessage = ecms.Encode();
            ecms.Decode(encodedMessage);

            RecipientInfoCollection recipients = ecms.RecipientInfos;
            Assert.Equal(1, recipients.Count);
            RecipientInfo recipientInfo = recipients[0];
            KeyTransRecipientInfo recipient = recipientInfo as KeyTransRecipientInfo;
            Assert.NotNull(recipientInfo);

            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            object value = subjectIdentifier.Value;
            Assert.True(value is X509IssuerSerial);
            X509IssuerSerial xis = (X509IssuerSerial)value;
            Assert.Equal("CN=RSAKeyTransfer1", xis.IssuerName);
            Assert.Equal("31D935FB63E8CFAB48A0BF7B397B67C0", xis.SerialNumber);
        }

        [Fact]
        public static void ReuseEnvelopeCmsDecodeThenEncode()
        {
            byte[] encodedMessage =
                ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481805e"
                + "bb2d08773594be9ec5d30c0707cf339f2b982a4f0797b74d520a0c973d668a9a6ad9d28066ef36e5b5620fef67f4d79ee50c"
                + "25eb999f0c656548347d5676ac4b779f8fce2b87e6388fbe483bb0fcf78ab1f1ff29169600401fded7b2803a0bf96cc160c4"
                + "96726216e986869eed578bda652855c85604a056201538ee56b6c4302b06092a864886f70d010701301406082a864886f70d"
                + "030704083adadf63cd297a86800835edc437e31d0b70").HexToByteArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }

            encodedMessage = ecms.Encode();
            ecms.Decode(encodedMessage);

            RecipientInfoCollection recipients = ecms.RecipientInfos;
            Assert.Equal(1, recipients.Count);
            RecipientInfo recipientInfo = recipients[0];
            KeyTransRecipientInfo recipient = recipientInfo as KeyTransRecipientInfo;
            Assert.NotNull(recipientInfo);

            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            object value = subjectIdentifier.Value;
            Assert.True(value is X509IssuerSerial);
            X509IssuerSerial xis = (X509IssuerSerial)value;
            Assert.Equal("CN=RSAKeyTransfer1", xis.IssuerName);
            Assert.Equal("31D935FB63E8CFAB48A0BF7B397B67C0", xis.SerialNumber);
        }

        [Fact]
        public static void EnvelopedCmsNullContent()
        {
            object ignore;
            Assert.Throws<ArgumentNullException>(() => ignore = new EnvelopedCms(null));
            Assert.Throws<ArgumentNullException>(() => ignore = new EnvelopedCms(null, new AlgorithmIdentifier(new Oid(Oids.TripleDesCbc))));
        }

        [Fact]
        public static void EnvelopedCmsNullAlgorithm()
        {
            object ignore;
            ContentInfo contentInfo = new ContentInfo(new byte[3]);
            Assert.Throws<ArgumentNullException>(() => ignore = new EnvelopedCms(contentInfo, null));
        }

        [Fact]
        public static void EnvelopedCmsEncryptWithNullRecipient()
        {
            EnvelopedCms ecms = new EnvelopedCms(new ContentInfo(new byte[3]));
            Assert.Throws<ArgumentNullException>(() => ecms.Encrypt((CmsRecipient)null));
        }

        [Fact]
        public static void EnvelopedCmsEncryptWithNullRecipients()
        {
            EnvelopedCms ecms = new EnvelopedCms(new ContentInfo(new byte[3]));
            Assert.Throws<ArgumentNullException>(() => ecms.Encrypt((CmsRecipientCollection)null));
        }

        [Fact]
        // On the desktop, this throws up a UI for the user to select a recipient. We don't support that.
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void EnvelopedCmsEncryptWithZeroRecipients()
        {
            EnvelopedCms ecms = new EnvelopedCms(new ContentInfo(new byte[3]));
            Assert.Throws<PlatformNotSupportedException>(() => ecms.Encrypt(new CmsRecipientCollection()));
        }

        [Fact]
        public static void EnvelopedCmsNullDecode()
        {
            EnvelopedCms ecms = new EnvelopedCms();
            Assert.Throws<ArgumentNullException>(() => ecms.Decode(null));
        }

        [Fact]
        public static void EnvelopedCmsDecryptNullary()
        {
            byte[] encodedMessage =
                ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481805e"
                + "bb2d08773594be9ec5d30c0707cf339f2b982a4f0797b74d520a0c973d668a9a6ad9d28066ef36e5b5620fef67f4d79ee50c"
                + "25eb999f0c656548347d5676ac4b779f8fce2b87e6388fbe483bb0fcf78ab1f1ff29169600401fded7b2803a0bf96cc160c4"
                + "96726216e986869eed578bda652855c85604a056201538ee56b6c4302b06092a864886f70d010701301406082a864886f70d"
                + "030704083adadf63cd297a86800835edc437e31d0b70").HexToByteArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            Assert.ThrowsAny<CryptographicException>(() => ecms.Decrypt());
        }

        [Fact]
        public static void EnvelopedCmsDecryptNullRecipient()
        {
            byte[] encodedMessage =
                ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481805e"
                + "bb2d08773594be9ec5d30c0707cf339f2b982a4f0797b74d520a0c973d668a9a6ad9d28066ef36e5b5620fef67f4d79ee50c"
                + "25eb999f0c656548347d5676ac4b779f8fce2b87e6388fbe483bb0fcf78ab1f1ff29169600401fded7b2803a0bf96cc160c4"
                + "96726216e986869eed578bda652855c85604a056201538ee56b6c4302b06092a864886f70d010701301406082a864886f70d"
                + "030704083adadf63cd297a86800835edc437e31d0b70").HexToByteArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            RecipientInfo recipientInfo = null;
            X509Certificate2Collection extraStore = new X509Certificate2Collection();
            Assert.Throws<ArgumentNullException>(() => ecms.Decrypt(recipientInfo));
            Assert.Throws<ArgumentNullException>(() => ecms.Decrypt(recipientInfo, extraStore));
        }

        [Fact]
        public static void EnvelopedCmsDecryptNullExtraStore()
        {
            byte[] encodedMessage =
                ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481805e"
                + "bb2d08773594be9ec5d30c0707cf339f2b982a4f0797b74d520a0c973d668a9a6ad9d28066ef36e5b5620fef67f4d79ee50c"
                + "25eb999f0c656548347d5676ac4b779f8fce2b87e6388fbe483bb0fcf78ab1f1ff29169600401fded7b2803a0bf96cc160c4"
                + "96726216e986869eed578bda652855c85604a056201538ee56b6c4302b06092a864886f70d010701301406082a864886f70d"
                + "030704083adadf63cd297a86800835edc437e31d0b70").HexToByteArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            RecipientInfo recipientInfo = ecms.RecipientInfos[0];
            X509Certificate2Collection extraStore = null;
            Assert.Throws<ArgumentNullException>(() => ecms.Decrypt(extraStore));
            Assert.Throws<ArgumentNullException>(() => ecms.Decrypt(recipientInfo, extraStore));
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void EnvelopedCmsDecryptWithoutMatchingCert()
        {
            // You don't have the private key? No message for you.

            // This is the private key that "we don't have." We want to force it to load anyway, though, to trigger
            // the "fail the test due to bad machine config" exception if someone left this cert in the MY store check. 
            using (X509Certificate2 ignore = Certificates.RSAKeyTransfer1.TryGetCertificateWithPrivateKey())
            { }

            byte[] encodedMessage =
                ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481805e"
                + "bb2d08773594be9ec5d30c0707cf339f2b982a4f0797b74d520a0c973d668a9a6ad9d28066ef36e5b5620fef67f4d79ee50c"
                + "25eb999f0c656548347d5676ac4b779f8fce2b87e6388fbe483bb0fcf78ab1f1ff29169600401fded7b2803a0bf96cc160c4"
                + "96726216e986869eed578bda652855c85604a056201538ee56b6c4302b06092a864886f70d010701301406082a864886f70d"
                + "030704083adadf63cd297a86800835edc437e31d0b70").HexToByteArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            RecipientInfo recipientInfo = ecms.RecipientInfos[0];
            X509Certificate2Collection extraStore = new X509Certificate2Collection();
            Assert.ThrowsAny<CryptographicException>(() => ecms.Decrypt(recipientInfo));
            Assert.ThrowsAny<CryptographicException>(() => ecms.Decrypt(extraStore));
            Assert.ThrowsAny<CryptographicException>(() => ecms.Decrypt(recipientInfo, extraStore));
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void EnvelopedCmsDecryptWithoutMatchingCertSki()
        {
            // You don't have the private key? No message for you.

            // This is the private key that "we don't have." We want to force it to load anyway, though, to trigger
            // the "fail the test due to bad machine config" exception if someone left this cert in the MY store check. 
            using (X509Certificate2 ignore = Certificates.RSAKeyTransfer1.TryGetCertificateWithPrivateKey())
            { }

            byte[] encodedMessage =
                ("3081f206092a864886f70d010703a081e43081e10201023181ae3081ab0201028014f2008aa9fa3742e8370cb1674ce1d158"
                + "2921dcc3300d06092a864886f70d01010105000481804336e978bc72ba2f5264cd854867fac438f36f2b3df6004528f2df83"
                + "4fb2113d6f7c07667e7296b029756222d6ced396a8fffed32be838eec7f2e54b9467fa80f85d097f7d1f0fbde57e07ab3d46"
                + "a60b31f37ef9844dcab2a8eef4fec5579fac5ec1e7ee82409898e17d30c3ac1a407fca15d23c9df2904a707294d78d4300ba"
                + "302b06092a864886f70d010701301406082a864886f70d03070408355c596e3e8540608008f1f811e862e51bbd").HexToByteArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            RecipientInfo recipientInfo = ecms.RecipientInfos[0];
            X509Certificate2Collection extraStore = new X509Certificate2Collection();
            Assert.ThrowsAny<CryptographicException>(() => ecms.Decrypt(recipientInfo));
            Assert.ThrowsAny<CryptographicException>(() => ecms.Decrypt(extraStore));
            Assert.ThrowsAny<CryptographicException>(() => ecms.Decrypt(recipientInfo, extraStore));
        }

        [Fact]
        public static void AlgorithmIdentifierNullaryCtor()
        {
            AlgorithmIdentifier a = new AlgorithmIdentifier();
            Assert.Equal(Oids.TripleDesCbc, a.Oid.Value);
            Assert.Equal(0, a.KeyLength);
        }

        [Fact]
        public static void CmsRecipient1AryCtor()
        {
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient r = new CmsRecipient(cert);
                Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, r.RecipientIdentifierType);
                Assert.Same(cert, r.Certificate);
            }
        }

        [Fact]
        public static void CmsRecipientPassUnknown()
        {
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient r = new CmsRecipient(SubjectIdentifierType.Unknown, cert);
                Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, r.RecipientIdentifierType);
                Assert.Same(cert, r.Certificate);
            }
        }

        [Fact]
        public static void CmsRecipientPassNullCertificate()
        {
            object ignore;
            Assert.Throws<ArgumentNullException>(() => ignore = new CmsRecipient(null));
            Assert.Throws<ArgumentNullException>(() => ignore = new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber, null));
        }

        [Fact]
        public static void ContentInfoNullOid()
        {
            object ignore;
            Assert.Throws<ArgumentNullException>(() => ignore = new ContentInfo(null, new byte[3]));
        }

        [Fact]
        public static void ContentInfoNullContent()
        {
            object ignore;
            Assert.Throws<ArgumentNullException>(() => ignore = new ContentInfo(null));
            Assert.Throws<ArgumentNullException>(() => ignore = new ContentInfo(null, null));
        }

        [Fact]
        public static void ContentInfoGetContentTypeNull()
        {
            Assert.Throws<ArgumentNullException>(() => ContentInfo.GetContentType(null));
        }

        [Fact]
        public static void CryptographicAttributeObjectOidCtor()
        {
            Oid oid = new Oid(Oids.DocumentDescription);
            CryptographicAttributeObject cao = new CryptographicAttributeObject(oid);
            Assert.Equal(oid.Value, cao.Oid.Value);
            Assert.Equal(0, cao.Values.Count);
        }

        [Fact]
        public static void CryptographicAttributeObjectPassNullValuesToCtor()
        {
            Oid oid = new Oid(Oids.DocumentDescription);
            // This is legal and equivalent to passing a zero-length AsnEncodedDataCollection.
            CryptographicAttributeObject cao = new CryptographicAttributeObject(oid, null);
            Assert.Equal(oid.Value, cao.Oid.Value);
            Assert.Equal(0, cao.Values.Count);
        }

        [Fact]
        public static void CryptographicAttributeObjectMismatch()
        {
            Oid oid = new Oid(Oids.DocumentDescription);
            Oid wrongOid = new Oid(Oids.DocumentName);

            AsnEncodedDataCollection col = new AsnEncodedDataCollection();
            col.Add(new AsnEncodedData(oid, new byte[3]));

            object ignore;
            Assert.Throws<InvalidOperationException>(() => ignore = new CryptographicAttributeObject(wrongOid, col));
        }
    }
}


