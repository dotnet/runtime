// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern legacy library mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .hash = (81 51 4F DD 43 6C 78 C8 8C 87 6B 7F 41 A9 D8 DA   // .QO.Clx...k.A...
           FA 84 09 19 ) 
  .ver 1:2:3400:0
}
.assembly legacy library Foo
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {E2294DFD-8443-4AA7-9104-BC680C3B8133}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x04130000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Foo
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  DoFoo() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Running Foo.DoFoo"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Foo::DoFoo

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Foo::.ctor

} // end of class Foo

.class public auto ansi beforefieldinit Bar
       extends Foo
{
  .method public hidebysig newslot virtual 
          instance void  NotDoFoo1() cil managed
  {
    .override Foo::DoFoo
 
   // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Running Bar.NotDoFoo1"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Bar::NotDoFoo1

  .method public hidebysig virtual instance void 
          DoFoo() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Running Bar.DoFoo"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Bar::DoFoo

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Foo::.ctor()
    IL_0006:  ret
  } // end of method Bar::.ctor

} // end of class Bar

.class public auto ansi beforefieldinit Test_Foo
       extends [mscorlib]System.Object
{

  .method public static int32 Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (class Bar V_0,
             	  class Foo V_1, 
		  class [mscorlib]System.Exception V_2,
                  int32 V_3)

    .try
    {
      newobj     instance void Bar::.ctor()
      stloc.0
      ldloc.0
      stloc.1

      ldstr      "PASS"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   100
      stloc.3
      leave.s    end
    }
    catch [mscorlib]System.Exception 
    {
      stloc.2
      ldstr      "Caught unepxected exception: {0}"
      ldloc.2
      callvirt   instance string [mscorlib]System.Exception::get_Message()
      call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
      ldstr      "FAIL"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   101
      stloc.3
      leave.s    end
    }
    end: ldloc.3
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file Foo.res
