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

.class sealed public auto ansi beforefieldinit Foo extends [mscorlib]System.ValueType
{
	.field public int32 __x
}

.class public auto ansi beforefieldinit Test<T> extends [mscorlib]System.Object
{
	//trivial type
	.field  private int32 a0
	//simple array
	.field private int32[] a1
	//complex array
	.field private int32[,,] a2
	//array with bounds
	.field private int32[1...] a3
	.field private int32[0...2] a4
	.field private int32[3...5] a5
	.field private int32[0...5, 2...] a6
	.field private int32[, 2...] a7

	//function pointers
	.field private method int32 *(int32) a8
	.field private method unmanaged cdecl int32 *(int32) a9
	.field private int32 * a10
	.field private void * a11
	.field private int32 modreq ([mscorlib]System.Runtime.CompilerServices.IsVolatile) * a12
	.field private void modopt ([mscorlib]System.Runtime.CompilerServices.IsVolatile)* a13

	.field private object a14
	.field private string a15

	.field private string modopt ([mscorlib]System.Runtime.CompilerServices.IsVolatile) [] a16

	.field valuetype [mscorlib]System.DateTime a17	
	.field valuetype Foo a18
	.field class Test<int32> a19
	.field class [mscorlib]System.IComparable`1<object> a20
	.field valuetype Test2<!T> a21

	.method public static void M1<TT> (!T foo, !!TT bar) cil managed
	{
		.maxstack 8
		ret
	}
}

.class sealed public auto ansi beforefieldinit Test2<T> extends [mscorlib]System.ValueType
{
	.field public int32 __x
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

