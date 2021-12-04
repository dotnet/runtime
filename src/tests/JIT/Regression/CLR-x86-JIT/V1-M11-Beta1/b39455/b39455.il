// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x98f62282 {}
.assembly extern xunit.core {}
.class ILGEN_0x98f62282 {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.locals (unsigned int64 local_0x0)

.try {
 ldc.i8 0x558  
 stloc local_0x0

Start_Orphan_0:
 ldloc local_0x0
 conv.ovf.u1
 ldloc local_0x0
 conv.u
 rem
 pop
End_Orphan_0:

ldc.i4 100

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100 ret
}

}
