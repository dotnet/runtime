// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .hash = (F6 F1 0B C9 D5 43 39 0C CC E4 FE 56 6B 52 C9 FA   // .....C9....VkR..
           18 FF 1F D7 ) 
  .ver 2:0:3600:0
}
.assembly DelegatesLib
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 

  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {75BA810B-D357-475C-83C1-8C098D3CB2C3}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03C80000


// =============== CLASS MEMBERS DECLARATION ===================


.class interface public abstract auto ansi IMinusT<-([mscorlib]System.Object) MinusT>
{
} // end of class IMinusT

.class interface public abstract auto ansi IPlusT<+([mscorlib]System.Object) PlusT>
{
} // end of class IPlusT




.class public auto ansi sealed Test001MinusT<- ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test001MinusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T> Invoke(!T arg) runtime managed
  {
  } // end of method Test001MinusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(!T arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test001MinusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T>  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test001MinusT::EndInvoke

} // end of class Test001MinusT


.class public auto ansi sealed Test002MinusT<- ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test002MinusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IMinusT<class IPlusT<!T>> Invoke(!T[] arg) runtime managed
  {
  } // end of method Test002MinusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(!T[] arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test002MinusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IMinusT<class IPlusT<!T>> EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test002MinusT::EndInvoke

} // end of class Test002MinusT


.class public auto ansi sealed Test003MinusT<- ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test003MinusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IPlusT<class IMinusT<!T>> Invoke(class IPlusT<!T> arg) runtime managed
  {
  } // end of method Test003MinusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IPlusT<!T> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test003MinusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IPlusT<class IMinusT<!T>>  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test003MinusT::EndInvoke

} // end of class Test003MinusT


.class public auto ansi sealed Test004MinusT<- ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test004MinusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IPlusT<class IMinusT<!T>[]> Invoke(class IPlusT<class IPlusT<!T[]>> arg) runtime managed
  {
  } // end of method Test004MinusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IPlusT<class IPlusT<!T[]>> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test004MinusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IPlusT<class IMinusT<!T>[]> EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test004MinusT::EndInvoke

} // end of class Test004MinusT

.class public auto ansi sealed Test005MinusT<- ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test005MinusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T>[] Invoke(class IPlusT<class IPlusT<!T[]>>[] arg) runtime managed
  {
  } // end of method Test005MinusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IPlusT<class IPlusT<!T[]>>[] arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test005MinusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T>[]  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test005MinusT::EndInvoke

} // end of class Test005MinusT


.class public auto ansi sealed Test001PlusT<+ ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test001PlusT::.ctor

  .method public hidebysig newslot virtual 
          instance !T  Invoke(class IMinusT<!T> arg) runtime managed
  {
  } // end of method Test001PlusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IMinusT<!T> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test001PlusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance !T  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test001PlusT::EndInvoke

} // end of class Test001PlusT


.class public auto ansi sealed Test002PlusT<+ ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test002PlusT::.ctor

  .method public hidebysig newslot virtual 
          instance !T[] Invoke(class IMinusT<!T[]> arg) runtime managed
  {
  } // end of method Test002PlusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IMinusT<!T[]> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test002PlusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance !T[] EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test002PlusT::EndInvoke

} // end of class Test002PlusT


.class public auto ansi sealed Test003PlusT<+ ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test003PlusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IPlusT<class IPlusT<!T>> Invoke(class IMinusT<!T> arg) runtime managed
  {
  } // end of method Test003PlusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IMinusT<!T> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test003PlusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IPlusT<class IPlusT<!T>>  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test003PlusT::EndInvoke

} // end of class Test003PlusT


.class public auto ansi sealed Test004PlusT<+ ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test004PlusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IMinusT<class IMinusT<!T>[]> Invoke(class IPlusT<class IMinusT<!T[]>> arg) runtime managed
  {
  } // end of method Test004PlusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IPlusT<class IMinusT<!T>[]> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test004PlusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IMinusT<class IMinusT<!T>[]>  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test004PlusT::EndInvoke

} // end of class Test004PlusT


.class public auto ansi sealed Test005PlusT<+ ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test005PlusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IPlusT<!T>[] Invoke(class IMinusT<class IPlusT<!T[]>>[] arg) runtime managed
  {
  } // end of method Test005PlusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IMinusT<class IPlusT<!T[]>>[] arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test005PlusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IPlusT<!T>[]  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test005PlusT::EndInvoke

} // end of class Test005PlusT



.class public auto ansi sealed Test001PlusTMinusU<+ ([mscorlib]System.Object) T, - ([mscorlib]System.Object) U>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test001PlusTMinusU::.ctor

  .method public hidebysig newslot virtual 
          instance !T  Invoke(!U arg) runtime managed
  {
  } // end of method Test001PlusTMinusU::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(!U arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test001PlusTMinusU::BeginInvoke

  .method public hidebysig newslot virtual 
          instance !T  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test001PlusTMinusU::EndInvoke

} // end of class Test001PlusT


.class public auto ansi sealed Test002PlusTMinusU<+ ([mscorlib]System.Object) T, - ([mscorlib]System.Object) U>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test002PlusTMinusU::.ctor

  .method public hidebysig newslot virtual 
          instance class IMinusT<!U>  Invoke(class IPlusT<!U> arg) runtime managed
  {
  } // end of method Test002PlusTMinusU::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IPlusT<!U> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test002PlusTMinusU::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IMinusT<!U>  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test002PlusTMinusU::EndInvoke

} // end of class Test002PlusTMinusU


