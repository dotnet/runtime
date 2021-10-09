// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    using RC2 = System.Security.Cryptography.RC2;

    public interface IRC2Provider
    {
        RC2 Create();
        bool OneShotSupported { get; }
    }

    public static partial class RC2Factory
    {
        public static RC2 Create()
        {
            return s_provider.Create();
        }

        public static bool OneShotSupported => s_provider.OneShotSupported;

        public static bool IsSupported { get; } = Test.Cryptography.PlatformSupport.IsRC2Supported;
    }
}
