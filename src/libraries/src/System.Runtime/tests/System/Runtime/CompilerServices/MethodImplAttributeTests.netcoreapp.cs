// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static class MethodImplAttributeTests
    {
        [Fact]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void AggressiveOptimizationTest()
        {
            MethodImplAttributes implAttributes = MethodBase.GetCurrentMethod().MethodImplementationFlags;
            Assert.Equal(MethodImplAttributes.AggressiveOptimization, implAttributes);
            Assert.Equal(MethodImplOptions.AggressiveOptimization, (MethodImplOptions)implAttributes);
        }
    }
}
