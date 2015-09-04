// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x24fde2d6 {}
.class ILGEN_0x24fde2d6 {
.field static unsigned int8 field_0x2

.method static int32 Main() {
.entrypoint
.maxstack	20

 ldc.i4 1
 stsfld unsigned int8 ILGEN_0x24fde2d6::field_0x2
 ldsfld unsigned int8 ILGEN_0x24fde2d6::field_0x2
 ldc.i4 -1 
 bgt Branch_0x4
 ldc.i4 4
 br Branch_0x5
Branch_0x4:
 ldc.i4 5
Branch_0x5:
 ldc.i4 95
 add
 ret
}
}