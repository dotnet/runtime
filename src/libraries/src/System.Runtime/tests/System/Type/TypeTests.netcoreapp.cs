// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests
{
    public class TypeTestsNetcore
    {
        private static readonly IList<Type> NonArrayBaseTypes;

        static TypeTestsNetcore()
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot)]
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
                yield return new object[] { typeof(DummyGenericClassForTypeTestsNetcore<>), false, false, false };
                yield return new object[] { typeof(DummyGenericClassForTypeTestsNetcore<int>), false, false, false };
                if (PlatformDetection.IsWindows) // GetTypeFromCLSID is Windows only
                {
                    yield return new object[] { Type.GetTypeFromCLSID(default(Guid)), false, false, false };
                }

                Type theT = typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0];
                yield return new object[] { theT, true, true, false };

                Type theM = typeof(TypeTestsNetcore).GetMethod(nameof(GenericMethod), BindingFlags.NonPublic | BindingFlags.Static).GetGenericArguments()[0];
                yield return new object[] { theM, true, false, true };
            }
        }

        private static void GenericMethod<M>() { }
    }
}

internal class DummyGenericClassForTypeTestsNetcore<T> { }
