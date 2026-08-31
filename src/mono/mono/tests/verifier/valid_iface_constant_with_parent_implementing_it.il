.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'repro'
{

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module repro.exe

.class interface public auto ansi abstract INameable {}
.class public auto ansi abstract Base implements INameable {}
.class public auto ansi abstract Derived extends Base {}

 .class public auto ansi Nameables<(class INameable) T>
{
	.method public specialname rtspecialname instance default void '.ctor' () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::'.ctor'()
		ret 
	}
}


.class auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method public static hidebysig default int32 Test ()  cil managed
	{
		.maxstack 8
		.locals init ()
		newobj instance void class Nameables<class Derived>::'.ctor'()
		pop
		ldc.i4.0
		ret
	}
	

	.method private static hidebysig default int32 Main ()  cil managed 
	{
		.entrypoint
		.maxstack 8
		.locals init ()

		call int32 class Driver::Test ()
		ret
	}
}

