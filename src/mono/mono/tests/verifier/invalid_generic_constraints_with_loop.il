.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'gtest-514'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'broken.exe'


.namespace test2
{
.class public auto ansi beforefieldinit Test`3<
	.ctor (!U) T,
	.ctor (!V) U,
	.ctor (!T, class [mscorlib]System.IDisposable) V>
	extends [mscorlib]System.Object
{
	.method public hidebysig specialname rtspecialname instance default void '.ctor' ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::'.ctor'()
		ret 
	}

	.method public hidebysig 
	   instance default void Method ()  cil managed 
	{
	.maxstack 8
	.locals init (
	class [mscorlib]System.IDisposable	V_0,
	class [mscorlib]System.IDisposable	V_1,
	class [mscorlib]System.IDisposable	V_2,
	!T	V_3,
	!U	V_4,
	!V	V_5)
	ldloca.s 3
	initobj !0
	ldloc.3 
	box !0
	brfalse L_0

	ldloca.s 3
	initobj !0
	ldloc.3 
	br.s L_1

L_0:
	call !!0 class [mscorlib]System.Activator::CreateInstance<!0> ()
L_1:
	box !0
	stloc.0 
	ldloca.s 4
	initobj !1
	ldloc.s 4
	box !1
	brfalse L_2

	ldloca.s 4
	initobj !1
	ldloc.s 4
	br.s L_3

L_2:
	call !!0 class [mscorlib]System.Activator::CreateInstance<!1> ()
L_3:
	box !1
	stloc.1 
	ldloca.s 5
	initobj !2
	ldloc.s 5
	box !2
	brfalse L_4

	ldloca.s 5
	initobj !2
	ldloc.s 5
	br.s L_5

L_4:
	call !!0 class [mscorlib]System.Activator::CreateInstance<!2> ()
L_5:
	box !2
	stloc.2 
	ret 
	}
}
}

.namespace test2
{
.class private auto ansi beforefieldinit MainClass extends [mscorlib]System.Object implements [mscorlib]System.IDisposable  {
	.method public hidebysig specialname rtspecialname instance default void '.ctor' ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::'.ctor'()
		ret 
	}

	.method public final virtual hidebysig newslot instance default void Dispose ()  cil managed 
	{
		.maxstack 8
		ret 
	}

	.method private static hidebysig default void Main ()  cil managed 
	{
		.entrypoint
		.maxstack 8
		newobj instance void class test2.Test`3<class test2.MainClass, class test2.MainClass, class test2.MainClass>::'.ctor'()
		callvirt instance void class test2.Test`3<class test2.MainClass, class test2.MainClass, class test2.MainClass>::Method()
		ret 
	}
}
}

