// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Tests;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class RealParserTests : RealParserTestsBase
    {
        // The actual tests are defined in: src\Common\tests\System\RealParserTestsBase.netcoreapp.cs

        //  We need 1 additional byte, per length, for the terminating null
        private const int DoubleNumberBufferLength = 767 + 1 + 1;  // 767 for the longest input + 1 for rounding: 4.9406564584124654E-324 
        private const int SingleNumberBufferLength = 112 + 1 + 1;  // 112 for the longest input + 1 for rounding: 1.40129846E-45

        protected override string InvariantToStringDouble(double d)
        {
            Span<byte> s = stackalloc byte[DoubleNumberBufferLength];
            bool formatSucceeded = Utf8Formatter.TryFormat(d, s, out int bytesWritten, StandardFormat.Parse("E17"));
            Assert.True(formatSucceeded, $"Failed to format {d.ToString("G17", CultureInfo.InvariantCulture)}");
            return s.ToUtf16String();
        }

        protected override string InvariantToStringSingle(float f)
        {
            Span<byte> s = stackalloc byte[SingleNumberBufferLength];
            bool formatSucceeded = Utf8Formatter.TryFormat(f, s, out int bytesWritten, StandardFormat.Parse("E9"));
            Assert.True(formatSucceeded, $"Failed to format {f.ToString("G9", CultureInfo.InvariantCulture)}");
            return s.ToUtf16String();
        }

        protected override bool InvariantTryParseDouble(string s, out double result)
        {
            return Utf8Parser.TryParse(s.ToUtf8Span(), out result, out int bytesConsumed);
        }

        protected override bool InvariantTryParseSingle(string s, out float result)
        {
            return Utf8Parser.TryParse(s.ToUtf8Span(), out result, out int bytesConsumed);
        }
    }
}

