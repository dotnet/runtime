// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Runtime.Loader.Tests
{
    public partial class AssemblyLoadContextTest
    {
        private const string TestAssembly = "System.Runtime.Loader.Test.Assembly";
        private const string TestAssembly2 = "System.Runtime.Loader.Test.Assembly2";

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
            Assert.Throws<FileNotFoundException>(() => AssemblyLoadContext.GetAssemblyName("Non.Existing.Assembly.dll"));
        }

        [Fact]
        public static void GetAssemblyNameTest_NullParameter()
        {
            Assert.Throws<ArgumentNullException>(() => AssemblyLoadContext.GetAssemblyName(null));
        }

        [Fact]
        public static void LoadFromAssemblyPath_PartiallyQualifiedPath_ThrowsArgumentException()
        {
            string path = Path.Combine("foo", "bar.dll");
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>("assemblyPath", () => (new AssemblyLoadContext("alc")).LoadFromAssemblyPath(path));
            Assert.Contains(path, ex.Message);
        }

        [Fact]
        public static void LoadFromNativeImagePath_PartiallyQualifiedPath_ThrowsArgumentException()
        {
            string path = Path.Combine("foo", "bar.dll");
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>("nativeImagePath", () => (new AssemblyLoadContext("alc")).LoadFromNativeImagePath(path, null));
            Assert.Contains(path, ex.Message);
        }

        [Fact]
        public static void LoadFromNativeImagePath_PartiallyQualifiedPath_ThrowsArgumentException2()
        {
            string path = Path.Combine("foo", "bar.dll");
            string rootedPath = Path.GetFullPath(Guid.NewGuid().ToString("N"));
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>("assemblyPath", () => (new AssemblyLoadContext("alc")).LoadFromNativeImagePath(rootedPath, path));
            Assert.Contains(path, ex.Message);
        }

        [Fact]
        [PlatformSpecific(~(TestPlatforms.iOS | TestPlatforms.tvOS))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51893", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public static void LoadAssemblyByPath_ValidUserAssembly()
        {
            var asmName = new AssemblyName(TestAssembly);
            var loadContext = new ResourceAssemblyLoadContext();
            loadContext.LoadBy = LoadBy.Path;

            var asm = loadContext.LoadFromAssemblyName(asmName);

            Assert.NotNull(asm);
            Assert.Same(loadContext, AssemblyLoadContext.GetLoadContext(asm));
            Assert.Contains(asm.DefinedTypes, t => t.Name == "TestClass");
        }

        [Fact]
        [PlatformSpecific(~(TestPlatforms.iOS | TestPlatforms.tvOS))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51893", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public static void LoadAssemblyByStream_ValidUserAssembly()
        {
            var asmName = new AssemblyName(TestAssembly);
            var loadContext = new ResourceAssemblyLoadContext();
            loadContext.LoadBy = LoadBy.Stream;

            var asm = loadContext.LoadFromAssemblyName(asmName);

            Assert.NotNull(asm);
            Assert.Same(loadContext, AssemblyLoadContext.GetLoadContext(asm));
            Assert.Contains(asm.DefinedTypes, t => t.Name == "TestClass");
        }

        [Fact]
        public static void LoadFromAssemblyName_AssemblyNotFound()
        {
            var asmName = new AssemblyName("Non.Existing.Assembly.dll");
            var loadContext = new ResourceAssemblyLoadContext();
            loadContext.LoadBy = LoadBy.Path;

            Assert.Throws<FileNotFoundException>(() => loadContext.LoadFromAssemblyName(asmName));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public static void LoadFromAssemblyName_ValidTrustedPlatformAssembly()
        {
            var asmName = typeof(System.Linq.Enumerable).Assembly.GetName();
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
        public static void LoadFromAssemblyName_FallbackToDefaultContext()
        {
            var asmName = typeof(System.Linq.Enumerable).Assembly.GetName();
            var loadContext = new AssemblyLoadContext("FallbackToDefaultContextTest");

            // This should not have any special handlers, so it should just find the version in the default context
            var asm = loadContext.LoadFromAssemblyName(asmName);
            Assert.NotNull(asm);
            var loadedContext = AssemblyLoadContext.GetLoadContext(asm);
            Assert.NotNull(loadedContext);
            Assert.Same(AssemblyLoadContext.Default, loadedContext);
            Assert.NotEqual(loadContext, loadedContext);
            Assert.Same(typeof(System.Linq.Enumerable).Assembly, asm);
        }

        [Fact]
        [PlatformSpecific(~(TestPlatforms.iOS | TestPlatforms.tvOS))]
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
            var asm = typeof(System.Linq.Enumerable).GetTypeInfo().Assembly;
            var context = AssemblyLoadContext.GetLoadContext(asm);

            Assert.NotNull(context);
            Assert.Same(AssemblyLoadContext.Default, context);
        }

        [Fact]
        public static void GetLoadContextTest_SystemPrivateCorelibAssembly()
        {
            // System.Private.Corelib is a special case
            // `int` is defined in S.P.C
            var asm = typeof(int).Assembly;
            var context = AssemblyLoadContext.GetLoadContext(asm);

            Assert.NotNull(context);
            Assert.Same(AssemblyLoadContext.Default, context);
        }

        [Fact]
        public static void DefaultAssemblyLoadContext_Properties()
        {
            AssemblyLoadContext alc = AssemblyLoadContext.Default;

            Assert.False(alc.IsCollectible);

            Assert.Equal("Default", alc.Name);
            Assert.Contains("\"Default\"", alc.ToString());
            Assert.Contains("System.Runtime.Loader.DefaultAssemblyLoadContext", alc.ToString());
            Assert.Contains(alc, AssemblyLoadContext.All);
            Assert.Contains(typeof(int).Assembly, alc.Assemblies);
        }

        [Fact]
        public static void PublicConstructor_Default()
        {
            AssemblyLoadContext alc = new AssemblyLoadContext("PublicConstructor");

            Assert.False(alc.IsCollectible);

            Assert.Equal("PublicConstructor", alc.Name);
            Assert.Contains("PublicConstructor", alc.ToString());
            Assert.Contains("System.Runtime.Loader.AssemblyLoadContext", alc.ToString());
            Assert.Contains(alc, AssemblyLoadContext.All);
            Assert.Empty(alc.Assemblies);
        }

        [Theory]
        [InlineData("AssemblyLoadContextCollectible", true)]
        [InlineData("AssemblyLoadContextNonCollectible", false)]
        public static void PublicConstructor_Theory(string name, bool isCollectible)
        {
            AssemblyLoadContext alc = new AssemblyLoadContext(name, isCollectible);

            Assert.Equal(isCollectible, alc.IsCollectible);

            Assert.Equal(name, alc.Name);
            Assert.Contains(name, alc.ToString());
            Assert.Contains("System.Runtime.Loader.AssemblyLoadContext", alc.ToString());
            Assert.Contains(alc, AssemblyLoadContext.All);
            Assert.Empty(alc.Assemblies);
        }

        [Fact]
        public static void SubclassAssemblyLoadContext_Properties()
        {
            AssemblyLoadContext alc = new ResourceAssemblyLoadContext();

            Assert.False(alc.IsCollectible);
            Assert.Null(alc.Name);
            Assert.Contains("\"\"", alc.ToString());
            Assert.Contains(typeof(ResourceAssemblyLoadContext).ToString(), alc.ToString());
            Assert.Contains(alc, AssemblyLoadContext.All);
            Assert.Empty(alc.Assemblies);
        }

        class RefEmitLoadContext : AssemblyLoadContext
        {
            protected override Assembly? Load(AssemblyName assemblyName)
            {
                return AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31804", TestRuntimes.Mono)]
        public static void LoadRefEmitAssembly()
        {
            RefEmitLoadContext alc = new();
            alc.Resolving += (sender, assembly) => { Assert.Fail("Resolving event not expected"); return null; };
            Exception error = Assert.Throws<FileLoadException>(() => alc.LoadFromAssemblyName(new AssemblyName("MyAssembly")));
            Assert.IsType<InvalidOperationException>(error.InnerException);
        }

        class NonRuntimeAssemblyContext : AssemblyLoadContext
        {
            class NonRuntimeAssembly : Assembly
            {
                private AssemblyName _name;

                public NonRuntimeAssembly(AssemblyName name) => _name = name;

                public override AssemblyName GetName(bool copiedName) => _name;
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                return new NonRuntimeAssembly(assemblyName);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsAssemblyLoadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31804", TestRuntimes.Mono)]
        public static void LoadNonRuntimeAssembly()
        {
            NonRuntimeAssemblyContext alc = new();
            alc.Resolving += (sender, assembly) => { Assert.Fail("Resolving event not expected"); return null; };
            Exception error = Assert.Throws<FileLoadException>(() => alc.LoadFromAssemblyName(new AssemblyName("MyAssembly")));
            Assert.IsType<InvalidOperationException>(error.InnerException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsAssemblyLoadingSupported))]
        [ActiveIssue("Error message format is CoreCLR-specific", TestRuntimes.Mono)]
        public static void InvalidCastException_GenericTypeArg_ShowsDifferingAssemblyInfo()
        {
            var alc = new AssemblyLoadContext("TestALC");
            Assembly alcAssembly = alc.LoadFromAssemblyPath(typeof(AssemblyLoadContextTest).Assembly.Location);

            // Get the same-named type from the custom ALC's copy of this assembly.
            Type alcType = alcAssembly.GetType(typeof(InvalidCastSharedType).FullName);

            // StrongBox<T> is from System.Private.CoreLib - same in all contexts.
            // The generic argument types come from different ALCs.
            Type boxType = typeof(StrongBox<>).MakeGenericType(alcType);
            object instance = Activator.CreateInstance(boxType);

            // Cast to StrongBox<InvalidCastSharedType> where InvalidCastSharedType is
            // from this assembly's Default ALC - should throw InvalidCastException.
            var ice = Assert.Throws<InvalidCastException>(() =>
            {
                StrongBox<InvalidCastSharedType> _ = (StrongBox<InvalidCastSharedType>)instance;
            });

            // The message should mention the generic argument and its differing ALCs.
            // Before the fix, it would only report the outer type's assembly
            // (System.Private.CoreLib from the Default context for both),
            // making the message unhelpful.
            // After the fix, it reports the generic argument's assembly and ALC context.
            if (!ice.Message.Contains("Debugging resource strings are unavailable"))
            {
                Assert.Contains("generic argument", ice.Message);
                Assert.Contains(nameof(InvalidCastSharedType), ice.Message);
                Assert.Contains("Default", ice.Message);
                Assert.Contains("TestALC", ice.Message);
            }
        }
    }

    public class InvalidCastSharedType
    {
    }
}
