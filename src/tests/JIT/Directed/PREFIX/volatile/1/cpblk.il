// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'cpblk_volatile' {
//This byte field requests that this assembly not be verified at run time and corresponds to this C# declaration:
  //[assembly:System.Security.Permissions.SecurityPermissionAttribute( [mscorlib]System.Security.Permissions.SecurityAction.RequestMinimum, Flags=System.Security.Permissions.SecurityPermissionFlag.SkipVerification )]
                        }


.class public _cpblk {

//-------------------------
// Global Data            -
//-------------------------

//    .data DATA = int32(0)
//    .data  int32(0)

	.field static int32 DATA
	.field static int32 DATA2

//-------------------------
// initialization routine -
//-------------------------
.method static int32 initialize() {
	// -- init first 4 bytes to 0xAAAAAAAA
	ldsflda		int32 _cpblk::DATA
	ldc.i4		0xAA
	ldc.i4		4
	volatile.
	initblk
	// -- init next 4 bytes to 0xFFFFFFFF
	ldsflda		int32 _cpblk::DATA
	ldc.i4		4
	add
	ldc.i4		0xFF
	ldc.i4		4
	volatile.
	initblk
	//-- be sure memory is properly initialized --
	ldsflda		int32 _cpblk::DATA
	ldind.i4
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL	
	ldsflda		int32 _cpblk::DATA
	ldc.i4		4
	add
	volatile.
	ldind.i4
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL	
	ldc.i4	0x00000001
	ret
FAIL:
	ldc.i4	0x00000000
	ret
}

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
// initialize    -
//----------------
	call	int32 _cpblk::initialize()
	brfalse		FAIL

//----------------
// begin testing -
//----------------
	// -- copy first chunk into second chunk
	ldsflda		int32 _cpblk::DATA
	ldc.i4		4
	conv.i
	add
	ldsflda		int32 _cpblk::DATA
	ldc.i4		4
	conv.i
	volatile.
	cpblk
	// -- Second chunk should now be 0xAAAAAAAA
	ldsflda		int32 _cpblk::DATA
	ldc.i4		4
	conv.i
	add
	volatile.
	ldind.i4
	ldc.i4		0xAAAAAAAA
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
