// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'refarg_i4'// as "refarg_i4"
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {0D4BD404-2598-466F-9449-2F343E513C7B}
.namespace Test_refarg_i4_il
{
  .class private auto ansi AA extends ['mscorlib']System.Object
  {
    .field public class Test_refarg_i4_il.AA self3
    .field public class Test_refarg_i4_il.AA self4
    .field private int32 pad1
    .field public int32 mm
    .field public class Test_refarg_i4_il.AA self1
    .field public class Test_refarg_i4_il.AA self2
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(int32 reclevel) il managed
    {
      // Code size       98 (0x62)
      .maxstack  5
      .locals (class Test_refarg_i4_il.AA V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldnull
      IL_0002:  stfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self3
      IL_0007:  ldarg.0
      IL_0008:  ldnull
      IL_0009:  stfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self4
      IL_000e:  ldarg.0
      IL_000f:  ldc.i4     0xbf
      IL_0014:  stfld      int32 Test_refarg_i4_il.AA::pad1
      IL_0019:  ldarg.0
      IL_001a:  ldc.i4.s   11
      IL_001c:  stfld      int32 Test_refarg_i4_il.AA::mm
      IL_0021:  ldarg.0
      IL_0022:  ldnull
      IL_0023:  stfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self1
      IL_0028:  ldarg.0
      IL_0029:  ldnull
      IL_002a:  stfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self2
      IL_002f:  ldarg.0
      IL_0030:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0035:  ldarg.0
      IL_0036:  ldarg.0
      IL_0037:  ldarg.0
      IL_0038:  ldarg.0
      IL_0039:  dup
      IL_003a:  stloc.0
      IL_003b:  stfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self4
      IL_0040:  ldloc.0
      IL_0041:  dup
      IL_0042:  stloc.0
      IL_0043:  stfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self2
      IL_0048:  ldloc.0
      IL_0049:  stfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self1
      IL_004e:  ldarg.1
      IL_004f:  ldc.i4.s   100
      IL_0051:  bge.s      IL_0061

      IL_0053:  ldarg.0
      IL_0054:  ldarg.1
      IL_0055:  ldc.i4.1
      IL_0056:  add
      IL_0057:  newobj     instance void Test_refarg_i4_il.AA::.ctor(int32)
      IL_005c:  stfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self3
      IL_0061:  ret
    } // end of method 'AA::.ctor'

    .method family hidebysig instance void
            CheckFields() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  brtrue.s   IL_0009

      IL_0003:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_0008:  throw

      IL_0009:  ldarg.0
      IL_000a:  ldfld      int32 Test_refarg_i4_il.AA::pad1
      IL_000f:  ldc.i4     0xbf
      IL_0014:  bne.un.s   IL_0020

      IL_0016:  ldarg.0
      IL_0017:  ldfld      int32 Test_refarg_i4_il.AA::mm
      IL_001c:  ldc.i4.s   11
      IL_001e:  beq.s      IL_0026

      IL_0020:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_0025:  throw

      IL_0026:  ret
    } // end of method 'AA::CheckFields'

    .method family hidebysig virtual instance void
            Finalize() il managed
    {
      // Code size       83 (0x53)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void Test_refarg_i4_il.AA::CheckFields()
      IL_0006:  ldarg.0
      IL_0007:  ldfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self1
      IL_000c:  brfalse.s  IL_0019

      IL_000e:  ldarg.0
      IL_000f:  ldfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self1
      IL_0014:  call       instance void Test_refarg_i4_il.AA::CheckFields()
      IL_0019:  ldarg.0
      IL_001a:  ldfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self2
      IL_001f:  brfalse.s  IL_002c

      IL_0021:  ldarg.0
      IL_0022:  ldfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self2
      IL_0027:  call       instance void Test_refarg_i4_il.AA::CheckFields()
      IL_002c:  ldarg.0
      IL_002d:  ldfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self3
      IL_0032:  brfalse.s  IL_003f

      IL_0034:  ldarg.0
      IL_0035:  ldfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self3
      IL_003a:  call       instance void Test_refarg_i4_il.AA::CheckFields()
      IL_003f:  ldarg.0
      IL_0040:  ldfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self4
      IL_0045:  brfalse.s  IL_0052

      IL_0047:  ldarg.0
      IL_0048:  ldfld      class Test_refarg_i4_il.AA Test_refarg_i4_il.AA::self4
      IL_004d:  call       instance void Test_refarg_i4_il.AA::CheckFields()
      IL_0052:  ret
    } // end of method 'AA::Finalize'

  } // end of class 'AA'

  .class public auto ansi App extends ['mscorlib']System.Object
  {
    .field private static class Test_refarg_i4_il.AA s_aa
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
            Test_refarg_i4_il(int32& n) il managed
    {
      // Code size       47 (0x2f)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldnull
      IL_0001:  stsfld     class Test_refarg_i4_il.AA Test_refarg_i4_il.App::s_aa
      IL_0006:  call       void Test_refarg_i4_il.App::Litter()
      IL_000b:  ldarg.0
      IL_000c:  ldind.i4
      IL_000d:  ldc.i4.s   11
      IL_000f:  beq.s      IL_001f

      IL_0011:  ldstr      "*** failed ***"
      IL_0016:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_001b:  ldc.i4.1
      IL_001c:  stloc.0
      IL_001d:  br.s       IL_002d

      IL_001f:  ldstr      "*** passed ***"
      IL_0024:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0029:  ldc.i4    0x64
      IL_002a:  stloc.0
      IL_002b:  br.s       IL_002d

      IL_002d:  ldloc.0
      IL_002e:  ret
    } // end of method 'App::Test_refarg_i4_il'

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
      IL_0000:  ldsfld     class Test_refarg_i4_il.AA Test_refarg_i4_il.App::s_aa
      IL_0005:  ldflda     int32 Test_refarg_i4_il.AA::mm
      IL_000a:  call       int32 Test_refarg_i4_il.App::Test_refarg_i4_il(int32&)
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
      IL_0001:  newobj     instance void Test_refarg_i4_il.AA::.ctor(int32)
      IL_0006:  stsfld     class Test_refarg_i4_il.AA Test_refarg_i4_il.App::s_aa
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

} // end of namespace 'Test_refarg_i4_il'

//*********** DISASSEMBLY COMPLETE ***********************
