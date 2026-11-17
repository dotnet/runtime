// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics.X86;

class Program
{
    static int Main()
    {
        if (AvxVnni.V512.IsSupported || AvxVnniInt8.V512.IsSupported || AvxVnniInt16.V512.IsSupported)
        {
            Console.WriteLine("BUG: A 512-bit VNNI instruction set is supported, so the instruction set configuration is not the one under test.");
            return 1;
        }

        Random random = new Random(1234);
        bool passed = true;

        if (AvxVnni.IsSupported)
        {
            passed &= Report("AvxVnni", VnniRegisterPressure.CountWrongAvxVnniResults(random));
        }
        else
        {
            Console.WriteLine("No support for AvxVnni, skipping it.");
        }

        if (AvxVnniInt8.IsSupported)
        {
            passed &= Report("AvxVnniInt8", VnniRegisterPressure.CountWrongAvxVnniInt8Results(random));
        }
        else
        {
            Console.WriteLine("No support for AvxVnniInt8, skipping it.");
        }

        if (AvxVnniInt16.IsSupported)
        {
            passed &= Report("AvxVnniInt16", VnniRegisterPressure.CountWrongAvxVnniInt16Results(random));
        }
        else
        {
            Console.WriteLine("No support for AvxVnniInt16, skipping it.");
        }

        return passed ? 100 : 1;
    }

    static bool Report(string isa, int wrong)
    {
        if (wrong != 0)
        {
            Console.WriteLine($"BUG: {wrong} of {VnniRegisterPressure.ResultCount} {isa} results are wrong.");
            return false;
        }

        return true;
    }
}
