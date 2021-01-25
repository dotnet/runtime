// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x38e3b289 {}
.class ILGEN_0x38e3b289 {

.field static	unsigned int16 field_0x1
.method static int32 main() {
.entrypoint
.maxstack	10

 ldc.i4 0x20e41ee1 
 stsfld unsigned int16 ILGEN_0x38e3b289::field_0x1
 
 ldsfld unsigned int16 ILGEN_0x38e3b289::field_0x1
 conv.ovf.u.un
 ldc.i4 7905
 sub
ldc.i4 100
add
ret
}
}
