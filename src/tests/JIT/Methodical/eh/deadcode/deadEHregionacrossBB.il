// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
}
.assembly extern eh_common {}
.assembly ASSEMBLY_NAME
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
.namespace Test_deadEHregionacrossBB_il
{
.class public auto ansi beforefieldinit test
       extends [mscorlib]System.Object
{
  .field private static class [eh_common]TestUtil.TestLog testLog
  .method private hidebysig specialname rtspecialname static void  .cctor() cil managed
  {
    .maxstack  2
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor()
    stsfld     class [eh_common]TestUtil.TestLog Test_deadEHregionacrossBB_il.test::testLog
    ret
  } // end of method Test_deadEHregionacrossBB_il.test::.cctor

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1
    .locals init (int32 V_0)
    ldsfld     class [eh_common]TestUtil.TestLog Test_deadEHregionacrossBB_il.test::testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()

    L:
    br  IL_0019
    .try
    {
      IL_0000:  ldstr      "In try"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
                ldloc.0
                brfalse L2
      leave.s L
      L2:
      .try {
         leave.s L
      } finally {
         endfinally
      }
      IL_000a:  leave.s    IL_0019
    }  // end .try
    catch [mscorlib]System.Object
    {
      IL_000c:  pop
      IL_000d:  ldstr      "In catch"
      IL_0012:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0017:  leave.s    IL_0019

    }  // end handler
    IL_0019:
        ldsfld     class [eh_common]TestUtil.TestLog Test_deadEHregionacrossBB_il.test::testLog
        callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()

        ldsfld     class [eh_common]TestUtil.TestLog Test_deadEHregionacrossBB_il.test::testLog
        callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()

    IL_0022:  ret
  } // end of method Test_deadEHregionacrossBB_il.test::Main

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_deadEHregionacrossBB_il.test::.ctor
} // end of class Test_deadEHregionacrossBB_il.test
}
