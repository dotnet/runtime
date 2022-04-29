// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'refloc_r4'// as "refloc_f4"
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {759403EF-2C74-4EEF-AB3C-166B1C468873}
.namespace Test_refloc_r4_il
{
  .class private auto ansi AA extends ['mscorlib']System.Object
  {
    .field static family class Test_refloc_r4_il.AA s_aa
    .field family class Test_refloc_r4_il.AA m_aa
    .field public float32 mm
    .field static family class Test_refloc_r4_il.AA s_bb
    .field family class Test_refloc_r4_il.AA m_bb
    .field public static bool finalizerCalled
    .method family hidebysig virtual instance void
            Finalize() il managed
    {
      // Code size       101 (0x65)
      .maxstack  2
      IL_0000:  ldstr      "finalizer called."
      IL_0005:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_000a:  ldarg.0
      IL_0051:  ldfld      float32 Test_refloc_r4_il.AA::mm
      			ldc.r4     45.119999
      IL_0056:  beq.s      IL_005e

      IL_0058:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_005d:  throw

      IL_005e:  ldc.i4.1
      IL_005f:  stsfld     bool Test_refloc_r4_il.AA::finalizerCalled
      IL_0064:  ret
    } // end of method 'AA::Finalize'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       27 (0x1b)
      .maxstack  8
      IL_0000:  newobj     instance void Test_refloc_r4_il.AA::.ctor()
      IL_0005:  stsfld     class Test_refloc_r4_il.AA Test_refloc_r4_il.AA::s_aa
      IL_000a:  newobj     instance void Test_refloc_r4_il.AA::.ctor()
      IL_000f:  stsfld     class Test_refloc_r4_il.AA Test_refloc_r4_il.AA::s_bb
      IL_0014:  ldc.i4.0
      IL_0015:  stsfld     bool Test_refloc_r4_il.AA::finalizerCalled
      IL_001a:  ret
    } // end of method 'AA::.cctor'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       40 (0x28)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldsfld     class Test_refloc_r4_il.AA Test_refloc_r4_il.AA::s_aa
      IL_0006:  stfld      class Test_refloc_r4_il.AA Test_refloc_r4_il.AA::m_aa
      IL_000b:  ldarg.0
      IL_000c:  ldc.r4     45.119999
      IL_0011:  stfld      float32 Test_refloc_r4_il.AA::mm
      IL_0016:  ldarg.0
      IL_0017:  ldsfld     class Test_refloc_r4_il.AA Test_refloc_r4_il.AA::s_bb
      IL_001c:  stfld      class Test_refloc_r4_il.AA Test_refloc_r4_il.AA::m_bb
      IL_0021:  ldarg.0
      IL_0022:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0027:  ret
    } // end of method 'AA::.ctor'

  } // end of class 'AA'

  .class public auto ansi App extends ['mscorlib']System.Object
  {
    .field private static class Test_refloc_r4_il.AA s_aa
    .method private hidebysig static void
            Litter() il managed
    {
      // Code size       43 (0x2b)
      .maxstack  2
      .locals (int32 V_0,
               int32[] V_1)
      IL_0000:  call       void ['mscorlib']System.GC::Collect()
      IL_0005:  ldc.i4.0
      IL_0006:  stloc.0
      IL_0007:  br.s       IL_0018

      IL_0009:  ldc.i4     0x3e8
      IL_000e:  newarr     ['mscorlib']System.Int32
      IL_0013:  stloc.1
      IL_0014:  ldloc.0
      IL_0015:  ldc.i4.1
      IL_0016:  add
      IL_0017:  stloc.0
      IL_0018:  ldloc.0
      IL_0019:  ldc.i4     0x3e8
      IL_001e:  blt.s      IL_0009

      IL_0020:  call       void ['mscorlib']System.GC::Collect()
      IL_0025:  call       void ['mscorlib']System.GC::WaitForPendingFinalizers()
      IL_002a:  ret
    } // end of method 'App::Litter'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       67 (0x43)
      .maxstack  2
      .locals (float32 & V_0,
               int32 V_1)
      IL_0000:  ldsfld     class Test_refloc_r4_il.AA Test_refloc_r4_il.App::s_aa
      IL_0005:  ldflda      float32 Test_refloc_r4_il.AA::mm
      IL_000a:  stloc.0
      IL_000b:  ldnull
      IL_000c:  stsfld     class Test_refloc_r4_il.AA Test_refloc_r4_il.App::s_aa
      IL_0011:  call       void Test_refloc_r4_il.App::Litter()
      IL_0016:  ldsfld     bool Test_refloc_r4_il.AA::finalizerCalled
      IL_001b:  brtrue.s   IL_0025

      IL_001d:  ldloc.0
      			ldind.r4
      IL_001e:  ldc.r4     45.119999
      IL_0023:  beq.s      IL_0033

      IL_0025:  ldstr      "*** failed ***"
      IL_002a:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_002f:  ldc.i4.1
      IL_0030:  stloc.1
      IL_0031:  br.s       IL_0041

      IL_0033:  ldstr      "*** passed ***"
      IL_0038:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_003d:  ldc.i4    0x64
      IL_003e:  stloc.1
      IL_003f:  br.s       IL_0041

      IL_0041:  ldloc.1
      IL_0042:  ret
    } // end of method 'App::Main'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  newobj     instance void Test_refloc_r4_il.AA::.ctor()
      IL_0005:  stsfld     class Test_refloc_r4_il.AA Test_refloc_r4_il.App::s_aa
      IL_000a:  ret
    } // end of method 'App::.cctor'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'App::.ctor'

  } // end of class 'App'

} // end of namespace 'Test_refloc_r4_il'

//*********** DISASSEMBLY COMPLETE ***********************
