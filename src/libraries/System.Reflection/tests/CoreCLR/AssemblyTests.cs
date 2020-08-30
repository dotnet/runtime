// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/39650", TestPlatforms.Browser)]
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
        public void DynamicAssembly_Location_ThrowsNotSupportedException()
        {
            AssemblyBuilder builder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("dynamic"), AssemblyBuilderAccess.Run);
            Assert.Throws<NotSupportedException>(() => builder.Location);
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
                return LoadFromStream(GetExecutingAssembly().GetManifestResourceStream(assemblyName.Name + ".dll"));
            }
        }
    }
}
