// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

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

#if NETCOREAPP
        [RequiresUnreferencedCode("CreateDeformatter is not trim compatible because the algorithm implementation referenced by DeformatterAlgorithm might be removed.")]
#endif
        public sealed override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
        {
            var item = (AsymmetricSignatureDeformatter)CryptoConfig.CreateFromName(DeformatterAlgorithm!)!;
            item.SetKey(key);
            item.SetHashAlgorithm(DigestAlgorithm!);
            return item;
        }

#if NETCOREAPP
        [RequiresUnreferencedCode("CreateFormatter is not trim compatible because the algorithm implementation referenced by FormatterAlgorithm might be removed.")]
#endif
        public sealed override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
        {
            var item = (AsymmetricSignatureFormatter)CryptoConfig.CreateFromName(FormatterAlgorithm!)!;
            item.SetKey(key);
            item.SetHashAlgorithm(DigestAlgorithm!);
            return item;
        }

#if NETCOREAPP
        [RequiresUnreferencedCode("CreateDigest is not trim compatible because the algorithm implementation referenced by DigestAlgorithm might be removed.")]
#endif
        public abstract override HashAlgorithm CreateDigest();
    }
}
