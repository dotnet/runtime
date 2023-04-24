// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'conv_i8_i'
{ }
.assembly extern xunit.core {}
.class public auto ansi Test_conv_i8_i extends [mscorlib]System.Object
{
.method public hidebysig static int32 Main() il managed
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	// Code size       48 (0x30)
	.maxstack  5
begin_1:
	.try {
		ldstr "test # 1"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000000000000092
		conv.i
		ldc.i4 0x00000092
		beq OK_1
		leave FAIL
	OK_1:
		leave begin_2
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_2:
	.try {
		ldstr "test # 2"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00000000000000e9
		conv.i
		ldc.i4 0x000000e9
		beq OK_2
		leave FAIL
	OK_2:
		leave begin_3
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_3:
	.try {
		ldstr "test # 3"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000000000000041
		conv.i
		ldc.i4 0x00000041
		beq OK_3
		leave FAIL
	OK_3:
		leave begin_4
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_4:
	.try {
		ldstr "test # 4"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000000000000014
		conv.i
		ldc.i4 0x00000014
		beq OK_4
		leave FAIL
	OK_4:
		leave begin_5
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_5:
	.try {
		ldstr "test # 5"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000000000006b37
		conv.i
		ldc.i4 0x00006b37
		beq OK_5
		leave FAIL
	OK_5:
		leave begin_6
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_6:
	.try {
		ldstr "test # 6"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00000000000053a8
		conv.i
		ldc.i4 0x000053a8
		beq OK_6
		leave FAIL
	OK_6:
		leave begin_7
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_7:
	.try {
		ldstr "test # 7"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000000000007561
		conv.i
		ldc.i4 0x00007561
		beq OK_7
		leave FAIL
	OK_7:
		leave begin_8
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_8:
	.try {
		ldstr "test # 8"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000000000000e30
		conv.i
		ldc.i4 0x00000e30
		beq OK_8
		leave FAIL
	OK_8:
		leave begin_9
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_9:
	.try {
		ldstr "test # 9"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00000000007ba976
		conv.i
		ldc.i4 0x007ba976
		beq OK_9
		leave FAIL
	OK_9:
		leave begin_10
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_10:
	.try {
		ldstr "test # 10"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00000000007540b0
		conv.i
		ldc.i4 0x007540b0
		beq OK_10
		leave FAIL
	OK_10:
		leave begin_11
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_11:
	.try {
		ldstr "test # 11"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x000000000089a7d6
		conv.i
		ldc.i4 0x0089a7d6
		beq OK_11
		leave FAIL
	OK_11:
		leave begin_12
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_12:
	.try {
		ldstr "test # 12"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000000000c66850
		conv.i
		ldc.i4 0x00c66850
		beq OK_12
		leave FAIL
	OK_12:
		leave begin_33
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_33:

	ldc.i4 0x64
	ldstr "passed"
	br PRINT
FAIL:
	ldc.i4.1
	ldstr "failed"
PRINT:
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ret
} // end of method 'Test::Main'

.method public hidebysig specialname rtspecialname 
        instance void .ctor() il managed
{
  // Code size       7 (0x7)
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
  IL_0006:  ret
} // end of method 'Test::.ctor'

} // end of class 'Test'
