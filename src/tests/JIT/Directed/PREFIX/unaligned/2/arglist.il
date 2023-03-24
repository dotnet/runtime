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
.assembly extern xunit.core {}
.assembly 'arglist_Target_32BIT_unaligned_2'{ //This byte field requests that this assembly not be verified at run time and corresponds to this C# declaration:
  //[assembly:System.Security.Permissions.SecurityPermissionAttribute( [mscorlib]System.Security.Permissions.SecurityAction.RequestMinimum, Flags=System.Security.Permissions.SecurityPermissionFlag.SkipVerification )]
                        }

.method static vararg int32 CompareArgs(int32){
.locals(int32 currentindex, int32 loopconstant)
.maxstack 10
.try{
	ldc.i4		1
	stloc		currentindex
	ldarg		0
	stloc		loopconstant
LOOP:	ldloc		currentindex
	ldc.i4		4
	mul
	arglist
	add
	unaligned. 0x2
	ldind.i4
	ldloc		currentindex
	ldloc		loopconstant
	add
	ldc.i4		4
	mul
	arglist
	add
	unaligned. 0x2
	ldind.i4
	ceq
	brfalse		EXITWITHFAIL
	ldloc		currentindex
	ldloc		loopconstant
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
.class public auto ansi arglist_ {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
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
