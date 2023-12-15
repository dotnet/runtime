// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {3CF0A869-1266-4091-97CF-DEFA0C36E038}
.namespace Test_refloc_o_il
{
  .class private auto ansi AA extends ['mscorlib']System.Object
  {
    .field public class Test_refloc_o_il.AA m_aa
    .field public int32 m_nn
    .field public static bool finalizerCalled
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(int32 nn) il managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldnull
      IL_0008:  stfld      class Test_refloc_o_il.AA Test_refloc_o_il.AA::m_aa
      IL_000d:  ldarg.0
      IL_000e:  ldarg.1
      IL_000f:  stfld      int32 Test_refloc_o_il.AA::m_nn
      IL_0014:  ret
    } // end of method 'AA::.ctor'

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(class Test_refloc_o_il.AA aa) il managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      class Test_refloc_o_il.AA Test_refloc_o_il.AA::m_aa
      IL_000d:  ldarg.0
      IL_000e:  ldc.i4.0
      IL_000f:  stfld      int32 Test_refloc_o_il.AA::m_nn
      IL_0014:  ret
    } // end of method 'AA::.ctor'

    .method family hidebysig virtual instance void
            Finalize() il managed
    {
      // Code size       60 (0x3c)
      .maxstack  2
      .locals (class Test_refloc_o_il.AA V_0)
      IL_0000:  ldstr      "finalizer called."
      IL_0005:  call       void [System.Console]System.Console::WriteLine(class System.String)

      IL_0035:  ldc.i4.1
      IL_0036:  stsfld     bool Test_refloc_o_il.AA::finalizerCalled
      IL_003b:  ret
    } // end of method 'AA::Finalize'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  stsfld     bool Test_refloc_o_il.AA::finalizerCalled
      IL_0006:  ret
    } // end of method 'AA::.cctor'

  } // end of class 'AA'

  .class public auto ansi App extends ['mscorlib']System.Object
  {
    .field private static class Test_refloc_o_il.AA s_aa
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
      // Code size       72 (0x48)
      .maxstack  2
      .locals (class Test_refloc_o_il.AA & V_0,
               int32 V_1)
      IL_0000:  ldsfld     class Test_refloc_o_il.AA Test_refloc_o_il.App::s_aa
      IL_0005:  ldflda      class Test_refloc_o_il.AA Test_refloc_o_il.AA::m_aa
      IL_000a:  stloc.0
      IL_000b:  ldnull
      IL_000c:  stsfld     class Test_refloc_o_il.AA Test_refloc_o_il.App::s_aa
      IL_0011:  call       void Test_refloc_o_il.App::Litter()
      IL_0016:  ldsfld     bool Test_refloc_o_il.AA::finalizerCalled
      IL_001b:  brtrue.s   IL_002a

      IL_001d:  ldloc.0
      			ldind.ref
      IL_001e:  ldfld      int32 Test_refloc_o_il.AA::m_nn
      IL_0023:  ldc.i4     0x6819
      IL_0028:  beq.s      IL_0038

      IL_002a:  ldstr      "*** failed ***"
      IL_002f:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0034:  ldc.i4.1
      IL_0035:  stloc.1
      IL_0036:  br.s       IL_0046

      IL_0038:  ldstr      "*** passed ***"
      IL_003d:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0042:  ldc.i4    0x64
      IL_0043:  stloc.1
      IL_0044:  br.s       IL_0046

      IL_0046:  ldloc.1
      IL_0047:  ret
    } // end of method 'App::Main'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldc.i4     0x6819
      IL_0005:  newobj     instance void Test_refloc_o_il.AA::.ctor(int32)
      IL_000a:  newobj     instance void Test_refloc_o_il.AA::.ctor(class Test_refloc_o_il.AA)
      IL_000f:  stsfld     class Test_refloc_o_il.AA Test_refloc_o_il.App::s_aa
      IL_0014:  ret
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

} // end of namespace 'Test_refloc_o_il'

//*********** DISASSEMBLY COMPLETE ***********************
