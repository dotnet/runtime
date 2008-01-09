
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
	.maxstack 8
	.locals init (int32 bla)

	ldc.i4 987
	stloc.0

	ldstr "Value is {0}"

	//array
	ldc.i4 10
	newarr object

	dup

	//index
	ldc.i4.2

	//value
	ldloc.0	
	box [mscorlib]System.Int32

	//array store (array, index, value) -> .
	stelem.ref

	call void class [mscorlib]System.Console::WriteLine(string, object[])

	ldc.i4.0
	ret
}
