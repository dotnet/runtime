// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly 'badcallee'
{
}


.class public auto ansi beforefieldinit P
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32
          A1() cil managed aggressiveinlining
  {
    // Code size       55 (0x37)
    .maxstack  2
    ldc.i4.0
    br.s   100
    ret
  } // end of method P::A1

  .method public hidebysig static int32  Test(string[] x) cil managed noinlining
  {
    ldarg.0
    ldlen
    dup
    xor
    ldc.i4.0
    ceq
    brtrue.s   OK
    call   int32 P::A1()
    pop
    OK: ldc.i4.s  100 
    ret
  } // end of method P::Main

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    ldc.i4.0
    newarr     [mscorlib]System.String
    call int32 P::Test(string[] x)
    ret
  }

} // end of class P
