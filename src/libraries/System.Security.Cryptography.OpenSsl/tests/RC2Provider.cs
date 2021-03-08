// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    using RC2 = System.Security.Cryptography.RC2;

    internal class RC2Provider : IRC2Provider
    {
        public RC2 Create()
        {
            return RC2.Create();
        }
    }

    public partial class RC2Factory
    {
        private static readonly IRC2Provider s_provider = new RC2Provider();
    }
}
