.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'valid_merge_boxed_values'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module valid_merge_boxed_values.exe // GUID = {47C47D22-2F3F-4B04-AED9-2BC28A196BCF}

.class private auto ansi sealed ThisDele extends [mscorlib]System.MulticastDelegate
{
	.method public hidebysig specialname rtspecialname  instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
	{
	}

	.method public virtual hidebysig newslot instance default bool Invoke (object obj)  runtime managed 
	{
	}

	.method public virtual hidebysig newslot  instance default class [mscorlib]System.IAsyncResult BeginInvoke (object obj, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
	{
	}

	.method public virtual hidebysig newslot  instance default bool EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
	{
	}
}

.class interface private auto ansi abstract IFace
{
	.method public virtual hidebysig newslot abstract instance default void Foo (class ThisDele b)  cil managed 
	{
	}
}

.class private sequential ansi sealed beforefieldinit MyList extends [mscorlib]System.ValueType implements IFace
{
	.pack 0
	.size 1

	.method public final virtual hidebysig newslot 
	instance default void Foo (class ThisDele b)  cil managed 
	{
		.maxstack 8
		IL_0000:  ret 
	}
}

.class private auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.field private static  class ThisDele '<>f__mg$cache0'

	.method public hidebysig specialname rtspecialname  instance default void '.ctor' ()  cil managed 
	{
		.maxstack 8
		IL_0000:  ldarg.0 
		IL_0001:  call instance void object::'.ctor'()
		IL_0006:  ret 
	}

	.method private static hidebysig default bool Dele (object o)  cil managed 
	{
		.maxstack 8
		IL_0000:  ldc.i4.0 
		IL_0001:  ret 
	}

	.method private static hidebysig default void Foo (class IFace a, class [mscorlib]System.Action b)  cil managed 
	{
		.maxstack 8
		IL_0000:  ret 
	}

	.method private static hidebysig default void Main ()  cil managed 
	{
		.entrypoint
		.maxstack 3
		.locals init (
			valuetype MyList	V_0,
			valuetype MyList	V_1)
		IL_0000:  ldloca.s 1
		IL_0002:  initobj MyList
		IL_0008:  ldloc.1 
		IL_0009:  stloc.0 
		IL_000a:  ldloc.0 
		IL_000b:  box MyList
		IL_0010:  ldsfld class ThisDele Driver::'<>f__mg$cache0'
		IL_0015:  brtrue.s IL_0028

		IL_0017:  ldnull 
		IL_0018:  ldftn bool class Driver::Dele(object)
		IL_001e:  newobj instance void class ThisDele::'.ctor'(object, native int)
		IL_0023:  stsfld class ThisDele Driver::'<>f__mg$cache0'
		IL_0028:  ldsfld class ThisDele Driver::'<>f__mg$cache0'
		IL_002d:  callvirt instance void class IFace::Foo(class ThisDele)
		IL_0032:  ret 
	}
}

