// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib {}
.class public		_cpblk {



.field public static int32 DATA
.field public static int32 DATA2

.method public static int32 initialize() {
	ldsflda		int32 _cpblk::DATA
	ldc.i4		0xAA
	ldc.i4		4
	initblk
	ldsflda		int32 _cpblk::DATA
	ldc.i4		4
	add
	ldc.i4		0xFF
	ldc.i4		4
	initblk
	ldsflda		int32 _cpblk::DATA
	ldind.i4
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL	
	ldsflda		int32 _cpblk::DATA
	ldc.i4		4
	add
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

.method public		void _cpblk() {
.maxstack	0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
	call	int32 _cpblk::initialize()
	brfalse		FAIL

	ldsflda		int32 _cpblk::DATA
	ldc.i4		4
	add
	ldsflda		int32 _cpblk::DATA
	ldc.i4		4
	cpblk
	ldsflda		int32 _cpblk::DATA
	ldc.i4		4
	add
	ldind.i4
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL	

PASS:
	ldc.i4 100
	br		END

FAIL:
	ldc.i4	0x00000000

END:
	ret
}
}



.assembly cpblk.exe{}
