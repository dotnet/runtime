//
// vararg.il: Tests for varargs and refany instructions
//

.assembly extern mscorlib {
}

.assembly extern TestDriver {
}

.assembly 'refanyval' {
}

.module 'refanyval.exe'

.class public auto ansi Tests extends [mscorlib]System.Object {
	.method public static vararg int32 varargtest_refany (int32 last) cil managed {
		.maxstack 8
		.locals init (valuetype [mscorlib]System.ArgIterator it)

		ldloca.s it
		arglist
		call instance void [mscorlib]System.ArgIterator::.ctor (valuetype [mscorlib]System.RuntimeArgumentHandle)

		ldloca.s it
		call instance typedref [mscorlib]System.ArgIterator::GetNextArg ()
		refanytype
		call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)

		pop

		ldc.i4.0
		ret
	}	

	.method public static int32 Main () cil managed {
		.maxstack 16
		.entrypoint

		ldc.i4 1
		ldc.i4 0
		call vararg int32 Tests::varargtest_refany (int32, ..., int32)


        ret

	}
}
