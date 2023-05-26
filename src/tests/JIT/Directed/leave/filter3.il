// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
  .method private hidebysig specialname rtspecialname static 
          void .cctor() il managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.3
    IL_0001:  stsfld     int32 VT::n
    IL_0006:  ret
  } // end of method VT::.cctor

} // end of class VT

.class private serializable sealed Days
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal value class Days Sun = int32(0x00000000)
  .field public static literal value class Days Mon = int32(0x00000001)
} // end of class Days

.class public sealed delegate1
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(class System.Object o,
                               native unsigned int 'method') runtime managed
  {
  } // end of method delegate1::.ctor

  .method public hidebysig virtual instance void
          Invoke() runtime managed
  {
  } // end of method delegate1::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                      class System.Object o) runtime managed
  {
  } // end of method delegate1::BeginInvoke

  .method public hidebysig newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method delegate1::EndInvoke

} // end of class delegate1

.class interface public abstract ITest
{
  .method public hidebysig newslot virtual abstract 
          instance int32 test() il managed
  {
  } // end of method ITest::test

} // end of class ITest

.class public beforefieldinit CTest
       extends [mscorlib]System.Object
       implements ITest
{
  .field private class delegate1 dg
  .method public hidebysig specialname instance void 
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

  .method public hidebysig specialname instance void 
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

  .method public hidebysig instance void f() il managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method CTest::f

  .method public hidebysig newslot final virtual 
          instance int32 test() il managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method CTest::test

  .method public hidebysig specialname rtspecialname 
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

.class public beforefieldinit filter3
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  f25(int32 arg) il managed
  {
    // Code size       990 (0x3de)
    .maxstack  60
    .locals (int32 V_0,
             unsigned int32 V_1,
             int16 V_2,
             unsigned int16 V_3,
             unsigned int8 V_4,
             int8 V_5,
             int64 V_6,
             unsigned int64 V_7,
             wchar V_8,
             float32 V_9,
             float64 V_10,
             bool V_11,
             value class [mscorlib]System.Decimal V_12,
             value class VT V_13,
             int32 V_14,
             int32 V_15,
             value class Days V_16,
             class System.Object V_17,
             class filter3 V_18,
             class System.String V_19,
             int32[] V_20,
             int32[0...,0...] V_21,
             int32[0...,0...,0...] V_22,
             class CTest V_23,
             int32 V_24,
             class CTest V_25,
             int32 V_26,
             int32 V_27,
             unsigned int32 V_28,
             int16 V_29,
             unsigned int16 V_30,
             unsigned int8 V_31,
             int8 V_32,
             int64 V_33,
             unsigned int64 V_34,
             wchar V_35,
             float32 V_36,
             float64 V_37,
             bool V_38,
             value class [mscorlib]System.Decimal V_39,
             value class VT V_40,
             int32 V_41,
             int32 V_42,
             value class Days V_43,
             class System.Object V_44,
             class filter3 V_45,
             class System.String V_46,
             int32[] V_47,
             int32 V_48,
             int32[0...,0...] V_49,
             int32 V_50,
             int32[0...,0...,0...] V_51,
             int32 V_52,
             class CTest V_53,
             int32 V_54,
             class CTest V_55,
             int32 V_56,
             int32 V_57,
             int32 V_58,
             unsigned int32 V_59,
             int16 V_60,
             unsigned int16 V_61,
             unsigned int8 V_62,
             int8 V_63,
             int64 V_64,
             unsigned int64 V_65,
             wchar V_66,
             float32 V_67,
             float64 V_68,
             bool V_69,
             value class [mscorlib]System.Decimal V_70,
             value class VT V_71,
             int32 V_72,
             int32 V_73,
             value class Days V_74,
             class System.Object V_75,
             class filter3 V_76,
             class System.String V_77,
             int32[] V_78,
             int32 V_79,
             int32[0...,0...] V_80,
             int32 V_81,
             int32[0...,0...,0...] V_82,
             int32 V_83,
             class CTest V_84,
             int32 V_85,
             class CTest V_86,
             int32 V_87,
             int32 V_88,
             int32[] V_89,
             int32 V_90)
    IL_0000:  ldloca.s   V_13
    IL_0002:  initobj    VT
    IL_0008:  ldloca.s   V_40
    IL_000a:  initobj    VT
    IL_0010:  ldloca.s   V_71
    IL_0012:  initobj    VT
    IL_0018:  ldc.i4     0xffff63c0
    IL_001d:  stloc.0
    IL_001e:  ldc.i4     0x9c40
    IL_0023:  stloc.1
    IL_0024:  ldc.i4     0xfffffc18
    IL_0029:  stloc.2
    IL_002a:  ldc.i4     0x3e8
    IL_002f:  stloc.3
    IL_0030:  ldc.i4.s   20
    IL_0032:  stloc.s    V_4
    IL_0034:  ldc.i4.s   -20
    IL_0036:  stloc.s    V_5
    IL_0038:  ldc.i8     0x8000000000000000
    IL_0041:  stloc.s    V_6
    IL_0043:  ldc.i4.0
    IL_0044:  conv.i8
    IL_0045:  stloc.s    V_7
    IL_0047:  ldc.i4.s   49
    IL_0049:  stloc.s    V_8
    IL_004b:  ldc.r4     (E1 7A 24 C0)
    IL_0050:  stloc.s    V_9
    IL_0052:  ldc.r8     -1.2888888888888881
    IL_005b:  stloc.s    V_10
    IL_005d:  ldc.i4.1
    IL_005e:  stloc.s    V_11
    IL_0060:  ldc.i4     0x2715
    IL_0065:  ldc.i4.0
    IL_0066:  ldc.i4.0
    IL_0067:  ldc.i4     0x80
    IL_006c:  ldc.i4.2
    IL_006d:  newobj     instance void [mscorlib]System.Decimal::.ctor(int32,
                                                                       int32,
                                                                       int32,
                                                                       bool,
                                                                       unsigned int8)
    IL_0072:  stloc.s    V_12
    IL_0074:  ldloca.s   V_13
    IL_0076:  ldc.i4     0xfffffed4
    IL_007b:  stfld      int32 VT::m
    IL_0080:  ldloca.s   V_13
    IL_0082:  ldfld      int32 VT::m
    IL_0087:  stloc.s    V_14
    IL_0089:  ldsfld     int32 VT::n
    IL_008e:  stloc.s    V_15
    IL_0090:  ldc.i4.0
    IL_0091:  stloc.s    V_16
    IL_0093:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0098:  stloc.s    V_17
    IL_009a:  newobj     instance void filter3::.ctor()
    IL_009f:  stloc.s    V_18
    IL_00a1:  ldstr      "test string"
    IL_00a6:  stloc.s    V_19
    IL_00a8:  ldc.i4.2
    IL_00a9:  newarr     [mscorlib]System.Int32
    IL_00ae:  stloc.s    V_89
    IL_00b0:  ldloc.s    V_89
    IL_00b2:  ldc.i4.0
    IL_00b3:  ldc.i4.1
    IL_00b4:  stelem.i4
    IL_00b5:  ldloc.s    V_89
    IL_00b7:  ldc.i4.1
    IL_00b8:  ldc.i4.2
    IL_00b9:  stelem.i4
    IL_00ba:  ldloc.s    V_89
    IL_00bc:  stloc.s    V_20
    IL_00be:  ldc.i4.2
    IL_00bf:  ldc.i4.2
    IL_00c0:  newobj     instance void int32[0...,0...]::.ctor(int32,
                                                               int32)
    IL_00c5:  dup
    IL_00c6:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000c-1' '<GlobalName>'::'$$method0x600000c-1'
    IL_00cb:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        value class [mscorlib]System.RuntimeFieldHandle)
    IL_00d0:  stloc.s    V_21
    IL_00d2:  ldc.i4.3
    IL_00d3:  ldc.i4.1
    IL_00d4:  ldc.i4.3
    IL_00d5:  newobj     instance void int32[0...,0...,0...]::.ctor(int32,
                                                                    int32,
                                                                    int32)
    IL_00da:  dup
    IL_00db:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000c-2' '<GlobalName>'::'$$method0x600000c-2'
    IL_00e0:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        value class [mscorlib]System.RuntimeFieldHandle)
    IL_00e5:  stloc.s    V_22
    IL_00e7:  newobj     instance void CTest::.ctor()
    IL_00ec:  stloc.s    V_23
    IL_00ee:  ldloc.s    V_23
    IL_00f0:  callvirt   instance int32 CTest::test()
    IL_00f5:  stloc.s    V_24
    IL_00f7:  newobj     instance void CTest::.ctor()
    IL_00fc:  stloc.s    V_25
    IL_00fe:  ldloc.s    V_25
    IL_0100:  ldloc.s    V_25
    IL_0102:  ldftn      instance void CTest::f()
    IL_0108:  newobj     instance void delegate1::.ctor(class System.Object,
                                                        native unsigned int)
    IL_010d:  callvirt   instance void CTest::add_dg(class delegate1)
    IL_0112:  ldarg.0
    IL_0113:  stloc.s    V_26
    .try
    {
      IL_0115:  ldc.i4     0xffff63c0
      //IL_011a:  stloc.s    V_27
      IL_011c:  ldc.i4     0x9c40
      //IL_0121:  stloc.s    V_28
      IL_0123:  ldc.i4     0xfffffc18
      //IL_0128:  stloc.s    V_29
      IL_012a:  ldc.i4     0x3e8
      //IL_012f:  stloc.s    V_30
      IL_0131:  ldc.i4.s   20
      //IL_0133:  stloc.s    V_31
      IL_0135:  ldc.i4.s   -20
      //IL_0137:  stloc.s    V_32
      IL_0139:  ldc.i8     0x7fffffffffffffff
      //IL_0142:  stloc.s    V_33
      IL_0144:  ldc.i4.m1
      IL_0145:  conv.i8
      //IL_0146:  stloc.s    V_34
      IL_0148:  ldc.i4.s   65
      //IL_014a:  stloc.s    V_35
      IL_014c:  ldc.r4     (E1 7A 24 40)
      //IL_0151:  stloc.s    V_36
      IL_0153:  ldc.r8     1.2888888888888881
      //IL_015c:  stloc.s    V_37
      IL_015e:  ldc.i4.0
      //IL_015f:  stloc.s    V_38
      IL_0161:  ldc.i4     0x676
      IL_0166:  ldc.i4.0
      IL_0167:  ldc.i4.0
      IL_0168:  ldc.i4.0
      IL_0169:  ldc.i4.3
      IL_016a:  newobj     instance void [mscorlib]System.Decimal::.ctor(int32,
                                                                         int32,
                                                                         int32,
                                                                         bool,
                                                                         unsigned int8)
      //IL_016f:  stloc.s    V_39
      IL_0171:  ldloca.s   V_40
      IL_0173:  ldc.i4     0x12c
      IL_0178:  stfld      int32 VT::m
      IL_017d:  ldloca.s   V_40
      IL_017f:  ldfld      int32 VT::m
      //IL_0184:  stloc.s    V_41
      IL_0186:  ldsfld     int32 VT::n
      //IL_018b:  stloc.s    V_42
      IL_018d:  ldc.i4.1
      IL_018e:  stloc.s    V_43
      IL_0190:  ldloc.s    V_43
      IL_0192:  box        Days
      //IL_0197:  call       void [System.Console]System.Console::WriteLine(class System.Object)
      IL_019c:  newobj     instance void [mscorlib]System.Object::.ctor()
      //IL_01a1:  stloc.s    V_44
      IL_01a3:  newobj     instance void filter3::.ctor()
      //IL_01a8:  stloc.s    V_45
      IL_01aa:  ldstr      "string test"
      //IL_01af:  stloc.s    V_46
      IL_01b1:  ldc.i4.2
      IL_01b2:  newarr     [mscorlib]System.Int32
      IL_01b7:  stloc.s    V_89
      IL_01b9:  ldloc.s    V_89
      IL_01bb:  ldc.i4.0
      IL_01bc:  ldc.i4.3
      IL_01bd:  stelem.i4
      IL_01be:  ldloc.s    V_89
      IL_01c0:  ldc.i4.1
      IL_01c1:  ldc.i4.4
      IL_01c2:  stelem.i4
      IL_01c3:  ldloc.s    V_89
      IL_01c5:  stloc.s    V_47
      IL_01c7:  ldloc.s    V_47
      IL_01c9:  ldc.i4.0
      IL_01ca:  ldelem.i4
      //IL_01cb:  stloc.s    V_48
      IL_01cd:  ldc.i4.2
      IL_01ce:  ldc.i4.2
      IL_01cf:  newobj     instance void int32[0...,0...]::.ctor(int32,
                                                                 int32)
      IL_01d4:  dup
      IL_01d5:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000c-3' '<GlobalName>'::'$$method0x600000c-3'
      //IL_01da:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
      //                                                                                                    value class [mscorlib]System.RuntimeFieldHandle)
      //IL_01df:  stloc.s    V_49
      //IL_01e1:  ldloc.s    V_49
      //IL_01e3:  ldc.i4.0
      //IL_01e4:  ldc.i4.1
      //IL_01e5:  call       instance int32 int32[0...,0...]::Get(int32,
      //                                                          int32)
      //IL_01ea:  stloc.s    V_50
      IL_01ec:  ldc.i4.3
      IL_01ed:  ldc.i4.1
      IL_01ee:  ldc.i4.3
      IL_01ef:  newobj     instance void int32[0...,0...,0...]::.ctor(int32,
                                                                      int32,
                                                                      int32)
      IL_01f4:  dup
      IL_01f5:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000c-4' '<GlobalName>'::'$$method0x600000c-4'
      IL_01fa:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                          value class [mscorlib]System.RuntimeFieldHandle)
      //IL_01ff:  stloc.s    V_51
      IL_0201:  ldloc.s    V_51
      IL_0203:  ldc.i4.0
      IL_0204:  ldc.i4.0
      IL_0205:  ldc.i4.1
      //IL_0206:  call       instance int32 int32[0...,0...,0...]::Get(int32,
      //                                                               int32,
      //                                                               int32)
      //IL_020b:  stloc.s    V_52
      IL_020d:  newobj     instance void CTest::.ctor()
      IL_0212:  stloc.s    V_53
      IL_0214:  ldloc.s    V_53
      IL_0216:  callvirt   instance int32 CTest::test()
      //IL_021b:  stloc.s    V_54
      IL_021d:  newobj     instance void CTest::.ctor()
      IL_0222:  stloc.s    V_55
      IL_0224:  ldloc.s    V_55
      IL_0226:  ldloc.s    V_55
      IL_0228:  ldftn      instance void CTest::f()
      IL_022e:  newobj     instance void delegate1::.ctor(class System.Object,
                                                          native unsigned int)
      IL_0233:  callvirt   instance void CTest::add_dg(class delegate1)
      IL_0238:  ldarg.0
      //IL_0239:  stloc.s    V_56
      IL_023b:  ldc.i4.1
      IL_023c:  stloc.s    V_57
      IL_023e:  ldloc.s    V_57
      IL_0240:  ldc.i4.2
      IL_0241:  mul
      IL_0242:  ldc.i4.2
      IL_0243:  bne.un.s   IL_026f

      IL_0245:  ldloc.s    V_57
      IL_0247:  stloc.s    V_90
      IL_0249:  ldloc.s    V_90
      IL_024b:  switch     ( 
                            IL_025e,
                            IL_0260,
                            IL_0268)
      IL_025c:  br.s       IL_026d

      IL_025e:  br.s       IL_026d

      IL_0260:  ldloc.s    V_57
      IL_0262:  ldc.i4.2
      IL_0263:  add
      IL_0264:  stloc.s    V_57
      IL_0266:  br.s       IL_026d

      IL_0268:  ldc.i4.3
      IL_0269:  stloc.s    V_57
      IL_026b:  br.s       IL_025e

      IL_026d:  br.s       IL_0275

      IL_026f:  ldloc.s    V_57
      IL_0271:  ldc.i4.5
      IL_0272:  sub
      IL_0273:  stloc.s    V_57
      IL_0275:  leave      IL_03dd

    }  // end .try
    filter
    {
    	pop
    	ldc.i4.1
    	endfilter
    }
    {
      ldc.i4     0xffff63c0
      stloc.s    V_58
      ldc.i4     0x9c40
      ldc.i4     0xfffffc18
      ldc.i4     0x3e8
      ldc.i4.s   20
      ldc.i4.s   -20
      ldc.i8     0x7fffffffffffffff
      ldc.i4.m1
      conv.i8
      ldc.i4.s   65
      ldc.r4     (E1 7A 24 40)
      ldc.r8     1.2888888888888881
      ldc.i4.0
      ldc.i4     0x676
      ldc.i4.0
      ldc.i4.0
      ldc.i4.0
      ldc.i4.3
      newobj     instance void [mscorlib]System.Decimal::.ctor(int32,
                                                                         int32,
                                                                         int32,
                                                                         bool,
                                                                         unsigned int8)
      ldloca.s   V_71
      ldc.i4     0x12c
      stfld      int32 VT::m
      ldloca.s   V_71
      ldfld      int32 VT::m
      ldsfld     int32 VT::n
      ldc.i4.1
      ldloc.s    V_74
      box        Days
      newobj     instance void filter3::.ctor()
      ldstr      "string test"
      ldc.i4.2
      newarr     [mscorlib]System.Int32
      stloc.s    V_89
      ldloc.s    V_89
      ldc.i4.0
      ldc.i4.3
      stelem.i4
      ldloc.s    V_89
      ldc.i4.1
      ldc.i4.4
      stelem.i4
      ldloc.s    V_89
      stloc.s    V_78
      ldloc.s    V_78
      ldc.i4.0
      ldelem.i4
      ldc.i4.2
      ldc.i4.2
      newobj     instance void int32[0...,0...]::.ctor(int32,
                                                                 int32)
      dup
      ldc.i4.3
      ldc.i4.1
      ldc.i4.3
      newobj     instance void int32[0...,0...,0...]::.ctor(int32,
                                                                      int32,
                                                                      int32)
      dup
      ldtoken    field value class '<GlobalName>'/'$$struct0x600000c-6' '<GlobalName>'::'$$method0x600000c-6'
      call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                          value class [mscorlib]System.RuntimeFieldHandle)
      stloc.s    V_82
      ldloc.s    V_82
      ldc.i4.0
      ldc.i4.0
      ldc.i4.1
      call       instance int32 int32[0...,0...,0...]::Get(int32,
                                                                     int32,
                                                                     int32)
      newobj     instance void CTest::.ctor()
      stloc.s    V_84
      ldloc.s    V_84
      callvirt   instance int32 CTest::test()
      newobj     instance void CTest::.ctor()
      stloc.s    V_86
      ldloc.s    V_86
      ldloc.s    V_86
      ldftn      instance void CTest::f()
      ldarg.0
      ldc.i4.1
      ldloc.s    V_88
      ldc.i4.2
      mul
      ldc.i4.2
      bne.un.s   BL_03d5

      ldloc.s    V_88
      stloc.s    V_90
      ldloc.s    V_90
      switch     ( 
                            BL_03c4,
                            BL_03c6,
                            BL_03ce)
      br.s       BL_03d3

      BL_03c4:  br.s       BL_03d3

      BL_03c6:  ldloc.s    V_88
      ldc.i4.2
      add
      stloc.s    V_88
      br.s       BL_03d3

      BL_03ce:  ldc.i4.3
      stloc.s    V_88
      br.s       BL_03c4

      BL_03d3:  br.s       BL_03db

      BL_03d5:  ldloc.s    V_88
      ldc.i4.5
      sub
      BL_03d9:  stloc.s    V_88
      BL_03db:  leave    IL_03dd
    }
    catch [mscorlib]System.Object 
    {
      //IL_027a:  pop
      IL_027b:  ldc.i4     0xffff63c0
      //IL_0280:  stloc.s    V_58
      IL_0282:  ldc.i4     0x9c40
      //IL_0287:  stloc.s    V_59
      IL_0289:  ldc.i4     0xfffffc18
      //IL_028e:  stloc.s    V_60
      IL_0290:  ldc.i4     0x3e8
      //IL_0295:  stloc.s    V_61
      IL_0297:  ldc.i4.s   20
      //IL_0299:  stloc.s    V_62
      IL_029b:  ldc.i4.s   -20
      //IL_029d:  stloc.s    V_63
      IL_029f:  ldc.i8     0x7fffffffffffffff
      //IL_02a8:  stloc.s    V_64
      IL_02aa:  ldc.i4.m1
      IL_02ab:  conv.i8
      //IL_02ac:  stloc.s    V_65
      IL_02ae:  ldc.i4.s   65
      //IL_02b0:  stloc.s    V_66
      IL_02b2:  ldc.r4     (E1 7A 24 40)
      //IL_02b7:  stloc.s    V_67
      IL_02b9:  ldc.r8     1.2888888888888881
      //IL_02c2:  stloc.s    V_68
      IL_02c4:  ldc.i4.0
     // IL_02c5:  stloc.s    V_69
      IL_02c7:  ldc.i4     0x676
      IL_02cc:  ldc.i4.0
      IL_02cd:  ldc.i4.0
      IL_02ce:  ldc.i4.0
      IL_02cf:  ldc.i4.3
      IL_02d0:  newobj     instance void [mscorlib]System.Decimal::.ctor(int32,
                                                                         int32,
                                                                         int32,
                                                                         bool,
                                                                         unsigned int8)
      //IL_02d5:  stloc.s    V_70
      IL_02d7:  ldloca.s   V_71
      IL_02d9:  ldc.i4     0x12c
      IL_02de:  stfld      int32 VT::m
      IL_02e3:  ldloca.s   V_71
      IL_02e5:  ldfld      int32 VT::m
      //IL_02ea:  stloc.s    V_72
      IL_02ec:  ldsfld     int32 VT::n
      //IL_02f1:  stloc.s    V_73
      IL_02f3:  ldc.i4.1
      //IL_02f4:  stloc.s    V_74
      IL_02f6:  ldloc.s    V_74
      IL_02f8:  box        Days
      //IL_02fd:  call       void [System.Console]System.Console::WriteLine(class System.Object)
      //IL_0302:  newobj     instance void [mscorlib]System.Object::.ctor()
      //IL_0307:  stloc.s    V_75
      IL_0309:  newobj     instance void filter3::.ctor()
      //IL_030e:  stloc.s    V_76
      IL_0310:  ldstr      "string test"
      //IL_0315:  stloc.s    V_77
      IL_0317:  ldc.i4.2
      IL_0318:  newarr     [mscorlib]System.Int32
      IL_031d:  stloc.s    V_89
      IL_031f:  ldloc.s    V_89
      IL_0321:  ldc.i4.0
      IL_0322:  ldc.i4.3
      IL_0323:  stelem.i4
      IL_0324:  ldloc.s    V_89
      IL_0326:  ldc.i4.1
      IL_0327:  ldc.i4.4
      IL_0328:  stelem.i4
      IL_0329:  ldloc.s    V_89
      IL_032b:  stloc.s    V_78
      IL_032d:  ldloc.s    V_78
      IL_032f:  ldc.i4.0
      IL_0330:  ldelem.i4
      //IL_0331:  stloc.s    V_79
      IL_0333:  ldc.i4.2
      IL_0334:  ldc.i4.2
      IL_0335:  newobj     instance void int32[0...,0...]::.ctor(int32,
                                                                 int32)
      IL_033a:  dup
      //IL_033b:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000c-5' '<GlobalName>'::'$$method0x600000c-5'
      //IL_0340:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
      //                                                                                                    value class [mscorlib]System.RuntimeFieldHandle)
      //IL_0345:  stloc.s    V_80
      //IL_0347:  ldloc.s    V_80
      //IL_0349:  ldc.i4.0
      //IL_034a:  ldc.i4.1
      //IL_034b:  call       instance int32 int32[0...,0...]::Get(int32,
      //                                                          int32)
      //IL_0350:  stloc.s    V_81
      IL_0352:  ldc.i4.3
      IL_0353:  ldc.i4.1
      IL_0354:  ldc.i4.3
      IL_0355:  newobj     instance void int32[0...,0...,0...]::.ctor(int32,
                                                                      int32,
                                                                      int32)
      IL_035a:  dup
      IL_035b:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000c-6' '<GlobalName>'::'$$method0x600000c-6'
      IL_0360:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                          value class [mscorlib]System.RuntimeFieldHandle)
      IL_0365:  stloc.s    V_82
      IL_0367:  ldloc.s    V_82
      IL_0369:  ldc.i4.0
      IL_036a:  ldc.i4.0
      IL_036b:  ldc.i4.1
      IL_036c:  call       instance int32 int32[0...,0...,0...]::Get(int32,
                                                                     int32,
                                                                     int32)
      //IL_0371:  stloc.s    V_83
      IL_0373:  newobj     instance void CTest::.ctor()
      IL_0378:  stloc.s    V_84
      IL_037a:  ldloc.s    V_84
      IL_037c:  callvirt   instance int32 CTest::test()
      //IL_0381:  stloc.s    V_85
      IL_0383:  newobj     instance void CTest::.ctor()
      IL_0388:  stloc.s    V_86
      IL_038a:  ldloc.s    V_86
      IL_038c:  ldloc.s    V_86
      IL_038e:  ldftn      instance void CTest::f()
      //IL_0394:  newobj     instance void delegate1::.ctor(class System.Object,
      //                                                    native unsigned int)
      //IL_0399:  callvirt   instance void CTest::add_dg(class delegate1)
      IL_039e:  ldarg.0
      //IL_039f:  stloc.s    V_87
      IL_03a1:  ldc.i4.1
      IL_03a2:  stloc.s    V_88
      IL_03a4:  ldloc.s    V_88
      IL_03a6:  ldc.i4.2
      IL_03a7:  mul
      IL_03a8:  ldc.i4.2
      IL_03a9:  bne.un.s   IL_03d5

      IL_03ab:  ldloc.s    V_88
      IL_03ad:  stloc.s    V_90
      IL_03af:  ldloc.s    V_90
      IL_03b1:  switch     ( 
                            IL_03c4,
                            IL_03c6,
                            IL_03ce)
      IL_03c2:  br.s       IL_03d3

      IL_03c4:  br.s       IL_03d3

      IL_03c6:  ldloc.s    V_88
      IL_03c8:  ldc.i4.2
      IL_03c9:  add
      IL_03ca:  stloc.s    V_88
      IL_03cc:  br.s       IL_03d3

      IL_03ce:  ldc.i4.3
      IL_03cf:  stloc.s    V_88
      IL_03d1:  br.s       IL_03c4

      IL_03d3:  br.s       IL_03db

      IL_03d5:  ldloc.s    V_88
      IL_03d7:  ldc.i4.5
      IL_03d8:  sub
      IL_03d9:  stloc.s    V_88
      IL_03db:  leave.s    IL_03dd
    }  // end handler
    IL_03dd:  ret
  } // end of method filter3::f25

  .method public hidebysig static void  f26(unsigned int16 arg1,
                                            int32 arg2,
                                            class System.String arg3,
                                            class System.Object arg4) il managed
  {
    // Code size       163 (0xa3)
    .maxstack  60
    .locals (unsigned int16 V_0,
             int32 V_1,
             class System.String V_2,
             class System.Object V_3,
             int16 V_4,
             unsigned int8 V_5,
             class System.String V_6,
             int32 V_7,
             class System.String[] V_8,
             unsigned int16 V_9,
             unsigned int16 V_10,
             int32 V_11,
             class System.String V_12,
             class System.Object V_13,
             int16 V_14,
             unsigned int8 V_15,
             class System.String V_16,
             int32 V_17,
             class System.String[] V_18,
             int32 V_19,
             unsigned int16* V_20,
             unsigned int16 V_21,
             class System.String[] V_22,
             unsigned int16 BUF)
    IL_0000:  ldarg.0
    IL_0001:  stloc.0
    IL_0002:  ldarg.1
    IL_0003:  stloc.1
    IL_0004:  ldarg.2
    IL_0005:  stloc.2
    IL_0006:  ldarg.3
    IL_0007:  stloc.3
    IL_0008:  ldc.i4     0xfffffc18
    IL_000d:  stloc.s    V_4
    IL_000f:  ldc.i4.s   20
    IL_0011:  stloc.s    V_5
    IL_0013:  ldstr      "f28"
    IL_0018:  stloc.s    V_6
    IL_001a:  ldc.i4     0xffff15a0
    IL_001f:  stloc.s    V_7
    IL_0021:  ldc.i4.2
    IL_0022:  newarr     [mscorlib]System.String
    IL_0027:  stloc.s    V_22
    IL_0029:  ldloc.s    V_22
    IL_002b:  ldc.i4.0
    IL_002c:  ldstr      "a"
    IL_0031:  stelem.ref
    IL_0032:  ldloc.s    V_22
    IL_0034:  ldc.i4.1
    IL_0035:  ldstr      "b"
    IL_003a:  stelem.ref
    IL_003b:  ldloc.s    V_22
    IL_003d:  stloc.s    V_8
    IL_003f:  ldc.i4     0x3e8
    IL_0044:  stloc.s    V_9

    start_try1:
      ldc.i4.2
      ldc.i4.1
      mul
      localloc
      stloc.s    V_20
      ldloc.s    V_20
      ldloc.s    V_9
      stind.i2
      ldloc.s    V_20
      ldind.u2
      
      ldarg.0
      ldarg.1
      ldarg.2
      ldarg.3
      ldloc.s    V_4
      ldc.i4.s   20
      ldstr      "f28"
      ldc.i4     0xffff15a0
      ldc.i4.2
      newarr     [mscorlib]System.String
      stloc.s    V_22
      ldloc.s    V_22
      ldc.i4.0
      ldstr      "a"
      stelem.ref
      ldloc.s    V_22
      ldc.i4.1
      ldstr      "b"
      stelem.ref
      ldloc.s    V_22
      stloc.s    V_18
      ldloc.s    V_18
      ldlen
      conv.i4
      
      //stloc.s    V_21
      IL_009d:  leave    IL_00a2
    end_try1: //

    exception_filter1:
    	pop
    	ldc.i4.1
    	endfilter
    start_filter1:
      pop
      ldc.i4.2
      ldc.i4.1
      mul
      pop ldloca.s BUF //localloc
      stloc.s    V_20
      ldloc.s    V_20
      ldloc.s    V_9
      stind.i2
      ldloc.s    V_20
      ldind.u2
      
      ldarg.0
      ldarg.1
      ldarg.2
      ldarg.3
      ldloc.s    V_4
      ldc.i4.s   20
      ldstr      "f28"
      ldc.i4     0xffff15a0
      ldc.i4.2
      newarr     [mscorlib]System.String
      stloc.s    V_22
      ldloc.s    V_22
      ldc.i4.0
      ldstr      "a"
      stelem.ref
      ldloc.s    V_22
      ldc.i4.1
      ldstr      "b"
      stelem.ref
      ldloc.s    V_22
      stloc.s    V_18
      ldloc.s    V_18
      ldlen
      conv.i4
      
      //stloc.s    V_21
      leave    IL_00a2
    end_filter1: //
    
    start_handler1:
      pop
      ldc.i4.2
      ldc.i4.1
      mul
      pop ldloca.s BUF //localloc
      stloc.s    V_20
      ldloc.s    V_20
      ldloc.s    V_9
      stind.i2
      ldloc.s    V_20
      ldind.u2
      
      ldarg.0
      ldarg.1
      ldarg.2
      ldarg.3
      ldloc.s    V_4
      ldc.i4.s   20
      ldstr      "f28"
      ldc.i4     0xffff15a0
      ldc.i4.2
      newarr     [mscorlib]System.String
      stloc.s    V_22
      ldloc.s    V_22
      ldc.i4.0
      ldstr      "a"
      stelem.ref
      ldloc.s    V_22
      ldc.i4.1
      ldstr      "b"
      stelem.ref
      ldloc.s    V_22
      stloc.s    V_18
      ldloc.s    V_18
      ldlen
      conv.i4
      
      //stloc.s    V_21
      IL_00a0:  leave    IL_00a2
    end_handler1: //
    IL_00a2:  ret

    .try start_try1 to end_try1 catch [mscorlib]System.Object handler start_handler1 to end_handler1
    filter exception_filter1 handler start_filter1 to end_filter1
    
  } // end of method filter3::f26

  .method public hidebysig static int32 Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       76 (0x4c)
    .maxstack  4
    .locals (bool V_0,
             int32 V_1)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0

    start_try1:
      IL_0002:  ldc.i4     0x9c40
      IL_0007:  call       void filter3::f25(int32)
      IL_000c:  ldc.i4     0xff
      IL_0011:  ldc.i4     0x80000000
      IL_0016:  ldstr      "f26"
      IL_001b:  newobj     instance void [mscorlib]System.Object::.ctor()
      IL_0020:  call       void filter3::f26(unsigned int16,
                                            int32,
                                            class System.String,
                                            class System.Object)
      IL_0025:  leave.s    IL_002a
    end_try1:  //

    exception_filter:
    	pop
    	ldc.i4.1
    	endfilter
    start_filter:
      leave.s    IL_002a
    end_filter: //
    
    start_handler1:
      IL_0027:  pop
      IL_0028:  leave.s    IL_002a
    end_handler1:

    .try start_try1 to end_try1
    filter exception_filter handler start_filter to end_filter
    catch [mscorlib]System.Object handler start_handler1 to end_handler1
        
    IL_002a:  ldloc.0
    IL_002b:  brfalse.s  IL_003c

    IL_002d:  ldstr      "PASSED"
    IL_0032:  call       void [System.Console]System.Console::WriteLine(class System.String)
    IL_0037:  ldc.i4.s   100
    IL_0039:  stloc.1
    IL_003a:  br.s       IL_004a

    IL_003c:  ldstr      "FAILED"
    IL_0041:  call       void [System.Console]System.Console::WriteLine(class System.String)
    IL_0046:  ldc.i4.1
    IL_0047:  stloc.1
    IL_0048:  br.s       IL_004a

    IL_004a:  ldloc.1
    IL_004b:  ret
  } // end of method filter3::Main

  .method public hidebysig specialname rtspecialname 
        instance void .ctor()
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method filter3::.ctor

} // end of class filter3

