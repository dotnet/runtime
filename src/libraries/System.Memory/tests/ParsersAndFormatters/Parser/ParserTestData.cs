// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Buffers.Text.Tests
{
    public sealed class ParserTestData<T>
    {
        public ParserTestData(string text, T expectedValue, char formatSymbol, bool expectedSuccess)
        {
            Text = text;
            ExpectedValue = expectedValue;
            FormatSymbol = formatSymbol;
            ExpectedSuccess = expectedSuccess;
            ExpectedBytesConsumed = Encoding.UTF8.GetByteCount(text);
        }

        public string Text { get; }
        public T ExpectedValue { get; }
        public char FormatSymbol { get; }
        public bool ExpectedSuccess { get; }
        public int ExpectedBytesConsumed { get; set; }  // Has a public setter so that individual test cases can override. By default, it's set to the Utf8 character length of Text

        public sealed override string ToString()
        {
            //
            // Take good care of this method: it affects Xunit output and makes a lot of difference in how annoying test investigations are.
            //

            string formatString = (FormatSymbol == default) ?
                "default" :
                FormatSymbol.ToString();

            return $"[Parse{typeof(T).Name} '{Text}',{formatString} to {(ExpectedSuccess ? ExpectedValue.DisplayString() : "(should-not-parse)")})]";
        }
    }
}
