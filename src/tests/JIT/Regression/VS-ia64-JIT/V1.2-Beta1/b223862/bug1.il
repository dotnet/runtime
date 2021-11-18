// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly ILGEN_0xc766623d {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}
.class ILGEN_0xc766623d {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	5
    ldc.i4.4
     ldc.i4.0
     ldc.i4.2
    sub
   rem.un
   ldc.i4.6
  rem
 pop
  ldc.i4 100
 ret
}
}
