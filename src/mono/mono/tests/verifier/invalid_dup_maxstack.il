// Invalid CIL which breaks the ECMA-335,III,3.33 rules. 
// This CIL should fail verification by a conforming CLI verifier.

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 1
	ldc.i4.0
	dup // Invalid, stack larger than maxstack.
	pop
	ret
}