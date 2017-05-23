// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public static partial class UIntPtrTests
    {
        private static unsafe bool Is64Bit => sizeof(void*) == 8;

        [Fact]
        public static void Zero()
        {
            VerifyPointer(UIntPtr.Zero, 0);
        }

        [Fact]
        public static void Ctor_UInt()
        {
            uint i = 42;
            VerifyPointer(new UIntPtr(i), i);
            VerifyPointer((UIntPtr)i, i);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static void Ctor_ULong()
        {
            ulong l = 0x0fffffffffffffff;
            VerifyPointer(new UIntPtr(l), l);
            VerifyPointer((UIntPtr)l, l);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void TestCtor_VoidPointer_ToPointer()
        {
            void* pv = new UIntPtr(42).ToPointer();

            VerifyPointer(new UIntPtr(pv), 42);
            VerifyPointer((UIntPtr)pv, 42);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void TestSize()
        {
            Assert.Equal(sizeof(void*), UIntPtr.Size);
        }

        public static IEnumerable<object[]> Add_TestData()
        {
            yield return new object[] { new UIntPtr(42), 6, (ulong)48 };
            yield return new object[] { new UIntPtr(40), 0, (ulong)40 };
            yield return new object[] { new UIntPtr(38), -2, (ulong)36 };

            yield return new object[] { new UIntPtr(0xffffffffffffffff), 5, unchecked(0x0000000000000004) }; /// Add should not throw an OverflowException
        }

        [ConditionalTheory(nameof(Is64Bit))]
        [MemberData(nameof(Add_TestData))]
        public static void Add(UIntPtr ptr, int offset, ulong expected)
        {
            UIntPtr p1 = UIntPtr.Add(ptr, offset);
            VerifyPointer(p1, expected);

            UIntPtr p2 = ptr + offset;
            VerifyPointer(p2, expected);

            UIntPtr p3 = ptr;
            p3 += offset;
            VerifyPointer(p3, expected);
        }

        public static IEnumerable<object[]> Subtract_TestData()
        {
            yield return new object[] { new UIntPtr(42), 6, (ulong)36 };
            yield return new object[] { new UIntPtr(40), 0, (ulong)40 };
            yield return new object[] { new UIntPtr(38), -2, (ulong)40 };
        }

        [ConditionalTheory(nameof(Is64Bit))]
        [MemberData(nameof(Subtract_TestData))]
        public static void Subtract(UIntPtr ptr, int offset, ulong expected)
        {
            UIntPtr p1 = UIntPtr.Subtract(ptr, offset);
            VerifyPointer(p1, expected);

            UIntPtr p2 = ptr - offset;
            VerifyPointer(p2, expected);

            UIntPtr p3 = ptr;
            p3 -= offset;
            VerifyPointer(p3, expected);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new UIntPtr(42), new UIntPtr(42), true };
            yield return new object[] { new UIntPtr(42), new UIntPtr(43), false };
            yield return new object[] { new UIntPtr(42), 42, false };
            yield return new object[] { new UIntPtr(42), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void Equals(UIntPtr ptr1, object obj, bool expected)
        {
            if (obj is UIntPtr)
            {
                UIntPtr ptr2 = (UIntPtr)obj;
                Assert.Equal(expected, ptr1 == ptr2);
                Assert.Equal(!expected, ptr1 != ptr2);
                Assert.Equal(expected, ptr1.GetHashCode().Equals(ptr2.GetHashCode()));
            }
            Assert.Equal(expected, ptr1.Equals(obj));
            Assert.Equal(ptr1.GetHashCode(), ptr1.GetHashCode());
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void TestImplicitCast()
        {
            var ptr = new UIntPtr(42);

            uint i = (uint)ptr;
            Assert.Equal(42u, i);
            Assert.Equal(ptr, (UIntPtr)i);

            ulong l = (ulong)ptr;
            Assert.Equal(42u, l);
            Assert.Equal(ptr, (UIntPtr)l);

            void* v = (void*)ptr;
            Assert.Equal(ptr, (UIntPtr)v);

            ptr = new UIntPtr(0x7fffffffffffffff);
            Assert.Throws<OverflowException>(() => (uint)ptr);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "This was a bug fix in .NET Core where the hash code should be different")]
        [ConditionalFact(nameof(Is64Bit))]
        public static void GetHashCodeRespectAllBits()
        {
            var ptr1 = new UIntPtr(0x123456FFFFFFFF);
            var ptr2 = new UIntPtr(0x654321FFFFFFFF);
            Assert.NotEqual(ptr1.GetHashCode(), ptr2.GetHashCode());
        }

        private static void VerifyPointer(UIntPtr ptr, ulong expected)
        {
            Assert.Equal(expected, ptr.ToUInt64());

            uint expected32 = unchecked((uint)expected);
            if (expected32 != expected)
            {
                Assert.Throws<OverflowException>(() => ptr.ToUInt32());
                return;
            }

            Assert.Equal(expected32, ptr.ToUInt32());

            Assert.Equal(expected.ToString(), ptr.ToString());

            Assert.Equal(ptr, new UIntPtr(expected));
            Assert.True(ptr == new UIntPtr(expected));
            Assert.False(ptr != new UIntPtr(expected));

            Assert.NotEqual(ptr, new UIntPtr(expected + 1));
            Assert.False(ptr == new UIntPtr(expected + 1));
            Assert.True(ptr != new UIntPtr(expected + 1));
        }
    }
}
