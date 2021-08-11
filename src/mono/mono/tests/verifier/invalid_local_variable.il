.assembly extern mscorlib
{}

.assembly 'bad-local'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}

.class public Test {
	.method public static void Foo() cil managed
	{
		.maxstack  8
		.locals init (!0 V_0)

		ret
	}

	.method public static int32 Main() cil managed
	{
		.entrypoint
		.maxstack  8
		.locals init ()

		call void class Test::Foo()
		ldc.i4.0
		ret
	}}
