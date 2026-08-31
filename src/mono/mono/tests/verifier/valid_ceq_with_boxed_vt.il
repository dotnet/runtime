.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'repro'
{

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module repro.exe

.class private auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method public static hidebysig default int32 Test<T> (!!T val)  cil managed
	{
		.maxstack 8
		.locals init ()
		ldarg.0
		box !!T
		ldnull
		ceq
		ret
	}
	

	.method private static hidebysig default int32 Main ()  cil managed 
	{
		.entrypoint
		.maxstack 8
		.locals init ()

		ldc.i4.0
		call int32 class Driver::Test<int32> (!!0)
		ret
	}
}

