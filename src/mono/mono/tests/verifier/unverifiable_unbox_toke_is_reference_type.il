
// valid CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly 'valid_bin_cgt_un_null_value_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class public auto ansi sealed MyEnum extends [mscorlib]System.Enum
{
	.field  public specialname  rtspecialname  int32 value__
	.field public static  literal  valuetype MyEnum Boolean = int32(0x00000026)
}


.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 8
	.locals init (object arr)

	ldnull
	unbox object
	pop

	ldc.i4.0
	ret
}
