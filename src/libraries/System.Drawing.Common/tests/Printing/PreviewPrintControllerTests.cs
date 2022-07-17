// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Drawing.Printing.Tests
{
    public class PreviewPrintControllerTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var controller = new PreviewPrintController();
            Assert.True(controller.IsPreview);
        }

        [ConditionalFact(Helpers.AnyInstalledPrinters, Helpers.IsDrawingSupported)]
        public void OnStartPage_InvokeWithPrint_ReturnsNull()
        {
            using (var document = new PrintDocument())
            {
                var controller = new PreviewPrintController();
                controller.OnStartPrint(document, new PrintEventArgs());

                var printEventArgs = new PrintPageEventArgs(null, Rectangle.Empty, Rectangle.Empty, new PageSettings());
                Assert.NotNull(controller.OnStartPage(document, printEventArgs));

                // Call OnEndPage.
                controller.OnEndPage(document, printEventArgs);

                // Call EndPrint.
                controller.OnEndPrint(document, new PrintEventArgs());
            }
        }

        [Fact]
        public void OnStartPage_InvokeNullDocument_ThrowsNullReferenceException()
        {
            var controller = new PreviewPrintController();
            var e = new PrintPageEventArgs(null, Rectangle.Empty, Rectangle.Empty, null);
            Assert.Throws<NullReferenceException>(() => controller.OnStartPage(null, e));
        }

        [Fact]
        public void OnStartPage_InvokeNullEventArgs_ThrowsNullReferenceException()
        {
            using (var document = new PrintDocument())
            {
                var controller = new PreviewPrintController();
                Assert.Throws<NullReferenceException>(() => controller.OnStartPage(document, null));
            }
        }

        [ConditionalFact(Helpers.AnyInstalledPrinters, Helpers.IsDrawingSupported)]
        public void OnStartPage_InvokeNullEventArgsPageSettings_ReturnsNull()
        {
            using (var document = new PrintDocument())
            {
                var controller = new PreviewPrintController();
                controller.OnStartPrint(document, new PrintEventArgs());

                var printEventArgs = new PrintPageEventArgs(null, Rectangle.Empty, Rectangle.Empty, null);
                Assert.Throws<NullReferenceException>(() => controller.OnStartPage(document, printEventArgs));
            }
        }

        [Fact]
        public void OnStartPage_PrintNotStarted_ThrowsNullReferenceException()
        {
            using (var document = new PrintDocument())
            {
                var controller = new PreviewPrintController();
                var e = new PrintPageEventArgs(null, Rectangle.Empty, Rectangle.Empty, null);
                Assert.Throws<NullReferenceException>(() => controller.OnStartPage(document, e));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Fixed a NullReferenceException")]
        public void OnEndPage_InvokeWithoutStarting_Nop()
        {
            using (var document = new PrintDocument())
            {
                var controller = new PreviewPrintController();
                controller.OnEndPage(document, new PrintPageEventArgs(null, Rectangle.Empty, Rectangle.Empty, null));
                controller.OnEndPage(null, null);
            }
        }

        public static IEnumerable<object[]> PrintEventArgs_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new PrintEventArgs() };
        }

        [ConditionalTheory(Helpers.AnyInstalledPrinters, Helpers.IsDrawingSupported)]
        [MemberData(nameof(PrintEventArgs_TestData))]
        public void OnStartPrint_InvokeWithDocument_Success(PrintEventArgs e)
        {
            using (var document = new PrintDocument())
            {
                var controller = new PreviewPrintController();
                controller.OnStartPrint(document, e);

                // Call OnEndPrint
                controller.OnEndPrint(document, e);
            }
        }

        [ConditionalFact(Helpers.AnyInstalledPrinters, Helpers.IsDrawingSupported)]
        public void OnStartPrint_InvokeMultipleTimes_Success()
        {
            using (var document = new PrintDocument())
            {
                var controller = new PreviewPrintController();
                controller.OnStartPrint(document, new PrintEventArgs());
                controller.OnStartPrint(document, new PrintEventArgs());

                // Call OnEndPrint
                controller.OnEndPrint(document, new PrintEventArgs());
            }
        }

        [Fact]
        public void OnStartPrint_InvokeNullDocument_ThrowsNullReferenceException()
        {
            var controller = new PreviewPrintController();
            Assert.Throws<NullReferenceException>(() => controller.OnStartPrint(null, new PrintEventArgs()));
        }

        [Theory]
        [MemberData(nameof(PrintEventArgs_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Fixed a NullReferenceException")]
        public void OnEndPrint_InvokeWithoutStarting_Nop(PrintEventArgs e)
        {
            using (var document = new PrintDocument())
            {
                var controller = new PreviewPrintController();
                controller.OnEndPrint(document, e);
                controller.OnEndPrint(null, e);
            }
        }
    }
}
