// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b49984' {}
.assembly extern xunit.core {}
.class ILGEN_0x693bdfcb {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
 .try
  {
   ldc.i4 65
   leave TRY_0x0
  }
 catch ['mscorlib']System.Exception
  {
    leave TRY_0x0
  }
 TRY_0x0:

 ldc.i4 100
 ret
}
}
