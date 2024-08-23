// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;
using TestAttributes;

[module: Foo]
[module: Complicated(1, Stuff = 2)]

namespace TestAttributes
{
    public class FooAttribute : Attribute
    {
    }

    public class ComplicatedAttribute : Attribute
    {
        public int Stuff
        {
            get;
            set;
        }

        public int Foo
        {
            get;
        }

        public ComplicatedAttribute(int foo)
        {
            Foo = foo;
        }
    }
}

namespace System.Reflection.Tests
{
    public class ModuleTests
    {
        public const string ConstField = "Value";
        public static Module Module => typeof(ModuleTests).Module;
        public static Module TestModule => typeof(TestModule.Dummy).Module;

        [Fact]
        public void TestAssembly()
        {
            Assert.Equal(Assembly.GetExecutingAssembly(), Module.Assembly);
        }

        [Fact]
        public void ModuleHandle()
        {
            Assert.Equal(typeof(PointerTests).Module.ModuleHandle, Module.ModuleHandle);
            Assert.NotEqual(typeof(PointerTests).Module.ModuleHandle, System.ModuleHandle.EmptyHandle);
        }

        [Fact]
        public void CustomAttributes()
        {
            List<CustomAttributeData> customAttributes = Module.CustomAttributes.ToList();
            Assert.True(customAttributes.Count >= 2);
            CustomAttributeData fooAttribute = customAttributes.Single(a => a.AttributeType == typeof(FooAttribute));
            Assert.Equal(typeof(FooAttribute).GetConstructors().First(), fooAttribute.Constructor);
            Assert.Equal(0, fooAttribute.ConstructorArguments.Count);
            Assert.Equal(0, fooAttribute.NamedArguments.Count);
            CustomAttributeData complicatedAttribute = customAttributes.Single(a => a.AttributeType == typeof(ComplicatedAttribute));
            Assert.Equal(typeof(ComplicatedAttribute).GetConstructors().First(), complicatedAttribute.Constructor);
            Assert.Equal(1, complicatedAttribute.ConstructorArguments.Count);
            Assert.Equal(typeof(int), complicatedAttribute.ConstructorArguments[0].ArgumentType);
            Assert.Equal(1, (int)complicatedAttribute.ConstructorArguments[0].Value);
            Assert.Equal(1, complicatedAttribute.NamedArguments.Count);
            Assert.False(complicatedAttribute.NamedArguments[0].IsField);
            Assert.Equal("Stuff", complicatedAttribute.NamedArguments[0].MemberName);
            Assert.Equal(typeof(ComplicatedAttribute).GetProperty("Stuff"), complicatedAttribute.NamedArguments[0].MemberInfo);
            Assert.Equal(typeof(int), complicatedAttribute.NamedArguments[0].TypedValue.ArgumentType);
            Assert.Equal(2, complicatedAttribute.NamedArguments[0].TypedValue.Value);
        }

        [Fact]
        public void FullyQualifiedName()
        {
#if SINGLE_FILE_TEST_RUNNER
            Assert.Equal("<Unknown>", Module.FullyQualifiedName);
#else
            var loc = AssemblyPathHelper.GetAssemblyLocation(Assembly.GetExecutingAssembly());

            // Browser will include the path (/), so strip it
            if (PlatformDetection.IsBrowser && loc.Length > 1)
            {
                loc = loc.Substring(1);
            }

            Assert.Equal(loc, Module.FullyQualifiedName);
#endif
        }

        [Fact]
        public void Name()
        {
#if SINGLE_FILE_TEST_RUNNER
            Assert.Equal("<Unknown>", Module.Name, ignoreCase: true);
#else
            Assert.Equal("system.runtime.tests.dll", Module.Name, ignoreCase: true);
#endif
        }

        [Fact]
        public void Equality()
        {
            Assert.True(Assembly.GetExecutingAssembly().GetModules().First() == Module);
            Assert.True(Module.Equals(Assembly.GetExecutingAssembly().GetModules().First()));
        }

        [Fact]
        public void TestGetHashCode()
        {
            Assert.Equal(Assembly.GetExecutingAssembly().GetModules().First().GetHashCode(), Module.GetHashCode());
        }

        [Theory]
        [InlineData(typeof(ModuleTests))]
        [InlineData(typeof(PointerTests))]
        public void TestGetType(Type type)
        {
            Assert.Equal(type, Module.GetType(type.FullName, true, true));
        }

        [Fact]
        public void TestToString()
        {
            Assert.Equal("System.Runtime.Tests.dll", Module.ToString());
        }

