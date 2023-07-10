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
