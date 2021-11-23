// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xbf04a0ad {}
.assembly extern xunit.core {}
.class ILGEN_0xbf04a0ad {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.locals (int16 local_0x6)
.try {
 ldc.i4 0x9eab1f64 
 stloc local_0x6
 ldloc local_0x6
 ldc.i4.0
 ldc.i4 0x97f26688 
 add.ovf
 mul.ovf.un

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100 ret
}
}