.class public auto ansi sealed Test003PlusTMinusU<+ ([mscorlib]System.Object) T, - ([mscorlib]System.Object) U>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test003PlusTMinusU::.ctor

  .method public hidebysig newslot virtual 
          instance void  Invoke(class IMinusT<!T> arg) runtime managed
  {
  } // end of method Test003PlusTMinusU::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IMinusT<!T> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test003PlusTMinusU::BeginInvoke

  .method public hidebysig newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test003PlusTMinusU::EndInvoke

} // end of class Test003PlusTMinusU




.class public auto ansi sealed Test101MinusT<- ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test101MinusT::.ctor

  .method public hidebysig newslot virtual 
          instance !T  Invoke(class IMinusT<!T> arg) runtime managed
  {
  } // end of method Test101MinusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IMinusT<!T> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test101MinusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance !T EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test101MinusT::EndInvoke

} // end of class Test101MinusT


.class public auto ansi sealed Test102MinusT<- ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test102MinusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IPlusT<!T> Invoke(class IMinusT<!T[]> arg) runtime managed
  {
  } // end of method Test102MinusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IMinusT<!T[]> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test102MinusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IPlusT<!T> EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test102MinusT::EndInvoke

} // end of class Test102MinusT


.class public auto ansi sealed Test103MinusT<- ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test103MinusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IPlusT<!T> Invoke(!T arg) runtime managed
  {
  } // end of method Test103MinusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(!T arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test103MinusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IPlusT<!T> EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test103MinusT::EndInvoke

} // end of class Test103MinusT


.class public auto ansi sealed Test104MinusT<- ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test104MinusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IPlusT<!T>[] Invoke(!T[] arg) runtime managed
  {
  } // end of method Test104MinusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(!T[] arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test104MinusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IPlusT<!T>[] EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test104MinusT::EndInvoke

} // end of class Test104MinusT


.class public auto ansi sealed Test105MinusT<- ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test105MinusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T>[] Invoke(class IMinusT<class IPlusT<!T[]>>[] arg) runtime managed
  {
  } // end of method Test105MinusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IMinusT<class IPlusT<!T[]>>[] arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test105MinusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T>[]  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test105MinusT::EndInvoke

} // end of class Test105MinusT



.class public auto ansi sealed Test101PlusT<+ ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test101PlusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T> Invoke(!T arg) runtime managed
  {
  } // end of method Test101PlusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(!T arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test101PlusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T>  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test101PlusT::EndInvoke

} // end of class Test101PlusT


.class public auto ansi sealed Test102PlusT<+ ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test102PlusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T[]> Invoke(!T[] arg) runtime managed
  {
  } // end of method Test102PlusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(!T[] arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test102PlusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T[]> EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test102PlusT::EndInvoke

} // end of class Test102PlusT


.class public auto ansi sealed Test103PlusT<+ ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test103PlusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IPlusT<!T> Invoke(class IPlusT<!T> arg) runtime managed
  {
  } // end of method Test103PlusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IPlusT<!T> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test103PlusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IPlusT<!T>  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test103PlusT::EndInvoke

} // end of class Test103PlusT


.class public auto ansi sealed Test104PlusT<+ ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test104PlusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T>[] Invoke(class IMinusT<!T[]> arg) runtime managed
  {
  } // end of method Test104PlusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IMinusT<!T[]> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test104PlusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T>[] EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test104PlusT::EndInvoke

} // end of class Test104PlusT


.class public auto ansi sealed Test105PlusT<+ ([mscorlib]System.Object) T>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test105PlusT::.ctor

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T>[] Invoke(class IPlusT<!T[]>[] arg) runtime managed
  {
  } // end of method Test105PlusT::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IPlusT<!T[]>[] arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test105PlusT::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T>[]  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test105PlusT::EndInvoke

} // end of class Test105PlusT


.class public auto ansi sealed Test101PlusTMinusU<+ ([mscorlib]System.Object) T, - ([mscorlib]System.Object) U>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test101PlusTMinusU::.ctor

  .method public hidebysig newslot virtual 
          instance !U  Invoke(!T arg) runtime managed
  {
  } // end of method Test101PlusTMinusU::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(!T arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test101PlusTMinusU::BeginInvoke

  .method public hidebysig newslot virtual 
          instance !U  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test101PlusTMinusU::EndInvoke

} // end of class Test101PlusTMinusU


.class public auto ansi sealed Test102PlusTMinusU<+ ([mscorlib]System.Object) T, - ([mscorlib]System.Object) U>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test102PlusTMinusU::.ctor

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T>  Invoke(class IPlusT<!T> arg) runtime managed
  {
  } // end of method Test102PlusTMinusU::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IPlusT<!T> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test102PlusTMinusU::BeginInvoke

  .method public hidebysig newslot virtual 
          instance class IMinusT<!T>  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test102PlusTMinusU::EndInvoke

} // end of class Test102PlusTMinusU


.class public auto ansi sealed Test103PlusTMinusU<+ ([mscorlib]System.Object) T, - ([mscorlib]System.Object) U>
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Test103PlusTMinusU::.ctor

  .method public hidebysig newslot virtual 
          instance void  Invoke(class IMinusT<!U> arg) runtime managed
  {
  } // end of method Test103PlusTMinusU::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class IMinusT<!U> arg,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Test103PlusTMinusU::BeginInvoke

  .method public hidebysig newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method Test103PlusTMinusU::EndInvoke

} // end of class Test103PlusTMinusU
