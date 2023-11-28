// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_recurse_ep
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .field private static int32 m
    .field private static int32 n
    .method public hidebysig static 
            int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       93 (0x5d)
      .maxstack  4
      .locals (int32 V_0)
      IL_0000:  ldsfld     int32 JitTest_recurse_ep.Test::n
      IL_0005:  ldc.i4.1
      IL_0006:  bne.un.s   IL_003d

      IL_0008:  ldsfld     int32 JitTest_recurse_ep.Test::m
      IL_000d:  ldc.i4     0x375f00
      IL_0012:  bne.un.s   IL_0021

      IL_0014:  ldstr      "PASSED: 10! == 3628800"
      IL_0019:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_001e:  ldc.i4.s   100
      IL_0020:  ret

      IL_0021:  ldstr      "FAILED: 10! != "
      IL_0026:  ldsflda    int32 JitTest_recurse_ep.Test::m
      IL_002b:  call       instance class System.String [mscorlib]System.Int32::ToString()
      IL_0030:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0035:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_003a:  ldc.i4.s   101
      IL_003c:  ret

      IL_003d:  ldsfld     int32 JitTest_recurse_ep.Test::m
      IL_0042:  ldsfld     int32 JitTest_recurse_ep.Test::n
      IL_0047:  dup
      IL_0048:  ldc.i4.1
      IL_0049:  sub
      IL_004a:  stsfld     int32 JitTest_recurse_ep.Test::n
      IL_004f:  mul
      IL_0050:  stsfld     int32 JitTest_recurse_ep.Test::m
      IL_0055:  tail.
      IL_0057:  call       int32 JitTest_recurse_ep.Test::Main()
      IL_005c:  ret
    } // end of method Test::Main

    .method private hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       14 (0xe)
      .maxstack  8
      IL_0000:  ldc.i4.1
      IL_0001:  stsfld     int32 JitTest_recurse_ep.Test::m
      IL_0006:  ldc.i4.s   10
      IL_0008:  stsfld     int32 JitTest_recurse_ep.Test::n
      IL_000d:  ret
    } // end of method Test::.cctor

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest_recurse_ep

//*********** DISASSEMBLY COMPLETE ***********************
