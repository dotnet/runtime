// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class TypeNameParserHelpersTests
    {
        public static IEnumerable<object[]> GetGenericArgumentCountReturnsExpectedValue_Args()
        {
            int maxArrayLength =
#if NETCOREAPP
                Array.MaxLength;
#else
                2147483591;
#endif

            yield return new object[] { $"TooLargeForInt`{long.MaxValue}", -1 };
            yield return new object[] { $"TooLargeForInt`{(long)int.MaxValue + 1}", -1 };
            yield return new object[] { $"TooLargeForInt`{(long)uint.MaxValue + 1}", -1 };
            yield return new object[] { $"MaxArrayLength`{maxArrayLength}", maxArrayLength };
            yield return new object[] { $"TooLargeForAnArray`{maxArrayLength + 1}", -1 };
        }

        [Theory]
        [InlineData("", 0)] // empty input
        [InlineData("1", 0)] // short, valid
        [InlineData("`1", -1)] // short, back tick as first char
        [InlineData("`111", -1)] // long, back tick as first char
        [InlineData("\\`111", 0)] // long enough, escaped back tick as first char
        [InlineData("NoBackTick2", 0)] // no backtick, single digit
        [InlineData("NoBackTick123", 0)] // no backtick, few digits
        [InlineData("a`1", 1)] // valid, single digit
        [InlineData("a`666", 666)] // valid, few digits
        [InlineData("DigitBeforeBackTick1`7", 7)] // valid, single digit
        [InlineData("DigitBeforeBackTick123`321", 321)] // valid, few digits
        [InlineData("EscapedBacktick\\`1", 0)] // escaped backtick, single digit
        [InlineData("EscapedBacktick\\`123", 0)] // escaped backtick, few digits
        [InlineData("NegativeValue`-1", -1)] // negative value, single digit
        [InlineData("NegativeValue`-222", -222)] // negative value, few digits
        [InlineData("EscapedBacktickNegativeValue\\`-1", 0)] // negative value, single digit
        [InlineData("EscapedBacktickNegativeValue\\`-222", 0)] // negative value, few digits
        [MemberData(nameof(GetGenericArgumentCountReturnsExpectedValue_Args))]
        public void GetGenericArgumentCountReturnsExpectedValue(string input, int expected)
            => Assert.Equal(expected, TypeNameParserHelpers.GetGenericArgumentCount(input.AsSpan()));


        [Theory]
        [InlineData("A[]", 1, false)]
        [InlineData("AB[a,b]", 2, false)]
        [InlineData("AB[[a, b],[c,d]]", 2, false)]
        [InlineData("12]]", 2, false)]
        [InlineData("ABC&", 3, false)]
        [InlineData("ABCD*", 4, false)]
        [InlineData("ABCDE,otherType]]", 5, false)]
        [InlineData("Containing+Nested", 10, true)]
        [InlineData("NoSpecial.Characters", 20, false)]
        [InlineData("Requires\\+Escaping", 18, false)]
        [InlineData("Requires\\[Escaping+Nested", 18, true)]
        [InlineData("Worst\\[\\]\\&\\*\\,\\+Case", 21, false)]
        [InlineData("EscapingSthThatShouldNotBeEscaped\\A", -1 , false)]
        [InlineData("EndsWithEscaping\\", -1, false)]
        public void GetFullTypeNameLengthReturnsExpectedValue(string input, int expected, bool expectedIsNested)
        {
            Assert.Equal(expected, TypeNameParserHelpers.GetFullTypeNameLength(input.AsSpan(), out bool isNested));
            Assert.Equal(expectedIsNested, isNested);

            string withNamespace = $"Namespace1.Namespace2.Namespace3.{input}";
            int expectedWithNamespace = expected < 0 ? expected : expected + withNamespace.Length - input.Length;
            Assert.Equal(expectedWithNamespace, TypeNameParserHelpers.GetFullTypeNameLength(withNamespace.AsSpan(), out isNested));
            Assert.Equal(expectedIsNested, isNested);
        }

        public static IEnumerable<object[]> InvalidNamesArguments()
        {
            yield return new object[] { "", 0 };
            yield return new object[] { "\0NullCharacterIsNotAllowed", 0 };
            yield return new object[] { "Null\0CharacterIsNotAllowed", 4 };
            yield return new object[] { "NullCharacterIsNotAllowed\0", 25 };
            yield return new object[] { "\bBackspaceIsNotAllowed", 0 };
            yield return new object[] { "EscapingIsNotAllowed\\", 20 };
            yield return new object[] { "EscapingIsNotAllowed\\\\", 20 };
            yield return new object[] { "EscapingIsNotAllowed\\*", 20 };
            yield return new object[] { "EscapingIsNotAllowed\\&", 20 };
            yield return new object[] { "EscapingIsNotAllowed\\+", 20 };
            yield return new object[] { "EscapingIsNotAllowed\\[", 20 };
            yield return new object[] { "EscapingIsNotAllowed\\]", 20 };
            yield return new object[] { "Slash/IsNotAllowed", 5 };
            yield return new object[] { "WhitespacesAre\tNotAllowed", 14 };
            yield return new object[] { "WhitespacesAreNot\r\nAllowed", 17 };
            yield return new object[] { "Question?MarkIsNotAllowed", 8 };
            yield return new object[] { "Quotes\"AreNotAllowed", 6 };
            yield return new object[] { "Quote'IsNotAllowed", 5 };
            yield return new object[] { "abcdefghijklmnopqrstuvwxyz", -1 };
            yield return new object[] { "ABCDEFGHIJKLMNOPQRSTUVWXYZ", -1 };
            yield return new object[] { "0123456789", -1 };
            yield return new object[] { "BacktickIsOk`1", -1 };
        }


        [Theory]
        [MemberData(nameof(InvalidNamesArguments))]
        [InlineData("Spaces AreAllowed", -1)]
        [InlineData("!@#$%^()-_{}|<>.~&;", -1)]
        public void GetIndexOfFirstInvalidAssemblyNameCharacter_ReturnsFirstInvalidCharacter(string input, int expected)
        {
            Assert.Equal(expected, TypeNameParserHelpers.GetIndexOfFirstInvalidAssemblyNameCharacter(input.AsSpan(), strictMode: true));

            TypeNameParserOptions strictOptions = new()
            {
                StrictValidation = true
            };

            string assemblyQualifiedName = $"Namespace.CorrectTypeName, {input}";

            if (expected >= 0)
            {
                Assert.False(TypeName.TryParse(assemblyQualifiedName.AsSpan(), out _, strictOptions));
                Assert.Throws<ArgumentException>(() => TypeName.Parse(assemblyQualifiedName.AsSpan(), strictOptions));
            }
            else
            {
                Assert.True(TypeName.TryParse(assemblyQualifiedName.AsSpan(), out TypeName parsed, strictOptions));
                Assert.Equal(assemblyQualifiedName, parsed.AssemblyQualifiedName);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidNamesArguments))]
        [InlineData("Spaces AreNotAllowed", 6)]
        [InlineData("!@#$%^()-_={}|<>.~", -1)]
        public void GetIndexOfFirstInvalidTypeNameCharacter_ReturnsFirstInvalidCharacter(string input, int expected)
        {
            Assert.Equal(expected, TypeNameParserHelpers.GetIndexOfFirstInvalidTypeNameCharacter(input.AsSpan(), strictMode: true));

            TypeNameParserOptions strictOptions = new()
            {
                StrictValidation = true
            };

            if (expected >= 0)
            {
                Assert.False(TypeName.TryParse(input.AsSpan(), out _, strictOptions));
                Assert.Throws<ArgumentException>(() => TypeName.Parse(input.AsSpan(), strictOptions));
            }
            else
            {
                Assert.True(TypeName.TryParse(input.AsSpan(), out TypeName parsed, strictOptions));
                Assert.Equal(input, parsed.FullName);
            }
        }

        [Theory]
        [InlineData("JustTypeName", "JustTypeName")]
        [InlineData("Namespace.TypeName", "TypeName")]
        [InlineData("Namespace1.Namespace2.TypeName", "TypeName")]
        [InlineData("Namespace.NotNamespace\\.TypeName", "NotNamespace\\.TypeName")]
        [InlineData("Namespace1.Namespace2.Containing+Nested", "Nested")]
        [InlineData("Namespace1.Namespace2.Not\\+Nested", "Not\\+Nested")]
        [InlineData("NotNamespace1\\.NotNamespace2\\.TypeName", "NotNamespace1\\.NotNamespace2\\.TypeName")]
        [InlineData("NotNamespace1\\.NotNamespace2\\.Not\\+Nested", "NotNamespace1\\.NotNamespace2\\.Not\\+Nested")]
        public void GetNameReturnsJustName(string fullName, string expected)
            => Assert.Equal(expected, TypeNameParserHelpers.GetName(fullName.AsSpan()).ToString());

        [Theory]
        [InlineData(TypeNameParserHelpers.SZArray, "[]")]
        [InlineData(TypeNameParserHelpers.Pointer, "*")]
        [InlineData(TypeNameParserHelpers.ByRef, "&")]
        [InlineData(1, "[*]")]
        [InlineData(2, "[,]")]
        [InlineData(3, "[,,]")]
        [InlineData(4, "[,,,]")]
        public void GetRankOrModifierStringRepresentationReturnsExpectedString(int input, string expected)
            => Assert.Equal(expected, TypeNameParserHelpers.GetRankOrModifierStringRepresentation(input));

        [Theory]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(int?))]
        [InlineData(typeof(List<string>))]
        [InlineData(typeof(Dictionary<string, DateTime>))]
        [InlineData(typeof(ValueTuple<bool, short, int, DateTime>))]
        [InlineData(typeof(ValueTuple<bool, short, int, DateTime, char, ushort, long, sbyte>))]
        public void GetGenericTypeFullNameReturnsSameStringAsTypeAPI(Type genericType)
        {
            TypeName openGenericTypeName = TypeName.Parse(genericType.GetGenericTypeDefinition().FullName.AsSpan());
            TypeName[] genericArgNames = genericType.GetGenericArguments().Select(arg => TypeName.Parse(arg.AssemblyQualifiedName.AsSpan())).ToArray();

            Assert.Equal(genericType.FullName, TypeNameParserHelpers.GetGenericTypeFullName(openGenericTypeName.FullName.AsSpan(), genericArgNames));
        }

        [Theory]
        [InlineData("", false, false, "")]
        [InlineData("[", false, false, "[")] // too little to be able to tell
        [InlineData("[[", true, true, "")]
        [InlineData("[[A],[B]]", true, true, "A],[B]]")]
        [InlineData("[ [    A],[B]]", true, true, "A],[B]]")]
        [InlineData("[\t[\t \r\nA],[B]]", true, true, "A],[B]]")] // whitespaces other than ' '
        [InlineData("[A,B]", true, false, "A,B]")]
        [InlineData("[  A,B]", true, false, "A,B]")]
        [InlineData("[]", false, false, "[]")]
        [InlineData("[*]", false, false, "[*]")]
        [InlineData("[,]", false, false, "[,]")]
        [InlineData("[,,]", false, false, "[,,]")]
        public void IsBeginningOfGenericAgsHandlesAllCasesProperly(string input, bool expectedResult, bool expectedDoubleBrackets, string expectedConsumedInput)
        {
            ReadOnlySpan<char> inputSpan = input.AsSpan();

            Assert.Equal(expectedResult, TypeNameParserHelpers.IsBeginningOfGenericAgs(ref inputSpan, out bool doubleBrackets));
            Assert.Equal(expectedDoubleBrackets, doubleBrackets);
            Assert.Equal(expectedConsumedInput, inputSpan.ToString());
        }

        [Theory]
        [InlineData(" \t\r\nA.B.C", "A.B.C")]
        [InlineData(" A.B.C\t\r\n", "A.B.C\t\r\n")] // don't trim the end
        public void TrimStartTrimsAllWhitespaces(string input, string expectedResult)
        {
            ReadOnlySpan<char> inputSpan = input.AsSpan();

            Assert.Equal(expectedResult, TypeNameParserHelpers.TrimStart(inputSpan).ToString());
        }

        [Theory]
        [InlineData("A.B.C", true, null, 5, 0)]
        [InlineData("A.B.C\\", false, null, 0, 0)] // invalid type name: ends with escape character
        [InlineData("A.B.C\\DoeNotNeedEscaping", false, null, 0, 0)] // invalid type name: escapes non-special character
        [InlineData("A.B+C", true, new int[] { 3 }, 5, 0)]
        [InlineData("A.B++C", false, null, 0, 0)] // invalid type name: two following, unescaped +
        [InlineData("A.B`1", true, null, 5, 1)]
        [InlineData("A+B`1+C1`2+DD2`3+E", true, new int[] { 1, 3, 4, 5 }, 18, 6)]
        public void TryGetTypeNameInfoGetsAllTheInfo(string input, bool expectedResult, int[] expectedNestedNameLengths,
            int expectedTotalLength, int expectedGenericArgCount)
        {
            List<int>? nestedNameLengths = null;
            bool result = TypeNameParserHelpers.TryGetTypeNameInfo(input.AsSpan(), ref nestedNameLengths, out int totalLength, out int genericArgCount);

            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedNestedNameLengths, nestedNameLengths?.ToArray());
            Assert.Equal(expectedTotalLength, totalLength);
            Assert.Equal(expectedGenericArgCount, genericArgCount);
        }

        [Theory]
        [InlineData("*", true, TypeNameParserHelpers.Pointer, "")]
        [InlineData(" *", false, default(int), " *")] // Whitespace cannot precede the decorator
        [InlineData("*    *", true, TypeNameParserHelpers.Pointer, "*")] // but it can follow the decorator.
        [InlineData("&", true, TypeNameParserHelpers.ByRef, "")]
        [InlineData("\t&", false, default(int), "\t&")]
        [InlineData("&\t\r\n[]", true, TypeNameParserHelpers.ByRef, "[]")]
        [InlineData("[]", true, TypeNameParserHelpers.SZArray, "")]
        [InlineData("\r\n[]", false, default(int), "\r\n[]")]
        [InlineData("[]   []", true, TypeNameParserHelpers.SZArray, "[]")]
        [InlineData("[,]", true, 2, "")]
        [InlineData(" [,,,]", false, default(int), " [,,,]")]
        [InlineData("[,,,,]   *[]", true, 5, "*[]")]
        public void TryParseNextDecoratorParsesTheDecoratorAndConsumesFollowingWhitespaces(
            string input, bool expectedResult, int expectedModifier, string expectedConsumedInput)
        {
            ReadOnlySpan<char> inputSpan = input.AsSpan();

            Assert.Equal(expectedResult, TypeNameParserHelpers.TryParseNextDecorator(ref inputSpan, out int parsedModifier));
            Assert.Equal(expectedModifier, parsedModifier);
            Assert.Equal(expectedConsumedInput, inputSpan.ToString());
        }

        [Theory]
        [InlineData(" , ", ',', false, " , ")] // it can not start with a whitespace
        [InlineData("AB", ',', false, "AB")] // does not start with given character
        [InlineData(",       ", ',', true, "")] // trimming
        [InlineData(",[AB]", ',', true, "[AB]")] // nothing to trim
        public void TryStripFirstCharAndTrailingSpacesWorksAsExpected(
            string input, char argument, bool expectedResult, string expectedConsumedInput)
        {
            ReadOnlySpan<char> inputSpan = input.AsSpan();

            Assert.Equal(expectedResult, TypeNameParserHelpers.TryStripFirstCharAndTrailingSpaces(ref inputSpan, argument));
            Assert.Equal(expectedConsumedInput, inputSpan.ToString());
        }
    }
}
