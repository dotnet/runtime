
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
	.field public [mscorlib]System.Decimal Value
}


.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 8
	.locals init (MyVT V_0)

	ldloca 0

	ldfld valuetype [mscorlib]System.Decimal MyVT::Value
	pop

	ldc.i4.0
	ret
}
