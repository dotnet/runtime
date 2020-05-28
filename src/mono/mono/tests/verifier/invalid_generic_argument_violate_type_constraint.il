
// unverifiable CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly 'unverifiable_src_ctrs_only_1_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.module bne_with_generic_type_type.exe

.class interface public auto ansi abstract IfaceA
{
} 

.class interface public auto ansi abstract IfaceB
{
} 

.class public auto ansi Class extends [mscorlib]System.Object
{
	.method public hidebysig  specialname  rtspecialname instance default void '.ctor' ()  cil managed 
	{
	    .maxstack 8
		ldarg.0 
	    call instance void object::'.ctor'()
	    ret 
	}
} 

.class public auto ansi DefaultArgument	extends Class implements IfaceA, IfaceB
{
	.method public hidebysig  specialname  rtspecialname instance default void '.ctor' ()  cil managed 
	{
	    .maxstack 8
		ldarg.0 
	    call instance void Class::'.ctor'()
	    ret 
	}
} 



.class public auto ansi beforefieldinit Test < (IfaceA)  T>
        extends [mscorlib]System.Object
{

	.method public hidebysig  specialname  rtspecialname instance default void '.ctor' ()  cil managed 
	{
	    .maxstack 8
		ldarg.0 
	    call instance void object::'.ctor'()
	    ret 
	}

	.method public static void Method ()
	{
		.locals init ()
		ret
	}
}

.class public auto ansi beforefieldinit Test2< T >
        extends [mscorlib]System.Object
{
	.method public static void Method ()
	{
		.locals init ()
		call void class Test<!T>::Method ()
		ret
	}
}

.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8

	call void class Test2< object >::Method()

	ldc.i4.0
	ret 
}

