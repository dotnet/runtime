// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// empty finally (with only endfinally in it)


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern eh_common {}
.assembly ASSEMBLY_NAME {}
.assembly extern xunit.core {}

.namespace hello_emptyfinally_basics_il
{
  .class public auto ansi beforefieldinit Class1
         extends [mscorlib]System.Object
  {
    .field private static class [eh_common]TestUtil.TestLog testLog
    .method private hidebysig specialname rtspecialname static void  .cctor() cil managed
    {
      .maxstack  2
      .locals init (class [mscorlib]System.IO.StringWriter expectedOut)
      newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
      stloc.s    expectedOut
      ldloc.s    expectedOut
      ldstr      "In try"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      ldstr      "Done"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
      stsfld     class [eh_common]TestUtil.TestLog hello_emptyfinally_basics_il.Class1::testLog
      ret
    } 

    .method public hidebysig static void 
            inTry() cil managed
    {
      .maxstack  1
      ldstr      "In try"
      call       void [System.Console]System.Console::WriteLine(string)
      ret
    } 


    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  1
      
      ldsfld     class [eh_common]TestUtil.TestLog hello_emptyfinally_basics_il.Class1::testLog
      callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
      
      .try
      {
        call       void hello_emptyfinally_basics_il.Class1::inTry()
        leave.s    done

      } finally {
        endfinally
      } 
    done:
      ldstr      "Done"
      call       void [System.Console]System.Console::WriteLine(string)

      ldsfld     class [eh_common]TestUtil.TestLog hello_emptyfinally_basics_il.Class1::testLog
      callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
      
      ldsfld     class [eh_common]TestUtil.TestLog hello_emptyfinally_basics_il.Class1::testLog
      callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()

      ret
    } 

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } 

  } 

} 
