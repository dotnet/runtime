// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography.Xml
{
    internal sealed class DSASignatureDescription : SignatureDescription
    {
        private const string HashAlgorithm = "SHA1";

        public DSASignatureDescription()
        {
            KeyAlgorithm = typeof(DSA).AssemblyQualifiedName;
            FormatterAlgorithm = typeof(DSASignatureFormatter).AssemblyQualifiedName;
            DeformatterAlgorithm = typeof(DSASignatureDeformatter).AssemblyQualifiedName;
            DigestAlgorithm = "SHA1";
        }

#if NETCOREAPP
        [RequiresUnreferencedCode("CreateDeformatter is not trim compatible because the algorithm implementation referenced by DeformatterAlgorithm might be removed.")]
#endif
        public sealed override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
        {
            var item = (AsymmetricSignatureDeformatter)CryptoConfig.CreateFromName(DeformatterAlgorithm!)!;
            item.SetKey(key);
            item.SetHashAlgorithm(HashAlgorithm);
            return item;
        }

#if NETCOREAPP
        [RequiresUnreferencedCode("CreateFormatter is not trim compatible because the algorithm implementation referenced by FormatterAlgorithm might be removed.")]
#endif
        public sealed override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
        {
            var item = (AsymmetricSignatureFormatter)CryptoConfig.CreateFromName(FormatterAlgorithm!)!;
            item.SetKey(key);
            item.SetHashAlgorithm(HashAlgorithm);
            return item;
        }

        [SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 needed for compat.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2046:AnnotationsMustMatchBase",
            Justification = "This derived implementation doesn't require unreferenced code, like the base does.")]
        public sealed override HashAlgorithm CreateDigest()
        {
            return SHA1.Create();
        }
    }
}
