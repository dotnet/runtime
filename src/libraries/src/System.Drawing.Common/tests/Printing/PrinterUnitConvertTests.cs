// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Author:
//
//	Jordi Mas i Hernandez, jordimash@gmail.com
//

using Xunit;

namespace System.Drawing.Printing.Tests
{
    public class PrinterUnitConvertTests
    {
        [Theory]
        [InlineData(PrinterUnit.Display, PrinterUnit.Display, 100)]
        [InlineData(PrinterUnit.Display, PrinterUnit.HundredthsOfAMillimeter, 2540)]
        [InlineData(PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter, 254)]
        [InlineData(PrinterUnit.Display, PrinterUnit.ThousandthsOfAnInch, 1000)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.Display, 4)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.HundredthsOfAMillimeter, 100)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.TenthsOfAMillimeter, 10)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.ThousandthsOfAnInch, 39)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.Display, 39)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.HundredthsOfAMillimeter, 1000)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.TenthsOfAMillimeter, 100)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.ThousandthsOfAnInch, 394)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.Display, 10)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.HundredthsOfAMillimeter, 254)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.TenthsOfAMillimeter, 25)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.ThousandthsOfAnInch, 100)]
        public void Convert_Int_ReturnsExpected(PrinterUnit fromUnit, PrinterUnit toUnit, int expectedResult)
        {
            var converted = PrinterUnitConvert.Convert(100, fromUnit, toUnit);
            Assert.Equal(expectedResult, converted);
        }

        [Theory]
        [InlineData(PrinterUnit.Display, PrinterUnit.Display, 100, 1000)]
        [InlineData(PrinterUnit.Display, PrinterUnit.HundredthsOfAMillimeter, 2540, 25400)]
        [InlineData(PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter, 254, 2540)]
        [InlineData(PrinterUnit.Display, PrinterUnit.ThousandthsOfAnInch, 1000, 10000)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.Display, 4, 39)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.HundredthsOfAMillimeter, 100, 1000)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.TenthsOfAMillimeter, 10, 100)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.ThousandthsOfAnInch, 39, 394)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.Display, 39, 394)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.HundredthsOfAMillimeter, 1000, 10000)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.TenthsOfAMillimeter, 100, 1000)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.ThousandthsOfAnInch, 394, 3937)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.Display, 10, 100)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.HundredthsOfAMillimeter, 254, 2540)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.TenthsOfAMillimeter, 25, 254)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.ThousandthsOfAnInch, 100, 1000)]
        public void Convert_Point_ReturnsExpected(PrinterUnit fromUnit, PrinterUnit toUnit, int expectedX, int expectedY)
        {
            var converted = PrinterUnitConvert.Convert(new Point(100, 1000), fromUnit, toUnit);
            Assert.Equal(new Point(expectedX, expectedY), converted);
        }

        [Theory]
        [InlineData(PrinterUnit.Display, PrinterUnit.Display, 100, 1000)]
        [InlineData(PrinterUnit.Display, PrinterUnit.HundredthsOfAMillimeter, 2540, 25400)]
        [InlineData(PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter, 254, 2540)]
        [InlineData(PrinterUnit.Display, PrinterUnit.ThousandthsOfAnInch, 1000, 10000)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.Display, 4, 39)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.HundredthsOfAMillimeter, 100, 1000)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.TenthsOfAMillimeter, 10, 100)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.ThousandthsOfAnInch, 39, 394)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.Display, 39, 394)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.HundredthsOfAMillimeter, 1000, 10000)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.TenthsOfAMillimeter, 100, 1000)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.ThousandthsOfAnInch, 394, 3937)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.Display, 10, 100)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.HundredthsOfAMillimeter, 254, 2540)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.TenthsOfAMillimeter, 25, 254)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.ThousandthsOfAnInch, 100, 1000)]
        public void Convert_Rectangle_ReturnsExpected(PrinterUnit fromUnit, PrinterUnit toUnit, int expectedLeftValue, int expectedRightValue)
        {
            var converted = PrinterUnitConvert.Convert(new Rectangle(100, 1000, 100, 1000), fromUnit, toUnit);
            Assert.Equal(new Rectangle(expectedLeftValue, expectedRightValue, expectedLeftValue, expectedRightValue), converted);
        }

        [Theory]
        [InlineData(PrinterUnit.Display, PrinterUnit.Display, 100, 1000)]
        [InlineData(PrinterUnit.Display, PrinterUnit.HundredthsOfAMillimeter, 2540, 25400)]
        [InlineData(PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter, 254, 2540)]
        [InlineData(PrinterUnit.Display, PrinterUnit.ThousandthsOfAnInch, 1000, 10000)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.Display, 4, 39)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.HundredthsOfAMillimeter, 100, 1000)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.TenthsOfAMillimeter, 10, 100)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.ThousandthsOfAnInch, 39, 394)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.Display, 39, 394)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.HundredthsOfAMillimeter, 1000, 10000)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.TenthsOfAMillimeter, 100, 1000)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.ThousandthsOfAnInch, 394, 3937)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.Display, 10, 100)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.HundredthsOfAMillimeter, 254, 2540)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.TenthsOfAMillimeter, 25, 254)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.ThousandthsOfAnInch, 100, 1000)]
        public void Convert_Size_ReturnsExpected(PrinterUnit fromUnit, PrinterUnit toUnit, int expectedX, int expectedY)
        {
            var converted = PrinterUnitConvert.Convert(new Size(100, 1000), fromUnit, toUnit);
            Assert.Equal(new Size(expectedX, expectedY), converted);
        }

        [Theory]
        [InlineData(PrinterUnit.Display, PrinterUnit.Display, 100, 1000, 100, 1000)]
        [InlineData(PrinterUnit.Display, PrinterUnit.HundredthsOfAMillimeter, 2540, 25400, 2540, 25400)]
        [InlineData(PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter, 254, 2540, 254, 2540)]
        [InlineData(PrinterUnit.Display, PrinterUnit.ThousandthsOfAnInch, 1000, 10000, 1000, 10000)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.Display, 4, 39, 4, 39)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.HundredthsOfAMillimeter, 100, 1000, 100, 1000)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.TenthsOfAMillimeter, 10, 100, 10, 100)]
        [InlineData(PrinterUnit.HundredthsOfAMillimeter, PrinterUnit.ThousandthsOfAnInch, 39, 394, 39, 394)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.Display, 39, 394, 39, 394)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.HundredthsOfAMillimeter, 1000, 10000, 1000, 10000)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.TenthsOfAMillimeter, 100, 1000, 100, 1000)]
        [InlineData(PrinterUnit.TenthsOfAMillimeter, PrinterUnit.ThousandthsOfAnInch, 394, 3937, 394, 3937)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.Display, 10, 100, 10, 100)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.HundredthsOfAMillimeter, 254, 2540, 254, 2540)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.TenthsOfAMillimeter, 25, 254, 25, 254)]
        [InlineData(PrinterUnit.ThousandthsOfAnInch, PrinterUnit.ThousandthsOfAnInch, 100, 1000, 100, 1000)]
        public void Convert_Margins_ReturnsExpected(PrinterUnit fromUnit, PrinterUnit toUnit, int expectedLeft, int expectedRight, int expectedTop, int expectedBottom)
        {
            var converted = PrinterUnitConvert.Convert(new Margins(100, 1000, 100, 1000), fromUnit, toUnit);
            Assert.Equal(new Margins(expectedLeft, expectedRight, expectedTop, expectedBottom), converted);
        }
    }
}
