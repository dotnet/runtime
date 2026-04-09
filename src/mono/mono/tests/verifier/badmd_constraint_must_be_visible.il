.assembly extern mscorlib
{
.ver 4:0:0:0
.publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'test'
{
.custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

.hash algorithm 0x00008004
.ver  0:0:0:0
}
.module test.exe // GUID = {EBF85B14-B518-4DFE-8A10-5BBC0D700C94}

.class private auto ansi beforefieldinit Foo`1<(class [mscorlib]Mono.Interop.IUnknown) T> extends [mscorlib]System.Object
{
	.method public hidebysig specialname rtspecialname  instance default void '.ctor' ()  cil managed 
	{
		.maxstack 8
		IL_0000:  ldarg.0 
		IL_0001:  call instance void object::'.ctor'()
		IL_0006:  ret 
	}
}

.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method private static hidebysig default void Main ()  cil managed 
	{
		.entrypoint
		.maxstack 8
		ldtoken Foo`1
		call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call void class [mscorlib]System.Console::WriteLine(object)
		IL_0000:  ret 
	}
}

