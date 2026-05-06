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
.assembly ASSEMBLY_NAME
{ }
.assembly extern xunit.core {}
.class public auto ansi Test_convovf_i8_u extends [mscorlib]System.Object
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
		ldc.i8 0x00000000000000fd
		conv.ovf.u
		ldc.i4 0x000000fd
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
		ldc.i8 0x00000000000000a6
		conv.ovf.u
		ldc.i4 0x000000a6
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
		ldc.i8 0x000000000000004c
		conv.ovf.u
		ldc.i4 0x0000004c
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
		ldc.i8 0x0000000000000069
		conv.ovf.u
		ldc.i4 0x00000069
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
		ldc.i8 0x0000000000006917
		conv.ovf.u
		ldc.i4 0x00006917
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
		ldc.i8 0x0000000000003910
		conv.ovf.u
		ldc.i4 0x00003910
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
		ldc.i8 0x0000000000006d94
		conv.ovf.u
		ldc.i4 0x00006d94
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
		ldc.i8 0x000000000000b8d4
		conv.ovf.u
		ldc.i4 0x0000b8d4
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
		ldc.i8 0x00000000002709f1
		conv.ovf.u
		ldc.i4 0x002709f1
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
		ldc.i8 0x0000000000bd498d
		conv.ovf.u
		ldc.i4 0x00bd498d
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
		ldc.i8 0x000000000026e414
		conv.ovf.u
		ldc.i4 0x0026e414
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
		ldc.i8 0x0000000000149abc
		conv.ovf.u
		ldc.i4 0x00149abc
		beq OK_12
		leave FAIL
	OK_12:
		leave begin_13
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_13:
	.try {
		ldstr "test # 13"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x000000007f5ca797
		conv.ovf.u
		ldc.i4 0x7f5ca797
		beq OK_13
		leave FAIL
	OK_13:
		leave begin_14
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_14:
	.try {
		ldstr "test # 14"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00000000ba989c83
		conv.ovf.u
		ldc.i4 0xba989c83
		conv.u
		beq OK_14
		leave FAIL
	OK_14:
		leave begin_15
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_15:
	.try {
		ldstr "test # 15"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00000000ef5d6f3c
		conv.ovf.u
		ldc.i4 0xef5d6f3c
		conv.u
		beq OK_15
		leave FAIL
	OK_15:
		leave begin_16
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_16:
	.try {
		ldstr "test # 16"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x000000007ff93757
		conv.ovf.u
		ldc.i4 0x7ff93757
		conv.u
		beq OK_16
		leave FAIL
	OK_16:
		leave begin_17
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_17:
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
