.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'ltest-07'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'ltest-07.exe'

.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.field private static int32 sfield

	.method public static hidebysig default void Main ()  cil managed 
	{
		.entrypoint
		.maxstack 8
		.locals init ()

		ldc.i4.1
		box int32
		ldsfld int32 Driver::sfield
		brtrue LABEL_1
		nop

LABEL_1:
		pop

		ret
	}
}


