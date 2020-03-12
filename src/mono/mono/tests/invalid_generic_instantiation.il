.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'repro'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module repro.exe // GUID = {5C01F6E6-77DE-4AB1-AD74-6AA56EAA8CD8}


  .class private auto ansi beforefieldinit Foo`1<T>
  	extends [mscorlib]System.Object
  {
    .field  private  !0 t

    // method line 1
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 22 (0x16)
	.maxstack 4
	.locals init (
		!T	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldloca.s 0
	IL_0009:  initobj !0
	IL_000f:  ldloc.0 
	IL_0010:  stfld !0 class Foo`1<!0>::t
	IL_0015:  ret 
    } // end of method Foo`1::.ctor

  } // end of class Foo`1

  .class private auto ansi beforefieldinit Program
  	extends [mscorlib]System.Object
  {

    // method line 2
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2110
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Program::.ctor

    // method line 3
    .method private static hidebysig 
           default void CreateInvalidInstantiation ()  cil managed noinlining 
    {
        // Method begins at RVA 0x2118
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  newobj instance void class Foo`1<void>::'.ctor'()
	IL_0005:  pop 
	IL_0006:  ret 
    } // end of method Program::CreateInvalidInstantiation

    // method line 4
    .method private static hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x2120
	.entrypoint
	// Code size 52 (0x34)
	.maxstack 3
	.locals init (
		int32	V_0)
	.try { // 0
	  IL_0000:  call void class Program::CreateInvalidInstantiation()
	  IL_0005:  ldstr "NO EXCEPTION"
	  IL_000a:  call void class [mscorlib]System.Console::WriteLine(string)
	  IL_000f:  ldc.i4.1 
	  IL_0010:  stloc.0 
	  IL_0011:  leave IL_0032

	  IL_0016:  leave IL_0032

	} // end .try 0
	catch class [mscorlib]System.TypeLoadException { // 0
	  IL_001b:  pop 
	  IL_001c:  ldstr "OK"
	  IL_0021:  call void class [mscorlib]System.Console::WriteLine(string)
	  IL_0026:  ldc.i4.0 
	  IL_0027:  stloc.0 
	  IL_0028:  leave IL_0032

	  IL_002d:  leave IL_0032

	} // end handler 0
	IL_0032:  ldloc.0 
	IL_0033:  ret 
    } // end of method Program::Main

  } // end of class Program

