// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Reflection.Tests
{
    public class TestAssemblyLoadContext : AssemblyLoadContext
    {
        public TestAssemblyLoadContext() : base(true) {}
        protected override Assembly Load(AssemblyName assemblyName) => null;
    }

    [ActiveIssue("https://github.com/mono/mono/issues/15142", TestRuntimes.Mono)]
    public class IsCollectibleTests
    {
        public static string asmNameString = "TestCollectibleAssembly";
        public static string asmPath = Path.Combine(Environment.CurrentDirectory, "TestCollectibleAssembly.dll");

        public static Func<AssemblyName, Assembly> assemblyResolver = (asmName) =>
            asmName.Name == asmNameString ? Assembly.LoadFrom(asmPath) : null;

        public static Func<AssemblyName, Assembly> collectibleAssemblyResolver(AssemblyLoadContext alc) =>
            (asmName) =>
                asmName.Name == asmNameString ? alc.LoadFromAssemblyPath(asmPath) : null;

        public static Func<Assembly, string, bool, Type> typeResolver(bool shouldThrowIfNotFound) =>
            (asm, simpleTypeName, isCaseSensitive) => asm == null ?
                Type.GetType(simpleTypeName, shouldThrowIfNotFound, isCaseSensitive) :
                asm.GetType(simpleTypeName, shouldThrowIfNotFound, isCaseSensitive);

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Assembly_IsCollectibleFalse_WhenUsingAssemblyLoad()
        {
            RemoteExecutor.Invoke(() => {
                Assembly asm = Assembly.LoadFrom(asmPath);

                Assert.NotNull(asm);

                Assert.False(asm.IsCollectible);

                AssemblyLoadContext alc = AssemblyLoadContext.GetLoadContext(asm);
                Assert.False(alc.IsCollectible);
                Assert.Equal(AssemblyLoadContext.Default, alc);
                Assert.Equal("Default", alc.Name);
                Assert.Contains("\"Default\"", alc.ToString());
                Assert.Contains("System.Runtime.Loader.DefaultAssemblyLoadContext", alc.ToString());
                Assert.Contains(alc, AssemblyLoadContext.All);
                Assert.Contains(asm, alc.Assemblies);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Assembly_IsCollectibleFalse_WhenUsingAssemblyLoadContext()
        {
            RemoteExecutor.Invoke(() => {
                AssemblyLoadContext alc = new AssemblyLoadContext("Assembly_IsCollectibleFalse_WhenUsingAssemblyLoadContext");

                Assembly asm = alc.LoadFromAssemblyPath(asmPath);

                Assert.NotNull(asm);

                Assert.False(asm.IsCollectible);
                Assert.False(alc.IsCollectible);

                Assert.Equal("Assembly_IsCollectibleFalse_WhenUsingAssemblyLoadContext", alc.Name);
                Assert.Contains("Assembly_IsCollectibleFalse_WhenUsingAssemblyLoadContext", alc.ToString());
                Assert.Contains("System.Runtime.Loader.AssemblyLoadContext", alc.ToString());
                Assert.Contains(alc, AssemblyLoadContext.All);
                Assert.Contains(asm, alc.Assemblies);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Assembly_IsCollectibleTrue_WhenUsingTestAssemblyLoadContext()
        {
            RemoteExecutor.Invoke(() => {
                AssemblyLoadContext alc = new TestAssemblyLoadContext();

                Assembly asm = alc.LoadFromAssemblyPath(asmPath);

                Assert.NotNull(asm);

                Assert.True(asm.IsCollectible);
                Assert.True(alc.IsCollectible);

                Assert.Null(alc.Name);
                Assert.Contains("\"\"", alc.ToString());
                Assert.Contains("System.Reflection.Tests.TestAssemblyLoadContext", alc.ToString());
                Assert.Contains(alc, AssemblyLoadContext.All);
                Assert.Contains(asm, alc.Assemblies);
            }).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("MyField")]
        [InlineData("MyProperty")]
        [InlineData("MyMethod")]
        [InlineData("MyGenericMethod")]
        [InlineData("MyStaticMethod")]
        [InlineData("MyStaticField")]
        [InlineData("MyStaticGenericMethod")]
        public void MemberInfo_IsCollectibleFalse_WhenUsingAssemblyLoad(string memberName)
        {
            RemoteExecutor.Invoke((marshalledName) =>
            {
                Type t1 = Type.GetType(
                    "TestCollectibleAssembly.MyTestClass, TestCollectibleAssembly, Version=1.0.0.0",
                    assemblyResolver,
                    typeResolver(false),
                    true
                );

                Assert.NotNull(t1);

                var member = t1.GetMember(marshalledName).FirstOrDefault();

                Assert.NotNull(member);

                Assert.False(member.IsCollectible);
            }, memberName).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("MyStaticGenericField")]
        [InlineData("MyStaticField")]
        [InlineData("MyStaticGenericMethod")]
        [InlineData("MyStaticMethod")]
        [InlineData("MyGenericField")]
        [InlineData("MyGenericProperty")]
        [InlineData("MyGenericMethod")]
        public void MemberInfoGeneric_IsCollectibleFalse_WhenUsingAssemblyLoad(string memberName)
        {
            RemoteExecutor.Invoke((marshalledName) =>
            {
                Type t1 = Type.GetType(
                    "TestCollectibleAssembly.MyGenericTestClass`1[System.Int32], TestCollectibleAssembly, Version=1.0.0.0",
                    assemblyResolver,
                    typeResolver(false),
                    true
                );

                Assert.NotNull(t1);

                var member = t1.GetMember(marshalledName).FirstOrDefault();

                Assert.NotNull(member);

                Assert.False(member.IsCollectible);
            }, memberName).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("MyField")]
        [InlineData("MyProperty")]
        [InlineData("MyMethod")]
        [InlineData("MyGenericMethod")]
        [InlineData("MyStaticMethod")]
        [InlineData("MyStaticField")]
        [InlineData("MyStaticGenericMethod")]
        public void MemberInfo_IsCollectibleTrue_WhenUsingAssemblyLoadContext(string memberName)
        {
            RemoteExecutor.Invoke((marshalledName) =>
            {
                AssemblyLoadContext alc = new TestAssemblyLoadContext();

                Type t1 = Type.GetType(
                    "TestCollectibleAssembly.MyTestClass, TestCollectibleAssembly, Version=1.0.0.0",
                    collectibleAssemblyResolver(alc),
                    typeResolver(false),
                    true
                );

                Assert.NotNull(t1);

                var member = t1.GetMember(marshalledName).FirstOrDefault();

                Assert.NotNull(member);

                Assert.True(member.IsCollectible);
            }, memberName).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("MyStaticGenericField")]
        [InlineData("MyStaticField")]
        [InlineData("MyStaticGenericMethod")]
        [InlineData("MyStaticMethod")]
        [InlineData("MyGenericField")]
        [InlineData("MyGenericProperty")]
        [InlineData("MyGenericMethod")]
        public void MemberInfoGeneric_IsCollectibleTrue_WhenUsingAssemblyLoadContext(string memberName)
        {
            RemoteExecutor.Invoke((marshalledName) =>
            {
                AssemblyLoadContext alc = new TestAssemblyLoadContext();

                Type t1 = Type.GetType(
                    "TestCollectibleAssembly.MyGenericTestClass`1[System.Int32], TestCollectibleAssembly, Version=1.0.0.0",
                    collectibleAssemblyResolver(alc),
                    typeResolver(false),
                    true
                );

                Assert.NotNull(t1);

                var member = t1.GetMember(marshalledName).FirstOrDefault();

                Assert.NotNull(member);

                Assert.True(member.IsCollectible);
            }, memberName).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GenericWithCollectibleTypeParameter_IsCollectibleTrue_WhenUsingAssemblyLoadContext()
        {
            RemoteExecutor.Invoke(() =>
            {
                AssemblyLoadContext alc = new TestAssemblyLoadContext();

                Type t1 = Type.GetType(
                    "System.Collections.Generic.Dictionary`2[[System.Int32],[TestCollectibleAssembly.MyTestClass, TestCollectibleAssembly, Version=1.0.0.0]]",
                    collectibleAssemblyResolver(alc),
                    typeResolver(false),
                    true
                );

                Assert.NotNull(t1);

                Assert.True(t1.IsCollectible);
            }).Dispose();
        }
    }
}
