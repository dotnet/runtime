// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
.assembly extern mscorlib
{
}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{
}
.assembly extern xunit.core {}
.namespace Test_refarg_box_f8_il
{
  .class public auto ansi App extends [mscorlib]System.Object
  {
    .field private static class System.Object s_aa
    .method private hidebysig static void
            Litter() il managed
    {
      .maxstack  2
      .locals (int32 V_0,
               int32[] V_1)
      IL_0000:  call       void [mscorlib]System.GC::Collect()
      IL_0005:  ldc.i4.0
      IL_0006:  stloc.0
      IL_0007:  br.s       IL_0018

      IL_0009:  ldc.i4     0x3e8
      IL_000e:  newarr     [mscorlib]System.Int32
      IL_0013:  stloc.1
      IL_0014:  ldloc.0
      IL_0015:  ldc.i4.1
      IL_0016:  add
      IL_0017:  stloc.0
      IL_0018:  ldloc.0
      IL_0019:  ldc.i4     0x3e8
      IL_001e:  blt.s      IL_0009

      IL_0020:  call       void [mscorlib]System.GC::Collect()
      			call       void [mscorlib]System.GC::WaitForPendingFinalizers()
      IL_0025:  ret
    } 

    .method private hidebysig static int32 
            Test_refarg_box_f8_il(float64& n) il managed
    {
      .maxstack  4
      .locals (int32 V_0)
      IL_0000:  ldnull
      IL_0001:  stsfld     class System.Object Test_refarg_box_f8_il.App::s_aa
      IL_0006:  call       void Test_refarg_box_f8_il.App::Litter()
      IL_000b:  ldarg.0
      IL_000c:  ldind.r8
                dup
                call       void [System.Console]System.Console::WriteLine(float64)
      IL_000d:  ldc.r8     3.1415926535896999
                dup
                call       void [System.Console]System.Console::WriteLine(float64)
      IL_0016:  beq.s      IL_0026

      IL_0018:  ldstr      "*** failed ***"
      IL_001d:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0022:  ldc.i4.1
      IL_0023:  stloc.0
      IL_0024:  br.s       IL_0034

      IL_0026:  ldstr      "*** passed ***"
      IL_002b:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0030:  ldc.i4    0x64
      IL_0031:  stloc.0
      IL_0032:  br.s       IL_0034

      IL_0034:  ldloc.0
      IL_0035:  ret
    } 

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  1
      .locals (float64 ZZZZ,  
               int32 V_1,
               int32 V_2)
      IL_0000:  ldc.r8     0.
      IL_0009:  stloc.0
      
      			ldsfld     class System.Object Test_refarg_box_f8_il.App::s_aa
      			unbox      [mscorlib]System.Double		
      
      IL_000c:  call       int32 Test_refarg_box_f8_il.App::Test_refarg_box_f8_il(float64&)
      IL_0011:  stloc.1
      IL_0012:  call       void [mscorlib]System.GC::Collect()
      IL_0017:  call       void [mscorlib]System.GC::WaitForPendingFinalizers()
      IL_001c:  ldloc.1
      IL_001d:  stloc.2
      IL_001e:  br.s       IL_0020

      IL_0020:  ldloc.2
      IL_0021:  ret
    } 

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      .maxstack  1
      .locals (float64 V_0)
      IL_0000:  ldc.r8     3.1415926535896999
      IL_0009:  stloc.0
      IL_000a:  ldloc.s   V_0
      IL_000c:  box     [mscorlib]System.Double
      IL_0011:  stsfld     class System.Object Test_refarg_box_f8_il.App::s_aa
      IL_0016:  ret
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
