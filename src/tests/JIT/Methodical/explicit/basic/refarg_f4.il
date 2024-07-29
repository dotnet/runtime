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
// MVID: {32D92914-4A90-441D-BAD9-DD653D751832}
.namespace Test_refarg_f4_il
{
  .class private auto ansi AA extends ['mscorlib']System.Object
  {
    .field public float32 mm2
    .field public class Test_refarg_f4_il.AA self1
    .field public float32 mm1
    .field public class Test_refarg_f4_il.AA self2
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(int32 reclevel) il managed
    {
      // Code size       95 (0x5f)
      .maxstack  3
      IL_0000:  ldarg.0
      IL_0001:  ldc.r4     12.
      IL_0006:  stfld      float32 Test_refarg_f4_il.AA::mm2
      IL_000b:  ldarg.0
      IL_000c:  ldnull
      IL_000d:  stfld      class Test_refarg_f4_il.AA Test_refarg_f4_il.AA::self1
      IL_0012:  ldarg.0
      IL_0013:  ldc.r4     11.314
      IL_0018:  stfld      float32 Test_refarg_f4_il.AA::mm1
      IL_001d:  ldarg.0
      IL_001e:  ldnull
      IL_001f:  stfld      class Test_refarg_f4_il.AA Test_refarg_f4_il.AA::self2
      IL_0024:  ldarg.0
      IL_0025:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_002a:  ldarg.1
      IL_002b:  ldc.i4.s   100
      IL_002d:  bge.s      IL_0050

      IL_002f:  ldarg.0
      IL_0030:  ldarg.1
      IL_0031:  ldc.i4.1
      IL_0032:  add
      IL_0033:  newobj     instance void Test_refarg_f4_il.AA::.ctor(int32)
      IL_0038:  stfld      class Test_refarg_f4_il.AA Test_refarg_f4_il.AA::self1
      IL_003d:  ldarg.0
      IL_003e:  ldarg.0
      IL_003f:  ldfld      class Test_refarg_f4_il.AA Test_refarg_f4_il.AA::self1
      IL_0044:  ldfld      class Test_refarg_f4_il.AA Test_refarg_f4_il.AA::self1
      IL_0049:  stfld      class Test_refarg_f4_il.AA Test_refarg_f4_il.AA::self2
      IL_004e:  br.s       IL_005e

      IL_0050:  ldarg.0
      IL_0051:  ldarg.0
      IL_0052:  stfld      class Test_refarg_f4_il.AA Test_refarg_f4_il.AA::self1
      IL_0057:  ldarg.0
      IL_0058:  ldnull
      IL_0059:  stfld      class Test_refarg_f4_il.AA Test_refarg_f4_il.AA::self2
      IL_005e:  ret
    } // end of method 'AA::.ctor'

    .method family hidebysig instance void
            CheckFields() il managed
    {
      // Code size       33 (0x21)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      float32 Test_refarg_f4_il.AA::mm2
      IL_0006:  ldc.r4     12.
      IL_000b:  bne.un.s   IL_001a

      IL_000d:  ldarg.0
      IL_000e:  ldfld      float32 Test_refarg_f4_il.AA::mm1
      IL_0013:  ldc.r4     11.314
      IL_0018:  beq.s      IL_0020

      IL_001a:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_001f:  throw

      IL_0020:  ret
    } // end of method 'AA::CheckFields'

    .method family hidebysig virtual instance void
            Finalize() il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void Test_refarg_f4_il.AA::CheckFields()
      IL_0006:  ldarg.0
      IL_0007:  ldfld      class Test_refarg_f4_il.AA Test_refarg_f4_il.AA::self1
      IL_000c:  brfalse.s  IL_0019

      IL_000e:  ldarg.0
      IL_000f:  ldfld      class Test_refarg_f4_il.AA Test_refarg_f4_il.AA::self1
      IL_0014:  call       instance void Test_refarg_f4_il.AA::CheckFields()
      IL_0019:  ldarg.0
      IL_001a:  ldfld      class Test_refarg_f4_il.AA Test_refarg_f4_il.AA::self2
      IL_001f:  brfalse.s  IL_002c

      IL_0021:  ldarg.0
      IL_0022:  ldfld      class Test_refarg_f4_il.AA Test_refarg_f4_il.AA::self2
      IL_0027:  call       instance void Test_refarg_f4_il.AA::CheckFields()
      IL_002c:  ret
    } // end of method 'AA::Finalize'

  } // end of class 'AA'

  .class public auto ansi App extends ['mscorlib']System.Object
  {
    .field private static class Test_refarg_f4_il.AA s_aa
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
            Test_refarg_f4_il(float32& n) il managed
    {
      // Code size       50 (0x32)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldnull
      IL_0001:  stsfld     class Test_refarg_f4_il.AA Test_refarg_f4_il.App::s_aa
      IL_0006:  call       void Test_refarg_f4_il.App::Litter()
      IL_000b:  ldarg.0
      IL_000c:  ldind.r4
      IL_000d:  ldc.r4     12.
      IL_0012:  beq.s      IL_0022

      IL_0014:  ldstr      "*** failed ***"
      IL_0019:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_001e:  ldc.i4.1
      IL_001f:  stloc.0
      IL_0020:  br.s       IL_0030

      IL_0022:  ldstr      "*** passed ***"
      IL_0027:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_002c:  ldc.i4    0x64
      IL_002d:  stloc.0
      IL_002e:  br.s       IL_0030

      IL_0030:  ldloc.0
      IL_0031:  ret
    } // end of method 'App::Test_refarg_f4_il'

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
      IL_0000:  ldsfld     class Test_refarg_f4_il.AA Test_refarg_f4_il.App::s_aa
      IL_0005:  ldflda     float32 Test_refarg_f4_il.AA::mm2
      IL_000a:  call       int32 Test_refarg_f4_il.App::Test_refarg_f4_il(float32&)
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
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  newobj     instance void Test_refarg_f4_il.AA::.ctor(int32)
      IL_0006:  stsfld     class Test_refarg_f4_il.AA Test_refarg_f4_il.App::s_aa
      IL_000b:  ret
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

} // end of namespace 'Test_refarg_f4_il'

//*********** DISASSEMBLY COMPLETE ***********************
