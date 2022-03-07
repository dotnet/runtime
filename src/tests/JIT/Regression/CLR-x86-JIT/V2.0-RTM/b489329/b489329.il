// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly 'b489329' {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}
.class FullProof {

.method static int32 Test() {
.maxstack 80
.zeroinit
.locals(int32 test2, int64 test14, float64 test23, object test37)

IL_1:    ldloc test2			//i
IL_30:     ldloc test23			//ri
IL_31:      ldc.r8 55.5			//rri
IL_34:      conv.ovf.i8			//Iri
IL_35:       ldloc test14			//IIri
IL_36:        ldc.i4 -1			//iIIri

IL_37:         ldloc test37			//oiIIri
IL_38:        brfalse IL_41			//iIIri
IL_39:        nop			//iIIri
IL_40:        nop			//iIIri

IL_41:       shl			//IIri
IL_42:       conv.r.un			//rIri
IL_45:       ckfinite			//rIri
IL_47:       conv.i8			//IIri
IL_50:     bgt IL_77			//ri
IL_51:     conv.u8			//Ii
IL_55:      ldc.i8 1			//IIi
IL_68:     clt			//ii
IL_69:    shr.un			//i
IL_71:     ldc.r4 0.0			//ri
IL_77:     conv.i8			//Ii
IL_79:    pop			//i
IL_80:   ret			//
}

.method public static int32  Main()
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack 3
 call int32 FullProof::Test()
 pop
 ldstr "Test SUCCESS"
 call void [System.Console]System.Console::WriteLine(string)
 ldc.i4 100
 ret
}
}


