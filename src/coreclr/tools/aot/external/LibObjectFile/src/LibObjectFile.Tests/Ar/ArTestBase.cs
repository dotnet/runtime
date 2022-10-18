// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using NUnit.Framework;

namespace LibObjectFile.Tests.Ar
{
    public abstract class ArTestBase
    {
        protected static void ExpectNoDiagnostics(DiagnosticBag diagnostics)
        {
            if (diagnostics.Messages.Count != 0)
            {
                Console.WriteLine(diagnostics);
                Assert.AreEqual(0, diagnostics.Messages.Count, $"Invalid number of diagnostics found, expecting no diagnostics");
            }
        }

        protected static void ExpectDiagnostics(DiagnosticBag diagnostics, params DiagnosticId[] ids)
        {
            if (diagnostics.Messages.Count != ids.Length)
            {
                Console.WriteLine(diagnostics);
                Assert.AreEqual(ids.Length, diagnostics.Messages.Count, $"Invalid number of diagnostics found, expecting {ids.Length} entries [{string.Join(", ", ids)}]");
            }

            for (var i = 0; i < diagnostics.Messages.Count; i++)
            {
                var diagnosticsMessage = diagnostics.Messages[i];
                var expectedId = ids[i];

                if (expectedId != diagnosticsMessage.Id)
                {
                    Console.WriteLine(diagnostics);
                    Assert.AreEqual(expectedId, diagnosticsMessage.Id, $"Invalid Id {diagnosticsMessage.Id} found for diagnostics [{i}] while expecting {expectedId} from entries [{string.Join(", ", ids)}]");
                }
            }
        }
   }
}