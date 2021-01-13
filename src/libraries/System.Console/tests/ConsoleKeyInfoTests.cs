// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public class ConsoleKeyInfoTests
    {
        [Fact]
        public void Ctor_DefaultCtor_PropertiesReturnDefaults()
        {
            ConsoleKeyInfo cki = new ConsoleKeyInfo();
            Assert.Equal(default(ConsoleKey), cki.Key);
            Assert.Equal(default(char), cki.KeyChar);
            Assert.Equal(default(ConsoleModifiers), cki.Modifiers);
        }

        [Theory]
        [MemberData(nameof(AllCombinationsOfThreeBools))]
        public void Ctor_ValueCtor_ValuesPassedToProperties(bool shift, bool alt, bool ctrl)
        {
            ConsoleKeyInfo cki = new ConsoleKeyInfo('a', ConsoleKey.A, shift, alt, ctrl);

            Assert.Equal(ConsoleKey.A, cki.Key);
            Assert.Equal('a', cki.KeyChar);

            Assert.Equal(shift, (cki.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift);
            Assert.Equal(alt, (cki.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt);
            Assert.Equal(ctrl, (cki.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control);
        }

        [Theory]
        [MemberData(nameof(SampleConsoleKeyInfos))]
        public void Equals_SameData(ConsoleKeyInfo cki)
        {
            ConsoleKeyInfo other = cki; // otherwise compiler warns about comparing the instance with itself

            Assert.True(cki.Equals((object)other));
            Assert.True(cki.Equals(other));
            Assert.True(cki == other);
            Assert.False(cki != other);

            Assert.Equal(cki.GetHashCode(), other.GetHashCode());
        }

        [Theory]
        [MemberData(nameof(NotEqualConsoleKeyInfos))]
        public void NotEquals_DifferentData(ConsoleKeyInfo left, ConsoleKeyInfo right)
        {
            Assert.False(left == right);
            Assert.False(left.Equals(right));
            Assert.False(left.Equals((object)right));
            Assert.True(left != right);
        }

        [Theory]
        [MemberData(nameof(NotEqualConsoleKeyInfos))]
        public void HashCodeNotEquals_DifferentData(ConsoleKeyInfo left, ConsoleKeyInfo right)
        {
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Fact]
        public void NotEquals_Object()
        {
            Assert.False(default(ConsoleKeyInfo).Equals(null));
            Assert.False(default(ConsoleKeyInfo).Equals(new object()));
        }

        [Fact]
        public void ImplementsIEquatableInterface() => Assert.True(typeof(IEquatable<ConsoleKeyInfo>).IsAssignableFrom(typeof(ConsoleKeyInfo)));

        public static readonly object[][] NotEqualConsoleKeyInfos = {
            new object[] { new ConsoleKeyInfo('a', ConsoleKey.A, true, true, true), new ConsoleKeyInfo('b', ConsoleKey.A, true, true, true)  },
            new object[] { new ConsoleKeyInfo('a', ConsoleKey.A, true, true, true), new ConsoleKeyInfo('a', ConsoleKey.B, true, true, true)  },
            new object[] { new ConsoleKeyInfo('a', ConsoleKey.A, true, true, true), new ConsoleKeyInfo('a', ConsoleKey.A, false, true, true) },
            new object[] { new ConsoleKeyInfo('a', ConsoleKey.A, true, true, true), new ConsoleKeyInfo('a', ConsoleKey.A, true, false, true) },
            new object[] { new ConsoleKeyInfo('a', ConsoleKey.A, true, true, true), new ConsoleKeyInfo('a', ConsoleKey.A, true, true, false) }
        };

        public static readonly object[][] SampleConsoleKeyInfos = {
            new object[] { new ConsoleKeyInfo() },
            new object[] { new ConsoleKeyInfo('a', ConsoleKey.A, true, false, true) },
            new object[] { new ConsoleKeyInfo('b', ConsoleKey.B, false, true, true) },
            new object[] { new ConsoleKeyInfo('c', ConsoleKey.C, true, true, false) },
        };

        public static IEnumerable<object[]> AllCombinationsOfThreeBools()
        {
            var bools = new[] { true, false };
            foreach (var one in bools)
                foreach (var two in bools)
                    foreach (var three in bools)
                        yield return new object[] { one, two, three };
        }
    }
}
