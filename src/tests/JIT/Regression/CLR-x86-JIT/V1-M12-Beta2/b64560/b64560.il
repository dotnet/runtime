// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b64560' {}
.assembly extern xunit.core {}
.class public test1
{
  .method public  static void f1() 
  {
    .maxstack  1
    .try
    {
      leave.s    IL_0005
    }
    catch [mscorlib]System.Object 
    {
      leave.s    IL_0005
    }
    IL_0005: ret
  } 
  .method public  static int32 Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  20
    IL_0002:  call       void test1::f1()
    		ldc.i4 100
    IL_009b:  ret
  }
}



