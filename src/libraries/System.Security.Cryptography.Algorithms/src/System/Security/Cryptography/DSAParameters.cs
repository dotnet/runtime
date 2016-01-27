// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    public struct DSAParameters
    {
        public byte[] P;
        public byte[] Q;
        public byte[] G;
        public byte[] Y;
        public byte[] J;
        public byte[] X;
        public byte[] Seed;
        public int Counter;
    }
}
