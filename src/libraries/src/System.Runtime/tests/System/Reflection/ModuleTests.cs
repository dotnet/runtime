// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Assert.Equal(false, complicatedAttribute.NamedArguments[0].IsField);
            Assert.Equal("Stuff", complicatedAttribute.NamedArguments[0].MemberName);
            Assert.Equal(typeof(ComplicatedAttribute).GetProperty("Stuff"), complicatedAttribute.NamedArguments[0].MemberInfo);
            Assert.Equal(typeof(int), complicatedAttribute.NamedArguments[0].TypedValue.ArgumentType);
            Assert.Equal(2, complicatedAttribute.NamedArguments[0].TypedValue.Value);
        }

        [Fact]
        public void FullyQualifiedName()
        {
            Assert.Equal(Assembly.GetExecutingAssembly().Location, Module.FullyQualifiedName);
        }

        [Fact]
        public void Name()
        {
            Assert.Equal("system.runtime.tests.dll", Module.Name, ignoreCase: true);
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

        public static IEnumerable<object[]> Types =>
            Module.GetTypes().Select(t => new object[] { t });

        [Theory]
        [MemberData(nameof(Types))]
        public void ResolveType(Type t)
        {
            Assert.Equal(t, Module.ResolveType(t.MetadataToken));
        }

        public static IEnumerable<object[]> BadResolveTypes =>
            new[]
            {
                new object[] { 1234 },
                new object[] { typeof(ModuleTests).GetMethod("ResolveType").MetadataToken },
            };

        [Theory]
        [MemberData(nameof(BadResolveTypes))]
        public void ResolveTypeFail(int token)
        {
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                Module.ResolveType(token);
            });
        }

        public static IEnumerable<object[]> Methods =>
            Module.GetMethods().Select(m => new object[] { m });

        [Theory]
        [MemberData(nameof(Methods))]
        public void ResolveMethod(MethodInfo t)
        {
            Assert.Equal(t, Module.ResolveMethod(t.MetadataToken));
        }

        public static IEnumerable<object[]> BadResolveMethods =>
            new[]
            {
                new object[] { 1234 },
                new object[] { typeof(ModuleTests).MetadataToken },
                new object[] { typeof(ModuleTests).MetadataToken + 1000 },
            };

        [Theory]
        [MemberData(nameof(BadResolveMethods))]
        public void ResolveMethodFail(int token)
        {
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                Module.ResolveMethod(token);
            });
        }

        public static IEnumerable<object[]> Fields =>
            Module.GetFields().Select(f => new object[] { f });

        [Theory]
        [MemberData(nameof(Fields))]
        public void ResolveField(FieldInfo t)
        {
            Assert.Equal(t, Module.ResolveField(t.MetadataToken));
        }

        public static IEnumerable<object[]> BadResolveFields =>
            new[]
            {
                new object[] { 1234 },
                new object[] { typeof(ModuleTests).MetadataToken },
                new object[] { typeof(ModuleTests).MetadataToken + 1000 },
            };

        [Theory]
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
            };

        [Theory]
        [MemberData(nameof(BadResolveStrings))]
        public void ResolveStringFail(int token)
        {
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                Module.ResolveString(token);
            });
        }

        [Theory]
        [MemberData(nameof(Types))]
        [MemberData(nameof(Methods))]
        [MemberData(nameof(Fields))]
        public void ResolveMember(MemberInfo member)
        {
            Assert.Equal(member, Module.ResolveMember(member.MetadataToken));
        }

        [Fact]
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
    }

    public class Foo<T>
    {
        public void Bar(T t)
        {
        }
    }
}
