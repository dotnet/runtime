// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly ASSEMBLY_NAME {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}
.class public FullProof {

.method static int32 Test() {
.maxstack 66
.zeroinit
.locals(int64 test10)
	 ldc.r8 1       //  1
         ckfinite       //  1
         ldc.r4 0       //  0 1
         ldc.r4 0       //  0 0 1
         ldc.i8 1       //  1 0 0 1
         conv.ovf.u8    //  1 0 0 1
         ldc.i4 1       //  1 1 0 0 1
         ldc.i4 0       //  0 1 1 0 0 1 
         ldc.i8 -1      // -1 0 1 1 0 0 1
         ldloc test10   // 0 -1 0 1 1 0 0 1
         ble.un IL_57   //  0 1 1 0 0 1
         add.ovf.un     //  1 1 0 0 1
         ldc.i4 5       //  5 1 1 0 0 1
IL_57:   shr.un         //  2 1 0 0 1
         shr.un         //  1 0 0 1
         pop            //  0 0 1
         pop            //  0 1
         cgt            //  1
         dup
         call void [System.Console]System.Console::WriteLine(int32)
         ret
}

  .method public static int32  Main()
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack 3
        ldc.r4 1
        ldc.r8 0
        cgt
        call void [System.Console]System.Console::WriteLine(int32)

        call int32 FullProof::Test()

	ldc.i4 1
	ceq
	brtrue.s PASS

FAIL:
	ldstr      "FAIL"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.0
	br.s       END
PASS:
	ldstr      "PASS"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4     100
	br.s       END

END:

        ret
}

}

