// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.Drawing.Printing.Test
{
    public class PrinterResolutionTest
    {
        [Fact]
        public void Default_Ctor()
        {
            PrinterResolution pr = new PrinterResolution();
            Assert.Equal(PrinterResolutionKind.Custom, pr.Kind);
        }

        [Theory]
        [InlineData(PrinterResolutionKind.High)]
        [InlineData(PrinterResolutionKind.Medium)]
        [InlineData(PrinterResolutionKind.Low)]
        [InlineData(PrinterResolutionKind.Draft)]
        [InlineData(PrinterResolutionKind.Custom)]
        public void Set_Kind(PrinterResolutionKind kind)
        {
            PrinterResolution pr = new PrinterResolution();
            pr.Kind = kind;
            Assert.Equal(kind, pr.Kind);
        }

        [Fact]
        public void Set_NotDefinedKind_ThrowsAnException()
        {
            PrinterResolution pr = new PrinterResolution();
            Assert.Throws<InvalidEnumArgumentException>(() => pr.Kind = (PrinterResolutionKind)999);
        }

        [Theory]
        [InlineData(Int32.MaxValue, Int32.MaxValue)]
        [InlineData(Int32.MinValue, Int32.MinValue)]
        [InlineData(1, 2)]
        public void Set_Coordinates(int x, int y)
        {
            PrinterResolution pr = new PrinterResolution();

            pr.X = x;
            pr.Y = y;

            Assert.Equal(x, pr.X);
            Assert.Equal(y, pr.Y);
        }
    }
}
