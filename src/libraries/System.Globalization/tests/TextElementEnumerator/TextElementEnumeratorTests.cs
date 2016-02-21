// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextElementEnumeratorMoveNext
    {
        [Fact]
        public void Enumerate()
        {
            string[] expectedElements = new string[] { "\uD800\uDC00", "\uD800\uDC00", "\u0061\u0300", "\u0061\u0300", "\u00C6" };
            int[] expectedElementIndices = new int[] { 0, 2, 4 };
            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator("\uD800\uDC00\u0061\u0300\u00C6");

            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(expectedElements[enumerator.ElementIndex], enumerator.GetTextElement());
                    Assert.Equal(enumerator.GetTextElement(), enumerator.Current);

                    Assert.Equal(expectedElementIndices[counter], enumerator.ElementIndex);
                    counter++;
                }
                enumerator.Reset();
            }
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
