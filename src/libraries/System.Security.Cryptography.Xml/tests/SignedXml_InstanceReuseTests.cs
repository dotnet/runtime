// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class SignedXml_InstanceReuseTests
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

        // --------------------------------------------------------------------
        // Preflight: clean input-validation throws must NOT poison the instance.
        // A caller that catches the exception, fixes the parameter, and retries
        // on the same instance should succeed.
        // --------------------------------------------------------------------

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

                // No SigningKey set: throws CryptographicException without marking the instance used.
                Assert.Throws<CryptographicException>(() => signedXml.ComputeSignature());

                // Setting the key and retrying on the same instance succeeds.
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

            // Non-HMAC keyed hash: throws CryptographicException without marking the instance used.
            using (NonHmacKeyedHash bad = new NonHmacKeyedHash())
            {
                Assert.Throws<CryptographicException>(() => signedXml.ComputeSignature(bad));
            }

            // Retry with a valid HMAC on the same instance succeeds.
            using (HMACSHA256 mac = new HMACSHA256(new byte[64]))
            {
                signedXml.ComputeSignature(mac);
                Assert.NotNull(signedXml.SignatureValue);
            }
        }

        private sealed class NonHmacKeyedHash : KeyedHashAlgorithm
        {
            public NonHmacKeyedHash() { HashSizeValue = 256; }
            public override void Initialize() { }
            protected override void HashCore(byte[] array, int ibStart, int cbSize) { }
            protected override byte[] HashFinal() => new byte[32];
        }

        // --------------------------------------------------------------------
        // Positive scenarios: legitimate patterns that must keep working.
        // --------------------------------------------------------------------

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void MultipleReferencesAndObjects_BeforeCompute_Work()
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

                Reference r1 = new Reference("#obj1");
                signedXml.AddReference(r1);
                Reference r2 = new Reference("#obj2");
                signedXml.AddReference(r2);

                signedXml.ComputeSignature();
                Assert.NotNull(signedXml.SignatureValue);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void LoadXml_ThenCheckSignature_Works()
        {
            using (RSA key = RSA.Create())
            {
                // Standard verify workflow: construct, LoadXml, CheckSignature. Must not throw.
                (_, SignedXml verifier) = PrepareVerifier(key);
                Assert.True(verifier.CheckSignature(key));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void FreshInstance_PerVerification_Works()
        {
            // Re-verifying the same signature with a fresh instance each time must work.
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
                    // Read-only inspection of the SignedXml state must not trip the guard.
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

                // GetXml is read-only; must not throw.
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

                // Read-only property access after a completed operation must not throw.
                Assert.NotNull(verifier.Signature);
                Assert.NotNull(verifier.SignedInfo);
                Assert.NotNull(verifier.SignatureValue);
                Assert.NotNull(verifier.SignatureMethod);
            }
        }
    }
}

