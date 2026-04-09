// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public partial class ECDsa : ECAlgorithm
    {
        public static new partial ECDsa Create()
        {
            throw new PlatformNotSupportedException();
        }

        public static partial ECDsa Create(ECCurve curve)
        {
            throw new PlatformNotSupportedException();
        }

        public static partial ECDsa Create(ECParameters parameters)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
