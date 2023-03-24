// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'add_Desktop_volatile' {
 //This byte field requests that this assembly not be verified at run time and corresponds to this C# declaration:
  //[assembly:System.Security.Permissions.SecurityPermissionAttribute( [mscorlib]System.Security.Permissions.SecurityAction.RequestMinimum, Flags=System.Security.Permissions.SecurityPermissionFlag.SkipVerification )]
                        }


.class public explicit _add {

.field [0] int32 global0
.field [4] int32 global1
.method public void .ctor() { ret }


//-------------------------
// Entry point - Main     -
//-------------------------
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	6
.locals init (class _add target)
//----------------
// begin testing -
//----------------

	// creating new instance of _add and storing it
	newobj instance void _add::.ctor()
	stloc.0

	// -- I4 + I2 -- Make sure we can _add smaller types to larger types
	ldc.i4	0xAABB
	conv.i2
	ldc.i4	0x1111
	add
	ldc.i4	0xFFFFBBCC
	ceq
	brfalse	FAIL
	// -- I4 + I4 --
	ldc.i4	0x11223344
	ldc.i4	0x11111111
	add
	ldc.i4	0x22334455
	ceq
	brfalse	FAIL

	// -- I4 + &  -- Managed ptr

	// -- I4 + *  -- Unmanaged ptr
	ldloc target
	ldflda	int32 _add::global0
	ldc.i4	0xAAAAAAAA
	volatile.
	stind.i4	

	ldloc target
	ldflda	int32 _add::global1
	ldc.i4	0x44444444
	volatile.
	stind.i4

	ldloc target
	ldflda	int32 _add::global0
	ldc.i4	0x4
	add
	volatile.
	ldind.i4
	ldc.i4	0x44444444
	ceq
	brfalse	FAIL

	// -- I8 + I8 --
	ldc.i8	0x0011223344556677
	ldc.i8	0x7766554433221100
	add
	ldc.i8	0x7777777777777777
	ceq
	brfalse	FAIL

	// -- R4 + R4 --
	ldc.r4	float32(0x3F800000)
	ldc.r4	float32(0x00000000)
	add
	ldc.r4	float32(0x3F800000)
	ceq
	brfalse	FAIL
	
	// -- R8 + R8 --
	ldc.r8	float64(0xBFF0000000000000)
	ldc.r8	float64(0x0000000000000000)
	add
	ldc.r8	float64(0xBFF0000000000000)
	ceq
	brfalse	FAIL


//----------------
PASS:
	ldstr "Test SUCCESS"
	call void [System.Console]System.Console::WriteLine(string)
	ldc.i4	0x64
	ret
//----------------
FAIL:
	ldstr "Test FAILED"
	call void [System.Console]System.Console::WriteLine(string)
	ldc.i4	0x0
	ret
}
}
