// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "AssemblyLoadContext not available in NetFx")]
    public class IsCollectibleTests
    {
        static public string asmNameString = "TestCollectibleAssembly";
        static public string asmPath = Path.Combine(Environment.CurrentDirectory, "TestCollectibleAssembly.dll");

        static public Func<AssemblyName, Assembly> assemblyResolver = (asmName) => 
            asmName.Name == asmNameString ? Assembly.LoadFrom(asmPath) : null;
        
        static public Func<AssemblyName, Assembly> collectibleAssemblyResolver(AssemblyLoadContext alc) => 
            (asmName) => 
                asmName.Name == asmNameString ? alc.LoadFromAssemblyPath(asmPath) : null;

        static public Func<Assembly, string, bool, Type> typeResolver(bool shouldThrowIfNotFound) => 
            (asm, simpleTypeName, isCaseSensitive) => asm == null ? 
                Type.GetType(simpleTypeName, shouldThrowIfNotFound, isCaseSensitive) : 
                asm.GetType(simpleTypeName, shouldThrowIfNotFound, isCaseSensitive);

        [Fact]
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

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
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

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
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

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Theory]
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

                return RemoteExecutor.SuccessExitCode;
            }, memberName).Dispose();
        }

        [Theory]
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

                return RemoteExecutor.SuccessExitCode;
            }, memberName).Dispose();
        }

        [Theory]
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

                return RemoteExecutor.SuccessExitCode;
            }, memberName).Dispose();
        }

        [Theory]
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

                return RemoteExecutor.SuccessExitCode;
            }, memberName).Dispose();
        }

        [Fact]
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

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }
    }
}
