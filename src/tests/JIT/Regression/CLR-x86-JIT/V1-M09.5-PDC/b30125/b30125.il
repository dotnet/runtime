// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x5a59b03c {}
.class ILGEN_0x5a59b03c {
.method static int32 Method_0x1001() {
.maxstack	3

 .locals (unsigned int16 local_0x3)
 ldc.i4 0x74464a0b 
 stloc local_0x3
 ldloc local_0x3
 conv.ovf.u2
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	2
 call int32 ILGEN_0x5a59b03c::Method_0x1001()
 ldc.i4 18955
 sub
ldc.i4 100
add
ret
}

}
