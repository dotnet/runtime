// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*
CompareArgs() works as such:

CompareArgs(3,1,2,3,1,2,3)
Where arg0 is 1/2 the number of the
remaining arguments.

And the arguments 1,2,3 and 1,2,3 are
treated as two separate lists of size
arg0 whose elements are compared to one
another.

ie. in this case CompareArgs checks that
arg1==arg4, arg2==arg5, arg3==arg6.
*/

.assembly extern legacy library mscorlib {}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary {}
.assembly 'arglist_Target_64BIT_unaligned_1'{ //This byte field requests that this assembly not be verified at run time and corresponds to this C# declaration:
  //[assembly:System.Security.Permissions.SecurityPermissionAttribute( [mscorlib]System.Security.Permissions.SecurityAction.RequestMinimum, Flags=System.Security.Permissions.SecurityPermissionFlag.SkipVerification )]
                        }

/*
CompareArgs(5,1,2,3,4,5,1,2,3,4,5)
arglist on x86: 5,4,3,2,1,5,4,3,2,1,5
                ^ index 1
arglist on ia64: 5,1,2,3,4,5,1,2,3,4,5
                 ^ index 1
*/

.method static vararg int32 CompareArgs(int32){
.locals(int32 currentindex, int32 loopconstant)
.maxstack 10
.try{
	ldc.i4		2
	stloc		currentindex
	ldarg		0
	stloc		loopconstant
LOOP:	ldloc		currentindex
	ldc.i4		8
	mul
	arglist
	add
	unaligned. 0x1
	ldind.i4
	ldloc		currentindex
	ldloc		loopconstant
	add
	ldc.i4		8
	mul
	arglist
	add
	unaligned. 0x1
	ldind.i4
	ceq
	brfalse		EXITWITHFAIL
	ldloc		currentindex
	ldloc		loopconstant
	ldc.i4		1
	add
	beq		EXITWITHPASS
	ldc.i4		1
	ldloc		currentindex
	add
	stloc		currentindex
	br		LOOP
	EXITWITHPASS:
	leave SUCCESS
	EXITWITHFAIL:
	leave FAIL
}catch [mscorlib]System.NullReferenceException{
	pop
	leave FAIL
}
SUCCESS:
	ldc.i4		0x64
	ret
FAIL:
	ldc.i4		0x0
REALLYDONE:
	ret
}

//-------------------------
// Entry point - Main     -
//-------------------------
.class public auto ansi arglist64 {
.method public static int32 main() {
// [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsVarArgSupported))]
.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ConditionalFactAttribute::.ctor(class [System.Runtime]System.Type,
                                                                                                string[]) = ( 01 00 61 54 65 73 74 4C 69 62 72 61 72 79 2E 50   // ..aTestLibrary.P
                                                                                                            6C 61 74 66 6F 72 6D 44 65 74 65 63 74 69 6F 6E   // latformDetection
                                                                                                            2C 20 54 65 73 74 4C 69 62 72 61 72 79 2C 20 56   // , TestLibrary, V
                                                                                                            65 72 73 69 6F 6E 3D 30 2E 30 2E 30 2E 30 2C 20   // ersion=0.0.0.0, 
                                                                                                            43 75 6C 74 75 72 65 3D 6E 65 75 74 72 61 6C 2C   // Culture=neutral,
                                                                                                            20 50 75 62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D   //  PublicKeyToken=
                                                                                                            6E 75 6C 6C 01 00 00 00 11 49 73 56 61 72 41 72   // null.....IsVarAr
                                                                                                            67 53 75 70 70 6F 72 74 65 64 00 00 )             // gSupported..
.entrypoint
.locals	()
.maxstack	10

	ldc.i4	1
	ldc.i4	1
	ldc.i4	1
	call	vararg int32 CompareArgs(int32,...,int32,int32)
	brfalse	FAIL

	ldc.i4	4
	ldc.i4	2
	ldc.i4	3
	ldc.i4	4
	ldc.i4	5
	ldc.i4	2
	ldc.i4	3
	ldc.i4	4
	ldc.i4	5
	call	vararg int32 CompareArgs(int32,...,int32,int32,int32,int32,int32,int32,int32,int32)
	brfalse	FAIL


PASS:
	ldc.i4	0x64
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
