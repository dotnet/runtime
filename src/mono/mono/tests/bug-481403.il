.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'bug-481403'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'bug-481403.exe' // GUID = {5A42787F-2088-473C-9915-25768E9D3E6C}


  .class public auto ansi beforefieldinit main
  	extends [mscorlib]System.Object
  {

    // method line 1
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method main::.ctor

    // method line 2
    .method public hidebysig 
           instance default void callee ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  ldstr "callee"
	IL_0005:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_000a:  ret 
    } // end of method main::callee

    // method line 3
    .method public hidebysig 
           instance default void caller ()  cil managed 
    {
        // Method begins at RVA 0x2100
	// Code size 7 (0x7)
	.maxstack 8
//	IL_0000:  ldarg.0 
//	IL_0001:  call instance void class main::callee()
//	IL_0006:  ret 
	IL_0001:  jmp instance void class main::callee()
    } // end of method main::caller

    // method line 4
    .method public hidebysig 
           instance default int32 caller_caller (int32 x, int32 y)  cil managed 
    {
        // Method begins at RVA 0x2108
	// Code size 10 (0xa)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class main::caller()
	IL_0006:  ldarg.1 
	IL_0007:  ldarg.2 
	IL_0008:  add 
	IL_0009:  ret 
    } // end of method main::caller_caller

    // method line 5
    .method public static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x2114
	.entrypoint
	// Code size 26 (0x1a)
	.maxstack 5
	.locals init (
		class main	V_0)
	IL_0000:  newobj instance void class main::'.ctor'()
	IL_0005:  stloc.0 
	IL_0006:  ldstr "bla"
	IL_000b:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_0010:  ldloc.0 
	IL_0011:  ldc.i4.2 
	IL_0012:  ldc.i4.s 0xfffffffe
	IL_0014:  callvirt instance int32 class main::caller_caller(int32, int32)
	IL_0019:  ret 
    } // end of method main::Main

  } // end of class main

