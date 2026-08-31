.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'array'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module array.exe // GUID = {8AA4DE74-4ADB-4A6F-A49C-C71D067F856F}


.class private auto ansi beforefieldinit Driver extends [mscorlib]System.Object
 {

	.method private static  hidebysig default void Main ()  cil managed 
	{
		.entrypoint
		.maxstack 2
		.locals init (int32[]	V_0, unsigned int32[]	V_1)
		ldc.i4.1 
		newarr [mscorlib]System.Int32
		stloc.0 
		ldc.i4.1 
		newarr [mscorlib]System.UInt32
		stloc.1 
		ldloc.1 
		stloc.0 
		ret 
	}

  }

