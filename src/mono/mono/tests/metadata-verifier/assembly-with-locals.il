.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}
.assembly 'simple_assembly'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module simple_assembly.exe

.class private auto ansi beforefieldinit Program extends [mscorlib]System.Object
{
	.method public static void M0 () cil managed
	{
		.locals init (int32 modreq ([mscorlib]System.Runtime.CompilerServices.IsVolatile) V_0,
					  int32& modreq ([mscorlib]System.Runtime.CompilerServices.IsVolatile) pinned V_1,
					  object pinned V_2,
					  int32*
					  	modreq ([mscorlib]System.Runtime.CompilerServices.IsVolatile) pinned
					  	modopt ([mscorlib]System.Runtime.CompilerServices.IsVolatile) V_3,
					  typedref& V_4,
					  int32& V_5,
					  int32 V_6)
		ret
	} 
	
	.method public static void M1 () cil managed
	{
		.maxstack 8
		.locals init (int32 & V_0, int32 V_1)
		ret
	} 

	.method public static void Main () cil managed 
	{
		.entrypoint
		.maxstack 8
		ret 
	}

}


