// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly compat_i_u2 { }
.assembly extern xunit.core {}
.namespace JitTest_compat_i_u2
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig  static 
            native int Method1() il managed
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

    .method private hidebysig  static 
            unsigned int16 Method2() il managed
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
      IL_005c:  call       native int JitTest_compat_i_u2.Test::Method1()
      IL_0061:  ret
    } // end of method Test::Method2

    .method public hidebysig  static 
            int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       40 (0x28)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  call       unsigned int16 JitTest_compat_i_u2.Test::Method2()
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

} // end of namespace JitTest_compat_i_u2

//*********** DISASSEMBLY COMPLETE ***********************
