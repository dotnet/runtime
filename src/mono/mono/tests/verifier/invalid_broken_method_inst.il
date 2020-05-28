.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}


.assembly 'test'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module test.dll

.class public Generic<T>
{
	.method public hidebysig specialname rtspecialname  instance default void '.ctor' ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::'.ctor'()
		ret 
	}

	.method public static void Method2 () {
		ret
	}
}

.class public Generic2<T>
{
}

.class public auto ansi beforefieldinit Base extends [mscorlib]System.Object
{
	.method public static void Method<T> () {
		ret
	}

	.method public static hidebysig void Main ()
	{
		.entrypoint
		call void class Generic<class Generic2>::Method2 ()
		//call void Base::Method<Generic> ()
	
		ret
	}
}
