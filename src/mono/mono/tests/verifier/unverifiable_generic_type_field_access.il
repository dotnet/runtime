.assembly extern mscorlib
{
	.ver 2:0:0:0
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'unverifiable_generic_type_field_access'
{
	.hash algorithm 0x00008004
	.ver	0:0:0:0
}
.module unverifiable_generic_type_field_access.exe


.class public auto ansi beforefieldinit Tst`2<T,F>
	extends [mscorlib]System.Object
{
	.field	private	!0 t
	.field	private	!1 f

	.method public hidebysig	specialname	rtspecialname 
				 instance default void .ctor ()	cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public hidebysig 
				 instance default void ChangeFields ()	cil managed 
	{
		.maxstack 2
		.locals init (
			!T	V_0,
			!F	V_1)
		ldarg.0 
		ldloca.s 0
		initobj !0
		ldloc.0 
		stfld !0 class Tst`2<!1,!0>::t
		ret 
	}
}

.class public auto ansi beforefieldinit Driver
	extends [mscorlib]System.Object
{
	.method public static	hidebysig 
				 default void Main ()	cil managed 
	{
		.entrypoint
		.maxstack 8
		newobj instance void class Tst`2<int32, string>::.ctor()
		callvirt instance void class Tst`2<int32, string>::ChangeFields()
		ret 
	}

}
