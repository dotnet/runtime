// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// a finally with 2 endfinallys (1 unreachable)


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern common {}
.assembly tryfinally {}
.module tryfinally.exe

.namespace hello
{
  .class private auto ansi beforefieldinit Class1
         extends [mscorlib]System.Object
  {
    .field private static class [common]TestUtil.TestLog testLog
    .method private hidebysig specialname rtspecialname static void .cctor() cil managed
    {
      .maxstack  2
      .locals init (class [mscorlib]System.IO.StringWriter expectedOut)
      newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
      stloc.s    expectedOut
      ldloc.s    expectedOut
      ldstr      "in try"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      ldstr      "in finally"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      newobj     instance void [common]TestUtil.TestLog::.ctor(object)
      stsfld     class [common]TestUtil.TestLog hello.Class1::testLog
      ret
    } 

    .method public hidebysig static int32 
            Main(string[] args) cil managed
    {
      .entrypoint
      .maxstack  1

      ldsfld     class [common]TestUtil.TestLog hello.Class1::testLog
      callvirt   instance void [common]TestUtil.TestLog::StartRecording()

      .try
      {
        ldstr      "in try"
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s    IL_0017

      }  
      finally
      {
        ldstr      "in finally"
        call       void [System.Console]System.Console::WriteLine(string)
        endfinally
        ldstr      "ack!"
        call       void [System.Console]System.Console::WriteLine(string)
        endfinally
      }  

      IL_0017:  
      ldsfld     class [common]TestUtil.TestLog hello.Class1::testLog
      callvirt   instance void [common]TestUtil.TestLog::StopRecording()
      
      ldsfld     class [common]TestUtil.TestLog hello.Class1::testLog
      callvirt   instance int32 [common]TestUtil.TestLog::VerifyOutput()

      ret
    } 

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } 

  } 

} 
