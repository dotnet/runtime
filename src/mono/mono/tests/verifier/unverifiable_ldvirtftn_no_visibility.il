
.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'ldftn_test'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module ldftn_test.exe

.class public Test
  	extends [mscorlib]System.Object
{
	.method public hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ldarg.0
		call instance void object::.ctor()
		ret 
	}

	.method family virtual void VirtMethod ()
	{
		ret
	}

	.method public void Method ()
	{
		ret
	}

	.method public static void StaticMethod ()
	{
		ret
	}
}

.class sealed MyValueType extends [mscorlib]System.ValueType
{
	.field private int32 v
}

.class public auto ansi beforefieldinit Driver
        extends [mscorlib]System.Object
{
	.method public static void Method ()
	{
		ret
	}

	.method public virtual void VirtMethod ()
	{
		ret
	}

	.method public static void Foo (native int bla) {
		ret
	}

	.method public static int32 Main ()
	{
		.entrypoint
		.maxstack 2
		.locals init (Test V0, MyValueType V1)
		newobj void class Test::.ctor()
		stloc.0

		newobj void class Test::.ctor() // valid
		ldvirtftn instance void class Test::VirtMethod()
		pop

		ldc.i4.0
		ret 
	}
}
