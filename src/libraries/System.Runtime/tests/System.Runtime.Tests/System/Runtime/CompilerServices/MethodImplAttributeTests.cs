// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static class MethodImplAttributeTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/133617", typeof(PlatformDetection), nameof(PlatformDetection.IsWasmReadyToRun))]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void AggressiveOptimizationTest()
        {
            MethodImplAttributes implAttributes = MethodBase.GetCurrentMethod().MethodImplementationFlags;
            if (implAttributes.HasFlag(MethodImplAttributes.NoInlining))
            {
                // when the assembly was processed with ILStrip, the NoInlining flag is set
                Assert.Equal(MethodImplAttributes.AggressiveOptimization | MethodImplAttributes.NoInlining,  implAttributes);
                Assert.Equal(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining, (MethodImplOptions)implAttributes);
            }
            else
            {
                Assert.Equal(MethodImplAttributes.AggressiveOptimization, implAttributes);
                Assert.Equal(MethodImplOptions.AggressiveOptimization, (MethodImplOptions)implAttributes);
            }
        }
    }
}
