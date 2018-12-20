// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static partial class RuntimeFeatureTests
    {
        [Fact]
        public static void PortablePdb()
        {
            Assert.True(RuntimeFeature.IsSupported("PortablePdb"));
        }

        [Fact]
        public static void DynamicCode()
        {
            Assert.Equal(RuntimeFeature.IsDynamicCodeSupported, RuntimeFeature.IsSupported("IsDynamicCodeSupported"));
            Assert.Equal(RuntimeFeature.IsDynamicCodeCompiled, RuntimeFeature.IsSupported("IsDynamicCodeCompiled"));

            if (RuntimeFeature.IsDynamicCodeCompiled)
            {
                Assert.True(RuntimeFeature.IsDynamicCodeSupported);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNative))]
        public static void DynamicCode_Jit()
        {
            Assert.True(RuntimeFeature.IsDynamicCodeSupported);
            Assert.True(RuntimeFeature.IsDynamicCodeCompiled);
        }
    }
}
