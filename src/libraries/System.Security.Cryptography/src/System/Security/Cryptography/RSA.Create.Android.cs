// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public partial class RSA : AsymmetricAlgorithm
    {
        public static new partial RSA Create() => new RSAImplementation.RSAAndroid();
    }
}
