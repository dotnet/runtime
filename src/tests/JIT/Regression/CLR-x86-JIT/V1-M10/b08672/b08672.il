// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly b08672
{
}
.assembly extern xunit.core {}
.assembly extern mscorlib{}
.class	public stelem_i {

.field static native int[] i

//-------------------------
// Constructor -
//-------------------------
.method void .ctor() {

.maxstack	10
	ldarg.0
	call instance void System.Object::.ctor()
	ret
}


//-------------------------
// Initialize -
//-------------------------
.method static void initialize() {
.maxstack	10
.locals init (int32)

	ldc.i4	0x00000004
	newarr	[mscorlib]System.Int32
	stsfld	native int[] stelem_i::i

//---------------------------------

BEGIN:
	// Initialize array index counter to 0
	ldc.i4		0x00000000
	stloc	0

LOOP:
	// -- initialize 4 element array of i --

	// Load the array reference
	ldsfld		native int[] stelem_i::i

	// Load the index of the array element to be updated
	ldloc	0

	// Load a unique int32 value to be stored at the
	// given location
	dup
	ldc.i4		0x10000000
	add

	// Store a native int
	stelem.i4
//-------------

	//Check for end of loop
	ldloc	0
	ldc.i4		0x00000003
	ceq
	brtrue		END

	//Loop isn't over, increment the counter and continue
	ldloc	0
	ldc.i4		0x00000001
	add
	stloc	0
	br			LOOP
END:
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
.locals init (int32)
//----------------
// begin testing -
//----------------

	// Create the i array with stelem.i
	call	void stelem_i::initialize()
//----------------	
BEGIN:

	//Initialize loop counter
	ldc.i4		0x00
	stloc	0
//---------------------------------
LOOP:
	//Load array reference
	ldsfld		native int[] stelem_i::i

	//Load index to be loaded from the
	//given array.
	ldloc	0

	//Load the element in the array at the
	//given index.
	ldelem.i4

	//Compare loaded array element with
	//expected value
	ldloc	0
	ldc.i4		0x10000000
	add
	ceq
	brfalse		FAIL
//----------------------------------
	//Check loop counter for end condition
	ldloc	0
	ldc.i4		0x00000003
	ceq
	brtrue		PASS

	//Loop not done, increment counter and continue
	ldloc	0
	ldc.i4		0x00000001
	add
	stloc	0
	br			LOOP


//----------------
PASS:
	ldc.i4	100
	ret
//----------------
FAIL:
	ldc.i4	0xABBA
	ret
}
}
