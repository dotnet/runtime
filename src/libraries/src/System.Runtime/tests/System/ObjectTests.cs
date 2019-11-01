// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public static class ObjectTests
    {
        public static IEnumerable<object[]> Equals_TestData()
        {
            var obj1 = new object();
            var obj2 = new object();

            yield return new object[] { obj1, obj1, true };
            yield return new object[] { obj1, null, false };
            yield return new object[] { obj1, obj2, false };

            yield return new object[] { null, null, true };
            yield return new object[] { null, obj1, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void Equals(object obj1, object obj2, bool expected)
        {
            if (obj1 != null)
            {
                Assert.Equal(expected, obj1.Equals(obj2));
                Assert.True(obj1.Equals(obj1));
                Assert.Equal(obj1.GetHashCode(), obj1.GetHashCode());
                if (obj2 != null)
                {
                    Assert.Equal(expected, obj1.GetHashCode().Equals(obj2.GetHashCode()));
                }
            }
            Assert.Equal(expected, Equals(obj1, obj2));
        }

        [Fact]
        public static void ReferenceEquals()
        {
            var e1 = new EOverrider(7);
            var e2 = new EOverrider(8);

            EOverrider.s_EqualsCalled = false;
            Assert.True(Equals(e1, e1));
            Assert.False(EOverrider.s_EqualsCalled);

            // ReferenceEquals should not call Equals
            EOverrider.s_EqualsCalled = false;
            Assert.False(ReferenceEquals(e1, e2));
            Assert.False(EOverrider.s_EqualsCalled);

            EOverrider.s_EqualsCalled = false;
            Assert.True(ReferenceEquals(e1, e1));
            Assert.False(EOverrider.s_EqualsCalled);

            EOverrider.s_EqualsCalled = false;
            Assert.True(ReferenceEquals(e1, e1));
            Assert.False(EOverrider.s_EqualsCalled);

            EOverrider.s_EqualsCalled = false;
            Assert.False(ReferenceEquals(e1, null));
            Assert.False(EOverrider.s_EqualsCalled);

            EOverrider.s_EqualsCalled = false;
            Assert.False(ReferenceEquals(null, e1));
            Assert.False(EOverrider.s_EqualsCalled);
        }

        public static IEnumerable<object[]> GetType_TestData()
        {
            yield return new object[] { new object(), typeof(object) };
            yield return new object[] { new C("Hello", 7, 9), typeof(C) };
            yield return new object[] { new Generic<string>(), typeof(Generic<string>) };
            yield return new object[] { new int[3], typeof(int[]) };
        }

        [Theory]
        [MemberData(nameof(GetType_TestData))]
        public static void GetType(object obj, Type expected)
        {
            Assert.Equal(expected, obj.GetType());
        }

        [Fact]
        public static void ToStringTest()
        {
            var obj = new object();
            Assert.Equal("System.Object", obj.ToString());
            Assert.Equal(obj.ToString(), obj.GetType().ToString());
        }

        [Fact]
        public static void MemberwiseCloneTest()
        {
            var emptyClass = new EmptyClass();
            Assert.IsType<EmptyClass>(emptyClass.CallMemberwiseClone());

            var c1 = new C("Hello", 7, 8);
            var c2 = c1.CallMemberwiseClone();
            Assert.Equal("Hello", c2.s);
            Assert.Equal(7, c2.x);
            Assert.Equal(8, c2.y);

            var s = new S(0x123456789);
            Assert.Equal(0x123456789, s.CallMemberwiseClone().a);
        }

        private class EmptyClass
        {
            public object CallMemberwiseClone() => MemberwiseClone();
        }

        private class C
        {
            public C(string s, int x, int y)
            {
                this.s = s;
                this.x = x;
                this.y = y;
            }

            public string s;
            public int x;
            public int y;

            public C CallMemberwiseClone() => (C)MemberwiseClone();
        }

        private struct S
        {
            public S(long a)
            {
                this.a = a;
            }

            public long a;

            public S CallMemberwiseClone() => (S)MemberwiseClone();
        }

        private class Generic<T> { }

        private class EOverrider
        {
            public EOverrider(int x)
            {
                X = x;
            }

            public override bool Equals(object obj)
            {
                s_EqualsCalled = true;

                EOverrider eo = obj as EOverrider;
                if (eo == null)
                    return false;
                return eo.X == X;
            }

            public override int GetHashCode() => 42;

            public int X;

            public static bool s_EqualsCalled = false;
        }
    }
}
