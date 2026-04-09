// Invalid CIL which breaks the ECMA-335,III,3.19 rule. 
// This CIL should fail verification by a conforming CLI verifier.
#FIXME how to tell ilasm to emit such method?

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 1
	call int32 f()
	ret
}

