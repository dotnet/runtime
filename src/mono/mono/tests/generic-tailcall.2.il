.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'generic-tailcall.2'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'generic-tailcall.2.exe' // GUID = {06726C6C-6C85-4737-B731-8585E31F7647}


  .class public auto ansi beforefieldinit ClassA
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
    } // end of method ClassA::.ctor

  } // end of class ClassA

  .class public auto ansi beforefieldinit Gen`1<T>
  	extends [mscorlib]System.Object
  {

    // method line 2
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Gen`1::.ctor

    // method line 3
    .method public hidebysig 
           instance default !T[] caller (int32 x)  cil managed 
    {
        // Method begins at RVA 0x20fc
	// Code size 10 (0xa)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldc.i4.1 
	IL_0003:  add 
	IL_0004:  tail. call instance !0[] class Gen`1<!T>::callee(int32)
	IL_0009:  ret 
    } // end of method Gen`1::caller

    // method line 4
    .method public hidebysig 
           instance default !T[] callee (int32 x)  cil managed 
    {
        // Method begins at RVA 0x2108
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  newarr !0
	IL_0006:  ret 
    } // end of method Gen`1::callee

  } // end of class Gen`1

  .class public auto ansi beforefieldinit main
  	extends [mscorlib]System.Object
  {

    // method line 5
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2110
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method main::.ctor

    // method line 6
    .method public static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x2118
	.entrypoint
	// Code size 48 (0x30)
	.maxstack 7
	.locals init (
		class Gen`1<class ClassA>	V_0,
		class ClassA[]	V_1)
	IL_0000:  newobj instance void class Gen`1<class ClassA>::'.ctor'()
	IL_0005:  stloc.0 
	IL_0006:  ldloc.0 
	IL_0007:  ldc.i4.3 
	IL_0008:  callvirt instance !0[] class Gen`1<class ClassA>::caller(int32)
	IL_000d:  stloc.1 
	IL_000e:  ldloc.1 
	IL_000f:  callvirt instance class [mscorlib]System.Type object::GetType()
	IL_0014:  ldtoken class ClassA[]
	IL_0019:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_001e:  bne.un IL_002c

	IL_0023:  ldloc.1 
	IL_0024:  ldlen 
	IL_0025:  conv.i4 
	IL_0026:  ldc.i4.4 
	IL_0027:  beq IL_002e

	IL_002c:  ldc.i4.1 
	IL_002d:  ret 
	IL_002e:  ldc.i4.0 
	IL_002f:  ret 
    } // end of method main::Main

  } // end of class main

