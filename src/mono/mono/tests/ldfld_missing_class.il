
//  CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly 'ldfld_missing_class_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class public auto ansi beforefieldinit MissingMember extends [mscorlib]System.Object
{
	.method public hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
    	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method static private hidebysig void PrivateMethod ()  cil managed 
    	{
		.maxstack 8
		ret 
	}


	.field private int32 privateField
}

.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method public hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
    	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public static void NoInline() cil managed
	{
		.maxstack 8
		ret
	}

	.method public static void TestMethod() cil managed
	{
		.maxstack 8
		call void Driver::NoInline()
		ldnull
		ldfld int32 [mscorlib]MissingClass::fld

		leave END
END:
		ret
	}

	.method public static int32 Main() cil managed
	{
		.entrypoint
		.maxstack 8
		.locals init ( class [mscorlib]System.Exception	V_0, int32 V_1)

		ldc.i4.0
		stloc.1

		.try {
			call void Driver::TestMethod()
			ldstr ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>ldfld_missing_class did not throw an exception"
			call void class [mscorlib]System.Console::WriteLine(string)
			ldc.i4.1 
			stloc.1
			leave END
		}
		catch [mscorlib]System.Exception {
			stloc.0

			ldloc.0
			callvirt instance class [mscorlib]System.Type class [mscorlib]System.Exception::GetType()
			ldtoken [mscorlib]System.TypeLoadException
			call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
			beq ARE_EQUAL
			ldstr ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>ldfld_missing_class thrown {0} instead of System.TypeLoadException"
			ldloc.0 
			callvirt instance class [mscorlib]System.Type class [mscorlib]System.Exception::GetType()
			call void class [mscorlib]System.Console::WriteLine(string, object)
			ldc.i4.1 
			stloc.1
ARE_EQUAL:
			leave END
		}


END:
		ldloc.1
		ret
	}
}
