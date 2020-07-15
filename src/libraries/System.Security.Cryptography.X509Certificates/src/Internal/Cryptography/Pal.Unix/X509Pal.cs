// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Cryptography.Pal
{
    internal sealed partial class X509Pal
    {
        public static IX509Pal Instance = new OpenSslX509Encoder();

        private X509Pal()
        {
        }
    }
}
