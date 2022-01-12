// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x12f85afc {}
.assembly extern xunit.core {}
.class ILGEN_0x12f85afc {
.method static int32 Method_0x7e13e3a1(native int Arg_0x3) {
.maxstack	7

ldc.i8 0x1474e5c93dd7fded 
 ldc.i8 0xecfbf93488b64350
 add.ovf.un
ldc.i4 0xf3d3f215 conv.i
 conv.ovf.i8.un
 beq Branch_0x0

ldc.i4 0xf3d3f215 conv.i
 ldc.i4.4
 sub.ovf.un
 br Branch_0x1

 Branch_0x0:

 ldarg Arg_0x3
 ldarg Arg_0x3
 mul.ovf.un
 Branch_0x1:

 conv.ovf.u4.un
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0xf3d3f215 conv.i

call int32 ILGEN_0x12f85afc::Method_0x7e13e3a1(native int Arg_0x3)
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
