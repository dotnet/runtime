// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates
{
    internal partial class X509Pal : IX509Pal
    {
        private static partial IX509Pal BuildSingleton()
        {
            return new X509Pal();
        }
    }
}
