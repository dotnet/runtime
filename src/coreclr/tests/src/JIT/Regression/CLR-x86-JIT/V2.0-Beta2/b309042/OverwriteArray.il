
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib { }

.assembly OverwriteArray
{
  .custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 02 00 00 )  .custom instance void [mscorlib]System.Security.SecurityTransparentAttribute::.ctor() = ( 01 00 00 00 )
	.permissionset reqrefuse =
		(3C 00 50 00 65 00 72 00 6D 00 69 00 73 00 73 00   
		 69 00 6F 00 6E 00 53 00 65 00 74 00 20 00 63 00   
		 6C 00 61 00 73 00 73 00 3D 00 22 00 53 00 79 00   
		 73 00 74 00 65 00 6D 00 2E 00 53 00 65 00 63 00   
		 75 00 72 00 69 00 74 00 79 00 2E 00 50 00 65 00   
		 72 00 6D 00 69 00 73 00 73 00 69 00 6F 00 6E 00   
		 53 00 65 00 74 00 22 00 0D 00 0A 00 20 00 20 00   
		 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00   
		 20 00 20 00 20 00 20 00 20 00 76 00 65 00 72 00   
		 73 00 69 00 6F 00 6E 00 3D 00 22 00 31 00 22 00   
		 3E 00 0D 00 0A 00 20 00 20 00 20 00 3C 00 49 00   
		 50 00 65 00 72 00 6D 00 69 00 73 00 73 00 69 00   
		 6F 00 6E 00 20 00 63 00 6C 00 61 00 73 00 73 00   
		 3D 00 22 00 53 00 79 00 73 00 74 00 65 00 6D 00   
		 2E 00 53 00 65 00 63 00 75 00 72 00 69 00 74 00   
		 79 00 2E 00 50 00 65 00 72 00 6D 00 69 00 73 00   
		 73 00 69 00 6F 00 6E 00 73 00 2E 00 53 00 65 00   
		 63 00 75 00 72 00 69 00 74 00 79 00 50 00 65 00   
		 72 00 6D 00 69 00 73 00 73 00 69 00 6F 00 6E 00   
		 2C 00 20 00 6D 00 73 00 63 00 6F 00 72 00 6C 00   
		 69 00 62 00 2C 00 20 00 56 00 65 00 72 00 73 00   
		 69 00 6F 00 6E 00 3D 00 31 00 2E 00 30 00 2E 00   
		 33 00 33 00 30 00 30 00 2E 00 30 00 2C 00 20 00   
		 43 00 75 00 6C 00 74 00 75 00 72 00 65 00 3D 00   
		 6E 00 65 00 75 00 74 00 72 00 61 00 6C 00 2C 00   
		 20 00 50 00 75 00 62 00 6C 00 69 00 63 00 4B 00   
		 65 00 79 00 54 00 6F 00 6B 00 65 00 6E 00 3D 00   
		 62 00 37 00 37 00 61 00 35 00 63 00 35 00 36 00   
		 31 00 39 00 33 00 34 00 65 00 30 00 38 00 39 00   
		 22 00 0D 00 0A 00 20 00 20 00 20 00 20 00 20 00   
		 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00   
		 20 00 20 00 20 00 76 00 65 00 72 00 73 00 69 00   
		 6F 00 6E 00 3D 00 22 00 31 00 22 00 0D 00 0A 00   
		 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00   
		 20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00   
		 46 00 6C 00 61 00 67 00 73 00 3D 00 22 00 53 00   
		 6B 00 69 00 70 00 56 00 65 00 72 00 69 00 66 00   
		 69 00 63 00 61 00 74 00 69 00 6F 00 6E 00 22 00   
		 2F 00 3E 00 0D 00 0A 00 3C 00 2F 00 50 00 65 00   
		 72 00 6D 00 69 00 73 00 73 00 69 00 6F 00 6E 00   
		 53 00 65 00 74 00 3E 00 0D 00 0A 00 )             
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
