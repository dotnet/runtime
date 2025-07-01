// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public class ImmutableCollectionsMarshalTest
    {
        [Fact]
        public void AsImmutableArrayFromNullArray()
        {
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray<int>(null).IsDefault);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray<int?>(null).IsDefault);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray<Guid>(null).IsDefault);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray<Guid?>(null).IsDefault);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray<string>(null).IsDefault);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray<CustomClass>(null).IsDefault);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray<ManagedCustomStruct>(null).IsDefault);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray<ManagedCustomStruct?>(null).IsDefault);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray<UnmanagedCustomStruct>(null).IsDefault);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray<UnmanagedCustomStruct?>(null).IsDefault);
        }

        [Fact]
        public void AsImmutableArrayFromEmptyArray()
        {
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray(Array.Empty<int>()).IsEmpty);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray(Array.Empty<int?>()).IsEmpty);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray(Array.Empty<Guid>()).IsEmpty);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray(Array.Empty<Guid?>()).IsEmpty);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray(Array.Empty<string>()).IsEmpty);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray(Array.Empty<CustomClass>()).IsEmpty);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray(Array.Empty<ManagedCustomStruct>()).IsEmpty);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray(Array.Empty<ManagedCustomStruct?>()).IsEmpty);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray(Array.Empty<UnmanagedCustomStruct>()).IsEmpty);
            Assert.True(ImmutableCollectionsMarshal.AsImmutableArray(Array.Empty<UnmanagedCustomStruct?>()).IsEmpty);
        }

        [Fact]
        public void AsImmutableArrayFromExistingArray()
        {
            static void Test<T>()
            {
                T[] array = new T[17];
                ImmutableArray<T> immutableArray = ImmutableCollectionsMarshal.AsImmutableArray(array);

                Assert.False(immutableArray.IsDefault);
                Assert.Equal(17, immutableArray.Length);

                ref T expectedRef = ref array[0];
                ref T actualRef = ref Unsafe.AsRef(in immutableArray.ItemRef(0));

                Assert.True(Unsafe.AreSame(ref expectedRef, ref actualRef));
            }

            Test<int>();
            Test<int?>();
            Test<Guid>();
            Test<Guid?>();
            Test<string>();
            Test<CustomClass>();
            Test<ManagedCustomStruct>();
            Test<ManagedCustomStruct?>();
            Test<UnmanagedCustomStruct>();
            Test<UnmanagedCustomStruct?>();
        }

        [Fact]
        public void AsArrayFromDefaultImmutableArray()
        {
            Assert.Null(ImmutableCollectionsMarshal.AsArray<int>(default));
            Assert.Null(ImmutableCollectionsMarshal.AsArray<int?>(default));
            Assert.Null(ImmutableCollectionsMarshal.AsArray<Guid>(default));
            Assert.Null(ImmutableCollectionsMarshal.AsArray<Guid?>(default));
            Assert.Null(ImmutableCollectionsMarshal.AsArray<string>(default));
            Assert.Null(ImmutableCollectionsMarshal.AsArray<CustomClass>(default));
            Assert.Null(ImmutableCollectionsMarshal.AsArray<ManagedCustomStruct>(default));
            Assert.Null(ImmutableCollectionsMarshal.AsArray<ManagedCustomStruct?>(default));
            Assert.Null(ImmutableCollectionsMarshal.AsArray<UnmanagedCustomStruct>(default));
            Assert.Null(ImmutableCollectionsMarshal.AsArray<UnmanagedCustomStruct?>(default));
        }

        [Fact]
        public void AsArrayFromEmptyImmutableArray()
        {
            static void Test<T>()
            {
                T[]? array = ImmutableCollectionsMarshal.AsArray(ImmutableArray<T>.Empty);

                Assert.NotNull(array);
                Assert.Empty(array);
            }

            Test<int>();
            Test<int?>();
            Test<Guid>();
            Test<Guid?>();
            Test<string>();
            Test<CustomClass>();
            Test<ManagedCustomStruct>();
            Test<ManagedCustomStruct?>();
            Test<UnmanagedCustomStruct>();
            Test<UnmanagedCustomStruct?>();
        }

        [Fact]
        public void AsArrayFromConstructedImmutableArray()
        {
            static void Test<T>()
            {
                ImmutableArray<T> immutableArray = ImmutableArray.Create(new T[17]);
                T[]? array = ImmutableCollectionsMarshal.AsArray(immutableArray);

                Assert.NotNull(array);
                Assert.Equal(17, array.Length);

                ref T expectedRef = ref Unsafe.AsRef(in immutableArray.ItemRef(0));
                ref T actualRef = ref array[0];

                Assert.True(Unsafe.AreSame(ref expectedRef, ref actualRef));
            }

            Test<int>();
            Test<int?>();
            Test<Guid>();
            Test<Guid?>();
            Test<string>();
            Test<CustomClass>();
            Test<ManagedCustomStruct>();
            Test<ManagedCustomStruct?>();
            Test<UnmanagedCustomStruct>();
            Test<UnmanagedCustomStruct?>();
        }

        [Fact]
        public void AsMemoryFromNullBuilder()
        {
            Memory<Guid> m = ImmutableCollectionsMarshal.AsMemory<Guid>(null);
            Assert.True(m.IsEmpty);
            Assert.True(MemoryMarshal.TryGetArray(m, out ArraySegment<Guid> segment));
            Assert.NotNull(segment.Array);
            Assert.Equal(0, segment.Array.Length);
            Assert.Equal(0, segment.Offset);
            Assert.Equal(0, segment.Count);
        }

        [Fact]
        public void AsMemoryFromEmptyBuilder()
        {
            Memory<string> m = ImmutableCollectionsMarshal.AsMemory(ImmutableArray.CreateBuilder<string>());
            Assert.True(m.IsEmpty);
            Assert.True(MemoryMarshal.TryGetArray(m, out ArraySegment<string> segment));
            Assert.NotNull(segment.Array);
            Assert.Equal(0, segment.Offset);
            Assert.Equal(0, segment.Count);
        }

        [Fact]
        public void AsMemoryFromNonEmptyBuilder()
        {
            ImmutableArray<int>.Builder builder = ImmutableArray.CreateBuilder<int>(1);
            builder.Add(42);
            builder.Add(43);
            builder.Add(44);

            Memory<int> m1 = ImmutableCollectionsMarshal.AsMemory(builder);
            Assert.Equal(3, m1.Length);

            Assert.True(MemoryMarshal.TryGetArray(m1, out ArraySegment<int> segment1));
            Assert.NotNull(segment1.Array);
            Assert.Equal(0, segment1.Offset);
            Assert.Equal(3, segment1.Count);

            Span<int> span = m1.Span;
            Assert.Equal(42, span[0]);
            Assert.Equal(43, span[1]);
            Assert.Equal(44, span[2]);

            Memory<int> m2 = ImmutableCollectionsMarshal.AsMemory(builder);
            Assert.Equal(3, m2.Length);
            Assert.True(MemoryMarshal.TryGetArray(m2, out ArraySegment<int> segment2));

            Assert.Same(segment1.Array, segment2.Array);
            Assert.Equal(segment1.Offset, segment2.Offset);
            Assert.Equal(segment1.Count, segment2.Count);
        }

        public class CustomClass
        {
            public object Foo;
            public Guid Bar;
        }

        public struct ManagedCustomStruct
        {
            public object Foo;
            public Guid Bar;
        }

        public struct UnmanagedCustomStruct
        {
            public Guid Foo;
            public int Bar;
        }
    }
}
