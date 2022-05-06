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
.assembly 'conv_i8_u'
{ }
.assembly extern xunit.core {}
.class public auto ansi Test_conv_i8_u extends [mscorlib]System.Object
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
		ldc.i8 0x000000000000003b
		conv.u
		ldc.i4 0x0000003b
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
		ldc.i8 0x0000000000000015
		conv.u
		ldc.i4 0x00000015
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
		ldc.i8 0x00000000000000e7
		conv.u
		ldc.i4 0x000000e7
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
		ldc.i8 0x00000000000000bb
		conv.u
		ldc.i4 0x000000bb
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
		ldc.i8 0x0000000000007615
		conv.u
		ldc.i4 0x00007615
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
		ldc.i8 0x000000000000d132
		conv.u
		ldc.i4 0x0000d132
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
		ldc.i8 0x000000000000642c
		conv.u
		ldc.i4 0x0000642c
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
		ldc.i8 0x0000000000006c14
		conv.u
		ldc.i4 0x00006c14
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
		ldc.i8 0x0000000000f118dc
		conv.u
		ldc.i4 0x00f118dc
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
		ldc.i8 0x000000000004c330
		conv.u
		ldc.i4 0x0004c330
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
		ldc.i8 0x000000000078c77f
		conv.u
		ldc.i4 0x0078c77f
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
		ldc.i8 0x0000000000e020ba
		conv.u
		ldc.i4 0x00e020ba
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
		ldc.i8 0x00000000776f0a7a
		conv.u
		ldc.i4 0x776f0a7a
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
		ldc.i8 0x0000000057186a67
		conv.u
		ldc.i4 0x57186a67
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
		ldstr "test # 15 - bogus due to 64-bit"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
//		ldc.i8 0x00000000deee9c6f
//		conv.u
//		ldc.i4 0xdeee9c6f
//		beq OK_15
//		leave FAIL
	OK_15:
//		leave begin_16
		leave begin_33
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_16:
	.try {
		ldstr "test # 16"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000000001378a07
		conv.u
		ldc.i4 0x01378a07
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
		ldc.i8 0x0000001d8f4a75e2
		conv.u
		ldc.i4 0x8f4a75e2
		beq OK_17
		leave FAIL
	OK_17:
		leave begin_18
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_18:
	.try {
		ldstr "test # 18"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00000064baba5d4e
		conv.u
		ldc.i4 0xbaba5d4e
		beq OK_18
		leave FAIL
	OK_18:
		leave begin_19
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_19:
	.try {
		ldstr "test # 19"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000006e404fd398
		conv.u
		ldc.i4 0x404fd398
		beq OK_19
		leave FAIL
	OK_19:
		leave begin_20
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_20:
	.try {
		ldstr "test # 20"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x000000252ccdff55
		conv.u
		ldc.i4 0x2ccdff55
		beq OK_20
		leave FAIL
	OK_20:
		leave begin_21
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_21:
	.try {
		ldstr "test # 21"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000276118d2bdaa
		conv.u
		ldc.i4 0x18d2bdaa
		beq OK_21
		leave FAIL
	OK_21:
		leave begin_22
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_22:
	.try {
		ldstr "test # 22"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00003ef8751db623
		conv.u
		ldc.i4 0x751db623
		beq OK_22
		leave FAIL
	OK_22:
		leave begin_23
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_23:
	.try {
		ldstr "test # 23"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000eefbead97db0
		conv.u
		ldc.i4 0xead97db0
		beq OK_23
		leave FAIL
	OK_23:
		leave begin_24
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_24:
	.try {
		ldstr "test # 24"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0000d873ee6273ce
		conv.u
		ldc.i4 0xee6273ce
		beq OK_24
		leave FAIL
	OK_24:
		leave begin_25
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_25:
	.try {
		ldstr "test # 25"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x00562d105c4b017d
		conv.u
		ldc.i4 0x5c4b017d
		beq OK_25
		leave FAIL
	OK_25:
		leave begin_26
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_26:
	.try {
		ldstr "test # 26"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0001f90ce65c3ecd
		conv.u
		ldc.i4 0xe65c3ecd
		beq OK_26
		leave FAIL
	OK_26:
		leave begin_27
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_27:
	.try {
		ldstr "test # 27"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x007e901cfebdcaae
		conv.u
		ldc.i4 0xfebdcaae
		beq OK_27
		leave FAIL
	OK_27:
		leave begin_28
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_28:
	.try {
		ldstr "test # 28"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x0064eebedf02e9c6
		conv.u
		ldc.i4 0xdf02e9c6
		beq OK_28
		leave FAIL
	OK_28:
		leave begin_29
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_29:
	.try {
		ldstr "test # 29"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0xfb60c56a5d39eaa4
		conv.u
		ldc.i4 0x5d39eaa4
		beq OK_29
		leave FAIL
	OK_29:
		leave begin_30
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_30:
	.try {
		ldstr "test # 30"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x3cfb0f8bf8157095
		conv.u
		ldc.i4 0xf8157095
		beq OK_30
		leave FAIL
	OK_30:
		leave begin_31
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_31:
	.try {
		ldstr "test # 31"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0xff4cef50a2b87120
		conv.u
		ldc.i4 0xa2b87120
		beq OK_31
		leave FAIL
	OK_31:
		leave begin_32
	} catch [mscorlib]System.OverflowException {
		pop
		leave FAIL
	}
begin_32:
	.try {
		ldstr "test # 32"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i8 0x9462882f61a19634
		conv.u
		ldc.i4 0x61a19634
		beq OK_32
		leave FAIL
	OK_32:
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
