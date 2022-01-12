// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x8ecc201e {}
.assembly extern xunit.core {}
.class ILGEN_0x8ecc201e {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	9
.try {
.locals (int64[] LOCAL_0x0,unsigned int8 LOCAL_0x1,unsigned int32 LOCAL_0x2)

 ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc LOCAL_0x0
 ldc.i4 0xfc2ee0a 
 stloc LOCAL_0x1
 ldc.i4 0x3d52e05c 
 stloc LOCAL_0x2

 ldc.i8 0x8e088f7ea1ab1693
 conv.u2
 ldc.i4.5
 ldc.i4.5
 ble.un Branch_0x22
 ldloc LOCAL_0x2
 br Branch_0x23
Branch_0x22:
 ldc.i4.5
Branch_0x23:
Branch_0x1b:
 ceq
 ldc.i4 0x57a1ee75
 conv.u4
 ldc.i4.m1
 ldc.i4.7
 add.ovf.un
 bne.un Branch_0x26
 ldc.i4 0x21c5a77e
 br Branch_0x27
Branch_0x26:
 ldc.i4.6
 ldc.i4.m1
 shr
Branch_0x27:
 ldc.i4.8
 ldc.i4 0xb38b868
 conv.ovf.i.un
 ceq
 shr.un
 and
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
