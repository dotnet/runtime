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
// MVID: {27E0B251-2BC1-4652-9A1C-3EFA7F79CFC4}
.namespace Test_refarg_box_val_il
{
  .class value private auto ansi sealed Val extends ['mscorlib']System.ValueType
  {
    .field public float64 padding1
    .field public class System.Object padding2
    .field public unsigned int8 padding3
    .field public unsigned int16 padding4
    .field public int32 'value'
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(int32 val) il managed
    {
      // Code size       55 (0x37)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldc.r8     0.
      IL_000a:  stfld      float64 Test_refarg_box_val_il.Val::padding1
      IL_000f:  ldarg.0
      IL_0010:  ldc.i4.s   11
      IL_0012:  stloc.0
      IL_0013:  ldloc.s   V_0
      IL_0015:  box     ['mscorlib']System.Int32
      IL_001a:  stfld      class System.Object Test_refarg_box_val_il.Val::padding2
      IL_001f:  ldarg.0
      IL_0020:  ldc.i4.s   11
      IL_0022:  stfld      unsigned int8 Test_refarg_box_val_il.Val::padding3
      IL_0027:  ldarg.0
      IL_0028:  ldc.i4.s   11
      IL_002a:  stfld      unsigned int16 Test_refarg_box_val_il.Val::padding4
      IL_002f:  ldarg.0
      IL_0030:  ldarg.1
      IL_0031:  stfld      int32 Test_refarg_box_val_il.Val::'value'
      IL_0036:  ret
    } // end of method 'Val::.ctor'

  } // end of class 'Val'

  .class public auto ansi App extends ['mscorlib']System.Object
  {
    .field private static class System.Object s_aa
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
            Test_refarg_box_val_il(value class Test_refarg_box_val_il.Val& n) il managed
    {
      // Code size       54 (0x36)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldnull
      IL_0001:  stsfld     class System.Object Test_refarg_box_val_il.App::s_aa
      IL_0006:  call       void Test_refarg_box_val_il.App::Litter()
      IL_000b:  ldarg.0
      IL_000c:  ldfld      int32 Test_refarg_box_val_il.Val::'value'
      IL_0011:  ldc.i4     0x13a
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
    } // end of method 'App::Test_refarg_box_val_il'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       32 (0x20)
      .maxstack  2
      .locals (value class Test_refarg_box_val_il.Val ZZZz, //not used now
               int32 V_1,
               int32 V_2)
      //IL_0000:  ldloc.s   V_0
      //IL_0002:  ldc.i4.0
      //IL_0003:  call       instance void Test_refarg_box_val_il.Val::.ctor(int32)
      //IL_0008:  ldloc.s   V_0
      			ldsfld     class System.Object Test_refarg_box_val_il.App::s_aa
      			unbox      Test_refarg_box_val_il.Val
      			
      IL_000a:  call       int32 Test_refarg_box_val_il.App::Test_refarg_box_val_il(value class Test_refarg_box_val_il.Val&)
      IL_000f:  stloc.1
      IL_0010:  call       void ['mscorlib']System.GC::Collect()
      IL_0015:  call       void ['mscorlib']System.GC::WaitForPendingFinalizers()
      IL_001a:  ldloc.1
      IL_001b:  stloc.2
      IL_001c:  br.s       IL_001e

      IL_001e:  ldloc.2
      IL_001f:  ret
    } // end of method 'App::Main'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       24 (0x18)
      .maxstack  2
      .locals (value class Test_refarg_box_val_il.Val V_0)
      IL_0000:  ldc.i4     0x13a
      IL_0005:  newobj     instance void Test_refarg_box_val_il.Val::.ctor(int32)
      IL_000a:  stloc.0
      IL_000b:  ldloc.s   V_0
      IL_000d:  box     Test_refarg_box_val_il.Val
      IL_0012:  stsfld     class System.Object Test_refarg_box_val_il.App::s_aa
      IL_0017:  ret
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

} // end of namespace 'Test_refarg_box_val_il'

//*********** DISASSEMBLY COMPLETE ***********************
