// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib { }
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_compat_r8_r4
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static 
            float64 Method1() il managed
    {
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
      IL_005a:  ldc.r8     10.
      IL_0063:  ret
    } 

    .method private hidebysig static 
            float32 Method2() il managed
    {
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
      IL_005c:  call       float64 JitTest_compat_r8_r4.Test::Method1()
      IL_0061:  ret
    } 

    .method public hidebysig static 
            int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  8
      .locals (int32 V_0)
      IL_0000:  call       float32 JitTest_compat_r8_r4.Test::Method2()
      IL_0005:  dup
      IL_0006:  ldc.r4     10.
      IL_000b:  beq.s      IL_0016

      IL_000d:  call       void [System.Console]System.Console::WriteLine(float32)
      IL_0012:  ldc.i4.1
      IL_0013:  stloc.0
      IL_0014:  br.s       IL_0026

      IL_0016:  pop
      IL_0017:  ldstr      "passed"
      IL_001c:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0021:  ldc.i4.s   100
      IL_0023:  stloc.0
      IL_0024:  br.s       IL_0026

      IL_0026:  ldloc.0
      IL_0027:  ret
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
