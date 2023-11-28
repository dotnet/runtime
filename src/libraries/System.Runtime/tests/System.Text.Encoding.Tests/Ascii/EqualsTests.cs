// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using Xunit;

namespace System.Text.Tests
{
    public abstract class AsciiEqualityTests<TLeft, TRight>
        where TLeft : unmanaged
        where TRight : unmanaged
    {
        protected abstract bool Equals(string left, string right);
        protected abstract bool EqualsIgnoreCase(string left, string right);
        protected abstract bool Equals(byte[] left, byte[] right);
        protected abstract bool EqualsIgnoreCase(byte[] left, byte[] right);
        protected abstract bool Equals(ReadOnlySpan<TLeft> left, ReadOnlySpan<TRight> right);
        protected abstract bool EqualsIgnoreCase(ReadOnlySpan<TLeft> left, ReadOnlySpan<TRight> right);

        public static IEnumerable<object[]> ValidAsciiInputs
        {
            get
            {
                yield return new object[] { "test" };

                for (char textLength = (char)0; textLength <= 127; textLength++)
                {
                    yield return new object[] { new string(textLength, textLength) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValidAsciiInputs))]
        public void Equals_ExactlyTheSameInputs_ReturnsTrue(string input)
        {
            Assert.True(Equals(input, input)); // reference equality
            Assert.True(Equals(input, new StringBuilder(input).ToString())); // content equality
        }

        [Theory]
        [MemberData(nameof(ValidAsciiInputs))]
        public void EqualsIgnoreCase_ExactlyTheSameInputs_ReturnsTrue(string input)
        {
            Assert.True(EqualsIgnoreCase(input, input)); // reference equality
            Assert.True(EqualsIgnoreCase(input, new StringBuilder(input).ToString())); // content equality
        }

        public static IEnumerable<object[]> DifferentInputs
        {
            get
            {
                yield return new object[] { "tak", "nie" };

                for (char i = (char)1; i <= 127; i++)
                {
                    if (i != '?') // ASCIIEncoding maps invalid ASCII to ?
                    {
                        yield return new object[] { new string(i, i), string.Create(i, i, (destination, iteration) =>
                        {
                            destination.Fill(iteration);
                            destination[iteration / 2] = (char)128;
                        })};
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(DifferentInputs))]
        public void Equals_DifferentInputs_ReturnsFalse(string left, string right)
        {
            Assert.False(Equals(left, right));
            Assert.False(Equals(right, left));
        }

        [Theory]
        [MemberData(nameof(DifferentInputs))]
        public void EqualsIgnoreCase_DifferentInputs_ReturnsFalse(string left, string right)
        {
            Assert.False(EqualsIgnoreCase(left, right));
            Assert.False(EqualsIgnoreCase(right, left));
        }

        public static IEnumerable<object[]> EqualIgnoringCaseConsiderations
        {
            get
            {
                yield return new object[] { "aBc", "AbC" };

                for (char i = (char)0; i <= 127; i++)
                {
                    char left = i;
                    char right = char.IsAsciiLetterUpper(left) ? char.ToLower(left) : char.IsAsciiLetterLower(left) ? char.ToUpper(left) : left;
                    yield return new object[] { new string(left, i), new string(right, i) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(EqualIgnoringCaseConsiderations))]
        public void EqualIgnoreCase_EqualIgnoringCaseConsiderations_ReturnsTrue(string left, string right)
        {
            Assert.True(EqualsIgnoreCase(left, right));
            Assert.True(EqualsIgnoreCase(right, left));
        }

        public static IEnumerable<object[]> ContainingNonAsciiCharactersBuffers
        {
            get
            {
                foreach (int length in new[] { 1, Vector128<byte>.Count - 1, Vector128<byte>.Count, Vector256<byte>.Count + 1 })
                {
                    for (int index = 0; index < length; index++)
                    {
                        yield return new object[] { Create(length, index) };
                    }
                }

                static byte[] Create(int length, int invalidCharacterIndex)
                {
                    byte[] buffer = Enumerable.Repeat(GetNextValidAsciiByte(), length).ToArray();
                    buffer[invalidCharacterIndex] = GetNextInvalidAsciiByte();

                    Assert.False(Ascii.IsValid(buffer));

                    return buffer;
                }

                static byte GetNextValidAsciiByte() => (byte)Random.Shared.Next(0, 127 + 1);

                static byte GetNextInvalidAsciiByte() => (byte)Random.Shared.Next(128, 255 + 1);
            }
        }

        [Theory]
        [MemberData(nameof(ContainingNonAsciiCharactersBuffers))]
        public void Equals_EqualValues_ButNonAscii_ReturnsFalse(byte[] input)
            => Assert.False(Equals(input, input));

        [Theory]
        [MemberData(nameof(ContainingNonAsciiCharactersBuffers))]
        public void EqualsIgnoreCase_EqualValues_ButNonAscii_ReturnsFalse(byte[] input)
            => Assert.False(EqualsIgnoreCase(input, input));

        [Theory]
        [InlineData(PoisonPagePlacement.After, PoisonPagePlacement.After)]
        [InlineData(PoisonPagePlacement.After, PoisonPagePlacement.Before)]
        [InlineData(PoisonPagePlacement.Before, PoisonPagePlacement.After)]
        [InlineData(PoisonPagePlacement.Before, PoisonPagePlacement.Before)]
        public void Boundaries_Are_Respected(PoisonPagePlacement leftPoison, PoisonPagePlacement rightPoison)
        {
            for (int size = 1; size < 129; size++)
            {
                using BoundedMemory<TLeft> left = BoundedMemory.Allocate<TLeft>(size, leftPoison);
                using BoundedMemory<TRight> right = BoundedMemory.Allocate<TRight>(size, rightPoison);

                left.Span.Fill(default);
                right.Span.Fill(default);

                left.MakeReadonly();
                right.MakeReadonly();

                Assert.True(Equals(left.Span, right.Span));
                Assert.True(EqualsIgnoreCase(left.Span, right.Span));
            }
        }
    }

    public class AsciiEqualityTests_Byte_Byte : AsciiEqualityTests<byte, byte>
    {
        protected override bool Equals(string left, string right)
            => Ascii.Equals(Encoding.ASCII.GetBytes(left), Encoding.ASCII.GetBytes(right));

        protected override bool EqualsIgnoreCase(string left, string right)
            => Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), Encoding.ASCII.GetBytes(right));

        protected override bool Equals(byte[] left, byte[] right)
            => Ascii.Equals(left, right);

        protected override bool EqualsIgnoreCase(byte[] left, byte[] right)
            => Ascii.EqualsIgnoreCase(left, right);

        protected override bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
            => Ascii.Equals(left, right);

        protected override bool EqualsIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
            => Ascii.EqualsIgnoreCase(left, right);
    }

    public class AsciiEqualityTests_Byte_Char : AsciiEqualityTests<byte, char>
    {
        protected override bool Equals(string left, string right)
            => Ascii.Equals(Encoding.ASCII.GetBytes(left), right);

        protected override bool EqualsIgnoreCase(string left, string right)
            => Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), right);

        protected override bool Equals(byte[] left, byte[] right)
            => Ascii.Equals(left, right.Select(b => (char)b).ToArray());

        protected override bool EqualsIgnoreCase(byte[] left, byte[] right)
            => Ascii.EqualsIgnoreCase(left, right.Select(b => (char)b).ToArray());

        protected override bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<char> right)
            => Ascii.Equals(left, right);

        protected override bool EqualsIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<char> right)
            => Ascii.EqualsIgnoreCase(left, right);
    }

    public class AsciiEqualityTests_Char_Byte : AsciiEqualityTests<char, byte>
    {
        protected override bool Equals(string left, string right)
            => Ascii.Equals(left, Encoding.ASCII.GetBytes(right));

        protected override bool EqualsIgnoreCase(string left, string right)
            => Ascii.EqualsIgnoreCase(left, Encoding.ASCII.GetBytes(right));

        protected override bool Equals(byte[] left, byte[] right)
            => Ascii.Equals(left.Select(b => (char)b).ToArray(), right);

        protected override bool EqualsIgnoreCase(byte[] left, byte[] right)
            => Ascii.EqualsIgnoreCase(left.Select(b => (char)b).ToArray(), right);

        protected override bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<byte> right)
            => Ascii.Equals(left, right);

        protected override bool EqualsIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<byte> right)
            => Ascii.EqualsIgnoreCase(left, right);
    }

    public class AsciiEqualityTests_Char_Char : AsciiEqualityTests<char, char>
    {
        protected override bool Equals(string left, string right)
            => Ascii.Equals(left, right);

        protected override bool EqualsIgnoreCase(string left, string right)
            => Ascii.EqualsIgnoreCase(left, right);

        protected override bool Equals(byte[] left, byte[] right)
            => Ascii.Equals(left.Select(b => (char)b).ToArray(), right.Select(b => (char)b).ToArray());

        protected override bool EqualsIgnoreCase(byte[] left, byte[] right)
            => Ascii.EqualsIgnoreCase(left.Select(b => (char)b).ToArray(), right.Select(b => (char)b).ToArray());

        protected override bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
            => Ascii.Equals(left, right);

        protected override bool EqualsIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
            => Ascii.EqualsIgnoreCase(left, right);
    }
}
