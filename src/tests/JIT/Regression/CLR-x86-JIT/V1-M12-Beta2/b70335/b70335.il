// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x68de21f5 {}
.assembly extern xunit.core
{
    .publickeytoken = (
        8d 05 b1 bb 7a 6f db 6c
     )
     .ver 2:4:2:0
}
.class ILGEN_0x68de21f5 {
.method static int32 Method_0xe207be7a() {
.maxstack	7
.locals (int64 LOCAL_0x2)
 ldc.i8 0xdde08ac3a1c32e82 
 stloc LOCAL_0x2
Start_Orphan_0:
 ldloc LOCAL_0x2
 ldloc LOCAL_0x2
 add.ovf.un
 stloc LOCAL_0x2
End_Orphan_0:
 ldc.i4 100
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
call int32 ILGEN_0x68de21f5::Method_0xe207be7a()
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