.class private '<GlobalName>'
       extends [mscorlib]System.Object
{
  .class explicit sealed nested private '$$struct0x600000c-1'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 16
  } // end of class '$$struct0x600000c-1'

  .class explicit sealed nested private '$$struct0x600000c-2'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 36
  } // end of class '$$struct0x600000c-2'

  .class explicit sealed nested private '$$struct0x600000c-3'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 16
  } // end of class '$$struct0x600000c-3'

  .class explicit sealed nested private '$$struct0x600000c-4'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 36
  } // end of class '$$struct0x600000c-4'

  .class explicit sealed nested private '$$struct0x600000c-5'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 16
  } // end of class '$$struct0x600000c-5'

  .class explicit sealed nested private '$$struct0x600000c-6'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 36
  } // end of class '$$struct0x600000c-6'

  .field static assembly value class '<GlobalName>'/'$$struct0x600000c-1' '$$method0x600000c-1' at D_000020A8
  .field static assembly value class '<GlobalName>'/'$$struct0x600000c-2' '$$method0x600000c-2' at D_000020B8
  .field static assembly value class '<GlobalName>'/'$$struct0x600000c-3' '$$method0x600000c-3' at D_000020E0
  .field static assembly value class '<GlobalName>'/'$$struct0x600000c-4' '$$method0x600000c-4' at D_000020F0
  .field static assembly value class '<GlobalName>'/'$$struct0x600000c-5' '$$method0x600000c-5' at D_00002118
  .field static assembly value class '<GlobalName>'/'$$struct0x600000c-6' '$$method0x600000c-6' at D_00002128
} // end of class '<GlobalName>'

