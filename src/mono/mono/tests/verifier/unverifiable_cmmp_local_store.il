.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'unverifiable_cmmp_local_store'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module unverifiable_cmmp_local_store.exe // GUID = {F3A9C2F4-CD8F-43B0-BA4A-C29C71B07966}


  .class private auto ansi beforefieldinit Program
  	extends [mscorlib]System.Object
  {

    // method line 1
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Program::.ctor

    // method line 2
    .method private static hidebysig 
           default void Test<T> (!!T[] t)  cil managed 
    {
        // Method begins at RVA 0x2058
	// Code size 24 (0x18)
	.maxstack 2
	.locals init (
		!!T&	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.0 
	IL_0002:  readonly. 
	IL_0004:  ldelema !!0
	IL_0009:  stloc.0 
	IL_000a:  ldarg.0 
	IL_000b:  ldc.i4.0 
	IL_000c:  ldelem !!0
	IL_0011:  ldloc.0 
	IL_0012:  call void class Program::Foo<!!0> (!!0, [out] !!0&)
	IL_0017:  ret 
    } // end of method Program::Test

    // method line 3
    .method private static hidebysig 
           default void Foo<T> (!!T a, !!T& b)  cil managed 
    {
        // Method begins at RVA 0x207c
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method Program::Foo

    // method line 4
    .method public static hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x207e
	.entrypoint
	// Code size 17 (0x11)
	.maxstack 8
	IL_0000:  ldc.i4.1 
	IL_0001:  newarr [mscorlib]System.Int32
	IL_0006:  dup 
	IL_0007:  ldc.i4.0 
	IL_0008:  ldc.i4.3 
	IL_0009:  stelem.i4 
	IL_000a:  call void class Program::Test<int32> (!!0[])
	IL_000f:  ldc.i4.0 
	IL_0010:  ret 
    } // end of method Program::Main

  } // end of class Program

