// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}


.assembly Test1 {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}
.class FullProof {

.method static int32 Test() {
.maxstack 47

.zeroinit
.locals(int32 test0, int32 test1, int32 test2, int32 test3, int32 test4, int32 test5, int32 test6, int32 test7, int32 test8, int32 test9, int64 test10, int64 test11, int64 test12, int64 test13, int64 test14, int64 test15, int64 test16, int64 test17, int64 test18, int64 test19, float64 test20, float64 test21, float64 test22, float64 test23, float64 test24, float64 test25, float64 test26, float64 test27, float64 test28, float64 test29, object test30, object test31, object test32, object test33, object test34, object test35, object test36, object test37, object test38, object test39)

IL_1:    ldc.i8 1			//I
IL_2:     ldc.i4 0			//iI
IL_3:      ldc.r4 0.0			//riI
IL_4:       ldc.r4 55.5			//rriI
IL_5:       nop			//rriI
IL_6:       conv.r8			//rriI
IL_7:       nop			//rriI
IL_8:      mul			//riI
IL_9:     pop			//iI
IL_10:    shr.un			//I
IL_11:     ldloc test8			//iI
IL_12:      ldc.i4 -1			//iiI
IL_13:       ldc.i4.s 0			//iiiI
IL_14:        ldloc test8			//iiiiI
IL_15:       mul			//iiiI
IL_16:        ldc.i4 0			//iiiiI
IL_17:        nop			//iiiiI
IL_18:       add.ovf.un			//iiiI
IL_19:        ldloc test2			//iiiiI
IL_20:         ldloc test0			//iiiiiI
IL_21:         nop			//iiiiiI
IL_22:       beq IL_29			//iiiI
IL_23:      cgt.un			//iiI
IL_24:       ldloc test3			//iiiI
IL_25:        ldc.i8 0			//IiiiI
IL_26:         ldc.i8 0			//IIiiiI
IL_27:        brfalse IL_28			//IiiiI
IL_28:       pop			//iiiI
IL_29:      and			//iiI
IL_30:       ldloc test31			//oiiI
IL_31:      brtrue IL_32			//iiI
IL_32:     shr			//iI
IL_33:      ldloc test32			//oiI
IL_34:      nop			//oiI
IL_35:     pop			//iI
IL_36:     conv.ovf.i8.un			//II
IL_37:      ldloc test2			//iII
IL_38:       ldc.i4.s 0			//iiII
IL_39:       conv.i2			//iiII
IL_40:      sub			//iII
IL_41:     shr			//II
IL_42:    mul			//I
IL_43:    conv.ovf.i4.un			//i
IL_44:     ldloc test15			//Ii
IL_45:      ldloc test19			//IIi
IL_46:    blt IL_47			//i
IL_47:   ret			//
}

  .method public static int32  Main()
{.entrypoint
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.maxstack 3

call int32 FullProof::Test()
pop

ldstr "Test SUCCESS"
call void [System.Console]System.Console::WriteLine(string)
ldc.i4 100
ret
}
}

