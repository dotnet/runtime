// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly ASSEMBLY_NAME { }
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary {}
.assembly extern xunit.core {}
.namespace JitTest_compat_i4_u
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static
            int32 Method1() il managed
    {
      // Code size       93 (0x5d)
      .maxstack  8
      IL_0000:  call       void [mscorlib]System.GC::Collect()
      IL_0005:  call       void [mscorlib]System.GC::Collect()
      IL_000a:  call       void [mscorlib]System.GC::Collect()
      IL_000f:  call       void [mscorlib]System.GC::Collect()
      IL_0014:  call       void [mscorlib]System.GC::Collect()
      IL_0019:  call       void [mscorlib]System.GC::Collect()
      IL_001e:  call       void [mscorlib]System.GC::Collect()
      IL_0023:  call       void [mscorlib]System.GC::Collect()
      IL_0028:  call       void [mscorlib]System.GC::Collect()
      IL_002d:  call       void [mscorlib]System.GC::Collect()
      IL_0032:  call       void [mscorlib]System.GC::Collect()
      IL_0037:  call       void [mscorlib]System.GC::Collect()
      IL_003c:  call       void [mscorlib]System.GC::Collect()
      IL_0041:  call       void [mscorlib]System.GC::Collect()
      IL_0046:  call       void [mscorlib]System.GC::Collect()
      IL_004b:  call       void [mscorlib]System.GC::Collect()
      IL_0050:  call       void [mscorlib]System.GC::Collect()
      IL_0055:  call       void [mscorlib]System.GC::Collect()
      IL_005a:  ldc.i4.s   10
      IL_005c:  ret
    } // end of method Test::Method1

    .method private hidebysig static
            native unsigned int Method2() il managed
    {
      // Code size       98 (0x62)
      .maxstack  8
      IL_0000:  call       void [mscorlib]System.GC::Collect()
      IL_0005:  call       void [mscorlib]System.GC::Collect()
      IL_000a:  call       void [mscorlib]System.GC::Collect()
      IL_000f:  call       void [mscorlib]System.GC::Collect()
      IL_0014:  call       void [mscorlib]System.GC::Collect()
      IL_0019:  call       void [mscorlib]System.GC::Collect()
      IL_001e:  call       void [mscorlib]System.GC::Collect()
      IL_0023:  call       void [mscorlib]System.GC::Collect()
      IL_0028:  call       void [mscorlib]System.GC::Collect()
      IL_002d:  call       void [mscorlib]System.GC::Collect()
      IL_0032:  call       void [mscorlib]System.GC::Collect()
      IL_0037:  call       void [mscorlib]System.GC::Collect()
      IL_003c:  call       void [mscorlib]System.GC::Collect()
      IL_0041:  call       void [mscorlib]System.GC::Collect()
      IL_0046:  call       void [mscorlib]System.GC::Collect()
      IL_004b:  call       void [mscorlib]System.GC::Collect()
      IL_0050:  call       void [mscorlib]System.GC::Collect()
      IL_0055:  call       void [mscorlib]System.GC::Collect()
      IL_005a:  tail.
      IL_005c:  call       int32 JitTest_compat_i4_u.Test::Method1()
      IL_0061:  ret
    } // end of method Test::Method2

    .method public hidebysig static
            int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      // [ActiveIssue("https://github.com/dotnet/runtimelab/issues/", typeof(PlatformDetection), nameof(PlatformDetection.IsAppleMobile))]
      .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [System.Runtime]System.Type, string[]) = (
                  01 00 30 68 74 74 70 73 3a 2f 2f 67 69 74 68 75
                  62 2e 63 6f 6d 2f 64 6f 74 6e 65 74 2f 72 75 6e
                  74 69 6d 65 6c 61 62 2f 69 73 73 75 65 73 2f 54
                  4f 44 4f 61 54 65 73 74 4c 69 62 72 61 72 79 2e
                  50 6c 61 74 66 6f 72 6d 44 65 74 65 63 74 69 6f
                  6e 2c 20 54 65 73 74 4c 69 62 72 61 72 79 2c 20
                  56 65 72 73 69 6f 6e 3d 30 2e 30 2e 30 2e 30 2c
                  20 43 75 6c 74 75 72 65 3d 6e 65 75 74 72 61 6c
                  2c 20 50 75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e
                  3d 6e 75 6c 6c 01 00 00 00 0d 49 73 41 70 70 6c
                  65 4d 6f 62 69 6c 65 00 00
              )
      .entrypoint
      // Code size       40 (0x28)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  call       native unsigned int JitTest_compat_i4_u.Test::Method2()
      IL_0005:  ldc.i4.s   10
      IL_0007:  beq.s      IL_0017

      IL_0009:  ldstr      "failed"
      IL_000e:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0013:  ldc.i4.1
      IL_0014:  stloc.0
      IL_0015:  br.s       IL_0026

      IL_0017:  ldstr      "passed"
      IL_001c:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0021:  ldc.i4.s   100
      IL_0023:  stloc.0
      IL_0024:  br.s       IL_0026

      IL_0026:  ldloc.0
      IL_0027:  ret
    } // end of method Test::Main

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

} // end of namespace JitTest_compat_i4_u

//*********** DISASSEMBLY COMPLETE ***********************
