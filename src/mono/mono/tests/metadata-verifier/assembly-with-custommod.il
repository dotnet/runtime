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
	.method public static 
		void modreq ([mscorlib]System.Runtime.CompilerServices.IsVolatile)
		ModRecOnRet () cil managed
	{
		ret
	} 
	
	.method public static
		int32 modopt ([mscorlib]System.Runtime.CompilerServices.IsVolatile)
		ModOptOnRet () cil managed
	{
		.maxstack 8
		ldc.i4.1
		ret
	} 
	
	.method public static
		void modreq ([mscorlib]System.Runtime.CompilerServices.IsVolatile)
			 modopt ([mscorlib]System.Runtime.CompilerServices.IsVolatile)
			 modreq ([mscorlib]System.Runtime.CompilerServices.IsVolatile)
			 modreq ([mscorlib]System.Runtime.CompilerServices.IsVolatile)
		MultipleModsOnRet () cil managed
	{
		ret
	}

	.method public static typedref StrangeMethod () cil managed
	{
		.locals init (typedref V_0)

		ldloca 0
		initobj typedref
		ldloc.0
		ret
	}

	.method public static int32& ByRefMethod () cil managed
	{
		.locals init (int32 V_0)

		ldloca 0
		ret
	}

	.method public static int32 SimpleMethod (int32 foo) cil managed
	{
		ldc.i4.0
		ret
	}

	.method public static void MethodWithCalli () cil managed
	{
		.locals init (native int V_0)

		ldftn int32 class Program::SimpleMethod ()
		stloc.0
		ldloc.0
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


