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
	.method public static hidebysig default !!T Foo<class T> (object o) cil managed 
    {
		.maxstack 8
		ldarg.0 
		isinst !!0
		ret 
	}

	.method public static hidebysig default int32 Main () cil managed 
	{
		.entrypoint
		.maxstack 4
		.locals init ()
		ldnull
		call !!0 class Driver::Foo<string> (object)
		pop
		ldc.i4.0
		ret 
	}
}

