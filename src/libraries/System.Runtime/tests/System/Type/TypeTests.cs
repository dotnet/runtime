// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

public class Outside
{
    public class Inside
    {
        public void GenericMethod<T>() { }
        public void TwoGenericMethod<T, U>() { }
    }

    public void GenericMethod<T>() { }
    public void TwoGenericMethod<T, U>() { }
}

public class Outside<T>
{
    public class Inside<U>
    {
        public void GenericMethod<V>() { }
        public void TwoGenericMethod<V, W>() { }
    }

    public void GenericMethod<U>() { }
    public void TwoGenericMethod<U, V>() { }
}

namespace System.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34328", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public partial class TypeTests
    {
        private static readonly IList<Type> NonArrayBaseTypes;

        static TypeTests()
        {
            NonArrayBaseTypes = new List<Type>()
            {
                typeof(int),
                typeof(void),
                typeof(int*),
                typeof(Outside),
                typeof(Outside<int>),
                typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0],
                new object().GetType().GetType()
            };

            if (PlatformDetection.IsWindows)
            {
                NonArrayBaseTypes.Add(Type.GetTypeFromCLSID(default(Guid)));
            }
        }

        [Fact]
        public void FilterName_Get_ReturnsExpected()
        {
            Assert.NotNull(Type.FilterName);
            Assert.Same(Type.FilterName, Type.FilterName);
            Assert.NotSame(Type.FilterName, Type.FilterNameIgnoreCase);
        }

        [Theory]
        [InlineData("FilterName_Invoke_DelegateFiltersExpectedMembers", true)]
        [InlineData("     FilterName_Invoke_DelegateFiltersExpectedMembers   ", true)]
        [InlineData("*", true)]
        [InlineData("     *   ", true)]
        [InlineData("     Filter*   ", true)]
        [InlineData("FilterName_Invoke_DelegateFiltersExpectedMembe*", true)]
        [InlineData("FilterName_Invoke_DelegateFiltersExpectedMember*", true)]
        [InlineData("filterName_Invoke_DelegateFiltersExpectedMembers", false)]
        [InlineData("FilterName_Invoke_DelegateFiltersExpectedMemberss*", false)]
        [InlineData("FilterName", false)]
        [InlineData("*FilterName", false)]
        [InlineData("", false)]
        [InlineData("     ", false)]
        public void FilterName_Invoke_DelegateFiltersExpectedMembers(string filterCriteria, bool expected)
        {
            MethodInfo mi = typeof(TypeTests).GetMethod(nameof(FilterName_Invoke_DelegateFiltersExpectedMembers));
            Assert.Equal(expected, Type.FilterName(mi, filterCriteria));
        }

        [Fact]
        public void FilterName_InvalidFilterCriteria_ThrowsInvalidFilterCriteriaException()
        {
            MethodInfo mi = typeof(TypeTests).GetMethod(nameof(FilterName_Invoke_DelegateFiltersExpectedMembers));
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterName(mi, null));
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterName(mi, new object()));
        }

        [Fact]
        public void FilterNameIgnoreCase_Get_ReturnsExpected()
        {
            Assert.NotNull(Type.FilterNameIgnoreCase);
            Assert.Same(Type.FilterNameIgnoreCase, Type.FilterNameIgnoreCase);
            Assert.NotSame(Type.FilterNameIgnoreCase, Type.FilterName);
        }

        [Theory]
        [InlineData("FilterNameIgnoreCase_Invoke_DelegateFiltersExpectedMembers", true)]
        [InlineData("filternameignorecase_invoke_delegatefiltersexpectedmembers", true)]
        [InlineData("     filterNameIgnoreCase_Invoke_DelegateFiltersexpectedMembers   ", true)]
        [InlineData("*", true)]
        [InlineData("     *   ", true)]
        [InlineData("     fIlTeR*   ", true)]
        [InlineData("FilterNameIgnoreCase_invoke_delegateFiltersExpectedMembe*", true)]
        [InlineData("FilterNameIgnoreCase_invoke_delegateFiltersExpectedMember*", true)]
        [InlineData("filterName_Invoke_DelegateFiltersExpectedMembers", false)]
        [InlineData("filterNameIgnoreCase_Invoke_DelegateFiltersExpectedMemberss", false)]
        [InlineData("FilterNameIgnoreCase_Invoke_DelegateFiltersExpectedMemberss*", false)]
        [InlineData("filterNameIgnoreCase", false)]
        [InlineData("*FilterNameIgnoreCase", false)]
        [InlineData("", false)]
        [InlineData("     ", false)]
        public void FilterNameIgnoreCase_Invoke_DelegateFiltersExpectedMembers(string filterCriteria, bool expected)
        {
            MethodInfo mi = typeof(TypeTests).GetMethod(nameof(FilterNameIgnoreCase_Invoke_DelegateFiltersExpectedMembers));
            Assert.Equal(expected, Type.FilterNameIgnoreCase(mi, filterCriteria));
        }

        [Fact]
        public void FilterNameIgnoreCase_InvalidFilterCriteria_ThrowsInvalidFilterCriteriaException()
        {
            MethodInfo mi = typeof(TypeTests).GetMethod(nameof(FilterName_Invoke_DelegateFiltersExpectedMembers));
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterNameIgnoreCase(mi, null));
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterNameIgnoreCase(mi, new object()));
        }

        public static IEnumerable<object[]> FindMembers_TestData()
        {
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterName, "HelloWorld", 0 };
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterName, "FilterName_Invoke_DelegateFiltersExpectedMembers", 1 };
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterName, "FilterName_Invoke_Delegate*", 1 };
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterName, "filterName_Invoke_Delegate*", 0 };

            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterNameIgnoreCase, "HelloWorld", 0 };
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterNameIgnoreCase, "FilterName_Invoke_DelegateFiltersExpectedMembers", 1 };
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterNameIgnoreCase, "FilterName_Invoke_Delegate*", 1 };
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterNameIgnoreCase, "filterName_Invoke_Delegate*", 1 };
        }

        [Theory]
        [MemberData(nameof(FindMembers_TestData))]
        public void FindMembers_Invoke_ReturnsExpected(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria, int expectedLength)
        {
            Assert.Equal(expectedLength, typeof(TypeTests).FindMembers(memberType, bindingAttr, filter, filterCriteria).Length);
        }

        [Theory]
        [InlineData(typeof(int), typeof(int))]
        [InlineData(typeof(int[]), typeof(int[]))]
        [InlineData(typeof(Outside<int>), typeof(Outside<int>))]
        public void TypeHandle(Type t1, Type t2)
        {
            RuntimeTypeHandle r1 = t1.TypeHandle;
            RuntimeTypeHandle r2 = t2.TypeHandle;
            Assert.Equal(r1, r2);

            Assert.Equal(t1, Type.GetTypeFromHandle(r1));
            Assert.Equal(t1, Type.GetTypeFromHandle(r2));
        }

        [Fact]
        public void GetTypeFromDefaultHandle()
        {
            Assert.Null(Type.GetTypeFromHandle(default(RuntimeTypeHandle)));
        }

        public static IEnumerable<object[]> MakeArrayType_TestData()
        {
            return new object[][]
            {
                // Primitives
                new object[] { typeof(string), typeof(string[]) },
                new object[] { typeof(sbyte), typeof(sbyte[]) },
                new object[] { typeof(byte), typeof(byte[]) },
                new object[] { typeof(short), typeof(short[]) },
                new object[] { typeof(ushort), typeof(ushort[]) },
                new object[] { typeof(int), typeof(int[]) },
                new object[] { typeof(uint), typeof(uint[]) },
                new object[] { typeof(long), typeof(long[]) },
                new object[] { typeof(ulong), typeof(ulong[]) },
                new object[] { typeof(char), typeof(char[]) },
                new object[] { typeof(bool), typeof(bool[]) },
                new object[] { typeof(float), typeof(float[]) },
                new object[] { typeof(double), typeof(double[]) },
                new object[] { typeof(IntPtr), typeof(IntPtr[]) },
                new object[] { typeof(UIntPtr), typeof(UIntPtr[]) },

                // Primitives enums
                new object[] { typeof(SByteEnum), typeof(SByteEnum[]) },
                new object[] { typeof(ByteEnum), typeof(ByteEnum[]) },
                new object[] { typeof(Int16Enum), typeof(Int16Enum[]) },
                new object[] { typeof(UInt16Enum), typeof(UInt16Enum[]) },
                new object[] { typeof(Int32Enum), typeof(Int32Enum[]) },
                new object[] { typeof(UInt32Enum), typeof(UInt32Enum[]) },
                new object[] { typeof(Int64Enum), typeof(Int64Enum[]) },
                new object[] { typeof(UInt64Enum), typeof(UInt64Enum[]) },

                // Array, pointers
                new object[] { typeof(string[]), typeof(string[][]) },
                new object[] { typeof(int[]), typeof(int[][]) },
                new object[] { typeof(int*), typeof(int*[]) },

                // Classes, structs, interfaces, enums
                new object[] { typeof(NonGenericClass), typeof(NonGenericClass[]) },
                new object[] { typeof(GenericClass<int>), typeof(GenericClass<int>[]) },
                new object[] { typeof(NonGenericStruct), typeof(NonGenericStruct[]) },
                new object[] { typeof(GenericStruct<int>), typeof(GenericStruct<int>[]) },
                new object[] { typeof(NonGenericInterface), typeof(NonGenericInterface[]) },
                new object[] { typeof(GenericInterface<int>), typeof(GenericInterface<int>[]) },
                new object[] { typeof(AbstractClass), typeof(AbstractClass[]) }
            };
        }

        [Theory]
        [MemberData(nameof(MakeArrayType_TestData))]
        public void MakeArrayType_Invoke_ReturnsExpected(Type t, Type tArrayExpected)
        {
            Type tArray = t.MakeArrayType();

            Assert.Equal(tArrayExpected, tArray);
            Assert.Equal(t, tArray.GetElementType());

            Assert.True(tArray.IsArray);
            Assert.True(tArray.HasElementType);
            Assert.Equal(1, tArray.GetArrayRank());

            Assert.Equal(t.ToString() + "[]", tArray.ToString());
        }

        public static IEnumerable<object[]> MakeArray_UnusualTypes_TestData()
        {
            yield return new object[] { typeof(StaticClass) };
            yield return new object[] { typeof(void) };
            yield return new object[] { typeof(GenericClass<>) };
            yield return new object[] { typeof(GenericClass<>).MakeGenericType(typeof(GenericClass<>)) };
            yield return new object[] { typeof(GenericClass<>).GetTypeInfo().GetGenericArguments()[0] };
        }

        [Theory]
        [MemberData(nameof(MakeArray_UnusualTypes_TestData))]
        public void MakeArrayType_UnusualTypes_ReturnsExpected(Type t)
        {
            Type tArray = t.MakeArrayType();

            Assert.Equal(t, tArray.GetElementType());

            Assert.True(tArray.IsArray);
            Assert.True(tArray.HasElementType);
            Assert.Equal(1, tArray.GetArrayRank());

            Assert.Equal(t.ToString() + "[]", tArray.ToString());
        }

        [Theory]
        [MemberData(nameof(MakeArrayType_TestData))]
        public void MakeArrayType_InvokeRankOne_ReturnsExpected(Type t, Type tArrayExpected)
        {
            Type tArray = t.MakeArrayType(1);

            Assert.NotEqual(tArrayExpected, tArray);
            Assert.Equal(t, tArray.GetElementType());

            Assert.True(tArray.IsArray);
            Assert.True(tArray.HasElementType);
            Assert.Equal(1, tArray.GetArrayRank());

            Assert.Equal(t.ToString() + "[*]", tArray.ToString());
        }

        [Theory]
        [MemberData(nameof(MakeArrayType_TestData))]
        public void MakeArrayType_InvokeRankTwo_ReturnsExpected(Type t, Type tArrayExpected)
        {
            Type tArray = t.MakeArrayType(2);

            Assert.NotEqual(tArrayExpected, tArray);
            Assert.Equal(t, tArray.GetElementType());

            Assert.True(tArray.IsArray);
            Assert.True(tArray.HasElementType);
            Assert.Equal(2, tArray.GetArrayRank());

            Assert.Equal(t.ToString() + "[,]", tArray.ToString());
        }

        public static IEnumerable<object[]> MakeArrayType_ByRef_TestData()
        {
            yield return new object[] { typeof(int).MakeByRefType() };
            yield return new object[] { typeof(TypedReference) };
            yield return new object[] { typeof(ArgIterator) };
            yield return new object[] { typeof(RuntimeArgumentHandle) };
            yield return new object[] { typeof(Span<int>) };
        }

        [Theory]
        [MemberData(nameof(MakeArrayType_ByRef_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/39001", TestRuntimes.Mono)]
        public void MakeArrayType_ByRef_ThrowsTypeLoadException(Type t)
        {
            Assert.Throws<TypeLoadException>(() => t.MakeArrayType());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void MakeArrayType_NegativeOrZeroArrayRank_ThrowsIndexOutOfRangeException(int rank)
        {
            Type t = typeof(int);
            Assert.Throws<IndexOutOfRangeException>(() => t.MakeArrayType(rank));
        }

        [Theory]
        [InlineData(33)]
        [InlineData(256)]
        [InlineData(257)]
        public void MakeArrayType_InvalidArrayRank_ThrowsTypeLoadException(int rank)
        {
            Type t = typeof(int);
            Assert.Throws<TypeLoadException>(() => t.MakeArrayType(rank));
        }

        public static IEnumerable<object[]> MakeByRefType_TestData()
        {
            return new object[][]
            {
                // Primitives
                new object[] { typeof(string) },
                new object[] { typeof(sbyte) },
                new object[] { typeof(byte) },
                new object[] { typeof(short) },
                new object[] { typeof(ushort) },
                new object[] { typeof(int) },
                new object[] { typeof(uint) },
                new object[] { typeof(long) },
                new object[] { typeof(ulong) },
                new object[] { typeof(char) },
                new object[] { typeof(bool) },
                new object[] { typeof(float) },
                new object[] { typeof(double) },
                new object[] { typeof(IntPtr) },
                new object[] { typeof(UIntPtr) },
                new object[] { typeof(void) },

                // Primitives enums
                new object[] { typeof(SByteEnum) },
                new object[] { typeof(ByteEnum) },
                new object[] { typeof(Int16Enum) },
                new object[] { typeof(UInt16Enum) },
                new object[] { typeof(Int32Enum) },
                new object[] { typeof(UInt32Enum) },
                new object[] { typeof(Int64Enum) },
                new object[] { typeof(UInt64Enum) },

                // Array, pointers
                new object[] { typeof(string[]) },
                new object[] { typeof(int[]) },
                new object[] { typeof(int*) },

                // Classes, structs, interfaces, enums
                new object[] { typeof(NonGenericClass) },
                new object[] { typeof(GenericClass<int>) },
                new object[] { typeof(NonGenericStruct) },
                new object[] { typeof(GenericStruct<int>) },
                new object[] { typeof(NonGenericInterface) },
                new object[] { typeof(GenericInterface<int>) },
                new object[] { typeof(AbstractClass) },
                new object[] { typeof(StaticClass) },

                // Generic types.
                new object[] { typeof(GenericClass<>) },
                new object[] { typeof(GenericClass<>).MakeGenericType(typeof(GenericClass<>)) },
                new object[] { typeof(GenericClass<>).GetTypeInfo().GetGenericArguments()[0] },

                // By-Ref Like.
                new object[] { typeof(TypedReference) },
                new object[] { typeof(ArgIterator) },
                new object[] { typeof(RuntimeArgumentHandle) },
                new object[] { typeof(Span<int>) },
            };
        }

        [Theory]
        [MemberData(nameof(MakeByRefType_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37489", TestRuntimes.Mono)]
        public void MakeByRefType_Invoke_ReturnsExpected(Type t)
        {
            Type tPointer = t.MakeByRefType();
            Assert.Equal(tPointer, t.MakeByRefType());

            Assert.True(tPointer.IsByRef);
            Assert.False(tPointer.IsPointer);
            Assert.True(tPointer.HasElementType);
            Assert.Equal(t, tPointer.GetElementType());

            Assert.Equal(t.ToString() + "&", tPointer.ToString());
        }

        [Fact]
        public void MakeByRefType_ByRef_ThrowsTypeLoadException()
        {
            Type t = typeof(int).MakeByRefType();
            Assert.Throws<TypeLoadException>(() => t.MakeByRefType());
        }

        public static IEnumerable<object[]> MakePointerType_TestData()
        {
            return new object[][]
            {
                // Primitives
                new object[] { typeof(string) },
                new object[] { typeof(sbyte) },
                new object[] { typeof(byte) },
                new object[] { typeof(short) },
                new object[] { typeof(ushort) },
                new object[] { typeof(int) },
                new object[] { typeof(uint) },
                new object[] { typeof(long) },
                new object[] { typeof(ulong) },
                new object[] { typeof(char) },
                new object[] { typeof(bool) },
                new object[] { typeof(float) },
                new object[] { typeof(double) },
                new object[] { typeof(IntPtr) },
                new object[] { typeof(UIntPtr) },
                new object[] { typeof(void) },

                // Primitives enums
                new object[] { typeof(SByteEnum) },
                new object[] { typeof(ByteEnum) },
                new object[] { typeof(Int16Enum) },
                new object[] { typeof(UInt16Enum) },
                new object[] { typeof(Int32Enum) },
                new object[] { typeof(UInt32Enum) },
                new object[] { typeof(Int64Enum) },
                new object[] { typeof(UInt64Enum) },

                // Array, pointers
                new object[] { typeof(string[]) },
                new object[] { typeof(int[]) },
                new object[] { typeof(int*) },

                // Classes, structs, interfaces, enums
                new object[] { typeof(NonGenericClass) },
                new object[] { typeof(GenericClass<int>) },
                new object[] { typeof(NonGenericStruct) },
                new object[] { typeof(GenericStruct<int>) },
                new object[] { typeof(NonGenericInterface) },
                new object[] { typeof(GenericInterface<int>) },
                new object[] { typeof(AbstractClass) },
                new object[] { typeof(StaticClass) },

                // Generic types.
                new object[] { typeof(GenericClass<>) },
                new object[] { typeof(GenericClass<>).MakeGenericType(typeof(GenericClass<>)) },
                new object[] { typeof(GenericClass<>).GetTypeInfo().GetGenericArguments()[0] },

                // ByRef Like.
                new object[] { typeof(TypedReference) },
                new object[] { typeof(ArgIterator) },
                new object[] { typeof(RuntimeArgumentHandle) },
                new object[] { typeof(Span<int>) },
            };
        }

        [Theory]
        [MemberData(nameof(MakePointerType_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37489", TestRuntimes.Mono)]
        public void MakePointerType_Invoke_ReturnsExpected(Type t)
        {
            Type tPointer = t.MakePointerType();
            Assert.Equal(tPointer, t.MakePointerType());

            Assert.False(tPointer.IsByRef);
            Assert.True(tPointer.IsPointer);
            Assert.True(tPointer.HasElementType);
            Assert.Equal(t, tPointer.GetElementType());

            Assert.Equal(t.ToString() + "*", tPointer.ToString());
        }

        [Fact]
        public void MakePointerType_ByRef_ThrowsTypeLoadException()
        {
            Type t = typeof(int).MakeByRefType();
            Assert.Throws<TypeLoadException>(() => t.MakePointerType());
        }

        [Theory]
        [InlineData("System.Nullable`1[System.Int32]", typeof(int?))]
        [InlineData("System.Int32*", typeof(int*))]
        [InlineData("System.Int32**", typeof(int**))]
        [InlineData("Outside`1", typeof(Outside<>))]
        [InlineData("Outside`1+Inside`1", typeof(Outside<>.Inside<>))]
        [InlineData("Outside[]", typeof(Outside[]))]
        [InlineData("Outside[,,]", typeof(Outside[,,]))]
        [InlineData("Outside[][]", typeof(Outside[][]))]
        [InlineData("Outside`1[System.Nullable`1[System.Boolean]]", typeof(Outside<bool?>))]
        public void GetTypeByName_ValidType_ReturnsExpected(string typeName, Type expectedType)
        {
            Assert.Equal(expectedType, Type.GetType(typeName, throwOnError: false, ignoreCase: false));
            Assert.Equal(expectedType, Type.GetType(typeName.ToLower(), throwOnError: false, ignoreCase: true));
        }

        [Theory]
        [InlineData("system.nullable`1[system.int32]", typeof(TypeLoadException), false)]
        [InlineData("System.NonExistingType", typeof(TypeLoadException), false)]
        [InlineData("", typeof(TypeLoadException), false)]
        [InlineData("System.Int32[,*,]", typeof(ArgumentException), false)]
        [InlineData("Outside`2", typeof(TypeLoadException), false)]
        [InlineData("Outside`1[System.Boolean, System.Int32]", typeof(ArgumentException), true)]
        public void GetTypeByName_Invalid(string typeName, Type expectedException, bool alwaysThrowsException)
        {
            if (!alwaysThrowsException)
            {
                Assert.Null(Type.GetType(typeName, throwOnError: false, ignoreCase: false));
            }

            Assert.Throws(expectedException, () => Type.GetType(typeName, throwOnError: true, ignoreCase: false));
        }

        [Fact]
        public void GetTypeByName_InvokeViaReflection_Success()
        {
            MethodInfo method = typeof(Type).GetMethod("GetType", new[] { typeof(string) });
            object result = method.Invoke(null, new object[] { "System.Tests.TypeTests" });
            Assert.Equal(typeof(TypeTests), result);
        }

        [Fact]
        public void Delimiter()
        {
            Assert.Equal('.', Type.Delimiter);
        }

        [Theory]
        [InlineData(typeof(bool), TypeCode.Boolean)]
        [InlineData(typeof(byte), TypeCode.Byte)]
        [InlineData(typeof(char), TypeCode.Char)]
        [InlineData(typeof(DateTime), TypeCode.DateTime)]
        [InlineData(typeof(decimal), TypeCode.Decimal)]
        [InlineData(typeof(double), TypeCode.Double)]
        [InlineData(null, TypeCode.Empty)]
        [InlineData(typeof(short), TypeCode.Int16)]
        [InlineData(typeof(int), TypeCode.Int32)]
        [InlineData(typeof(long), TypeCode.Int64)]
        [InlineData(typeof(object), TypeCode.Object)]
        [InlineData(typeof(System.Nullable), TypeCode.Object)]
        [InlineData(typeof(Nullable<int>), TypeCode.Object)]
        [InlineData(typeof(Dictionary<,>), TypeCode.Object)]
        [InlineData(typeof(Exception), TypeCode.Object)]
        [InlineData(typeof(sbyte), TypeCode.SByte)]
        [InlineData(typeof(float), TypeCode.Single)]
        [InlineData(typeof(string), TypeCode.String)]
        [InlineData(typeof(ushort), TypeCode.UInt16)]
        [InlineData(typeof(uint), TypeCode.UInt32)]
        [InlineData(typeof(ulong), TypeCode.UInt64)]
        public void GetTypeCode_ValidType_ReturnsExpected(Type t, TypeCode typeCode)
        {
            Assert.Equal(typeCode, Type.GetTypeCode(t));
        }

        [Fact]
        public void ReflectionOnlyGetType()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Type.ReflectionOnlyGetType(null, true, false));
            Assert.Throws<PlatformNotSupportedException>(() => Type.ReflectionOnlyGetType("", true, true));
            Assert.Throws<PlatformNotSupportedException>(() => Type.ReflectionOnlyGetType("System.Tests.TypeTests", false, true));
        }

        [Fact]
        public void IsSZArray_FalseForNonArrayTypes()
        {
            foreach (Type type in NonArrayBaseTypes)
            {
                Assert.False(type.IsSZArray);
            }
        }

        [Fact]
        public void IsSZArray_TrueForSZArrayTypes()
        {
            foreach (Type type in NonArrayBaseTypes.Select(nonArrayBaseType => nonArrayBaseType.MakeArrayType()))
            {
                Assert.True(type.IsSZArray);
            }
        }

        [Fact]
        public void IsSZArray_FalseForVariableBoundArrayTypes()
        {
            foreach (Type type in NonArrayBaseTypes.Select(nonArrayBaseType => nonArrayBaseType.MakeArrayType(1)))
            {
                Assert.False(type.IsSZArray);
            }

            foreach (Type type in NonArrayBaseTypes.Select(nonArrayBaseType => nonArrayBaseType.MakeArrayType(2)))
            {
                Assert.False(type.IsSZArray);
            }
        }

        [Fact]
        public void IsSZArray_FalseForNonArrayByRefType()
        {
            Assert.False(typeof(int).MakeByRefType().IsSZArray);
        }

        [Fact]
        public void IsSZArray_FalseForByRefSZArrayType()
        {
            Assert.False(typeof(int[]).MakeByRefType().IsSZArray);
        }


        [Fact]
        public void IsSZArray_FalseForByRefVariableArrayType()
        {
            Assert.False(typeof(int[,]).MakeByRefType().IsSZArray);
        }

        [Fact]
        public void IsVariableBoundArray_FalseForNonArrayTypes()
        {
            foreach (Type type in NonArrayBaseTypes)
            {
                Assert.False(type.IsVariableBoundArray);
            }
        }

        [Fact]
        public void IsVariableBoundArray_FalseForSZArrayTypes()
        {
            foreach (Type type in NonArrayBaseTypes.Select(nonArrayBaseType => nonArrayBaseType.MakeArrayType()))
            {
                Assert.False(type.IsVariableBoundArray);
            }
        }

        [Fact]
        public void IsVariableBoundArray_TrueForVariableBoundArrayTypes()
        {
            foreach (Type type in NonArrayBaseTypes.Select(nonArrayBaseType => nonArrayBaseType.MakeArrayType(1)))
            {
                Assert.True(type.IsVariableBoundArray);
            }

            foreach (Type type in NonArrayBaseTypes.Select(nonArrayBaseType => nonArrayBaseType.MakeArrayType(2)))
            {
                Assert.True(type.IsVariableBoundArray);
            }
        }

        [Fact]
        public void IsVariableBoundArray_FalseForNonArrayByRefType()
        {
            Assert.False(typeof(int).MakeByRefType().IsVariableBoundArray);
        }

        [Fact]
        public void IsVariableBoundArray_FalseForByRefSZArrayType()
        {
            Assert.False(typeof(int[]).MakeByRefType().IsVariableBoundArray);
        }


        [Fact]
        public void IsVariableBoundArray_FalseForByRefVariableArrayType()
        {
            Assert.False(typeof(int[,]).MakeByRefType().IsVariableBoundArray);
        }

        [Theory]
        [MemberData(nameof(DefinedTypes))]
        public void IsTypeDefinition_True(Type type)
        {
            Assert.True(type.IsTypeDefinition);
        }

        [Theory]
        [MemberData(nameof(NotDefinedTypes))]
        public void IsTypeDefinition_False(Type type)
        {
            Assert.False(type.IsTypeDefinition);
        }

        // In the unlikely event we ever add new values to the CorElementType enumeration, CoreCLR will probably miss it because of the way IsTypeDefinition
        // works. It's likely that such a type will live in the core assembly so to improve our chances of catching this situation, test IsTypeDefinition
        // on every type exposed out of that assembly.
        //
        // Skipping this on .NET Native because:
        //  - We really don't want to opt in all the metadata in System.Private.CoreLib
        //  - The .NET Native implementation of IsTypeDefinition is not the one that works by enumerating selected values off CorElementType.
        //    It has much less need of a test like this.
        [Fact]
        public void IsTypeDefinition_AllDefinedTypesInCoreAssembly()
        {
            foreach (Type type in typeof(object).Assembly.DefinedTypes)
            {
                Assert.True(type.IsTypeDefinition, "IsTypeDefinition expected to be true for type " + type);
            }
        }

        public static IEnumerable<object[]> DefinedTypes
        {
            get
            {
                yield return new object[] { typeof(void) };
                yield return new object[] { typeof(int) };
                yield return new object[] { typeof(Outside) };
                yield return new object[] { typeof(Outside.Inside) };
                yield return new object[] { typeof(Outside<>) };
                yield return new object[] { typeof(IEnumerable<>) };
                yield return new object[] { 3.GetType().GetType() };  // This yields a reflection-blocked type on .NET Native - which is implemented separately

                if (PlatformDetection.IsWindows)
                    yield return new object[] { Type.GetTypeFromCLSID(default(Guid)) };
            }
        }

        public static IEnumerable<object[]> NotDefinedTypes
        {
            get
            {
                Type theT = typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0];

                yield return new object[] { typeof(int[]) };
                yield return new object[] { theT.MakeArrayType(1) }; // Using an open type as element type gets around .NET Native nonsupport of rank-1 multidim arrays
                yield return new object[] { typeof(int[,]) };

                yield return new object[] { typeof(int).MakeByRefType() };

                yield return new object[] { typeof(int).MakePointerType() };

                yield return new object[] { typeof(Outside<int>) };
                yield return new object[] { typeof(Outside<int>.Inside<int>) };

                yield return new object[] { theT };
            }
        }

        [Theory]
        [MemberData(nameof(IsByRefLikeTestData))]
        public static void TestIsByRefLike(Type type, bool expected)
        {
            Assert.Equal(expected, type.IsByRefLike);
        }

        public static IEnumerable<object[]> IsByRefLikeTestData
        {
            get
            {
                Type theT = typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0];

                yield return new object[] { typeof(ArgIterator), true };
                yield return new object[] { typeof(ByRefLikeStruct), true };
                yield return new object[] { typeof(RegularStruct), false };
                yield return new object[] { typeof(RuntimeArgumentHandle), true };
                yield return new object[] { typeof(Span<>), true };
                yield return new object[] { typeof(Span<>).MakeGenericType(theT), true };
                yield return new object[] { typeof(Span<int>), true };
                yield return new object[] { typeof(Span<int>).MakeByRefType(), false };
                yield return new object[] { typeof(Span<int>).MakePointerType(), false };
                yield return new object[] { typeof(TypedReference), true };
                yield return new object[] { theT, false };
                yield return new object[] { typeof(int[]), false };
                yield return new object[] { typeof(int[,]), false };
                yield return new object[] { typeof(object), false };
                if (PlatformDetection.IsWindows) // GetTypeFromCLSID is Windows only
                {
                    yield return new object[] { Type.GetTypeFromCLSID(default(Guid)), false };
                }
            }
        }

        private ref struct ByRefLikeStruct
        {
            public ByRefLikeStruct(int dummy)
            {
                S = default(Span<int>);
            }

            public Span<int> S;
        }

        private struct RegularStruct
        {
        }

        [Theory]
        [MemberData(nameof(IsGenericParameterTestData))]
        public static void TestIsGenericParameter(Type type, bool isGenericParameter, bool isGenericTypeParameter, bool isGenericMethodParameter)
        {
            Assert.Equal(isGenericParameter, type.IsGenericParameter);
            Assert.Equal(isGenericTypeParameter, type.IsGenericTypeParameter);
            Assert.Equal(isGenericMethodParameter, type.IsGenericMethodParameter);
        }

        public static IEnumerable<object[]> IsGenericParameterTestData
        {
            get
            {
                yield return new object[] { typeof(void), false, false, false };
                yield return new object[] { typeof(int), false, false, false };
                yield return new object[] { typeof(int[]), false, false, false };
                yield return new object[] { typeof(int).MakeArrayType(1), false, false, false };
                yield return new object[] { typeof(int[,]), false, false, false };
                yield return new object[] { typeof(int).MakeByRefType(), false, false, false };
                yield return new object[] { typeof(int).MakePointerType(), false, false, false };
                yield return new object[] { typeof(DummyGenericClassForTypeTests<>), false, false, false };
                yield return new object[] { typeof(DummyGenericClassForTypeTests<int>), false, false, false };
                if (PlatformDetection.IsWindows) // GetTypeFromCLSID is Windows only
                {
                    yield return new object[] { Type.GetTypeFromCLSID(default(Guid)), false, false, false };
                }

                Type theT = typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0];
                yield return new object[] { theT, true, true, false };

                Type theM = typeof(TypeTests).GetMethod(nameof(GenericMethod), BindingFlags.NonPublic | BindingFlags.Static).GetGenericArguments()[0];
                yield return new object[] { theM, true, false, true };
            }
        }

        private static void GenericMethod<M>() { }
    }

    public class TypeTestsExtended    {
        public class ContextBoundClass : ContextBoundObject
        {
            public string Value = "The Value property.";
        }

        static string s_testAssemblyPath = Path.Combine(Environment.CurrentDirectory, "TestLoadAssembly.dll");
        static string testtype = "System.Collections.Generic.Dictionary`2[[Program, Foo], [Program, Foo]]";

        private static Func<AssemblyName, Assembly> assemblyloader = (aName) => aName.Name == "TestLoadAssembly" ?
                           Assembly.LoadFrom(@".\TestLoadAssembly.dll") :
                           null;
        private static Func<Assembly, string, bool, Type> typeloader = (assem, name, ignore) => assem == null ?
                             Type.GetType(name, false, ignore) :
                                 assem.GetType(name, false, ignore);
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetTypeByName()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            RemoteExecutor.Invoke(() =>
               {
                   string test1 = testtype;
                   Type t1 = Type.GetType(test1,
                             (aName) => aName.Name == "Foo" ?
                                   Assembly.LoadFrom(s_testAssemblyPath) : null,
                             typeloader,
                             true
                     );

                   Assert.NotNull(t1);

                   string test2 = "System.Collections.Generic.Dictionary`2[[Program, TestLoadAssembly], [Program, TestLoadAssembly]]";
                   Type t2 = Type.GetType(test2, assemblyloader, typeloader, true);

                   Assert.NotNull(t2);
                   Assert.Equal(t1, t2);
               }, options).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("System.Collections.Generic.Dictionary`2[[Program, TestLoadAssembly], [Program2, TestLoadAssembly]]")]
        [InlineData("")]
        public void GetTypeByName_NoSuchType_ThrowsTypeLoadException(string typeName)
        {
            RemoteExecutor.Invoke(marshalledTypeName =>
            {
                Assert.Throws<TypeLoadException>(() => Type.GetType(marshalledTypeName, assemblyloader, typeloader, true));
                Assert.Null(Type.GetType(marshalledTypeName, assemblyloader, typeloader, false));
            }, typeName).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetTypeByNameCaseSensitiveTypeloadFailure()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            RemoteExecutor.Invoke(() =>
               {
                   //Type load failure due to case sensitive search of type Ptogram
                   string test3 = "System.Collections.Generic.Dictionary`2[[Program, TestLoadAssembly], [program, TestLoadAssembly]]";
                   Assert.Throws<TypeLoadException>(() =>
                                Type.GetType(test3,
                                            assemblyloader,
                                            typeloader,
                                            true,
                                            false     //case sensitive
                   ));

                   //non throwing version
                   Type t2 = Type.GetType(test3,
                                          assemblyloader,
                                          typeloader,
                                          false,  //no throw
                                          false
                  );

                   Assert.Null(t2);
               }, options).Dispose();
        }

        [Fact]
        public void IsContextful()
        {
            Assert.True(!typeof(TypeTestsExtended).IsContextful);
            Assert.True(!typeof(ContextBoundClass).IsContextful);
        }

#region GetInterfaceMap tests
        public static IEnumerable<object[]> GetInterfaceMap_TestData()
        {
            yield return new object[]
            {
                typeof(ISimpleInterface),
                typeof(SimpleType),
                new Tuple<MethodInfo, MethodInfo>[]
                {
                    new Tuple<MethodInfo, MethodInfo>(typeof(ISimpleInterface).GetMethod("Method"), typeof(SimpleType).GetMethod("Method")),
                    new Tuple<MethodInfo, MethodInfo>(typeof(ISimpleInterface).GetMethod("GenericMethod"), typeof(SimpleType).GetMethod("GenericMethod"))
                }
            };
            yield return new object[]
            {
                typeof(IGenericInterface<object>),
                typeof(DerivedType),
                new Tuple<MethodInfo, MethodInfo>[]
                {
                    new Tuple<MethodInfo, MethodInfo>(typeof(IGenericInterface<object>).GetMethod("Method"), typeof(DerivedType).GetMethod("Method", new Type[] { typeof(object) })),
                }
            };
            yield return new object[]
            {
                typeof(IGenericInterface<string>),
                typeof(DerivedType),
                new Tuple<MethodInfo, MethodInfo>[]
                {
                    new Tuple<MethodInfo, MethodInfo>(typeof(IGenericInterface<string>).GetMethod("Method"), typeof(DerivedType).GetMethod("Method", new Type[] { typeof(string) })),
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetInterfaceMap_TestData))]
        // Android-only, change to TestPlatforms.Android once arcade dependency is updated
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36653", TestRuntimes.Mono)]
        public void GetInterfaceMap(Type interfaceType, Type classType, Tuple<MethodInfo, MethodInfo>[] expectedMap)
        {
            InterfaceMapping actualMapping = classType.GetInterfaceMap(interfaceType);

            Assert.Equal(interfaceType, actualMapping.InterfaceType);
            Assert.Equal(classType, actualMapping.TargetType);

            Assert.Equal(expectedMap.Length, actualMapping.InterfaceMethods.Length);
            Assert.Equal(expectedMap.Length, actualMapping.TargetMethods.Length);

            for (int i = 0; i < expectedMap.Length; i++)
            {
                Assert.Contains(expectedMap[i].Item1, actualMapping.InterfaceMethods);

                int index = Array.IndexOf(actualMapping.InterfaceMethods, expectedMap[i].Item1);
                Assert.Equal(expectedMap[i].Item2, actualMapping.TargetMethods[index]);
            }
        }

        interface ISimpleInterface
        {
            void Method();
            void GenericMethod<T>();
        }

        class SimpleType : ISimpleInterface
        {
            public void Method() { }
            public void GenericMethod<T>() { }
        }

        interface IGenericInterface<T>
        {
            void Method(T arg);
        }

        class GenericBaseType<T> : IGenericInterface<T>
        {
            public void Method(T arg) { }
        }

        class DerivedType : GenericBaseType<object>, IGenericInterface<string>
        {
            public void Method(string arg) { }
        }
#endregion
    }

    public class NonGenericClass { }

    public class NonGenericSubClassOfNonGeneric : NonGenericClass { }

    public class GenericClass<T> { }

    public class NonGenericSubClassOfGeneric : GenericClass<string> { }

    public class GenericClass<T, U> { }
    public abstract class AbstractClass { }
    public static class StaticClass { }

    public struct NonGenericStruct { }

    public ref struct RefStruct { }

    public struct GenericStruct<T> { }
    public struct GenericStruct<T, U> { }

    public interface NonGenericInterface { }
    public interface GenericInterface<T> { }
    public interface GenericInterface<T, U> { }
}

internal class DummyGenericClassForTypeTests<T> { }
