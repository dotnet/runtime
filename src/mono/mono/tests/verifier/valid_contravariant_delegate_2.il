.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'valid_generic_delegate_1'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module valid_covariant_delegate.exe // GUID = {D9327351-CCFF-40F3-A80F-810E3C4283A7}

.class interface public auto ansi beforefieldinit Generic<-T>
{

}

.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method public static hidebysig default !!R Foo<T, R> (!!T arg)  cil managed 
	{
		.maxstack 8
		.locals init (!!R l)
		ldloca 0
		initobj !!R
		ldloc.0
		ret 
	}

	.method public static hidebysig default void Main ()  cil managed 
	{
		.entrypoint
		.maxstack 8

		ldnull 
		ldftn !!1 class Driver::Foo<object, class Generic<object>> (!!0)
		newobj instance void class Dele`2<object, class Generic<string>>::'.ctor'(object, native int)

		pop 
		ret 
	}
}

  .class public auto ansi sealed Dele`2<T,R>
  	extends [mscorlib]System.MulticastDelegate
  {
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
    }

    .method public virtual  hidebysig  newslot 
           instance default !R Invoke (!T t)  runtime managed 
    {
    }

    .method public virtual  hidebysig  newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (!T t, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
    }

    .method public virtual  hidebysig  newslot 
           instance default !R EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
    }
}

