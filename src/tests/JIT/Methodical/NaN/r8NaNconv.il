// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
}
.assembly 'r8NaNconv' {}
.assembly extern xunit.core {}
.namespace Test_r8NaNconv
{
.class public auto ansi beforefieldinit ConvTest
       extends [mscorlib]System.Object
{
	.method public hidebysig static int8 test1() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 1 (conv.i1) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.i1
	  dup
	  call       void [System.Console]System.Console::WriteLine(int32)
	  ret
	}

	.method public hidebysig static int16 test2() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 2 (conv.i2) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.i2
	  dup
	  call       void [System.Console]System.Console::WriteLine(int32)
	  ret
	}

	.method public hidebysig static int32 test3() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 3 (conv.i4) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.i4
	  dup
	  call       void [System.Console]System.Console::WriteLine(int32)
	  ret
	}

	.method public hidebysig static int64 test4() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 4 (conv.i8) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.i8
	  dup
	  call       void [System.Console]System.Console::WriteLine(int64)
	  ret
	}

	.method public hidebysig static float32 test5() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 5 (conv.r4) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.r4
	  dup
	  call       void [System.Console]System.Console::WriteLine(float64)
	  ret
	}

	.method public hidebysig static float64 test6() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 6 (conv.r8) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.r8
	  dup
	  call       void [System.Console]System.Console::WriteLine(float64)
	  ret
	}

	.method public hidebysig static unsigned int8 test7() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 7 (conv.u1) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.u1
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static unsigned int16 test8() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 8 (conv.u2) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.u2
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static unsigned int32 test9() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 9 (conv.u4) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.u4
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static unsigned int64 test10() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 10 (conv.u8) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.u8
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int64)
	  ret
	}

	.method public hidebysig static int32 test11() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 11 (conv.i) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.i
	  dup
	  call       void [System.Console]System.Console::WriteLine(int32)
	  ret
	}

	.method public hidebysig static unsigned int32 test12() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 12 (conv.u) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.u
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static float32 test13() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 13 (conv.r.un) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.r.un
	  dup
	  call       void [System.Console]System.Console::WriteLine(float32)
	  ret
	}

	.method public hidebysig static int8 test14() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 14 (conv.ovf.i1) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.i1
	  dup
	  call       void [System.Console]System.Console::WriteLine(int32)
	  ret
	}

	.method public hidebysig static int16 test15() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 15 (conv.ovf.i2) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.i2
	  dup
	  call       void [System.Console]System.Console::WriteLine(int32)
	  ret
	}

	.method public hidebysig static int32 test16() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 16 (conv.ovf.i4) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.i4
	  dup
	  call       void [System.Console]System.Console::WriteLine(int32)
	  ret
	}

	.method public hidebysig static int64 test17() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 17 (conv.ovf.i8) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.i8
	  dup
	  call       void [System.Console]System.Console::WriteLine(int64)
	  ret
	}

	.method public hidebysig static unsigned int8 test18() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 18 (conv.ovf.u1) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.u1
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static unsigned int16 test19() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 19 (conv.ovf.u2) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.u2
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static unsigned int32 test20() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 20 (conv.ovf.u4) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.u4
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static unsigned int64 test21() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 21 (conv.ovf.u8) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.u8
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int64)
	  ret
	}

	.method public hidebysig static int32 test22() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 22 (conv.ovf.i) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.i
	  dup
	  call       void [System.Console]System.Console::WriteLine(int32)
	  ret
	}

	.method public hidebysig static unsigned int32 test23() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 23 (conv.ovf.u) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.u
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static int8 test24() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 24 (conv.ovf.i1.un) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.i1.un
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static int16 test25() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 25 (conv.ovf.i2.un) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.i2.un
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static int32 test26() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 26 (conv.ovf.i4.un) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.i4.un
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static int64 test27() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 27 (conv.ovf.i8.un) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.i8.un
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int64)
	  ret
	}

	.method public hidebysig static unsigned int8 test28() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 28 (conv.ovf.u1.un) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.u1.un
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static unsigned int16 test29() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 29 (conv.ovf.u2.un) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.u2.un
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static unsigned int32 test30() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 30 (conv.ovf.u4.un) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.u4.un
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static unsigned int64 test31() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 31 (conv.ovf.u8.un) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.u8.un
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int64)
	  ret
	}

	.method public hidebysig static int32 test32() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 32 (conv.ovf.i.un) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.i.un
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}

	.method public hidebysig static unsigned int32 test33() cil managed 
	{
	  .maxstack 10
	  ldstr "testcase 33 (conv.ovf.u.un) : "
	  call       void [System.Console]System.Console::Write(string)
	  ldc.r8     (00 00 00 00 00 00 F8 FF)
	  conv.ovf.u.un
	  dup
	  call       void [System.Console]System.Console::WriteLine(unsigned int32)
	  ret
	}		
  .method public hidebysig static int32  Main() cil managed 
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  50
    .locals init ([0] bool passed )

    ldstr "Based on bug #91021 - testing Double.NaN"
    call void [System.Console]System.Console::WriteLine(string)
	ldstr "The conv.* instructions never throw overflow exceptions.  The results of the conversions is undefined for NaN."
	call void [System.Console]System.Console::WriteLine(string)
	ldstr "The conv.ovf.* instructions should throw OverflowException if the result can not be represented in the result type"
	call void [System.Console]System.Console::WriteLine(string)
	ldstr ""
	call void [System.Console]System.Console::WriteLine(string)

    ldc.i4.1
    stloc.0
    
	call       int8 Test_r8NaNconv.ConvTest::test1()
	pop
	call       int16 Test_r8NaNconv.ConvTest::test2()
	pop
	call       int32 Test_r8NaNconv.ConvTest::test3()
	pop
	call       int64 Test_r8NaNconv.ConvTest::test4()
	pop
	call       float32 Test_r8NaNconv.ConvTest::test5()
	pop
	call       float64 Test_r8NaNconv.ConvTest::test6()
	pop
	call       unsigned int8 Test_r8NaNconv.ConvTest::test7()
	pop
	call       unsigned int16 Test_r8NaNconv.ConvTest::test8()
	pop
	call       unsigned int32 Test_r8NaNconv.ConvTest::test9()
	pop
	call       unsigned int64 Test_r8NaNconv.ConvTest::test10()
	pop
	call       int32 Test_r8NaNconv.ConvTest::test11()
	pop
	call       unsigned int32 Test_r8NaNconv.ConvTest::test12()
	pop
	call       float32 Test_r8NaNconv.ConvTest::test13()
	pop


	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       int8 Test_r8NaNconv.ConvTest::test14()
	    leave.s    Test_15
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_15
	  }  
	  Test_15: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       int16 Test_r8NaNconv.ConvTest::test15()
	    leave.s    Test_16
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_16
	  }  
	  Test_16: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       int32 Test_r8NaNconv.ConvTest::test16()
	    leave.s    Test_17
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_17
	  }  
	  Test_17: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       int64 Test_r8NaNconv.ConvTest::test17()
	    leave.s    Test_18
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_18
	  }  
	  Test_18: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       unsigned int8 Test_r8NaNconv.ConvTest::test18()
	    leave.s    Test_19
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_19
	  }  
	  Test_19: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       unsigned int16 Test_r8NaNconv.ConvTest::test19()
	    leave.s    Test_20
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_20
	  }  
	  Test_20: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       unsigned int32 Test_r8NaNconv.ConvTest::test20()
	    leave.s    Test_21
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_21
	  }  
	  Test_21: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       unsigned int64 Test_r8NaNconv.ConvTest::test21()
	    leave.s    Test_22
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_22
	  }  
	  Test_22: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       int32 Test_r8NaNconv.ConvTest::test22()
	    leave.s    Test_23
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_23
	  }  
	  Test_23: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       unsigned int32 Test_r8NaNconv.ConvTest::test23()
	    leave.s    Test_24
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_24
	  }  
	  Test_24: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       int8 Test_r8NaNconv.ConvTest::test24()
	    leave.s    Test_25
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_25
	  }  
	  Test_25: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       int16 Test_r8NaNconv.ConvTest::test25()
	    leave.s    Test_26
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_26
	  }  
	  Test_26: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       int32 Test_r8NaNconv.ConvTest::test26()
	    leave.s    Test_27
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_27
	  }  
	  Test_27: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       int64 Test_r8NaNconv.ConvTest::test27()
	    leave.s    Test_28
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_28
	  }  
	  Test_28: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       unsigned int8 Test_r8NaNconv.ConvTest::test28()
	    leave.s    Test_29
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_29
	  }  
	  Test_29: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       unsigned int16 Test_r8NaNconv.ConvTest::test29()
	    leave.s    Test_30
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_30
	  }  
	  Test_30: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       unsigned int32 Test_r8NaNconv.ConvTest::test30()
	    leave.s    Test_31
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_31
	  }  
	  Test_31: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       unsigned int64 Test_r8NaNconv.ConvTest::test31()
	    leave.s    Test_32
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	   pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_32
	  }  
	  Test_32: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       int32 Test_r8NaNconv.ConvTest::test32()
	    leave.s    Test_33
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_33
	  }  
	  Test_33: nop
	  .try
	  {
	    ldc.i4.0
	    stloc.0
	    call       unsigned int32 Test_r8NaNconv.ConvTest::test33()
	    leave.s    Test_34
	  }  
	  catch [mscorlib]System.OverflowException 
	  {
	    pop
	    ldc.i4.1
	    stloc.0
	    ldstr "OverflowException caught as expected"
	    call       void [System.Console]System.Console::WriteLine(string)
	    leave.s    Test_34
	  }  
	  Test_34: nop

	   ldstr ""
	   call void [System.Console]System.Console::WriteLine(string)
	   ldloc.0
	   brfalse.s  IL_0022

	IL_0013:  ldstr      "PASSED"
    IL_0018:  call       void [System.Console]System.Console::WriteLine(string)
    IL_001d:  ldc.i4.s   100
    IL_0020:  br.s       IL_0030

    IL_0022:  ldstr      "FAILED"
    IL_0027:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002c:  ldc.i4.m1
    IL_002e:  br.s       IL_0030

    IL_0030:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed 
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 
}
