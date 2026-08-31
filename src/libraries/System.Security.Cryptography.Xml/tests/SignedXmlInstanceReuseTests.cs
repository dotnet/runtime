// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class SignedXmlInstanceReuseTests
    {
        private const string ExampleXml = @"<?xml version=""1.0""?>
<example>
<test>some text node</test>
</example>";

        private static SignedXml PrepareSigner(RSA key, out XmlDocument doc)
        {
            doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(ExampleXml);

            SignedXml signedXml = new SignedXml(doc) { SigningKey = key };
            Reference reference = new Reference { Uri = "" };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);
            return signedXml;
        }

        private static (XmlDocument doc, SignedXml verifier) PrepareVerifier(RSA key)
        {
            XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(ExampleXml);
            SignedXml signer = new SignedXml(doc) { SigningKey = key };
            Reference reference = new Reference { Uri = "" };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signer.AddReference(reference);
            signer.ComputeSignature();
            doc.DocumentElement!.AppendChild(doc.ImportNode(signer.GetXml(), true));

            XmlDocument verifyDoc = new XmlDocument { PreserveWhitespace = true };
            verifyDoc.LoadXml(doc.OuterXml);
            SignedXml verifier = new SignedXml(verifyDoc);
            verifier.LoadXml((XmlElement)verifyDoc.GetElementsByTagName("Signature")[0]!);
            return (verifyDoc, verifier);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void ComputeSignature_Twice_Throws()
        {
            using (RSA key = RSA.Create())
            {
                SignedXml signedXml = PrepareSigner(key, out _);
                signedXml.ComputeSignature();

                Assert.Throws<InvalidOperationException>(() => signedXml.ComputeSignature());
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void ComputeSignature_ThenCheckSignature_Throws()
        {
            using (RSA key = RSA.Create())
            {
                SignedXml signedXml = PrepareSigner(key, out _);
                signedXml.ComputeSignature();

                Assert.Throws<InvalidOperationException>(() => signedXml.CheckSignature(key));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void CheckSignature_Twice_Throws()
        {
            using (RSA key = RSA.Create())
            {
                (_, SignedXml verifier) = PrepareVerifier(key);

                Assert.True(verifier.CheckSignature(key));
                Assert.Throws<InvalidOperationException>(() => verifier.CheckSignature(key));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void CheckSignatureNoArg_Twice_Throws()
        {
            using (RSA key = RSA.Create())
            {
                (_, SignedXml verifier) = PrepareVerifier(key);
                verifier.KeyInfo = new KeyInfo();
                verifier.KeyInfo.AddClause(new RSAKeyValue(key));

                Assert.True(verifier.CheckSignature());
                Assert.Throws<InvalidOperationException>(() => verifier.CheckSignature());
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void CheckSignatureReturningKey_Twice_Throws()
        {
            using (RSA key = RSA.Create())
            {
                (_, SignedXml verifier) = PrepareVerifier(key);
                verifier.KeyInfo = new KeyInfo();
                verifier.KeyInfo.AddClause(new RSAKeyValue(key));

                Assert.True(verifier.CheckSignatureReturningKey(out _));
                Assert.Throws<InvalidOperationException>(() => verifier.CheckSignatureReturningKey(out _));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void CheckSignature_ThenComputeSignature_Throws()
        {
            using (RSA key = RSA.Create())
            {
                (_, SignedXml verifier) = PrepareVerifier(key);

                Assert.True(verifier.CheckSignature(key));

                verifier.SigningKey = key;
                Assert.Throws<InvalidOperationException>(() => verifier.ComputeSignature());
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void CheckSignature_KeyedHash_Twice_Throws()
        {
            byte[] hmacKey = new byte[64];
            using (HMACSHA256 mac = new HMACSHA256(hmacKey))
            {
                XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml(ExampleXml);

                SignedXml signer = new SignedXml(doc);
                Reference reference = new Reference { Uri = "" };
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signer.AddReference(reference);
                signer.ComputeSignature(mac);
                doc.DocumentElement!.AppendChild(doc.ImportNode(signer.GetXml(), true));

                XmlDocument verifyDoc = new XmlDocument { PreserveWhitespace = true };
                verifyDoc.LoadXml(doc.OuterXml);
                SignedXml verifier = new SignedXml(verifyDoc);
                verifier.LoadXml((XmlElement)verifyDoc.GetElementsByTagName("Signature")[0]!);

                using (HMACSHA256 mac2 = new HMACSHA256(hmacKey))
                {
                    Assert.True(verifier.CheckSignature(mac2));
                    Assert.Throws<InvalidOperationException>(() => verifier.CheckSignature(mac2));
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void ComputeSignature_KeyedHash_Twice_Throws()
        {
            byte[] hmacKey = new byte[64];
            using (HMACSHA256 mac = new HMACSHA256(hmacKey))
            {
                XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml(ExampleXml);

                SignedXml signer = new SignedXml(doc);
                Reference reference = new Reference { Uri = "" };
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signer.AddReference(reference);
                signer.ComputeSignature(mac);

                Assert.Throws<InvalidOperationException>(() => signer.ComputeSignature(mac));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void AddReference_AfterCompute_Throws()
        {
            using (RSA key = RSA.Create())
            {
                SignedXml signedXml = PrepareSigner(key, out _);
                signedXml.ComputeSignature();

                Reference extra = new Reference { Uri = "" };
                extra.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                Assert.Throws<InvalidOperationException>(() => signedXml.AddReference(extra));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void AddObject_AfterCompute_Throws()
        {
            using (RSA key = RSA.Create())
            {
                SignedXml signedXml = PrepareSigner(key, out _);
                signedXml.ComputeSignature();

                XmlDocument scratch = new XmlDocument();
                scratch.LoadXml("<data>x</data>");
                DataObject obj = new DataObject("id", null, null, scratch.DocumentElement!);
                Assert.Throws<InvalidOperationException>(() => signedXml.AddObject(obj));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void LoadXml_AfterCheck_Throws()
        {
            using (RSA key = RSA.Create())
            {
                (XmlDocument verifyDoc, SignedXml verifier) = PrepareVerifier(key);
                Assert.True(verifier.CheckSignature(key));

                XmlElement sig = (XmlElement)verifyDoc.GetElementsByTagName("Signature")[0]!;
                Assert.Throws<InvalidOperationException>(() => verifier.LoadXml(sig));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void ComputeSignature_MissingSigningKey_DoesNotPoisonInstance()
        {
            using (RSA key = RSA.Create())
            {
                XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml(ExampleXml);

                SignedXml signedXml = new SignedXml(doc);
                Reference reference = new Reference { Uri = "" };
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signedXml.AddReference(reference);

                Assert.Throws<CryptographicException>(() => signedXml.ComputeSignature());

                signedXml.SigningKey = key;
                signedXml.ComputeSignature();
                Assert.NotNull(signedXml.SignatureValue);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void ComputeSignature_NonHmacKeyedHash_DoesNotPoisonInstance()
        {
            XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(ExampleXml);

            SignedXml signedXml = new SignedXml(doc);
            Reference reference = new Reference { Uri = "" };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);

            using (NonHmacKeyedHash bad = new NonHmacKeyedHash())
            {
                Assert.Throws<CryptographicException>(() => signedXml.ComputeSignature(bad));
            }

            using (HMACSHA256 mac = new HMACSHA256(new byte[64]))
            {
                signedXml.ComputeSignature(mac);
                Assert.NotNull(signedXml.SignatureValue);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void CheckSignature_NullAsymmetricKey_DoesNotPoisonInstance()
        {
            using (RSA key = RSA.Create())
            {
                (_, SignedXml verifier) = PrepareVerifier(key);

                Assert.Throws<ArgumentNullException>(() => verifier.CheckSignature((AsymmetricAlgorithm)null!));

                Assert.True(verifier.CheckSignature(key));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void CheckSignature_NullKeyedHash_DoesNotPoisonInstance()
        {
            byte[] hmacKey = new byte[64];
            using (HMACSHA256 mac = new HMACSHA256(hmacKey))
            {
                XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml(ExampleXml);

                SignedXml signer = new SignedXml(doc);
                Reference reference = new Reference { Uri = "" };
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signer.AddReference(reference);
                signer.ComputeSignature(mac);
                doc.DocumentElement!.AppendChild(doc.ImportNode(signer.GetXml(), true));

                XmlDocument verifyDoc = new XmlDocument { PreserveWhitespace = true };
                verifyDoc.LoadXml(doc.OuterXml);
                SignedXml verifier = new SignedXml(verifyDoc);
                verifier.LoadXml((XmlElement)verifyDoc.GetElementsByTagName("Signature")[0]!);

                Assert.Throws<ArgumentNullException>(() => verifier.CheckSignature((KeyedHashAlgorithm)null!));

                using (HMACSHA256 mac2 = new HMACSHA256(hmacKey))
                {
                    Assert.True(verifier.CheckSignature(mac2));
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void CheckSignature_NullCertificate_DoesNotPoisonInstance()
        {
            using (RSA key = RSA.Create())
            {
                (_, SignedXml verifier) = PrepareVerifier(key);

                Assert.Throws<ArgumentNullException>(() => verifier.CheckSignature((X509Certificate2)null!, verifySignatureOnly: true));

                Assert.True(verifier.CheckSignature(key));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void MultipleReferencesAndObjects_BeforeCompute_Works()
        {
            using (RSA key = RSA.Create())
            {
                XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml("<root><a>1</a><b>2</b></root>");

                SignedXml signedXml = new SignedXml(doc) { SigningKey = key };

                XmlDocument obj1Doc = new XmlDocument();
                obj1Doc.LoadXml("<o1>x</o1>");
                signedXml.AddObject(new DataObject("obj1", null, null, obj1Doc.DocumentElement!));

                XmlDocument obj2Doc = new XmlDocument();
                obj2Doc.LoadXml("<o2>y</o2>");
                signedXml.AddObject(new DataObject("obj2", null, null, obj2Doc.DocumentElement!));

                signedXml.AddReference(new Reference("#obj1"));
                signedXml.AddReference(new Reference("#obj2"));

                signedXml.ComputeSignature();
                Assert.NotNull(signedXml.SignatureValue);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void LoadXml_ThenCheckSignature_Works()
        {
            using (RSA key = RSA.Create())
            {
                (_, SignedXml verifier) = PrepareVerifier(key);
                Assert.True(verifier.CheckSignature(key));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void FreshInstance_PerVerification_Works()
        {
            using (RSA key = RSA.Create())
            {
                XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml(ExampleXml);
                SignedXml signer = new SignedXml(doc) { SigningKey = key };
                Reference reference = new Reference { Uri = "" };
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signer.AddReference(reference);
                signer.ComputeSignature();
                doc.DocumentElement!.AppendChild(doc.ImportNode(signer.GetXml(), true));
                string signed = doc.OuterXml;

                for (int i = 0; i < 3; i++)
                {
                    XmlDocument verifyDoc = new XmlDocument { PreserveWhitespace = true };
                    verifyDoc.LoadXml(signed);
                    SignedXml verifier = new SignedXml(verifyDoc);
                    verifier.LoadXml((XmlElement)verifyDoc.GetElementsByTagName("Signature")[0]!);
                    Assert.True(verifier.CheckSignature(key));
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void SignatureFormatValidator_ReadOnly_Works()
        {
            using (RSA key = RSA.Create())
            {
                (_, SignedXml verifier) = PrepareVerifier(key);
                bool validatorCalled = false;
                verifier.SignatureFormatValidator = sx =>
                {
                    validatorCalled = true;
                    return sx.SignedInfo != null && sx.SignatureValue != null;
                };
                Assert.True(verifier.CheckSignature(key));
                Assert.True(validatorCalled);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void GetXml_AfterCompute_Works()
        {
            using (RSA key = RSA.Create())
            {
                SignedXml signedXml = PrepareSigner(key, out _);
                signedXml.ComputeSignature();

                XmlElement sig1 = signedXml.GetXml();
                XmlElement sig2 = signedXml.GetXml();
                Assert.Equal(sig1.OuterXml, sig2.OuterXml);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void ReadingProperties_AfterCheck_Works()
        {
            using (RSA key = RSA.Create())
            {
                (_, SignedXml verifier) = PrepareVerifier(key);
                Assert.True(verifier.CheckSignature(key));

                Assert.NotNull(verifier.Signature);
                Assert.NotNull(verifier.SignedInfo);
                Assert.NotNull(verifier.SignatureValue);
                Assert.NotNull(verifier.SignatureMethod);
            }
        }

        private sealed class NonHmacKeyedHash : KeyedHashAlgorithm
        {
            public NonHmacKeyedHash() { HashSizeValue = 256; }
            public override void Initialize() { }
            protected override void HashCore(byte[] array, int ibStart, int cbSize) { }
            protected override byte[] HashFinal() => new byte[32];
        }
    }
}
