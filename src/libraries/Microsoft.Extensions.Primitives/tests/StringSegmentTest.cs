// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Primitives
{
    public class StringSegmentTest
    {
        [Fact]
        public void StringSegment_Empty()
        {
            // Arrange & Act
            StringSegment segment = StringSegment.Empty;

            // Assert
            Assert.True(segment.HasValue);
            Assert.Same(string.Empty, segment.Value);
            Assert.Equal(0, segment.Offset);
            Assert.Equal(0, segment.Length);
        }

        [Fact]
        public void StringSegment_ImplicitConvertFromString()
        {
            StringSegment segment = "Hello";

            Assert.True(segment.HasValue);
            Assert.Equal(0, segment.Offset);
            Assert.Equal(5, segment.Length);
            Assert.Equal("Hello", segment.Value);
        }

        [Fact]
        public void StringSegment_AsSpan()
        {
            var segment = new StringSegment("Hello");

            ReadOnlySpan<char> span = segment.AsSpan();

            Assert.Equal("Hello", span.ToString());
        }

        [Fact]
        public void StringSegment_AsSpanStart_Valid()
        {
            var segment = new StringSegment("Hello, World!", 1, 4);

            ReadOnlySpan<char> result = segment.AsSpan(start: 1);

            Assert.Equal("llo", result.ToString());
        }

        [Fact]
        public void StringSegment_AsSpan_Valid()
        {
            var segment = new StringSegment("Hello, World!", 1, 4);

            ReadOnlySpan<char> result = segment.AsSpan(start: 1, length: 2);

            Assert.Equal("ll", result.ToString());
        }

        [Fact]
        public void StringSegment_AsSpan_Empty()
        {
            var segment = new StringSegment("Hello, World!");

            ReadOnlySpan<char> result = segment.AsSpan(start: 1, length: 0);

            Assert.True(result.IsEmpty);
        }

        [Fact]
        public void StringSegment_AsSpanStart_NoValue()
        {
            StringSegment segment = default;

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => segment.AsSpan(0));
            Assert.Equal("start", exception.ParamName);
        }

        [Fact]
        public void StringSegment_AsSpan_StartBehindLength()
        {
            var segment = new StringSegment("Hello, World!", 1, 3);

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => segment.AsSpan(4));
            Assert.Equal("start", exception.ParamName);
        }

        [Fact]
        public void StringSegment_AsSpanStartLength_NoValue()
        {
            StringSegment segment = default;

            Assert.Throws<ArgumentOutOfRangeException>(() => segment.AsSpan(0, 0));
        }

        [Fact]
        public void StringSegment_AsSpan_InvalidStart()
        {
            var segment = new StringSegment("Hello, World!", 1, 3);

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => segment.AsSpan(-1, 1));
            Assert.Equal("start", exception.ParamName);
        }

        [Fact]
        public void StringSegment_AsSpan_InvalidLength()
        {
            var segment = new StringSegment("Hello, World!", 1, 3);

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => segment.AsSpan(0, -1));
            Assert.Equal("length", exception.ParamName);
        }

        [Fact]
        public void StringSegment_AsSpan_InvalidOffsetAndLength()
        {
            var segment = new StringSegment("Hello, World!", 1, 3);

            ArgumentException exception = Assert.Throws<ArgumentException>(() => segment.AsSpan(2, 3));
            Assert.Contains("bounds", exception.Message);
        }

        [Fact]
        public void StringSegment_AsSpan_OffsetAndLengthOverflows()
        {
            var segment = new StringSegment("Hello, World!", 1, 3);

            ArgumentException exception = Assert.Throws<ArgumentException>(() => segment.AsSpan(1, int.MaxValue));
            Assert.Contains("bounds", exception.Message);
        }

        [Fact]
        public void StringSegment_ImplicitConvertToSpan()
        {
            ReadOnlySpan<char> span = new StringSegment("Hello");

            Assert.Equal(5, span.Length);
        }

        [Fact]
        public void StringSegment_AsMemory()
        {
            var segment = new StringSegment("Hello");

            ReadOnlyMemory<char> memory = segment.AsMemory();

            Assert.Equal(5, memory.Length);
        }

        [Fact]
        public void StringSegment_ImplicitConvertToMemory()
        {
            ReadOnlyMemory<char> memory = new StringSegment("Hello");

            Assert.Equal(5, memory.Length);
        }

        [Fact]
        public void StringSegment_StringCtor_AllowsNullBuffers()
        {
            // Arrange & Act
            var segment = new StringSegment(null);

            // Assert
            Assert.False(segment.HasValue);
            Assert.Equal(0, segment.Offset);
            Assert.Equal(0, segment.Length);
            Assert.Null(segment.Buffer);
            Assert.Null(segment.Value);
            Assert.Throws<ArgumentOutOfRangeException>(() => segment[0]);
        }

        [Fact]
        public void StringSegmentConstructor_NullBuffer_Throws()
        {
            // Arrange, Act and Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new StringSegment(null, 0, 0));
            Assert.Contains("buffer", exception.Message);
        }

        [Fact]
        public void StringSegmentConstructor_NegativeOffset_Throws()
        {
            // Arrange, Act and Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new StringSegment("", -1, 0));
            Assert.Contains("offset", exception.Message);
        }

        [Fact]
        public void StringSegmentConstructor_NegativeLength_Throws()
        {
            // Arrange, Act and Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new StringSegment("", 0, -1));
            Assert.Contains("length", exception.Message);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(10, 0)]
        [InlineData(5, 5)]
        [InlineData(int.MaxValue, int.MaxValue)]
        public void StringSegmentConstructor_OffsetOrLengthOutOfBounds_Throws(int offset, int length)
        {
            // Arrange, Act and Assert
            Assert.Throws<ArgumentException>(() => new StringSegment("lengthof9", offset, length));
        }

        [Theory]
        [InlineData("", 0, 0)]
        [InlineData("abc", 2, 0)]
        public void StringSegmentConstructor_AllowsEmptyBuffers(string text, int offset, int length)
        {
            // Arrange & Act
            var segment = new StringSegment(text, offset, length);

            // Assert
            Assert.True(segment.HasValue);
            Assert.Equal(offset, segment.Offset);
            Assert.Equal(length, segment.Length);
        }

        [Fact]
        public void StringSegment_StringCtor_InitializesValuesCorrectly()
        {
            // Arrange
            string buffer = "Hello world!";

            // Act
            var segment = new StringSegment(buffer);

            // Assert
            Assert.True(segment.HasValue);
            Assert.Equal(0, segment.Offset);
            Assert.Equal(buffer.Length, segment.Length);
        }

        [Fact]
        public void StringSegment_Value_Valid()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            string value = segment.Value;

            // Assert
            Assert.Equal("ello", value);
        }

        [Fact]
        public void StringSegment_Value_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act
            string value = segment.Value;

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public void StringSegment_HasValue_Valid()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            bool hasValue = segment.HasValue;

            // Assert
            Assert.True(hasValue);
        }

        [Fact]
        public void StringSegment_HasValue_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act
            bool hasValue = segment.HasValue;

            // Assert
            Assert.False(hasValue);
        }

        [Theory]
        [InlineData("a", 0, 1, 0, 'a')]
        [InlineData("abc", 1, 1, 0, 'b')]
        [InlineData("abcdef", 1, 4, 0, 'b')]
        [InlineData("abcdef", 1, 4, 1, 'c')]
        [InlineData("abcdef", 1, 4, 2, 'd')]
        [InlineData("abcdef", 1, 4, 3, 'e')]
        public void StringSegment_Indexer_InRange(string value, int offset, int length, int index, char expected)
        {
            var segment = new StringSegment(value, offset, length);

            char result = segment[index];

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("", 0, 0, 0)]
        [InlineData("a", 0, 1, -1)]
        [InlineData("a", 0, 1, 1)]
        public void StringSegment_Indexer_OutOfRangeThrows(string value, int offset, int length, int index)
        {
            var segment = new StringSegment(value, offset, length);

            Assert.Throws<ArgumentOutOfRangeException>(() => segment[index]);
        }

        // candidate / comparer / expected result
        public static TheoryData<string, StringComparison, bool> EndsWithData => new()
        {
            { "Hello", StringComparison.Ordinal, false },
            { "ello ", StringComparison.Ordinal, false },
            { "ll", StringComparison.Ordinal, false },
            { "ello", StringComparison.Ordinal, true },
            { "llo", StringComparison.Ordinal, true },
            { "lo", StringComparison.Ordinal, true },
            { "o", StringComparison.Ordinal, true },
            { string.Empty, StringComparison.Ordinal, true },
            { "eLLo", StringComparison.Ordinal, false },
            { "eLLo", StringComparison.OrdinalIgnoreCase, true },
        };

        [Theory]
        [MemberData(nameof(EndsWithData))]
        public void StringSegment_EndsWith_Valid(string candidate, StringComparison comparison, bool expectedResult)
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            bool result = segment.EndsWith(candidate, comparison);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void StringSegment_EndsWith_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act
            bool result = segment.EndsWith(string.Empty, StringComparison.Ordinal);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void StringSegment_EndsWith_NullString_Throws()
        {
            // Arrange
            var segment = new StringSegment();

            // Act & assert
            Assert.Throws<ArgumentNullException>("text", () => segment.EndsWith((string)null, StringComparison.Ordinal));
        }

        [Fact]
        public void StringSegment_EndsWith_String_InvalidComparisonType_Throws()
        {
            // Arrange
            var segment = new StringSegment();

            // Act & assert
            Assert.Throws<ArgumentOutOfRangeException>("comparisonType", () => segment.EndsWith(string.Empty, (StringComparison)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>("comparisonType", () => segment.EndsWith(string.Empty, (StringComparison)6));
        }

        // candidate / comparer / expected result
        public static TheoryData<string, StringComparison, bool> StartsWithData => new()
        {
            { "Hello", StringComparison.Ordinal, false },
            { "ello ", StringComparison.Ordinal, false },
            { "ll", StringComparison.Ordinal, false },
            { "ello", StringComparison.Ordinal, true },
            { "ell", StringComparison.Ordinal, true },
            { "el", StringComparison.Ordinal, true },
            { "e", StringComparison.Ordinal, true },
            { string.Empty, StringComparison.Ordinal, true },
            { "eLLo", StringComparison.Ordinal, false },
            { "eLLo", StringComparison.OrdinalIgnoreCase, true },
        };

        [Theory]
        [MemberData(nameof(StartsWithData))]
        public void StringSegment_StartsWith_Valid(string candidate, StringComparison comparison, bool expectedResult)
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            bool result = segment.StartsWith(candidate, comparison);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void StringSegment_StartsWith_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act
            bool result = segment.StartsWith(string.Empty, StringComparison.Ordinal);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void StringSegment_StartsWith_NullString_Throws()
        {
            // Arrange
            var segment = new StringSegment();

            // Act & assert
            Assert.Throws<ArgumentNullException>("text", () => segment.StartsWith((string)null, StringComparison.Ordinal));
        }

        [Fact]
        public void StringSegment_StartsWith_String_InvalidComparisonType_Throws()
        {
            // Arrange
            var segment = new StringSegment();

            // Act & assert
            Assert.Throws<ArgumentOutOfRangeException>("comparisonType", () => segment.StartsWith(string.Empty, (StringComparison)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>("comparisonType", () => segment.StartsWith(string.Empty, (StringComparison)6));
        }

        // candidate / comparer / expected result
        public static TheoryData<string, StringComparison, bool> EqualsStringData =>new()
        {
            { "eLLo", StringComparison.OrdinalIgnoreCase, true },
            { "eLLo", StringComparison.Ordinal, false },
            { null, StringComparison.OrdinalIgnoreCase, false },
            { null, StringComparison.Ordinal, false },
        };

        [Theory]
        [MemberData(nameof(EqualsStringData))]
        public void StringSegment_Equals_String_Valid(string candidate, StringComparison comparison, bool expectedResult)
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            bool result = segment.Equals(candidate, comparison);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        // candidate / comparer / expected result
        public static TheoryData<string, StringComparison, bool> NullEqualsStringData => new()
        {
            { null, StringComparison.OrdinalIgnoreCase, true },
            { null, StringComparison.Ordinal, true },
            { "eLLo", StringComparison.OrdinalIgnoreCase, false },
            { "eLLo", StringComparison.Ordinal, false },
            { string.Empty, StringComparison.OrdinalIgnoreCase, false },
            { string.Empty, StringComparison.Ordinal, false },
        };

        [Theory]
        [MemberData(nameof(NullEqualsStringData))]
        public void StringSegment_Equals_NullString_Valid(string candidate, StringComparison comparison, bool expectedResult)
        {
            // Arrange
            var segment = new StringSegment(null);

            // Act
            bool result = segment.Equals(candidate, comparison);

            // Act & assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void StringSegment_Equals_String_InvalidComparisonType_Throws()
        {
            // Arrange
            var segment = new StringSegment();

            // Act & assert
            Assert.Throws<ArgumentOutOfRangeException>("comparisonType", () => segment.Equals("Hello!", (StringComparison)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>("comparisonType", () => segment.Equals("Hello!", (StringComparison)6));
        }

        [Fact]
        public void StringSegment_Equals_StringSegment_InvalidComparisonType_Throws()
        {
            // Arrange
            var segment = new StringSegment();

            // Act & assert
            Assert.Throws<ArgumentOutOfRangeException>("comparisonType", () => segment.Equals(new StringSegment(), (StringComparison)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>("comparisonType", () => segment.Equals(new StringSegment(), (StringComparison)6));
        }

        [Fact]
        public void StringSegment_EqualsObject_Valid()
        {
            var segment1 = new StringSegment("My Car Is Cool", 3, 3);
            var segment2 = new StringSegment("Your Carport is blue", 5, 3);

            Assert.True(segment1.Equals((object)segment2));
        }

        [Fact]
        public void StringSegment_EqualsNull_Invalid()
        {
            var segment1 = new StringSegment("My Car Is Cool", 3, 3);

            Assert.False(segment1.Equals(null as object));
        }

        [Fact]
        public void StringSegment_StaticEquals_Valid()
        {
            var segment1 = new StringSegment("My Car Is Cool", 3, 3);
            var segment2 = new StringSegment("Your Carport is blue", 5, 3);

            Assert.True(StringSegment.Equals(segment1, segment2));
        }

        [Fact]
        public void StringSegment_StaticEquals_Invalid()
        {
            var segment1 = new StringSegment("My Car Is Cool", 3, 4);
            var segment2 = new StringSegment("Your Carport is blue", 5, 4);

            Assert.False(StringSegment.Equals(segment1, segment2));
        }

        [Fact]
        public void StringSegment_IsNullOrEmpty_Valid()
        {
            Assert.True(StringSegment.IsNullOrEmpty(null));
            Assert.True(StringSegment.IsNullOrEmpty(string.Empty));
            Assert.True(StringSegment.IsNullOrEmpty(new StringSegment(null)));
            Assert.True(StringSegment.IsNullOrEmpty(new StringSegment(string.Empty)));
            Assert.True(StringSegment.IsNullOrEmpty(StringSegment.Empty));
            Assert.True(StringSegment.IsNullOrEmpty(new StringSegment(string.Empty, 0, 0)));
            Assert.True(StringSegment.IsNullOrEmpty(new StringSegment("Hello", 0, 0)));
            Assert.True(StringSegment.IsNullOrEmpty(new StringSegment("Hello", 3, 0)));
        }

        [Fact]
        public void StringSegment_IsNullOrEmpty_Invalid()
        {
            Assert.False(StringSegment.IsNullOrEmpty("A"));
            Assert.False(StringSegment.IsNullOrEmpty("ABCDefg"));
            Assert.False(StringSegment.IsNullOrEmpty(new StringSegment("A", 0, 1)));
            Assert.False(StringSegment.IsNullOrEmpty(new StringSegment("ABCDefg", 3, 2)));
        }

        public static TheoryData GetHashCode_ReturnsSameValueForEqualSubstringsData => new TheoryData<StringSegment, StringSegment>
        {
            { default(StringSegment), default(StringSegment) },
            { default(StringSegment), new StringSegment() },
            { new StringSegment("Test123", 0, 0), new StringSegment(string.Empty) },
            { new StringSegment("C`est si bon", 2, 3), new StringSegment("Yesterday", 1, 3) },
            { new StringSegment("Hello", 1, 4), new StringSegment("Hello world", 1, 4) },
            { new StringSegment("Hello"), new StringSegment("Hello", 0, 5) },
        };

        [Theory]
        [MemberData(nameof(GetHashCode_ReturnsSameValueForEqualSubstringsData))]
        public void GetHashCode_ReturnsSameValueForEqualSubstrings(StringSegment segment1, StringSegment segment2)
        {
            // Act
            int hashCode1 = segment1.GetHashCode();
            int hashCode2 = segment2.GetHashCode();

            // Assert
            Assert.Equal(hashCode1, hashCode2);
        }

        public static TheoryData GetHashCode_ReturnsDifferentValuesForInequalSubstringsData
        {
            get
            {
                string testString = "Test123";
                return new TheoryData<StringSegment, StringSegment>
                {
                    { new StringSegment(testString, 0, 1), new StringSegment(string.Empty) },
                    { new StringSegment(testString, 0, 1), new StringSegment(testString, 1, 1) },
                    { new StringSegment(testString, 1, 2), new StringSegment(testString, 1, 3) },
                    { new StringSegment(testString, 0, 4), new StringSegment("TEST123", 0, 4) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetHashCode_ReturnsDifferentValuesForInequalSubstringsData))]
        public void GetHashCode_ReturnsDifferentValuesForInequalSubstrings(
            StringSegment segment1,
            StringSegment segment2)
        {
            // Act
            int hashCode1 = segment1.GetHashCode();
            int hashCode2 = segment2.GetHashCode();

            // Assert
            Assert.NotEqual(hashCode1, hashCode2);
        }

        [Fact]
        public void StringSegment_EqualsString_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act
            bool result = segment.Equals(string.Empty, StringComparison.Ordinal);

            // Assert
            Assert.False(result);
        }

        // candidate
        public static TheoryData<StringSegment> DefaultStringSegmentEqualsStringSegmentData => new()
        {
            { default(StringSegment) },
            { new StringSegment() },
        };

        [Theory]
        [MemberData(nameof(DefaultStringSegmentEqualsStringSegmentData))]
        public void DefaultStringSegment_EqualsStringSegment(StringSegment candidate)
        {
            // Arrange
            var segment = default(StringSegment);

            // Act
            bool result = segment.Equals(candidate, StringComparison.Ordinal);

            // Assert
            Assert.True(result);
        }

        // candidate
        public static TheoryData<StringSegment> DefaultStringSegmentDoesNotEqualStringSegmentData => new()
        {
            { new StringSegment("Hello, World!", 1, 4) },
            { new StringSegment("Hello", 1, 0) },
            { new StringSegment(string.Empty) },
        };

        [Theory]
        [MemberData(nameof(DefaultStringSegmentDoesNotEqualStringSegmentData))]
        public void DefaultStringSegment_DoesNotEqualStringSegment(StringSegment candidate)
        {
            // Arrange
            var segment = default(StringSegment);

            // Act
            bool result = segment.Equals(candidate, StringComparison.Ordinal);

            // Assert
            Assert.False(result);
        }

        // candidate
        public static TheoryData<string> DefaultStringSegmentDoesNotEqualStringData => new()
        {
            { string.Empty },
            { "Hello, World!" },
        };

        [Theory]
        [MemberData(nameof(DefaultStringSegmentDoesNotEqualStringData))]
        public void DefaultStringSegment_DoesNotEqualString(string candidate)
        {
            // Arrange
            var segment = default(StringSegment);

            // Act
            bool result = segment.Equals(candidate, StringComparison.Ordinal);

            // Assert
            Assert.False(result);
        }

        // candidate / comparer / expected result
        public static TheoryData<StringSegment, StringComparison, bool> EqualsStringSegmentData => new()
        {
            { new StringSegment("Hello, World!", 1, 4), StringComparison.Ordinal, true },
            { new StringSegment("HELlo, World!", 1, 4), StringComparison.Ordinal, false },
            { new StringSegment("HELlo, World!", 1, 4), StringComparison.OrdinalIgnoreCase, true },
            { new StringSegment("ello, World!", 0, 4), StringComparison.Ordinal, true },
            { new StringSegment("ello, World!", 0, 3), StringComparison.Ordinal, false },
            { new StringSegment("ello, World!", 1, 3), StringComparison.Ordinal, false },
        };

        [Theory]
        [MemberData(nameof(EqualsStringSegmentData))]
        public void StringSegment_Equals_StringSegment_Valid(StringSegment candidate, StringComparison comparison, bool expectedResult)
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            bool result = segment.Equals(candidate, comparison);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void StringSegment_EqualsStringSegment_Invalid()
        {
            // Arrange
            var segment = new StringSegment();
            var candidate = new StringSegment("Hello, World!", 3, 2);

            // Act
            bool result = segment.Equals(candidate, StringComparison.Ordinal);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void StringSegment_SubstringOffset_Valid()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            string result = segment.Substring(offset: 1);

            // Assert
            Assert.Equal("llo", result);
        }

        [Fact]
        public void StringSegment_Substring_Valid()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            string result = segment.Substring(offset: 1, length: 2);

            // Assert
            Assert.Equal("ll", result);
        }

        [Fact]
        public void StringSegment_Substring_NoValue()
        {
            StringSegment segment = default;

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => segment.Substring(0, 0));
            Assert.Equal("offset", exception.ParamName);
        }

        [Fact]
        public void StringSegment_Substring_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.Substring(0, 0));
        }

        [Fact]
        public void StringSegment_Substring_InvalidOffset()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act & Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => segment.Substring(-1, 1));
            Assert.Equal("offset", exception.ParamName);
        }

        [Fact]
        public void StringSegment_Substring_InvalidLength()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act & Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => segment.Substring(0, -1));
            Assert.Equal("length", exception.ParamName);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50878", TestPlatforms.Android)]
        public void StringSegment_Substring_InvalidOffsetAndLength()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act & Assert
            ArgumentException exception = Assert.Throws<ArgumentException>(() => segment.Substring(2, 3));
            Assert.Contains("bounds", exception.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50878", TestPlatforms.Android)]
        public void StringSegment_Substring_OffsetAndLengthOverflows()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act & Assert
            ArgumentException exception = Assert.Throws<ArgumentException>(() => segment.Substring(1, int.MaxValue));
            Assert.Contains("bounds", exception.Message);
        }

        [Fact]
        public void StringSegment_SubsegmentOffset_Valid()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            StringSegment result = segment.Subsegment(offset: 1);

            // Assert
            Assert.Equal(new StringSegment("Hello, World!", 2, 3), result);
            Assert.Equal("llo", result.Value);
        }

        [Fact]
        public void StringSegment_Subsegment_Valid()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            StringSegment result = segment.Subsegment(offset: 1, length: 2);

            // Assert
            Assert.Equal(new StringSegment("Hello, World!", 2, 2), result);
            Assert.Equal("ll", result.Value);
        }

        [Fact]
        public void StringSegment_Subsegment_NoValue()
        {
            StringSegment segment = default;

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => segment.Subsegment(0, 0));
            Assert.Equal("offset", exception.ParamName);
        }

        [Fact]
        public void StringSegment_Subsegment_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.Subsegment(0, 0));
        }

        [Fact]
        public void StringSegment_Subsegment_InvalidOffset()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act & Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => segment.Subsegment(-1, 1));
            Assert.Equal("offset", exception.ParamName);
        }

        [Fact]
        public void StringSegment_Subsegment_InvalidLength()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act & Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => segment.Subsegment(0, -1));
            Assert.Equal("length", exception.ParamName);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50878", TestPlatforms.Android)]
        public void StringSegment_Subsegment_InvalidOffsetAndLength()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act & Assert
            ArgumentException exception = Assert.Throws<ArgumentException>(() => segment.Subsegment(2, 3));
            Assert.Contains("bounds", exception.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50878", TestPlatforms.Android)]
        public void StringSegment_Subsegment_OffsetAndLengthOverflows()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act & Assert
            ArgumentException exception = Assert.Throws<ArgumentException>(() => segment.Subsegment(1, int.MaxValue));
            Assert.Contains("bounds", exception.Message);
        }

        // candidate / comparer
        public static TheoryData<StringSegment, StringSegmentComparer> CompareLesserData => new()
        {
            { new StringSegment("abcdef", 1, 4), StringSegmentComparer.Ordinal },
            { new StringSegment("abcdef", 1, 5), StringSegmentComparer.OrdinalIgnoreCase },
            { new StringSegment("ABCDEF", 2, 2), StringSegmentComparer.OrdinalIgnoreCase },
        };

        [Theory]
        [MemberData(nameof(CompareLesserData))]
        public void StringSegment_Compare_Lesser(StringSegment candidate, StringSegmentComparer comparer)
        {
            // Arrange
            var segment = new StringSegment("ABCDEF", 1, 4);

            // Act
            int result = comparer.Compare(segment, candidate);

            // Assert
            Assert.True(result < 0, $"{segment} should be less than {candidate}");
        }

        // candidate / comparer
        public static TheoryData<StringSegment, StringSegmentComparer> CompareEqualData => new()
        {
            { new StringSegment("abcdef", 1, 4), StringSegmentComparer.Ordinal },
            { new StringSegment("ABCDEF", 1, 4), StringSegmentComparer.OrdinalIgnoreCase },
            { new StringSegment("bcde", 0, 4), StringSegmentComparer.Ordinal },
            { new StringSegment("BcDeF", 0, 4), StringSegmentComparer.OrdinalIgnoreCase },
        };

        [Theory]
        [MemberData(nameof(CompareEqualData))]
        public void StringSegment_Compare_Equal(StringSegment candidate, StringSegmentComparer comparer)
        {
            // Arrange
            var segment = new StringSegment("abcdef", 1, 4);

            // Act
            int result = comparer.Compare(segment, candidate);

            // Assert
            Assert.True(result == 0, $"{segment} should equal {candidate}");
        }

        // candidate / comparer
        public static TheoryData<StringSegment, StringSegmentComparer> CompareGreaterData => new()
        {
            { new StringSegment("ABCDEF", 1, 4), StringSegmentComparer.Ordinal },
            { new StringSegment("ABCDEF", 0, 6), StringSegmentComparer.OrdinalIgnoreCase },
            { new StringSegment("abcdef", 0, 3), StringSegmentComparer.Ordinal },
        };

        [Theory]
        [MemberData(nameof(CompareGreaterData))]
        public void StringSegment_Compare_Greater(StringSegment candidate, StringSegmentComparer comparer)
        {
            // Arrange
            var segment = new StringSegment("abcdef", 1, 4);

            // Act
            int result = comparer.Compare(segment, candidate);

            // Assert
            Assert.True(result > 0, $"{segment} should be greater than {candidate}");
        }

        [Fact]
        public void StringSegment_Compare_InvalidComparisonType_Throws()
        {
            // Arrange
            var segment = new StringSegment();

            // Act & assert
            Assert.Throws<ArgumentOutOfRangeException>("comparisonType", () => StringSegment.Compare(segment, segment, (StringComparison)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>("comparisonType", () => StringSegment.Compare(segment, segment, (StringComparison)6));
        }

        [Theory]
        [MemberData(nameof(GetHashCode_ReturnsSameValueForEqualSubstringsData))]
        public void StringSegmentComparerOrdinal_GetHashCode_ReturnsSameValueForEqualSubstrings(StringSegment segment1, StringSegment segment2)
        {
            // Arrange
            StringSegmentComparer comparer = StringSegmentComparer.Ordinal;

            // Act
            int hashCode1 = comparer.GetHashCode(segment1);
            int hashCode2 = comparer.GetHashCode(segment2);

            // Assert
            Assert.Equal(hashCode1, hashCode2);
        }

        [Theory]
        [MemberData(nameof(GetHashCode_ReturnsSameValueForEqualSubstringsData))]
        public void StringSegmentComparerOrdinalIgnoreCase_GetHashCode_ReturnsSameValueForEqualSubstrings(StringSegment segment1, StringSegment segment2)
        {
            // Arrange
            StringSegmentComparer comparer = StringSegmentComparer.OrdinalIgnoreCase;

            // Act
            int hashCode1 = comparer.GetHashCode(segment1);
            int hashCode2 = comparer.GetHashCode(segment2);

            // Assert
            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public void StringSegmentComparerOrdinalIgnoreCase_GetHashCode_ReturnsSameValueForDifferentlyCasedStrings()
        {
            // Arrange
            var segment1 = new StringSegment("abc");
            var segment2 = new StringSegment("Abcd", 0, 3);
            StringSegmentComparer comparer = StringSegmentComparer.OrdinalIgnoreCase;

            // Act
            int hashCode1 = comparer.GetHashCode(segment1);
            int hashCode2 = comparer.GetHashCode(segment2);

            // Assert
            Assert.Equal(hashCode1, hashCode2);
        }

        [Theory]
        [MemberData(nameof(GetHashCode_ReturnsDifferentValuesForInequalSubstringsData))]
        public void StringSegmentComparerOrdinal_GetHashCode_ReturnsDifferentValuesForInequalSubstrings(StringSegment segment1, StringSegment segment2)
        {
            // Arrange
            StringSegmentComparer comparer = StringSegmentComparer.Ordinal;

            // Act
            int hashCode1 = comparer.GetHashCode(segment1);
            int hashCode2 = comparer.GetHashCode(segment2);

            // Assert
            Assert.NotEqual(hashCode1, hashCode2);
        }

        [Fact]
        public void IndexOf_ComputesIndex_RelativeToTheCurrentSegment()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 10);

            // Act
            int result = segment.IndexOf(',');

            // Assert
            Assert.Equal(4, result);
        }

        [Fact]
        public void IndexOf_ReturnsMinusOne_IfElementNotInSegment()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act
            int result = segment.IndexOf(',');

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void IndexOf_ReturnsMinusOne_OnDefaultStringSegment()
        {
            // Arrange
            StringSegment segment = default;

            // Act
            int result = segment.IndexOf(',');

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void IndexOf_SkipsANumberOfCaracters_IfStartIsProvided()
        {
            // Arrange
            const string buffer = "Hello, World!, Hello people!";
            var segment = new StringSegment(buffer, 3, buffer.Length - 3);

            // Act
            int result = segment.IndexOf('!', 15);

            // Assert
            Assert.Equal(buffer.Length - 4, result);
        }

        [Fact]
        public void IndexOf_SearchOnlyInsideTheRange_IfStartAndCountAreProvided()
        {
            // Arrange
            const string buffer = "Hello, World!, Hello people!";
            var segment = new StringSegment(buffer, 3, buffer.Length - 3);

            // Act
            int result = segment.IndexOf('!', 15, 5);

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void IndexOf_NegativeStart_OutOfRangeThrows()
        {
            // Arrange
            const string buffer = "Hello, World!, Hello people!";
            var segment = new StringSegment(buffer, 3, buffer.Length - 3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.IndexOf('!', -1, 3));
        }

        [Fact]
        public void IndexOf_StartOverflowsWithOffset_OutOfRangeThrows()
        {
            // Arrange
            const string buffer = "Hello, World!, Hello people!";
            var segment = new StringSegment(buffer, 3, buffer.Length - 3);

            // Act & Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => segment.IndexOf('!', int.MaxValue, 3));
            Assert.Equal("start", exception.ParamName);
        }

        [Fact]
        public void IndexOfAny_ComputesIndex_RelativeToTheCurrentSegment()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 10);

            // Act
            int result = segment.IndexOfAny(new[] { ',' });

            // Assert
            Assert.Equal(4, result);
        }

        [Fact]
        public void IndexOfAny_ReturnsMinusOne_IfElementNotInSegment()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act
            int result = segment.IndexOfAny(new[] { ',' });

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void IndexOfAny_ReturnsMinusOne_OnDefaultStringSegment()
        {
            // Arrange
            StringSegment segment = default;

            // Act
            int result = segment.IndexOfAny(new[] { ',' });

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void IndexOfAny_SkipsANumberOfCaracters_IfStartIsProvided()
        {
            // Arrange
            const string buffer = "Hello, World!, Hello people!";
            var segment = new StringSegment(buffer, 3, buffer.Length - 3);

            // Act
            int result = segment.IndexOfAny(new[] { '!' }, 15);

            // Assert
            Assert.Equal(buffer.Length - 4, result);
        }

        [Fact]
        public void IndexOfAny_SearchOnlyInsideTheRange_IfStartAndCountAreProvided()
        {
            // Arrange
            const string buffer = "Hello, World!, Hello people!";
            var segment = new StringSegment(buffer, 3, buffer.Length - 3);

            // Act
            int result = segment.IndexOfAny(new[] { '!' }, 15, 5);

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void IndexOfAny_StartOverflowsWithOffset_OutOfRangeThrows()
        {
            // Arrange
            StringSegment segment = new StringSegment("12345", 0, 1);

            // Act & Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => segment.IndexOfAny(new []{ '5' }, 2, 3));
            Assert.Equal("start", exception.ParamName);
        }

        [Fact]
        public void LastIndexOf_ComputesIndex_RelativeToTheCurrentSegment()
        {
            // Arrange
            var segment = new StringSegment("Hello, World, how, are, you!", 1, 14);

            // Act
            int result = segment.LastIndexOf(',');

            // Assert
            Assert.Equal(11, result);
        }

        [Fact]
        public void LastIndexOf_ReturnsMinusOne_IfElementNotInSegment()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act
            int result = segment.LastIndexOf(',');

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void LastIndexOf_ReturnsMinusOne_OnDefaultStringSegment()
        {
            // Arrange
            StringSegment segment = default;

            // Act
            int result = segment.LastIndexOf(',');

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void Value_DoesNotAllocateANewString_IfTheSegmentContainsTheWholeBuffer()
        {
            // Arrange
            const string buffer = "Hello, World!";
            var segment = new StringSegment(buffer);

            // Act
            string result = segment.Value;

            // Assert
            Assert.Same(buffer, result);
        }

        [Fact]
        public void StringSegment_CreateEmptySegment()
        {
            // Arrange
            var segment = new StringSegment("//", 1, 0);

            // Assert
            Assert.True(segment.HasValue);
        }

        [Theory]
        [InlineData("   value", 0, 8, "value")]
        [InlineData("value   ", 0, 8, "value")]
        [InlineData("\t\tvalue", 0, 7, "value")]
        [InlineData("value\t\t", 0, 7, "value")]
        [InlineData("\t\tvalue \t a", 1, 8, "value")]
        [InlineData("   a     ", 0, 9, "a")]
        [InlineData("value\t value  value ", 2, 13, "lue\t value  v")]
        [InlineData("\x0009value \x0085", 0, 8, "value")]
        [InlineData(" \f\t\u000B\u2028Hello \u2029\n\t ", 1, 13, "Hello")]
        [InlineData("      ", 1, 2, "")]
        [InlineData("\t\t\t", 0, 3, "")]
        [InlineData("\n\n\t\t  \t", 2, 3, "")]
        [InlineData("      ", 1, 0, "")]
        [InlineData("", 0, 0, "")]
        public void Trim_RemovesLeadingAndTrailingWhitespaces(string value, int start, int length, string expected)
        {
            // Arrange
            var segment = new StringSegment(value, start, length);

            // Act
            StringSegment actual = segment.Trim();

            // Assert
            Assert.Equal(expected, actual.Value);
        }

        [Theory]
        [InlineData("   value", 0, 8, "value")]
        [InlineData("value   ", 0, 8, "value   ")]
        [InlineData("\t\tvalue", 0, 7, "value")]
        [InlineData("value\t\t", 0, 7, "value\t\t")]
        [InlineData("\t\tvalue \t a", 1, 8, "value \t")]
        [InlineData("   a     ", 0, 9, "a     ")]
        [InlineData("value\t value  value ", 2, 13, "lue\t value  v")]
        [InlineData("\x0009value \x0085", 0, 8, "value \x0085")]
        [InlineData(" \f\t\u000B\u2028Hello \u2029\n\t ", 1, 13, "Hello \u2029\n\t")]
        [InlineData("      ", 1, 2, "")]
        [InlineData("\t\t\t", 0, 3, "")]
        [InlineData("\n\n\t\t  \t", 2, 3, "")]
        [InlineData("      ", 1, 0, "")]
        [InlineData("", 0, 0, "")]
        public void TrimStart_RemovesLeadingWhitespaces(string value, int start, int length, string expected)
        {
            // Arrange
            var segment = new StringSegment(value, start, length);

            // Act
            StringSegment actual = segment.TrimStart();

            // Assert
            Assert.Equal(expected, actual.Value);
        }

        [Theory]
        [InlineData("   value", 0, 8, "   value")]
        [InlineData("value   ", 0, 8, "value")]
        [InlineData("\t\tvalue", 0, 7, "\t\tvalue")]
        [InlineData("value\t\t", 0, 7, "value")]
        [InlineData("\t\tvalue \t a", 1, 8, "\tvalue")]
        [InlineData("   a     ", 0, 9, "   a")]
        [InlineData("value\t value  value ", 2, 13, "lue\t value  v")]
        [InlineData("\x0009value \x0085", 0, 8, "\x0009value")]
        [InlineData(" \f\t\u000B\u2028Hello \u2029\n\t ", 1, 13, "\f\t\u000B\u2028Hello")]
        [InlineData("      ", 1, 2, "")]
        [InlineData("\t\t\t", 0, 3, "")]
        [InlineData("\n\n\t\t  \t", 2, 3, "")]
        [InlineData("      ", 1, 0, "")]
        [InlineData("", 0, 0, "")]
        public void TrimEnd_RemovesTrailingWhitespaces(string value, int start, int length, string expected)
        {
            // Arrange
            var segment = new StringSegment(value, start, length);

            // Act
            StringSegment actual = segment.TrimEnd();

            // Assert
            Assert.Equal(expected, actual.Value);
        }

        public static TheoryData<string, string, StringComparison, int> GlobalizationCompareTestData => new()
        {
            { null, string.Empty, StringComparison.Ordinal, -1 }, // null always compares before non-null
            { null, string.Empty, StringComparison.InvariantCultureIgnoreCase, -1 }, // null always compares before non-null
            { null, null, StringComparison.Ordinal, 0 },
            { null, null, StringComparison.InvariantCultureIgnoreCase, 0 },
            { string.Empty, null, StringComparison.Ordinal, 1 },
            { string.Empty, null, StringComparison.InvariantCultureIgnoreCase, 1 },
            { "x\u00E9y", "xE\u0301y", StringComparison.InvariantCulture,
            PlatformDetection.IsInvariantGlobalization ? 1 : -1 }, // linguistic: lowercase sorts before uppercase
            { "x\u00E9y", "xE\u0301y", StringComparison.InvariantCultureIgnoreCase,
            PlatformDetection.IsInvariantGlobalization ? 1 : 0 }, // equal (linguistic, one is normalized)
            { "Hello", "HELLO", StringComparison.InvariantCulture,
            PlatformDetection.IsInvariantGlobalization ? 1 : -1 }, // linguistic: lowercase sorts before uppercase
            { "Hello", "HELLO", StringComparison.InvariantCultureIgnoreCase, 0 },
        };

        [Theory]
        [MemberData(nameof(GlobalizationCompareTestData))]
        public void StringSegment_CompareEqual_Globalized(string a, string b, StringComparison comparisonType, int expectedCompareToSign)
        {
            // quick sanity check: run the parameters against the normal string functions to ensure our test data is valid
            int returnedSign = string.Compare(a, b, comparisonType);
            Assert.Equal(expectedCompareToSign, Math.Sign(returnedSign));

            StringSegment sa = MakePaddedStringSegment(a);
            StringSegment sb = MakePaddedStringSegment(b);

            // StringSegment.Compare
            {
                returnedSign = StringSegment.Compare(sa, sb, comparisonType);
                Assert.Equal(expectedCompareToSign, Math.Sign(returnedSign));
            }

            // StringSegment.Equals(StringSegment, ...) and op_Equality
            {
                bool areEqual = StringSegment.Equals(sa, sb, comparisonType);
                Assert.Equal(expectedCompareToSign == 0, areEqual);

                areEqual = sa.Equals(sb, comparisonType);
                Assert.Equal(expectedCompareToSign == 0, areEqual);

                if (comparisonType == StringComparison.Ordinal)
                {
                    areEqual = sa.Equals(sb);
                    Assert.Equal(expectedCompareToSign == 0, areEqual);

                    areEqual = sa.Equals((object)sb);
                    Assert.Equal(expectedCompareToSign == 0, areEqual);

                    areEqual = (sa == sb);
                    Assert.Equal(expectedCompareToSign == 0, areEqual);

                    areEqual = !(sa != sb);
                    Assert.Equal(expectedCompareToSign == 0, areEqual);
                }
            }

            // StringSegment.Equals(string, ...) and IEquatable<string>.Equals
            {
                bool areEqual = sa.Equals(b, comparisonType);
                Assert.Equal(expectedCompareToSign == 0, areEqual);

                if (comparisonType == StringComparison.Ordinal)
                {
                    areEqual = sa.Equals(b);
                    Assert.Equal(expectedCompareToSign == 0, areEqual);

                    areEqual = ((IEquatable<string>)sa).Equals(b);
                    Assert.Equal(expectedCompareToSign == 0, areEqual);
                }
            }
        }

        public static TheoryData<string, string, StringComparison, bool> GlobalizationStartsWithData => new()
        {
            { null, "\u200d", StringComparison.Ordinal, false }, // null never starts with anything
            { null, "\u200d", StringComparison.InvariantCulture, false }, // null never starts with anything
            { null, string.Empty, StringComparison.Ordinal, false }, // null never starts with anything
            { string.Empty, string.Empty, StringComparison.Ordinal, true }, // not char-for-char equivalent
            { string.Empty, "\u200d", StringComparison.Ordinal, false }, // not char-for-char equivalent
            { string.Empty, "\u200d", StringComparison.InvariantCulture,
            PlatformDetection.IsInvariantGlobalization ? false : true }, // linguistic: ZWJ is zero-weight, occurs at all indices
            { "\u200d", string.Empty, StringComparison.Ordinal, true }, // all strings trivially start with the empty string
            { "\u200d", "\u200d\u200d", StringComparison.InvariantCulture,
            PlatformDetection.IsInvariantGlobalization ? false : true }, // linguistic: ZWJ is zero-weight
            { "Hello", "h", StringComparison.Ordinal, false },
            { "Hello", "h", StringComparison.OrdinalIgnoreCase, true },
            { "Hello", "hi", StringComparison.Ordinal, false },
            { "Hello", "hi", StringComparison.OrdinalIgnoreCase, false },
        };

        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "netfx has some IsPrefix / IsSuffix globalization bugs.")]
        [MemberData(nameof(GlobalizationStartsWithData))]
        public void StringSegment_StartsWith_Globalized(string a, string b, StringComparison comparisonType, bool expectedResult)
        {
            // quick sanity check: run the parameters against the normal string functions to ensure our test data is valid
            if (a != null)
            {
                Assert.Equal(expectedResult, a.StartsWith(b, comparisonType));
            }

            // Arrange
            StringSegment sa = MakePaddedStringSegment(a);

            // Act
            bool actualResult = sa.StartsWith(b, comparisonType);

            // Assert
            Assert.Equal(expectedResult, actualResult);
        }

        public static TheoryData<string, string, StringComparison, bool> GlobalizationEndsWithData => new()
        {
            { null, "\u200d", StringComparison.Ordinal, false }, // null never ends with anything
            { null, "\u200d", StringComparison.InvariantCulture, false }, // null never ends with anything
            { null, string.Empty, StringComparison.Ordinal, false }, // null never ends with anything
            { string.Empty, string.Empty, StringComparison.Ordinal, true }, // not char-for-char equivalent
            { string.Empty, "\u200d", StringComparison.Ordinal, false }, // not char-for-char equivalent
            { string.Empty, "\u200d", StringComparison.InvariantCulture,
            PlatformDetection.IsInvariantGlobalization ? false : true }, // linguistic: ZWJ is zero-weight, occurs at all indices
            { "\u200d", string.Empty, StringComparison.Ordinal, true }, // all strings trivially ends with the empty string
            { "\u200d", "\u200d\u200d", StringComparison.InvariantCulture,
            PlatformDetection.IsInvariantGlobalization ? false : true }, // linguistic: ZWJ is zero-weight
            { "HELLO", "o", StringComparison.Ordinal, false },
            { "HELLO", "o", StringComparison.OrdinalIgnoreCase, true },
            { "HELLO", "illo", StringComparison.Ordinal, false },
            { "HELLO", "illo", StringComparison.OrdinalIgnoreCase, false },
        };

        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "netfx has some IsPrefix / IsSuffix globalization bugs.")]
        [MemberData(nameof(GlobalizationEndsWithData))]
        public void StringSegment_EndsWith_Globalized(string a, string b, StringComparison comparisonType, bool expectedResult)
        {
            // quick sanity check: run the parameters against the normal string functions to ensure our test data is valid
            if (a != null)
            {
                Assert.Equal(expectedResult, a.EndsWith(b, comparisonType));
            }

            // Arrange
            StringSegment sa = MakePaddedStringSegment(a);

            // Act
            bool actualResult = sa.EndsWith(b, comparisonType);

            // Assert
            Assert.Equal(expectedResult, actualResult);
        }

        private static StringSegment MakePaddedStringSegment(string input)
        {
            return (input is null) ? new StringSegment() : new StringSegment("xx" + input + "zzz", 2, input.Length);
        }
    }
}
