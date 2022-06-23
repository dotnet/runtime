// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Buffers;
using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.InteropServices;
using System.MemoryTests;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        // Calling Span APIs via Reflection is not supported yet.
        // These tests check that using reflection results in graceful failures. See https://github.com/dotnet/runtime/issues/10057
        // These tests are only relevant for fast span.

        [Fact]
        public static void MemoryExtensions_StaticReturningReadOnlySpan()
        {
            Type type = typeof(MemoryExtensions);

            MethodInfo method = type.GetMethod(nameof(MemoryExtensions.AsSpan), new Type[] { typeof(string) });
            Assert.Throws<NotSupportedException>(() => method.Invoke(null, new object[] { "Hello" }));

            method = type.GetMethod(nameof(MemoryExtensions.AsSpan), new Type[] { typeof(string), typeof(int), typeof(int) });
            Assert.Throws<NotSupportedException>(() => method.Invoke(null, new object[] { "Hello", 1, 1 }));
        }

        [Fact]
        public static void MemoryExtensions_StaticWithSpanArguments()
        {
            Type type = typeof(MemoryExtensions);
            MethodInfo method = type.GetMethod(nameof(MemoryExtensions.CompareTo));
            Assert.Throws<NotSupportedException>(() => method.Invoke(null, new object[] { default, default, StringComparison.Ordinal }));
        }

        [Fact]
        public static void BinaryPrimitives_StaticWithSpanArgument()
        {
            Type type = typeof(BinaryPrimitives);

            MethodInfo method = type.GetMethod(nameof(BinaryPrimitives.ReadInt16LittleEndian));
            Assert.Throws<NotSupportedException>(() => method.Invoke(null, new object[] { default }));

            method = type.GetMethod(nameof(BinaryPrimitives.TryReadInt16LittleEndian));
            Assert.Throws<NotSupportedException>(() => method.Invoke(null, new object[] { default, null }));
        }

        [Fact]
        public static void MemoryMarshal_GenericStaticReturningSpan()
        {
            Type type = typeof(MemoryMarshal);

            int value = 0;
            ref int refInt = ref value;

            MethodInfo method = type.GetMethod(nameof(MemoryMarshal.CreateSpan)).MakeGenericMethod((refInt.GetType()));
            Assert.Throws<NotSupportedException>(() => method.Invoke(null, new object[] { null, 0 }));
        }

        [Fact]
        public static void Span_Constructor()
        {
            Type type = typeof(Span<int>);

            ConstructorInfo ctor = type.GetConstructor(new Type[] { typeof(int[]) });
            Assert.Throws<TargetException>(() => ctor.Invoke(new object[] { new int[10] }));

            ctor = type.GetConstructor(new Type[] { typeof(int[]), typeof(int), typeof(int) });
            Assert.Throws<TargetException>(() => ctor.Invoke(new object[] { new int[10], 1, 1 }));

            ctor = type.GetConstructor(new Type[] { typeof(void*), typeof(int) });
            Assert.Throws<TargetException>(() => ctor.Invoke(new object[] { null, 1 }));
        }

        [Fact]
        public static void Span_Property()
        {
            Type type = typeof(Span<int>);

            PropertyInfo property = type.GetProperty(nameof(Span<int>.Empty));
            Assert.Throws<NotSupportedException>(() => property.GetValue(default));
        }

        [Fact]
        public static void Span_StaticOperator()
        {
            Type type = typeof(Span<int>);

            MethodInfo method = type.GetMethod("op_Equality");
            Assert.Throws<NotSupportedException>(() => method.Invoke(null, new object[] { default, default }));

            method = type.GetMethod("op_Inequality");
            Assert.Throws<NotSupportedException>(() => method.Invoke(null, new object[] { default, default }));
        }

        [Fact]
        public static void Span_InstanceMethod()
        {
            Type type = typeof(Span<int>);

            MethodInfo method = type.GetMethod(nameof(Span<int>.CopyTo), new Type[] { typeof(Span<int>) });
            Assert.Throws<NotSupportedException>(() => method.Invoke(default, new object[] { default }));
        }

        [Fact]
        public static void ReadOnlySpan_Constructor()
        {
            Type type = typeof(ReadOnlySpan<int>);

            ConstructorInfo ctor = type.GetConstructor(new Type[] { typeof(int[]) });
            Assert.Throws<TargetException>(() => ctor.Invoke(new object[] { new int[10] }));

            ctor = type.GetConstructor(new Type[] { typeof(int[]), typeof(int), typeof(int) });
            Assert.Throws<TargetException>(() => ctor.Invoke(new object[] { new int[10], 1, 1 }));

            ctor = type.GetConstructor(new Type[] { typeof(void*), typeof(int) });
            Assert.Throws<TargetException>(() => ctor.Invoke(new object[] { null, 1 }));
        }

        [Fact]
        public static void ReadOnlySpan_Property()
        {
            Type type = typeof(ReadOnlySpan<int>);

            PropertyInfo property = type.GetProperty(nameof(ReadOnlySpan<int>.Empty));
            Assert.Throws<NotSupportedException>(() => property.GetValue(default));
        }

        [Fact]
        public static void ReadOnlySpan_Operator()
        {
            Type type = typeof(ReadOnlySpan<int>);

            MethodInfo method = type.GetMethod("op_Equality");
            Assert.Throws<NotSupportedException>(() => method.Invoke(null, new object[] { default, default }));

            method = type.GetMethod("op_Inequality");
            Assert.Throws<NotSupportedException>(() => method.Invoke(null, new object[] { default, default }));
        }

        [Fact]
        public static void ReadOnlySpan_InstanceMethod()
        {
            Type type = typeof(ReadOnlySpan<int>);

            MethodInfo method = type.GetMethod(nameof(ReadOnlySpan<int>.CopyTo), new Type[] { typeof(Span<int>) });
            Assert.Throws<NotSupportedException>(() => method.Invoke(default, new object[] { default }));
        }

        [Fact]
        public static void Memory_PropertyReturningSpan()
        {
            Type type = typeof(Memory<int>);

            PropertyInfo property = type.GetProperty(nameof(Memory<int>.Span));
            Assert.Throws<NotSupportedException>(() => property.GetValue(null));
        }

        [Fact]
        public static void ReadOnlyMemory_PropertyReturningReadOnlySpan()
        {
            Type type = typeof(ReadOnlyMemory<int>);

            PropertyInfo property = type.GetProperty(nameof(ReadOnlyMemory<int>.Span));
            Assert.Throws<NotSupportedException>(() => property.GetValue(null));
        }

        [Fact]
        public static void MemoryManager_MethodReturningSpan()
        {
            Type type = typeof(MemoryManager<int>);

            MemoryManager<int> manager = new CustomMemoryForTest<int>(new int[10]);
            MethodInfo method = type.GetMethod(nameof(MemoryManager<int>.GetSpan), BindingFlags.Public | BindingFlags.Instance);
            Assert.Throws<NotSupportedException>(() => method.Invoke(manager, null));
        }
    }
}
