// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly ILGEN_0x5c19e96e {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}
.class ILGEN_0x5c19e96e {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.try
{
call int32 ILGEN_0x5c19e96e::f()
leave FAIL
}
catch [mscorlib]System.OverflowException
{
leave PASS
}
FAIL:
ldc.i4 1
ret
PASS:
ldc.i4 100
ret
}
.method static int32 f() {
.maxstack	7

   ldc.r4 3.2
  conv.r8
   ldc.r4 3.1
  conv.r8
 bge Branch_0x2				
   ldc.r4 2.1 
   br Branch_0x3
  Branch_0x2:
   ldc.r4 2.2
  Branch_0x3:


   ldc.i4.0
   ldc.i4 1
   ldc.i4 2
  sub.ovf.un
 beq Branch_0x6
   ldc.r4 1.2 
   br Branch_0x7
  Branch_0x6:
   ldc.r4 1.1
  Branch_0x7:

 beq Branch_0x0
   ldc.i4 55
   br Branch_0x1
  Branch_0x0:
   ldc.i4.0
  Branch_0x1:
 ret
}
}