.data D_000020A8 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00) 
.data D_000020B8 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00
                 04 00 00 00 05 00 00 00 06 00 00 00 07 00 00 00
                 08 00 00 00 00 00 00 00) 
.data D_000020E0 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00) 
.data D_000020F0 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00
                 04 00 00 00 05 00 00 00 06 00 00 00 07 00 00 00
                 08 00 00 00 00 00 00 00) 
.data D_00002118 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00) 
.data D_00002128 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00
                 04 00 00 00 05 00 00 00 06 00 00 00 07 00 00 00
                 08 00 00 00 0B 30 06 00 DE 03 00 00 02 00 00 11  // .....0..........
                 12 0D FE 15 02 00 00 02 12 28 FE 15 02 00 00 02  // .........(......
                 12 47 FE 15 02 00 00 02 20 C0 63 FF FF 0A 20 40  // .G...... .c... @
                 9C 00 00 0B 20 18 FC FF FF 0C 20 E8 03 00 00 0D  // .... ..... .....
                 1F 14 13 04 1F EC 13 05 21 00 00 00 00 00 00 00  // ........!.......
                 80 13 06 16 6A 13 07 1F 31 13 08 22 E1 7A 24 C0  // ....j...1..".z$.
                 13 09 23 F1 49 9F F4 49 9F F4 BF 13 0A 17 13 0B  // ..#.I..I........
                 20 15 27 00 00 16 16 20 80 00 00 00 18 73 06 00  //  .'.... .....s..
                 00 0A 13 0C 12 0D 20 D4 FE FF FF 7D 01 00 00 04  // ...... ....}....
                 12 0D 7B 01 00 00 04 13 0E 7E 02 00 00 04 13 0F  // ..{......~......
                 16 13 10 73 05 00 00 0A 13 11 73 0F 00 00 06 13  // ...s......s.....
                 12 72 01 00 00 70 13 13 18 8D 0C 00 00 01 13 59  // .r...p.........Y
                 11 59 16 17 9E 11 59 17 18 9E 11 59 13 14 18 18  // .Y....Y....Y....
                 73 07 00 00 0A 25 D0 07 00 00 04 28 08 00 00 0A  // s....%.....(....
                 13 15 19 17 19 73 09 00 00 0A 25 D0 08 00 00 04  // .....s....%.....
                 28 08 00 00 0A 13 16 73 0B 00 00 06 13 17 11 17  // (......s........
                 6F 0A 00 00 06 13 18 73 0B 00 00 06 13 19 11 19  // o......s........
                 11 19 FE 06 09 00 00 06 73 02 00 00 06 6F 07 00  // ........s....o..
                 00 06 02 13 1A 20 C0 63 FF FF 13 1B 20 40 9C 00  // ..... .c.... @..
                 00 13 1C 20 18 FC FF FF 13 1D 20 E8 03 00 00 13  // ... ...... .....
                 1E 1F 14 13 1F 1F EC 13 20 21 FF FF FF FF FF FF  // ........ !......
                 FF 7F 13 21 15 6A 13 22 1F 41 13 23 22 E1 7A 24  // ...!.j.".A.#".z$
                 40 13 24 23 F1 49 9F F4 49 9F F4 3F 13 25 16 13  // @.$#.I..I..?.%..
                 26 20 76 06 00 00 16 16 16 19 73 06 00 00 0A 13  // & v.......s.....
                 27 12 28 20 2C 01 00 00 7D 01 00 00 04 12 28 7B  // '.( ,...}.....({
                 01 00 00 04 13 29 7E 02 00 00 04 13 2A 17 13 2B  // .....)~.....*..+
                 11 2B 8C 03 00 00 02 28 0A 00 00 0A 73 05 00 00  // .+.....(....s...
                 0A 13 2C 73 0F 00 00 06 13 2D 72 19 00 00 70 13  // ..,s.....-r...p.
                 2E 18 8D 0C 00 00 01 13 59 11 59 16 19 9E 11 59  // ........Y.Y....Y
                 17 1A 9E 11 59 13 2F 11 2F 16 94 13 30 18 18 73  // ....Y././...0..s
                 07 00 00 0A 25 D0 09 00 00 04 28 08 00 00 0A 13  // ....%.....(.....
                 31 11 31 16 17 28 0B 00 00 0A 13 32 19 17 19 73  // 1.1..(.....2...s
                 09 00 00 0A 25 D0 0A 00 00 04 28 08 00 00 0A 13  // ....%.....(.....
                 33 11 33 16 16 17 28 0C 00 00 0A 13 34 73 0B 00  // 3.3...(.....4s..
                 00 06 13 35 11 35 6F 0A 00 00 06 13 36 73 0B 00  // ...5.5o.....6s..
                 00 06 13 37 11 37 11 37 FE 06 09 00 00 06 73 02  // ...7.7.7......s.
                 00 00 06 6F 07 00 00 06 02 13 38 17 13 39 11 39  // ...o......8..9.9
                 18 5A 18 33 2A 11 39 13 5A 11 5A 45 03 00 00 00  // .Z.3*.9.Z.ZE....
                 02 00 00 00 04 00 00 00 0C 00 00 00 2B 0F 2B 0D  // ............+.+.
                 11 39 18 58 13 39 2B 05 19 13 39 2B F1 2B 06 11  // .9.X.9+...9+.+..
                 39 1B 59 13 39 DD 63 01 00 00 26 20 C0 63 FF FF  // 9.Y.9.c...& .c..
                 13 3A 20 40 9C 00 00 13 3B 20 18 FC FF FF 13 3C  // .: @....; .....<
                 20 E8 03 00 00 13 3D 1F 14 13 3E 1F EC 13 3F 21  //  .....=...>...?!
                 FF FF FF FF FF FF FF 7F 13 40 15 6A 13 41 1F 41  // .........@.j.A.A
                 13 42 22 E1 7A 24 40 13 43 23 F1 49 9F F4 49 9F  // .B".z$@.C#.I..I.
                 F4 3F 13 44 16 13 45 20 76 06 00 00 16 16 16 19  // .?.D..E v.......
                 73 06 00 00 0A 13 46 12 47 20 2C 01 00 00 7D 01  // s.....F.G ,...}.
                 00 00 04 12 47 7B 01 00 00 04 13 48 7E 02 00 00  // ....G{.....H~...
                 04 13 49 17 13 4A 11 4A 8C 03 00 00 02 28 0A 00  // ..I..J.J.....(..
                 00 0A 73 05 00 00 0A 13 4B 73 0F 00 00 06 13 4C  // ..s.....Ks.....L
                 72 19 00 00 70 13 4D 18 8D 0C 00 00 01 13 59 11  // r...p.M.......Y.
                 59 16 19 9E 11 59 17 1A 9E 11 59 13 4E 11 4E 16  // Y....Y....Y.N.N.
                 94 13 4F 18 18 73 07 00 00 0A 25 D0 0B 00 00 04  // ..O..s....%.....
                 28 08 00 00 0A 13 50 11 50 16 17 28 0B 00 00 0A  // (.....P.P..(....
                 13 51 19 17 19 73 09 00 00 0A 25 D0 0C 00 00 04  // .Q...s....%.....
                 28 08 00 00 0A 13 52 11 52 16 16 17 28 0C 00 00  // (.....R.R...(...
                 0A 13 53 73 0B 00 00 06 13 54 11 54 6F 0A 00 00  // ..Ss.....T.To...
                 06 13 55 73 0B 00 00 06 13 56 11 56 11 56 FE 06  // ..Us.....V.V.V..
                 09 00 00 06 73 02 00 00 06 6F 07 00 00 06 02 13  // ....s....o......
                 57 17 13 58 11 58 18 5A 18 33 2A 11 58 13 5A 11  // W..X.X.Z.3*.X.Z.
                 5A 45 03 00 00 00 02 00 00 00 04 00 00 00 0C 00  // ZE..............
                 00 00 2B 0F 2B 0D 11 58 18 58 13 58 2B 05 19 13  // ..+.+..X.X.X+...
                 58 2B F1 2B 06 11 58 1B 59 13 58 DE 00 2A 00 00  // X+.+..X.Y.X..*..
                 41 1C 00 00 00 00 00 00 15 01 00 00 65 01 00 00  // A...........e...
                 7A 02 00 00 63 01 00 00 04 00 00 01 0B 30 03 00  // z...c........0..
                 A3 00 00 00 03 00 00 11 02 0A 03 0B 04 0C 05 0D
                 20 18 FC FF FF 13 04 1F 14 13 05 72 31 00 00 70  //  ..........r1..p
                 13 06 20 A0 15 FF FF 13 07 18 8D 11 00 00 01 13  // .. .............
                 16 11 16 16 72 39 00 00 70 A2 11 16 17 72 3D 00  // ....r9..p....r=.
                 00 70 A2 11 16 13 08 20 E8 03 00 00 13 09 02 13  // .p..... ........
                 0A 03 13 0B 04 13 0C 05 13 0D 11 04 13 0E 1F 14
                 13 0F 72 31 00 00 70 13 10 20 A0 15 FF FF 13 11  // ..r1..p.. ......
                 18 8D 11 00 00 01 13 16 11 16 16 72 39 00 00 70  // ...........r9..p
                 A2 11 16 17 72 3D 00 00 70 A2 11 16 13 12 11 12  // ....r=..p.......
                 8E 69 13 13 18 17 5A FE 0F 13 14 11 14 11 09 53  // .i....Z........S
                 11 14 49 13 15 DE 03 26 DE 00 2A 00 01 10 00 00  // ..I....&..*.....
                 00 00 46 00 59 9F 00 03 04 00 00 01 00 00 00 00  // ..F.Y...........
                 00 00 00 00 00 00 00 00 0B 30 04 00 4C 00 00 00  // .........0..L...
                 04 00 00 11 17 0A 20 40 9C 00 00 28 0C 00 00 06  // ...... @...(....
                 20 FF 00 00 00 20 00 00 00 80 72 41 00 00 70 73  //  .... ....rA..ps
                 05 00 00 0A 28 0D 00 00 06 DE 03 26 DE 00 06 2C  // ....(......&...,
                 0F 72 49 00 00 70 28 0D 00 00 0A 1F 64 0B 2B 0E  // .rI..p(.....d.+.
                 72 57 00 00 70 28 0D 00 00 0A 17 0B 2B 00 07 2A  // rW..p(......+..*
                 01 10 00 00 00 00 02 00 25 27 00 03 04 00 00 01  // ........%'......
                 00 00 00 00 00 00 00 00 00 00 00 00 1E 02 28 05  // ..............(.
                 00 00 0A 2A 42 53 4A 42 01 00 01 00 00 00 00 00  // ...*BSJB........
                 0C 00 00 00 76 31 2E 78 38 36 66 72 65 00 00 00  // ....v1.x86fre...
                 00 00 05 00 6C 00 00 00 38 04 00 00 23 7E 00 00  // ....l...8...#~..
                 A4 04 00 00 A4 03 00 00 23 53 74 72 69 6E 67 73  // ........#Strings
                 00 00 00 00 48 08 00 00 A0 03 00 00 23 42 6C 6F  // ....H.......#Blo
                 62 00 00 00 E8 0B 00 00 68 00 00 00 23 55 53 00  // b.......h...#US.
                 50 0C 00 00 10 00 00 00 23 47 55 49 44 00 00 00  // P.......#GUID...
                 00 00 00 00 01 00 00 01 57 DF 16 29 09 02 00 00  // ........W..)....
                 00 FA 01 33 00 02 00 00 01 00 00 00 12 00 00 00  // ...3............
                 0E 00 00 00 0C 00 00 00 0F 00 00 00 0C 00 00 00
                 01 00 00 00 0E 00 00 00 02 00 00 00 02 00 00 00
                 01 00 00 00 06 00 00 00 04 00 00 00 01 00 00 00
                 01 00 00 00 02 00 00 00 02 00 00 00 06 00 00 00
                 01 00 00 00 01 00 00 00 06 00 00 00 00 00 4C 00  // ..............L.
                 01 00 00 00 00 00 06 00 67 00 60 00 06 00 74 00  // ........g.`...t.
                 60 00 06 00 7E 00 60 00 06 00 A0 00 60 00 06 00  // `...~.`.....`...
                 DC 00 60 00 06 00 E9 00 60 00 06 00 51 01 35 01  // ..`.....`...Q.5.
                 06 00 6D 01 35 01 06 00 8F 01 7C 01 06 00 C7 01  // ..m.5.....|.....
                 60 00 06 00 E3 01 60 00 06 00 EB 01 60 00 06 00  // `.....`.....`...
                 46 02 26 02 06 00 55 02 60 00 06 00 5B 02 60 00  // F.&...U.`...[.`.
                 06 00 A6 02 60 00 06 00 70 03 60 00 06 00 87 03  // ....`...p.`.....
                 77 03 00 00 00 00 43 00 00 00 00 00 01 00 01 00  // w.....C.........
                 08 01 10 00 71 00 00 00 05 00 01 00 01 00 00 21  // ....q..........!
                 00 00 79 00 00 00 09 00 03 00 02 00 01 01 00 00  // ..y.............
                 90 00 00 00 0D 00 06 00 02 00 A1 00 00 00 9A 00
                 00 00 00 00 06 00 06 00 01 00 10 00 A7 00 00 00
                 11 00 06 00 07 00 01 00 10 00 AD 00 00 00 11 00
                 07 00 0C 00 00 00 00 00 F1 01 00 00 11 00 07 00
                 10 00 03 01 00 00 FE 01 00 00 05 00 0D 00 10 00
                 03 01 00 00 7E 02 00 00 05 00 0D 00 10 00 03 01  // ....~...........
                 00 00 B8 02 00 00 05 00 0D 00 10 00 03 01 00 00
                 E4 02 00 00 05 00 0D 00 10 00 03 01 00 00 0C 03
                 00 00 05 00 0D 00 10 00 03 01 00 00 34 03 00 00  // ............4...
                 05 00 0D 00 10 00 06 00 B4 00 1F 00 16 00 B6 00
                 1F 00 06 06 BF 00 1F 00 56 80 C7 00 26 00 56 80  // ........V...&.V.
                 CB 00 26 00 01 00 12 01 50 00 13 01 12 02 9B 00  // ..&.....P.......
                 13 01 92 02 B7 00 13 01 CC 02 C0 00 13 01 F8 02
                 CA 00 13 01 20 03 D5 00 13 01 48 03 D9 00 50 20  // .... .....H...P 
                 00 00 00 00 91 18 B8 00 22 00 01 00 00 00 00 00  // ........".......
                 03 00 86 18 CF 00 34 00 01 00 00 00 00 00 03 00  // ......4.........
                 C6 00 D5 00 3A 00 03 00 00 00 00 00 03 00 C6 01  // ....:...........
                 F7 00 3E 00 03 00 00 00 00 00 03 00 C6 01 03 01  // ..>.............
                 46 00 05 00 00 00 00 00 00 00 C6 05 0D 01 4C 00  // F.............L.
                 06 00 58 20 00 00 20 00 86 08 15 01 54 00 06 00  // ..X .. .....T...
                 71 20 00 00 20 00 86 08 1C 01 54 00 07 00 8A 20  // q .. .....T.... 
                 00 00 00 00 86 00 26 01 3A 00 08 00 8C 20 00 00  // ......&.:.... ..
                 00 00 E6 01 0D 01 4C 00 08 00 9E 20 00 00 00 00  // ......L.... ....
                 86 18 CF 00 3A 00 08 00 4C 21 00 00 00 00 96 00  // ....:...L!......
                 28 01 5A 00 08 00 54 25 00 00 00 00 96 00 2C 01  // (.Z...T%......,.
                 5F 00 09 00 20 26 00 00 00 00 96 00 30 01 67 00  // _... &......0.g.
                 0D 00 94 26 00 00 00 00 86 18 CF 00 3A 00 0D 00  // ...&........:...
                 00 00 01 00 A3 01 00 00 02 00 AA 01 00 00 01 00
                 B1 01 00 00 02 00 A3 01 00 00 01 00 BA 01 00 00
                 01 00 C1 01 00 00 01 00 C1 01 00 00 01 00 DF 01
                 00 00 01 00 5C 03 00 00 02 00 61 03 00 00 03 00  // ....\.....a.....
                 66 03 00 00 04 00 6B 03 06 00 14 00 39 00 CF 00  // f.....k.....9...
                 6B 00 49 00 CF 00 71 00 51 00 D0 01 77 00 51 00  // k.I...q.Q...w.Q.
                 D8 01 77 00 21 00 CF 00 3A 00 59 00 CF 00 84 00  // ..w.!...:.Y.....
                 0C 00 CF 00 95 00 69 00 6E 02 9F 00 14 00 CF 00  // ......i.n.......
                 B0 00 81 00 AE 02 BB 00 0C 00 E0 02 C4 00 14 00
                 E0 02 CE 00 81 00 AE 02 97 01 91 00 CF 00 3A 00  // ..............:.
                 08 00 10 00 2A 00 08 00 14 00 2F 00 27 00 73 00  // ....*...../.'.s.
                 2F 00 2E 00 13 00 A1 01 08 00 06 00 A8 01 01 00  // /...............
                 10 00 00 00 09 00 01 00 24 00 00 00 0A 00 01 00  // ........$.......
                 10 00 00 00 0B 00 01 00 24 00 00 00 0C 00 01 00  // ........$.......
                 10 00 00 00 0D 00 01 00 24 00 00 00 0E 00 80 00  // ........$.......
                 DD 00 79 01 9C 01 06 00 01 00 00 00 12 01 10 00  // ..y.............
                 08 00 07 00 02 00 10 00 08 00 02 00 8D 00 A7 00
                 A8 20 00 00 07 00 B8 20 00 00 08 00 E0 20 00 00  // . ..... ..... ..
                 09 00 F0 20 00 00 0A 00 18 21 00 00 0B 00 28 21  // ... .....!....(!
                 00 00 0C 00 04 80 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 AD 00 00 00 01 00 00 00 6B 09  // ..............k.
                 00 00 00 00 00 00 01 00 57 00 00 00 0A 00 09 00  // ........W.......
                 08 00 0A 00 08 00 0B 00 08 00 0C 00 08 00 0D 00
                 08 00 0E 00 08 00 00 00 00 56 65 72 73 69 6F 6E  // .........Version
                 20 6F 66 20 72 75 6E 74 69 6D 65 20 61 67 61 69  //  of runtime agai
                 6E 73 74 20 77 68 69 63 68 20 74 68 65 20 62 69  // nst which the bi
                 6E 61 72 79 20 69 73 20 62 75 69 6C 74 20 3A 20  // nary is built : 
                 31 2E 30 2E 32 34 31 34 2E 30 00 3C 4D 6F 64 75  // 1.0.2414.0.<Modu
                 6C 65 3E 00 63 61 74 63 68 33 2E 65 78 65 00 6D  // le>.filter3.exe.m
                 73 63 6F 72 6C 69 62 00 53 79 73 74 65 6D 00 56  // scorlib.System.V
                 61 6C 75 65 54 79 70 65 00 56 54 00 45 6E 75 6D  // alueType.VT.Enum
                 00 44 61 79 73 00 4D 75 6C 74 69 63 61 73 74 44  // .Days.MulticastD
                 65 6C 65 67 61 74 65 00 64 65 6C 65 67 61 74 65  // elegate.delegate
                 31 00 49 54 65 73 74 00 4F 62 6A 65 63 74 00 43  // 1.ITest.Object.C
                 54 65 73 74 00 63 61 74 63 68 33 00 6D 00 6E 00  // Test.filter3.m.n.
                 2E 63 63 74 6F 72 00 76 61 6C 75 65 5F 5F 00 53  // .cctor.value__.S
                 75 6E 00 4D 6F 6E 00 2E 63 74 6F 72 00 49 6E 76  // un.Mon..ctor.Inv
                 6F 6B 65 00 49 41 73 79 6E 63 52 65 73 75 6C 74  // oke.IAsyncResult
                 00 41 73 79 6E 63 43 61 6C 6C 62 61 63 6B 00 42  // .AsyncCallback.B
                 65 67 69 6E 49 6E 76 6F 6B 65 00 45 6E 64 49 6E  // eginInvoke.EndIn
                 76 6F 6B 65 00 74 65 73 74 00 64 67 00 61 64 64  // voke.test.dg.add
                 5F 64 67 00 72 65 6D 6F 76 65 5F 64 67 00 66 00  // _dg.remove_dg.f.
                 66 32 35 00 66 32 36 00 4D 61 69 6E 00 53 79 73  // f25.f26.Main.Sys
                 74 65 6D 2E 53 65 63 75 72 69 74 79 2E 50 65 72  // tem.Security.Per
                 6D 69 73 73 69 6F 6E 73 00 53 65 63 75 72 69 74  // missions.Securit
                 79 50 65 72 6D 69 73 73 69 6F 6E 41 74 74 72 69  // yPermissionAttri
                 62 75 74 65 00 53 65 63 75 72 69 74 79 41 63 74  // bute.SecurityAct
                 69 6F 6E 00 53 79 73 74 65 6D 2E 44 69 61 67 6E  // ion.System.Diagn
                 6F 73 74 69 63 73 00 44 65 62 75 67 67 61 62 6C  // ostics.Debuggabl
                 65 41 74 74 72 69 62 75 74 65 00 6F 62 6A 65 63  // eAttribute.objec
                 74 00 6D 65 74 68 6F 64 00 63 61 6C 6C 62 61 63  // t.method.callbac
                 6B 00 72 65 73 75 6C 74 00 76 61 6C 75 65 00 44  // k.result.value.D
                 65 6C 65 67 61 74 65 00 43 6F 6D 62 69 6E 65 00  // elegate.Combine.
                 52 65 6D 6F 76 65 00 61 72 67 00 44 65 63 69 6D  // Remove.arg.Decim
                 61 6C 00 49 6E 74 33 32 00 3C 47 6C 6F 62 61 6C  // al.Int32.<Global
                 4E 61 6D 65 3E 00 24 24 73 74 72 75 63 74 30 78  // Name>.$$struct0x
                 36 30 30 30 30 30 63 2D 31 00 24 24 6D 65 74 68  // 600000c-1.$$meth
                 6F 64 30 78 36 30 30 30 30 30 63 2D 31 00 53 79  // od0x600000c-1.Sy
                 73 74 65 6D 2E 52 75 6E 74 69 6D 65 2E 43 6F 6D  // stem.Runtime.Com
                 70 69 6C 65 72 53 65 72 76 69 63 65 73 00 52 75  // pilerServices.Ru
                 6E 74 69 6D 65 48 65 6C 70 65 72 73 00 41 72 72  // ntimeHelpers.Arr
                 61 79 00 52 75 6E 74 69 6D 65 46 69 65 6C 64 48  // ay.RuntimeFieldH
                 61 6E 64 6C 65 00 49 6E 69 74 69 61 6C 69 7A 65  // andle.Initialize
                 41 72 72 61 79 00 24 24 73 74 72 75 63 74 30 78  // Array.$$struct0x
                 36 30 30 30 30 30 63 2D 32 00 24 24 6D 65 74 68  // 600000c-2.$$meth
                 6F 64 30 78 36 30 30 30 30 30 63 2D 32 00 43 6F  // od0x600000c-2.Co
                 6E 73 6F 6C 65 00 57 72 69 74 65 4C 69 6E 65 00  // nsole.WriteLine.
                 24 24 73 74 72 75 63 74 30 78 36 30 30 30 30 30  // $$struct0x600000
                 63 2D 33 00 24 24 6D 65 74 68 6F 64 30 78 36 30  // c-3.$$method0x60
                 30 30 30 30 63 2D 33 00 47 65 74 00 24 24 73 74  // 0000c-3.Get.$$st
                 72 75 63 74 30 78 36 30 30 30 30 30 63 2D 34 00  // ruct0x600000c-4.
                 24 24 6D 65 74 68 6F 64 30 78 36 30 30 30 30 30  // $$method0x600000
                 63 2D 34 00 24 24 73 74 72 75 63 74 30 78 36 30  // c-4.$$struct0x60
                 30 30 30 30 63 2D 35 00 24 24 6D 65 74 68 6F 64  // 0000c-5.$$method
                 30 78 36 30 30 30 30 30 63 2D 35 00 24 24 73 74  // 0x600000c-5.$$st
                 72 75 63 74 30 78 36 30 30 30 30 30 63 2D 36 00  // ruct0x600000c-6.
                 24 24 6D 65 74 68 6F 64 30 78 36 30 30 30 30 30  // $$method0x600000
                 63 2D 36 00 61 72 67 31 00 61 72 67 32 00 61 72  // c-6.arg1.arg2.ar
                 67 33 00 61 72 67 34 00 53 74 72 69 6E 67 00 53  // g3.arg4.String.S
                 79 73 74 65 6D 2E 53 65 63 75 72 69 74 79 00 55  // ystem.Security.U
                 6E 76 65 72 69 66 69 61 62 6C 65 43 6F 64 65 41  // nverifiableCodeA
                 74 74 72 69 62 75 74 65 00 00 00 00 00 08 B7 7A  // ttribute.......z
                 5C 56 19 34 E0 89 14 20 5A 42 25 D6 7A DD D7 BA  // \V.4... ZB%.z...
                 14 D5 1C EE 42 95 EE 4F C6 7A F9 02 06 08 03 00  // ....B..O.z......
                 00 01 03 06 11 0C 04 00 00 00 00 04 01 00 00 00
                 05 20 02 01 1C 19 03 20 00 01 07 20 02 12 15 12  // . ..... ... ....
                 19 1C 05 20 01 01 12 15 03 20 00 08 03 06 12 10  // ... ..... ......
                 05 20 01 01 12 10 04 00 01 01 08 07 00 04 01 07  // . ..............
                 08 0E 1C 03 00 00 08 05 20 01 01 11 21 05 20 02  // ........ ...!. .
                 01 02 02 08 00 02 12 29 12 29 12 29 03 07 01 08  // .......).).)....
                 08 20 05 01 08 08 08 02 05 07 14 08 02 00 02 00  // . ..............
                 00 05 20 02 01 08 08 03 06 11 24 07 00 02 01 12  // .. .......$.....
                 39 11 3D 08 14 08 03 00 03 00 00 00 06 20 03 01  // 9.=.......... ..
                 08 08 08 03 06 11 28 04 00 01 01 1C 03 06 11 2C  // ......(........,
                 05 20 02 08 08 08 03 06 11 30 06 20 03 08 08 08  // . .......0. ....
                 08 03 06 11 34 03 06 11 38 80 9A 07 5B 08 09 06  // ....4...8...[...
                 07 05 04 0A 0B 03 0C 0D 02 11 2D 11 08 08 08 11  // ..........-.....
                 0C 1C 12 1C 0E 1D 08 14 08 02 00 02 00 00 14 08
                 03 00 03 00 00 00 12 18 08 12 18 08 08 09 06 07
                 05 04 0A 0B 03 0C 0D 02 11 2D 11 08 08 08 11 0C  // .........-......
                 1C 12 1C 0E 1D 08 08 14 08 02 00 02 00 00 08 14
                 08 03 00 03 00 00 00 08 12 18 08 12 18 08 08 08
                 09 06 07 05 04 0A 0B 03 0C 0D 02 11 2D 11 08 08  // ............-...
                 08 11 0C 1C 12 1C 0E 1D 08 08 14 08 02 00 02 00
                 00 08 14 08 03 00 03 00 00 00 08 12 18 08 12 18
                 08 08 1D 08 08 1D 07 17 07 08 0E 1C 06 05 0E 08
                 1D 0E 07 07 08 0E 1C 06 05 0E 08 1D 0E 08 0F 07
                 07 1D 0E 04 00 01 01 0E 04 07 02 02 08 06 01 00
                 00 01 00 00 81 F6 00 01 00 00 00 FF FF FF FF 01
                 00 00 00 00 00 00 00 02 01 00 00 00 1D 53 79 73  // .............Sys
                 74 65 6D 2E 53 65 63 75 72 69 74 79 2E 50 65 72  // tem.Security.Per
                 6D 69 73 73 69 6F 6E 53 65 74 04 00 00 00 0C 72  // missionSet.....r
                 65 61 64 61 62 6C 65 6F 6E 6C 79 0E 6D 5F 55 6E  // eadableonly.m_Un
                 72 65 73 74 72 69 63 74 65 64 15 6D 5F 75 6E 72  // restricted.m_unr
                 65 73 74 72 69 63 74 65 64 50 65 72 6D 53 65 74  // estrictedPermSet
                 0F 6D 5F 6E 6F 72 6D 61 6C 50 65 72 6D 53 65 74  // .m_normalPermSet
                 01 00 09 02 00 00 00 09 03 00 00 00 02 02 00 00
                 00 22 53 79 73 74 65 6D 2E 53 65 63 75 72 69 74  // ."System.Securit
                 79 2E 55 74 69 6C 2E 54 6F 6B 65 6E 42 61 73 65  // y.Util.TokenBase
                 64 53 65 74 05 00 00 00 08 6D 5F 6F 62 6A 53 65  // dSet.....m_objSe
                 74 06 6D 5F 63 45 6C 74 0A 6D 5F 69 6E 69 74 53  // t.m_cElt.m_initS
                 69 7A 65 0B 6D 5F 69 6E 63 72 65 6D 65 6E 74 0A  // ize.m_increment.
                 6D 5F 6D 61 78 49 6E 64 65 78 09 04 00 00 00 01  // m_maxIndex......
                 00 00 00 10 00 00 00 08 00 00 00 06 00 00 00 01
                 03 00 00 00 02 00 00 00 09 05 00 00 00 00 00 00
                 00 08 00 00 00 08 00 00 00 FF FF FF FF 07 04 00
                 00 00 00 01 00 00 00 10 00 00 00 03 0D 53 79 73  // .............Sys
                 74 65 6D 2E 4F 62 6A 65 63 74 0A 0A 0A 0A 0A 0A  // tem.Object......
                 09 06 00 00 00 0A 0A 0A 0A 0A 0A 0A 0A 0A 07 05
                 00 00 00 00 01 00 00 00 08 00 00 00 03 0D 53 79  // ..............Sy
                 73 74 65 6D 2E 4F 62 6A 65 63 74 0A 0A 0A 0A 0A  // stem.Object.....
                 0A 0A 0A 02 06 00 00 00 2E 53 79 73 74 65 6D 2E  // .........System.
                 53 65 63 75 72 69 74 79 2E 50 65 72 6D 69 73 73  // Security.Permiss
                 69 6F 6E 73 2E 53 65 63 75 72 69 74 79 50 65 72  // ions.SecurityPer
                 6D 69 73 73 69 6F 6E 01 00 00 00 07 6D 5F 66 6C  // mission.....m_fl
                 61 67 73 02 FF FF FF FF 32 53 79 73 74 65 6D 2E  // ags.....2System.
                 53 65 63 75 72 69 74 79 2E 50 65 72 6D 69 73 73  // Security.Permiss
                 69 6F 6E 73 2E 53 65 63 75 72 69 74 79 50 65 72  // ions.SecurityPer
                 6D 69 73 73 69 6F 6E 46 6C 61 67 01 00 00 00 07  // missionFlag.....
                 76 61 6C 75 65 5F 5F 04 00 00 00 0B 00 17 74 00  // value__.......t.
                 65 00 73 00 74 00 20 00 73 00 74 00 72 00 69 00  // e.s.t. .s.t.r.i.
                 6E 00 67 00 00 17 73 00 74 00 72 00 69 00 6E 00  // n.g...s.t.r.i.n.
                 67 00 20 00 74 00 65 00 73 00 74 00 00 07 66 00  // g. .t.e.s.t...f.
                 32 00 38 00 00 03 61 00 00 03 62 00 00 07 66 00  // 2.8...a...b...f.
                 32 00 36 00 00 0D 50 00 41 00 53 00 53 00 45 00  // 2.6...P.A.S.S.E.
                 44 00 00 0D 46 00 41 00 49 00 4C 00 45 00 44 00  // D...F.A.I.L.E.D.
                 00 00 00 00 70 0E A4 BD 7F FE 22 41 99 48 D0 CF  // ....p....."A.H..
                 3C 7C F3 FF 24 33 00 00 00 00 00 00 00 00 00 00  // <|..$3..........
                 3E 33 00 00 00 20 00 00 00 00 00 00 00 00 00 00  // >3... ..........
                 00 00 00 00 00 00 00 00 00 00 00 00 30 33 00 00  // ............03..
                 00 00 00 00 00 00 00 00 00 00 5F 43 6F 72 45 78  // .........._CorEx
                 65 4D 61 69 6E 00 6D 73 63 6F 72 65 65 2E 64 6C  // eMain.mscoree.dl
                 6C 00 00 00 00 00 FF 25 00 20 40 00)             // l......%. @.
//*********** DISASSEMBLY COMPLETE ***********************
