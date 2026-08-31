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

.class sealed public auto ansi beforefieldinit GenClass<T> extends [mscorlib]System.Object
{
	.field public int32 __x
}

.class sealed public auto ansi beforefieldinit GenStruct<T> extends [mscorlib]System.ValueType
{
	.field public int32 __x
}


.class public auto ansi beforefieldinit Test<T> extends [mscorlib]System.Object
{
	.method public static void Main () cil managed 
	{
		.maxstack 20
		sizeof void*
		sizeof int32*
		sizeof int32 modreq ([mscorlib]System.Runtime.CompilerServices.IsVolatile)*
		sizeof method int32 *(int32)
		sizeof method unmanaged cdecl int32 *(int32)
		ldc.i4.0
		newarr int32[]
		sizeof int32[,]
		sizeof class GenClass<int32>	
		sizeof valuetype GenStruct<object>

		leave END
END:
		ret
	}
}


.class public auto ansi beforefieldinit Program extends [mscorlib]System.Object
{
	.method public static void Main () cil managed 
	{
		.entrypoint
		.maxstack 8
		ret 
	}
}

