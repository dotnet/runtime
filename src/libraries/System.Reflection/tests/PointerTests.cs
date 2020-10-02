// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Reflection;

namespace System.Reflection.Tests
{
    public class PointerTests
    {
        public unsafe struct BitwiseComparable
        {
            public int* PublicInt;
        }

        public unsafe struct MemberwiseComparable
        {
            public string ReferenceType;
            public int* PublicInt;
        }

        [Fact]
        public unsafe void BitwiseEquality_AreEqual()
        {
            int someNumber = 1;
            var a = new BitwiseComparable();
            a.PublicInt = &someNumber;
            BitwiseComparable b = a;

            Assert.True(a.Equals(b));
        }

        [Fact]
        public unsafe void BitwiseEquality_EqualWithSelf()
        {
            int someNumber = 1;
            var a = new BitwiseComparable();
            a.PublicInt = &someNumber;

            Assert.True(a.Equals(a));
        }

        [Fact]
        public unsafe void MemberwiseEquality_AreEqual()
        {
            int someNumber = 1;
            var a = new MemberwiseComparable();
            a.PublicInt = &someNumber;
            MemberwiseComparable b = a;

            Assert.True(a.Equals(b));
        }

        [Fact]
        public unsafe void MemberwiseEquality_EqualWithSelf()
        {
            int someNumber = 1;
            var a = new MemberwiseComparable();
            a.PublicInt = &someNumber;

            Assert.True(a.Equals(a));
        }

        [Fact]
        public unsafe void Nullptrs_AreEqual()
        {
            object a = Pointer.Box(null, typeof(int*));
            object b = Pointer.Box(null, typeof(int*));

            Assert.True(a.Equals(b));
        }

        [Fact]
        public unsafe void DifferentPointerTypes_AreEqual()
        {
            object a = Pointer.Box((void*)0x12340000, typeof(int*));
            object b = Pointer.Box((void*)0x12340000, typeof(uint*));

            Assert.True(a.Equals(b));
        }
    }
}
