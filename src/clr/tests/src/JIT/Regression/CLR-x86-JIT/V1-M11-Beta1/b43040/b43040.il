// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xd8633b24 {}
.class ILGEN_0xd8633b24 {
.field static	unsigned int8[] field_0x5
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
 ldsfld unsigned int8[] ILGEN_0xd8633b24::field_0x5
ldc.i4 55
ldelem.u1

  pop
  leave out_of_here
} catch [mscorlib]System.NullReferenceException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100 ret
}

}
