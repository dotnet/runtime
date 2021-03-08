// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test.Cryptography
{
    internal static class PlatformSupport
    {
        // Platforms that support OpenSSL - all Unix except OSX and Android
        internal const TestPlatforms OpenSSL = TestPlatforms.AnyUnix & ~(TestPlatforms.OSX | TestPlatforms.Android);

        // Whether or not the current platform supports RC2
        internal static readonly bool IsRC2Supported = !PlatformDetection.IsAndroid;
    }
}
