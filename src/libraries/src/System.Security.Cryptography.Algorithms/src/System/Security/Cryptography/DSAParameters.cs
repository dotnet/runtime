// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    // DSAParameters is serializable so that one could pass the public parameters
    // across a remote call, but we explicitly make the private key X non-serializable
    // so you cannot accidently send it along with the public parameters.
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
