// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography.Xml
{
    internal sealed class RSAPKCS1SHA256SignatureDescription : RSAPKCS1SignatureDescription
    {
        public RSAPKCS1SHA256SignatureDescription() : base("SHA256")
        {
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2046:AnnotationsMustMatchBase",
            Justification = "This derived implementation doesn't require unreferenced code, like the base does.")]
        public sealed override HashAlgorithm CreateDigest()
        {
            return SHA256.Create();
        }
    }
}
