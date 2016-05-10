// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xaca96c86 {}
.class ILGEN_0xaca96c86 {
.method static float64  Method_0x88567f92(native int Arg_0x0, float32 Arg_0x1) {
.maxstack	3
 .locals (int32 LOCAL_0x7)
ldc.i4 0x6da83b8f 
stloc LOCAL_0x7
 ldloc LOCAL_0x7
conv.r4
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	5
ldc.i4 0x11e97b18 conv.i
ldc.r4 float32(0x6f0a635d) 
 call float64  ILGEN_0xaca96c86::Method_0x88567f92(native int Arg_0x0, float32 Arg_0x1)
 conv.i4
 ldc.i4 1839741724
 sub
 ret

}

}
