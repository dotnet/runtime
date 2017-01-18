// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// (C) 2002 Motus Technologies Inc. (http://www.motus.com)
// (C) 2004 Novell  http://www.novell.com

using Xunit;

namespace System.Security.Cryptography.Algorithms.Tests
{
    public class SignatureDescriptionTests
    {
        [Fact]
        public void Constructor_Default()
        {
            SignatureDescription sig = new SignatureDescription();
            Assert.Null(sig.KeyAlgorithm);
            Assert.Null(sig.DigestAlgorithm);
            Assert.Null(sig.FormatterAlgorithm);
            Assert.Null(sig.DeformatterAlgorithm);
        }

        [Fact]
        public void Constructor_Null()
        {
            Assert.Throws<ArgumentNullException>("el", () => new SignatureDescription(null));
        }

        [Fact]
        public void Constructor_SecurityElement_Empty()
        {
            SecurityElement se = new SecurityElement("xml");
            SignatureDescription sig = new SignatureDescription(se);
            Assert.Null(sig.KeyAlgorithm);
            Assert.Null(sig.DigestAlgorithm);
            Assert.Null(sig.FormatterAlgorithm);
            Assert.Null(sig.DeformatterAlgorithm);
        }

        [Fact]
        public void Constructor_SecurityElement_DSA()
        {
            SecurityElement se = new SecurityElement("DSASignature");
            se.AddChild(new SecurityElement("Key", "System.Security.Cryptography.DSACryptoServiceProvider"));
            se.AddChild(new SecurityElement("Digest", "System.Security.Cryptography.SHA1CryptoServiceProvider"));
            se.AddChild(new SecurityElement("Formatter", "System.Security.Cryptography.DSASignatureFormatter"));
            se.AddChild(new SecurityElement("Deformatter", "System.Security.Cryptography.DSASignatureDeformatter"));

            SignatureDescription sig = new SignatureDescription(se);
            Assert.Equal("System.Security.Cryptography.DSACryptoServiceProvider", sig.KeyAlgorithm);
            Assert.Equal("System.Security.Cryptography.SHA1CryptoServiceProvider", sig.DigestAlgorithm);
            Assert.Equal("System.Security.Cryptography.DSASignatureFormatter", sig.FormatterAlgorithm);
            Assert.Equal("System.Security.Cryptography.DSASignatureDeformatter", sig.DeformatterAlgorithm);
        }

        [Fact]
        public void Constructor_SecurityElement_RSA()
        {
            SecurityElement se = new SecurityElement("RSASignature");
            se.AddChild(new SecurityElement("Key", "System.Security.Cryptography.RSACryptoServiceProvider"));
            se.AddChild(new SecurityElement("Digest", "System.Security.Cryptography.SHA1CryptoServiceProvider"));
            se.AddChild(new SecurityElement("Formatter", "System.Security.Cryptography.RSAPKCS1SignatureFormatter"));
            se.AddChild(new SecurityElement("Deformatter", "System.Security.Cryptography.RSAPKCS1SignatureDeformatter"));

            SignatureDescription sig = new SignatureDescription(se);
            Assert.Equal("System.Security.Cryptography.RSACryptoServiceProvider", sig.KeyAlgorithm);
            Assert.Equal("System.Security.Cryptography.SHA1CryptoServiceProvider", sig.DigestAlgorithm);
            Assert.Equal("System.Security.Cryptography.RSAPKCS1SignatureFormatter", sig.FormatterAlgorithm);
            Assert.Equal("System.Security.Cryptography.RSAPKCS1SignatureDeformatter", sig.DeformatterAlgorithm);
        }

        [Fact]
        public void Properties()
        {
            const string invalid = "invalid";
            SignatureDescription sig = new SignatureDescription();

            sig.DeformatterAlgorithm = invalid;
            Assert.NotNull(sig.DeformatterAlgorithm);
            Assert.Equal(invalid, sig.DeformatterAlgorithm);
            sig.DeformatterAlgorithm = null;
            Assert.Null(sig.DeformatterAlgorithm);

            sig.DigestAlgorithm = invalid;
            Assert.NotNull(sig.DigestAlgorithm);
            Assert.Equal(invalid, sig.DigestAlgorithm);
            sig.DigestAlgorithm = null;
            Assert.Null(sig.DigestAlgorithm);

            sig.FormatterAlgorithm = invalid;
            Assert.NotNull(sig.FormatterAlgorithm);
            Assert.Equal(invalid, sig.FormatterAlgorithm);
            sig.FormatterAlgorithm = null;
            Assert.Null(sig.FormatterAlgorithm);

            sig.KeyAlgorithm = invalid;
            Assert.NotNull(sig.KeyAlgorithm);
            Assert.Equal(invalid, sig.KeyAlgorithm);
            sig.KeyAlgorithm = null;
            Assert.Null(sig.KeyAlgorithm);
        }

