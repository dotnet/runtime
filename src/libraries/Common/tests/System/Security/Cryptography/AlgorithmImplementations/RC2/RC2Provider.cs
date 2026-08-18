// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    using RC2 = System.Security.Cryptography.RC2;

    public abstract class RC2Provider
    {
        public abstract RC2 Create();

        public abstract bool OneShotSupported { get; }

        public static bool IsSupported { get; } = Test.Cryptography.PlatformSupport.IsRC2Supported;
    }
}
