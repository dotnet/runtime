.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly extern public_field
{
  .ver 0:0:0:0
}
.assembly 'loader'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module resolve_field_bug.exe


.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method public static  hidebysig default void MemberRefToken ()  cil managed 
	{
		.maxstack 2
		.locals init ()
		ldfld int32 [mscorlib]System.String::FieldThatDontExist
		pop
		ret 
	}

	.method public static  hidebysig default int32 Main ()  cil managed 
	{
		.entrypoint
		.maxstack 8
		.locals init (
			class [mscorlib]System.Reflection.Assembly	V_0,
			class [mscorlib]System.Reflection.Module	V_1,
			int32						V_2)
		.try {
			call class [mscorlib]System.Reflection.Assembly class [mscorlib]System.Reflection.Assembly::GetExecutingAssembly()
			stloc.0 
			ldloc.0 
			callvirt instance class [mscorlib]System.Reflection.Module[] class [mscorlib]System.Reflection.Assembly::GetLoadedModules()
			ldc.i4.0 
			ldelem.ref 
			stloc.1 
			ldstr "field {0}"
			ldloc.1 
			ldc.i4 0xA000001	/* Has to match FieldThatDontExist TOKEN */
			callvirt instance class [mscorlib]System.Reflection.FieldInfo class [mscorlib]System.Reflection.Module::ResolveField(int32)
			call void class [mscorlib]System.Console::WriteLine(string, object)
			ldc.i4.1
			stloc.2
			leave END
		} catch [mscorlib]System.MissingFieldException {
			ldc.i4.0
			stloc.2
			leave END
		}
END:
		ldloc.2
		ret
	}
}

