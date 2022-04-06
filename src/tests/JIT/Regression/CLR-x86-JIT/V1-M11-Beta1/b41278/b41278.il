// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b41278' {}
.assembly extern xunit.core {}
.class ILGEN_0x233972d {
.field static	unsigned int8 field_0x2
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

 ldc.i4 0xc0ad8a3b 
 stsfld unsigned int8 ILGEN_0x233972d::field_0x2

 ldsfld unsigned int8 ILGEN_0x233972d::field_0x2
 brtrue Branch_0x0
 ldc.i8 0x99  
 br Branch_0x1
Branch_0x0:
 ldc.i8 0x9b6911c3340352e8
Branch_0x1:
 ldc.i4.3
 shr.un
 ldc.i4 0xfe   
 ldc.i4 0xFF 
 ble Branch_0x2
 ldc.i4.4
 br Branch_0x3
Branch_0x2:
 ldc.i4 0x35e531f6
Branch_0x3:
 conv.ovf.i8
 clt
ldc.i4 100
add
 ret
}
}
