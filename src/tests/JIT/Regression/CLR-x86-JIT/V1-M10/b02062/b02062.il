// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b02062
{
}
.class public test
{
  .field public        int32 m_i
  .field public static int32 s_i

	.method public specialname static rtspecialname void .cctor() il managed
	{
		ldc.i4 555
		stsfld	int32 test::s_i
		ret
	}

	.method public specialname rtspecialname instance void .ctor(int32) il managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ldarg 0
		ldarg 1
		stfld	int32 test::m_i
		ret
	}

	.method public static int32 main() il managed
	{
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.locals(class test)
		
		ldc.i4  666
		dup
		stsfld	int32 test::s_i
		ldsfld	int32 test::s_i
		beq PASS1
		
FAIL1:	ldstr "Test failed. Looks like the static field access was incorrectly done"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i4 1
		ret
		
PASS1:	ldstr "Static test passed"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		
		ldc.i4 777
		newobj instance void test::.ctor(int32)
		dup
		dup
		stloc 0
		ldc.i4 888
		stfld int32 test::m_i
		ldfld int32 test::m_i
		ldc.i4 888
		beq PASS2
		
FAIL2:	ldstr "Test failed. Looks like the static field access was incorrectly done"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i4 1
		ret
		
PASS2:	ldstr "Even instance test passed"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i4 100
		ret
	}
}
