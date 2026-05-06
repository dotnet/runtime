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
// MVID: {DB5DE5DC-5255-410E-B565-E88E6CF43E3E}
.namespace Test_refarg_i2_il
{
  .class private auto ansi AA extends ['mscorlib']System.Object
  {
    .field private int16 pad1
    .field public int32 mm
    .field public class Test_refarg_i2_il.AA self
    .method family hidebysig virtual instance void
            Finalize() il managed
    {
      // Code size       67 (0x43)
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int16 Test_refarg_i2_il.AA::pad1
      IL_0006:  ldc.i4     0xbf
      IL_000b:  bne.un.s   IL_0017

      IL_000d:  ldarg.0
      IL_000e:  ldfld      int32 Test_refarg_i2_il.AA::mm
      IL_0013:  ldc.i4.s   11
      IL_0015:  beq.s      IL_001d

      IL_0017:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_001c:  throw

      IL_001d:  ldarg.0
      IL_001e:  ldfld      class Test_refarg_i2_il.AA Test_refarg_i2_il.AA::self
      IL_0023:  brfalse.s  IL_0042

      IL_0025:  ldarg.0
      IL_0026:  ldfld      int16 Test_refarg_i2_il.AA::pad1
      IL_002b:  ldc.i4     0xbf
      IL_0030:  bne.un.s   IL_003c

      IL_0032:  ldarg.0
      IL_0033:  ldfld      int32 Test_refarg_i2_il.AA::mm
      IL_0038:  ldc.i4.s   11
      IL_003a:  beq.s      IL_0042

      IL_003c:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_0041:  throw

      IL_0042:  ret
    } // end of method 'AA::Finalize'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       33 (0x21)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4     0xbf
      IL_0006:  stfld      int16 Test_refarg_i2_il.AA::pad1
      IL_000b:  ldarg.0
      IL_000c:  ldc.i4.s   11
      IL_000e:  stfld      int32 Test_refarg_i2_il.AA::mm
      IL_0013:  ldarg.0
      IL_0014:  ldnull
      IL_0015:  stfld      class Test_refarg_i2_il.AA Test_refarg_i2_il.AA::self
      IL_001a:  ldarg.0
      IL_001b:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0020:  ret
    } // end of method 'AA::.ctor'

  } // end of class 'AA'

  .class public auto ansi App extends ['mscorlib']System.Object
  {
    .field private static class Test_refarg_i2_il.AA s_aa
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
            Test_refarg_i2_il(int32& n) il managed
    {
      // Code size       47 (0x2f)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldnull
      IL_0001:  stsfld     class Test_refarg_i2_il.AA Test_refarg_i2_il.App::s_aa
      IL_0006:  call       void Test_refarg_i2_il.App::Litter()
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
    } // end of method 'App::Test_refarg_i2_il'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       47 (0x2f)
      .maxstack  2
      .locals (int32 V_0,
               int32 V_1)
      IL_0000:  ldsfld     class Test_refarg_i2_il.AA Test_refarg_i2_il.App::s_aa
      IL_0005:  newobj     instance void Test_refarg_i2_il.AA::.ctor()
      IL_000a:  stfld      class Test_refarg_i2_il.AA Test_refarg_i2_il.AA::self
      IL_000f:  ldsfld     class Test_refarg_i2_il.AA Test_refarg_i2_il.App::s_aa
      IL_0014:  ldflda     int32 Test_refarg_i2_il.AA::mm
      IL_0019:  call       int32 Test_refarg_i2_il.App::Test_refarg_i2_il(int32&)
      IL_001e:  stloc.0
      IL_001f:  call       void ['mscorlib']System.GC::Collect()
      IL_0024:  call       void ['mscorlib']System.GC::WaitForPendingFinalizers()
      IL_0029:  ldloc.0
      IL_002a:  stloc.1
      IL_002b:  br.s       IL_002d

      IL_002d:  ldloc.1
      IL_002e:  ret
    } // end of method 'App::Main'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  newobj     instance void Test_refarg_i2_il.AA::.ctor()
      IL_0005:  stsfld     class Test_refarg_i2_il.AA Test_refarg_i2_il.App::s_aa
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

} // end of namespace 'Test_refarg_i2_il'

//*********** DISASSEMBLY COMPLETE ***********************
