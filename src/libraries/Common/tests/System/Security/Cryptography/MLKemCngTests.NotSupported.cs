// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Security.Cryptography.Tests
{
#if NET || NETSTANDARD2_1_OR_GREATER
    [PlatformSpecific(~TestPlatforms.Windows)]
    public sealed class MLKemCngNotSupportedTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        public static void MLKemCng_NotSupportedOnNonWindowsPlatforms()
        {
            // We cannot actually construct a CngKey on non-Windows platforms, so this cheats by just instantiating
            // a bogus object. We don't need the object to do anything. We shouldn't touch it before the platform check.
            CngKey key = (CngKey)RuntimeHelpers.GetUninitializedObject(typeof(CngKey));
            Assert.Throws<PlatformNotSupportedException>(() => new MLKemCng(key));
        }
    }
#endif
}
