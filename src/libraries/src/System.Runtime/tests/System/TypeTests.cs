// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Xunit;

internal class Outside
{
    public class Inside
    {
        public void GenericMethod<T>() { }
        public void TwoGenericMethod<T, U>() { }
    }

    public void GenericMethod<T>() { }
    public void TwoGenericMethod<T, U>() { }
}

internal class Outside<T>
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
    public class TypeTests
    {
        public static IEnumerable<object[]> DeclaringType_TestData()
        {
            // Primitives.
            yield return new object[] { typeof(int), null };
            yield return new object[] { typeof(int).MakeByRefType(), null };
            yield return new object[] { typeof(int).MakePointerType(), null };

            // Arrays.
            yield return new object[] { typeof(int[]), null };
            yield return new object[] { typeof(int[,]), null };
            yield return new object[] { typeof(int[][]), null };
            yield return new object[] { typeof(Array), null };
            yield return new object[] { typeof(Outside.Inside[]), null };
            yield return new object[] { typeof(Outside<int>.Inside<double>[]), null };

            // Classes.
            yield return new object[] { typeof(TypedReference), null };

            // Generic type parameters from a type.
            yield return new object[] { typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0], typeof(Outside<>) };
            yield return new object[] { typeof(Outside<>.Inside<>).GetTypeInfo().GenericTypeParameters[0], typeof(Outside<>.Inside<>) };
        
            // Generic type parameters from a method.
            yield return new object[] { typeof(Outside<>).GetMethod(nameof(Outside<string>.GenericMethod)).GetGenericArguments()[0], typeof(Outside<>) };
            yield return new object[] { typeof(Outside<string>).GetMethod(nameof(Outside<string>.GenericMethod)).GetGenericArguments()[0], typeof(Outside<>) };
            yield return new object[] { typeof(Outside<>.Inside<>).GetMethod(nameof(Outside<string>.Inside<int>.GenericMethod)).GetGenericArguments()[0], typeof(Outside<>.Inside<>) };
            yield return new object[] { typeof(Outside<string>.Inside<int>).GetMethod(nameof(Outside<string>.Inside<int>.GenericMethod)).GetGenericArguments()[0], typeof(Outside<>.Inside<>) };
        
            // Nested.
            yield return new object[] { typeof(Outside.Inside), typeof(Outside) };
            yield return new object[] { typeof(Outside<int>.Inside<double>), typeof(Outside<>) };
            yield return new object[] { typeof(Outside<>.Inside<>), typeof(Outside<>) };
        }

        [Theory]
        [MemberData(nameof(DeclaringType_TestData))]
        public void DeclaringType_Invoke_ReturnsExpected(Type t, Type expected)
        {
            Assert.Equal(expected, t.DeclaringType);
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
        [InlineData("     Filter*   ", true)]
        [InlineData("FilterName_Invoke_DelegateFiltersExpectedMembe*", true)]
        [InlineData("FilterName_Invoke_DelegateFiltersExpectedMember*", true)]
        [InlineData("filterName_Invoke_DelegateFiltersExpectedMembers", false)]
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

        public static IEnumerable<object[]> GenericParameterPosition_Valid_TestData()
        {
            yield return new object[] { typeof(GenericClass<,>).GetTypeInfo().GenericTypeParameters[0], 0 };
            yield return new object[] { typeof(GenericClass<,>).GetTypeInfo().GenericTypeParameters[1], 1 };

            yield return new object[] { typeof(Outside<>.Inside<>).GetTypeInfo().GenericTypeParameters[0], 0 };
            yield return new object[] { typeof(Outside<>.Inside<>).GetTypeInfo().GenericTypeParameters[1], 1 };

            yield return new object[] { typeof(Outside<>).GetMethod(nameof(Outside<string>.TwoGenericMethod)).GetGenericArguments()[0], 0 };
            yield return new object[] { typeof(Outside<>).GetMethod(nameof(Outside<string>.TwoGenericMethod)).GetGenericArguments()[1], 1 };
        }

        [Theory]
        [MemberData(nameof(GenericParameterPosition_Valid_TestData))]
        public void GenericParameterPosition_ValidParameter_ReturnsExpected(Type t, int expected)
        {
            Assert.Equal(expected, t.GenericParameterPosition);
        }

        public static IEnumerable<object[]> GenericParameterPosition_Invalid_TestData()
        {
            // Primitives.
            yield return new object[] { typeof(int) };
            yield return new object[] { typeof(int).MakeByRefType() };
            yield return new object[] { typeof(int).MakePointerType() };

            // Arrays.
            yield return new object[] { typeof(int[]) };
            yield return new object[] { typeof(int[,]) };
            yield return new object[] { typeof(int[][]) };
            yield return new object[] { typeof(Outside.Inside[]) };
            yield return new object[] { typeof(Outside<int>.Inside<double>[]) };
            yield return new object[] { typeof(Array) };

            // Classes.
            yield return new object[] { typeof(NonGenericClass) };
            yield return new object[] { typeof(NonGenericSubClassOfGeneric) };
            yield return new object[] { typeof(TypedReference) };
            yield return new object[] { typeof(GenericClass<string>) };
            yield return new object[] { typeof(GenericClass<>) };
            yield return new object[] { typeof(NonGenericSubClassOfGeneric) };

            // Structs.
            yield return new object[] { typeof(NonGenericStruct) };
            yield return new object[] { typeof(RefStruct) };
            yield return new object[] { typeof(GenericStruct<string>) };
            yield return new object[] { typeof(GenericStruct<>) };

            // Interfaces.
            yield return new object[] { typeof(NonGenericInterface) };
            yield return new object[] { typeof(GenericInterface<string>) };
            yield return new object[] { typeof(GenericInterface<>) };

            // Generic Parameters.
            yield return new object[] { typeof(Outside<string>).GetTypeInfo().GenericTypeArguments[0] };

            // Nested.
            yield return new object[] { typeof(Outside.Inside) };
            yield return new object[] { typeof(Outside<int>.Inside<double>) };
            yield return new object[] { typeof(Outside<>.Inside<>) };
        }

        [Theory]
        [MemberData(nameof(GenericParameterPosition_Invalid_TestData))]
        public void GenericParameterPosition_NotGenericParameter_ThrowsInvalidOperationException(Type t)
        {
            Assert.Throws<InvalidOperationException>(() => t.GenericParameterPosition);
        }

        public static IEnumerable<object[]> GenericTypeArguments_TestData()
        {
            // Primitives.
            yield return new object[] { typeof(int), new Type[0] };
            yield return new object[] { typeof(int).MakeByRefType(), new Type[0] };
            yield return new object[] { typeof(int).MakePointerType(), new Type[0] };

            // Arrays.
            yield return new object[] { typeof(int[]), new Type[0] };
            yield return new object[] { typeof(int[,]), new Type[0] };
            yield return new object[] { typeof(int[][]), new Type[0] };
            yield return new object[] { typeof(Outside.Inside[]), new Type[0] };
            yield return new object[] { typeof(Outside<int>.Inside<double>[]), new Type[0] };
            yield return new object[] { typeof(Array), new Type[0] };

            // Classes.
            yield return new object[] { typeof(NonGenericClass), new Type[0] };
            yield return new object[] { typeof(NonGenericSubClassOfGeneric), new Type[0] };
            yield return new object[] { typeof(TypedReference), new Type[0] };
            yield return new object[] { typeof(GenericClass<string>), new Type[] { typeof(string) } };
            yield return new object[] { typeof(GenericClass<int, string>), new Type[] { typeof(int), typeof(string) } };
            yield return new object[] { typeof(GenericClass<>), new Type[0] };
            yield return new object[] { typeof(NonGenericSubClassOfGeneric), new Type[0] };

            // Structs.
            yield return new object[] { typeof(NonGenericStruct), new Type[0] };
            yield return new object[] { typeof(RefStruct), new Type[0] };
            yield return new object[] { typeof(GenericStruct<string>), new Type[] { typeof(string) } };
            yield return new object[] { typeof(GenericStruct<int, string>), new Type[] { typeof(int), typeof(string) } };
            yield return new object[] { typeof(GenericStruct<>), new Type[0] };

            // Interfaces.
            yield return new object[] { typeof(NonGenericInterface), new Type[0] };
            yield return new object[] { typeof(GenericInterface<string>), new Type[] { typeof(string) } };
            yield return new object[] { typeof(GenericInterface<int, string>), new Type[] { typeof(int), typeof(string) } };
            yield return new object[] { typeof(GenericInterface<>), new Type[0] };

            // Generic Parameters.
            yield return new object[] { typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0], new Type[0] };
            yield return new object[] { typeof(Outside<string>).GetTypeInfo().GenericTypeArguments[0], new Type[0] };
            yield return new object[] { typeof(Outside<>).GetMethod(nameof(Outside<string>.GenericMethod)).GetGenericArguments()[0], new Type[0] };

            // Nested.
            yield return new object[] { typeof(Outside.Inside), new Type[0] };
            yield return new object[] { typeof(Outside<int>.Inside<double>), new Type[] { typeof(int), typeof(double) } };
            yield return new object[] { typeof(Outside<>.Inside<>), new Type[0] };
        }

        [Theory]
        [MemberData(nameof(GenericTypeArguments_TestData))]
        public void GenericTypeArguments_Get_ReturnsExpected(Type t, Type[] expected)
        {
            Assert.Equal(expected, t.GenericTypeArguments);
        }

        public static IEnumerable<object[]> HasElementType_TestData()
        {
            // Primitives.
            yield return new object[] { typeof(int), false };
            yield return new object[] { typeof(int).MakeByRefType(), true };
            yield return new object[] { typeof(int).MakePointerType(), true };

            // Arrays.
            yield return new object[] { typeof(int[]), true };
            yield return new object[] { typeof(int[,]), true };
            yield return new object[] { typeof(int[][]), true };
            yield return new object[] { typeof(Outside.Inside[]), true };
            yield return new object[] { typeof(Outside<int>.Inside<double>[]), true };
            yield return new object[] { typeof(Array), false };

            // Classes.
            yield return new object[] { typeof(NonGenericClass), false };
            yield return new object[] { typeof(NonGenericSubClassOfNonGeneric), false };
            yield return new object[] { typeof(TypedReference), false };
            yield return new object[] { typeof(GenericClass<string>), false };
            yield return new object[] { typeof(GenericClass<int, string>), false };
            yield return new object[] { typeof(GenericClass<>), false };
            yield return new object[] { typeof(NonGenericSubClassOfGeneric), false };

            // Structs.
            yield return new object[] { typeof(NonGenericStruct), false };
            yield return new object[] { typeof(RefStruct), false };
            yield return new object[] { typeof(GenericStruct<string>), false };
            yield return new object[] { typeof(GenericStruct<int, string>), false };
            yield return new object[] { typeof(GenericStruct<>), false };

            // Interfaces.
            yield return new object[] { typeof(NonGenericInterface), false };
            yield return new object[] { typeof(GenericInterface<string>), false };
            yield return new object[] { typeof(GenericInterface<int, string>), false };
            yield return new object[] { typeof(GenericInterface<>), false };

            // Generic Parameters.
            yield return new object[] { typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0], false };
            yield return new object[] { typeof(Outside<string>).GetTypeInfo().GenericTypeArguments[0], false };
            yield return new object[] { typeof(Outside<>).GetMethod(nameof(Outside<string>.GenericMethod)).GetGenericArguments()[0], false };

            // Nested.
            yield return new object[] { typeof(Outside.Inside), false };
            yield return new object[] { typeof(Outside<int>.Inside<double>), false };
            yield return new object[] { typeof(Outside<>.Inside<>), false };
        }

        [Theory]
        [MemberData(nameof(HasElementType_TestData))]
        public void HasElementType_Get_ReturnsExpected(Type t, bool expected)
        {
            Assert.Equal(expected, t.HasElementType);
        }

        public static IEnumerable<object[]> IsArray_TestData()
        {
            // Primitives.
            yield return new object[] { typeof(int), false };
            yield return new object[] { typeof(int).MakeByRefType(), false };
            yield return new object[] { typeof(int).MakePointerType(), false };

            // Arrays.
            yield return new object[] { typeof(int[]), true };
            yield return new object[] { typeof(int[,]), true };
            yield return new object[] { typeof(int[][]), true };
            yield return new object[] { typeof(Outside.Inside[]), true };
            yield return new object[] { typeof(Outside<int>.Inside<double>[]), true };
            yield return new object[] { typeof(Array), false };

            // Classes.
            yield return new object[] { typeof(NonGenericClass), false };
            yield return new object[] { typeof(NonGenericSubClassOfNonGeneric), false };
            yield return new object[] { typeof(TypedReference), false };
            yield return new object[] { typeof(GenericClass<string>), false };
            yield return new object[] { typeof(GenericClass<int, string>), false };
            yield return new object[] { typeof(GenericClass<>), false };
            yield return new object[] { typeof(NonGenericSubClassOfGeneric), false };

            // Structs.
            yield return new object[] { typeof(NonGenericStruct), false };
            yield return new object[] { typeof(RefStruct), false };
            yield return new object[] { typeof(GenericStruct<string>), false };
            yield return new object[] { typeof(GenericStruct<int, string>), false };
            yield return new object[] { typeof(GenericStruct<>), false };

            // Interfaces.
            yield return new object[] { typeof(NonGenericInterface), false };
            yield return new object[] { typeof(GenericInterface<string>), false };
            yield return new object[] { typeof(GenericInterface<int, string>), false };
            yield return new object[] { typeof(GenericInterface<>), false };

            // Generic Parameters.
            yield return new object[] { typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0], false };
            yield return new object[] { typeof(Outside<string>).GetTypeInfo().GenericTypeArguments[0], false };
            yield return new object[] { typeof(Outside<>).GetMethod(nameof(Outside<string>.GenericMethod)).GetGenericArguments()[0], false };
        
            // Nested.
            yield return new object[] { typeof(Outside.Inside), false };
            yield return new object[] { typeof(Outside<int>.Inside<double>), false };
            yield return new object[] { typeof(Outside<>.Inside<>), false };
        }

        [Theory]
        [MemberData(nameof(IsArray_TestData))]
        public void IsArray_Get_ReturnsExpected(Type t, bool expected)
        {
            Assert.Equal(expected, t.IsArray);
        }

        public static IEnumerable<object[]> IsByRef_TestData()
        {
            // Primitives.
            yield return new object[] { typeof(int), false };
            yield return new object[] { typeof(int).MakeByRefType(), true };
            yield return new object[] { typeof(int).MakePointerType(), false };

            // Arrays.
            yield return new object[] { typeof(int[]), false };
            yield return new object[] { typeof(int[,]), false };
            yield return new object[] { typeof(int[][]), false };
            yield return new object[] { typeof(Outside.Inside[]), false };
            yield return new object[] { typeof(Outside<int>.Inside<double>[]), false };
            yield return new object[] { typeof(Array), false };

            // Classes.
            yield return new object[] { typeof(NonGenericClass), false };
            yield return new object[] { typeof(NonGenericSubClassOfNonGeneric), false };
            yield return new object[] { typeof(TypedReference), false };
            yield return new object[] { typeof(GenericClass<string>), false };
            yield return new object[] { typeof(GenericClass<int, string>), false };
            yield return new object[] { typeof(GenericClass<>), false };
            yield return new object[] { typeof(NonGenericSubClassOfGeneric), false };

            // Structs.
            yield return new object[] { typeof(NonGenericStruct), false };
            yield return new object[] { typeof(RefStruct), false };
            yield return new object[] { typeof(GenericStruct<string>), false };
            yield return new object[] { typeof(GenericStruct<int, string>), false };
            yield return new object[] { typeof(GenericStruct<>), false };

            // Interfaces.
            yield return new object[] { typeof(NonGenericInterface), false };
            yield return new object[] { typeof(GenericInterface<string>), false };
            yield return new object[] { typeof(GenericInterface<int, string>), false };
            yield return new object[] { typeof(GenericInterface<>), false };

            // Generic Parameters.
            yield return new object[] { typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0], false };
            yield return new object[] { typeof(Outside<string>).GetTypeInfo().GenericTypeArguments[0], false };
            yield return new object[] { typeof(Outside<>).GetMethod(nameof(Outside<string>.GenericMethod)).GetGenericArguments()[0], false };
        
            // Nested.
            yield return new object[] { typeof(Outside.Inside), false };
            yield return new object[] { typeof(Outside<int>.Inside<double>), false };
            yield return new object[] { typeof(Outside<>.Inside<>), false };
        }

        [Theory]
        [MemberData(nameof(IsByRef_TestData))]
        public void IsByRef_Get_ReturnsExpected(Type t, bool expected)
        {
            Assert.Equal(expected, t.IsByRef);
            if (!t.IsByRef && t != typeof(TypedReference))
            {
                Assert.True(t.MakeByRefType().IsByRef);
            }
        }

        public static IEnumerable<object[]> IsPointer_TestData()
        {
            // Primitives.
            yield return new object[] { typeof(int), false };
            yield return new object[] { typeof(int).MakeByRefType(), false };
            yield return new object[] { typeof(int).MakePointerType(), true };

            // Arrays.
            yield return new object[] { typeof(int[]), false };
            yield return new object[] { typeof(int[,]), false };
            yield return new object[] { typeof(int[][]), false };
            yield return new object[] { typeof(Outside.Inside[]), false };
            yield return new object[] { typeof(Outside<int>.Inside<double>[]), false };
            yield return new object[] { typeof(Array), false };

            // Interfaces.
            yield return new object[] { typeof(NonGenericClass), false };
            yield return new object[] { typeof(NonGenericSubClassOfNonGeneric), false };
            yield return new object[] { typeof(TypedReference), false };
            yield return new object[] { typeof(GenericClass<string>), false };
            yield return new object[] { typeof(GenericClass<int, string>), false };
            yield return new object[] { typeof(GenericClass<>), false };
            yield return new object[] { typeof(NonGenericSubClassOfGeneric), false };

            // Structs.
            yield return new object[] { typeof(NonGenericStruct), false };
            yield return new object[] { typeof(RefStruct), false };
            yield return new object[] { typeof(GenericStruct<string>), false };
            yield return new object[] { typeof(GenericStruct<int, string>), false };
            yield return new object[] { typeof(GenericStruct<>), false };

            // Interfaces.
            yield return new object[] { typeof(NonGenericInterface), false };
            yield return new object[] { typeof(GenericInterface<string>), false };
            yield return new object[] { typeof(GenericInterface<int, string>), false };
            yield return new object[] { typeof(GenericInterface<>), false };

            // Generic Parameters.
            yield return new object[] { typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0], false };
            yield return new object[] { typeof(Outside<string>).GetTypeInfo().GenericTypeArguments[0], false };
            yield return new object[] { typeof(Outside<>).GetMethod(nameof(Outside<string>.GenericMethod)).GetGenericArguments()[0], false };

            // Nested.
            yield return new object[] { typeof(Outside.Inside), false };
            yield return new object[] { typeof(Outside<int>.Inside<double>), false };
            yield return new object[] { typeof(Outside<>.Inside<>), false };
        }

        [Theory]
        [MemberData(nameof(IsPointer_TestData))]
        public void IsPointer_Get_ReturnsExpected(Type t, bool expected)
        {
            Assert.Equal(expected, t.IsPointer);
            if (!t.IsByRef && t != typeof(TypedReference))
            {
                Assert.True(t.MakePointerType().IsPointer);
            }
        }

        public static IEnumerable<object[]> IsConstructedGenericType_TestData()
        {
            // Primitives.
            yield return new object[] { typeof(int), false };
            yield return new object[] { typeof(int).MakeByRefType(), false };
            yield return new object[] { typeof(int).MakePointerType(), false };
            yield return new object[] { typeof(int[]), false };
            yield return new object[] { typeof(int[,]), false };
            yield return new object[] { typeof(int[][]), false };
            yield return new object[] { typeof(Outside.Inside[]), false };
            yield return new object[] { typeof(Outside<int>.Inside<double>[]), false };
            yield return new object[] { typeof(Array), false };

            // Classes.
            yield return new object[] { typeof(NonGenericClass), false };
            yield return new object[] { typeof(NonGenericSubClassOfNonGeneric), false };
            yield return new object[] { typeof(TypedReference), false };
            yield return new object[] { typeof(GenericClass<string>), true };
            yield return new object[] { typeof(GenericClass<int, string>), true };
            yield return new object[] { typeof(GenericClass<>), false };
            yield return new object[] { typeof(NonGenericSubClassOfGeneric), false };

            // Structs.
            yield return new object[] { typeof(NonGenericStruct), false };
            yield return new object[] { typeof(RefStruct), false };
            yield return new object[] { typeof(GenericStruct<string>), true };
            yield return new object[] { typeof(GenericStruct<int, string>), true };
            yield return new object[] { typeof(GenericStruct<>), false };

            // Interfaces.
            yield return new object[] { typeof(NonGenericInterface), false };
            yield return new object[] { typeof(GenericInterface<string>), true };
            yield return new object[] { typeof(GenericInterface<int, string>), true };
            yield return new object[] { typeof(GenericInterface<>), false };

            // Generic Parameters.
            yield return new object[] { typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0], false };
            yield return new object[] { typeof(Outside<string>).GetTypeInfo().GenericTypeArguments[0], false };
            yield return new object[] { typeof(Outside<>).GetMethod(nameof(Outside<string>.GenericMethod)).GetGenericArguments()[0], false };

            // Nested.
            yield return new object[] { typeof(Outside.Inside), false };
            yield return new object[] { typeof(Outside<int>.Inside<double>), true };
            yield return new object[] { typeof(Outside<>.Inside<>), false };
        }

        [Theory]
        [MemberData(nameof(IsConstructedGenericType_TestData))]
        public void IsConstructedGenericType_Get_ReturnsExpected(Type t, bool expected)
        {
            Assert.Equal(expected, t.IsConstructedGenericType);
        }

        public static IEnumerable<object[]> IsGenericParameter_TestData()
        {
            // Primitives.
            yield return new object[] { typeof(int), false };
            yield return new object[] { typeof(int).MakeByRefType(), false };
            yield return new object[] { typeof(int).MakePointerType(), false };

            // Arrays.
            yield return new object[] { typeof(int[]), false };
            yield return new object[] { typeof(int[,]), false };
            yield return new object[] { typeof(int[][]), false };
            yield return new object[] { typeof(Outside.Inside[]), false };
            yield return new object[] { typeof(Outside<int>.Inside<double>[]), false };
            yield return new object[] { typeof(Array), false };

            // Classes.
            yield return new object[] { typeof(NonGenericClass), false };
            yield return new object[] { typeof(NonGenericSubClassOfNonGeneric), false };
            yield return new object[] { typeof(TypedReference), false };
            yield return new object[] { typeof(GenericClass<string>), false };
            yield return new object[] { typeof(GenericClass<int, string>), false };
            yield return new object[] { typeof(GenericClass<>), false };
            yield return new object[] { typeof(NonGenericSubClassOfGeneric), false };

            // Structs.
            yield return new object[] { typeof(NonGenericStruct), false };
            yield return new object[] { typeof(RefStruct), false };
            yield return new object[] { typeof(GenericStruct<string>), false };
            yield return new object[] { typeof(GenericStruct<int, string>), false };
            yield return new object[] { typeof(GenericStruct<>), false };

            // Interfaces.
            yield return new object[] { typeof(NonGenericInterface), false };
            yield return new object[] { typeof(GenericInterface<string>), false };
            yield return new object[] { typeof(GenericInterface<int, string>), false };
            yield return new object[] { typeof(GenericInterface<>), false };

            // Generic Parameters.
            yield return new object[] { typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0], true };
            yield return new object[] { typeof(Outside<string>).GetTypeInfo().GenericTypeArguments[0], false };
            yield return new object[] { typeof(Outside<>).GetMethod(nameof(Outside<string>.GenericMethod)).GetGenericArguments()[0], true };

            // Nested.
            yield return new object[] { typeof(Outside.Inside), false };
            yield return new object[] { typeof(Outside<int>.Inside<double>), false };
            yield return new object[] { typeof(Outside<>.Inside<>), false };
        }

        [Theory]
        [MemberData(nameof(IsGenericParameter_TestData))]
        public void IsGenericParameter_Get_ReturnsExpected(Type t, bool expected)
        {
            Assert.Equal(expected, t.IsGenericParameter);
        }

        public static IEnumerable<object[]> IsNested_TestData()
        {
            // Primitives.
            yield return new object[] { typeof(int), false };
            yield return new object[] { typeof(int).MakeByRefType(), false };
            yield return new object[] { typeof(int).MakePointerType(), false };

            // Arrays.
            yield return new object[] { typeof(int[]), false };
            yield return new object[] { typeof(int[,]), false };
            yield return new object[] { typeof(int[][]), false };
            yield return new object[] { typeof(Outside.Inside[]), false };
            yield return new object[] { typeof(Outside<int>.Inside<double>[]), false };
            yield return new object[] { typeof(Array), false };

            // Classes.
            yield return new object[] { typeof(NonGenericClass), false };
            yield return new object[] { typeof(NonGenericSubClassOfNonGeneric), false };
            yield return new object[] { typeof(TypedReference), false };
            yield return new object[] { typeof(GenericClass<string>), false };
            yield return new object[] { typeof(GenericClass<int, string>), false };
            yield return new object[] { typeof(GenericClass<>), false };
            yield return new object[] { typeof(NonGenericSubClassOfGeneric), false };

            // Structs.
            yield return new object[] { typeof(NonGenericStruct), false };
            yield return new object[] { typeof(RefStruct), false };
            yield return new object[] { typeof(GenericStruct<string>), false };
            yield return new object[] { typeof(GenericStruct<int, string>), false };
            yield return new object[] { typeof(GenericStruct<>), false };

            // Interfaces.
            yield return new object[] { typeof(NonGenericInterface), false };
            yield return new object[] { typeof(GenericInterface<string>), false };
            yield return new object[] { typeof(GenericInterface<int, string>), false };
            yield return new object[] { typeof(GenericInterface<>), false };

            // Generic Parameters.
            yield return new object[] { typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0], true };
            yield return new object[] { typeof(Outside<string>).GetTypeInfo().GenericTypeArguments[0], false };
            yield return new object[] { typeof(Outside<>).GetMethod(nameof(Outside<string>.GenericMethod)).GetGenericArguments()[0], true };

            // Nested.
            yield return new object[] { typeof(Outside.Inside), true };
            yield return new object[] { typeof(Outside<int>.Inside<double>), true };
            yield return new object[] { typeof(Outside<>.Inside<>), true };
        }

        [Theory]
        [MemberData(nameof(IsNested_TestData))]
        public void IsNested_Get_ReturnsExpected(Type t, bool expected)
        {
            Assert.Equal(expected, t.IsNested);
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

        [Theory]
        [InlineData(typeof(int[]), 1)]
        [InlineData(typeof(int[,,]), 3)]
        public void GetArrayRank_Get_ReturnsExpected(Type t, int expected)
        {
            Assert.Equal(expected, t.GetArrayRank());
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(IList<int>))]
        [InlineData(typeof(IList<>))]
        public void GetArrayRank_NonArrayType_ThrowsArgumentException(Type t)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => t.GetArrayRank());
        }
        
        public static IEnumerable<object[]> GetElementType_TestData()
        {
            // Primitives.
            yield return new object[] { typeof(int), null };
            yield return new object[] { typeof(int).MakeByRefType(), typeof(int) };
            yield return new object[] { typeof(int).MakePointerType(), typeof(int) };

            // Classes.
            yield return new object[] { typeof(GenericClass<string>), null };
            yield return new object[] { typeof(GenericClass<>), null };
            
            // Arrays.
            yield return new object[] { typeof(int[]), typeof(int) };
            yield return new object[] { typeof(int[,]), typeof(int) };
            yield return new object[] { typeof(int[][]), typeof(int[]) };
            yield return new object[] { typeof(Outside.Inside[]), typeof(Outside.Inside) };
            yield return new object[] { typeof(Outside<int>.Inside<double>[]), typeof(Outside<int>.Inside<double>) };
            yield return new object[] { typeof(Array), null };

            // Nested.
            yield return new object[] { typeof(Outside.Inside), null };
            yield return new object[] { typeof(Outside<int>.Inside<double>), null };
            yield return new object[] { typeof(Outside<>.Inside<>), null };
        }

        [Theory]
        [MemberData(nameof(GetElementType_TestData))]
        [InlineData(typeof(Outside.Inside), null)]
        [InlineData(typeof(int[]), typeof(int))]
        [InlineData(typeof(Outside<int>.Inside<double>[]), typeof(Outside<int>.Inside<double>))]
        [InlineData(typeof(Outside<int>), null)]
        [InlineData(typeof(Outside<int>.Inside<double>), null)]
        public void GetElementType_Get_ReturnsExpected(Type t, Type expected)
        {
            Assert.Equal(expected, t.GetElementType());
        }

        [Theory]
        [InlineData(typeof(int), typeof(int[]))]
        public void MakeArrayType_Invoke_ReturnsExpected(Type t, Type tArrayExpected)
        {
            Type tArray = t.MakeArrayType();

            Assert.Equal(tArrayExpected, tArray);
            Assert.Equal(t, tArray.GetElementType());

            Assert.True(tArray.IsArray);
            Assert.True(tArray.HasElementType);

            string s1 = t.ToString();
            string s2 = tArray.ToString();
            Assert.Equal(s2, s1 + "[]");
        }

        [Theory]
        [InlineData(typeof(int))]
        public void MakeByRefType_Invoke_ReturnsExpected(Type t)
        {
            Type tRef1 = t.MakeByRefType();
            Type tRef2 = t.MakeByRefType();

            Assert.Equal(tRef1, tRef2);

            Assert.True(tRef1.IsByRef);
            Assert.True(tRef1.HasElementType);

            Assert.Equal(t, tRef1.GetElementType());

            string s1 = t.ToString();
            string s2 = tRef1.ToString();
            Assert.Equal(s2, s1 + "&");
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Stackwalking is not supported on UaoAot")]
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

        public void ReflectionOnlyGetType()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeName", () => Type.ReflectionOnlyGetType(null, true, false));
            Assert.Throws<TypeLoadException>(() => Type.ReflectionOnlyGetType("", true, true));
            Assert.Throws<NotSupportedException>(() => Type.ReflectionOnlyGetType("System.Tests.TypeTests", false, true));
        }
    }

    public class TypeTestsExtended : RemoteExecutorTestBase
    {
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
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.LoadFrom() is not supported on UapAot")]
        public void GetTypeByName()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            RemoteInvoke(() =>
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

                   return SuccessExitCode;
               }, options).Dispose();
        }

        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.LoadFrom() is not supported on UapAot")]
        [InlineData("System.Collections.Generic.Dictionary`2[[Program, TestLoadAssembly], [Program2, TestLoadAssembly]]")]
        [InlineData("")]
        public void GetTypeByName_NoSuchType_ThrowsTypeLoadException(string typeName)
        {
            RemoteInvoke(marshalledTypeName =>
            {
                Assert.Throws<TypeLoadException>(() => Type.GetType(marshalledTypeName, assemblyloader, typeloader, true));
                Assert.Null(Type.GetType(marshalledTypeName, assemblyloader, typeloader, false));

                return SuccessExitCode;
            }, typeName).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.LoadFrom() is not supported on UapAot")]
        public void GetTypeByNameCaseSensitiveTypeloadFailure()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            RemoteInvoke(() =>
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

                   return SuccessExitCode;
               }, options).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Type.GetInterfaceMap() is not supported on UapAot")]
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

    public struct NonGenericStruct { }

    public ref struct RefStruct { }

    public struct GenericStruct<T> { }
    public struct GenericStruct<T, U> { }

    public interface NonGenericInterface { }
    public interface GenericInterface<T> { }
    public interface GenericInterface<T, U> { }
}
