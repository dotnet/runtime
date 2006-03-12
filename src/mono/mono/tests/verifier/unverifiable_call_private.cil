// Invalid CIL which breaks the ECMA-335,III,3.19 rule. 
// This CIL should fail verification by a conforming CLI verifier.

.class c
{
	.method private static int32 f(int32 i) cil managed
	{
		ldc.i4.0
		ret
	}
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 1
	ldc.i4.0
	call int32 c::f(int32) // Unverifiable, private method.
	ret
}