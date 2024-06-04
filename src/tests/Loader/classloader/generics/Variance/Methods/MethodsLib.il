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
.assembly MethodsLib
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



.class interface public abstract auto ansi Test001PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual 
          instance !PlusT method1(class IMinusT<!PlusT> t) cil managed
  {
  } // end of method 'IPlusT1!1'::method1

} // end of class 'IPlusT1!1'


.class interface public abstract auto ansi Test002PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual 
          instance !PlusT[] method2(class IMinusT<!PlusT[]> t) cil managed
  {
  } // end of method 'IPlusT2!1'::method2

} // end of class 'IPlusT2!1'


.class interface public abstract auto ansi Test003PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IPlusT<!PlusT> method3(class IMinusT<!PlusT>[] t) cil managed
  {
  } // end of method 'IPlusT3!1'::method3

} // end of class 'IPlusT3!1'

.class interface public abstract auto ansi Test004PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<class IMinusT<!PlusT[]>> method4(class IPlusT<class IMinusT<!PlusT[]>> t) cil managed
  {
  } // end of method 'IPlusT4!1'::method4

} // end of class 'IPlusT4!1'


.class interface public abstract auto ansi Test005PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<class IMinusT<!PlusT>[]> method5(class IMinusT<!PlusT>[][] t) cil managed
  {
  } // end of method 'IMinusT5!1'::method5

} // end of class 'IMinusT5!1'



.class interface public abstract auto ansi Test001MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<!MinusT[]> method1(!MinusT t) cil managed
  {
  } // end of method 'IMinusT1!1'::method1

} // end of class 'IMinusT1!1'

.class interface public abstract auto ansi Test002MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<!MinusT>[] method2(!MinusT[] t) cil managed
  {
  } // end of method 'IMinusT2!1'::method2

} // end of class 'IMinusT2!1'


.class interface public abstract auto ansi Test003MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<class IPlusT<!MinusT>> method3(class IPlusT<!MinusT> t) cil managed
  {
  } // end of method 'IMinusT3!1'::method3

} // end of class 'IMinusT3!1'



.class interface public abstract auto ansi Test004MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<class IPlusT<!MinusT>[]> method4(class IPlusT<class IPlusT<!MinusT[]>> t) cil managed
  {
  } // end of method 'IMinusT4!1'::method4

} // end of class 'IMinusT4!1'


.class interface public abstract auto ansi Test005MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IPlusT<class IMinusT<!MinusT>> method5(class IPlusT<!MinusT>[] t) cil managed
  {
  } // end of method 'IMinusT5!1'::method5

} // end of class 'IMinusT5!1'



.class interface public abstract auto ansi Test101PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<!PlusT> method1(!PlusT t) cil managed
  {
  } // end of method 'IPlusT1!1'::method1

} // end of class 'IPlusT1!1'


.class interface public abstract auto ansi Test102PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<!PlusT[]> method2(!PlusT[] t) cil managed
  {
  } // end of method 'IPlusT2!1'::method2

} // end of class 'IPlusT2!1'


.class interface public abstract auto ansi Test103PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<!PlusT>[] method3(class IPlusT<!PlusT> t) cil managed
  {
  } // end of method 'IPlusT3!1'::method3

} // end of class 'IPlusT3!1'

.class interface public abstract auto ansi Test104PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IPlusT<class IMinusT<!PlusT[]>> method4(class IMinusT<class IMinusT<!PlusT[]>> t) cil managed
  {
  } // end of method 'IPlusT4!1'::method4

} // end of class 'IPlusT4!1'


.class interface public abstract auto ansi Test105PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<!PlusT>[][] method5(class IMinusT<class IMinusT<!PlusT>[]> t) cil managed
  {
  } // end of method 'IMinusT5!1'::method5

} // end of class 'IMinusT5!1'

.class interface public abstract auto ansi Test106PlusT<+([mscorlib]System.Object) PlusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IMinusT<!PlusT>[][] method5(!PlusT[] t) cil managed
  {
  } // end of method 'IMinusT5!1'::method5

} // end of class 'IMinusT5!1'


.class interface public abstract auto ansi Test101MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance !MinusT method1(class IMinusT<!MinusT[]> t) cil managed
  {
  } // end of method 'IMinusT1!1'::method1

} // end of class 'IMinusT1!1'

.class interface public abstract auto ansi Test102MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance !MinusT[] method2(class IMinusT<!MinusT>[] t) cil managed
  {
  } // end of method 'IMinusT2!1'::method2

} // end of class 'IMinusT2!1'


.class interface public abstract auto ansi Test103MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IPlusT<!MinusT> method3(class IMinusT<class IPlusT<!MinusT>> t) cil managed
  {
  } // end of method 'IMinusT3!1'::method3

} // end of class 'IMinusT3!1'


.class interface public abstract auto ansi Test104MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IPlusT<class IPlusT<!MinusT[]>> method4(class IMinusT<class IPlusT<!MinusT>[]> t) cil managed
  {
  } // end of method 'IMinusT4!1'::method4

} // end of class 'IMinusT4!1'


.class interface public abstract auto ansi Test105MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IPlusT<!MinusT>[] method5(class IPlusT<class IMinusT<!MinusT>> t) cil managed
  {
  } // end of method 'IMinusT5!1'::method5

} // end of class 'IMinusT5!1'


.class interface public abstract auto ansi Test106MinusT<-([mscorlib]System.Object) MinusT>
{
  .method public hidebysig newslot abstract virtual 
          instance class IPlusT<!MinusT>[] method5(!MinusT t) cil managed
  {
  } // end of method 'IMinusT5!1'::method5

} // end of class 'IMinusT5!1'
// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file Lib.res
