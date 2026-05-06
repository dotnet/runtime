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
.class public auto ansi Test_convovf_i8_i extends [mscorlib]System.Object
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
		ldc.i8 0x00000000000000a0
		conv.ovf.i
		ldc.i4 0x000000a0
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
		ldc.i8 0x0000000000000027
		conv.ovf.i
		ldc.i4 0x00000027
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
		ldc.i8 0x00000000000000a1
		conv.ovf.i
		ldc.i4 0x000000a1
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
		ldc.i8 0x00000000000000b3
		conv.ovf.i
		ldc.i4 0x000000b3
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
		ldc.i8 0x00000000000029ec
		conv.ovf.i
		ldc.i4 0x000029ec
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
		ldc.i8 0x000000000000bc27
		conv.ovf.i
		ldc.i4 0x0000bc27
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
		ldc.i8 0x0000000000002040
		conv.ovf.i
		ldc.i4 0x00002040
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
		ldc.i8 0x0000000000004fa6
		conv.ovf.i
		ldc.i4 0x00004fa6
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
		ldc.i8 0x0000000000863096
		conv.ovf.i
		ldc.i4 0x00863096
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
		ldc.i8 0x00000000009234c5
		conv.ovf.i
		ldc.i4 0x009234c5
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
		ldc.i8 0x0000000000d351e5
		conv.ovf.i
		ldc.i4 0x00d351e5
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
		ldc.i8 0x000000000032df2f
		conv.ovf.i
		ldc.i4 0x0032df2f
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
		ldc.i8 0x000000002d0d6e01
		conv.ovf.i
		ldc.i4 0x2d0d6e01
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
		ldc.i8 0x000000003b4b4ac4
		conv.ovf.i
		ldc.i4 0x3b4b4ac4
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
		ldc.i8 0x000000007fbfa780
		conv.ovf.i
		ldc.i4 0x7fbfa780
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
		ldc.i8 0x000000007a17e61a
		conv.ovf.i
		ldc.i4 0x7a17e61a
		beq OK_16
		leave FAIL
	OK_16:
		leave begin_17
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_17:
	.try {
		ldstr "test # 17"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
//		ldc.i8 0x0000009e8211b75e
//		conv.ovf.i
//		pop
//		leave FAIL
		leave begin_33
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_18
	}
begin_18:
	.try {
		ldstr "test # 18"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000001a9c70412e
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_19
	}
begin_19:
	.try {
		ldstr "test # 19"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x000000ff92ab9a0f
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_20
	}
begin_20:
	.try {
		ldstr "test # 20"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x000000cb35d01001
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_21
	}
begin_21:
	.try {
		ldstr "test # 21"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000dfe3f33843ae
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_22
	}
begin_22:
	.try {
		ldstr "test # 22"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00007208b8ad3ff6
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_23
	}
begin_23:
	.try {
		ldstr "test # 23"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00002bbdc338cf11
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_24
	}
begin_24:
	.try {
		ldstr "test # 24"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00004bd52cec8f40
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_25
	}
begin_25:
	.try {
		ldstr "test # 25"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x001c0898bce41d8a
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_26
	}
begin_26:
	.try {
		ldstr "test # 26"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00769052acced36a
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_27
	}
begin_27:
	.try {
		ldstr "test # 27"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00200b84c6ebdb4e
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_28
	}
begin_28:
	.try {
		ldstr "test # 28"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00880c4f39fb2110
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_29
	}
begin_29:
	.try {
		ldstr "test # 29"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0xcd18f8b94c4173ea
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_30
	}
begin_30:
	.try {
		ldstr "test # 30"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0xc0311479f3cabd61
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_31
	}
begin_31:
	.try {
		ldstr "test # 31"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0xad12391d3bcf1551
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_32
	}
begin_32:
	.try {
		ldstr "test # 32"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0xc52cc15b10d62455
		conv.ovf.i
		pop
		leave FAIL
	} catch [mscorlib]System.OverflowException {
		pop
		leave begin_33
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
