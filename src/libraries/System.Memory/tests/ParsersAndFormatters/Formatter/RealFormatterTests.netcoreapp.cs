// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Tests;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class RealFormatterTests : RealFormatterTestsBase
    {
        // The actual tests are defined in: src\Common\tests\System\RealFormatterTestsBase.netcoreapp.cs

        //  We need 1 additional byte, per length, for the terminating null
        private const int DoubleNumberBufferLength = 767 + 1 + 1;  // 767 for the longest input + 1 for rounding: 4.9406564584124654E-324 
        private const int SingleNumberBufferLength = 112 + 1 + 1;  // 112 for the longest input + 1 for rounding: 1.40129846E-45

        protected override string InvariantToStringDouble(double d, string format)
        {
            Span<byte> s = stackalloc byte[DoubleNumberBufferLength];
            bool formatSucceeded = Utf8Formatter.TryFormat(d, s, out int bytesWritten, StandardFormat.Parse(format));
            Assert.True(formatSucceeded, $"Failed to format '{d.ToString(CultureInfo.InvariantCulture)}' using '{format}'.");
            return s.Slice(0, bytesWritten).ToUtf16String();
        }

        protected override string InvariantToStringSingle(float f, string format)
        {
            Span<byte> s = stackalloc byte[SingleNumberBufferLength];
            bool formatSucceeded = Utf8Formatter.TryFormat(f, s, out int bytesWritten, StandardFormat.Parse(format));
            Assert.True(formatSucceeded, $"Failed to format '{f.ToString(CultureInfo.InvariantCulture)}' using '{format}'.");
            return s.Slice(0, bytesWritten).ToUtf16String();
        }
    }
}
