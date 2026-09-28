// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.Loader;
using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void CurrentLocation_HasLocaton()
        {
            string location = GetExecutingAssembly().Location;
            Assert.NotEmpty(location);
            string actualDir = Path.GetDirectoryName(location);

            // Check that location is not relative
            Assert.True(Path.IsPathRooted(actualDir));
            Assert.Equal("System.Reflection.CoreCLR.Tests.dll", Path.GetFileName(location), StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void LoadFromStream_Location_IsEmpty()
        {
            Assembly assembly = new TestStreamLoadContext().LoadFromAssemblyName(new AssemblyName("TinyAssembly"));
            Assert.Empty(assembly.Location);
        }

        [Fact]
        public void LoadFromStream_EmptyStream_ThrowsBadImageFormatException()
        {
            using (var emptyStream = new MemoryStream())
            {
#pragma warning disable IL2026 // https://github.com/dotnet/runtime/issues/126862
                BadImageFormatException ex = Assert.Throws<BadImageFormatException>(
                    () => AssemblyLoadContext.Default.LoadFromStream(emptyStream));
#pragma warning restore IL2026
                Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void EntryPoint()
        {
            MethodInfo entryPoint = typeof(TestExe).GetTypeInfo().Assembly.EntryPoint;
            Assert.NotNull(entryPoint);
            Assert.Equal(42, entryPoint.Invoke(null, null));
        }

        private static Assembly GetExecutingAssembly() => typeof(AssemblyTests).GetTypeInfo().Assembly;

        private sealed class TestStreamLoadContext : AssemblyLoadContext
        {
            protected override Assembly Load(AssemblyName assemblyName)
            {
#pragma warning disable IL2026 // https://github.com/dotnet/runtime/issues/126862
                return LoadFromStream(GetExecutingAssembly().GetManifestResourceStream(assemblyName.Name + ".dll"));
#pragma warning restore IL2026
            }
        }
    }
}
