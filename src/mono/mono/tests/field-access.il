.assembly extern mscorlib {}

.assembly 'field-access' {}

.class public FieldFail extends [mscorlib]System.Object {

	.field private static string _sfield

	.field private string _field

	.method public specialname rtspecialname instance void  .ctor()
	{
		ldarg.0
		call       instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.class Test {

	.method static void read_sfield ()
	{
		ldsfld string FieldFail::_sfield
		pop
		ret
	}

	.method static void read_field ()
	{
		.locals (FieldFail ff)

		newobj instance void FieldFail::.ctor()
		stloc ff
		ldloc ff
		ldfld string FieldFail::_field
		pop
		ret
	}

	.method static int32 Main ()
	{
		.entrypoint

		.try {
			call void Test::read_sfield ()
			br fail
		} catch [mscorlib]System.FieldAccessException {
			pop
			br continue
		} catch [mscorlib]System.Exception {
			pop
			br fail
		}

continue:
		.try {
			call void Test::read_field ()
			br fail
		} catch [mscorlib]System.FieldAccessException {
			pop
			br success
		} catch [mscorlib]System.Exception {
			pop
			br fail
		}
		
success:
		ldstr "PASS"
		call void [mscorlib]System.Console::WriteLine(string)
		ldc.i4.0
		ret

fail:
		ldstr "FAIL"
		call void [mscorlib]System.Console::WriteLine(string)
		ldc.i4.1
		ret
	}
}
