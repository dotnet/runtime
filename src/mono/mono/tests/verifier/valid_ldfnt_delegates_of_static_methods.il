.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'ltest-07'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'ltest-07.exe'

.class public auto ansi sealed StringDelegate extends [mscorlib]System.MulticastDelegate
{
	.method public hidebysig  specialname  rtspecialname instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot instance default string Invoke ()  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot instance default string EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
	{
	}
}

.class public auto ansi sealed StringStringDelegate extends [mscorlib]System.MulticastDelegate
{
	.method public hidebysig  specialname  rtspecialname instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot instance default string Invoke (string str)  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot instance default class [mscorlib]System.IAsyncResult BeginInvoke (string str, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot instance default string EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
	{
	}
}

.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method public static hidebysig default string Method1 (string str) cil managed 
	{
		ldarg.0
		ret
	}

	.method public static hidebysig default void Main ()  cil managed 
	{
		.entrypoint
		.maxstack 8
		.locals init (string loc_0)

		ldnull
		ldftn string class Driver::Method1(string)
		newobj instance void class StringDelegate::'.ctor'(object, native int)
		callvirt instance string class StringDelegate::Invoke()
		call void class [mscorlib]System.Console::WriteLine(string)

		ldloc.0
		ldftn string class Driver::Method1(string)
		newobj instance void class StringDelegate::'.ctor'(object, native int)
		callvirt instance string class StringDelegate::Invoke()
		call void class [mscorlib]System.Console::WriteLine(string)


		ldnull
		ldftn string class Driver::Method1(string)
		newobj instance void class StringStringDelegate::'.ctor'(object, native int)
		ldstr "test"
		callvirt instance string class StringStringDelegate::Invoke(string)
		call void class [mscorlib]System.Console::WriteLine(string)

		ret
	}
}


