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
.assembly extern eh_common{}
.assembly ASSEMBLY_NAME
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}

.namespace hello_deadoponerrorinfunclet_deadcode_il
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
        ldstr      "1234"
        callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
        ldloc.s    expectedOut
        ldstr      "test2"
        callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
        ldloc.s    expectedOut
        ldstr      "1234"
        callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
        ldloc.s    expectedOut
        newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
        stsfld     class [eh_common]TestUtil.TestLog hello_deadoponerrorinfunclet_deadcode_il.Class1::testLog
        ret
    }

    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  2
      .locals init (int32 V_0,
               string V_1,
               int32 V_2)

                ldsfld     class [eh_common]TestUtil.TestLog hello_deadoponerrorinfunclet_deadcode_il.Class1::testLog
                callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
      IL_0000:  ldc.i4   1234
      IL_0002:  stloc.0
      IL_0003:  ldloc.0
      IL_0004:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_0009:  br.s       IL_0011

      IL_000b:  ldstr      "test1"
      IL_0010:  stloc.1
      IL_0011:  ldstr      "test2"
      IL_0016:  stloc.1
      .try
      {
        IL_0017:  ldloc.1
        IL_0018:  call       void [System.Console]System.Console::WriteLine(string)
        IL_001d:  newobj     instance void [mscorlib]System.Exception::.ctor()
        IL_0022:  throw

      }
      catch [mscorlib]System.Object
      {
        IL_0023:  pop
        leave.s  IL_0066

        .try
        {
          IL_0024:  ldloc.0
          IL_0025:  ldc.i4.3
          IL_0026:  bne.un.s   IL_002a

          IL_0028:  leave.s    IL_0017

          IL_002a:  ldloc.0
          IL_002b:  ldc.i4.0
          IL_002c:  blt.s      IL_0030

          IL_002e:  leave.s    IL_0054

          IL_0030:  ldloc.0
          IL_0031:  ldc.i4.0
          IL_0032:  bge.s      IL_0036

          IL_0034:  leave.s    IL_000b

          IL_0036:  leave.s    IL_004a

        }
        catch [mscorlib]System.Object
        {
          IL_0038:  pop
          IL_0039:  ldloc.0
          IL_003a:  brfalse.s  IL_003e

          IL_003c:  leave.s    IL_0054

          IL_003e:  ldstr      "end inner catch"
          IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
          IL_0048:  leave.s    IL_004a

        }
        IL_004a:  ldstr      "unreached"
        IL_004f:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0054:  ldstr      "end outer catch "
        IL_0059:  ldloc.1
        IL_005a:  call       string [mscorlib]System.String::Concat(string,
                                                                    string)
        IL_005f:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0064:  leave.s    IL_0066

      }
      IL_0066:  ldloc.0
      IL_0067:  stloc.2
      IL_0068:  br.s       IL_006a

      IL_006a:  ldloc.2
                call  void [System.Console]System.Console::WriteLine(int32)
                ldsfld     class [eh_common]TestUtil.TestLog hello_deadoponerrorinfunclet_deadcode_il.Class1::testLog
                callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()

                ldsfld     class [eh_common]TestUtil.TestLog hello_deadoponerrorinfunclet_deadcode_il.Class1::testLog
                callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()

      IL_006b:  ret
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
