// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//different datatype in try block
//
.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{
}
.class private sealed VT
       extends [mscorlib]System.ValueType
{
  .field public int32 m
  .field public static int32 n
  .method private  specialname rtspecialname static 
          void .cctor() il managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.3
    IL_0001:  stsfld     int32 VT::n
    IL_0006:  ret
  } // end of method VT::.cctor

} // end of class VT

.class private sealed Days
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal value class Days Sun = int32(0x00000000)
  .field public static literal value class Days Mon = int32(0x00000001)
} // end of class Days

.class public auto ansi sealed delegate1
       extends [mscorlib]System.MulticastDelegate
{
  .method public  specialname rtspecialname 
          instance void  .ctor(class System.Object o,
                               native unsigned int 'method') runtime managed
  {
  } // end of method delegate1::.ctor

  .method public  virtual instance void
          Invoke() runtime managed
  {
  } // end of method delegate1::Invoke

  .method public  newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                      class System.Object o) runtime managed
  {
  } // end of method delegate1::BeginInvoke

  .method public  newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method delegate1::EndInvoke

} // end of class delegate1

.class interface public abstract auto ansi ITest
{
  .method public  newslot virtual abstract 
          instance int32 test() il managed
  {
  } // end of method ITest::test

} // end of class ITest

.class public auto ansi beforefieldinit CTest
       extends [mscorlib]System.Object
       implements ITest
{
  .field private class delegate1 dg
  .method public  specialname instance void 
          add_dg(class delegate1 'value') il managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class delegate1 CTest::dg
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate,
                                                                                            class [mscorlib]System.Delegate)
    IL_000d:  castclass  delegate1
    IL_0012:  stfld      class delegate1 CTest::dg
    IL_0017:  ret
  } // end of method CTest::add_dg

  .method public  specialname instance void 
          remove_dg(class delegate1 'value') il managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class delegate1 CTest::dg
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate,
                                                                                           class [mscorlib]System.Delegate)
    IL_000d:  castclass  delegate1
    IL_0012:  stfld      class delegate1 CTest::dg
    IL_0017:  ret
  } // end of method CTest::remove_dg

  .method public  instance void f() il managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method CTest::f

  .method public  newslot final virtual 
          instance int32 test() il managed
  {
    // Code size       6 (0x6)
    .maxstack  20
    .locals (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method CTest::test

  .method public  specialname rtspecialname 
          instance void .ctor() il managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CTest::.ctor

  .event delegate1 dg
  {
    .addon instance void CTest::add_dg(class delegate1)
    .removeon instance void CTest::remove_dg(class delegate1)
  } // end of event CTest::dg
} // end of class CTest

.class public try1
       extends [mscorlib]System.Object
{
  .method public  static void f1() il managed
  {
    // Code size       6 (0x6)
    .maxstack  20
    .try
    {
      IL_0000:  leave.s    IL_0005

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0002:  pop
      IL_0003:  leave.s    IL_0005

    }  // end handler
    IL_0005:  ret
  } // end of method try1::f1

  .method public  static void f2() il managed
  {
    // Code size       18 (0x12)
    .maxstack  20
    .locals (int32 V_0,
             int32 V_1)
    IL_0000:  ldc.i4     0xffff63c0
    IL_0005:  stloc.0
    .try
    {
      IL_0006:  ldc.i4     0xffff63c0
      //IL_000b:  stloc.1
      IL_000c:  leave.s    IL_0011

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_000e:  pop
      IL_000f:  leave.s    IL_0011

    }  // end handler
    IL_0011:  ret
  } // end of method try1::f2

  .method public  static void f3() il managed
  {
    // Code size       38 (0x26)
  .maxstack  20
  .locals (unsigned int32 V_0,
           unsigned int32 V_1,
           unsigned int32 V_2)
  ldc.i4     0x9c40
  stloc.0
  ldloc.0
  stloc.2
  ldloc.2
  ldc.i4.1
  beq.s      BL_0016
  ldloc.2
  ldc.i4     0x9c40
  beq.s      BL_0018
  br.s       BL_0025
  BL_0016:   br.s       BL_0025
  .try
  {
    BL_0018:  ldc.i4     0x9c40
    //IL_001d:  stloc.1
    BL_001e:  leave.s    BL_0023
  }  // end .try
  catch [mscorlib]System.Object 
  {
    //IL_0020:  pop
    BL_0021:  leave.s    BL_0023
  }  // end handler
  BL_0023:  br.s       BL_0025
  BL_0025:  ret
  } // end of method try1::f3

  .method public  static void f4() il managed
  {
    // Code size       28 (0x1c)
  .maxstack  20
  .locals (int16 V_0,
           int16 V_1)
  ldc.i4     0xfffffc18
  stloc.0
  ldloc.0
  ldc.i4     0x7d0
  add
  ldc.i4.0
  ble.s      BL_001b
  .try
  {
    ldc.i4     0xfffffc18
    //stloc.1
    leave.s    BL_001b
  }  // end .try
  catch [mscorlib]System.Object 
  {
    //pop
    leave.s    BL_001b
  }  // end handler
  BL_001b:  ret
  } // end of method try1::f4

  .method public  static void f5() il managed
  {
    // Code size       35 (0x23)
  .maxstack  20
  .locals (unsigned int16 V_0,
           unsigned int16 V_1)
  ldc.i4     0x3e8
  stloc.0
  br.s       BL_001d
  .try
  {
    BL_0008: ldc.i4     0x3e8
    //stloc.1
    leave.s    BL_0013
  }  // end .try
  catch [mscorlib]System.Object 
  {
    //pop
    leave.s    BL_0013
  }  // end handler
  BL_0013: ldloc.0
  conv.i4
  ldc.i4     0x1f4
  sub
  conv.u2
  stloc.0
  BL_001d: ldloc.0
  conv.i4
  ldc.i4.0
  blt.s      BL_0008
  ret
  } // end of method try1::f5

  .method public  static void f6() il managed
  {
    // Code size       12 (0xc)
    .maxstack  20
    .locals (unsigned int8 V_0,
             unsigned int8 V_1)
    IL_0000:  ldc.i4.s   20
    IL_0002:  stloc.0
    .try
    {
      IL_0003:  ldc.i4.s   20
      //IL_0005:  stloc.1
      IL_0006:  leave.s    IL_000b

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0008:  pop
      IL_0009:  leave.s    IL_000b

    }  // end handler
    IL_000b:  ret
  } // end of method try1::f6

  .method public  static void f7() il managed
  {
    // Code size       12 (0xc)
    .maxstack  20
    .locals (int8 V_0,
             int8 V_1)
    IL_0000:  ldc.i4.s   -20
    IL_0002:  stloc.0
    .try
    {
      IL_0003:  ldc.i4.s   -20
      //IL_0005:  stloc.1
      IL_0006:  leave.s    IL_000b

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0008:  pop
      IL_0009:  leave.s    IL_000b

    }  // end handler
    IL_000b:  ret
  } // end of method try1::f7

  .method public  static void f8() il managed
  {
    // Code size       26 (0x1a)
    .maxstack  20
    .locals (int64 V_0,
             int64 V_1)
    IL_0000:  ldc.i8     0x8000000000000000
    IL_0009:  stloc.0
    .try
    {
      IL_000a:  ldc.i8     0x7fffffffffffffff
      //IL_0013:  stloc.1
      IL_0014:  leave.s    IL_0019

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0016:  pop
      IL_0017:  leave.s    IL_0019

    }  // end handler
    IL_0019:  ret
  } // end of method try1::f8

  .method public  static void f9() il managed
  {
    // Code size       12 (0xc)
    .maxstack  20
    .locals (unsigned int64 V_0,
             unsigned int64 V_1)
    IL_0000:  ldc.i4.0
    IL_0001:  conv.i8
    IL_0002:  stloc.0
    .try
    {
      IL_0003:  ldc.i4.m1
      IL_0004:  conv.i8
      //IL_0005:  stloc.1
      IL_0006:  leave.s    IL_000b

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0008:  pop
      IL_0009:  leave.s    IL_000b

    }  // end handler
    IL_000b:  ret
  } // end of method try1::f9

  .method public  static void f10() il managed
  {
    // Code size       12 (0xc)
    .maxstack  20
    .locals (wchar V_0,
             wchar V_1)
    IL_0000:  ldc.i4.s   49
    IL_0002:  stloc.0
    .try
    {
      IL_0003:  ldc.i4.s   65
      //IL_0005:  stloc.1
      IL_0006:  leave.s    IL_000b

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0008:  pop
      IL_0009:  leave.s    IL_000b

    }  // end handler
    IL_000b:  ret
  } // end of method try1::f10

  .method public  static void f11() il managed
  {
    // Code size       18 (0x12)
    .maxstack  20
    .locals (float32 V_0,
             float32 V_1)
    IL_0000:  ldc.r4     (E1 7A 24 C0)
    IL_0005:  stloc.0
    .try
    {
      IL_0006:  ldc.r4     (E1 7A 24 40)
      //IL_000b:  stloc.1
      IL_000c:  leave.s    IL_0011

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_000e:  pop
      IL_000f:  leave.s    IL_0011

    }  // end handler
    IL_0011:  ret
  } // end of method try1::f11

  .method public  static void f12() il managed
  {
    // Code size       26 (0x1a)
    .maxstack  20
    .locals (float64 V_0,
             float64 V_1)
    IL_0000:  ldc.r8     -1.2888888888888881
    IL_0009:  stloc.0
    .try
    {
      IL_000a:  ldc.r8     1.2888888888888881
      //IL_0013:  stloc.1
      IL_0014:  leave.s    IL_0019

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0016:  pop
      IL_0017:  leave.s    IL_0019

    }  // end handler
    IL_0019:  ret
  } // end of method try1::f12

  .method public  static void f13() il managed
  {
    // Code size       10 (0xa)
    .maxstack  20
    .locals (bool V_0,
             bool V_1)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    .try
    {
      IL_0002:  ldc.i4.0
      //IL_0003:  stloc.1
      IL_0004:  leave.s    IL_0009

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0006:  pop
      IL_0007:  leave.s    IL_0009

    }  // end handler
    IL_0009:  ret
  } // end of method try1::f13

  .method public  static void f14() il managed
  {
    // Code size       40 (0x28)
    .maxstack  6
    .locals (value class [mscorlib]System.Decimal V_0,
             value class [mscorlib]System.Decimal V_1)
    IL_0000:  ldc.i4     0x2715
    IL_0005:  ldc.i4.0
    IL_0006:  ldc.i4.0
    IL_0007:  ldc.i4     0x80
    IL_000c:  ldc.i4.2
    IL_000d:  newobj     instance void [mscorlib]System.Decimal::.ctor(int32,
                                                                       int32,
                                                                       int32,
                                                                       bool,
                                                                       unsigned int8)
    IL_0012:  stloc.0
    .try
    {
      IL_0013:  ldc.i4     0x676
      IL_0018:  ldc.i4.0
      IL_0019:  ldc.i4.0
      IL_001a:  ldc.i4.0
      IL_001b:  ldc.i4.3
      IL_001c:  newobj     instance void [mscorlib]System.Decimal::.ctor(int32,
                                                                         int32,
                                                                         int32,
                                                                         bool,
                                                                         unsigned int8)
      //IL_0021:  stloc.1
      IL_0022:  leave.s    IL_0027

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0024:  pop
      IL_0025:  leave.s    IL_0027

    }  // end handler
    IL_0027:  ret
  } // end of method try1::f14

  .method public  static void f15() il managed
  {
    // Code size       76 (0x4c)
    .maxstack  20
    .locals (value class VT V_0,
             int32 V_1,
             int32 V_2,
             value class VT V_3,
             int32 V_4,
             int32 V_5)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    VT
    IL_0008:  ldloca.s   V_3
    IL_000a:  initobj    VT
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldc.i4     0xfffffed4
    IL_0017:  stfld      int32 VT::m
    IL_001c:  ldloca.s   V_0
    IL_001e:  ldfld      int32 VT::m
    IL_0023:  stloc.1
    IL_0024:  ldsfld     int32 VT::n
    IL_0029:  stloc.2
    .try
    {
      IL_002a:  ldloca.s   V_3
      IL_002c:  ldc.i4     0x12c
      IL_0031:  stfld      int32 VT::m
      IL_0036:  ldloca.s   V_3
      IL_0038:  ldfld      int32 VT::m
      //IL_003d:  stloc.s    V_4
      IL_003f:  ldsfld     int32 VT::n
      //IL_0044:  stloc.s    V_5
      IL_0046:  leave.s    IL_004b

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0048:  pop
      IL_0049:  leave.s    IL_004b

    }  // end handler
    IL_004b:  ret
  } // end of method try1::f15

  .method public  static void f16() il managed
  {
    // Code size       21 (0x15)
    .maxstack  20
    .locals (value class Days V_0,
             value class Days V_1)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    .try
    {
      IL_0002:  ldc.i4.1
      IL_0003:  stloc.1
      IL_0004:  ldloc.1
      IL_0005:  box        Days
      //IL_000a:  call       void [System.Console]System.Console::WriteLine(class System.Object)
      IL_000f:  leave.s    IL_0014

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0011:  pop
      IL_0012:  leave.s    IL_0014

    }  // end handler
    IL_0014:  ret
  } // end of method try1::f16

  .method public  static void f17() il managed
  {
    // Code size       18 (0x12)
    .maxstack  20
    .locals (class System.Object V_0,
             class System.Object V_1)
    IL_0000:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0005:  stloc.0
    .try
    {
      IL_0006:  newobj     instance void [mscorlib]System.Object::.ctor()
      //IL_000b:  stloc.1
      IL_000c:  leave.s    IL_0011

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_000e:  pop
      IL_000f:  leave.s    IL_0011

    }  // end handler
    IL_0011:  ret
  } // end of method try1::f17

  .method public  static void f18() il managed
  {
    // Code size       18 (0x12)
    .maxstack  20
    .locals (class try1 V_0,
             class try1 V_1)
    IL_0000:  newobj     instance void try1::.ctor()
    IL_0005:  stloc.0
    .try
    {
      IL_0006:  newobj     instance void try1::.ctor()
      //IL_000b:  stloc.1
      IL_000c:  leave.s    IL_0011

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_000e:  pop
      IL_000f:  leave.s    IL_0011

    }  // end handler
    IL_0011:  ret
  } // end of method try1::f18

  .method public  static void f19() il managed
  {
    // Code size       18 (0x12)
    .maxstack  20
    .locals (class System.String V_0,
             class System.String V_1)
    IL_0000:  ldstr      "test string"
    IL_0005:  stloc.0
    .try
    {
      IL_0006:  ldstr      "string test"
      //IL_000b:  stloc.1
      IL_000c:  leave.s    IL_0011

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_000e:  pop
      IL_000f:  leave.s    IL_0011

    }  // end handler
    IL_0011:  ret
  } // end of method try1::f19

  .method public  static void f20a() il managed
  {
    // Code size       44 (0x2c)
    .maxstack  20
    .locals (int32[] V_0,
             int32[] V_1,
             int32 V_2,
             int32[] V_3)
    IL_0000:  ldc.i4.2
    IL_0001:  newarr     [mscorlib]System.Int32
    IL_0006:  stloc.3
    IL_0007:  ldloc.3
    IL_0008:  ldc.i4.0
    IL_0009:  ldc.i4.1
    IL_000a:  stelem.i4
    IL_000b:  ldloc.3
    IL_000c:  ldc.i4.1
    IL_000d:  ldc.i4.2
    IL_000e:  stelem.i4
    IL_000f:  ldloc.3
    IL_0010:  stloc.0
    .try
    {
      IL_0011:  ldc.i4.2
      IL_0012:  newarr     [mscorlib]System.Int32
      //IL_0017:  stloc.3
      //IL_0018:  ldloc.3
      //IL_0019:  ldc.i4.0
      //IL_001a:  ldc.i4.3
      //IL_001b:  stelem.i4
      //IL_001c:  ldloc.3
      //IL_001d:  ldc.i4.1
      //IL_001e:  ldc.i4.4
      //IL_001f:  stelem.i4
      //IL_0020:  ldloc.3
      //IL_0021:  stloc.1
      //IL_0022:  ldloc.1
      //IL_0023:  ldc.i4.0
      //IL_0024:  ldelem.i4
      //IL_0025:  stloc.2
      IL_0026:  leave.s    IL_002b

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0028:  pop
      IL_0029:  leave.s    IL_002b

    }  // end handler
    IL_002b:  ret
  } // end of method try1::f20a

  .method public  static void f20b() il managed
  {
    // Code size       44 (0x2c)
    .maxstack  20
    .locals (int32[] V_0,
             int32[] V_1,
             int32 V_2,
             int32[] V_3)
    IL_0000:  ldc.i4.2
    IL_0001:  newarr     [mscorlib]System.Int32
    IL_0006:  stloc.3
    IL_0007:  ldloc.3
    IL_0008:  ldc.i4.0
    IL_0009:  ldc.i4.1
    IL_000a:  stelem.i4
    IL_000b:  ldloc.3
    IL_000c:  ldc.i4.1
    IL_000d:  ldc.i4.2
    IL_000e:  stelem.i4
    IL_000f:  ldloc.3
    IL_0010:  stloc.0
    .try
    {
      IL_0011:  ldc.i4.2
      IL_0012:  newarr     [mscorlib]System.Int32
      IL_0017:  stloc.3
      IL_0018:  ldloc.3
      IL_0019:  ldc.i4.0
      IL_001a:  ldc.i4.3
      IL_001b:  stelem.i4
      IL_001c:  ldloc.3
      IL_001d:  ldc.i4.1
      IL_001e:  ldc.i4.4
      IL_001f:  stelem.i4
      IL_0020:  ldloc.3
      IL_0021:  stloc.1
      IL_0022:  ldloc.1
      IL_0023:  ldc.i4.0
      IL_0024:  ldelem.i4
      //IL_0025:  stloc.2
      IL_0026:  leave.s    IL_002b

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0028:  pop
      IL_0029:  leave.s    IL_002b

    }  // end handler
    IL_002b:  ret
  } // end of method try1::f20b

  .method public  static void f21() il managed
  {
    // Code size       53 (0x35)
    .maxstack  20
    .locals (int32[0...,0...] V_0,
             int32[0...,0...] V_1,
             int32 V_2)
    IL_0000:  ldc.i4.2
    IL_0001:  ldc.i4.2
    IL_0002:  newobj     instance void int32[0...,0...]::.ctor(int32,
                                                               int32)
    IL_0007:  dup
    IL_0008:  ldtoken    field value class '<GlobalName>'/'$$struct0x6000020-1' '<GlobalName>'::'$$method0x6000020-1'
    IL_000d:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        value class [mscorlib]System.RuntimeFieldHandle)
    IL_0012:  stloc.0
    .try
    {
      IL_0013:  ldc.i4.2
      IL_0014:  ldc.i4.2
      IL_0015:  newobj     instance void int32[0...,0...]::.ctor(int32,
                                                                 int32)
      IL_001a:  dup
      IL_001b:  ldtoken    field value class '<GlobalName>'/'$$struct0x6000020-2' '<GlobalName>'::'$$method0x6000020-2'
      IL_0020:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                          value class [mscorlib]System.RuntimeFieldHandle)
      //IL_0025:  stloc.1
      //IL_0026:  ldloc.1
      //IL_0027:  ldc.i4.0
      //IL_0028:  ldc.i4.1
      //IL_0029:  call       instance int32 int32[0...,0...]::Get(int32,
      //                                                          int32)
      //IL_002e:  stloc.2
      IL_002f:  leave.s    IL_0034

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0031:  pop
      IL_0032:  leave.s    IL_0034

    }  // end handler
    IL_0034:  ret
  } // end of method try1::f21

  .method public  static void f22() il managed
  {
    // Code size       56 (0x38)
    .maxstack  4
    .locals (int32[0...,0...,0...] V_0,
             int32[0...,0...,0...] V_1,
             int32 V_2)
    IL_0000:  ldc.i4.3
    IL_0001:  ldc.i4.1
    IL_0002:  ldc.i4.3
    IL_0003:  newobj     instance void int32[0...,0...,0...]::.ctor(int32,
                                                                    int32,
                                                                    int32)
    IL_0008:  dup
    IL_0009:  ldtoken    field value class '<GlobalName>'/'$$struct0x6000021-1' '<GlobalName>'::'$$method0x6000021-1'
    IL_000e:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        value class [mscorlib]System.RuntimeFieldHandle)
    IL_0013:  stloc.0
    .try
    {
      IL_0014:  ldc.i4.3
      IL_0015:  ldc.i4.1
      IL_0016:  ldc.i4.3
      IL_0017:  newobj     instance void int32[0...,0...,0...]::.ctor(int32,
                                                                      int32,
                                                                      int32)
      IL_001c:  dup
      IL_001d:  ldtoken    field value class '<GlobalName>'/'$$struct0x6000021-2' '<GlobalName>'::'$$method0x6000021-2'
      IL_0022:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                          value class [mscorlib]System.RuntimeFieldHandle)
      IL_0027:  stloc.1
      IL_0028:  ldloc.1
      IL_0029:  ldc.i4.0
      IL_002a:  ldc.i4.0
      IL_002b:  ldc.i4.1
      IL_002c:  call       instance int32 int32[0...,0...,0...]::Get(int32,
                                                                     int32,
                                                                     int32)
      //IL_0031:  stloc.2
      IL_0032:  leave.s    IL_0037

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0034:  pop
      IL_0035:  leave.s    IL_0037

    }  // end handler
    IL_0037:  ret
  } // end of method try1::f22

  .method public  static void f23() il managed
  {
    // Code size       32 (0x20)
    .maxstack  20
    .locals (class CTest V_0,
             int32 V_1,
             class CTest V_2,
             int32 V_3)
    IL_0000:  newobj     instance void CTest::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  callvirt   instance int32 CTest::test()
    IL_000c:  stloc.1
    .try
    {
      IL_000d:  newobj     instance void CTest::.ctor()
      IL_0012:  stloc.2
      IL_0013:  ldloc.2
      IL_0014:  callvirt   instance int32 CTest::test()
      //IL_0019:  stloc.3
      IL_001a:  leave.s    IL_001f

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_001c:  pop
      IL_001d:  leave.s    IL_001f

    }  // end handler
    IL_001f:  ret
  } // end of method try1::f23

  .method public  static void f24() il managed
  {
    // Code size       54 (0x36)
    .maxstack  4
    .locals (class CTest V_0,
             class CTest V_1)
    IL_0000:  newobj     instance void CTest::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldftn      instance void CTest::f()
    IL_000e:  newobj     instance void delegate1::.ctor(class System.Object,
                                                        native unsigned int)
    IL_0013:  callvirt   instance void CTest::add_dg(class delegate1)
    .try
    {
      IL_0018:  newobj     instance void CTest::.ctor()
      IL_001d:  stloc.1
      IL_001e:  ldloc.1
      IL_001f:  ldloc.1
      IL_0020:  ldftn      instance void CTest::f()
      //IL_0026:  newobj     instance void delegate1::.ctor(class System.Object,
      //                                                    native unsigned int)
      //IL_002b:  callvirt   instance void CTest::add_dg(class delegate1)
      IL_0030:  leave.s    IL_0035

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0032:  pop
      IL_0033:  leave.s    IL_0035

    }  // end handler
    IL_0035:  ret
  } // end of method try1::f24

  .method public hidebysig static void  f25(int32 arg) il managed
  {
    // Code size       10 (0xa)
    .maxstack  20
    .locals (int32 V_0,
             int32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  stloc.0
    .try
    {
      IL_0002:  ldarg.0
      //IL_0003:  stloc.1
      IL_0004:  leave.s    IL_0009

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0006:  pop
      IL_0007:  leave.s    IL_0009

    }  // end handler
    IL_0009:  ret
  } // end of method try1::f25

  .method public hidebysig static void  f26(unsigned int16 arg1,
                                            int32 arg2,
                                            class System.String arg3,
                                            class System.Object arg4) il managed
  {
    // Code size       26 (0x1a)
    .maxstack  20
    .locals (unsigned int16 V_0,
             int32 V_1,
             class System.String V_2,
             class System.Object V_3,
             unsigned int16 V_4,
             int32 V_5,
             class System.String V_6,
             class System.Object V_7)
    IL_0000:  ldarg.0
    IL_0001:  stloc.0
    IL_0002:  ldarg.1
    IL_0003:  stloc.1
    IL_0004:  ldarg.2
    IL_0005:  stloc.2
    IL_0006:  ldarg.3
    IL_0007:  stloc.3
    .try
    {
      IL_0008:  ldarg.0
      //IL_0009:  stloc.s    V_4
      IL_000b:  ldarg.1
      //IL_000c:  stloc.s    V_5
      IL_000e:  ldarg.2
      //IL_000f:  stloc.s    V_6
      IL_0011:  ldarg.3
      //IL_0012:  stloc.s    V_7
      IL_0014:  leave.s    IL_0019

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0016:  pop
      IL_0017:  leave.s    IL_0019

    }  // end handler
    IL_0019:  ret
  } // end of method try1::f26

  .method public hidebysig static void f27() il managed
  {
    // Code size       14 (0xe)
    .maxstack  20
    .locals (int16 V_0,
             int16 V_1)
    IL_0000:  ldc.i4     0xfffffc18
    IL_0005:  stloc.0
    .try
    {
      IL_0006:  ldloc.0
      //IL_0007:  stloc.1
      IL_0008:  leave.s    IL_000d

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_000a:  pop
      IL_000b:  leave.s    IL_000d

    }  // end handler
    IL_000d:  ret
  } // end of method try1::f27

  .method public hidebysig static void f28() il managed
  {
    // Code size       67 (0x43)
    .maxstack  20
    .locals (unsigned int8 V_0,
             class System.String V_1,
             int32 V_2,
             class System.String[] V_3,
             unsigned int8 V_4,
             class System.String V_5,
             int32 V_6,
             class System.String[] V_7,
             int32 V_8,
             class System.String[] V_9)
    IL_0000:  ldc.i4.s   20
    IL_0002:  stloc.0
    IL_0003:  ldstr      "f28"
    IL_0008:  stloc.1
    IL_0009:  ldc.i4     0xffff15a0
    IL_000e:  stloc.2
    IL_000f:  ldc.i4.2
    IL_0010:  newarr     [mscorlib]System.String
    IL_0015:  stloc.s    V_9
    IL_0017:  ldloc.s    V_9
    IL_0019:  ldc.i4.0
    IL_001a:  ldstr      "a"
    IL_001f:  stelem.ref
    IL_0020:  ldloc.s    V_9
    IL_0022:  ldc.i4.1
    IL_0023:  ldstr      "b"
    IL_0028:  stelem.ref
    IL_0029:  ldloc.s    V_9
    IL_002b:  stloc.3
    .try
    {
      IL_002c:  ldloc.0
      //IL_002d:  stloc.s    V_4
      IL_002f:  ldloc.1
      //IL_0030:  stloc.s    V_5
      IL_0032:  ldloc.2
      //IL_0033:  stloc.s    V_6
      IL_0035:  ldloc.3
      //IL_0036:  stloc.s    V_7
      IL_0038:  ldloc.3
      IL_0039:  ldlen
      IL_003a:  conv.i4
      //IL_003b:  stloc.s    V_8
      IL_003d:  leave.s    IL_0042

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_003f:  pop
      IL_0040:  leave.s    IL_0042

    }  // end handler
    IL_0042:  ret
  } // end of method try1::f28

  .method public hidebysig static void f29() il managed
  {
    // Code size       24 (0x18)
    .maxstack  20
    .locals (unsigned int16 V_0,
             unsigned int16* V_1,
             unsigned int16 V_2)
    IL_0000:  ldc.i4     0x3e8
    IL_0005:  stloc.0
    .try
    {
      IL_0006:  ldc.i4.2
      IL_0007:  ldc.i4.1
      IL_0008:  mul
      IL_0009:  localloc
      IL_000b:  stloc.1
      IL_000c:  ldloc.1
      IL_000d:  ldloc.0
      IL_000e:  stind.i2
      IL_000f:  ldloc.1
      IL_0010:  ldind.u2
      //IL_0011:  stloc.2
      IL_0012:  leave.s    IL_0017

    }  // end .try
    catch [mscorlib]System.Object 
    {
      //IL_0014:  pop
      IL_0015:  leave.s    IL_0017

    }  // end handler
    IL_0017:  ret
  } // end of method try1::f29

  .method public  static int32 Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       156 (0x9c)
    .maxstack  20
    .locals (bool V_0,
             int32 V_1)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  call       void try1::f1()
    IL_0007:  call       void try1::f2()
    IL_000c:  call       void try1::f3()
    IL_0011:  call       void try1::f4()
    IL_0016:  call       void try1::f5()
    IL_001b:  call       void try1::f6()
    IL_0020:  call       void try1::f7()
    IL_0025:  call       void try1::f8()
    IL_002a:  call       void try1::f9()
    IL_002f:  call       void try1::f10()
    IL_0034:  call       void try1::f11()
    IL_0039:  call       void try1::f12()
    IL_003e:  call       void try1::f13()
    IL_0043:  call       void try1::f14()
    IL_0048:  call       void try1::f15()
    IL_004d:  call       void try1::f16()
    IL_0052:  call       void try1::f17()
    IL_0057:  call       void try1::f18()
    IL_005c:  call       void try1::f19()
    IL_0061:  call       void try1::f20a()
	      call       void try1::f20b()
    IL_0066:  call       void try1::f21()
    IL_006b:  call       void try1::f22()
    IL_0070:  call       void try1::f23()
    IL_0075:  call       void try1::f24()

    ldc.i4     0x9c40
    call       void try1::f25(int32)
    ldc.i4     0xff
    ldc.i4     0x80000000
    ldstr      "f26"
    newobj     instance void [mscorlib]System.Object::.ctor()
    call       void try1::f26(unsigned int16,
                                        int32,
                                        class System.String,
                                        class System.Object)
    call       void try1::f27()
    call       void try1::f28()
    call       void try1::f29()

    IL_007a:  ldloc.0
    IL_007b:  brfalse.s  IL_008c

    IL_007d:  ldstr      "PASSED"
    IL_0082:  call       void [System.Console]System.Console::WriteLine(class System.String)
    IL_0087:  ldc.i4.s   100
    IL_0089:  stloc.1
    IL_008a:  br.s       IL_009a

    IL_008c:  ldstr      "FAILED"
    IL_0091:  call       void [System.Console]System.Console::WriteLine(class System.String)
    IL_0096:  ldc.i4.1
    IL_0097:  stloc.1
    IL_0098:  br.s       IL_009a

    IL_009a:  ldloc.1
    IL_009b:  ret
  } // end of method try1::Main

  .method public hidebysig specialname rtspecialname 
        instance void .ctor()
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method try1::.ctor

} // end of class try1

