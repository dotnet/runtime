.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}
.assembly 'repro'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module repro.exe

.class public auto ansi beforefieldinit Foo`1<T> extends [mscorlib]System.Object
{
	.field private class [mscorlib]System.__Type bad_bad_field

	.method public hidebysig  specialname  rtspecialname 
	instance default void '.ctor' ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::'.ctor'()
		ret
	}
}

.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method public static  hidebysig default void faulty ()  cil managed 
	{
		.maxstack 8
		newobj instance void class Foo`1<int32>::'.ctor'()
		call void class [mscorlib]System.Console::WriteLine(object)
		ret 
	}

	.method public static hidebysig default int32 Main () cil managed 
	{
		.entrypoint
		.maxstack 8
		.try {
			call void Driver::faulty ()
			leave err
		} catch class [mscorlib]System.TypeLoadException {
			pop
			leave ok
		}

err:
		ldc.i4.1
		ret 
ok:
		ldc.i4.0
		ret	

	}
}


