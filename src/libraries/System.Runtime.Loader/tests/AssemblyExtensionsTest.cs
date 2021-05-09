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

            // Tests that this assembly isn't not editable
            Assert.Throws<InvalidOperationException>(() =>
                AssemblyExtensions.ApplyUpdate(typeof(AssemblyExtensions).Assembly, new ReadOnlySpan<byte>(metadataDelta), new ReadOnlySpan<byte>(ilDelta), ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public void GetApplyUpdateCapabilitiesIsCallable()
        {
            var ty = typeof(System.Reflection.Metadata.AssemblyExtensions);
            var mi = ty.GetMethod("GetApplyUpdateCapabilities", BindingFlags.NonPublic | BindingFlags.Static, Array.Empty<Type>());

            Assert.NotNull(mi);

            var result = mi.Invoke(null, null);

            Assert.NotNull(result);
            Assert.Equal(typeof(string), result.GetType());
        }
    }
}
