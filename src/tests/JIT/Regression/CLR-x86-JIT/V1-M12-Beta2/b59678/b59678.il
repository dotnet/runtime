// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern legacy library mscorlib {}
.assembly convr4a {}
.assembly extern xunit.core {}
.class public bug1 extends [mscorlib]System.Object
{
  .method public static int32 Main() 
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  5
    .locals (  bool V_0,
    		 float32 V_1,
             float32 V_2,
             int32 V_3,
             float32 V_4)

    ldc.r4     1.
    stloc.1
    ldc.r4     1.e-010
    stloc.2
    ldc.i4.3
    stloc.3
    ldc.r4     0.33333334
    stloc.s    V_4

    ldc.r4     1.
    ldc.r4     1.e-010
    add
    conv.r4
    ldc.r4     1.
    ceq
    call       void [System.Console]System.Console::WriteLine(bool)

    ldloc.1
    ldloc.2
    add
    conv.r4
    ldloc.1
    ceq
    call       void [System.Console]System.Console::WriteLine(bool)

    ldc.r4     1.
    ldc.r4     1.e-010
    sub
    conv.r4
    ldc.r4     1.
    ceq
    call       void [System.Console]System.Console::WriteLine(bool)

    ldloc.1
    ldloc.2
    add
    conv.r4
    ldloc.1
    ceq
    call       void [System.Console]System.Console::WriteLine(bool)

    ldc.i4.3
    conv.r4
    ldc.r4     0.33333334
    mul
    conv.r4
    ldc.r4     1.
    ceq
    call       void [System.Console]System.Console::WriteLine(bool)

    ldloc.3
    conv.r4
    ldloc.s    V_4
    mul
    conv.r4
    ldloc.1
    ceq
    call       void [System.Console]System.Console::WriteLine(bool)

    ldc.r4     1.
    ldc.i4.3
    conv.r4
    div
    conv.r4
    ldc.r4     0.33333334
    ceq
    call       void [System.Console]System.Console::WriteLine(bool)

    ldloc.1
    ldloc.3
    conv.r4
    div
    conv.r4
    ldloc.s    V_4
    ceq
    call       void [System.Console]System.Console::WriteLine(bool)

	ldc.i4 100
    ret
  }
}
	
