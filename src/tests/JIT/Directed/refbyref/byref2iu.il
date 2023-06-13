// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern legacy library mscorlib {}
.assembly ASSEMBLY_NAME {}
.field static char c
.class public auto ansi byref2iu {
.method public static int32 Main()
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack 10
	.locals init (char& V_0,
		int8 V_1,
		uint8 V_2,
		int16 V_3,
		uint16 V_4,
		int32 V_5,
		uint32 V_6,
		int64 V_7,
		uint64 V_8,
		native int V_9,
		native unsigned int V_10,
		uint64 V_11)
	ldsflda char c
	stloc.0
	ldloc.0
	conv.i1
	stloc  V_1
	ldloc.0
	conv.u1
	stloc  V_2
	ldloc.0
	conv.i2
	stloc  V_3
	ldloc.0
	conv.u2
	stloc  V_4
	ldloc.0
	conv.i4
	stloc  V_5
	ldloc.0
	conv.u4
	stloc  V_6
	ldloc.0
	conv.i8
	stloc  V_7
	ldloc.0
	conv.u8
	stloc  V_8
	ldloc.0
	conv.i
	stloc  V_9
	ldloc.0
	conv.u
	stloc  V_10

	ldc.i4 0x8000000
	conv.u8
	ldloc  V_1
	conv.u8
	add
	ldloc  V_2
	conv.u8
	add
	ldloc  V_3
	conv.u8
	add
	ldloc  V_4
	conv.u8
	add
	ldloc  V_5
	conv.u8
	add
	ldloc  V_6
	conv.u8
	add
	ldloc  V_7
	conv.u8
	add
	ldloc  V_8
	conv.u8
	add
	ldloc  V_9
	conv.u8
	add
	ldloc  V_10
	conv.u8
	add
	stloc V_11
	ldloc V_11
	call void [System.Console]System.Console::WriteLine(uint64)

	ldloca V_11
	stloc  V_1
	ldloca V_10
	stloc  V_2
	ldloca V_9
	stloc  V_3
	ldloca V_8
	stloc  V_4
	ldloca V_7
	stloc  V_5
	ldloca V_6
	stloc  V_6
	ldloca V_3
	stloc  V_9
	ldloca V_2
	stloc  V_10

	ldc.i4 0xFFFF
	conv.u8
	ldloc  V_1
	conv.u8
	add
	ldloc  V_2
	conv.u8
	add
	ldloc  V_3
	conv.u8
	add
	ldloc  V_4
	conv.u8
	add
	ldloc  V_5
	conv.u8
	add
	ldloc  V_6
	conv.u8
	add
	ldloc  V_7
	conv.u8
	add
	ldloc  V_8
	conv.u8
	add
	ldloc  V_9
	conv.u8
	add
	ldloc  V_10
	conv.u8
	add
	stloc V_11
	ldloc V_11
	call void [System.Console]System.Console::WriteLine(uint64)

	ldstr "PASSED"
	call void [System.Console]System.Console::WriteLine(string)

	ldc.i4 100
	ret
}
}
