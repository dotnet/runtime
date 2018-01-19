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
    }
}
