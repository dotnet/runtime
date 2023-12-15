// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'ldind_stind_unaligned_2'{ //This byte field requests that this assembly not be verified at run time and corresponds to this C# declaration:
  //[assembly:System.Security.Permissions.SecurityPermissionAttribute( [mscorlib]System.Security.Permissions.SecurityAction.RequestMinimum, Flags=System.Security.Permissions.SecurityPermissionFlag.SkipVerification )]
                        }

.class public ldind_stind {

//-------------------------
// Global Data            -
//-------------------------
/*
    .data I1 = int32(0x00000000)

    .data I2 = int32(0x00000000)

    .data I4 = int32(0x00000000)

    .data I8 = int64(0x0000000000000000)

    .data R4 = int32(0x00000000)

    .data R8 = int64(0x0000000000000000)

    .data I = int32(0xAAAAAAAA)
*/

	.field static int32 I1
	.field static int32 I2
	.field static int32 I4
	.field static int64 I8
	.field static float32 R4
	.field static float64 R8
	.field static int32 I


//-------------------------
// Entry point - Main     -
//-------------------------
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
//----------------
// begin testing -
//----------------
// --- Store values ---
	ldsflda		int32 ldind_stind::I1
	ldc.i4		0x00000011
	unaligned. 0x2
	stind.i1

	ldsflda		int32 ldind_stind::I2
	ldc.i4		0x00002222
	unaligned. 0x2
	stind.i2

	ldsflda		int32 ldind_stind::I4
	ldc.i4		0x44444444
	unaligned. 0x2
	stind.i4

	ldsflda		int64 ldind_stind::I8
	ldc.i8		0x8888888888888888
	unaligned. 0x2
	stind.i8

	ldsflda		float32 ldind_stind::R4
	ldc.r4		float32(0x3F800000)
	unaligned. 0x2
	stind.r4

	ldsflda		float64 ldind_stind::R8
	ldc.r8		float64(0x3FF0000000000000)
	unaligned. 0x2
	stind.r8

	ldsflda		int32 ldind_stind::I
	ldsflda		int32 ldind_stind::I
	unaligned. 0x2
	stind.i

//----------------------------------

	ldsflda		int32 ldind_stind::I1
	unaligned. 0x2
	ldind.i1
	ldc.i4		0x00000011
	ceq
	brfalse		FAIL

	ldsflda		int32 ldind_stind::I2
	unaligned. 0x2
	ldind.i2
	ldc.i4		0x00002222
	ceq
	brfalse		FAIL

	ldsflda		int32 ldind_stind::I4
	unaligned. 0x2
	ldind.i4
	ldc.i4		0x44444444
	ceq
	brfalse		FAIL

	ldsflda		int64 ldind_stind::I8
	unaligned. 0x2
	ldind.i8
	ldc.i8		0x8888888888888888
	ceq
	brfalse		FAIL

	ldsflda		float32 ldind_stind::R4
	unaligned. 0x2
	ldind.r4
	ldc.r4		float32(0x3F800000)
	ceq
	brfalse		FAIL

	ldsflda		float64 ldind_stind::R8
	unaligned. 0x2
	ldind.r8
	ldc.r8		float64(0x3FF0000000000000)
	ceq
	brfalse		FAIL

	ldsflda		int32 ldind_stind::I
	unaligned. 0x2
	ldind.i
	conv.i4
	ldsflda		int32 ldind_stind::I
	conv.i4
	ceq
	brfalse		FAIL

//----------------
PASS:
	ldc.i4	0x64
	ret
//----------------
FAIL:
	ldc.i4	0x0
	ret
}
}
