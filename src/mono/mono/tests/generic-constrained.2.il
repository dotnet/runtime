.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'bla'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module bla.exe // GUID = {B0BB2CAE-B57B-48F6-BDD2-2B93A6A7F57F}


  .class interface public auto ansi abstract IGen`1<IGenT>
  {

    // method line 1
    .method public virtual  hidebysig  newslot  abstract 
           instance default int32 bla ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IGen`1::bla

  } // end of class IGen`1

  .class public sequential ansi sealed beforefieldinit Gen`1<GenT>
  	extends [mscorlib]System.ValueType
  	implements class IGen`1<!0>  {

    // method line 2
    .method public final  virtual  hidebysig  newslot 
           instance default int32 bla ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 3 (0x3)
	.maxstack 8
	IL_0000:  ldc.i4.s 0x7b
	IL_0002:  ret 
    } // end of method Gen`1::bla

  } // end of class Gen`1

  .class private auto ansi beforefieldinit A`2<S,T>
  	extends [mscorlib]System.Object
  {

    // method line 3
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20f0
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method A`2::.ctor

    // method line 4
    .method public hidebysig 
           instance default int32 Foo (valuetype Gen`1<!T> t)  cil managed 
    {
        // Method begins at RVA 0x20f8
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarga.s 1
		  constrained. valuetype Gen`1<!T>
	IL_0006:  callvirt instance int32 class IGen`1<!T>::bla()
	IL_0007:  ret 
    } // end of method A`2::Foo

  } // end of class A`2

  .class public auto ansi beforefieldinit main
  	extends [mscorlib]System.Object
  {

    // method line 5
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2104
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
        // Method begins at RVA 0x210c
	.entrypoint
	// Code size 24 (0x18)
	.maxstack 3
	.locals init (
		valuetype Gen`1<string>	V_0,
		class A`2<object, string>	V_1)
	IL_0000:  ldloca.s 0
	IL_0002:  initobj valuetype Gen`1<string>
	IL_0008:  newobj instance void class A`2<object, string>::'.ctor'()
	IL_000d:  stloc.1 
	IL_000e:  ldloc.1 
	IL_000f:  ldloc.0 
	IL_0010:  callvirt instance int32 class A`2<object, string>::Foo(valuetype Gen`1<!1>)
	IL_0015:  pop 
	IL_0016:  ldc.i4.0 
	IL_0017:  ret 
    } // end of method main::Main

  } // end of class main

