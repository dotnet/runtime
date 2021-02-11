
// unverifiable CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly 'unverifiable_assign_compat_9_1_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.method public static void Foo() cil managed
{
	.maxstack 2
	.locals init (
		method int32 *(float64) L0
	)
	ldloc.0
	//The verifier performs DSE before looking at unverifiable types
	stloc.0
	ret
}

.method public static void Main() cil managed
{
	.maxstack 2
	.entrypoint
	call void Foo()
	ret
}
