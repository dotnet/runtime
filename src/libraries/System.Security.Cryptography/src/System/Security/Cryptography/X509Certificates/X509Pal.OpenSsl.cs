// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class X509Pal
    {
        private static partial IX509Pal BuildSingleton()
        {
            return new OpenSslX509Encoder();
        }
    }
}
