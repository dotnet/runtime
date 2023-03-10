// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'initblk_unaligned_1'{ //This byte field requests that this assembly not be verified at run time and corresponds to this C# declaration:
  //[assembly:System.Security.Permissions.SecurityPermissionAttribute( [mscorlib]System.Security.Permissions.SecurityAction.RequestMinimum, Flags=System.Security.Permissions.SecurityPermissionFlag.SkipVerification )]
                        }

.class public _initblk {


//    .data DATA = int32(0x00000000)
	.field static int32 DATA

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
	// -- init a 4 byte block of memory to 0xAAAAAAAA
	ldsflda		int32 _initblk::DATA
	ldc.i4		0xAA
	ldc.i4		4
	unaligned. 0x1
	initblk
	// -- load the 4 bytes of memory _and be sure it is 0xAAAAAAAA
	ldsflda		int32 _initblk::DATA
	unaligned. 0x1
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
