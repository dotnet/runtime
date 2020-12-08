// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly switch.exe{}

.class public		_switch {





.method public		void _switch() {
.maxstack	0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
.locals	(int32)
	ldc.i4	0x0
	stloc	0
	ldc.i4	0x0

	TAG:
	switch (TC1,TC2,TC3)
	ldloc	0
	ldc.i4		0x3
	ceq
	brfalse		FAIL
	br			PASS

		TC1:
			ldloc	0
			ldc.i4		0x1
			add
			stloc	0
			
			ldc.i4		0x1
			br			TAG

		TC2:
			ldloc	0
			ldc.i4		0x1
			add
			stloc	0

			ldc.i4		0x2
			br			TAG

		TC3:
			ldloc	0
			ldc.i4		0x1
			add
			stloc	0

			ldc.i4		0x3
			br			TAG

	br	FAIL


PASS:
	ldc.i4 100
	br		END

FAIL:
	ldc.i4	0x00000000

END:
	ret
}
}

