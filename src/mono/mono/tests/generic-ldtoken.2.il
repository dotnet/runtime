.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'generic-ldtoken.2'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'generic-ldtoken.2.exe' // GUID = {816E886F-1485-43E8-AC46-501D301BC67A}


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
           instance default class [mscorlib]System.Type typeOfT ()  cil managed 
    {
        // Method begins at RVA 0x20fc
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  ldtoken !T
		  nop
	IL_0005:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_000a:  ret 
    } // end of method Gen`1::typeOfT

    // method line 4
    .method public hidebysig 
           instance default class [mscorlib]System.Type typeOfGenT ()  cil managed 
    {
        // Method begins at RVA 0x2108
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  ldtoken class Gen`1<!T>
		  nop
	IL_0005:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_000a:  ret 
    } // end of method Gen`1::typeOfGenT

  } // end of class Gen`1

  .class public auto ansi beforefieldinit main
  	extends [mscorlib]System.Object
  {

    // method line 5
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2114
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
        // Method begins at RVA 0x211c
	.entrypoint
	// Code size 54 (0x36)
	.maxstack 9
	.locals init (
		class Gen`1<class ClassA>	V_0)
	IL_0000:  newobj instance void class Gen`1<class ClassA>::'.ctor'()
	IL_0005:  stloc.0 
	IL_0006:  ldloc.0 
	IL_0007:  callvirt instance class [mscorlib]System.Type class Gen`1<class ClassA>::typeOfT()
	IL_000c:  ldtoken ClassA
	IL_0011:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0016:  beq IL_001d

	IL_001b:  ldc.i4.1 
	IL_001c:  ret 
	IL_001d:  ldloc.0 
	IL_001e:  callvirt instance class [mscorlib]System.Type class Gen`1<class ClassA>::typeOfGenT()
	IL_0023:  ldtoken class Gen`1<class ClassA>
	IL_0028:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_002d:  beq IL_0034

	IL_0032:  ldc.i4.1 
	IL_0033:  ret 
	IL_0034:  ldc.i4.0 
	IL_0035:  ret 
    } // end of method main::Main

  } // end of class main

