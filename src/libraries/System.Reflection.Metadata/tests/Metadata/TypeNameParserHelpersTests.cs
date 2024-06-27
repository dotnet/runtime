// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class TypeNameParserHelpersTests
    {
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
        [InlineData("simple", "simple")]
        [InlineData("simple]", "simple")]
        [InlineData("esc\\]aped", "esc\\]aped")]
        [InlineData("esc\\]aped]", "esc\\]aped")]
        public void GetAssemblyNameCandidateReturnsExpectedValue(string input, string expected)
            => Assert.Equal(expected, TypeNameParserHelpers.GetAssemblyNameCandidate(input.AsSpan()).ToString());

        [Theory]
        [InlineData(TypeNameParserHelpers.SZArray, "[]")]
        [InlineData(TypeNameParserHelpers.Pointer, "*")]
        [InlineData(TypeNameParserHelpers.ByRef, "&")]
        [InlineData(1, "[*]")]
        [InlineData(2, "[,]")]
        [InlineData(3, "[,,]")]
        [InlineData(4, "[,,,]")]
        public void AppendRankOrModifierStringRepresentationAppendsExpectedString(int input, string expected)
        {
            ValueStringBuilder builder = new ValueStringBuilder(initialCapacity: 10);
            Assert.Equal(expected, TypeNameParserHelpers.GetRankOrModifierStringRepresentation(input, ref builder));
        }

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
            ReadOnlySpan<TypeName> genericArgNames = genericType.GetGenericArguments().Select(arg => TypeName.Parse(arg.AssemblyQualifiedName.AsSpan())).ToArray();

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

            Assert.Equal(expectedResult, TypeNameParserHelpers.IsBeginningOfGenericArgs(ref inputSpan, out bool doubleBrackets));
            Assert.Equal(expectedDoubleBrackets, doubleBrackets);
            Assert.Equal(expectedConsumedInput, inputSpan.ToString());
        }

        [Theory]
        [InlineData("A.B.C", true, null, 5)]
        [InlineData("A.B.C\\", false, null, 0)] // invalid type name: ends with escape character
        [InlineData("A.B.C\\DoeNotNeedEscaping", false, null, 0)] // invalid type name: escapes non-special character
        [InlineData("A.B+C", true, new int[] { 3 }, 5)]
        [InlineData("A.B++C", false, null, 0)] // invalid type name: two following, unescaped +
        [InlineData("A.B`1", true, null, 5)]
        [InlineData("A+B`1+C1`2+DD2`3+E", true, new int[] { 1, 3, 4, 5 }, 18)]
        [InlineData("Integer`2147483646+NoOverflow`1", true, new int[] { 18 }, 31)]
        [InlineData("Integer`2147483647+Overflow`1", true, new int[] { 18 }, 29)]
        public void TryGetTypeNameInfoGetsAllTheInfo(string input, bool expectedResult, int[] expectedNestedNameLengths, int expectedTotalLength)
        {
            List<int>? nestedNameLengths = null;
            ReadOnlySpan<char> span = input.AsSpan();
            bool result = TypeNameParserHelpers.TryGetTypeNameInfo(ref span, ref nestedNameLengths, out int totalLength);

            Assert.Equal(expectedResult, result);

            if (expectedResult)
            {
                Assert.Equal(expectedNestedNameLengths, nestedNameLengths?.ToArray());
                Assert.Equal(expectedTotalLength, totalLength);
            }
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
