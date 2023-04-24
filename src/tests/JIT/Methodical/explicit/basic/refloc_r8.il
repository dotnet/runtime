// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'refloc_r8'// as "refloc_f8"
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {6A5E4D09-08FF-4E91-A37B-252AB7E08397}
.namespace Test_refloc_r8_il
{
  .class private auto ansi AA extends ['mscorlib']System.Object
  {
    .field static family class Test_refloc_r8_il.AA s_aa
    .field family class Test_refloc_r8_il.AA m_aa
    .field public float64 mm
    .field public static bool finalizerCalled
    .method family hidebysig virtual instance void
            Finalize() il managed
    {
      // Code size       59 (0x3b)
      .maxstack  8
      IL_0000:  ldstr      "finalizer called."
      IL_0005:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_000a:  ldarg.0
      IL_0027:  ldfld      float64 Test_refloc_r8_il.AA::mm
      			ldc.r8     -45.119999999999997
      IL_002c:  beq.s      IL_0034

      IL_002e:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_0033:  throw

      IL_0034:  ldc.i4.1
      IL_0035:  stsfld     bool Test_refloc_r8_il.AA::finalizerCalled
      IL_003a:  ret
    } // end of method 'AA::Finalize'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       17 (0x11)
      .maxstack  8
      IL_0000:  newobj     instance void Test_refloc_r8_il.AA::.ctor()
      IL_0005:  stsfld     class Test_refloc_r8_il.AA Test_refloc_r8_il.AA::s_aa
      IL_000a:  ldc.i4.0
      IL_000b:  stsfld     bool Test_refloc_r8_il.AA::finalizerCalled
      IL_0010:  ret
    } // end of method 'AA::.cctor'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       33 (0x21)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldsfld     class Test_refloc_r8_il.AA Test_refloc_r8_il.AA::s_aa
      IL_0006:  stfld      class Test_refloc_r8_il.AA Test_refloc_r8_il.AA::m_aa
      IL_000b:  ldarg.0
      IL_000c:  ldc.r8     -45.119999999999997
      IL_0015:  stfld      float64 Test_refloc_r8_il.AA::mm
      IL_001a:  ldarg.0
      IL_001b:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0020:  ret
    } // end of method 'AA::.ctor'

  } // end of class 'AA'

  .class public auto ansi App extends ['mscorlib']System.Object
  {
    .field private static class Test_refloc_r8_il.AA s_aa
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
      // Code size       71 (0x47)
      .maxstack  2
      .locals (float64 & V_0,
               int32 V_1)
      IL_0000:  ldsfld     class Test_refloc_r8_il.AA Test_refloc_r8_il.App::s_aa
      IL_0005:  ldflda      float64 Test_refloc_r8_il.AA::mm
      IL_000a:  stloc.0
      IL_000b:  ldnull
      IL_000c:  stsfld     class Test_refloc_r8_il.AA Test_refloc_r8_il.App::s_aa
      IL_0011:  call       void Test_refloc_r8_il.App::Litter()
      IL_0016:  ldsfld     bool Test_refloc_r8_il.AA::finalizerCalled
      IL_001b:  brtrue.s   IL_0029

      IL_001d:  ldloc.0
      			ldind.r8
      IL_001e:  ldc.r8     -45.119999999999997
      IL_0027:  beq.s      IL_0037

      IL_0029:  ldstr      "*** failed ***"
      IL_002e:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0033:  ldc.i4.1
      IL_0034:  stloc.1
      IL_0035:  br.s       IL_0045

      IL_0037:  ldstr      "*** passed ***"
      IL_003c:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0041:  ldc.i4    0x64
      IL_0042:  stloc.1
      IL_0043:  br.s       IL_0045

      IL_0045:  ldloc.1
      IL_0046:  ret
    } // end of method 'App::Main'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  newobj     instance void Test_refloc_r8_il.AA::.ctor()
      IL_0005:  stsfld     class Test_refloc_r8_il.AA Test_refloc_r8_il.App::s_aa
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

} // end of namespace 'Test_refloc_r8_il'

//*********** DISASSEMBLY COMPLETE ***********************
