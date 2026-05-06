/*
This test using a token for the typedef table
*/
.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}
.assembly 'bla'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module ld_type.exe


.class public Test`1<T>
{
	.field public class [mscorlib]System.Collections.Generic.List`1<!T> m_List

	.method public specialname rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret
	}

	.method public int32 Test() cil managed
	{
		ldc.i4.0
		ret
	}

	.method public hidebysig instance void DoStuff<M>() cil managed
	{
		.maxstack  3
		.locals init ()
		ret
	}

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

        .method public static int32 Main ()
        {
                .entrypoint
                .maxstack 2
                .locals init (int32 bla)
		nop
		ldftn instance void class Test`1<string>::DoStuff<int32> ()
		pop
		ldc.i4.0
		ret

        }
}


