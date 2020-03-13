
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


.method public static void Test (object &res) cil managed
{
	.maxstack 8
	ldarg.0
	ldc.i4.0
	box [mscorlib]System.Int32
	stind.ref
	ret
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 8
	.locals init (object res)
	ldloca 0
	call void Test(object&)


	ldc.i4.0
	ret
}
