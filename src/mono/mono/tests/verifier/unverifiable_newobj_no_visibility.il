
// valid CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly 'valid_newobj_good_instantiation_1_generated'
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
	.method family hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}
}


.method public static void Main() cil managed
{
	.entrypoint
	.maxstack 4
	.locals init (
	    int32 V_0
	)
	newobj instance void class ClassA::.ctor()
	pop
	ret
}
