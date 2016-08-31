// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Security.Cryptography
{
    // We allow only the public components of an RSAParameters object, the Modulus and Exponent to be serializable.
    [Serializable]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct RSAParameters
    {
        [NonSerialized] public byte[] D;
        [NonSerialized] public byte[] DP;
        [NonSerialized] public byte[] DQ;
        public byte[] Exponent;
        [NonSerialized] public byte[] InverseQ;
        public byte[] Modulus;
        [NonSerialized] public byte[] P;
        [NonSerialized] public byte[] Q;
    }
}
