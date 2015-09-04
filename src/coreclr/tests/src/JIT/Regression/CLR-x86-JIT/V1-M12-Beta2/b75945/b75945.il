// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xb12e836e {}
.class ILGEN_0xb12e836e {
.field static	int8 field_0x1
.method static unsigned int16  Method_0x1be573ce() {
.maxstack	13
 nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop
 nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop nop
 ldc.i4 0x588 
 conv.i1
 stsfld int8 ILGEN_0xb12e836e::field_0x1
 ldsfld int8 ILGEN_0xb12e836e::field_0x1
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	9
 call unsigned int16  ILGEN_0xb12e836e::Method_0x1be573ce()
 conv.i4
 pop
 ldc.i4.s 100
 ret
}
}