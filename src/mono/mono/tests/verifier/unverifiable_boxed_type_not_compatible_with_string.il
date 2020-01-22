.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}

.assembly 'boxing_tests' {
}

.module 'boxing_tests'

.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method private static hidebysig default string Foo () cil managed
	{
		.maxstack 8
		ldc.i4.s 0x0a
		box [mscorlib]System.Int32
		ret 
	}

	.method private static hidebysig default int32 Main () cil managed
	{
		.entrypoint
		.maxstack 8
		call string class Driver::Foo()
		pop
		ldc.i4.0
		ret 
	}
}

