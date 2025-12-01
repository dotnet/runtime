// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly SwitchTests
{
}

.class public auto ansi beforefieldinit SwitchTestsType
       extends [System.Runtime]System.Object
{
    .method static public hidebysig void Switch.Int32Value_Valid() cil managed
    {
        .maxstack 1

        ldc.i4.s  10
        switch    (lbl_ret, lbl_a, lbl_a, lbl_b, lbl_b)

        br.s      lbl_ret

        lbl_a: nop
        br.s      lbl_ret

        lbl_b: nop

        lbl_ret: ret
    }

    .method static public hidebysig void Switch.Int64Value_Invalid_StackUnexpected() cil managed
    {
        .maxstack 1

        ldc.i8 10
        switch    (lbl_ret, lbl_a, lbl_a, lbl_b, lbl_b)

        br.s      lbl_ret

        lbl_a: nop
        br.s      lbl_ret

        lbl_b: nop

        lbl_ret: ret
    }

    .method static public hidebysig void Switch.InsideTry_Valid() cil managed
    {
        .maxstack 1

        .try
        {
            ldc.i4.s  10
            switch    (lbl_leave, lbl_a, lbl_a, lbl_b, lbl_b)

            br.s      lbl_leave

            lbl_a: nop
            br.s      lbl_leave

            lbl_b: nop

            lbl_leave: leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            nop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Switch.IntoTryStart_Valid() cil managed
    {
        .maxstack 1

        ldc.i4.s   10
        switch     (lbl_ret, lbl_a)

        br.s       lbl_ret

        .try
        {
            lbl_a: nop
            br.s     lbl_leave

            lbl_leave: leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            nop
            leave.s     lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Switch.IntoTryMiddle_Invalid_BranchIntoTry() cil managed
    {
        .maxstack 1

        ldc.i4.s   10
        switch     (lbl_ret, lbl_a)

        br.s       lbl_ret

        .try
        {
            nop
            lbl_a: br.s     lbl_leave

            lbl_leave: leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            nop
            leave.s     lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Switch.OutOfTry_Invalid_BranchOutOfTry() cil managed
    {
        .maxstack 1

        .try
        {
            ldc.i4.s  10
            switch    (lbl_ret, lbl_a)

            br.s      lbl_leave

            lbl_a: nop
            br.s      lbl_leave

            lbl_leave: leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            nop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Switch.NestedOutOfTry_Invalid_BranchOutOfTry() cil managed
    {
        .maxstack 1

        .try
        {
            .try
            {
                ldc.i4.s  10
                switch    (lbl_leave, lbl_a, lbl_leave2)

                br.s      lbl_leave

                lbl_a: nop
                br.s      lbl_leave

                lbl_leave: leave.s   lbl_leave2
            }
            catch [System.Runtime]System.Object
            {
                pop
                leave.s   lbl_leave2
            }

            lbl_leave2: leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Switch.NestedIntoTryStart_Valid() cil managed
    {
        .maxstack 1

        .try
        {
            ldc.i4.s  10
            switch    (lbl_leave, lbl_a, lbl_b)

            br.s      lbl_leave

            lbl_a: nop
            br.s      lbl_leave

            .try
            {
                lbl_b: nop
                leave.s   lbl_leave
            }
            catch [System.Runtime]System.Object
            {
                pop
                leave.s   lbl_leave
            }

            lbl_leave: leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Switch.NestedIntoTryMiddle_Invalid_BranchIntoTry() cil managed
    {
        .maxstack 1

        .try
        {
            ldc.i4.s  10
            switch    (lbl_leave, lbl_a, lbl_b)

            br.s      lbl_leave

            lbl_a: nop
            br.s      lbl_leave

            .try
            {
                nop
                lbl_b: leave.s   lbl_leave
            }
            catch [System.Runtime]System.Object
            {
                pop
                leave.s   lbl_leave
            }

            lbl_leave: leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Switch.ObjectValue_Invalid_StackUnexpected() cil managed
    {
        .maxstack 1

        ldnull
        switch    (lbl_ret, lbl_a, lbl_a, lbl_b, lbl_b)

        br.s      lbl_ret

        lbl_a: nop
        br.s      lbl_ret

        lbl_b: nop

        lbl_ret: ret
    }
}
