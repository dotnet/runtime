// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextElementEnumeratorReset
    {
        // PosTest1: Calling Reset method
        [Fact]
        public void PosTest1()
        {
            // Creates and initializes a String containing the following:
            //   - a surrogate pair (high surrogate U+D800 and low surrogate U+DC00)
            //   - a combining character sequence (the Latin small letter "a" followed by the combining grave accent)
            //   - a base character (the ligature "")
            String myString = "\uD800\uDC00\u0061\u0300\u00C6";
            // Creates and initializes a TextElementEnumerator for myString.
            TextElementEnumerator myTEE = StringInfo.GetTextElementEnumerator(myString);
            myTEE.Reset();
            myTEE.MoveNext();
            Assert.Equal(0, myTEE.ElementIndex);
        }
    }
}

