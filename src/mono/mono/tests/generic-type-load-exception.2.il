.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'typeloadexcgeneric'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module typeloadexcgeneric.exe // GUID = {877F6CE7-B3D8-4CC7-AEB0-DA6A9DB96059}


  .class interface private auto ansi abstract IFoo
  {

    // method line 1
    .method public virtual  hidebysig  newslot  abstract 
           instance default int32 A ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IFoo::A

    // method line 2
    .method public virtual  hidebysig  newslot  abstract 
           instance default int32 B ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IFoo::B

  } // end of class IFoo

  .class private auto ansi beforefieldinit Foo`1<T>
  	extends [mscorlib]System.Object
  	implements IFoo  {

    // method line 3
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Foo`1::.ctor

    // method line 4
    .method public final  virtual  hidebysig  newslot 
           instance default int32 A ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldc.i4.1 
	IL_0001:  ret 
    } // end of method Foo`1::A
  } // end of class Foo`1

  .class private auto ansi beforefieldinit Bar`1<T>
  	extends [mscorlib]System.Object
  {

    // method line 6
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20fc
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Bar`1::.ctor

    // method line 7
    .method public hidebysig 
           instance default void work ()  cil managed 
    {
        // Method begins at RVA 0x2104
	// Code size 33 (0x21)
	.maxstack 5
	.locals init (
		class Foo`1<!T>	V_0)
	IL_0000:  newobj instance void class Foo`1<!T>::'.ctor'()
	IL_0005:  stloc.0 
	IL_0006:  ldstr "A "
	IL_000b:  ldloc.0 
	IL_000c:  callvirt instance int32 class Foo`1<!T>::A()
	IL_0011:  box [mscorlib]System.Int32
	IL_0016:  call string string::Concat(object, object)
	IL_001b:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_0020:  ret 
    } // end of method Bar`1::work

  } // end of class Bar`1

  .class public auto ansi beforefieldinit main
  	extends [mscorlib]System.Object
  {

    // method line 8
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2134
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method main::.ctor

    // method line 9
    .method public static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x213c
	.entrypoint
	// Code size 34 (0x22)
	.maxstack 4
	.locals init (
		class Bar`1<string>	V_0,
		int32	V_1)
	IL_0000:  newobj instance void class Bar`1<string>::'.ctor'()
	IL_0005:  stloc.0 
	.try { // 0
	  IL_0006:  ldloc.0 
	  IL_0007:  callvirt instance void class Bar`1<string>::work()
	  IL_000c:  leave IL_001e

	} // end .try 0
	catch class [mscorlib]System.TypeLoadException { // 0
	  IL_0011:  pop 
	  IL_0012:  ldc.i4.0 
	  IL_0013:  stloc.1 
	  IL_0014:  leave IL_0020

	  IL_0019:  leave IL_001e

	} // end handler 0
	IL_001e:  ldc.i4.1 
	IL_001f:  ret 
	IL_0020:  ldloc.1 
	IL_0021:  ret 
    } // end of method main::Main

  } // end of class main

