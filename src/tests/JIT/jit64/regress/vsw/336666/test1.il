// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}








.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:3600:0
}

.assembly 'test1_336666' {}


// We inline the call chain foo()->Throw(). This causes the DFS reader
//  to screw up when trying to maintain the operand stack between inlined
//  methods.



.class public auto ansi test1 {
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    
    ldc.i4 100
    ldstr "PASS"

    // Value is live across this call - entire call chain should be inlined
    call       void foo()


    call       void [System.Console]System.Console::WriteLine(string)
    ret
  }
}



  // This call should be inlined.
  .field public static int32 x
  .method public hidebysig static void  foo() cil managed
  {
    ldsfld     int32 x
    brfalse.s   NO_THROW

    call       void Throw()

    NO_THROW:
    ret
  }



  // This call should be inlined.
  .method public hidebysig static void  Throw() cil managed
  {
    newobj     instance void [mscorlib]System.Exception::.ctor()
    throw
  }
