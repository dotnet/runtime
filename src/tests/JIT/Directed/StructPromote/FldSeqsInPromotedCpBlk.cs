// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

// In this test, we have a block assignment with a source that is a promoted struct and
// an indirect destination. When morphing it, we would decompose that assignment into a series
// of field assignments: `IND(ADDR) = FirstLngValue; IND(ADDR_CLONE + 8) = SecondLngValue`.
// In the process, we would also attach field sequences to the destination addresses so that VN
// knew to analyze them. That was the part which was susceptible to the bug being tested: morphing
// reused the address node (the "firstElem" LCL_VAR) for the first field, and at the same time
// used it to create more copies for subsequent addresses.
//
// Thus:
//   1) A zero-offset field sequence for the first field was attached to ADDR
//   2) ADDR was cloned, the clone still had the sequence attached
//   3) ADD(ADDR_CLONE [FldSeq FirstLngValue], 8 [FldSeq SecondLngValue]) was created.
//
// And so we ended up with an incorrect FldSeq: [FirstLngValue, SecondLngValue], causing
// VN to wrongly treat the "firstElem = b" store as not modifiying SecondLngValue.
//
// The fix was to reuse the address for the last field instead.

class FldSeqsInPromotedCpBlk
{
    public static int Main()
    {
        PromotableStruct s = default;
        return Problem(new PromotableStruct[1]) == 2 ? 100 : 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Problem(PromotableStruct[] a)
    {
        ref var firstElem = ref a[0];

        firstElem.SecondLngValue = 1;
        var b = new PromotableStruct() { SecondLngValue = 2 };

        if (firstElem.SecondLngValue == 1)
        {
            firstElem = b;
            return firstElem.SecondLngValue;
        }

        return -1;
    }
}

struct PromotableStruct
{
    public long FirstLngValue;
    public long SecondLngValue;
}
