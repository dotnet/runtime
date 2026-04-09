.assembly extern mscorlib
{
  .ver 2:0:5:0
  .publickeytoken = (7C EC 85 D7 BE A7 79 8E ) // |.....y.
}
.assembly 'gtest-253'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'gtest-253.exe' // GUID = {F1C7E6F5-B296-496E-997E-F323C3A6865B}


  .class interface public auto ansi abstract IExtensible`1<T>
  {

    // method line 1
    .method public virtual  hidebysig  newslot  abstract 
           instance default void AddAll<(!T) U> (class [mscorlib]System.Collections.Generic.IEnumerable`1<!!U> items)  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IExtensible`1::AddAll

  } // end of class IExtensible`1

  .class public auto ansi abstract beforefieldinit CollectionValueTester`2<(class IExtensible`1<!S>) R,S>
  	extends [mscorlib]System.Object
  {
    .field  family  !0 collection

    // method line 2
    .method family hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method CollectionValueTester`2::.ctor

  } // end of class CollectionValueTester`2

  .class public auto ansi beforefieldinit ExtensibleTester`1<(class IExtensible`1<int32>) U>
  	extends class CollectionValueTester`2<!0,int32>
  {

    // method line 3
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' (!U u)  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class CollectionValueTester`2<!U, int32>::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld !0 class CollectionValueTester`2<!0,int32>::collection
	IL_000d:  ret 
    } // end of method ExtensibleTester`1::.ctor

    // method line 4
    .method public hidebysig 
           instance default void Direct ()  cil managed 
    {
        // Method begins at RVA 0x2104
	// Code size 24 (0x18)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldflda !0 class CollectionValueTester`2<!0,int32>::collection
	IL_0006:  ldc.i4.0 
	IL_0007:  newarr [mscorlib]System.Int32
	IL_000c:  constrained. !0
	IL_0012:  callvirt instance void class IExtensible`1<int32>::AddAll<int32> (class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
	IL_0017:  ret 
    } // end of method ExtensibleTester`1::Direct

  } // end of class ExtensibleTester`1

  .class public auto ansi beforefieldinit Extensible`1<V>
  	extends [mscorlib]System.Object
  	implements class IExtensible`1<!0>  {

    // method line 5
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2120
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Extensible`1::.ctor

    // method line 6
    .method public final  virtual  hidebysig  newslot 
           instance default void AddAll<(!V) W> (class [mscorlib]System.Collections.Generic.IEnumerable`1<!!W> items)  cil managed 
    {
        // Method begins at RVA 0x2128
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method Extensible`1::AddAll

  } // end of class Extensible`1

  .class private auto ansi beforefieldinit X
  	extends [mscorlib]System.Object
  {

    // method line 7
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x212c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method X::.ctor

    // method line 8
    .method private static  hidebysig 
           default void Main ()  cil managed 
    {
        // Method begins at RVA 0x2134
	.entrypoint
	// Code size 20 (0x14)
	.maxstack 2
	.locals init (
		class Extensible`1<int32>	V_0,
		class ExtensibleTester`1<class Extensible`1<int32>>	V_1)
	IL_0000:  newobj instance void class Extensible`1<int32>::'.ctor'()
	IL_0005:  stloc.0 
	IL_0006:  ldloc.0 
	IL_0007:  newobj instance void class ExtensibleTester`1<class Extensible`1<int32>>::'.ctor'(!0)
	IL_000c:  stloc.1 
	IL_000d:  ldloc.1 
	IL_000e:  callvirt instance void class ExtensibleTester`1<class Extensible`1<int32>>::Direct()
	IL_0013:  ret 
    } // end of method X::Main

  } // end of class X

