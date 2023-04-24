// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly convX {}
.assembly extern xunit.core {}
.class public auto ansi beforefieldinit convX
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  3

    // i4 - conv.i1 - truncate
IL_CASE0:  	ldc.i4     0x1234abcd
    conv.i1
    dup
    call       void [System.Console]System.Console::WriteLine(int32)
    ldc.i4     0xffffffcd
    beq.s      IL_CASE1
	ldstr "CASE0 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

    // i4 - conv.u1 - truncate
IL_CASE1:  ldc.i4     0x1234abcd
    conv.u1
	dup
	call       void [System.Console]System.Console::WriteLine(int32)
    ldc.i4     0xcd
    beq.s      IL_CASE2
    ldstr "CASE1 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

    // i8 - conv.i2 - truncate
IL_CASE2:  ldc.i8     0x1234abcd
    conv.i2
    dup
    call       void [System.Console]System.Console::WriteLine(int32)
    ldc.i4     0xffffabcd
    beq.s      IL_CASE3
	ldstr "CASE2 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

    // native int - conv.u2 - truncate
IL_CASE3:  ldc.i4     0x1234abcd
    conv.i
    conv.u2
	dup
	call       void [System.Console]System.Console::WriteLine(int32)
    ldc.i4     0xabcd
    beq.s      IL_CASE4
    ldstr "CASE3 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

	// i8 - conv.i4 - truncate
IL_CASE4:  ldc.i8     0x8234abcd
    conv.i4
    dup
    call       void [System.Console]System.Console::WriteLine(int32)
    ldc.i4     0x8234abcd
    beq.s      IL_CASE5
	ldstr "CASE4 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

	// native int - conv.u4 - truncate
IL_CASE5:  ldc.i8     0x8234abcd
	conv.i
    conv.u4
    dup
    call       void [System.Console]System.Console::WriteLine(unsigned int32)
    ldc.i4     0x8234abcd
    beq.s      IL_CASE6
	ldstr "CASE5 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

	// i4 - conv.i8 - sign extend
IL_CASE6:  ldc.i4     0xedcb5433
    conv.i8
	dup
	call       void [System.Console]System.Console::WriteLine(int64)
    ldc.i8	0xffffffffedcb5433
    beq.s      IL_CASE7
    ldstr "CASE6 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

	// i4 - conv.u8 - zero extend
IL_CASE7:  ldc.i4     0x1234abcd
    conv.u8
	dup
	call       void [System.Console]System.Console::WriteLine(unsigned int64)
    ldc.i8	0x000000001234abcd
    beq.s      IL_CASE8
    ldstr "CASE7 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

	// F - conv.i1 - truncate to zero
IL_CASE8:  ldc.r4	1.1
    conv.i1
	dup
	call       void [System.Console]System.Console::WriteLine(int32)
    ldc.i4	1
    beq.s      IL_CASE9
    ldstr "CASE8 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

	// F - conv.i1 - truncate to zero
IL_CASE9:  ldc.r4	-1.1
    conv.i1
	dup
	call       void [System.Console]System.Console::WriteLine(int32)
    ldc.i4	-1
    beq.s      IL_CASE10
    ldstr "CASE9 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

	// r8 - conv.r4 - OVERFLOW to Single.PositiveInfinity
IL_CASE10:  ldc.r8     5.1234567890000001e+054
	conv.r4
	dup
	call       void [System.Console]System.Console::WriteLine(float32)
    ldc.r4     (00 00 80 7F)
    beq.s      IL_CASE11
    ldstr "CASE10 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

	// r8 - conv.r4 - OVERFLOW to Single.NegativeInfinity
IL_CASE11:  ldc.r8     -1.0000000000000001e+054
	conv.r4
	dup
	call       void [System.Console]System.Console::WriteLine(float32)
    ldc.r4     (00 00 80 FF)
    beq.s      IL_CASE12
    ldstr "CASE11 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

	// r8 - conv.i4 - OVERFLOW to undefined
IL_CASE12:

	// r8 - conv.u4 - truncate to zero (ASURT#125535)
IL_CASE14:  ldc.r8     4294967295
    conv.u4
    dup
    call       void [System.Console]System.Console::WriteLine(unsigned int32)
    ldc.i4	4294967295
    beq.s      IL_CASE15
    ldstr "CASE14 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

	// r8 - conv.u2 - truncate to zero
IL_CASE15:  ldc.r8     65535
    conv.u2
    dup
    call       void [System.Console]System.Console::WriteLine(unsigned int32)
    ldc.i4	65535
    beq.s      IL_CASE17
    ldstr "CASE15 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

	// r8 - conv.u1 - truncate to zero
IL_CASE17:  ldc.r8     255
    conv.u1
    dup
    call       void [System.Console]System.Console::WriteLine(unsigned int32)
    ldc.i4	255
    beq.s      IL_CASE18
    ldstr "CASE17 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

	// r8 - conv.i1 - truncate to zero
IL_CASE18:  ldc.r8     127
    conv.i1
    dup
    call       void [System.Console]System.Console::WriteLine(int32)
    ldc.i4	127
    beq.s      IL_CASE19
    ldstr "CASE18 FAILED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.1
    br       IL_0041

IL_CASE19:
	
IL_PASS:
    ldstr "PASSED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4 100

IL_0041:  ret
  } 
  
}
