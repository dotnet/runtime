.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'delegate'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module delegate.exe


.class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
{
	.method public hidebysig  specialname  rtspecialname  instance default void .ctor ()  cil managed 
    {
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public final hidebysig virtual instance default int32 Tst ()  cil managed 
	{
		.maxstack 8
		ldc.i4.0 
		ret 
	}
}

.class private auto ansi beforefieldinit Program extends [mscorlib]System.Object
{
	.method public hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
    {
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method private static  hidebysig default int32 Main ()  cil managed 
	{
		.entrypoint
		.maxstack 4
		.locals init (
			class Test	V_0,
			class SimpleDelegate	V_1)

		newobj instance void class Test::.ctor()
		stloc.0 
		ldloc.0 
		ldftn instance int32 class Test::Tst()
		newobj instance void class SimpleDelegate::.ctor(object, native int)
		stloc.1 
		ldstr "OK"
		call void class [mscorlib]System.Console::WriteLine(string)
		ldc.i4.0 
		ret 
	}
}

.class public auto ansi sealed SimpleDelegate extends [mscorlib]System.MulticastDelegate
{
	.method public hidebysig  specialname  rtspecialname instance default void .ctor (object 'object', native int 'method')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot instance default int32 Invoke ()  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot instance default int32 EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
	{
	}
}


