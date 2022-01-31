// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b57492' {}
.assembly extern xunit.core {}
.class ILGEN_0x2019eaf2 {
.field static	unsigned int64 field_0x4
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
.locals (int8 local_0x2)

 ldc.i4 0x69f07ea
 stloc local_0x2
 ldc.i8 5
 stsfld unsigned int64 ILGEN_0x2019eaf2::field_0x4
 ldc.i4.4
 ldc.i4 4
 blt  Branch_0x0
Branch_0x0:
 ldc.i8 0xe
 ldsfld unsigned int64 ILGEN_0x2019eaf2::field_0x4
 conv.i4
 shr
 ldloc local_0x2
 conv.ovf.u8
 ldc.i8 0x3
 ceq
 shr
 pop
 ldc.i4 55
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
