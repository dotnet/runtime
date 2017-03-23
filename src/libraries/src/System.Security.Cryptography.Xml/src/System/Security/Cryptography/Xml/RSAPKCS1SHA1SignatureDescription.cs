// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Security.Cryptography.Xml
{
    internal class RSAPKCS1SHA1SignatureDescription : RSAPKCS1SignatureDescription
    {
        public RSAPKCS1SHA1SignatureDescription() : base("SHA1")
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 needed for compat.")]
        public sealed override HashAlgorithm CreateDigest()
        {
            return SHA1.Create();
        }
    }
}
