.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'find-method'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'find-method.exe' // GUID = {42B655E5-110B-419B-8B7B-B523C11EDAB3}


  .class private auto ansi beforefieldinit X`2<T,U>
  	extends [mscorlib]System.Object
  {

    // method line 1
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method X`2::instance default void .ctor () 

    // method line 2
    .method public virtual  hidebysig  newslot 
           instance default int32 f (!T t, !U u)  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldc.i4.1 
	IL_0001:  ret 
    } // end of method X`2::instance default int32 f (!T t, !U u) 

    // method line 3
    .method public hidebysig 
           instance default int32 f (int32 i, int64 l)  cil managed 
    {
        // Method begins at RVA 0x20f8
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldc.i4.2 
	IL_0001:  ret 
    } // end of method X`2::instance default int32 f (int32 i, int64 l) 

  } // end of class X`2

  .class private auto ansi beforefieldinit Y`1<V>
  	extends class X`2<int32, !0>
  {

    // method line 4
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x20fc
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class X`2<int32, !V>::.ctor()
	IL_0006:  ret 
    } // end of method Y`1::instance default void .ctor () 
  } // end of class Y`1

  .class private auto ansi beforefieldinit m
  	extends [mscorlib]System.Object
  {

    // method line 6
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x2108
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method m::instance default void .ctor () 

    // method line 7
    .method private static  hidebysig 
           default void Main ()  cil managed 
    {
        // Method begins at RVA 0x2110
	.entrypoint
	// Code size 27 (0x1b)
	.maxstack 4
	.locals init (
		class Y`1<int32>	V_0)
	IL_0000:  newobj instance void class Y`1<int32>::.ctor()
	IL_0005:  stloc.0 
	IL_0006:  ldloc.0 
	IL_0007:  ldc.i4.0 
	IL_0008:  ldc.i4.1 
	IL_0009:  callvirt instance int32 class Y`1<int32>::f(int32, !0)
	IL_000e:  ldc.i4.1 
	IL_000f:  beq IL_001a

	IL_0014:  newobj instance void class [mscorlib]System.Exception::.ctor()
	IL_0019:  throw 
	IL_001a:  ret 
    } // end of method m::default void Main () 

  } // end of class m

