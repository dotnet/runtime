// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    using RC2 = System.Security.Cryptography.RC2;

    public sealed class DefaultRC2Provider : RC2Provider
    {
        public static readonly DefaultRC2Provider Instance = new DefaultRC2Provider();

        private DefaultRC2Provider() { }

        public override RC2 Create() => RC2.Create();

        public override bool OneShotSupported => true;
    }
}
