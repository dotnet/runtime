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

    .method public hidebysig instance default int32 checkEqual () cil managed
    {
        ldtoken method instance int32 class Gen`1<!T>::checkEqual()
		ldtoken class Gen`1<!T>
	call [mscorlib]System.Reflection.MethodBase [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle (valuetype [mscorlib]System.RuntimeMethodHandle,
	valuetype [mscorlib]System.RuntimeTypeHandle)

        ldtoken method instance int32 class Gen`1<class ClassA>::checkEqual ()
		ldtoken class Gen`1<class ClassA>
	call [mscorlib]System.Reflection.MethodBase [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle (valuetype [mscorlib]System.RuntimeMethodHandle,
	valuetype [mscorlib]System.RuntimeTypeHandle)
	//	call instance int32 [mscorlib]System.RuntimeMethodHandle::Equals ([mscorlib]System.RuntimeMethodHandle)
	ceq
	ret
    }

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
		  ldc.i4.1
	IL_0000:  newobj instance void class Gen`1<class ClassA>::'.ctor'()
		  callvirt instance int32 class Gen`1<class ClassA>::checkEqual()
		  sub
	IL_0035:  ret 
    } // end of method main::Main

  } // end of class main

