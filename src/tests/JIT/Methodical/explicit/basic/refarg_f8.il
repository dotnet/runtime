// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'refarg_f8'// as "refarg_f8"
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {37A8EE05-2AD1-417E-ABC1-6C1463EFE15F}
.namespace Test_refarg_f8_il
{
  .class private auto ansi AA extends ['mscorlib']System.Object
  {
    .field public float64 mm1
    .field public class Test_refarg_f8_il.AA self1
    .field public class Test_refarg_f8_il.AA self2
    .field public float64 mm2
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(int32 reclevel) il managed
    {
      // Code size       103 (0x67)
      .maxstack  3
      IL_0000:  ldarg.0
      IL_0001:  ldc.r8     11.314
      IL_000a:  stfld      float64 Test_refarg_f8_il.AA::mm1
      IL_000f:  ldarg.0
      IL_0010:  ldnull
      IL_0011:  stfld      class Test_refarg_f8_il.AA Test_refarg_f8_il.AA::self1
      IL_0016:  ldarg.0
      IL_0017:  ldnull
      IL_0018:  stfld      class Test_refarg_f8_il.AA Test_refarg_f8_il.AA::self2
      IL_001d:  ldarg.0
      IL_001e:  ldc.r8     12.
      IL_0027:  stfld      float64 Test_refarg_f8_il.AA::mm2
      IL_002c:  ldarg.0
      IL_002d:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0032:  ldarg.1
      IL_0033:  ldc.i4.s   100
      IL_0035:  bge.s      IL_0058

      IL_0037:  ldarg.0
      IL_0038:  ldarg.1
      IL_0039:  ldc.i4.1
      IL_003a:  add
      IL_003b:  newobj     instance void Test_refarg_f8_il.AA::.ctor(int32)
      IL_0040:  stfld      class Test_refarg_f8_il.AA Test_refarg_f8_il.AA::self1
      IL_0045:  ldarg.0
      IL_0046:  ldarg.0
      IL_0047:  ldfld      class Test_refarg_f8_il.AA Test_refarg_f8_il.AA::self1
      IL_004c:  ldfld      class Test_refarg_f8_il.AA Test_refarg_f8_il.AA::self1
      IL_0051:  stfld      class Test_refarg_f8_il.AA Test_refarg_f8_il.AA::self2
      IL_0056:  br.s       IL_0066

      IL_0058:  ldarg.0
      IL_0059:  ldarg.0
      IL_005a:  stfld      class Test_refarg_f8_il.AA Test_refarg_f8_il.AA::self1
      IL_005f:  ldarg.0
      IL_0060:  ldnull
      IL_0061:  stfld      class Test_refarg_f8_il.AA Test_refarg_f8_il.AA::self2
      IL_0066:  ret
    } // end of method 'AA::.ctor'

    .method family hidebysig instance void
            CheckFields() il managed
    {
      // Code size       41 (0x29)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      float64 Test_refarg_f8_il.AA::mm2
      IL_0006:  ldc.r8     12.
      IL_000f:  bne.un.s   IL_0022

      IL_0011:  ldarg.0
      IL_0012:  ldfld      float64 Test_refarg_f8_il.AA::mm1
      IL_0017:  ldc.r8     11.314
      IL_0020:  beq.s      IL_0028

      IL_0022:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_0027:  throw

      IL_0028:  ret
    } // end of method 'AA::CheckFields'

    .method family hidebysig virtual instance void
            Finalize() il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void Test_refarg_f8_il.AA::CheckFields()
      IL_0006:  ldarg.0
      IL_0007:  ldfld      class Test_refarg_f8_il.AA Test_refarg_f8_il.AA::self1
      IL_000c:  brfalse.s  IL_0019

      IL_000e:  ldarg.0
      IL_000f:  ldfld      class Test_refarg_f8_il.AA Test_refarg_f8_il.AA::self1
      IL_0014:  call       instance void Test_refarg_f8_il.AA::CheckFields()
      IL_0019:  ldarg.0
      IL_001a:  ldfld      class Test_refarg_f8_il.AA Test_refarg_f8_il.AA::self2
      IL_001f:  brfalse.s  IL_002c

      IL_0021:  ldarg.0
      IL_0022:  ldfld      class Test_refarg_f8_il.AA Test_refarg_f8_il.AA::self2
      IL_0027:  call       instance void Test_refarg_f8_il.AA::CheckFields()
      IL_002c:  ret
    } // end of method 'AA::Finalize'

  } // end of class 'AA'

  .class public auto ansi App extends ['mscorlib']System.Object
  {
    .field private static class Test_refarg_f8_il.AA s_aa
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
            Test_refarg_f8_il(float64& n) il managed
    {
      // Code size       54 (0x36)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldnull
      IL_0001:  stsfld     class Test_refarg_f8_il.AA Test_refarg_f8_il.App::s_aa
      IL_0006:  call       void Test_refarg_f8_il.App::Litter()
      IL_000b:  ldarg.0
      IL_000c:  ldind.r8
      IL_000d:  ldc.r8     11.314
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
    } // end of method 'App::Test_refarg_f8_il'

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
      IL_0000:  ldsfld     class Test_refarg_f8_il.AA Test_refarg_f8_il.App::s_aa
      IL_0005:  ldflda     float64 Test_refarg_f8_il.AA::mm1
      IL_000a:  call       int32 Test_refarg_f8_il.App::Test_refarg_f8_il(float64&)
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
      IL_0001:  newobj     instance void Test_refarg_f8_il.AA::.ctor(int32)
      IL_0006:  stsfld     class Test_refarg_f8_il.AA Test_refarg_f8_il.App::s_aa
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

} // end of namespace 'Test_refarg_f8_il'

//*********** DISASSEMBLY COMPLETE ***********************
