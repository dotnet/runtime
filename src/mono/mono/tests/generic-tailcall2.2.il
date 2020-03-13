.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'generic-tailcall2.2'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'generic-tailcall2.2.exe' // GUID = {84BAE15D-0B00-4F8D-8A54-1C09F6F10C96}


  .class private auto ansi beforefieldinit Gen`1<T>
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
    } // end of method Gen`1::.ctor

    // method line 2
    .method public static  hidebysig 
           default !T[] newArr ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldc.i4.3 
	IL_0001:  newarr !0
	IL_0006:  ret 
    } // end of method Gen`1::newArr

  } // end of class Gen`1

  .class public auto ansi beforefieldinit main
  	extends [mscorlib]System.Object
  {

    // method line 3
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20fc
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method main::.ctor

    // method line 4
    .method public static  hidebysig 
           default string[] work ()  cil managed 
    {
        // Method begins at RVA 0x2104
	// Code size 6 (0x6)
	.maxstack 8
		  tail.
	IL_0000:  call !0[] class Gen`1<string>::newArr()
	IL_0005:  ret 
    } // end of method main::work

    // method line 5
    .method public static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x210c
	.entrypoint
	// Code size 31 (0x1f)
	.maxstack 6
	.locals init (
		string[]	V_0)
	IL_0000:  call string[] class main::work()
	IL_0005:  stloc.0 
	IL_0006:  ldloc.0 
	IL_0007:  callvirt instance class [mscorlib]System.Type object::GetType()
	IL_000c:  ldtoken string[]
	IL_0011:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0016:  beq IL_001d

	IL_001b:  ldc.i4.1 
	IL_001c:  ret 
	IL_001d:  ldc.i4.0 
	IL_001e:  ret 
    } // end of method main::Main

  } // end of class main

