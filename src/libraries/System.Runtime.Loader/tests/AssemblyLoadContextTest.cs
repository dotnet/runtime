// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace System.Runtime.Loader.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "AssemblyLoadContext not supported on .Net Native")]
    public class AssemblyLoadContextTest
    {
        private const string TestAssembly = "System.Runtime.Loader.Test.Assembly";

        [Fact]
        public static void GetAssemblyNameTest_ValidAssembly()
        {
            var expectedName = typeof(AssemblyLoadContextTest).Assembly.GetName();
            var actualAsmName = AssemblyLoadContext.GetAssemblyName("System.Runtime.Loader.Tests.dll");
            Assert.Equal(expectedName.FullName, actualAsmName.FullName);

            // Verify that the AssemblyName returned by GetAssemblyName can be used to load an assembly. System.Runtime would
            // already be loaded, but this is just verifying it does not throw some other unexpected exception.
            var asm = Assembly.Load(actualAsmName);
            Assert.NotNull(asm);
            Assert.Equal(asm, typeof(AssemblyLoadContextTest).Assembly);
        }

        [Fact]
        public static void GetAssemblyNameTest_AssemblyNotFound()
        {            
            Assert.Throws(typeof(FileNotFoundException), 
                () => AssemblyLoadContext.GetAssemblyName("Non.Existing.Assembly.dll"));
        }

        [Fact]
        public static void GetAssemblyNameTest_NullParameter()
        {               
            Assert.Throws(typeof(ArgumentNullException), 
                () => AssemblyLoadContext.GetAssemblyName(null));
        }

        [Fact]
        public static void LoadAssemblyByPath_ValidUserAssembly()
        {            
            var asmName = new AssemblyName(TestAssembly);
            var loadContext = new ResourceAssemblyLoadContext();
            loadContext.LoadBy = LoadBy.Path;

            var asm = loadContext.LoadFromAssemblyName(asmName);

            Assert.NotNull(asm);
            Assert.True(asm.DefinedTypes.Any(t => t.Name == "TestClass"));
        }       

        [Fact]
        public static void LoadAssemblyByStream_ValidUserAssembly()
        {
            var asmName = new AssemblyName(TestAssembly);
            var loadContext = new ResourceAssemblyLoadContext();
            loadContext.LoadBy = LoadBy.Stream;

            var asm = loadContext.LoadFromAssemblyName(asmName);

            Assert.NotNull(asm);
            Assert.True(asm.DefinedTypes.Any(t => t.Name == "TestClass"));
        }

        [Fact]
        public static void LoadFromAssemblyName_AssemblyNotFound()
        {
            var asmName = new AssemblyName("Non.Existing.Assembly.dll");
            var loadContext = new ResourceAssemblyLoadContext();
            loadContext.LoadBy = LoadBy.Path;

            Assert.Throws(typeof(FileNotFoundException), 
                () => loadContext.LoadFromAssemblyName(asmName));
        }

        [Fact]
        public static void LoadFromAssemblyName_ValidTrustedPlatformAssembly()
        {
            var asmName = typeof(ISet<>).Assembly.GetName();
            asmName.CodeBase = null;
            var loadContext = new CustomTPALoadContext();

            // We should be able to override (and thus, load) assemblies that were
            // loaded in TPA load context.
            var asm = loadContext.LoadFromAssemblyName(asmName);
            Assert.NotNull(asm);
            var loadedContext = AssemblyLoadContext.GetLoadContext(asm);
            Assert.NotNull(loadedContext);
            Assert.Same(loadContext, loadedContext);
        }

        [Fact]
        public static void GetLoadContextTest_ValidUserAssembly()
        {
            var asmName = new AssemblyName(TestAssembly);
            var expectedContext = new ResourceAssemblyLoadContext();
            expectedContext.LoadBy = LoadBy.Stream;

            var asm = expectedContext.LoadFromAssemblyName(asmName);
            var actualContext = AssemblyLoadContext.GetLoadContext(asm);

            Assert.Equal(expectedContext, actualContext);
        }

        [Fact]
        public static void GetLoadContextTest_ValidTrustedPlatformAssembly()
        {
            var asm = typeof(ISet<>).GetTypeInfo().Assembly;
            var context = AssemblyLoadContext.GetLoadContext(asm);

            Assert.NotNull(context);
        }
    }
}
