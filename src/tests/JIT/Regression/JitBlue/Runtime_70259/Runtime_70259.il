// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// See Runtime_70259.cs.

//  Microsoft (R) .NET IL Disassembler.  Version 7.0.0-dev



// Metadata version: v4.0.30319
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly Runtime_70259
{
}
// MVID: {78ECA09B-26A9-44BB-9FD2-D94B639A44D5}
.custom instance void [System.Runtime]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 )
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x000001F5DF740000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Runtime_70259
       extends [System.Runtime]System.Object
{
  .class interface abstract auto ansi nested private IFace
  {
    .method public hidebysig newslot abstract virtual
            instance int32  Method() cil managed
    {
    } // end of method IFace::Method

  } // end of class IFace

  .class auto ansi nested private beforefieldinit C
         extends [System.Runtime]System.Object
         implements Runtime_70259/IFace
  {
    .method public hidebysig newslot virtual
            instance int32  Method() cil managed
    {
      // Code size       3 (0x3)
      .maxstack  8
      IL_0000:  ldc.i4.s   100
      IL_0002:  ret
    } // end of method C::Method

    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method C::.ctor

  } // end of class C

  .method public hidebysig static int32
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       31 (0x1f)
    .maxstack  8
    IL_0000:  ldtoken    Runtime_70259/IFace
    IL_0005:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_000a:  ldstr      "Method"
    IL_000f:  call       instance class [System.Runtime]System.Reflection.MethodInfo [System.Runtime]System.Type::GetMethod(string)
    IL_0014:  callvirt   instance !!0 [System.Runtime]System.Reflection.MethodInfo::CreateDelegate<class [System.Runtime]System.Func`2<class Runtime_70259/IFace,int32>>()
    IL_0019:  call       int32 Runtime_70259::CallMe(class [System.Runtime]System.Func`2<class Runtime_70259/IFace,int32>)
    IL_001e:  ret
  } // end of method Runtime_70259::Main

  .method private hidebysig static int32
          CallMe(class [System.Runtime]System.Func`2<class Runtime_70259/IFace,int32> del) cil managed noinlining
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  newobj     instance void Runtime_70259/C::.ctor()
    IL_0006:  tail.
    IL_0008:  call       instance !1 class [System.Runtime]System.Func`2<class Runtime_70259/IFace,int32>::Invoke(!0)
    IL_000d:  ret
  } // end of method Runtime_70259::CallMe

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Runtime_70259::.ctor

} // end of class Runtime_70259

// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
