// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class SizeOfTests
    {
        [Fact]
        public void SizeOf_StructWithEnumArray_ReturnsExpected()
        {
            var s = new TestStructWithEnumArray
            {
                ArrayOfEnum = new TestEnum[] { TestEnum.Red, TestEnum.Green, TestEnum.Blue }
            };

            Assert.Equal(12, Marshal.SizeOf((object)s));
            Assert.Equal(12, Marshal.SizeOf(s));
            Assert.Equal(12, Marshal.SizeOf(typeof(TestStructWithEnumArray)));
            Assert.Equal(12, Marshal.SizeOf<TestStructWithEnumArray>());
        }

        [Fact]
        public void SizeOf_Object_ReturnsExpected()
        {
            SomeTestStruct someTestStruct = new SomeTestStruct();
            Assert.NotEqual(0, Marshal.SizeOf(someTestStruct.GetType()));
        }

        [Fact]
        public void SizeOf_Pointer_ReturnsExpected()
        {
            Assert.Equal(IntPtr.Size, Marshal.SizeOf(typeof(int).MakePointerType()));
        }

        [Fact]
        public void SizeOf_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("t", () => Marshal.SizeOf(null));
        }

        [Fact]
        public void SizeOf_NullStructure_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("structure", () => Marshal.SizeOf((object)null));
            AssertExtensions.Throws<ArgumentNullException>("structure", () => Marshal.SizeOf<string>(null));
        }

        [Fact]
        public void SizeOf_Struct_With_GenericValueTypeField_ReturnsExpected()
        {
            Assert.Equal(8, Marshal.SizeOf<TestStructWithGenericStructField>());
            Assert.Equal(8, Marshal.SizeOf<TestStructWithNullable>());
            Assert.Equal(8, Marshal.SizeOf<TestStructWithVector64>());
        }

        [Fact]
        public void SizeOf_TypeWithEmptyBase_ReturnsExpected()
        {
            Assert.Equal(4, Marshal.SizeOf<DerivedClass>());
        }

        public static IEnumerable<object[]> SizeOf_InvalidType_TestData()
        {
            yield return new object[] { typeof(int).MakeByRefType(), null };

            yield return new object[] { typeof(GenericClass<>), "t" };
            yield return new object[] { typeof(GenericClass<string>), "t" };
            yield return new object[] { typeof(GenericStruct<>), "t" };
            yield return new object[] { typeof(GenericStruct<string>), "t" };
            yield return new object[] { typeof(IGenericInterface<>), "t" };
            yield return new object[] { typeof(IGenericInterface<string>), "t" };

            yield return new object[] { typeof(GenericClass<>).GetTypeInfo().GenericTypeParameters[0], null };

            if (PlatformDetection.IsReflectionEmitSupported)
            {
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.Run);
                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
                TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
                yield return new object[] { typeBuilder, "t" };
            }

            yield return new object[] { typeof(TestStructWithFxdLPSTRSAFld), null };
            yield return new object[] { typeof(int[]), null };
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15087", TestRuntimes.Mono)]
        [MemberData(nameof(SizeOf_InvalidType_TestData))]
        public void SizeOf_InvalidType_ThrowsArgumentException(Type type, string paramName)
        {
            AssertExtensions.Throws<ArgumentException>(paramName, () => Marshal.SizeOf(type));
        }

        [Fact]
        public void SizeOf_GenericStruct_Value_NonGeneric()
        {
            GenericStruct<int> value = default;
            Assert.Equal(8, Marshal.SizeOf((object)value));
        }

        [Fact]
        public void SizeOf_GenericStruct_Value_Generic()
        {
            GenericStruct<int> value = default;
            Assert.Equal(8, Marshal.SizeOf(value));
        }

        [Fact]
        public void SizeOf_GenericClass_Value_NonGeneric()
        {
            SequentialGenericClass<int> value = new();
            Assert.Equal(4, Marshal.SizeOf((object)value));
        }

        [Fact]
        public void SizeOf_GenericClass_Value_Generic()
        {
            SequentialGenericClass<int> value = new();
            Assert.Equal(4, Marshal.SizeOf(value));
        }

        public struct TestStructWithEnumArray
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public TestEnum[] ArrayOfEnum;
        }

        public enum TestEnum
        {
            Red,
            Green,
            Blue
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SomeTestStruct
        {
            public int i;
            public string s;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TestStructWithFxdLPSTRSAFld
        {
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.LPStr, SizeConst = 0)]
            public string[] Arr;
        }

        public struct GenericStruct<T>
        {
            public T t;
            public bool b;
        }

        public struct TestStructWithGenericStructField
        {
            public GenericStruct<int> i;
        }

        public struct TestStructWithNullable
        {
            public int? i;
        }

        public struct TestStructWithVector64
        {
            public System.Runtime.Intrinsics.Vector64<double> v;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class EmptyClass
        {
        }

        [StructLayout(LayoutKind.Sequential)]
        public class DerivedClass : EmptyClass
        {
            public int i;
        }
    }
}
