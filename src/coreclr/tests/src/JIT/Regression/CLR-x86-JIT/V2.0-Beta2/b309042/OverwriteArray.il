
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib { }

.assembly OverwriteArray
{
  .custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 02 00 00 )  .custom instance void [mscorlib]System.Security.SecurityTransparentAttribute::.ctor() = ( 01 00 00 00 )
}

.class public OverwriteArray extends [mscorlib]System.Object
{
	.method public static int32 Main() cil managed
	{
		.entrypoint
		.maxstack		4
		
		.try
		{
			ldstr		"Trying to stelem.i4 directly into an int32[0...]"
			call		void [System.Console]System.Console::WriteLine(string)

			ldc.i4		2
			ldc.i4		3
			newobj		instance void int32[...]::.ctor(int32, int32)

			ldc.i4		0
			ldc.i4		0xc8
			stelem.i4		
			ldstr		"Shouldn't be here..."
			call		void [System.Console]System.Console::WriteLine(string)
			leave		FAIL
		}
		catch [mscorlib]System.Security.VerificationException
		{
			ldstr		"Got a verification exception"
			call		void [System.Console]System.Console::WriteLine(string)
			leave		PASS
		}
		catch [mscorlib]System.InvalidProgramException
		{
			ldstr		"Got an invalid program exeption"
			call		void [System.Console]System.Console::WriteLine(string)
			leave		PASS
		}
		catch [mscorlib]System.Exception
		{
			callvirt	instance string [mscorlib]System.Object::ToString()
			ldstr		"Got an unknown exception"
			call		void [System.Console]System.Console::WriteLine(string)
			call		void [System.Console]System.Console::WriteLine(string)
			leave		FAIL
		}

PASS:
		ldc.i4		100
		ldstr		"Pass"
		call		void [System.Console]System.Console::WriteLine(string)
		ret

FAIL:
		ldc.i4		101
		ldstr		"FAIL"
		call		void [System.Console]System.Console::WriteLine(string)
		ret
	}
}
