.assembly extern mscorlib
{}

.assembly 'ldtoken-bad-type'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}


.class public Test {

	.method public static void GenMethod<T>() cil managed
	{
		ret
	}


	.method public static int32 Main() cil managed
	{
		.entrypoint
		.maxstack  14
		.locals init ()

		ldtoken	method void Test::GenMethod<!!0>()
		pop

		ldc.i4.0
		ret
	}}
