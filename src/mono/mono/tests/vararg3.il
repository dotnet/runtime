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

	.method public static vararg void varargtest () cil managed {
		.maxstack 16
		.locals init (
			valuetype [mscorlib]System.ArgIterator it
		)

		ldloca.s it
		arglist
		call instance void [mscorlib]System.ArgIterator::.ctor (valuetype [mscorlib]System.RuntimeArgumentHandle)
	L0:
		ldloca.s it
		call instance int32 [mscorlib]System.ArgIterator::GetRemainingCount ()
		ldc.i4.0
		ceq
		brtrue L1
		ldloca.s it
		call instance typedref [mscorlib]System.ArgIterator::GetNextArg ()
		call object [mscorlib]System.TypedReference::ToObject (typedref)
		call void [mscorlib]System.Console::WriteLine (object)
		br.s L0
	L1:
		ret
	}

	// Test varargs
	.method public static int32 test_0_varargs () cil managed {
		.maxstack 16

		ldc.i4.0
		ldc.i4.1
		call vararg void Tests::varargtest (..., int32, int32)
		ldc.i4.0
		ret
	}		

	.method public static vararg int32 varargtest_refany (int32 last) cil managed {
		.maxstack 16
		.locals init (
			valuetype [mscorlib]System.ArgIterator it,
			int32 sum
		)

		ldloca.s it
		arglist
		call instance void [mscorlib]System.ArgIterator::.ctor (valuetype [mscorlib]System.RuntimeArgumentHandle)
		ldc.i4.0
		stloc.s sum
	L0:
		ldloca.s it
		call instance int32 [mscorlib]System.ArgIterator::GetRemainingCount ()
		ldc.i4.0
		ceq
		brtrue L1
		ldloca.s it
		call instance typedref [mscorlib]System.ArgIterator::GetNextArg ()
		ldloc.s sum
		call void [mscorlib]System.Console::WriteLine (int32)
		refanyval int32
		ldind.i4
		dup
		call void [mscorlib]System.Console::WriteLine (int32)
		ldloc.s sum
		add
		stloc.s sum
		br.s L0
	L1:
		ldloc.s sum
		ret
	}	

	// Test varargs
	.method public static int32 test_99_varargs_refany () cil managed {
		.maxstack 16

		ldc.i4 1
		ldc.i4 2
		ldc.i4 3
		ldc.i4 4
		ldc.i4 5
		ldc.i4 42
		ldc.i4 43
		call vararg int32 Tests::varargtest_refany (int32, ..., int32, int32, int32, int32, int32, int32)
		ret
	}

	// Test varargs
	.method public static int32 test_0_varargs_refany_noargs () cil managed {
		.maxstack 16

		ldc.i4 1
		call vararg int32 Tests::varargtest_refany (int32)
		ret
	}

	.method public static int32 test_0_refanytest () cil managed {
		.maxstack 16
		.locals init (
			int32 sum,
			typedref ref
		)

		ldloca.s sum
		mkrefany int32
		stloc.s ref

		// Test refany with valid type
		ldloc.s ref
		refanyval int32
		ldloca.s sum
		ceq
		brfalse ERR1

		// Test refany with invalid type
		.try {
			ldloc.s ref
			refanyval int64
			pop
			leave ERR2
		}
		catch [mscorlib]System.InvalidCastException {
			pop
			leave L_LEAVE
		}
	L_LEAVE:

		// Test refanytype
		ldloc.s ref
		refanytype
		call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle (valuetype [mscorlib]System.RuntimeTypeHandle)
		ldtoken int32
		call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle (valuetype [mscorlib]System.RuntimeTypeHandle)
		ceq
		brfalse ERR3

		ldc.i4.0
		ret
	ERR1:
		ldc.i4.1
		ret
	ERR2:
		ldc.i4.2
		ret
	ERR3:
		ldc.i4.3
		ret
	}

	.method public static vararg float64 varargtest_refany_fp (float64 last) cil managed {
		.maxstack 16
		.locals init (
			valuetype [mscorlib]System.ArgIterator it,
			float64 sum
		)

		ldloca.s it
		arglist
		call instance void [mscorlib]System.ArgIterator::.ctor (valuetype [mscorlib]System.RuntimeArgumentHandle)
	L0:
		ldloca.s it
		call instance int32 [mscorlib]System.ArgIterator::GetRemainingCount ()
		ldc.i4.0
		ceq
		brtrue L1
		ldloca.s it
		call instance typedref [mscorlib]System.ArgIterator::GetNextArg ()
		//ldloc.s sum
		//call void [mscorlib]System.Console::WriteLine (float64)
		refanyval float64
		ldind.r8
		//dup
		//call void [mscorlib]System.Console::WriteLine (float64)
		ldloc.s sum
		add
		stloc.s sum
		br.s L0
	L1:
		ldloc.s sum
		ret
	}	

	.method public static int32 test_99_varargs_refany_fp () cil managed {
		.maxstack 16

		ldc.r8 1
		ldc.r8 2
		ldc.r8 3
		ldc.r8 4
		ldc.r8 5
		ldc.r8 42
		ldc.r8 43
		call vararg float64 Tests::varargtest_refany_fp (float64, ..., float64, float64, float64, float64, float64, float64)
		conv.i4
		ret
	}

	.method public static int32 Main () cil managed {
		.maxstack 16
		.entrypoint

		ldtoken Tests
        call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        call int32 class [TestDriver]TestDriver::RunTests(class [mscorlib]System.Type)
        ret

	}
}