.class private auto ansi '<GlobalName>'
       extends [mscorlib]System.Object
{
  .class explicit ansi sealed nested private '$$struct0x6000020-1'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 16
  } // end of class '$$struct0x6000020-1'

  .class explicit ansi sealed nested private '$$struct0x6000020-2'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 16
  } // end of class '$$struct0x6000020-2'

  .class explicit ansi sealed nested private '$$struct0x6000021-1'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 36
  } // end of class '$$struct0x6000021-1'

  .class explicit ansi sealed nested private '$$struct0x6000021-2'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 36
  } // end of class '$$struct0x6000021-2'

  .field static assembly value class '<GlobalName>'/'$$struct0x6000020-1' '$$method0x6000020-1' at D_000025A0
  .field static assembly value class '<GlobalName>'/'$$struct0x6000020-2' '$$method0x6000020-2' at D_000025B0
  .field static assembly value class '<GlobalName>'/'$$struct0x6000021-1' '$$method0x6000021-1' at D_00002620
  .field static assembly value class '<GlobalName>'/'$$struct0x6000021-2' '$$method0x6000021-2' at D_00002648
} // end of class '<GlobalName>'

.data D_000025A0 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00) 
.data D_000025B0 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00
                 0B 30 03 00 35 00 00 00 15 00 00 11 18 18 73 07  // .0..5.........s.
                 00 00 0A 25 D0 07 00 00 04 28 08 00 00 0A 0A 18  // ...%.....(......
                 18 73 07 00 00 0A 25 D0 08 00 00 04 28 08 00 00  // .s....%.....(...
                 0A 0B 07 16 17 28 09 00 00 0A 0C DE 03 26 DE 00  // .....(.......&..
                 2A 00 00 00 01 10 00 00 00 00 13 00 1E 31 00 03  // *............1..
                 04 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00) 
