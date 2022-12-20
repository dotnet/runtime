// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern System.Runtime.Extensions { auto }
.assembly extern mscorlib { auto }

.assembly test_switch { }
.assembly extern xunit.core {}
.namespace JitTest_test_switch
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .field private static int32 m
    .field private static int32 n
    .method private hidebysig static int32 Main1() il managed
    {
        .maxstack  4
        tail. call       int32 JitTest_test_switch.Test::Main2()
        ret
    }
    .method private hidebysig static int32 Main2() il managed
    {
        .maxstack  4
        tail. call       int32 JitTest_test_switch.Test::Main3()
        ret
    }
    .method private hidebysig static int32 Main3() il managed
    {
        .maxstack  4
        tail. call       int32 JitTest_test_switch.Test::Main()
        ret
    }
    .method public hidebysig static int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  4
      .locals (int32 V_0)
      ldc.i4.1
      call       void [System.Runtime.Extensions]System.Environment::set_ExitCode(int32)
      ldsfld     int32 JitTest_test_switch.Test::n
      ldc.i4.1
      bne.un.s   IL_0047

      ldsfld     int32 JitTest_test_switch.Test::m
      ldc.i4     0x375f00
      bne.un.s   IL_0029

      ldstr      "PASSED: 10! == 3628800"
      call       void [System.Console]System.Console::WriteLine(class System.String)
      ldc.i4.s   100
      ret

IL_0029:
      ldstr      "FAILED: 10! != "
      ldsflda    int32 JitTest_test_switch.Test::m
      call       instance class System.String [mscorlib]System.Int32::ToString()
      call       class System.String [mscorlib]System.String::Concat(class System.String, class System.String)
      call       void [System.Console]System.Console::WriteLine(class System.String)
      ldc.i4.s   101
      ret

IL_0047:
      ldsfld     int32 JitTest_test_switch.Test::n
      ldc.i4.1
      beq        MERGE

      ldsfld     int32 JitTest_test_switch.Test::m
      ldsfld     int32 JitTest_test_switch.Test::n
      dup
      ldc.i4.1
      sub
      stsfld     int32 JitTest_test_switch.Test::n
      mul
      stsfld     int32 JitTest_test_switch.Test::m
MERGE:
      ldsfld int32 JitTest_test_switch.Test::n
      switch (CASE1, CASE2, CASE3)
      ldftn int32 JitTest_test_switch.Test::Main()
      br.s MERGE1
CASE1:
      ldftn int32 JitTest_test_switch.Test::Main1()
      br.s MERGE1
CASE2:
      ldftn int32 JitTest_test_switch.Test::Main2()
      br.s MERGE1
CASE3:
      ldftn int32 JitTest_test_switch.Test::Main3()
MERGE1:
      tail. calli int32()
      ret
    } // end of method Test::Main

    .method private hidebysig specialname rtspecialname static
            void .cctor() il managed
    {
      // Code size       14 (0xe)
      .maxstack  8
      IL_0000:  ldc.i4.1
      IL_0001:  stsfld     int32 JitTest_test_switch.Test::m
      IL_0006:  ldc.i4.s   10
      IL_0008:  stsfld     int32 JitTest_test_switch.Test::n
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

} // end of namespace JitTest_test_switch

//*********** DISASSEMBLY COMPLETE ***********************
