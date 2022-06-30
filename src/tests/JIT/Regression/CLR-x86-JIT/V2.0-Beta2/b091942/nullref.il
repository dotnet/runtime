// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ASURT     149071
// VSWHIDBEY 91942


.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'nullref'
{
}
.assembly extern xunit.core {}

.class public explicit ansi beforefieldinit BigClass
{
  .field [0] public int32 firstField
  .field [0x7FFFFF9] public int32 lastField

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}

.class public explicit ansi sealed value beforefieldinit BigStruct
{
  .field [0] public int32 firstField
  .field [0x100] public int32 someField
//  .field [0x31C1FFF] public int32 lastField
  .field [0x31C2000] public int32 lastField
}

.class public auto ansi beforefieldinit TestClass
       extends [mscorlib]System.Object
{
// Global field to store the big class
.field static class BigClass s_bigClass

// Global field to store the big struct
.field static valuetype BigStruct s_bigStruct

  .method public hidebysig static int32 Main(string [] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (int32 retValue)

	ldarg.0
	call       bool TestClass::Test1(string [] args)
	brfalse.s  FAILED

	ldarg.0
	call       bool TestClass::Test2(string [] args)
	brfalse.s  FAILED

	ldarg.0
	call       bool TestClass::Test3(string [] args)
	brfalse.s  FAILED

PASSED:

	ldstr      "Test PASSED. Exit Code=100"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.s   100
	stloc.0
	br.s       RET

FAILED:
        ldc.i4.s   101
	stloc.0

RET:
	ldloc.0
	ret
  } // end of method TestClass::Main

  .method public hidebysig static void RunTest1(string [] args) cil managed
  {
	.locals init (int32 val)
// adding some conditional code so the JIT can't optimize it

	ldarg.0
	ldlen
	conv.i4
	ldc.i4.s   100
	beq.s      FALSE

STNULL:	ldnull
	stsfld     class BigClass TestClass::s_bigClass
	br.s       NEXT

FALSE:	newobj     instance void BigClass::.ctor()
	stsfld     class BigClass TestClass::s_bigClass

NEXT:	ldsfld     class BigClass TestClass::s_bigClass
	ldfld      int32 BigClass::lastField
	stloc.0

    IL_0000:  ret
  }

  .method public hidebysig static bool Test1(string [] args) cil managed
  {
    .locals init (bool retValue)
	ldstr   "Running Test1: Using a class with a big offset field"
	call    void [System.Console]System.Console::WriteLine(string)
	.try
	{
		ldarg.0
		call       void TestClass::RunTest1(string [] args)
		leave.s    FAILED
	} catch [mscorlib] System.NullReferenceException
	{
		pop
		ldstr      "Test1 PASSED: expected NullReferenceException"
		call       void [System.Console]System.Console::WriteLine(string)
		ldc.i4.s   1
		stloc.0
		leave.s    RET
	}

FAILED:
	ldstr      "Test1 FAILED: NullReferenceException has not been thrown"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.s   0
	stloc.0

RET:
	ldloc.0
	ret
  }

  .method public hidebysig static void RunTest2(string [] args) cil managed
  {
	.locals init (valuetype BigStruct &pBigStruct, int32 val)
// adding some conditional code so the JIT can't optimize it

	ldarg.0
	ldlen
	conv.i4
	ldc.i4.s   100
	beq.s      FALSE

	br.s       NEXT

FALSE:	ldsflda    valuetype BigStruct TestClass::s_bigStruct
	stloc.0

NEXT:	ldloc.0
	ldfld      int32 BigStruct::someField

	stloc.1

	ldstr      "Value: {0}"
	ldloc.1
	box        [mscorlib]System.Int32
	call       void [System.Console]System.Console::WriteLine(string, object)

    IL_0000:  ret
  }

  .method public hidebysig static bool Test2(string [] args) cil managed
  {
    .locals init (bool retValue)
	ldstr   "Running Test2: Using a valuetype with a not so big offset field"
	call    void [System.Console]System.Console::WriteLine(string)
	.try
	{
		ldarg.0
		call       void TestClass::RunTest2(string [] args)
		leave.s    FAILED
	} catch [mscorlib] System.NullReferenceException
	{
		pop
		ldstr      "Test2 PASSED: expected NullReferenceException"
		call       void [System.Console]System.Console::WriteLine(string)
		ldc.i4.s   1
		stloc.0
		leave.s    RET
	}

FAILED:
	ldstr      "Test2 FAILED: NullReferenceException has not been thrown"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.s   0
	stloc.0

RET:
	ldloc.0
	ret
  }

  .method public hidebysig static void RunTest3(string [] args) cil managed
  {
	.locals init (valuetype BigStruct &pBigStruct, int32 prevVal, int32 newVal)
// adding some conditional code so the JIT can't optimize it

	ldarg.0
	ldlen
	conv.i4
	ldc.i4.s   100
	beq.s      FALSE

	br.s       NEXT

FALSE:	ldsflda    valuetype BigStruct TestClass::s_bigStruct
	stloc.0

NEXT:	ldloc.0
	ldfld      int32 BigStruct::lastField
	stloc.1

// store the new value to the memory location
	ldloc.0
	ldc.i4.s   11
	stfld      int32 BigStruct::lastField

	ldloc.0
	ldfld      int32 BigStruct::lastField
	stloc.1

	ldstr      "Previous value: {0} - new value: {1}"
	ldloc.2
	box        [mscorlib]System.Int32
	ldloc.1
	box        [mscorlib]System.Int32
	call       void [System.Console]System.Console::WriteLine(string, object, object)

    IL_0000:  ret
  }

  .method public hidebysig static bool Test3(string [] args) cil managed
  {
    .locals init (bool retValue)
	ldstr   "Running Test3: Using a valuetype with a big offset field"
	call    void [System.Console]System.Console::WriteLine(string)
	.try
	{
		ldarg.0
		call       void TestClass::RunTest3(string [] args)
		leave.s    FAILED
	} catch [mscorlib] System.NullReferenceException
	{
		pop
		ldstr      "Test3 PASSED: expected NullReferenceException"
		call       void [System.Console]System.Console::WriteLine(string)
		ldc.i4.s   1
		stloc.0
		leave.s    RET
	}

FAILED:
	ldstr      "Test3 FAILED: NullReferenceException has not been thrown"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.s   0
	stloc.0

RET:
	ldloc.0
	ret
  }
}