        [Fact]
        public void IsDefined_NullType()
        {
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("attributeType", () =>
            {
                Module.IsDefined(null, false);
            });
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public void GetField_NullName()
        {
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("name", () =>
            {
                Module.GetField(null);
            });
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);

            ex = AssertExtensions.Throws<ArgumentNullException>("name", () =>
            {
                Module.GetField(null, 0);
            });
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public void GetField()
        {
            FieldInfo testInt = TestModule.GetField("TestInt", BindingFlags.Public | BindingFlags.Static);
            Assert.Equal(1, (int)testInt.GetValue(null));
            testInt.SetValue(null, 100);
            Assert.Equal(100, (int)testInt.GetValue(null));
            FieldInfo testLong = TestModule.GetField("TestLong", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.Equal(2L, (long)testLong.GetValue(null));
            testLong.SetValue(null, 200);
            Assert.Equal(200L, (long)testLong.GetValue(null));
        }

        [Fact]
        public void GetFields()
        {
            List<FieldInfo> fields = TestModule.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).OrderBy(f => f.Name).ToList();
            Assert.Equal(2, fields.Count);
            Assert.Equal(TestModule.GetField("TestInt"), fields[0]);
            Assert.Equal(TestModule.GetField("TestLong", BindingFlags.NonPublic | BindingFlags.Static), fields[1]);
        }

        [Fact]
        public void GetMethod_NullName()
        {
            var ex = AssertExtensions.Throws<ArgumentNullException>("name", () => Module.GetMethod(null));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);

            ex = AssertExtensions.Throws<ArgumentNullException>("name", () => Module.GetMethod(null, Type.EmptyTypes));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public void GetMethod_NullTypes()
        {
            var ex = AssertExtensions.Throws<ArgumentNullException>("types", () => Module.GetMethod("TestMethodFoo", null));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51912", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public void GetMethod_AmbiguousMatch()
        {
            var ex = Assert.Throws<AmbiguousMatchException>(() => TestModule.GetMethod("TestMethodFoo"));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51912", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public void GetMethod()
        {
            var method = TestModule.GetMethod("TestMethodFoo", Type.EmptyTypes);
            Assert.True(method.IsPublic);
            Assert.True(method.IsStatic);
            Assert.Equal(typeof(void), method.ReturnType);
            Assert.Empty(method.GetParameters());

            method = TestModule.GetMethod("TestMethodBar", BindingFlags.NonPublic | BindingFlags.Static, null, CallingConventions.Any, new[] { typeof(int) }, null);
            Assert.False(method.IsPublic);
            Assert.True(method.IsStatic);
            Assert.Equal(typeof(int), method.ReturnType);
            Assert.Equal(typeof(int), method.GetParameters().Single().ParameterType);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51912", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public void GetMethods()
        {
            var methodNames = TestModule.GetMethods().Select(m => m.Name).ToArray();
            AssertExtensions.SequenceEqual(new[]{ "TestMethodFoo", "TestMethodFoo" }, methodNames );

            methodNames = TestModule.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Select(m => m.Name).ToArray();
            Array.Sort<string>(methodNames);
            AssertExtensions.SequenceEqual(new[]{ "TestMethodBar", "TestMethodFoo", "TestMethodFoo" }, methodNames );
        }

        public static IEnumerable<Type> Types => Module.GetTypes();

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMetadataTokenSupported))]
        public void ResolveTypes()
        {
            foreach(Type t in Types)
                Assert.Equal(t, Module.ResolveType(t.MetadataToken));
        }

        public static IEnumerable<object[]> BadResolveTypes =>
            new[]
            {
                new object[] { 1234 },
                new object[] { typeof(ModuleTests).GetMethod("ResolveTypes").MetadataToken },
            }
            .Union(NullTokens);

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMetadataTokenSupported))]
        [MemberData(nameof(BadResolveTypes))]
        public void ResolveTypeFail(int token)
        {
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                Module.ResolveType(token);
            });
        }

        public static IEnumerable<MemberInfo> Methods =>
            typeof(ModuleTests).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMetadataTokenSupported))]
        public void ResolveMethodsByMethodInfo()
        {
            foreach(MethodInfo mi in Methods)
                Assert.Equal(mi, Module.ResolveMethod(mi.MetadataToken));
        }

        public static IEnumerable<object[]> BadResolveMethods =>
            new[]
            {
                new object[] { 1234 },
                new object[] { typeof(ModuleTests).MetadataToken },
                new object[] { typeof(ModuleTests).MetadataToken + 1000 },
            }
            .Union(NullTokens);

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMetadataTokenSupported))]
        [MemberData(nameof(BadResolveMethods))]
        public void ResolveMethodFail(int token)
        {
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                Module.ResolveMethod(token);
            });
        }

        public static IEnumerable<MemberInfo> Fields =>
            typeof(ModuleTests).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMetadataTokenSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52072", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void ResolveFieldsByFieldInfo()
        {
            foreach(FieldInfo fi in Fields)
                Assert.Equal(fi, Module.ResolveField(fi.MetadataToken));
        }

        public static IEnumerable<object[]> BadResolveFields =>
            new[]
            {
                new object[] { 1234 },
                new object[] { typeof(ModuleTests).MetadataToken },
                new object[] { typeof(ModuleTests).MetadataToken + 1000 },
            }
            .Union(NullTokens);

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMetadataTokenSupported))]
        [MemberData(nameof(BadResolveFields))]
        public void ResolveFieldFail(int token)
        {
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                Module.ResolveField(token);
            });
        }

        public static IEnumerable<object[]> BadResolveStrings =>
            new[]
            {
                new object[] { 1234 },
                new object[] { typeof(ModuleTests).MetadataToken },
                new object[] { typeof(ModuleTests).MetadataToken + 1000 },
            }
            .Union(NullTokens);

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMetadataTokenSupported))]
        [MemberData(nameof(BadResolveStrings))]
        public void ResolveStringFail(int token)
        {
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                Module.ResolveString(token);
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMetadataTokenSupported))]
        public void ResolveTypesByMemberInfo()
        {
            foreach(MemberInfo mi in Types)
                Assert.Equal(mi, Module.ResolveMember(mi.MetadataToken));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMetadataTokenSupported))]
        public void ResolveMethodsByMemberInfo()
        {
            foreach (MemberInfo mi in Methods)
                Assert.Equal(mi, Module.ResolveMember(mi.MetadataToken));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMetadataTokenSupported))]
        public void ResolveFieldsByMemberInfo()
        {
            foreach (MemberInfo mi in Fields)
                Assert.Equal(mi, Module.ResolveMember(mi.MetadataToken));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMetadataTokenSupported))]
        public void ResolveMethodOfGenericClass()
        {
            Type t = typeof(Foo<>);
            Module mod = t.Module;
            MethodInfo method = t.GetMethod("Bar");
            MethodBase actual = mod.ResolveMethod(method.MetadataToken);
            Assert.Equal(method, actual);
        }

        [Fact]
        public void GetTypes()
        {
            List<Type> types = TestModule.GetTypes().ToList();
            Assert.Equal(1, types.Count);
            Assert.Equal("System.Reflection.TestModule.Dummy, System.Reflection.TestModule, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", types[0].AssemblyQualifiedName);
        }

        private static object[][] NullTokens =>
            new[]
            {
                new object[] { 0x00000000 }, // mdtModule
                new object[] { 0x01000000 }, // mdtTypeRef
                new object[] { 0x02000000 }, // mdtTypeDef
                new object[] { 0x04000000 }, // mdtFieldDef
                new object[] { 0x06000000 }, // mdtMethodDef
                new object[] { 0x08000000 }, // mdtParamDef
                new object[] { 0x09000000 }, // mdtInterfaceImpl
                new object[] { 0x0a000000 }, // mdtMemberRef
                new object[] { 0x0c000000 }, // mdtCustomAttribute
                new object[] { 0x0e000000 }, // mdtPermission
                new object[] { 0x11000000 }, // mdtSignature
                new object[] { 0x14000000 }, // mdtEvent
                new object[] { 0x17000000 }, // mdtProperty
                new object[] { 0x19000000 }, // mdtMethodImpl
                new object[] { 0x1a000000 }, // mdtModuleRef
                new object[] { 0x1b000000 }, // mdtTypeSpec
                new object[] { 0x20000000 }, // mdtAssembly
                new object[] { 0x23000000 }, // mdtAssemblyRef
                new object[] { 0x26000000 }, // mdtFile
                new object[] { 0x27000000 }, // mdtExportedType
                new object[] { 0x28000000 }, // mdtManifestResource
                new object[] { 0x2a000000 }, // mdtGenericParam
                new object[] { 0x2b000000 }, // mdtMethodSpec
                new object[] { 0x2c000000 }, // mdtGenericParamConstraint
                new object[] { 0x70000000 }, // mdtString
                new object[] { 0x71000000 }, // mdtName
                new object[] { 0x72000000 }  // mdtBaseType
            };
    }

    public class Foo<T>
    {
        public void Bar(T t)
        {
        }
    }
}
