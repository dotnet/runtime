// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Xml
{
    internal abstract class RSAPKCS1SignatureDescription : SignatureDescription
    {
        public RSAPKCS1SignatureDescription(string hashAlgorithmName)
        {
            KeyAlgorithm = typeof(RSA).AssemblyQualifiedName;
            FormatterAlgorithm = typeof(RSAPKCS1SignatureFormatter).AssemblyQualifiedName;
            DeformatterAlgorithm = typeof(RSAPKCS1SignatureDeformatter).AssemblyQualifiedName;
            DigestAlgorithm = hashAlgorithmName;
        }

        public sealed override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
        {
            var item = (AsymmetricSignatureDeformatter)CryptoHelpers.CreateFromName(DeformatterAlgorithm);
            item.SetKey(key);
            item.SetHashAlgorithm(DigestAlgorithm);
            return item;
        }

        public sealed override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
        {
            var item = (AsymmetricSignatureFormatter)CryptoHelpers.CreateFromName(FormatterAlgorithm);
            item.SetKey(key);
            item.SetHashAlgorithm(DigestAlgorithm);
            return item;
        }

        public abstract override HashAlgorithm CreateDigest();
    }
}
