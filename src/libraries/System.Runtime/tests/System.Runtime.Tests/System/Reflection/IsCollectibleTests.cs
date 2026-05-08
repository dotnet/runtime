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

    public class BasicIsCollectibleTests
    {
        [Fact]
        public void CoreLib_IsCollectibleFalse()
        {
            Assert.False(typeof(object).IsCollectible);
            Assert.False(typeof(Console).Assembly.IsCollectible);
            Assert.False(typeof(Math).Assembly.IsCollectible);
            Assert.False(typeof(string).Assembly.IsCollectible);
            Assert.False(typeof(List<>).Assembly.IsCollectible);
            Assert.False(typeof(object).Assembly.IsCollectible);
            Assert.False(typeof(IDisposable).Assembly.IsCollectible);
            Assert.False(typeof(Span<>).Assembly.IsCollectible);
            Assert.False(typeof(Enumerable).Assembly.IsCollectible);
            Assert.False(typeof(string).GetMethods().First(m => m.Name == "Substring" && m.GetParameters().Length == 1).IsCollectible);
            Assert.False(typeof(Dictionary<,>).Assembly.IsCollectible);
            Assert.False(typeof(object).GetMethod("ToString").IsCollectible);
            Assert.False(typeof(string).GetMethod("Contains", new[] { typeof(string) }).IsCollectible);
            Assert.False(typeof(IntPtr).GetField("Zero").IsCollectible);
            Assert.False(typeof(IntPtr).GetProperty("MaxValue").IsCollectible);
            Assert.False(typeof(IntPtr).GetProperty("MinValue").IsCollectible);
            Assert.False(typeof(DateTime).GetProperty("Now").IsCollectible);
            Assert.False(typeof(AppDomain).GetMethod("GetData").IsCollectible);
            Assert.False(typeof(AppDomain).GetMethod("ToString").IsCollectible);
            Assert.False(typeof(string).GetConstructor(new[] { typeof(char[]), typeof(int), typeof(int) })!.IsCollectible);
            Assert.False(typeof(string).GetConstructor(new[] { typeof(char), typeof(int) })!.IsCollectible);
            Assert.False(typeof(TimeSpan).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) })!.IsCollectible);
            Assert.False(typeof(Uri).GetConstructor(new[] { typeof(string) })!.IsCollectible);
            Assert.False(typeof(AppDomain).GetEvent("AssemblyLoad")!.IsCollectible);
            Assert.False(typeof(AppDomain).GetEvent("ProcessExit")!.IsCollectible);

       }
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MemberInfo_IsCollectibleFalse_WhenUsingAssemblyLoad()
        {
            RemoteExecutor.Invoke(() =>
            {
                Type t1 = Type.GetType(
                    "TestCollectibleAssembly.MyTestClass, TestCollectibleAssembly, Version=1.0.0.0",
                    assemblyResolver,
                    typeResolver(false),
                    true
                );

                Assert.NotNull(t1);

                foreach (var member in t1.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    Assert.False(member.IsCollectible, member.ToString());
                }

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MemberInfoGeneric_IsCollectibleFalse_WhenUsingAssemblyLoad()
        {
            RemoteExecutor.Invoke(() =>
            {
                Type t1 = Type.GetType(
                    "TestCollectibleAssembly.MyGenericTestClass`1[System.Int32], TestCollectibleAssembly, Version=1.0.0.0",
                    assemblyResolver,
                    typeResolver(false),
                    true
                );

                Assert.NotNull(t1);

                foreach (var member in t1.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    Assert.False(member.IsCollectible, member.ToString());
                }

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MemberInfo_IsCollectibleTrue_WhenUsingAssemblyLoadContext()
        {
            RemoteExecutor.Invoke(() =>
            {
                AssemblyLoadContext alc = new TestAssemblyLoadContext();

                Type t1 = Type.GetType(
                    "TestCollectibleAssembly.MyTestClass, TestCollectibleAssembly, Version=1.0.0.0",
                    collectibleAssemblyResolver(alc),
                    typeResolver(false),
                    true
                );

                Assert.NotNull(t1);

                foreach (var member in t1.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    Assert.True(member.IsCollectible, member.ToString());
                }

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MemberInfoGeneric_IsCollectibleTrue_WhenUsingAssemblyLoadContext()
        {
            RemoteExecutor.Invoke(() =>
            {
                AssemblyLoadContext alc = new TestAssemblyLoadContext();

                Type t1 = Type.GetType(
                    "TestCollectibleAssembly.MyGenericTestClass`1[System.Int32], TestCollectibleAssembly, Version=1.0.0.0",
                    collectibleAssemblyResolver(alc),
                    typeResolver(false),
                    true
                );

                Assert.NotNull(t1);

                foreach (var member in t1.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    Assert.True(member.IsCollectible, member.ToString());
                }

            }).Dispose();
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

                foreach (var member in t1.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (member is Type)
                    {
                        continue;
                    }

                    Assert.True(member.IsCollectible, member.ToString());
                }

            }).Dispose();
        }
    }
}
