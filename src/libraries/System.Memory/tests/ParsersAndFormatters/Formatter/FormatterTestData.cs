// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text.Tests
{
    public sealed class FormatterTestData<T>
    {
        public FormatterTestData(T value, SupportedFormat format, byte precision, string expectedOutput)
        {
            Value = value;
            Format = format;
            Precision = precision;
            ExpectedOutput = expectedOutput;
            PassedInBufferLength = expectedOutput.Length;
        }

        public T Value { get; }
        public char FormatSymbol => Format.Symbol;
        public byte Precision { get; }
        public SupportedFormat Format { get; }
        public string ExpectedOutput { get; }
        public int PassedInBufferLength { get; set; } // by default, the length of expected output: test cases can override with shorter or longer lengths

        public ParserTestData<T> ToParserTestData()
        {
            return new ParserTestData<T>(ExpectedOutput, Value, FormatSymbol, expectedSuccess: true);
        }

        public sealed override string ToString()
        {
            //
            // Take good care of this method: it affects Xunit output and makes a lot of difference in how annoying test investigations are.
            //

            string formatString = (FormatSymbol == default) ?
                "default" :
                FormatSymbol + ((Precision == StandardFormat.NoPrecision) ?
                    string.Empty :
                    Precision.ToString());

            string bufferLengthString;
            if (PassedInBufferLength == ExpectedOutput.Length)
            {
                bufferLengthString = string.Empty;
            }
            else if (PassedInBufferLength < ExpectedOutput.Length)
            {
                bufferLengthString = $", Buffer Length = {PassedInBufferLength} bytes (too short)";
            }
            else
            {
                bufferLengthString = $", Buffer Length = {PassedInBufferLength} bytes (longer than needed)";
            }

            return $"[Format{typeof(T).Name} {Value.DisplayString()},{formatString} to '{ExpectedOutput}'{bufferLengthString})]";
        }
    }
}
