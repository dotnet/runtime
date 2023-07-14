// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern System.Runtime.Extensions { auto }
.assembly extern mscorlib { auto }

.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_test_mutual_rec
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .field private static int32 m
    .field private static int32 n
    .method public hidebysig  static int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  4
      .locals (int32 V_0)
      ldc.i4.1
      call       void [System.Runtime.Extensions]System.Environment::set_ExitCode(int32)
      ldsfld     int32 JitTest_test_mutual_rec.Test::n
      ldc.i4.1
      bne.un.s   IL_0047

      ldsfld     int32 JitTest_test_mutual_rec.Test::m
      ldc.i4     0x375f00
      bne.un.s   IL_0029

      ldstr      "PASSED: 10! == 3628800"
      call       void [System.Console]System.Console::WriteLine(class System.String)
      ldc.i4.s   100
      ret

IL_0029:
      ldstr      "FAILED: 10! != "
      ldsflda    int32 JitTest_test_mutual_rec.Test::m
      call       instance class System.String [mscorlib]System.Int32::ToString()
      call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      call       void [System.Console]System.Console::WriteLine(class System.String)
      ldc.i4.s   101
      ret

IL_0047:
      ldsfld     int32 JitTest_test_mutual_rec.Test::m
      ldsfld     int32 JitTest_test_mutual_rec.Test::n
      dup
      ldc.i4.1
      sub
      stsfld     int32 JitTest_test_mutual_rec.Test::n
      mul
      stsfld     int32 JitTest_test_mutual_rec.Test::m

      ldsfld int32 JitTest_test_mutual_rec.Test::n
      ldc.i4.3
      rem
      switch (CASE1, CASE2)
      ldftn int32 JitTest_test_mutual_rec.Test::Main()
      br.s MERGE1
CASE1:
      ldftn int32 JitTest_test_mutual_rec.Test::Main1()
      br.s MERGE1
CASE2:
      ldftn int32 JitTest_test_mutual_rec.Test::Main2()
MERGE1:
      tail. calli int32()
      ret
    } // end of method Test::Main
    .method private hidebysig  static int32 Main1() il managed
    {
      .maxstack  4
      .locals (int32 V_0)
      ldc.i4.1
      call       void [System.Runtime.Extensions]System.Environment::set_ExitCode(int32)
      ldsfld     int32 JitTest_test_mutual_rec.Test::n
      ldc.i4.1
      bne.un.s   IL_0047

      ldsfld     int32 JitTest_test_mutual_rec.Test::m
      ldc.i4     0x375f00
      bne.un.s   IL_0029

      ldstr      "PASSED: 10! == 3628800"
      call       void [System.Console]System.Console::WriteLine(class System.String)
      ldc.i4.s   100
      ret

IL_0029:
      ldstr      "FAILED: 10! != "
      ldsflda    int32 JitTest_test_mutual_rec.Test::m
      call       instance class System.String [mscorlib]System.Int32::ToString()
      call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      call       void [System.Console]System.Console::WriteLine(class System.String)
      ldc.i4.s   101
      ret

IL_0047:
      ldsfld     int32 JitTest_test_mutual_rec.Test::m
      ldsfld     int32 JitTest_test_mutual_rec.Test::n
      dup
      ldc.i4.1
      sub
      stsfld     int32 JitTest_test_mutual_rec.Test::n
      mul
      stsfld     int32 JitTest_test_mutual_rec.Test::m

      ldsfld int32 JitTest_test_mutual_rec.Test::n
      ldc.i4.3
      rem
      switch (CASE1, CASE2)
      ldftn int32 JitTest_test_mutual_rec.Test::Main()
      br.s MERGE1
CASE1:
      ldftn int32 JitTest_test_mutual_rec.Test::Main1()
      br.s MERGE1
CASE2:
      ldftn int32 JitTest_test_mutual_rec.Test::Main2()
MERGE1:
      tail. calli int32()
      ret
    } // end of method Test::Main
    .method private hidebysig  static int32 Main2() il managed
    {
      .maxstack  4
      .locals (int32 V_0)
      ldc.i4.1
      call       void [System.Runtime.Extensions]System.Environment::set_ExitCode(int32)
      ldsfld     int32 JitTest_test_mutual_rec.Test::n
      ldc.i4.1
      bne.un.s   IL_0047

      ldsfld     int32 JitTest_test_mutual_rec.Test::m
      ldc.i4     0x375f00
      bne.un.s   IL_0029

      ldstr      "PASSED: 10! == 3628800"
      call       void [System.Console]System.Console::WriteLine(class System.String)
      ldc.i4.s   100
      ret

IL_0029:
      ldstr      "FAILED: 10! != "
      ldsflda    int32 JitTest_test_mutual_rec.Test::m
      call       instance class System.String [mscorlib]System.Int32::ToString()
      call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      call       void [System.Console]System.Console::WriteLine(class System.String)
      ldc.i4.s   101
      ret

IL_0047:
      ldsfld     int32 JitTest_test_mutual_rec.Test::m
      ldsfld     int32 JitTest_test_mutual_rec.Test::n
      dup
      ldc.i4.1
      sub
      stsfld     int32 JitTest_test_mutual_rec.Test::n
      mul
      stsfld     int32 JitTest_test_mutual_rec.Test::m

      ldsfld int32 JitTest_test_mutual_rec.Test::n
      ldc.i4.3
      rem
      switch (CASE1, CASE2)
      ldftn int32 JitTest_test_mutual_rec.Test::Main()
      br.s MERGE1
CASE1:
      ldftn int32 JitTest_test_mutual_rec.Test::Main1()
      br.s MERGE1
CASE2:
      ldftn int32 JitTest_test_mutual_rec.Test::Main2()
MERGE1:
      tail. calli int32()
      ret
    } // end of method Test::Main

    .method private hidebysig  specialname rtspecialname static
            void .cctor() il managed
    {
      // Code size       14 (0xe)
      .maxstack  8
      IL_0000:  ldc.i4.1
      IL_0001:  stsfld     int32 JitTest_test_mutual_rec.Test::m
      IL_0006:  ldc.i4.s   10
      IL_0008:  stsfld     int32 JitTest_test_mutual_rec.Test::n
      IL_000d:  ret
    } // end of method Test::.cctor

    .method public hidebysig  specialname rtspecialname
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest_test_mutual_rec

//*********** DISASSEMBLY COMPLETE ***********************
