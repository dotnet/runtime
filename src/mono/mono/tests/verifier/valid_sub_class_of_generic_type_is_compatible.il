
// valid CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly 'valid_bin_cgt_un_null_value_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class private sequential ansi sealed beforefieldinit MyVT
        extends [mscorlib]System.Object
{
	.field public valuetype [mscorlib]System.Decimal Value
}


.class public ansi beforefieldinit CustomDict
        extends class [mscorlib]System.Collections.Generic.Dictionary`2<string,int32>
{
	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0
		call instance void class [mscorlib]System.Collections.Generic.Dictionary`2<string, int32>::.ctor()
		ret 
	}
}


.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 8
	.locals init (class [mscorlib]System.Collections.Generic.Dictionary`2<string, int32> V_0)

	newobj instance void CustomDict::.ctor()
	stloc.0
	

	ldc.i4.0
	ret
}
