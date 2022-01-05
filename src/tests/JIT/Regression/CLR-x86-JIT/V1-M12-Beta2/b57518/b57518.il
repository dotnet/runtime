// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b57518' {}
.assembly extern xunit.core {}
.class ILGEN_0xa4d819be {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
.locals (unsigned int8 local_0x0,int64 local_0x1)
 ldc.i4 0x49e06da2 
 stloc local_0x0
 ldc.i8 0x760d196330615623 
 stloc local_0x1

Start_Orphan_0:
 ldc.i4 0x7f0aa403 
 ldloc local_0x0
 bgt Branch_0x0
 Branch_0x0:
End_Orphan_0:

 ldloc local_0x1
 ldc.r4 float32(0x458e8bc9)
 neg
 conv.u8
 add.ovf.un
 ldc.i4.3
 conv.i8
 cgt.un
 ldc.i4.8
 ceq
pop
leave END
} catch [mscorlib]System.OverflowException {
pop
leave END
}
END:
ldc.i4 100
ret

}
}
