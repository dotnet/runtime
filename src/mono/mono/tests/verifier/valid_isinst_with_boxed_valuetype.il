
// valid CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly 'valid_bin_cgt_un_null_value_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	.locals init ()

	ldc.i4 987
	box [mscorlib]System.Int32

	isinst [mscorlib]System.Int32
	unbox [mscorlib]System.Int32
	ldind.i4
	call void [mscorlib]System.Console::WriteLine(int32)

	ldc.i4.0
	ret
}

