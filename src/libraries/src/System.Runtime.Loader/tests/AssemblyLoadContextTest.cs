// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace System.Runtime.Loader.Tests
{
    public class AssemblyLoadContextTest
    {
        private const string TestAssembly = "System.Runtime.Loader.Test.Assembly";

        [ActiveIssue(/* dotnet/coreclr */ 1187, PlatformID.Windows)] // dependency on coreclr behavior, waiting for new coreclr
        [Fact]
        public static void GetAssemblyNameTest_ValidAssembly()
        {
            var expectedName = typeof(ISet<>).GetTypeInfo().Assembly.GetName();
            var actualAsmName = AssemblyLoadContext.GetAssemblyName("System.Runtime.dll");
            Assert.Equal(expectedName.FullName, actualAsmName.FullName);

            // Verify that the AssemblyName returned by GetAssemblyName can be used to load an assembly. System.Runtime would
            // already be loaded, but this is just verifying it does not throw some other unexpected exception.
            var asm = Assembly.Load(actualAsmName);
            Assert.NotNull(asm);
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
            var asmName = AssemblyLoadContext.GetAssemblyName("System.Runtime.dll");
            var loadContext = new CustomTPALoadContext();

            // Usage of TPA and AssemblyLoadContext is mutually exclusive, you cannot use both.
            // Since the premise is that you either want to use the default binding mechanism (via coreclr TPA binder) 
            // or supply your own (via AssemblyLoadContext) for your own assemblies.
            Assert.Throws(typeof(FileLoadException), 
                () => loadContext.LoadFromAssemblyName(asmName));
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

        [Fact]
        public static void InitializeDefaultContextTest()
        {
            // The coreclr binding model will become locked upon loading the first assembly that is not on the TPA list, or
            // upon initializing the default context for the first time. For this test, test assemblies are located alongside
            // corerun, and hence will be on the TPA list. So, we should be able to set the default context once successfully,
            // and fail on the second try.

            var loadContext = new ResourceAssemblyLoadContext();
            AssemblyLoadContext.InitializeDefaultContext(loadContext);
            Assert.Equal(loadContext, AssemblyLoadContext.Default);

            loadContext = new ResourceAssemblyLoadContext();
            Assert.Throws(typeof(InvalidOperationException), 
                () => AssemblyLoadContext.InitializeDefaultContext(loadContext));
        }
    }
}
