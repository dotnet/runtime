// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly initblk.exe{ //This byte field requests that this assembly not be verified at run time and corresponds to this C# declaration:
  //[assembly:System.Security.Permissions.SecurityPermissionAttribute( [mscorlib]System.Security.Permissions.SecurityAction.RequestMinimum, Flags=System.Security.Permissions.SecurityPermissionFlag.SkipVerification )]
  .permission reqmin ['mscorlib']System.Security.Permissions.SecurityPermissionAttribute ( "SkipVerification" = true )
                        }

.class _initblk {


//    .data DATA = int32(0x00000000)
	.field static int32 DATA

//-------------------------
// Entry point - Main     -
//-------------------------
.method static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
//----------------
// begin testing -
//----------------
	// -- init a 4 byte block of memory to 0xAAAAAAAA
	ldsflda		int32 _initblk::DATA
	ldc.i4		0xAA
	ldc.i4		4
	volatile.
	initblk
	// -- load the 4 bytes of memory _and be sure it is 0xAAAAAAAA
	ldsflda		int32 _initblk::DATA
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
