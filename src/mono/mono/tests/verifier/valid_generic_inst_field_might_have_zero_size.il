.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'gtest-106'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'gtest-106.exe' // GUID = {6DDC13E4-6237-4A6D-A5D7-ABE1083C4C55}


  .class public sequential ansi sealed beforefieldinit KeyValuePair`2<X,Y>
  	extends [mscorlib]System.ValueType
  {
    .field  private  unsigned int8 $PRIVATE$

    // method line 1
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' (!X x, !Y y)  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method KeyValuePair`2::.ctor

  } // end of class KeyValuePair`2

  .class interface public auto ansi abstract IComparer`1<T>
  {

    // method line 2
    .method public virtual  hidebysig  newslot  abstract 
           instance default int32 Compare (!T x)  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IComparer`1::Compare

  } // end of class IComparer`1

  .class public auto ansi beforefieldinit KeyValuePairComparer`2<K,V>
  	extends [mscorlib]System.Object
  	implements class IComparer`1<valuetype KeyValuePair`2<!0,!1>>  {

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
    } // end of method KeyValuePairComparer`2::.ctor

    // method line 4
    .method public final  virtual  hidebysig  newslot 
           instance default int32 Compare (valuetype KeyValuePair`2<!K, !V> a)  cil managed 
    {
        // Method begins at RVA 0x20f8
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldc.i4.0 
	IL_0001:  ret 
    } // end of method KeyValuePairComparer`2::Compare

  } // end of class KeyValuePairComparer`2

  .class public auto ansi beforefieldinit TreeBag`1<T>
  	extends [mscorlib]System.Object
  {
    .field  private  class IComparer`1<!0> comparer
    .field  private  !0 item

    // method line 5
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' (class IComparer`1<!T> comparer, !T item)  cil managed 
    {
        // Method begins at RVA 0x20fc
	// Code size 21 (0x15)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld class IComparer`1<!0> class TreeBag`1<!0>::comparer
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.2 
	IL_000f:  stfld !0 class TreeBag`1<!0>::item
	IL_0014:  ret 
    } // end of method TreeBag`1::.ctor

    // method line 6
    .method public hidebysig 
           instance default int32 Find ()  cil managed 
    {
        // Method begins at RVA 0x2114
	// Code size 18 (0x12)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class IComparer`1<!0> class TreeBag`1<!0>::comparer
	IL_0006:  ldarg.0 
	IL_0007:  ldfld !0 class TreeBag`1<!0>::item
	IL_000c:  callvirt instance int32 class IComparer`1<!T>::Compare(!0)
	IL_0011:  ret 
    } // end of method TreeBag`1::Find

  } // end of class TreeBag`1

  .class public auto ansi beforefieldinit X
  	extends [mscorlib]System.Object
  {

    // method line 7
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2128
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method X::.ctor

    // method line 8
    .method public static  hidebysig 
           default void Main ()  cil managed 
    {
        // Method begins at RVA 0x2130
	.entrypoint
	// Code size 32 (0x20)
	.maxstack 5
	.locals init (
		valuetype KeyValuePair`2<int32, int32>	V_0,
		class KeyValuePairComparer`2<int32, int32>	V_1,
		class TreeBag`1<valuetype KeyValuePair`2<int32, int32>>	V_2)
	IL_0000:  ldloca.s 0
	IL_0002:  ldc.i4.3 
	IL_0003:  ldc.i4.s 0x59
	IL_0005:  call instance void valuetype KeyValuePair`2<int32, int32>::'.ctor'(!0, !1)
	IL_000a:  newobj instance void class KeyValuePairComparer`2<int32, int32>::'.ctor'()
	IL_000f:  stloc.1 
	IL_0010:  ldloc.1 
	IL_0011:  ldloc.0 
	IL_0012:  newobj instance void class TreeBag`1<valuetype KeyValuePair`2<int32, int32>>::'.ctor'(class IComparer`1<!0>, !0)
	IL_0017:  stloc.2 
	IL_0018:  ldloc.2 
	IL_0019:  callvirt instance int32 class TreeBag`1<valuetype KeyValuePair`2<int32, int32>>::Find()
	IL_001e:  pop 
	IL_001f:  ret 
    } // end of method X::Main

  } // end of class X

