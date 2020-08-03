// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x44a8fb5b {}
.class ILGEN_0x44a8fb5b {
.method static unsigned int64  Method_0x18dabcc4( int32 Arg_0x3) {
.maxstack	5
  ldarg Arg_0x3
 conv.ovf.u8
  ldarg Arg_0x3
 conv.ovf.u8
 blt.un Branch_0x0
 ldc.i4 0x64
 conv.u8
 br Branch_0x1
Branch_0x0:
 ldc.i8 0x55
Branch_0x1:
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	15
.try {
ldc.i4 0xd48892f8
 call unsigned int64  ILGEN_0x44a8fb5b::Method_0x18dabcc4(int32 Arg_0x3)
conv.i4
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
