
// valid CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly 'ldstr_bad_token'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	.locals init ()

	.emitbyte 0x72
	.emitbyte 0x00
	.emitbyte 0x01
	.emitbyte 0x00
	.emitbyte 0x00

	call void [mscorlib]System.Console::WriteLine(object)
	ldc.i4.0
	ret
}
