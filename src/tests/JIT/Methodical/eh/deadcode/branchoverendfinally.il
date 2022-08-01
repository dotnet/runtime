// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern eh_common {}
.assembly 'branchoverendfinally' {}
.assembly extern xunit.core {}

.namespace Test_branchoverendfinally_il
{
.class public auto ansi beforefieldinit foo
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
    ldstr      "In try..."
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In finally..."
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "Good! - Getting closer"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "Done!"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)

    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stsfld     class [eh_common]TestUtil.TestLog Test_branchoverendfinally_il.foo::testLog
    ret
  } 

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1

    ldsfld     class [eh_common]TestUtil.TestLog Test_branchoverendfinally_il.foo::testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
    
    .try
    {
      IL_0000:  ldstr      "In try..."
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  leave.s    IL_0031

    }  
    finally
    {
      IL_000c:  ldstr      "In finally..."
      IL_0011:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0016:  br         IL_001c

      IL_001b:  endfinally

      IL_001c:  ldstr      "Good! - Getting closer"
      IL_0021:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0026:  endfinally
    }  

    IL_0027:  ldstr      "Unreached!!!"
    IL_002c:  call       void [System.Console]System.Console::WriteLine(string)

    IL_0031:  ldstr      "Done!"
    IL_0036:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0039:
      
    ldsfld     class [eh_common]TestUtil.TestLog Test_branchoverendfinally_il.foo::testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
    ldsfld     class [eh_common]TestUtil.TestLog Test_branchoverendfinally_il.foo::testLog
    callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
    
    IL_003b:  ret
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
