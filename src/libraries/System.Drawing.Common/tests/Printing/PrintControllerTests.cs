// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Drawing.Printing.Tests
{
    public class PrintControllerTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var controller = new SubPrintController();
            Assert.False(controller.IsPreview);
        }

        [ConditionalFact(Helpers.AnyInstalledPrinters, Helpers.IsDrawingSupported)]
        public void OnStartPage_InvokeWithPrint_ReturnsNull()
        {
            using (var document = new PrintDocument())
            {
                var controller = new SubPrintController();
                controller.OnStartPrint(document, new PrintEventArgs());

                var printEventArgs = new PrintPageEventArgs(null, Rectangle.Empty, Rectangle.Empty, null);
                Assert.Null(controller.OnStartPage(document, printEventArgs));

                // Call OnEndPage.
                controller.OnEndPage(document, printEventArgs);

                // Call EndPrint.
                controller.OnEndPrint(document, new PrintEventArgs());
            }
        }

        [Fact]
        public void OnStartPage_Invoke_ReturnsNull()
        {
            using (var document = new PrintDocument())
            {
                var controller = new SubPrintController();
                Assert.Null(controller.OnStartPage(document, new PrintPageEventArgs(null, Rectangle.Empty, Rectangle.Empty, null)));
                Assert.Null(controller.OnStartPage(null, null));
            }
        }

        [Fact]
        public void OnEndPage_InvokeWithoutStarting_Nop()
        {
            using (var document = new PrintDocument())
            {
                var controller = new SubPrintController();
                controller.OnEndPage(document, new PrintPageEventArgs(null, Rectangle.Empty, Rectangle.Empty, null));
                controller.OnEndPage(null, null);
            }
        }

        public static IEnumerable<object[]> PrintEventArgs_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new PrintEventArgs() };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(PrintEventArgs_TestData))]
        public void OnStartPrint_InvokeWithDocument_Success(PrintEventArgs e)
        {
            using (var document = new PrintDocument())
            {
                var controller = new SubPrintController();
                controller.OnStartPrint(document, e);

                // Call OnEndPrint
                controller.OnEndPrint(document, e);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(PrintEventArgs_TestData))]
        public void OnStartPrint_InvokeWithDocumentSeveralTimes_Success(PrintEventArgs e)
        {
            using (var document = new PrintDocument())
            {
                var controller = new SubPrintController();
                controller.OnStartPrint(document, e);
                controller.OnStartPrint(document, e);

                // Call OnEndPrint
                controller.OnEndPrint(document, e);
            }
        }

        [Fact]
        public void OnStartPrint_InvokeNullDocument_ThrowsNullReferenceException()
        {
            var controller = new SubPrintController();
            Assert.Throws<NullReferenceException>(() => controller.OnStartPrint(null, new PrintEventArgs()));
        }

        [Theory]
        [MemberData(nameof(PrintEventArgs_TestData))]
        public void OnEndPrint_InvokeWithoutStarting_Nop(PrintEventArgs e)
        {
            using (var document = new PrintDocument())
            {
                var controller = new SubPrintController();
                controller.OnEndPrint(document, e);
                controller.OnEndPrint(null, e);
            }
        }

        private class SubPrintController : PrintController
        {
        }
    }
}
