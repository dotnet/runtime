// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Reflection.Metadata;
using System.Reflection;

namespace System.Runtime.Loader.Tests
{
    public unsafe class AssemblyExtensionsTest
    {
        [Fact]
        public void TryGetRawMetadata()
        {
            bool supportsRawMetadata = PlatformDetection.IsNotMonoRuntime && PlatformDetection.IsNotNativeAot;

            Assembly assembly = typeof(AssemblyExtensionsTest).Assembly;
            bool hasMetadata = assembly.TryGetRawMetadata(out byte* blob, out int length);

            Assert.Equal(supportsRawMetadata, hasMetadata);
            Assert.Equal(supportsRawMetadata, blob != null);
            Assert.Equal(supportsRawMetadata, length > 0);
        }
    }
}
