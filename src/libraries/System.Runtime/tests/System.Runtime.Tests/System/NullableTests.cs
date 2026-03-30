// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public static class NullableTests
    {
        [Fact]
        public static void Basics()
        {
            // Nullable and Nullable<T> are mostly verbatim ports so we don't test much here.
            int? n = default(int?);
            Assert.False(n.HasValue);
            Assert.Throws<InvalidOperationException>(() => n.Value);
            Assert.Throws<InvalidOperationException>(() => (int)n);
            Assert.Null(n);
            Assert.NotEqual(7, n);
            Assert.Equal(0, n.GetHashCode());
            Assert.Equal("", n.ToString());
            Assert.Equal(default(int), n.GetValueOrDefault());
            Assert.Equal(999, n.GetValueOrDefault(999));

            n = new int?(42);
            Assert.True(n.HasValue);
            Assert.Equal(42, n.Value);
            Assert.Equal(42, (int)n);
            Assert.NotNull(n);
            Assert.NotEqual(7, n);
            Assert.Equal(42, n);
            Assert.Equal(42.GetHashCode(), n.GetHashCode());
            Assert.Equal(42.ToString(), n.ToString());
            Assert.Equal(42, n.GetValueOrDefault());
            Assert.Equal(42, n.GetValueOrDefault(999));

            n = 88;
            Assert.True(n.HasValue);
            Assert.Equal(88, n.Value);
        }

        [Fact]
        public static void Boxing()
        {
            int? n = new int?(42);
            Unbox(n);
        }

        private static void Unbox(object o)
        {
            Type t = o.GetType();
            Assert.IsNotType<int?>(t);
            Assert.Equal(typeof(int), t);
        }

        [Fact]
        public static void ImplicitCast_T()
        {
            int? nullable = 5;
            Assert.True(nullable.HasValue);
            Assert.Equal(5, nullable.GetValueOrDefault());

            nullable = null;
            Assert.False(nullable.HasValue);
            Assert.Equal(0, nullable.GetValueOrDefault());
        }

        [Fact]
        public static void ExplicitCast_T()
        {
            int? nullable = 5;
            int value = (int)nullable;
            Assert.Equal(5, value);

            nullable = null;
            Assert.Throws<InvalidOperationException>(() => (int)nullable);
        }

        [Theory]
        [InlineData(typeof(int?), typeof(int))]
        [InlineData(typeof(int), null)]
        [InlineData(typeof(G<int>), null)]
        public static void GetUnderlyingType(Type nullableType, Type? expected)
        {
            Assert.Equal(expected, Nullable.GetUnderlyingType(nullableType));
        }

        [Theory]
        [InlineData(typeof(int?), typeof(int))]
        [InlineData(typeof(int), null)]
        [InlineData(typeof(G<int>), null)]
        public static void GetNullableUnderlyingType_RuntimeType(Type type, Type? expected)
        {
            Assert.Equal(expected, type.GetNullableUnderlyingType());
        }

        [Fact]
        public static void GetUnderlyingType_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("nullableType", () => Nullable.GetUnderlyingType((Type)null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public static void GetUnderlyingType_MetadataLoadContext_NullableInt_ReturnsUnderlyingType()
        {
            string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
            var resolver = new PathAssemblyResolver(runtimeAssemblies);
            using var mlc = new MetadataLoadContext(resolver);

            Assembly coreAssembly = mlc.LoadFromAssemblyName("System.Runtime");
            Type intType = coreAssembly.GetType("System.Int32")!;
            Type nullableIntType = coreAssembly.GetType("System.Nullable`1")!.MakeGenericType(intType);

            // Test via Nullable.GetUnderlyingType (forwards to the virtual)
            Type? underlying = Nullable.GetUnderlyingType(nullableIntType);
            Assert.NotNull(underlying);
            Assert.Equal("System.Int32", underlying.FullName);

            // Test via Type.GetNullableUnderlyingType directly
            Type? underlyingDirect = nullableIntType.GetNullableUnderlyingType();
            Assert.NotNull(underlyingDirect);
            Assert.Equal("System.Int32", underlyingDirect.FullName);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public static void GetUnderlyingType_MetadataLoadContext_NonNullableTypes_ReturnsNull()
        {
            string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
            var resolver = new PathAssemblyResolver(runtimeAssemblies);
            using var mlc = new MetadataLoadContext(resolver);

            Assembly coreAssembly = mlc.LoadFromAssemblyName("System.Runtime");
            Type intType = coreAssembly.GetType("System.Int32")!;
            Type stringType = coreAssembly.GetType("System.String")!;
            Type kvpType = coreAssembly.GetType("System.Collections.Generic.KeyValuePair`2")!.MakeGenericType(intType, stringType);

            Assert.Null(Nullable.GetUnderlyingType(intType));
            Assert.Null(Nullable.GetUnderlyingType(stringType));
            Assert.Null(Nullable.GetUnderlyingType(kvpType));

            Assert.Null(intType.GetNullableUnderlyingType());
            Assert.Null(stringType.GetNullableUnderlyingType());
            Assert.Null(kvpType.GetNullableUnderlyingType());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public static void GetUnderlyingType_MetadataLoadContext_OpenNullable_ReturnsNull()
        {
            string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
            var resolver = new PathAssemblyResolver(runtimeAssemblies);
            using var mlc = new MetadataLoadContext(resolver);

            Assembly coreAssembly = mlc.LoadFromAssemblyName("System.Runtime");
            Type openNullableType = coreAssembly.GetType("System.Nullable`1")!;

            Assert.Null(Nullable.GetUnderlyingType(openNullableType));
            Assert.Null(openNullableType.GetNullableUnderlyingType());
        }

        [Fact]
        public static void GetValueRefOrDefaultRef_WithValue()
        {
            static void Test<T>(T before, T after)
                where T : struct
            {
                T? nullable = before;
                ref readonly T reference = ref Nullable.GetValueRefOrDefaultRef(in nullable);

                Assert.Equal(before, nullable!.Value);

                Unsafe.AsRef<T>(in reference) = after;

                Assert.Equal(after, nullable.Value);
            }

            Test((byte)0, (byte)42);
            Test(0, 42);
            Test(1.3f, 3.14f);
            Test(0.555, 8.49);
            Test(Guid.NewGuid(), Guid.NewGuid());
        }

        [Fact]
        public static void GetValueRefOrDefaultRef_WithDefault()
        {
            static void Test<T>()
                where T : struct
            {
                T? nullable = null;
                ref readonly T reference = ref Nullable.GetValueRefOrDefaultRef(in nullable);

                Assert.Equal(nullable!.GetValueOrDefault(), reference);
            }

            Test<byte>();
            Test<int>();
            Test<float>();
            Test<double>();
            Test<Guid>();
        }

        [Fact]
        public static void GetValueRefOrDefaultRef_UnsafeWriteToNullMaintainsExpectedBehavior()
        {
            static void Test<T>(T after)
               where T : struct
            {
                T? nullable = null;
                ref readonly T reference = ref Nullable.GetValueRefOrDefaultRef(in nullable);

                Unsafe.AsRef<T>(in reference) = after;

                Assert.Equal(after, nullable.GetValueOrDefault()); // GetValueOrDefault() unconditionally returns the field
                Assert.False(nullable.HasValue);
                Assert.Equal(0, nullable.GetHashCode()); // GetHashCode() returns 0 if HasValue is false, without reading the field
                Assert.Throws<InvalidOperationException>(() => nullable.Value); // Accessing the value should still throw despite the write
                Assert.Throws<InvalidOperationException>(() => (T)nullable);
            }

            Test((byte)42);
            Test(42);
            Test(3.14f);
            Test(8.49);
            Test(Guid.NewGuid());
        }

        public static IEnumerable<object[]> Compare_Equals_TestData()
        {
            yield return new object[] { default(int?), default(int?), 0 };
            yield return new object[] { new int?(7), default(int?), 1 };
            yield return new object[] { default(int?), new int?(7), -1 };
            yield return new object[] { new int?(7), new int?(7), 0 };
            yield return new object[] { new int?(7), new int?(5), 1 };
            yield return new object[] { new int?(5), new int?(7), -1 };
        }

        [Theory]
        [MemberData(nameof(Compare_Equals_TestData))]
        public static void Compare_Equals(int? n1, int? n2, int expected)
        {
            Assert.Equal(expected == 0, Nullable.Equals(n1, n2));
            Assert.Equal(expected == 0, n1.Equals(n2));
            Assert.Equal(expected, Nullable.Compare(n1, n2));
        }

        [Fact]
        public static void MutatingMethods_MutationsAffectOriginal()
        {
            MutatingStruct? ms = new MutatingStruct() { Value = 1 };

            for (int i = 1; i <= 2; i++)
            {
                Assert.Equal(i.ToString(), ms.Value.ToString());
                Assert.Equal(i, ms.Value.Value);

                Assert.Equal(i.ToString(), ms.ToString());
                Assert.Equal(i + 1, ms.Value.Value);
            }

            for (int i = 3; i <= 4; i++)
            {
                Assert.Equal(i, ms.Value.GetHashCode());
                Assert.Equal(i, ms.Value.Value);

                Assert.Equal(i, ms.GetHashCode());
                Assert.Equal(i + 1, ms.Value.Value);
            }

            for (int i = 5; i <= 6; i++)
            {
                ms.Value.Equals(new object());
                Assert.Equal(i, ms.Value.Value);

                ms.Equals(new object());
                Assert.Equal(i + 1, ms.Value.Value);
            }
        }

        private struct MutatingStruct
        {
            public int Value;
            public override string ToString() => Value++.ToString();
            public override bool Equals(object obj) => Value++.Equals(null);
            public override int GetHashCode() => Value++.GetHashCode();
        }

        public class G<T> { }
    }
}
