// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v1.2.21127
.assembly extern legacy library mscorlib
{
  .ver 0:0:0:0
}
.assembly legacy library bug
{
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.module bug.exe
// MVID: {643CA244-98FB-40AF-A64C-CDBA1F121F03}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x04090000


// =============== CLASS MEMBERS DECLARATION ===================

.namespace JitTest
{
  .class private auto ansi Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static void 
            DoStuff(method void *() A_0) cil managed
    {
      // Code size       9 (0x9)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  tail.
      IL_0003:  calli      void()
      IL_0008:  ret
    } // end of method Test::DoStuff

    .method private hidebysig static method void *() 
            RetStuff() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldftn      void JitTest.Test::DummyMethod()
      IL_0006:  ret
    } // end of method Test::RetStuff

    .method private hidebysig static void 
            DummyMethod() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  ldstr      "Looks good."
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  ret
    } // end of method Test::DummyMethod

    .method private hidebysig static void 
            TestMain() cil managed
    {
      // Code size       36 (0x24)
      .maxstack  4
      .locals (int32 V_0)
      IL_0000:  ldftn      void JitTest.Test::DummyMethod()
      IL_0006:  ldftn      void JitTest.Test::DoStuff(method void *())
      IL_000c:  calli      void(method void *())
      IL_0011:  ldftn      method void *() JitTest.Test::RetStuff()
      IL_0017:  calli      method void *()()
      IL_001c:  tail.
      IL_001e:  calli      void()
      IL_0023:  ret
    } // end of method Test::TestMain

    .method private hidebysig static int32 
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       17 (0x11)
      .maxstack  4
      IL_0000:  ldftn      void JitTest.Test::TestMain()
      IL_0006:  calli      void()
      IL_000b:  ldc.i4     0x64
      IL_0010:  ret
    } // end of method Test::Main

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

} // end of namespace JitTest


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
