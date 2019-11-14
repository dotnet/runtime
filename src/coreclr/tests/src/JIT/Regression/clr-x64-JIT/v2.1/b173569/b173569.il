// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}



// Regression tc for ddb 173569 - slow delegate-creation path does not work for p/invoke methods.


// Metadata version: v2.0.50727
.module extern kernel32
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly ddb173569
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module ddb173569.exe
// MVID: {0019E28F-AD1E-40AC-B6B2-5554D213CAAE}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000000000890000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi sealed MyDelegate
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method MyDelegate::.ctor

  .method public hidebysig newslot virtual 
          instance int32  Invoke() runtime managed
  {
  } // end of method MyDelegate::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method MyDelegate::BeginInvoke

  .method public hidebysig newslot virtual 
          instance int32  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method MyDelegate::EndInvoke

} // end of class MyDelegate

.class private auto ansi beforefieldinit My
       extends [mscorlib]System.Object
{
  .method private hidebysig static pinvokeimpl("kernel32" winapi) 
          int32  GetTickCount() cil managed preservesig
  {
  }
  .method private hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (class MyDelegate V_0)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  ldftn      int32 My::GetTickCount()

              nop

    IL_0008:  newobj     instance void MyDelegate::.ctor(object,
                                                         native int)
    IL_000d:  stloc.0
    IL_000e:  ldloc.0
    IL_000f:  callvirt   instance int32 MyDelegate::Invoke()
    IL_0014:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0019:  nop
    IL_001a:  ldc.i4 100
    IL_001b:  ret
  } // end of method My::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method My::.ctor

} // end of class My


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file ddb173569.res
