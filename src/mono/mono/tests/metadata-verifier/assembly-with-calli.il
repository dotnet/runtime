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
	.method public static int32 SimpleMethod (int32 foo) cil managed
	{
		ldc.i4.0
		ret
	}

	.method public static void MethodWithCalli () cil managed
	{
		ldftn int32 class Program::SimpleMethod (int32)
		ldc.i4.1
		calli unmanaged cdecl int32 (int32) 
		pop
		ret
	}

	.method public static void Main () cil managed 
	{
		.entrypoint
		.maxstack 8
		ret 
	}

}


