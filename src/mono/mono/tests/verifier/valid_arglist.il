//
// vararg.il: Tests for varargs and refany instructions
//

.assembly extern mscorlib {
}

.assembly extern TestDriver {
}

.assembly 'vararg' {
}

.module 'vararg'

.class public auto ansi Tests extends [mscorlib]System.Object {
	.method public static vararg int32 varargtest_refany (int32 last) cil managed {
		.maxstack 16
		.locals init (valuetype [mscorlib]System.ArgIterator it)

		ldloca.s it
		arglist
		call instance void [mscorlib]System.ArgIterator::.ctor (valuetype [mscorlib]System.RuntimeArgumentHandle)


		ldc.i4.0
		ret
	}

	.method public static int32 Main () cil managed {
		.maxstack 8
		.entrypoint

		ldc.i4.0
		call int32 Tests::varargtest_refany(int32, ...)
        ret
	}
}
