// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextElementEnumeratorTests
    {
        public static IEnumerable<object[]> Enumerate_TestData()
        {
            yield return new object[] { new string[] { /* empty */ } };
            yield return new object[] { new string[] { "H", "e", "l", "l", "o" } };

            // Creates and initializes a string containing the following:
            //   - a surrogate pair (high surrogate U+D800 and low surrogate U+DC00)
            //   - a combining character sequence (the Latin small letter "a" followed by the combining grave accent)
            //   - a base character (the ligature "")
            yield return new object[] { new string[] { "\uD800\uDC00", "\uD800\uDC00", "\u0061\u0300", "\u0061\u0300", "\u00C6" } };
        }

        [Theory]
        [MemberData(nameof(Enumerate_TestData))]
        public void Enumerate(string[] expectedElements)
        {
            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(string.Concat(expectedElements));
            for (int i = 0; i < 2; i++)
            {
                int charsProcessedSoFar = 0;

                foreach (string expectedElement in expectedElements)
                {
                    Assert.True(enumerator.MoveNext());
                    Assert.Equal(charsProcessedSoFar, enumerator.ElementIndex);
                    Assert.Equal(expectedElement, enumerator.Current);
                    charsProcessedSoFar += expectedElement.Length;
                }

                Assert.False(enumerator.MoveNext());
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

            // Cannot access Current, ElementIndex, or GetTextElement() after the enumerator has finished
            // enumerating.
            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.ElementIndex);
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
