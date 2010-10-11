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
.module test.exe // GUID = {3B74EF02-761C-4A31-8BF2-54610FC3ACCC}


  .class public auto ansi beforefieldinit N`2<(class N`2<int16, !T>) T,U>
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
    } // end of method N`2::.ctor

  } // end of class N`2

  .class public auto ansi beforefieldinit Bla
  	extends [mscorlib]System.Object
  {

    // method line 2
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Bla::.ctor

    // method line 3
    .method public static hidebysig 
           default void Main ()  cil managed 
    {
        // Method begins at RVA 0x20fc
	.entrypoint
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method Bla::Main

  } // end of class Bla

