// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly '118414' {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}

.class public ILGEN_0x3243f637 {

    .method static int64  Method_0xa31c545(native int Arg_0x1) {
        .zeroinit 
        .maxstack	9
        .locals (int32 count)

        //BEGIN INIT LOCALS
        ldc.i4.4
        stloc.0
        //END INIT LOCALS

Loop_0x0:
        ldloc.0
        ldc.i4 1 
        sub
        stloc.0

        ldarga Arg_0x1
        ldc.i4 0xeece6911
        stind.i4

        ldarga Arg_0x1
        ldc.i4 0xeece6911
        stind.i

        ldloc.0
        brtrue Loop_0x0

        ldc.i8 100 
        ret
    }

    .method public static int32 Main() {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .maxstack	5

        ldc.i4 0xf28bc4c7 
        call int64  ILGEN_0x3243f637::Method_0xa31c545(native int Arg_0x1)
        conv.i4
        ret
    }

}
