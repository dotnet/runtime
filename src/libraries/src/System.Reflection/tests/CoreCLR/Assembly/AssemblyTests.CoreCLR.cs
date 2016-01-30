// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyTests
    {
        [Fact]
        public void CurrentAssemblyHasLocation()
        {
            string location = ThisAssembly().Location;

            Assert.NotNull(location);
            Assert.NotEmpty(location);

            string expectedDir = AppContext.BaseDirectory;
            string actualDir = Path.GetDirectoryName(location);

            // Check that neither are relative
            Assert.True(Path.IsPathRooted(expectedDir));
            Assert.True(Path.IsPathRooted(actualDir));

            // normalize paths before comparison
            expectedDir = Path.GetFullPath(expectedDir).TrimEnd(Path.DirectorySeparatorChar);
            actualDir = Path.GetFullPath(actualDir).TrimEnd(Path.DirectorySeparatorChar);

            Assert.Equal(expectedDir, actualDir, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("System.Reflection.CoreCLR.Tests.dll", Path.GetFileName(location), StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void LoadFromStreamHasEmptyLocation()
        {
            Assembly assembly = new TestStreamLoadContext().LoadFromAssemblyName(new AssemblyName("TinyAssembly"));
            string location = assembly.Location;

            Assert.NotNull(location);
            Assert.Empty(location);
        }

        [Fact]
        public void DynamicAssemblyThrowsNotSupportedException()
        {
            AssemblyBuilder builder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("dynamic"), AssemblyBuilderAccess.Run);
            Assert.Throws<NotSupportedException>(() => builder.Location); // wish this were Assert.Empty, but it's long been this way...
        }

        [Fact]
        public void TestExeEntryPoint()
        {
            MethodInfo entryPoint = typeof(TestExe).GetTypeInfo().Assembly.EntryPoint;
            Assert.NotNull(entryPoint);
            Assert.Equal(42, entryPoint.Invoke(null, null));
        }

        private static Assembly ThisAssembly()
        {
            return typeof(AssemblyTests).GetTypeInfo().Assembly;
        }

        private sealed class TestStreamLoadContext : AssemblyLoadContext
        {
            protected override Assembly Load(AssemblyName assemblyName)
            {
                return LoadFromStream(ThisAssembly().GetManifestResourceStream(assemblyName.Name + ".dll"));
            }
        }
    }
}