.data D_00002620 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00
                 04 00 00 00 05 00 00 00 06 00 00 00 07 00 00 00
                 08 00 00 00 00 00 00 00) 
.data D_00002648 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00
                 04 00 00 00 05 00 00 00 06 00 00 00 07 00 00 00
                 08 00 00 00 0B 30 04 00 38 00 00 00 16 00 00 11  // .....0..8.......
                 19 17 19 73 0A 00 00 0A 25 D0 09 00 00 04 28 08  // ...s....%.....(.
                 00 00 0A 0A 19 17 19 73 0A 00 00 0A 25 D0 0A 00  // .......s....%...
                 00 04 28 08 00 00 0A 0B 07 16 16 17 28 0B 00 00  // ..(.........(...
                 0A 0C DE 03 26 DE 00 2A 01 10 00 00 00 00 14 00  // ....&..*........
                 20 34 00 03 04 00 00 01 00 00 00 00 00 00 00 00  //  4..............
                 00 00 00 00 0B 30 01 00 20 00 00 00 17 00 00 11  // .....0.. .......
                 73 0B 00 00 06 0A 06 6F 0A 00 00 06 0B 73 0B 00  // s......o.....s..
                 00 06 0C 08 6F 0A 00 00 06 0D DE 03 26 DE 00 2A  // ....o.......&..*
                 01 10 00 00 00 00 0D 00 0F 1C 00 03 04 00 00 01
                 00 00 00 00 00 00 00 00 00 00 00 00 0B 30 04 00  // .............0..
                 36 00 00 00 18 00 00 11 73 0B 00 00 06 0A 06 06  // 6.......s.......
                 FE 06 09 00 00 06 73 02 00 00 06 6F 07 00 00 06  // ......s....o....
                 73 0B 00 00 06 0B 07 07 FE 06 09 00 00 06 73 02  // s.............s.
                 00 00 06 6F 07 00 00 06 DE 03 26 DE 00 2A 00 00  // ...o......&..*..
                 01 10 00 00 00 00 18 00 1A 32 00 03 04 00 00 01  // .........2......
                 00 00 00 00 00 00 00 00 00 00 00 00 03 30 01 00  // .............0..
                 9C 00 00 00 19 00 00 11 17 0A 28 0C 00 00 06 28  // ..........(....(
                 0D 00 00 06 28 0E 00 00 06 28 0F 00 00 06 28 10  // ....(....(....(.
                 00 00 06 28 11 00 00 06 28 12 00 00 06 28 13 00  // ...(....(....(..
                 00 06 28 14 00 00 06 28 15 00 00 06 28 16 00 00  // ..(....(....(...
                 06 28 17 00 00 06 28 18 00 00 06 28 19 00 00 06  // .(....(....(....
                 28 1A 00 00 06 28 1B 00 00 06 28 1C 00 00 06 28  // (....(....(....(
                 1D 00 00 06 28 1E 00 00 06 28 1F 00 00 06 28 20  // ....(....(....( 
                 00 00 06 28 21 00 00 06 28 22 00 00 06 28 23 00  // ...(!...("...(#.
                 00 06 06 2C 0F 72 31 00 00 70 28 0C 00 00 0A 1F  // ...,.r1..p(.....
                 64 0B 2B 0E 72 3F 00 00 70 28 0C 00 00 0A 17 0B  // d.+.r?..p(......
                 2B 00 07 2A 1E 02 28 04 00 00 0A 2A 42 53 4A 42  // +..*..(....*BSJB
                 01 00 01 00 00 00 00 00 10 00 00 00 76 31 2E 30  // ............v1.0
                 2E 32 34 31 33 46 53 54 43 48 4B 00 00 00 05 00  // .2413FSTCHK.....
                 70 00 00 00 F8 04 00 00 23 7E 00 00 68 05 00 00  // p.......#~..h...
                 0C 03 00 00 23 53 74 72 69 6E 67 73 00 00 00 00  // ....#Strings....
                 74 08 00 00 7C 01 00 00 23 42 6C 6F 62 00 00 00  // t...|...#Blob...
                 F0 09 00 00 50 00 00 00 23 55 53 00 40 0A 00 00  // ....P...#US.@...
                 10 00 00 00 23 47 55 49 44 00 00 00 00 00 00 00  // ....#GUID.......
                 01 00 00 01 57 9F 16 29 09 02 00 00 00 FA 01 33  // ....W..).......3
                 00 02 00 00 01 00 00 00 0E 00 00 00 0C 00 00 00
                 0A 00 00 00 25 00 00 00 07 00 00 00 01 00 00 00  // ....%...........
                 0C 00 00 00 02 00 00 00 01 00 00 00 04 00 00 00
                 19 00 00 00 01 00 00 00 01 00 00 00 02 00 00 00
                 02 00 00 00 04 00 00 00 01 00 00 00 01 00 00 00
                 04 00 00 00 00 00 4C 00 01 00 00 00 00 00 06 00  // ......L.........
                 65 00 5E 00 06 00 72 00 5E 00 06 00 7C 00 5E 00  // e.^...r.^...|.^.
                 06 00 9E 00 5E 00 06 00 D8 00 5E 00 06 00 E5 00  // ....^.....^.....
                 5E 00 06 00 93 01 80 01 06 00 CB 01 5E 00 06 00  // ^...........^...
                 E3 01 5E 00 06 00 EB 01 5E 00 06 00 FD 01 5E 00  // ..^.....^.....^.
                 06 00 58 02 38 02 06 00 67 02 5E 00 06 00 6D 02  // ..X.8...g.^...m.
                 5E 00 00 00 00 00 43 00 00 00 00 00 01 00 01 00  // ^.....C.........
                 08 01 10 00 6F 00 00 00 05 00 01 00 01 00 00 21  // ....o..........!
                 00 00 77 00 00 00 09 00 03 00 02 00 01 01 00 00  // ..w.............
                 8E 00 00 00 0D 00 06 00 02 00 A1 00 00 00 98 00
                 00 00 00 00 06 00 06 00 01 00 10 00 A5 00 00 00
                 11 00 06 00 07 00 01 00 10 00 AB 00 00 00 11 00
                 07 00 0C 00 00 00 00 00 03 02 00 00 11 00 07 00
                 26 00 03 01 00 00 10 02 00 00 05 00 0B 00 26 00  // &.............&.
                 03 01 00 00 90 02 00 00 05 00 0B 00 26 00 03 01  // ............&...
                 00 00 BC 02 00 00 05 00 0B 00 26 00 03 01 00 00  // ..........&.....
                 E4 02 00 00 05 00 0B 00 26 00 06 00 B0 00 1F 00  // ........&.......
                 16 00 B2 00 1F 00 06 06 BB 00 1F 00 56 80 C3 00  // ............V...
                 26 00 56 80 C7 00 26 00 01 00 0E 01 50 00 13 01  // &.V...&.....P...
                 24 02 FD 00 13 01 A4 02 09 01 13 01 D0 02 35 01  // $.............5.
                 13 01 F8 02 39 01 50 20 00 00 00 00 91 18 B4 00  // ....9.P ........
                 22 00 01 00 00 00 00 00 03 00 86 18 CB 00 34 00  // ".............4.
                 01 00 00 00 00 00 03 00 C6 00 D1 00 3A 00 03 00  // ............:...
                 00 00 00 00 03 00 C6 01 F3 00 3E 00 03 00 00 00  // ..........>.....
                 00 00 03 00 C6 01 FF 00 46 00 05 00 00 00 00 00  // ........F.......
                 00 00 C6 05 09 01 4C 00 06 00 58 20 00 00 20 00  // ......L...X .. .
                 86 08 11 01 54 00 06 00 71 20 00 00 20 00 86 08  // ....T...q .. ...
                 18 01 54 00 07 00 8A 20 00 00 00 00 86 00 22 01  // ..T.... ......".
                 3A 00 08 00 8C 20 00 00 00 00 E6 01 09 01 4C 00  // :.... ........L.
                 08 00 9E 20 00 00 00 00 86 18 CB 00 3A 00 08 00  // ... ........:...
                 A8 20 00 00 00 00 96 00 24 01 22 00 08 00 D8 20  // . ......$.".... 
                 00 00 00 00 96 00 27 01 22 00 08 00 14 21 00 00  // ......'."....!..
                 00 00 96 00 2A 01 22 00 08 00 50 21 00 00 00 00  // ....*."...P!....
                 96 00 2D 01 22 00 08 00 8C 21 00 00 00 00 96 00  // ..-."....!......
                 30 01 22 00 08 00 C8 21 00 00 00 00 96 00 33 01  // 0."....!......3.
                 22 00 08 00 FC 21 00 00 00 00 96 00 36 01 22 00  // "....!......6.".
                 08 00 30 22 00 00 00 00 96 00 39 01 22 00 08 00  // ..0"......9."...
                 74 22 00 00 00 00 96 00 3C 01 22 00 08 00 A8 22  // t"......<."...."
                 00 00 00 00 96 00 3F 01 22 00 08 00 DC 22 00 00  // ......?."...."..
                 00 00 96 00 43 01 22 00 08 00 18 23 00 00 00 00  // ....C."....#....
                 96 00 47 01 22 00 08 00 5C 23 00 00 00 00 96 00  // ..G."...\#......
                 4B 01 22 00 08 00 90 23 00 00 00 00 96 00 4F 01  // K."....#......O.
                 22 00 08 00 E0 23 00 00 00 00 96 00 53 01 22 00  // "....#......S.".
                 08 00 54 24 00 00 00 00 96 00 57 01 22 00 08 00  // ..T$......W."...
                 94 24 00 00 00 00 96 00 5B 01 22 00 08 00 D0 24  // .$......[."....$
                 00 00 00 00 96 00 5F 01 22 00 08 00 0C 25 00 00  // ......_."....%..
                 00 00 96 00 63 01 22 00 08 00 48 25 00 00 00 00  // ....c."...H%....
                 96 00 67 01 22 00 08 00 C0 25 00 00 00 00 96 00  // ..g."....%......
                 6B 01 22 00 08 00 6C 26 00 00 00 00 96 00 6F 01  // k."...l&......o.
                 22 00 08 00 CC 26 00 00 00 00 96 00 73 01 22 00  // "....&......s.".
                 08 00 14 27 00 00 00 00 96 00 77 01 22 00 08 00  // ...'......w."...
                 74 27 00 00 00 00 96 00 7B 01 5A 00 08 00 1C 28  // t'......{.Z....(
                 00 00 00 00 86 18 CB 00 3A 00 08 00 00 00 01 00  // ........:.......
                 A7 01 00 00 02 00 AE 01 00 00 01 00 B5 01 00 00
                 02 00 A7 01 00 00 01 00 BE 01 00 00 01 00 C5 01
                 00 00 01 00 C5 01 06 00 14 00 39 00 CB 00 5E 00  // ..........9...^.
                 41 00 D4 01 64 00 41 00 DC 01 64 00 21 00 CB 00  // A...d.A...d.!...
                 3A 00 49 00 CB 00 AD 00 51 00 F3 01 C8 00 0C 00  // :.I.....Q.......
                 CB 00 F7 00 61 00 80 02 01 01 0C 00 B8 02 0D 01  // ....a...........
                 14 00 CB 00 2E 01 14 00 B8 02 3D 01 51 00 F3 01  // ..........=.Q...
                 68 01 08 00 10 00 2A 00 08 00 14 00 2F 00 2E 00  // h.....*...../...
                 0B 00 72 01 01 00 10 00 00 00 09 00 01 00 10 00  // ..r.............
                 00 00 0A 00 01 00 24 00 00 00 0B 00 01 00 24 00  // ......$.......$.
                 00 00 0C 00 6D 00 71 00 76 00 7B 00 80 00 85 00  // ....m.q.v.{.....
                 8A 00 8F 00 94 00 99 00 9E 00 A3 00 A8 00 B6 00
                 BD 00 CD 00 D4 00 D9 00 E0 00 E5 00 13 01 44 01  // ..............D.
                 58 01 61 01 6D 01 06 00 01 00 00 00 0E 01 10 00  // X.a.m...........
                 08 00 07 00 02 00 10 00 08 00 02 00 EF 00 25 01  // ..............%.
                 A0 25 00 00 07 00 B0 25 00 00 08 00 20 26 00 00  // .%.....%.... &..
                 09 00 48 26 00 00 0A 00 04 80 00 00 00 00 00 00  // ..H&............
                 00 00 00 00 00 00 00 00 00 00 AB 00 00 00 01 00
                 00 00 6B 09 00 00 00 00 00 00 01 00 55 00 00 00  // ..k.........U...
                 0A 00 09 00 08 00 0A 00 08 00 0B 00 08 00 0C 00
                 08 00 00 00 00 56 65 72 73 69 6F 6E 20 6F 66 20  // .....Version of 
                 72 75 6E 74 69 6D 65 20 61 67 61 69 6E 73 74 20  // runtime against 
                 77 68 69 63 68 20 74 68 65 20 62 69 6E 61 72 79  // which the binary
                 20 69 73 20 62 75 69 6C 74 20 3A 20 31 2E 30 2E  //  is built : 1.0.
                 32 34 31 33 2E 30 00 3C 4D 6F 64 75 6C 65 3E 00  // 2413.0.<Module>.
                 74 72 79 31 2E 65 78 65 00 6D 73 63 6F 72 6C 69  // try1.exe.mscorli
                 62 00 53 79 73 74 65 6D 00 56 61 6C 75 65 54 79  // b.System.ValueTy
                 70 65 00 56 54 00 45 6E 75 6D 00 44 61 79 73 00  // pe.VT.Enum.Days.
                 4D 75 6C 74 69 63 61 73 74 44 65 6C 65 67 61 74  // MulticastDelegat
                 65 00 64 65 6C 65 67 61 74 65 31 00 49 54 65 73  // e.delegate1.ITes
                 74 00 4F 62 6A 65 63 74 00 43 54 65 73 74 00 74  // t.Object.CTest.t
                 72 79 31 00 6D 00 6E 00 2E 63 63 74 6F 72 00 76  // ry1.m.n..cctor.v
                 61 6C 75 65 5F 5F 00 53 75 6E 00 4D 6F 6E 00 2E  // alue__.Sun.Mon..
                 63 74 6F 72 00 49 6E 76 6F 6B 65 00 49 41 73 79  // ctor.Invoke.IAsy
                 6E 63 52 65 73 75 6C 74 00 41 73 79 6E 63 43 61  // ncResult.AsyncCa
                 6C 6C 62 61 63 6B 00 42 65 67 69 6E 49 6E 76 6F  // llback.BeginInvo
                 6B 65 00 45 6E 64 49 6E 76 6F 6B 65 00 74 65 73  // ke.EndInvoke.tes
                 74 00 64 67 00 61 64 64 5F 64 67 00 72 65 6D 6F  // t.dg.add_dg.remo
                 76 65 5F 64 67 00 66 00 66 31 00 66 32 00 66 33  // ve_dg.f.f1.f2.f3
                 00 66 34 00 66 35 00 66 36 00 66 37 00 66 38 00  // .f4.f5.f6.f7.f8.
                 66 39 00 66 31 30 00 66 31 31 00 66 31 32 00 66  // f9.f10.f11.f12.f
                 31 33 00 66 31 34 00 66 31 35 00 66 31 36 00 66  // 13.f14.f15.f16.f
                 31 37 00 66 31 38 00 66 31 39 00 66 32 30 00 66  // 17.f18.f19.f20.f
                 32 31 00 66 32 32 00 66 32 33 00 66 32 34 00 4D  // 21.f22.f23.f24.M
                 61 69 6E 00 53 79 73 74 65 6D 2E 44 69 61 67 6E  // ain.System.Diagn
                 6F 73 74 69 63 73 00 44 65 62 75 67 67 61 62 6C  // ostics.Debuggabl
                 65 41 74 74 72 69 62 75 74 65 00 6F 62 6A 65 63  // eAttribute.objec
                 74 00 6D 65 74 68 6F 64 00 63 61 6C 6C 62 61 63  // t.method.callbac
                 6B 00 72 65 73 75 6C 74 00 76 61 6C 75 65 00 44  // k.result.value.D
                 65 6C 65 67 61 74 65 00 43 6F 6D 62 69 6E 65 00  // elegate.Combine.
                 52 65 6D 6F 76 65 00 44 65 63 69 6D 61 6C 00 43  // Remove.Decimal.C
                 6F 6E 73 6F 6C 65 00 57 72 69 74 65 4C 69 6E 65  // onsole.WriteLine
                 00 49 6E 74 33 32 00 3C 47 6C 6F 62 61 6C 4E 61  // .Int32.<GlobalNa
                 6D 65 3E 00 24 24 73 74 72 75 63 74 30 78 36 30  // me>.$$struct0x60
                 30 30 30 32 30 2D 31 00 24 24 6D 65 74 68 6F 64  // 00020-1.$$method
                 30 78 36 30 30 30 30 32 30 2D 31 00 53 79 73 74  // 0x6000020-1.Syst
                 65 6D 2E 52 75 6E 74 69 6D 65 2E 43 6F 6D 70 69  // em.Runtime.Compi
                 6C 65 72 53 65 72 76 69 63 65 73 00 52 75 6E 74  // lerServices.Runt
                 69 6D 65 48 65 6C 70 65 72 73 00 41 72 72 61 79  // imeHelpers.Array
                 00 52 75 6E 74 69 6D 65 46 69 65 6C 64 48 61 6E  // .RuntimeFieldHan
                 64 6C 65 00 49 6E 69 74 69 61 6C 69 7A 65 41 72  // dle.InitializeAr
                 72 61 79 00 24 24 73 74 72 75 63 74 30 78 36 30  // ray.$$struct0x60
                 30 30 30 32 30 2D 32 00 24 24 6D 65 74 68 6F 64  // 00020-2.$$method
                 30 78 36 30 30 30 30 32 30 2D 32 00 47 65 74 00  // 0x6000020-2.Get.
                 24 24 73 74 72 75 63 74 30 78 36 30 30 30 30 32  // $$struct0x600002
                 31 2D 31 00 24 24 6D 65 74 68 6F 64 30 78 36 30  // 1-1.$$method0x60
                 30 30 30 32 31 2D 31 00 24 24 73 74 72 75 63 74  // 00021-1.$$struct
                 30 78 36 30 30 30 30 32 31 2D 32 00 24 24 6D 65  // 0x6000021-2.$$me
                 74 68 6F 64 30 78 36 30 30 30 30 32 31 2D 32 00  // thod0x6000021-2.
                 00 08 B7 7A 5C 56 19 34 E0 89 14 11 29 F4 49 85  // ...z\V.4....).I.
                 0C 7F 24 30 CE E2 FD A4 90 78 8D 09 50 3B 04 02  // ..$0.....x..P;..
                 06 08 03 00 00 01 03 06 11 0C 04 00 00 00 00 04
                 01 00 00 00 05 20 02 01 1C 19 03 20 00 01 07 20  // ..... ..... ... 
                 02 12 15 12 19 1C 05 20 01 01 12 15 03 20 00 08  // ....... ..... ..
                 03 06 12 10 05 20 01 01 12 10 03 00 00 08 05 20  // ..... ......... 
                 02 01 02 02 08 00 02 12 21 12 21 12 21 03 07 01  // ........!.!.!...
                 08 04 07 02 08 08 04 07 02 09 09 04 07 02 06 06
                 04 07 02 07 07 04 07 02 05 05 04 07 02 04 04 04
                 07 02 0A 0A 04 07 02 0B 0B 04 07 02 03 03 04 07
                 02 0C 0C 04 07 02 0D 0D 04 07 02 02 02 08 20 05  // .............. .
                 01 08 08 08 02 05 06 07 02 11 25 11 25 0A 07 06  // ..........%.%...
                 11 08 08 08 11 08 08 08 04 00 01 01 1C 06 07 02
                 11 0C 11 0C 04 07 02 1C 1C 06 07 02 12 1C 12 1C
                 04 07 02 0E 0E 09 07 04 1D 08 1D 08 08 1D 08 07
                 14 08 02 00 02 00 00 05 20 02 01 08 08 03 06 11  // ........ .......
                 24 07 00 02 01 12 35 11 39 03 06 11 28 05 20 02  // $.....5.9...(. .
                 08 08 08 11 07 03 14 08 02 00 02 00 00 14 08 02
                 00 02 00 00 08 08 14 08 03 00 03 00 00 00 06 20  // ............... 
                 03 01 08 08 08 03 06 11 2C 03 06 11 30 06 20 03  // ........,...0. .
                 08 08 08 08 13 07 03 14 08 03 00 03 00 00 00 14
                 08 03 00 03 00 00 00 08 08 07 04 12 18 08 12 18
                 08 06 07 02 12 18 12 18 04 00 01 01 0E 04 07 02
                 02 08 06 01 00 00 01 00 00 00 00 00 00 17 74 00  // ..............t.
                 65 00 73 00 74 00 20 00 73 00 74 00 72 00 69 00  // e.s.t. .s.t.r.i.
                 6E 00 67 00 00 17 73 00 74 00 72 00 69 00 6E 00  // n.g...s.t.r.i.n.
                 67 00 20 00 74 00 65 00 73 00 74 00 00 0D 50 00  // g. .t.e.s.t...P.
                 41 00 53 00 53 00 45 00 44 00 00 0D 46 00 41 00  // A.S.S.E.D...F.A.
                 49 00 4C 00 45 00 44 00 00 00 00 00 27 D8 F7 3C  // I.L.E.D.....'..<
                 47 A8 09 42 86 5E 26 68 20 F5 4C 22 9C 32 00 00  // G..B.^&h .L".2..
                 00 00 00 00 00 00 00 00 BE 32 00 00 00 20 00 00  // .........2... ..
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 B0 32 00 00 00 00 00 00 00 00 00 00  // .....2..........
                 00 00 00 00 00 00 00 00 00 00 5F 43 6F 72 45 78  // .........._CorEx
                 65 4D 61 69 6E 00 6D 73 63 6F 72 65 65 2E 64 6C  // eMain.mscoree.dl
                 6C 00 00 00 00 00 FF 25 00 20 40 00)             // l......%. @.
//*********** DISASSEMBLY COMPLETE ***********************
