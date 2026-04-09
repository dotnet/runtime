.assembly extern mscorlib
{
  .ver 2:0:0:0
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
.module test.exe // GUID = {A8D53569-66DA-4F08-92BD-595A17CE93AC}


  .class public auto ansi beforefieldinit Foo`1<T>
  	extends [mscorlib]System.Object
  {

    // method line 1
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Foo`1::.ctor

  } // end of class Foo`1

  .class public auto ansi beforefieldinit Bar`1<T>
  	extends class Foo`1
  {

    // method line 2
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class Foo`1::'.ctor'()
	IL_0006:  ret 
    } // end of method Bar`1::.ctor

  } // end of class Bar`1

  .class public auto ansi beforefieldinit Bla
  	extends [mscorlib]System.Object
  {

    // method line 3
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20fc
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Bla::.ctor

    // method line 4
    .method public static hidebysig 
           default void Main ()  cil managed 
    {
        // Method begins at RVA 0x2104
	.entrypoint
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method Bla::Main

  } // end of class Bla

