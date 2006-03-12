// Invalid CIL which breaks the ECMA-335,III,4.14 rules. 
// This CIL should fail verification by a conforming CLI verifier.

.class public auto ansi beforefieldinit c
  	extends [mscorlib]System.Object
{
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	.locals init (
		class c V_0
	)
	ldloc.0
	ldsfld int32 c::invalid // Invalid, no field invalid in class c.
	pop
	ldc.i4.0
	ret
}