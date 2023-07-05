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
// MVID: {98C433B3-3689-48CF-A0CC-1320952F1424}
.namespace Test_refarg_o_il
{
  .class private auto ansi AA extends ['mscorlib']System.Object
  {
    .field family wchar pad1
    .field public int32 mm
    .field public class Test_refarg_o_il.AA m_aa
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(class Test_refarg_o_il.AA aa) il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.s   122
      IL_0003:  stfld      wchar Test_refarg_o_il.AA::pad1
      IL_0008:  ldarg.0
      IL_0009:  ldc.i4.s   11
      IL_000b:  stfld      int32 Test_refarg_o_il.AA::mm
      IL_0010:  ldarg.0
      IL_0011:  ldnull
      IL_0012:  stfld      class Test_refarg_o_il.AA Test_refarg_o_il.AA::m_aa
      IL_0017:  ldarg.0
      IL_0018:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_001d:  ldarg.0
      IL_001e:  ldarg.1
      IL_001f:  stfld      class Test_refarg_o_il.AA Test_refarg_o_il.AA::m_aa
      IL_0024:  ret
    } // end of method 'AA::.ctor'

    .method family hidebysig virtual instance void
            Finalize() il managed
    {
      // Code size       71 (0x47)
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  ldfld      wchar Test_refarg_o_il.AA::pad1
      IL_0006:  ldc.i4.s   122
      IL_0008:  bne.un.s   IL_0014

      IL_000a:  ldarg.0
      IL_000b:  ldfld      int32 Test_refarg_o_il.AA::mm
      IL_0010:  ldc.i4.s   11
      IL_0012:  beq.s      IL_001a

      IL_0014:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_0019:  throw

      IL_001a:  ldarg.0
      IL_001b:  ldfld      class Test_refarg_o_il.AA Test_refarg_o_il.AA::m_aa
      IL_0020:  brfalse.s  IL_0046

      IL_0022:  ldarg.0
      IL_0023:  ldfld      class Test_refarg_o_il.AA Test_refarg_o_il.AA::m_aa
      IL_0028:  ldfld      wchar Test_refarg_o_il.AA::pad1
      IL_002d:  ldc.i4.s   122
      IL_002f:  bne.un.s   IL_0040

      IL_0031:  ldarg.0
      IL_0032:  ldfld      class Test_refarg_o_il.AA Test_refarg_o_il.AA::m_aa
      IL_0037:  ldfld      int32 Test_refarg_o_il.AA::mm
      IL_003c:  ldc.i4.s   11
      IL_003e:  beq.s      IL_0046

      IL_0040:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_0045:  throw

      IL_0046:  ret
    } // end of method 'AA::Finalize'

  } // end of class 'AA'

  .class public auto ansi App extends ['mscorlib']System.Object
  {
    .field private static class Test_refarg_o_il.AA s_aa
    .method private hidebysig static void
            Litter() il managed
    {
      // Code size       38 (0x26)
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
      IL_0025:  ret
    } // end of method 'App::Litter'

    .method private hidebysig static int32 
            Test_refarg_o_il(class Test_refarg_o_il.AA& aa) il managed
    {
      // Code size       52 (0x34)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldnull
      IL_0001:  stsfld     class Test_refarg_o_il.AA Test_refarg_o_il.App::s_aa
      IL_0006:  call       void Test_refarg_o_il.App::Litter()
      IL_000b:  ldarg.0
      IL_000c:  ldind.ref
      IL_000d:  ldfld      int32 Test_refarg_o_il.AA::mm
      IL_0012:  ldc.i4.s   11
      IL_0014:  beq.s      IL_0024

      IL_0016:  ldstr      "*** failed ***"
      IL_001b:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0020:  ldc.i4.1
      IL_0021:  stloc.0
      IL_0022:  br.s       IL_0032

      IL_0024:  ldstr      "*** passed ***"
      IL_0029:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_002e:  ldc.i4    0x64
      IL_002f:  stloc.0
      IL_0030:  br.s       IL_0032

      IL_0032:  ldloc.0
      IL_0033:  ret
    } // end of method 'App::Test_refarg_o_il'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       32 (0x20)
      .maxstack  1
      .locals (int32 V_0,
               int32 V_1)
      IL_0000:  ldsfld     class Test_refarg_o_il.AA Test_refarg_o_il.App::s_aa
      IL_0005:  ldflda     class Test_refarg_o_il.AA Test_refarg_o_il.AA::m_aa
      IL_000a:  call       int32 Test_refarg_o_il.App::Test_refarg_o_il(class Test_refarg_o_il.AA&)
      IL_000f:  stloc.0
      IL_0010:  call       void ['mscorlib']System.GC::Collect()
      IL_0015:  call       void ['mscorlib']System.GC::WaitForPendingFinalizers()
      IL_001a:  ldloc.0
      IL_001b:  stloc.1
      IL_001c:  br.s       IL_001e

      IL_001e:  ldloc.1
      IL_001f:  ret
    } // end of method 'App::Main'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       17 (0x11)
      .maxstack  8
      IL_0000:  ldnull
      IL_0001:  newobj     instance void Test_refarg_o_il.AA::.ctor(class Test_refarg_o_il.AA)
      IL_0006:  newobj     instance void Test_refarg_o_il.AA::.ctor(class Test_refarg_o_il.AA)
      IL_000b:  stsfld     class Test_refarg_o_il.AA Test_refarg_o_il.App::s_aa
      IL_0010:  ret
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

} // end of namespace 'Test_refarg_o_il'

//*********** DISASSEMBLY COMPLETE ***********************
