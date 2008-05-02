
// unverifiable CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly 'unverifiable_ret_generics_vt_4_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}


.method public static int32 _Foo() cil managed
{
	.entrypoint
	.maxstack 8
	.locals init ()

	ldc.i4.0
	leave EMPTY
EMPTY:

	.try {
		leave END
	} catch [mscorlib]System.InvalidCastException {
		leave END		
	}	 

END:
	ldc.i4.0
	ret
}
