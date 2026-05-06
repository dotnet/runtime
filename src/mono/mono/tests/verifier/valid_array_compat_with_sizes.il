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
	.method public static hidebysig default int32 Test ()  cil managed
	{
		.maxstack 8
		.locals init (int32[4, 8] V_0)

		ldloc.0
		ldc.i4.0
		ldc.i4.0
		callvirt instance int32 int32[,]::Get(int32, int32)	
		ret
	}
	

	.method private static hidebysig default int32 Main ()  cil managed 
	{
		.entrypoint
		.maxstack 8
		.locals init ()

		call int32 class Driver::Test ()
		ret
	}
}

