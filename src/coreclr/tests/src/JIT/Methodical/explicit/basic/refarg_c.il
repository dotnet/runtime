// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'refarg_c'// as "refarg_c"
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.module 'refarg_c.exe'
// MVID: {E2604B1E-0A0A-41BB-B225-61A4B5873CA3}
.namespace Test
{
  .class private auto ansi AA extends ['mscorlib']System.Object
  {
    .field family wchar pad1
    .field private class Test.AA self
    .field public int32 mm
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.s   122
      IL_0003:  stfld      wchar Test.AA::pad1
      IL_0008:  ldarg.0
      IL_0009:  ldnull
      IL_000a:  stfld      class Test.AA Test.AA::self
      IL_000f:  ldarg.0
      IL_0010:  ldc.i4.s   11
      IL_0012:  stfld      int32 Test.AA::mm
      IL_0017:  ldarg.0
      IL_0018:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_001d:  ldarg.0
      IL_001e:  ldarg.0
      IL_001f:  stfld      class Test.AA Test.AA::self
      IL_0024:  ret
    } // end of method 'AA::.ctor'

    .method family hidebysig virtual instance void
            Finalize() il managed
    {
      // Code size       61 (0x3d)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      wchar Test.AA::pad1
      IL_0006:  ldc.i4.s   122
      IL_0008:  bne.un.s   IL_0014

      IL_000a:  ldarg.0
      IL_000b:  ldfld      int32 Test.AA::mm
      IL_0010:  ldc.i4.s   11
      IL_0012:  beq.s      IL_001a

      IL_0014:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_0019:  throw

      IL_001a:  ldarg.0
      IL_001b:  ldfld      class Test.AA Test.AA::self
      IL_0020:  brfalse.s  IL_003c

      IL_0022:  ldarg.0
      IL_0023:  ldfld      wchar Test.AA::pad1
      IL_0028:  ldc.i4.s   122
      IL_002a:  bne.un.s   IL_0036

      IL_002c:  ldarg.0
      IL_002d:  ldfld      int32 Test.AA::mm
      IL_0032:  ldc.i4.s   11
      IL_0034:  beq.s      IL_003c

      IL_0036:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_003b:  throw

      IL_003c:  ret
    } // end of method 'AA::Finalize'

  } // end of class 'AA'

  .class private auto ansi App extends ['mscorlib']System.Object
  {
    .field private static class Test.AA s_aa
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
            Test(int32& n) il managed
    {
      // Code size       47 (0x2f)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldnull
      IL_0001:  stsfld     class Test.AA Test.App::s_aa
      IL_0006:  call       void Test.App::Litter()
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
    } // end of method 'App::Test'

    .method private hidebysig static int32
            Main() il managed
    {
      .entrypoint
      // Code size       32 (0x20)
      .maxstack  1
      .locals (int32 V_0,
               int32 V_1)
      IL_0000:  ldsfld     class Test.AA Test.App::s_aa
      IL_0005:  ldflda     int32 Test.AA::mm
      IL_000a:  call       int32 Test.App::Test(int32&)
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
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  newobj     instance void Test.AA::.ctor()
      IL_0005:  stsfld     class Test.AA Test.App::s_aa
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

} // end of namespace 'Test'

//*********** DISASSEMBLY COMPLETE ***********************
