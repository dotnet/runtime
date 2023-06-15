// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern System.Runtime.Extensions { auto }
.assembly extern mscorlib { auto }

.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_recurse_ep_void
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .field private static int32 m
    .field private static int32 n
    .field private static int32 returnValue
    .method private hidebysig static
            void Main() il managed
    {
      .maxstack  4
      .locals (int32 V_0)
      IL_0000:  ldsfld     int32 JitTest_recurse_ep_void.Test::n
      IL_0005:  ldc.i4.1
      IL_0006:  bne.un.s   IL_0049

      IL_0008:  ldsfld     int32 JitTest_recurse_ep_void.Test::m
      IL_000d:  ldc.i4     0x375f00
      IL_0012:  bne.un.s   IL_0025

      IL_0014:  ldstr      "PASSED: 10! == 3628800"
      IL_0019:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_001e:  ldc.i4     0x64
                stsfld     int32 JitTest_recurse_ep_void.Test::returnValue
                ret

      IL_0025:  ldstr      "FAILED: 10! != "
      IL_002a:  ldsflda    int32 JitTest_recurse_ep_void.Test::m
      IL_002f:  call       instance class System.String [mscorlib]System.Int32::ToString()
      IL_0034:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0039:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_003e:  ldc.i4     0x65
                stsfld     int32 JitTest_recurse_ep_void.Test::returnValue
                ret

      IL_0049:  ldsfld     int32 JitTest_recurse_ep_void.Test::m
      IL_004e:  ldsfld     int32 JitTest_recurse_ep_void.Test::n
      IL_0053:  dup
      IL_0054:  ldc.i4.1
      IL_0055:  sub
      IL_0056:  stsfld     int32 JitTest_recurse_ep_void.Test::n
      IL_005b:  mul
      IL_005c:  stsfld     int32 JitTest_recurse_ep_void.Test::m
      IL_0061:  tail.
      IL_0063:  call       void JitTest_recurse_ep_void.Test::Main()
      IL_0068:  ret
    }

    .method public hidebysig static
            int32 TestEntrypoint() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  4
                call       void JitTest_recurse_ep_void.Test::Main()
                ldsfld     int32 JitTest_recurse_ep_void.Test::returnValue
                ret
    }

    .method private hidebysig specialname rtspecialname static
            void .cctor() il managed
    {
      .maxstack  8
      IL_0000:  ldc.i4.1
      IL_0001:  stsfld     int32 JitTest_recurse_ep_void.Test::m
      IL_0006:  ldc.i4.s   10
      IL_0008:  stsfld     int32 JitTest_recurse_ep_void.Test::n
      IL_000d:  ret
    }

    .method public hidebysig specialname rtspecialname
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    }

  }

}
