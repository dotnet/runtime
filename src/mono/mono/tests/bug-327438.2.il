
.assembly extern mscorlib
{
 .ver 2:0:0:0
 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'constaint_checks'
{
 .hash algorithm 0x00008004
 .ver 0:0:0:0
}
.module constraint_checks.exe


.class public auto ansi beforefieldinit Subject<T> extends [mscorlib]System.Object
{
	.method public hidebysig specialname rtspecialname 
	instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}
}

.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public static hidebysig  default void Ldtoken () cil managed 
	{
		ldtoken class Subject<valuetype [mscorlib]System.Nullable<int32>>
		pop
		ldc.i4.0 
		ret 
	}

	.method public static hidebysig  default int32 Main () cil managed 
	{
		.entrypoint
		.try {
			call void class Driver::Ldtoken()
			leave RET
		}
        catch [mscorlib]System.Exception {
			pop
			leave RET
		}

RET:	ldc.i4.0 
		ret 
	}
}


