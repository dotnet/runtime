// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }

// Metadata version: v2.0.40216
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .hash = (F2 9A DF 4E 4F 6B AB 87 31 14 43 DD C2 C8 D5 1E   // ...NOk..1.C.....
           83 95 73 F7 )                                     // ..s.
  .ver 2:0:3600:0
}
.assembly MethodsLib2
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 

  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {90D94B96-3C0E-4A14-8E27-8AD41717FB3D}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x041D0000


// =============== CLASS MEMBERS DECLARATION ===================


.class interface public abstract auto ansi IMinusT<-([mscorlib]System.Object) MinusT>
{
} // end of class IMinusT

.class interface public abstract auto ansi IPlusT<+([mscorlib]System.Object) PlusT>
{
} // end of class IPlusT


// POSITIVE tests

.class interface public auto ansi beforefieldinit Test001PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual instance void 
  method1<(class IMinusT<!PlusT>) M>(class IMinusT<!PlusT> t) cil managed
 
  {
  } // end of method Test001PlusT::method1

} // end of class Test101PlusT


.class interface public abstract auto ansi Test002PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual 
       instance !PlusT[] method2<(class IMinusT<!PlusT[]>) M>(class IMinusT<!PlusT[]> t) cil managed
  {
  } // end of method Test102PlusT::method2

} // end of class Test102PlusT




.class interface public abstract auto ansi Test001MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<!MinusT[]> method1<(class IPlusT<!MinusT>) M>(!MinusT t) cil managed
  {
  } // end of method Test101MinusT::method1

} // end of class Test101MinusT


.class interface public abstract auto ansi Test002MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<!MinusT>[] method2<(class IMinusT<!MinusT>) M>() cil managed
  {
  } // end of method Test102MinusT::method2

} // end of class Test102MinusT




// NEGATIVE tests


.class interface public auto ansi beforefieldinit Test101PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual instance void 
          method1<(class IPlusT<!PlusT>) M>(class IMinusT<!PlusT> t) cil managed
  {
  } // end of method Test001PlusT::method1

} // end of class Test101PlusT


.class interface public abstract auto ansi Test102PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual 
       instance !PlusT[] method2<(class IPlusT<!PlusT>) M>(class IMinusT<!PlusT[]> t) cil managed
  {
  } // end of method Test102PlusT::method2

} // end of class Test102PlusT




.class interface public abstract auto ansi Test101MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<!MinusT[]> method1<(class IMinusT<!MinusT>) M>(!MinusT t) cil managed
  {
  } // end of method Test101MinusT::method1

} // end of class Test101MinusT


.class interface public abstract auto ansi Test102MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<!MinusT>[] method2<(class IMinusT<!MinusT>) M>() cil managed
  {
  } // end of method Test102MinusT::method2

} // end of class Test102MinusT





// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file Lib2.res
