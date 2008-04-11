
// valid CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly 'valid_method_constraint_vt_object_ct_12_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.class Target<class K> extends [mscorlib]System.Object
{
	.method public hidebysig  specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public static void DoStuff<valuetype T> () cil managed
	{
		.maxstack 8
		.locals init ()
		ldtoken !K
         call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        call void class [mscorlib]System.Console::WriteLine(object)

		ldtoken !!T
        call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        call void class [mscorlib]System.Console::WriteLine(object)
		ret
	}
}

.class Driver extends [mscorlib]System.Object
{
	.method public static void MemberMain() cil managed
	{
		.maxstack 8
		.locals init ()
		call void class Target<int32>::DoStuff<int32>()
		ret
	}

	.method public static void Main() cil managed
	{
		.entrypoint
		.maxstack 8
		.locals init ()

		call void Driver::MemberMain()
		leave END

END:
		ret
	}

}