        [Fact]
        public void Deformatter()
        {
            AsymmetricSignatureDeformatter def;
            SignatureDescription sig = new SignatureDescription();
            DSA dsa = DSA.Create();

            // Deformatter with all properties null
            Assert.Throws<ArgumentNullException>("name", () => sig.CreateDeformatter(dsa));

            // Deformatter with invalid DeformatterAlgorithm property
            sig.DeformatterAlgorithm = "DSA";
            Assert.ThrowsAny<Exception>(() => def = sig.CreateDeformatter(dsa));

            // Deformatter with valid DeformatterAlgorithm property
            sig.DeformatterAlgorithm = "DSASignatureDeformatter";
            Assert.Throws<NullReferenceException>(() => def = sig.CreateDeformatter(dsa));

            // Deformatter with valid DeformatterAlgorithm property
            sig.KeyAlgorithm = "DSA";
            sig.DigestAlgorithm = "SHA1";
            sig.DeformatterAlgorithm = "DSASignatureDeformatter";
            Assert.Throws<NullReferenceException>(() => def = sig.CreateDeformatter(dsa));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Digest()
        {
            bool rightClass = false;
            HashAlgorithm hash = null;
            SignatureDescription sig = new SignatureDescription();
            
            // null hash
            Assert.Throws<ArgumentNullException>("name", () => hash = sig.CreateDigest());

            sig.DigestAlgorithm = "SHA1";
            hash = sig.CreateDigest();
            Assert.NotNull(hash);
            rightClass = (hash.ToString().IndexOf(sig.DigestAlgorithm) > 0);
            Assert.True(rightClass, "CreateDigest(SHA1)");

            sig.DigestAlgorithm = "MD5";
            hash = sig.CreateDigest();
            Assert.NotNull(hash);
            rightClass = (hash.ToString().IndexOf(sig.DigestAlgorithm) > 0);
            Assert.True(rightClass, "CreateDigest(MD5)");

            sig.DigestAlgorithm = "SHA256";
            hash = sig.CreateDigest();
            Assert.NotNull(hash);
            rightClass = (hash.ToString().IndexOf(sig.DigestAlgorithm) > 0);
            Assert.True(rightClass, "CreateDigest(SHA256)");

            sig.DigestAlgorithm = "SHA384";
            hash = sig.CreateDigest();
            Assert.NotNull(hash);
            rightClass = (hash.ToString().IndexOf(sig.DigestAlgorithm) > 0);
            Assert.True(rightClass, "CreateDigest(SHA384)");

            sig.DigestAlgorithm = "SHA512";
            hash = sig.CreateDigest();
            Assert.NotNull(hash);
            rightClass = (hash.ToString().IndexOf(sig.DigestAlgorithm) > 0);
            Assert.True(rightClass, "CreateDigest(SHA512)");

            sig.DigestAlgorithm = "bad";
            hash = sig.CreateDigest();
            Assert.Null(hash);
        }

        [Fact]
        public void Formatter()
        {
            SignatureDescription sig = new SignatureDescription();
            DSA dsa = DSA.Create();

            // Formatter with all properties null
            Assert.Throws<ArgumentNullException>("name", () => sig.CreateFormatter(dsa));

            // Formatter with invalid FormatterAlgorithm property
            AsymmetricSignatureFormatter fmt = null;
            sig.FormatterAlgorithm = "DSA";
            Assert.ThrowsAny<Exception>(() => fmt = sig.CreateFormatter(dsa));

            // Formatter with valid FormatterAlgorithm property
            sig.FormatterAlgorithm = "DSASignatureFormatter";
            Assert.Throws<NullReferenceException>(() => sig.CreateFormatter(dsa));

            // Deformatter with valid DeformatterAlgorithm property
            sig.KeyAlgorithm = "DSA";
            sig.DigestAlgorithm = "SHA1";
            sig.FormatterAlgorithm = "DSASignatureFormatter";
            Assert.Throws<NullReferenceException>(() => sig.CreateFormatter(dsa));
        }
    }
}
