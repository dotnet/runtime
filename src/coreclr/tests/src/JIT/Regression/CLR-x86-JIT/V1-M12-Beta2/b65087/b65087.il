// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x6d8b44b0 {}
.class ILGEN_0x6d8b44b0 {
.method static int32 Method_0xcc3db83d(native int Arg_0x0, int8 Arg_0x1, native int Arg_0x2, unsigned int8 Arg_0x3) {
.maxstack	7
.locals (float32 LOCAL_0x0)
 ldc.r4 float32(0xb2df8aae) 
 stloc LOCAL_0x0
 ldc.i8 0xcf587d6fd35e49db
 ldc.i8 0x269fab8e100e2fe0
 ble Branch_0x2
 ldloc LOCAL_0x0
 br Branch_0x3
Branch_0x2:
 ldloc LOCAL_0x0
Branch_0x3:
 ldc.r4 float32(0x28ee9f3e)
 bne.un Branch_0x0
 ldc.i4.3
 br Branch_0x1
Branch_0x0:
 ldc.i8 0x6e370a896c79b11a
 conv.ovf.u1
 Branch_0x1:
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
 ldc.i4 0x5b4bdf2d conv.i
 ldc.i4 0x62df0adc 
 ldc.i4 0xc56a183a conv.i
 ldc.i4 0xf7a3b39e 
 call int32 ILGEN_0x6d8b44b0::Method_0xcc3db83d(native int Arg_0x0, int8 Arg_0x1, native int Arg_0x2, unsigned int8 Arg_0x3)
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