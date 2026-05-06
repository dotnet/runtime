.assembly extern mscorlib
{}

.assembly 'ldtoken-bad-type'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}


.class public Foo<T> {
	.field public !T fld
}


.class public Test {
	.method public static int32 Main() cil managed
	{
		.entrypoint
		.maxstack 2
		.locals init (int32 V_0)

		ldtoken field !0 class Foo<!!0>::fld
		pop

		ldc.i4.0
		ret
	}}

