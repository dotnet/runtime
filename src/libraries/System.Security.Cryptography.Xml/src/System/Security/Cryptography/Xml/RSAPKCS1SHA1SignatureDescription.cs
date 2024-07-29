// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography.Xml
{
    internal sealed class RSAPKCS1SHA1SignatureDescription : RSAPKCS1SignatureDescription
    {
        public RSAPKCS1SHA1SignatureDescription() : base("SHA1")
        {
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
