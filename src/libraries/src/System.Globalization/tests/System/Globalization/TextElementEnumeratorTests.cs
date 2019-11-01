// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextElementEnumeratorTests
    {
        public static IEnumerable<object[]> Enumerate_TestData()
        {
            yield return new object[] { "", new string[] { "" }, new int[0] };
            yield return new object[] { "Hello", new string[] { "H", "e", "l", "l", "o" }, new int[] { 0, 1, 2, 3, 4 } };

            // Creates and initializes a string containing the following:
            //   - a surrogate pair (high surrogate U+D800 and low surrogate U+DC00)
            //   - a combining character sequence (the Latin small letter "a" followed by the combining grave accent)
            //   - a base character (the ligature "")
            yield return new object[] { "\uD800\uDC00\u0061\u0300\u00C6", new string[] { "\uD800\uDC00", "\uD800\uDC00", "\u0061\u0300", "\u0061\u0300", "\u00C6" }, new int[] { 0, 2, 4 } };
        }

        [Theory]
        [MemberData(nameof(Enumerate_TestData))]
        public void Enumerate(string str, string[] expectedElements, int[] expectedElementIndices)
        {
            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(str);
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    string currentTextElement = enumerator.GetTextElement();
                    Assert.Equal(expectedElements[enumerator.ElementIndex], currentTextElement);
                    Assert.Equal(currentTextElement, enumerator.Current);

                    Assert.Equal(expectedElementIndices[counter], enumerator.ElementIndex);
                    counter++;
                }
                Assert.Equal(expectedElementIndices.Length, counter);

                enumerator.Reset();
            }
        }

        [Fact]
        public void AccessingMembersBeforeEnumeration_ThrowsInvalidOperationException()
        {
            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator("abc");

            // Cannot access Current, ElementIndex or GetTextElement() before the enumerator has started
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.ElementIndex);
            Assert.Throws<InvalidOperationException>(() => enumerator.GetTextElement());
        }

        [Fact]
        public void AccessingMembersAfterEnumeration_ThrowsInvalidOperationException()
        {
            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator("abc");

            // Cannot access Current or GetTextElement() after the enumerator has finished
            // enumerating, but ElementIndex does not
            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Equal(3, enumerator.ElementIndex);
            Assert.Throws<InvalidOperationException>(() => enumerator.GetTextElement());
        }

        [Fact]
        public void AccessingMembersAfterReset_ThrowsInvalidOperationException()
        {
            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator("abc");
            enumerator.MoveNext();

            // Cannot access Current, ElementIndex or GetTextElement() after the enumerator has been reset
            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.ElementIndex);
            Assert.Throws<InvalidOperationException>(() => enumerator.GetTextElement());
        }
    }
}
