.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'generic-delegate2-lib.2'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'generic-delegate2-lib.2.dll' // GUID = {C471C5E6-B265-42A8-9471-E4BDD124EC49}


  .class public auto ansi beforefieldinit Gen`1<T>
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
    } // end of method Gen`1::.ctor

    // method line 2
    .method public hidebysig 
           instance default object newArr (int32 i)  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  newarr !0
	IL_0006:  ret 
    } // end of method Gen`1::newArr

    // method line 3
    .method public hidebysig 
           instance default class ArrDel newDel ()  cil managed 
    {
        // Method begins at RVA 0x20fc
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldftn instance object class Gen`1<!T>::newArr(int32)
	IL_0007:  newobj instance void class ArrDel::.ctor(object, native int)
	IL_000c:  ret 
    } // end of method Gen`1::newDel

    // method line 4
    .method public hidebysig 
           instance default class ObjArrDel newObjDel ()  cil managed 
    {
        // Method begins at RVA 0x210c
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldnull 
	IL_0001:  ldftn instance object class Gen`1<!T>::newArr(int32)
	IL_0007:  newobj instance void class ObjArrDel::.ctor(object, native int)
	IL_000c:  ret 
    } // end of method Gen`1::newObjDel

    // method line 5
    .method public hidebysig 
           instance default class StrArrDel newStrDel ()  cil managed 
    {
        // Method begins at RVA 0x2110
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldnull 
	IL_0001:  ldftn instance object class Gen`1<!T>::newArr(int32)
	IL_0007:  newobj instance void class StrArrDel::.ctor(object, native int)
	IL_000c:  ret 
    } // end of method Gen`1::newStrDel

  } // end of class Gen`1

  .class public auto ansi sealed ArrDel
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 6
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ArrDel::.ctor

    // method line 7
    .method public virtual  hidebysig  newslot 
           instance default object Invoke (int32 i)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ArrDel::Invoke

    // method line 8
    .method public virtual  hidebysig  newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (int32 i, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ArrDel::BeginInvoke

    // method line 9
    .method public virtual  hidebysig  newslot 
           instance default object EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ArrDel::EndInvoke

  } // end of class ArrDel

  .class public auto ansi sealed ObjArrDel
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 10
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ObjArrDel::.ctor

    // method line 11
    .method public virtual  hidebysig  newslot 
           instance default object Invoke (class Gen`1<object> g, int32 i)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ObjArrDel::Invoke

    // method line 12
    .method public virtual  hidebysig  newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class Gen`1<object> g, int32 i, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ObjArrDel::BeginInvoke

    // method line 13
    .method public virtual  hidebysig  newslot 
           instance default object EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ObjArrDel::EndInvoke

  } // end of class ObjArrDel

  .class public auto ansi sealed StrArrDel
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 14
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method StrArrDel::.ctor

    // method line 15
    .method public virtual  hidebysig  newslot 
           instance default object Invoke (class Gen`1<string> g, int32 i)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method StrArrDel::Invoke

    // method line 16
    .method public virtual  hidebysig  newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class Gen`1<string> g, int32 i, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method StrArrDel::BeginInvoke

    // method line 17
    .method public virtual  hidebysig  newslot 
           instance default object EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method StrArrDel::EndInvoke

  } // end of class StrArrDel

