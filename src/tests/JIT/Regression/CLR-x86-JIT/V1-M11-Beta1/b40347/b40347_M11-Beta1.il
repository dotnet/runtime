// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b40347_M11-Beta1'
{
  
}
.class public C
{
  .method public static int32 main() 
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  10

    .locals(int8[] x)

    ldc.i4 20
    newarr int8
    stloc x
 
    ldloc x
    ldc.i4 0
    ldc.i4 -20
    stelem.i1

    ldloc x
    ldc.i4 0
    ldelem.i1
    call void [System.Console]System.Console::WriteLine(int32)

    ldloc x
    ldc.i4 0
    ldelema int8
    ldind.i1
    call void [System.Console]System.Console::WriteLine(int32)

    ldloc x
    ldc.i4 0
    ldelema int8
    ldobj int8
    call void [System.Console]System.Console::WriteLine(int32)
    ldc.i4 100
    ret
  }

}      

