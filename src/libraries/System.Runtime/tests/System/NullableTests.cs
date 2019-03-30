// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
            Assert.Equal(null, n);
            Assert.NotEqual(7, n);
            Assert.Equal(0, n.GetHashCode());
            Assert.Equal("", n.ToString());
            Assert.Equal(default(int), n.GetValueOrDefault());
            Assert.Equal(999, n.GetValueOrDefault(999));

            n = new int?(42);
            Assert.True(n.HasValue);
            Assert.Equal(42, n.Value);
            Assert.Equal(42, (int)n);
            Assert.NotEqual(null, n);
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
        public static void GetUnderlyingType(Type nullableType, Type expected)
        {
            Assert.Equal(expected, Nullable.GetUnderlyingType(nullableType));
        }

        [Fact]
        public static void GetUnderlyingType_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("nullableType", () => Nullable.GetUnderlyingType((Type)null));
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
