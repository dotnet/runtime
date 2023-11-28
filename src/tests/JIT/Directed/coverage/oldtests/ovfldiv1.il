// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib {}
.assembly ASSEMBLY_NAME {}
.class public auto ansi beforefieldinit ovfldiv1
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  10

    ldc.i8     0x8000000000000000
    conv.u8
    ldc.i4 1
    conv.i8
    div
    dup
    call void [System.Console]System.Console::WriteLine(int64)
    ldc.i8     0x8000000000000000
    ceq
    brfalse FAIL
    
    ldc.i8     0xFFFFFFFFFFFFFFFF
    conv.u8
    ldc.i4.m1
    conv.i8
    div
    dup
    call void [System.Console]System.Console::WriteLine(int64)
    ldc.i4.1
    conv.i8
    ceq
    brfalse FAIL

    ldc.i8     0x7999999999999999
    conv.u8
    ldc.i4.m1
    conv.i8
    div
    dup
    call void [System.Console]System.Console::WriteLine(int64)
    ldc.i8     0x8666666666666667
    conv.u8
    ceq
    brfalse FAIL
    
    .try
    {
    ldc.i8     0x8000000000000000
    conv.u8
    ldc.i4.m1
    conv.i8
    div
    leave FAIL
    }
    catch [mscorlib]System.OverflowException
    {
    	leave PASS
    }
PASS:
    	ldstr "PASSED"
    	call void [System.Console]System.Console::WriteLine(string)
    	ldc.i4 100
    	ret
FAIL:
    	ldstr "FAILED"
    	call void [System.Console]System.Console::WriteLine(string)
    	ldc.i4 1
    	ret
  }

}
