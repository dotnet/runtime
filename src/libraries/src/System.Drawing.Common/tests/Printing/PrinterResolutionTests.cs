// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.Drawing.Printing.Tests
{
    public class PrinterResolutionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            PrinterResolution pr = new PrinterResolution();
            Assert.Equal(PrinterResolutionKind.Custom, pr.Kind);
            Assert.Equal(0, pr.X);
            Assert.Equal(0, pr.Y);
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void X_Value_ReturnsExpected(int value)
        {
            PrinterResolution pr = new PrinterResolution();
            pr.X = value;
            Assert.Equal(value, pr.X);
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Y_Value_ReturnsExpected(int value)
        {
            PrinterResolution pr = new PrinterResolution();
            pr.Y = value;
            Assert.Equal(value, pr.Y);
        }

        [Theory]
        [InlineData(PrinterResolutionKind.Custom)]
        [InlineData(PrinterResolutionKind.Draft)]
        [InlineData(PrinterResolutionKind.High)]
        [InlineData(PrinterResolutionKind.Low)]
        [InlineData(PrinterResolutionKind.Medium)]
        public void Kind_ReturnsExpected(PrinterResolutionKind kind)
        {
            PrinterResolution pr = new PrinterResolution();
            pr.Kind = kind;
            Assert.Equal(kind, pr.Kind);
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [Theory]
        [InlineData(PrinterResolutionKind.Custom + 1)]
        [InlineData(PrinterResolutionKind.High - 1)]
        public void Kind_InvalidEnum_ThrowsInvalidEnumArgumentException(PrinterResolutionKind overflowKind)
        {
            PrinterResolution pr = new PrinterResolution();
            Assert.ThrowsAny<ArgumentException>(() => pr.Kind = overflowKind);
        }
    }
}
