.assembly extern mscorlib {}

.assembly 'method-access' {}

.class public MethFail extends [mscorlib]System.Object {

	.method private void foo ()
	{
		ret
	}

	.method static private void sfoo ()
	{
		ret
	}

	.method public specialname rtspecialname instance void  .ctor()
	{
		ldarg.0
		call       instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.class Test {

	.method static void call_sfoo ()
	{
		call void MethFail::sfoo ()
		ret
	}

	.method static void call_foo ()
	{
		.locals (MethFail mf)

		newobj instance void MethFail::.ctor()
		stloc mf
		ldloc mf
		call instance void MethFail::foo ()
		ret
	}

	.method static int32 Main ()
	{
		.entrypoint

		.try {
			call void Test::call_sfoo ()
			br fail
		} catch [mscorlib]System.MethodAccessException {
		  	call instance string [mscorlib]System.MethodAccessException::get_Message ()
			ldstr "Method `MethFail.sfoo()' is inaccessible from method `Test.call_sfoo()'"
			call instance bool [mscorlib]System.String::Equals (string)
			brfalse fail
			br continue
		} catch [mscorlib]System.Exception {
			pop
			br fail
		}

continue:
		.try {
			call void Test::call_foo ()
			br fail
		} catch [mscorlib]System.MethodAccessException {
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
