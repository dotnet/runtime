
// unverifiable CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly 'unverifiable_callvirt_final_virtual_method_2_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}


.class ClassA extends [mscorlib]System.Object
{
	.method public hidebysig  specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public  void Method1() cil managed
	{
		ret
	}

	.method public  void Method2(int32 a) cil managed
	{
		ret
	}

	.method public static void StaticMethod() cil managed
	{
		ret
	}
}


.class Driver extends [mscorlib]System.Object
{

	.method public virtual void VirtMethod ()
	{
		ret
	}

	.method public hidebysig  specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public static void Main() cil managed
	{
		.entrypoint
		.maxstack 8
		.locals init ()

		.try {
			newobj instance void ClassA::.ctor()
			callvirt instance void ClassA::StaticMethod()

			leave END
		} catch [mscorlib]System.NullReferenceException {
			leave END
		}

END:
		ret
	}

}
