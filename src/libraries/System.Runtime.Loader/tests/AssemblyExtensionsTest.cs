// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Metadata
{
    public class AssemblyExtensionsTest
    {
        class NonRuntimeAssembly : Assembly
        {
        }

        [Fact]
        public static void ApplyUpdateInvalidParameters()
        {
            // Dummy delta arrays
            var metadataDelta = new byte[20];
            var ilDelta = new byte[20];

            // Assembly can't be null
            Assert.Throws<ArgumentNullException>("assembly", () =>
                AssemblyExtensions.ApplyUpdate(null, new ReadOnlySpan<byte>(metadataDelta), new ReadOnlySpan<byte>(ilDelta), ReadOnlySpan<byte>.Empty));

            // Tests fail on non-runtime assemblies
            Assert.Throws<ArgumentException>(() =>
                AssemblyExtensions.ApplyUpdate(new NonRuntimeAssembly(), new ReadOnlySpan<byte>(metadataDelta), new ReadOnlySpan<byte>(ilDelta), ReadOnlySpan<byte>.Empty));

            if (PlatformDetection.IsNotMonoRuntime && PlatformDetection.IsArmOrArm64Process)
            {
                // Not implemented on .NET Core arm and arm64
                Assert.Throws<NotImplementedException>(() =>
                    AssemblyExtensions.ApplyUpdate(typeof(AssemblyExtensions).Assembly, new ReadOnlySpan<byte>(metadataDelta), new ReadOnlySpan<byte>(ilDelta), ReadOnlySpan<byte>.Empty));
            }
            else
            {
                // Tests that this assembly isn't not editable
                Assert.Throws<InvalidOperationException>(() =>
                    AssemblyExtensions.ApplyUpdate(typeof(AssemblyExtensions).Assembly, new ReadOnlySpan<byte>(metadataDelta), new ReadOnlySpan<byte>(ilDelta), ReadOnlySpan<byte>.Empty));
            }
        }
    }
}
