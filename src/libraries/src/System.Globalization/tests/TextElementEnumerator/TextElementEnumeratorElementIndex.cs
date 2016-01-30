// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextElementEnumeratorElementIndex
    {
        // PosTest1: Calling Current Property
        [Fact]
        public void PosTest1()
        {
            // Creates and initializes a String containing the following:
            //   - a surrogate pair (high surrogate U+D800 and low surrogate U+DC00)
            //   - a combining character sequence (the Latin small letter "a" followed by the combining grave accent)
            //   - a base character (the ligature "")
            String myString = "\uD800\uDC00\u0061\u0300\u00C6";
            int[] expectValue = new int[3];
            expectValue[0] = 0;
            expectValue[1] = 2;
            expectValue[2] = 4;
            // Creates and initializes a TextElementEnumerator for myString.
            TextElementEnumerator myTEE = StringInfo.GetTextElementEnumerator(myString);
            myTEE.Reset();
            int i = 0;
            while (myTEE.MoveNext())
            {
                Assert.Equal(myTEE.ElementIndex, expectValue[i]);
                i++;
            }
        }

        // NegTest1: The enumerator is positioned before the first text element of the string
        [Fact]
        public void NegTest1()
        {
            String myString = "\uD800\uDC00\u0061\u0300\u00C6";
            // Creates and initializes a TextElementEnumerator for myString.
            TextElementEnumerator myTEE = StringInfo.GetTextElementEnumerator(myString);
            myTEE.Reset();
            Assert.Throws<InvalidOperationException>(() =>
            {
                int actualValue = myTEE.ElementIndex;
            });
        }
    }
}

