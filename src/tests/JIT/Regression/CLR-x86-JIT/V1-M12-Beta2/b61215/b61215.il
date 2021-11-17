// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x947e98be {}
.assembly extern xunit.core {}
.class ILGEN_0x947e98be {
.field static	native int field_0x0
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	13
.try {

 .locals (int64 LOCAL_0x0,native int LOCAL_0x3,float32 LOCAL_0x4)
ldc.i8 0x7ff6df43481ab153 
stloc LOCAL_0x0
ldc.i4 0x5970466c 
stloc LOCAL_0x3
ldc.r4 float32(0x9c601427) 
stloc LOCAL_0x4

ldc.i4 0xd4f6f07d 
stsfld native int ILGEN_0x947e98be::field_0x0
 ldloc LOCAL_0x4
 conv.ovf.i
 ldloc LOCAL_0x4
 conv.u1
 bgt Branch_0x36
 ldc.r4 float32(0xa4c913ea)
 conv.ovf.i
 ldsfld native int ILGEN_0x947e98be::field_0x0
Branch_0x4b:
 ble.un Branch_0x48
 ldc.i8 0xf26d9013724c7484
 conv.ovf.u.un
 br Branch_0x49
Branch_0x48:
 ldc.i4.s 4
 ldsfld native int ILGEN_0x947e98be::field_0x0
 bge Branch_0x50
 ldloc LOCAL_0x3
 br Branch_0x51
Branch_0x50:
 ldloc LOCAL_0x3
Branch_0x51:
 ldloc LOCAL_0x0
 conv.ovf.i
 blt.un Branch_0x4e
 ldc.i4.m1
 br Branch_0x4f
Branch_0x4e:
 ldloc LOCAL_0x3
Branch_0x4f:
Branch_0x49:
 br Branch_0x37
Branch_0x36:
 ldc.i4.2
 conv.ovf.i8
 ldloc LOCAL_0x0
Branch_0x55:
 clt
 ldloc LOCAL_0x0
 conv.ovf.i2.un
 Branch_0x57:
 mul
 Branch_0x37:
 conv.ovf.u8
 conv.u1
EOM: pop
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
