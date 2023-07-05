// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly 'b48850' {}
 
.class public auto ansi Test_b48850 extends [mscorlib] System.Object {
.method public static int32 Main()
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
    .maxstack  3
    .locals init (int32 V_0)

    ldc.i4.s   101
    stloc.0

    .try 
    {
	    call void TestClass::RuntestCase()
        leave.s Return
    }
    catch [mscorlib]System.NullReferenceException
    {
        pop
        ldc.i4.s   100
        stloc.0
        leave.s    Return
    }
    
Return:
  IL_001f:  ldstr      "!!!!!!!! TEST {0} !!!!!!!!!!"
  IL_0024:  ldloc.0
  IL_0025:  ldc.i4.s   100
  IL_0027:  beq.s      IL_0030
  IL_0029:  ldstr      "FAILED"
  IL_002e:  br.s       IL_0035
  IL_0030:  ldstr      "PASSED"
  IL_0035:  call       void [System.Console]System.Console::WriteLine(string,
                                                                object)
  IL_003f:  ldloc.0
  IL_0040:  ret
}
}

.class public value BigStructtestCase
{
	.field public int32 bigOffsetField
}

.class public TestClass
{
  .method public static void RuntestCase()
  {
	.locals init (valuetype BigStructtestCase &p1)
	ldloc.0	
	ldfld int32 BigStructtestCase::bigOffsetField
	pop
	ret
  }
}
