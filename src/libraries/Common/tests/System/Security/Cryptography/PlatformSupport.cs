// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test.Cryptography
{
    internal static class PlatformSupport
    {
        // Platforms that use Apple Cryptography
        internal const TestPlatforms AppleCrypto = TestPlatforms.OSX | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst;
        internal const TestPlatforms MobileAppleCrypto = TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst;

        // Platforms that support OpenSSL - all Unix except OSX/iOS/tvOS/MacCatalyst and Android
        internal const TestPlatforms OpenSSL = TestPlatforms.AnyUnix & ~(AppleCrypto | TestPlatforms.Android);

        // Whether or not the current platform supports RC2
        internal static readonly bool IsRC2Supported = !PlatformDetection.IsAndroid;
    }
}
