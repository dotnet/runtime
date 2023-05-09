// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ovfldiv2 {}
.class public auto ansi beforefieldinit ovfldiv2
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  10
    
    ldc.i4     0x80000000
    conv.u4
    ldc.i4 1
    div
    dup
    call void [System.Console]System.Console::WriteLine(int32)
    //ldc.i4 0xFFFFFFFF80000000
    ldc.i4 0x80000000
    ceq
    brfalse FAIL    

    ldc.i4     0xFFFFFFFF
    conv.u4
    ldc.i4 1
    div
    dup
    call void [System.Console]System.Console::WriteLine(int32)
    ldc.i4.m1
    ceq
    brfalse FAIL    

    ldc.i4     0x79999999
    conv.u4
    ldc.i4.m1
    div
    dup
    call void [System.Console]System.Console::WriteLine(int32)
    //ldc.i4 0xFFFFFFFF86666667
    ldc.i4 0x86666667
    ceq
    brfalse FAIL 
    
    .try
    {
    ldc.i4     0x80000000
    conv.u4
    ldc.i4.m1
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
