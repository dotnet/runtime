// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public partial class DSA : AsymmetricAlgorithm
    {
        public static new DSA Create()
        {
            return new DSAImplementation.DSAAndroid();
        }
    }
}