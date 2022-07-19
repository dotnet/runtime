// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    public class SignatureDescription
    {
        public string? KeyAlgorithm { get; set; }
        public string? DigestAlgorithm { get; set; }
        public string? FormatterAlgorithm { get; set; }
        public string? DeformatterAlgorithm { get; set; }

        public SignatureDescription()
        {
        }

        public SignatureDescription(SecurityElement el)
        {
            ArgumentNullException.ThrowIfNull(el);

            KeyAlgorithm = el.SearchForTextOfTag("Key");
            DigestAlgorithm = el.SearchForTextOfTag("Digest");
            FormatterAlgorithm = el.SearchForTextOfTag("Formatter");
            DeformatterAlgorithm = el.SearchForTextOfTag("Deformatter");
        }

        [RequiresUnreferencedCode("CreateDeformatter is not trim compatible because the algorithm implementation referenced by DeformatterAlgorithm might be removed.")]
        public virtual AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
        {
            AsymmetricSignatureDeformatter? item = (AsymmetricSignatureDeformatter?)CryptoConfig.CreateFromName(DeformatterAlgorithm!);
            item!.SetKey(key);
            return item;
        }

        [RequiresUnreferencedCode("CreateFormatter is not trim compatible because the algorithm implementation referenced by FormatterAlgorithm might be removed.")]
        public virtual AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
        {
            AsymmetricSignatureFormatter? item = (AsymmetricSignatureFormatter?)CryptoConfig.CreateFromName(FormatterAlgorithm!);
            item!.SetKey(key);
            return item;
        }

        [RequiresUnreferencedCode("CreateDigest is not trim compatible because the algorithm implementation referenced by DigestAlgorithm might be removed.")]
        public virtual HashAlgorithm? CreateDigest()
        {
            return (HashAlgorithm?)CryptoConfig.CreateFromName(DigestAlgorithm!);
        }
    }
}
