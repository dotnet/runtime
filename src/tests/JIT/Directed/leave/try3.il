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
  .method private specialname rtspecialname static 
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

.class public sealed delegate1
       extends [mscorlib]System.MulticastDelegate
{
  .method public specialname rtspecialname 
          instance void  .ctor(class System.Object o,
                               native unsigned int 'method') runtime managed
  {
  } // end of method delegate1::.ctor

  .method public virtual instance void
          Invoke() runtime managed
  {
  } // end of method delegate1::Invoke

  .method public newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                      class System.Object o) runtime managed
  {
  } // end of method delegate1::BeginInvoke

  .method public newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method delegate1::EndInvoke

} // end of class delegate1

.class interface public abstract ITest
{
  .method public newslot virtual abstract 
          instance int32 test() il managed
  {
  } // end of method ITest::test

} // end of class ITest

.class public CTest
       extends [mscorlib]System.Object
       implements ITest
{
  .field private class delegate1 dg
  .method public specialname instance void 
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

  .method public specialname instance void 
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

  .method public instance void f() il managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method CTest::f

  .method public newslot final virtual 
          instance int32 test() il managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals ([0] int32 _Vb_t_$00000003$00000000)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method CTest::test

  .method public specialname rtspecialname 
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

.class public try3
       extends [mscorlib]System.Object
{
  .method public static void  f25(int32 arg) il managed
  {
    // Code size       627 (0x273)
    .maxstack  60
    .locals ([0] int32 a2,
             [1] unsigned int32 a3,
             [2] int16 a4,
             [3] unsigned int16 a5,
             [4] unsigned int8 a6,
             [5] int8 a7,
             [6] int64 a8,
             [7] unsigned int64 a9,
             [8] wchar a10,
             [9] float32 a11,
             [10] float64 a12,
             [11] bool a13,
             [12] value class [mscorlib]System.Decimal a14,
             [13] value class VT a15,
             [14] int32 c15a,
             [15] int32 d15a,
             [16] value class Days a16,
             [17] class System.Object a17,
             [18] class try3 a18,
             [19] class System.String a19,
             [20] int32[] a20,
             [21] int32[0...,0...] a21,
             [22] int32[0...,0...,0...] a22,
             [23] class CTest a23,
             [24] int32 c23,
             [25] class CTest a24,
             [26] int32 a25,
             [27] int32 b2,
             [28] unsigned int32 b3,
             [29] int16 b4,
             [30] unsigned int16 b5,
             [31] unsigned int8 b6,
             [32] int8 b7,
             [33] int64 b8,
             [34] unsigned int64 b9,
             [35] wchar b10,
             [36] float32 b11,
             [37] float64 b12,
             [38] bool b13,
             [39] value class [mscorlib]System.Decimal b14,
             [40] value class VT b15,
             [41] int32 c15b,
             [42] int32 d15b,
             [43] value class Days b16,
             [44] class System.Object b17,
             [45] class try3 b18,
             [46] class System.String b19,
             [47] int32[] b20,
             [48] int32 b,
             [49] int32[0...,0...] b21,
             [50] int32 c21,
             [51] int32[0...,0...,0...] b22,
             [52] int32 c22,
             [53] class CTest b23,
             [54] int32 d23,
             [55] class CTest b24,
             [56] int32 b25,
             [57] int32 a,
             [58] int32[] _Vb_t_$00000002$00000000,
             [59] int32[] _Vb_t_$00000002$00000001,
             [60] int32 _Vb_t_$00000002$00000002)
    IL_0000:  ldloca.s   a15
    IL_0002:  initobj    VT
    IL_0008:  ldloca.s   b15
    IL_000a:  initobj    VT
    IL_0010:  ldc.i4     0xffff63c0
    IL_0015:  stloc.0
    IL_0016:  ldc.i4     0x9c40
    IL_001b:  stloc.1
    IL_001c:  ldc.i4     0xfffffc18
    IL_0021:  stloc.2
    IL_0022:  ldc.i4     0x3e8
    IL_0027:  stloc.3
    IL_0028:  ldc.i4.s   20
    IL_002a:  stloc.s    a6
    IL_002c:  ldc.i4.s   -20
    IL_002e:  stloc.s    a7
    IL_0030:  ldc.i8     0x8000000000000000
    IL_0039:  stloc.s    a8
    IL_003b:  ldc.i4.0
    IL_003c:  conv.i8
    IL_003d:  stloc.s    a9
    IL_003f:  ldc.i4.s   49
    IL_0041:  stloc.s    a10
    IL_0043:  ldc.r4     -2.5699999
    IL_0048:  stloc.s    a11
    IL_004a:  ldc.r8     -1.2888888888888881
    IL_0053:  stloc.s    a12
    IL_0055:  ldc.i4.1
    IL_0056:  stloc.s    a13
    IL_0058:  ldc.i4     0x2715
    IL_005d:  ldc.i4.0
    IL_005e:  ldc.i4.0
    IL_005f:  ldc.i4     0x80
    IL_0064:  ldc.i4.2
    IL_0065:  newobj     instance void [mscorlib]System.Decimal::.ctor(int32,
                                                                       int32,
                                                                       int32,
                                                                       bool,
                                                                       unsigned int8)
    IL_006a:  stloc.s    a14
    IL_006c:  ldloca.s   a15
    IL_006e:  ldc.i4     0xfffffed4
    IL_0073:  stfld      int32 VT::m
    IL_0078:  ldloca.s   a15
    IL_007a:  ldfld      int32 VT::m
    IL_007f:  stloc.s    c15a
    IL_0081:  ldsfld     int32 VT::n
    IL_0086:  stloc.s    d15a
    IL_0088:  ldc.i4.0
    IL_0089:  stloc.s    a16
    IL_008b:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0090:  stloc.s    a17
    IL_0092:  newobj     instance void try3::.ctor()
    IL_0097:  stloc.s    a18
    IL_0099:  ldstr      "test string"
    IL_009e:  stloc.s    a19
    IL_00a0:  ldc.i4.2
    IL_00a1:  newarr     [mscorlib]System.Int32
    IL_00a6:  stloc.s    _Vb_t_$00000002$00000000
    IL_00a8:  ldloc.s    _Vb_t_$00000002$00000000
    IL_00aa:  ldc.i4.0
    IL_00ab:  ldc.i4.1
    IL_00ac:  stelem.i4
    IL_00ad:  ldloc.s    _Vb_t_$00000002$00000000
    IL_00af:  ldc.i4.1
    IL_00b0:  ldc.i4.2
    IL_00b1:  stelem.i4
    IL_00b2:  ldloc.s    _Vb_t_$00000002$00000000
    IL_00b4:  stloc.s    a20
    IL_00b6:  ldc.i4.2
    IL_00b7:  ldc.i4.2
    IL_00b8:  newobj     instance void int32[0...,0...]::.ctor(int32,
                                                               int32)
    IL_00bd:  dup
    IL_00be:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000c-1' '<GlobalName>'::'$$method0x600000c-1'
    IL_00c3:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        value class [mscorlib]System.RuntimeFieldHandle)
    IL_00c8:  stloc.s    a21
    IL_00ca:  ldc.i4.3
    IL_00cb:  ldc.i4.1
    IL_00cc:  ldc.i4.3
    IL_00cd:  newobj     instance void int32[0...,0...,0...]::.ctor(int32,
                                                                    int32,
                                                                    int32)
    IL_00d2:  dup
    IL_00d3:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000c-2' '<GlobalName>'::'$$method0x600000c-2'
    IL_00d8:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        value class [mscorlib]System.RuntimeFieldHandle)
    IL_00dd:  stloc.s    a22
    IL_00df:  newobj     instance void CTest::.ctor()
    IL_00e4:  stloc.s    a23
    IL_00e6:  ldloc.s    a23
    IL_00e8:  callvirt   instance int32 CTest::test()
    IL_00ed:  stloc.s    c23
    IL_00ef:  newobj     instance void CTest::.ctor()
    IL_00f4:  stloc.s    a24
    IL_00f6:  ldloc.s    a24
    IL_00f8:  ldloc.s    a24
    IL_00fa:  ldftn      instance void CTest::f()
    IL_0100:  newobj     instance void delegate1::.ctor(class System.Object,
                                                        native unsigned int)
    IL_0105:  callvirt   instance void CTest::add_dg(class delegate1)
    IL_010a:  ldarg.0
    IL_010b:  stloc.s    a25
    .try
    {
      IL_010d:  ldc.i4     0xffff63c0
      //IL_0112:  stloc.s    b2
      IL_0114:  ldc.i4     0x9c40
      //IL_0119:  stloc.s    b3
      IL_011b:  ldc.i4     0xfffffc18
      //IL_0120:  stloc.s    b4
      IL_0122:  ldc.i4     0x3e8
      //IL_0127:  stloc.s    b5
      IL_0129:  ldc.i4.s   20
      //IL_012b:  stloc.s    b6
      IL_012d:  ldc.i4.s   -20
      //IL_012f:  stloc.s    b7
      IL_0131:  ldc.i8     0x7fffffffffffffff
      //IL_013a:  stloc.s    b8
      IL_013c:  ldc.i4.m1
      IL_013d:  conv.i8
      //IL_013e:  stloc.s    b9
      IL_0140:  ldc.i4.s   65
      //IL_0142:  stloc.s    b10
      IL_0144:  ldc.r4     2.5699999
      //IL_0149:  stloc.s    b11
      IL_014b:  ldc.r8     1.2888888888888881
      //IL_0154:  stloc.s    b12
      IL_0156:  ldc.i4.0
      //IL_0157:  stloc.s    b13
      IL_0159:  ldc.i4     0x676
      IL_015e:  ldc.i4.0
      IL_015f:  ldc.i4.0
      IL_0160:  ldc.i4.0
      IL_0161:  ldc.i4.3
      IL_0162:  newobj     instance void [mscorlib]System.Decimal::.ctor(int32,
                                                                         int32,
                                                                         int32,
                                                                         bool,
                                                                         unsigned int8)
      //IL_0167:  stloc.s    b14
      IL_0169:  ldloca.s   b15
      IL_016b:  ldc.i4     0x12c
      IL_0170:  stfld      int32 VT::m
      IL_0175:  ldloca.s   b15
      IL_0177:  ldfld      int32 VT::m
      //IL_017c:  stloc.s    c15b
      IL_017e:  ldsfld     int32 VT::n
      //IL_0183:  stloc.s    d15b
      IL_0185:  ldc.i4.1
      IL_0186:  stloc.s    b16
      IL_0188:  ldloc.s    b16
      IL_018a:  box        Days
      //IL_018f:  call       void [System.Console]System.Console::WriteLine(class System.Object)
      IL_0194:  newobj     instance void [mscorlib]System.Object::.ctor()
      IL_0199:  stloc.s    b17
      IL_019b:  newobj     instance void try3::.ctor()
      //IL_01a0:  stloc.s    b18
      IL_01a2:  ldstr      "string test"
      //IL_01a7:  stloc.s    b19
      IL_01a9:  ldc.i4.2
      IL_01aa:  newarr     [mscorlib]System.Int32
      IL_01af:  stloc.s    _Vb_t_$00000002$00000001
      IL_01b1:  ldloc.s    _Vb_t_$00000002$00000001
      IL_01b3:  ldc.i4.0
      IL_01b4:  ldc.i4.3
      IL_01b5:  stelem.i4
      IL_01b6:  ldloc.s    _Vb_t_$00000002$00000001
      IL_01b8:  ldc.i4.1
      IL_01b9:  ldc.i4.4
      IL_01ba:  stelem.i4
      IL_01bb:  ldloc.s    _Vb_t_$00000002$00000001
      IL_01bd:  stloc.s    b20
      IL_01bf:  ldloc.s    b20
      IL_01c1:  ldc.i4.0
      IL_01c2:  ldelem.i4
      //IL_01c3:  stloc.s    b
      IL_01c5:  ldc.i4.2
      IL_01c6:  ldc.i4.2
      IL_01c7:  newobj     instance void int32[0...,0...]::.ctor(int32,
                                                                 int32)
      IL_01cc:  dup
      IL_01cd:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000c-3' '<GlobalName>'::'$$method0x600000c-3'
      //IL_01d2:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
      //                                                                                                    value class [mscorlib]System.RuntimeFieldHandle)
      //IL_01d7:  stloc.s    b21
      //IL_01d9:  ldloc.s    b21
      //IL_01db:  ldc.i4.0
      //IL_01dc:  ldc.i4.1
      //IL_01dd:  call       instance int32 int32[0...,0...]::Get(int32,
      //                                                          int32)
      //IL_01e2:  stloc.s    c21
      IL_01e4:  ldc.i4.3
      IL_01e5:  ldc.i4.1
      IL_01e6:  ldc.i4.3
      IL_01e7:  newobj     instance void int32[0...,0...,0...]::.ctor(int32,
                                                                      int32,
                                                                      int32)
      IL_01ec:  dup
      //IL_01ed:  ldtoken    field value class '<GlobalName>'/'$$struct0x600000c-4' '<GlobalName>'::'$$method0x600000c-4'
      //IL_01f2:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
      //                                                                                                    value class [mscorlib]System.RuntimeFieldHandle)
      //IL_01f7:  stloc.s    b22
      //IL_01f9:  ldloc.s    b22
      //IL_01fb:  ldc.i4.0
      //IL_01fc:  ldc.i4.0
      //IL_01fd:  ldc.i4.1
      //IL_01fe:  call       instance int32 int32[0...,0...,0...]::Get(int32,
      //                                                               int32,
      //                                                               int32)
      //IL_0203:  stloc.s    c22
      IL_0205:  newobj     instance void CTest::.ctor()
      IL_020a:  stloc.s    b23
      IL_020c:  ldloc.s    b23
      IL_020e:  callvirt   instance int32 CTest::test()
      //IL_0213:  stloc.s    d23
      IL_0215:  newobj     instance void CTest::.ctor()
      IL_021a:  stloc.s    b24
      IL_021c:  ldloc.s    b24
      IL_021e:  ldloc.s    b24
      IL_0220:  ldftn      instance void CTest::f()
      //IL_0226:  newobj     instance void delegate1::.ctor(class System.Object,
      //                                                    native unsigned int)
      //IL_022b:  callvirt   instance void CTest::add_dg(class delegate1)
      //IL_0230:  ldarg.0
      //IL_0231:  stloc.s    b25
      IL_0233:  ldc.i4.1
      IL_0234:  stloc.s    a
      IL_0236:  ldloc.s    a
      IL_0238:  ldc.i4.2
      IL_0239:  mul
      IL_023a:  ldc.i4.2
      IL_023b:  bne.un.s   IL_0267

      IL_023d:  ldloc.s    a
      IL_023f:  stloc.s    _Vb_t_$00000002$00000002
      IL_0241:  ldloc.s    _Vb_t_$00000002$00000002
      IL_0243:  switch     ( 
                            IL_0256,
                            IL_0258,
                            IL_0260)
      IL_0254:  br.s       IL_0265

      IL_0256:  br.s       IL_0265

      IL_0258:  ldloc.s    a
      IL_025a:  ldc.i4.2
      IL_025b:  add
      IL_025c:  stloc.s    a
      IL_025e:  br.s       IL_0265

      IL_0260:  ldc.i4.3
      IL_0261:  stloc.s    a
      IL_0263:  br.s       IL_0256

      IL_0265:  br.s       IL_026d

      IL_0267:  ldloc.s    a
      IL_0269:  ldc.i4.5
      IL_026a:  sub
      IL_026b:  stloc.s    a
      IL_026d:  leave.s    IL_0272

    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_026f:  pop
      IL_0270:  leave.s    IL_0272

    }  // end handler
    IL_0272:  ret
  } // end of method try3::f25

  .method public static void  f26(unsigned int16 arg1,
                                            int32 arg2,
                                            class System.String arg3,
                                            class System.Object arg4) il managed
  {
    // Code size       163 (0xa3)
    .maxstack  60
    .locals ([0] unsigned int16 a26,
             [1] int32 b26,
             [2] class System.String c26,
             [3] class System.Object d26,
             [4] int16 a27,
             [5] unsigned int8 a28,
             [6] class System.String b28,
             [7] int32 c28,
             [8] class System.String[] d28,
             [9] unsigned int16 a29,
             [10] unsigned int16* b29,
             [11] unsigned int16 c29,
             [12] unsigned int16 a26b,
             [13] int32 b26b,
             [14] class System.String c26b,
             [15] class System.Object d26b,
             [16] int16 b27,
             [17] unsigned int8 a28b,
             [18] class System.String b28b,
             [19] int32 c28b,
             [20] class System.String[] d28b,
             [21] int32 e28b,
             [22] class System.String[] _Vb_t_$00000002$00000000,
             [23] class System.String[] _Vb_t_$00000002$00000001)
    IL_0000:  ldarg.0
    IL_0001:  stloc.0
    IL_0002:  ldarg.1
    IL_0003:  stloc.1
    IL_0004:  ldarg.2
    IL_0005:  stloc.2
    IL_0006:  ldarg.3
    IL_0007:  stloc.3
    IL_0008:  ldc.i4     0xfffffc18
    IL_000d:  stloc.s    a27
    IL_000f:  ldc.i4.s   20
    IL_0011:  stloc.s    a28
    IL_0013:  ldstr      "f28"
    IL_0018:  stloc.s    b28
    IL_001a:  ldc.i4     0xffff15a0
    IL_001f:  stloc.s    c28
    IL_0021:  ldc.i4.2
    IL_0022:  newarr     [mscorlib]System.String
    IL_0027:  stloc.s    _Vb_t_$00000002$00000000
    IL_0029:  ldloc.s    _Vb_t_$00000002$00000000
    IL_002b:  ldc.i4.0
    IL_002c:  ldstr      "a"
    IL_0031:  stelem.ref
    IL_0032:  ldloc.s    _Vb_t_$00000002$00000000
    IL_0034:  ldc.i4.1
    IL_0035:  ldstr      "b"
    IL_003a:  stelem.ref
    IL_003b:  ldloc.s    _Vb_t_$00000002$00000000
    IL_003d:  stloc.s    d28
    IL_003f:  ldc.i4     0x3e8
    IL_0044:  stloc.s    a29
    .try
    {
      IL_0046:  ldc.i4.2
      IL_0047:  ldc.i4.1
      IL_0048:  mul
      IL_0049:  localloc
      IL_004b:  stloc.s    b29
      IL_004d:  ldloc.s    b29
      IL_004f:  ldloc.s    a29
      IL_0051:  stind.i2
      IL_0052:  ldloc.s    b29
      IL_0054:  ldind.u2
      //IL_0055:  stloc.s    c29
      IL_0057:  ldarg.0
      //IL_0058:  stloc.s    a26b
      IL_005a:  ldarg.1
      //IL_005b:  stloc.s    b26b
      IL_005d:  ldarg.2
      //IL_005e:  stloc.s    c26b
      IL_0060:  ldarg.3
      //IL_0061:  stloc.s    d26b
      IL_0063:  ldloc.s    a27
      //IL_0065:  stloc.s    b27
      IL_0067:  ldc.i4.s   20
      //IL_0069:  stloc.s    a28b
      IL_006b:  ldstr      "f28"
      //IL_0070:  stloc.s    b28b
      IL_0072:  ldc.i4     0xffff15a0
      //IL_0077:  stloc.s    c28b
      IL_0079:  ldc.i4.2
      IL_007a:  newarr     [mscorlib]System.String
      IL_007f:  stloc.s    _Vb_t_$00000002$00000001
      IL_0081:  ldloc.s    _Vb_t_$00000002$00000001
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "a"
      //IL_0089:  stelem.ref
      IL_008a:  ldloc.s    _Vb_t_$00000002$00000001
      IL_008c:  ldc.i4.1
      IL_008d:  ldstr      "b"
      //IL_0092:  stelem.ref
      IL_0093:  ldloc.s    _Vb_t_$00000002$00000001
      IL_0095:  stloc.s    d28b
      IL_0097:  ldloc.s    d28b
      IL_0099:  ldlen
      //IL_009a:  conv.i4
      //IL_009b:  stloc.s    e28b
      IL_009d:  leave.s    IL_00a2

    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_009f:  pop
      IL_00a0:  leave.s    IL_00a2

    }  // end handler
    IL_00a2:  ret
  } // end of method try3::f26

  .method public static int32 Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       76 (0x4c)
    .maxstack  4
    .locals ([0] bool pass,
             [1] int32 _Vb_t_$00000003$00000000)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    .try
    {
      IL_0002:  ldc.i4     0x9c40
      IL_0007:  call       void try3::f25(int32)
      IL_000c:  ldc.i4     0xff
      IL_0011:  ldc.i4     0x80000000
      IL_0016:  ldstr      "f26"
      IL_001b:  newobj     instance void [mscorlib]System.Object::.ctor()
      IL_0020:  call       void try3::f26(unsigned int16,
                                            int32,
                                            class System.String,
                                            class System.Object)
      IL_0025:  leave.s    IL_002a

    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_0027:  pop
      IL_0028:  leave.s    IL_002a

    }  // end handler
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
  } // end of method try3::Main

  .method public hidebysig specialname rtspecialname 
          instance void .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method try3::.ctor  
  
} // end of class try3

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

  .field static assembly value class '<GlobalName>'/'$$struct0x600000c-1' '$$method0x600000c-1' at D_000020B0
  .field static assembly value class '<GlobalName>'/'$$struct0x600000c-2' '$$method0x600000c-2' at D_000020C0
  .field static assembly value class '<GlobalName>'/'$$struct0x600000c-3' '$$method0x600000c-3' at D_000020E8
  .field static assembly value class '<GlobalName>'/'$$struct0x600000c-4' '$$method0x600000c-4' at D_000020F8
} // end of class '<GlobalName>'

.data D_000020B0 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00) 
.data D_000020C0 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00
                 04 00 00 00 05 00 00 00 06 00 00 00 07 00 00 00
                 08 00 00 00 00 00 00 00) 
.data D_000020E8 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00) 
.data D_000020F8 = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00
                 04 00 00 00 05 00 00 00 06 00 00 00 07 00 00 00
                 08 00 00 00 0B 30 06 00 73 02 00 00 03 00 00 11  // .....0..s.......
                 12 0D FE 15 02 00 00 02 12 28 FE 15 02 00 00 02  // .........(......
                 20 C0 63 FF FF 0A 20 40 9C 00 00 0B 20 18 FC FF  //  .c... @.... ...
                 FF 0C 20 E8 03 00 00 0D 1F 14 13 04 1F EC 13 05  // .. .............
                 21 00 00 00 00 00 00 00 80 13 06 16 6A 13 07 1F  // !...........j...
                 31 13 08 22 E1 7A 24 C0 13 09 23 F1 49 9F F4 49  // 1..".z$...#.I..I
                 9F F4 BF 13 0A 17 13 0B 20 15 27 00 00 16 16 20  // ........ .'.... 
                 80 00 00 00 18 73 06 00 00 0A 13 0C 12 0D 20 D4  // .....s........ .
                 FE FF FF 7D 01 00 00 04 12 0D 7B 01 00 00 04 13  // ...}......{.....
                 0E 7E 02 00 00 04 13 0F 16 13 10 73 05 00 00 0A  // .~.........s....
                 13 11 73 0F 00 00 06 13 12 72 01 00 00 70 13 13  // ..s......r...p..
                 18 8D 0C 00 00 01 13 3A 11 3A 16 17 9E 11 3A 17  // .......:.:....:.
                 18 9E 11 3A 13 14 18 18 73 07 00 00 0A 25 D0 07  // ...:....s....%..
                 00 00 04 28 08 00 00 0A 13 15 19 17 19 73 09 00  // ...(.........s..
                 00 0A 25 D0 08 00 00 04 28 08 00 00 0A 13 16 73  // ..%.....(......s
                 0B 00 00 06 13 17 11 17 6F 0A 00 00 06 13 18 73  // ........o......s
                 0B 00 00 06 13 19 11 19 11 19 FE 06 09 00 00 06
                 73 02 00 00 06 6F 07 00 00 06 02 13 1A 20 C0 63  // s....o....... .c
                 FF FF 13 1B 20 40 9C 00 00 13 1C 20 18 FC FF FF  // .... @..... ....
                 13 1D 20 E8 03 00 00 13 1E 1F 14 13 1F 1F EC 13  // .. .............
                 20 21 FF FF FF FF FF FF FF 7F 13 21 15 6A 13 22  //  !.........!.j."
                 1F 41 13 23 22 E1 7A 24 40 13 24 23 F1 49 9F F4  // .A.#".z$@.$#.I..
                 49 9F F4 3F 13 25 16 13 26 20 76 06 00 00 16 16  // I..?.%..& v.....
                 16 19 73 06 00 00 0A 13 27 12 28 20 2C 01 00 00  // ..s.....'.( ,...
                 7D 01 00 00 04 12 28 7B 01 00 00 04 13 29 7E 02  // }.....({.....)~.
                 00 00 04 13 2A 17 13 2B 11 2B 8C 03 00 00 02 28  // ....*..+.+.....(
                 0A 00 00 0A 73 05 00 00 0A 13 2C 73 0F 00 00 06  // ....s.....,s....
                 13 2D 72 19 00 00 70 13 2E 18 8D 0C 00 00 01 13  // .-r...p.........
                 3B 11 3B 16 19 9E 11 3B 17 1A 9E 11 3B 13 2F 11  // ;.;....;....;./.
                 2F 16 94 13 30 18 18 73 07 00 00 0A 25 D0 09 00  // /...0..s....%...
                 00 04 28 08 00 00 0A 13 31 11 31 16 17 28 0B 00  // ..(.....1.1..(..
                 00 0A 13 32 19 17 19 73 09 00 00 0A 25 D0 0A 00  // ...2...s....%...
                 00 04 28 08 00 00 0A 13 33 11 33 16 16 17 28 0C  // ..(.....3.3...(.
                 00 00 0A 13 34 73 0B 00 00 06 13 35 11 35 6F 0A  // ....4s.....5.5o.
                 00 00 06 13 36 73 0B 00 00 06 13 37 11 37 11 37  // ....6s.....7.7.7
                 FE 06 09 00 00 06 73 02 00 00 06 6F 07 00 00 06  // ......s....o....
                 02 13 38 17 13 39 11 39 18 5A 18 33 2A 11 39 13  // ..8..9.9.Z.3*.9.
                 3C 11 3C 45 03 00 00 00 02 00 00 00 04 00 00 00  // <.<E............
                 0C 00 00 00 2B 0F 2B 0D 11 39 18 58 13 39 2B 05  // ....+.+..9.X.9+.
                 19 13 39 2B F1 2B 06 11 39 1B 59 13 39 DE 03 26  // ..9+.+..9.Y.9..&
                 DE 00 2A 00 41 1C 00 00 00 00 00 00 0D 01 00 00  // ..*.A...........
                 62 01 00 00 6F 02 00 00 03 00 00 00 04 00 00 01  // b...o...........
                 0B 30 03 00 A3 00 00 00 41 00 00 11 02 0A 03 0B  // .0......A.......
                 04 0C 05 0D 20 18 FC FF FF 13 04 1F 14 13 05 72  // .... ..........r
                 31 00 00 70 13 06 20 A0 15 FF FF 13 07 18 8D 11  // 1..p.. .........
                 00 00 01 13 16 11 16 16 72 39 00 00 70 A2 11 16  // ........r9..p...
                 17 72 3D 00 00 70 A2 11 16 13 08 20 E8 03 00 00  // .r=..p..... ....
                 13 09 18 17 5A FE 0F 13 0A 11 0A 11 09 53 11 0A  // ....Z........S..
                 49 13 0B 02 13 0C 03 13 0D 04 13 0E 05 13 0F 11  // I...............
                 04 13 10 1F 14 13 11 72 31 00 00 70 13 12 20 A0  // .......r1..p.. .
                 15 FF FF 13 13 18 8D 11 00 00 01 13 17 11 17 16
                 72 39 00 00 70 A2 11 17 17 72 3D 00 00 70 A2 11  // r9..p....r=..p..
                 17 13 14 11 14 8E 69 13 15 DE 03 26 DE 00 2A 00  // ......i....&..*.
                 01 10 00 00 00 00 46 00 59 9F 00 03 04 00 00 01  // ......F.Y.......
                 00 00 00 00 00 00 00 00 00 00 00 00 0B 30 04 00  // .............0..
                 4C 00 00 00 5A 00 00 11 17 0A 20 40 9C 00 00 28  // L...Z..... @...(
                 0C 00 00 06 20 FF 00 00 00 20 00 00 00 80 72 41  // .... .... ....rA
                 00 00 70 73 05 00 00 0A 28 0D 00 00 06 DE 03 26  // ..ps....(......&
                 DE 00 06 2C 0F 72 49 00 00 70 28 0D 00 00 0A 1F  // ...,.rI..p(.....
                 64 0B 2B 0E 72 57 00 00 70 28 0D 00 00 0A 17 0B  // d.+.rW..p(......
                 2B 00 07 2A 01 10 00 00 00 00 02 00 25 27 00 03  // +..*........%'..
                 04 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00
                 1E 02 28 05 00 00 0A 2A 00 00 00 00 F1 47 3E 3A  // ..(....*.....G>:
                 00 00 00 00 02 00 00 00 24 03 00 00 1C 25 00 00  // ........$....%..
                 1C 07 00 00 52 53 44 53 CC 34 96 CB 7B 02 7D 48  // ....RSDS.4..{.}H
                 95 35 49 1C D9 21 FD A6 07 00 00 00 74 72 79 33  // .5I..!......try3
                 2E 70 64 62 00 00 00 00 00 00 00 00 00 00 00 00  // .pdb............
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 00 00 42 53 4A 42 01 00 01 00  // ........BSJB....
                 00 00 00 00 0C 00 00 00 76 31 2E 78 38 36 63 68  // ........v1.x86ch
                 6B 00 00 00 00 00 05 00 6C 00 00 00 9C 04 00 00  // k.......l.......
                 23 7E 00 00 08 05 00 00 54 03 00 00 23 53 74 72  // #~......T...#Str
                 69 6E 67 73 00 00 00 00 5C 08 00 00 C0 03 00 00  // ings....\.......
                 23 42 6C 6F 62 00 00 00 1C 0C 00 00 68 00 00 00  // #Blob.......h...
                 23 55 53 00 84 0C 00 00 10 00 00 00 23 47 55 49  // #US.........#GUI
                 44 00 00 00 00 00 00 00 01 00 00 01 57 DF 16 29  // D...........W..)
                 09 02 00 00 00 FA 01 33 00 02 00 00 01 00 00 00  // .......3........
                 12 00 00 00 0C 00 00 00 0A 00 00 00 0F 00 00 00
                 0C 00 00 00 01 00 00 00 0E 00 00 00 02 00 00 00
                 02 00 00 00 01 00 00 00 04 00 00 00 5C 00 00 00  // ............\...
                 01 00 00 00 01 00 00 00 02 00 00 00 02 00 00 00
                 04 00 00 00 01 00 00 00 01 00 00 00 04 00 00 00
                 00 00 4C 00 01 00 00 00 00 00 06 00 65 00 5E 00  // ..L.........e.^.
                 06 00 72 00 5E 00 06 00 7C 00 5E 00 06 00 9E 00  // ..r.^...|.^.....
                 5E 00 06 00 DA 00 5E 00 06 00 E7 00 5E 00 06 00  // ^.....^.....^...
                 4F 01 33 01 06 00 6B 01 33 01 06 00 8D 01 7A 01  // O.3...k.3.....z.
                 06 00 CA 01 5E 00 06 00 E6 01 5E 00 06 00 EE 01  // ....^.....^.....
                 5E 00 06 00 49 02 29 02 06 00 58 02 5E 00 06 00  // ^...I.)...X.^...
                 5E 02 5E 00 06 00 A9 02 5E 00 06 00 23 03 5E 00  // ^.^.....^...#.^.
                 06 00 3A 03 2A 03 00 00 00 00 43 00 00 00 00 00  // ..:.*.....C.....
                 01 00 01 00 08 01 10 00 6F 00 00 00 05 00 01 00  // ........o.......
                 01 00 00 21 00 00 77 00 00 00 09 00 03 00 02 00  // ...!..w.........
                 01 01 00 00 8E 00 00 00 0D 00 06 00 02 00 A1 00
                 00 00 98 00 00 00 00 00 06 00 06 00 01 00 10 00
                 A5 00 00 00 11 00 06 00 07 00 01 00 10 00 AB 00
                 00 00 11 00 07 00 0C 00 00 00 00 00 F4 01 00 00
                 11 00 07 00 10 00 13 01 00 00 01 02 00 00 05 00
                 0B 00 10 00 13 01 00 00 81 02 00 00 05 00 0B 00
                 10 00 13 01 00 00 BB 02 00 00 05 00 0B 00 10 00
                 13 01 00 00 E7 02 00 00 05 00 0B 00 10 00 06 00
                 B2 00 1F 00 16 00 B4 00 1F 00 06 06 BD 00 1F 00
                 56 80 C5 00 26 00 56 80 C9 00 26 00 01 00 10 01  // V...&.V...&.....
                 50 00 13 01 15 02 9B 00 13 01 95 02 B7 00 13 01  // P...............
                 CF 02 C0 00 13 01 FB 02 CA 00 50 20 00 00 00 00  // ..........P ....
                 91 18 B6 00 22 00 01 00 00 00 00 00 03 00 86 18  // ...."...........
                 CD 00 34 00 01 00 00 00 00 00 03 00 C6 00 D3 00  // ..4.............
                 3A 00 03 00 00 00 00 00 03 00 C6 01 F5 00 3E 00  // :.............>.
                 03 00 00 00 00 00 03 00 C6 01 01 01 46 00 05 00  // ............F...
                 00 00 00 00 00 00 C6 05 0B 01 4C 00 06 00 58 20  // ..........L...X 
                 00 00 20 00 86 08 13 01 54 00 06 00 71 20 00 00  // .. .....T...q ..
                 20 00 86 08 1A 01 54 00 07 00 8A 20 00 00 00 00  //  .....T.... ....
                 86 00 24 01 3A 00 08 00 90 20 00 00 00 00 E6 01  // ..$.:.... ......
                 0B 01 4C 00 08 00 A2 20 00 00 00 00 86 18 CD 00  // ..L.... ........
                 3A 00 08 00 1C 21 00 00 00 00 96 00 26 01 5A 00  // :....!......&.Z.
                 08 00 B8 23 00 00 00 00 96 00 2A 01 5F 00 09 00  // ...#......*._...
                 84 24 00 00 00 00 96 00 2E 01 67 00 0D 00 F8 24  // .$........g....$
                 00 00 00 00 86 18 CD 00 3A 00 0D 00 00 00 01 00  // ........:.......
                 A6 01 00 00 02 00 AD 01 00 00 01 00 B4 01 00 00
                 02 00 A6 01 00 00 01 00 BD 01 00 00 01 00 C4 01
                 00 00 01 00 C4 01 00 00 01 00 E2 01 00 00 01 00
                 0F 03 00 00 02 00 14 03 00 00 03 00 19 03 00 00
                 04 00 1E 03 06 00 14 00 39 00 CD 00 6B 00 49 00  // ........9...k.I.
                 CD 00 71 00 51 00 D3 01 77 00 51 00 DB 01 77 00  // ..q.Q...w.Q...w.
                 21 00 CD 00 3A 00 59 00 CD 00 84 00 0C 00 CD 00  // !...:.Y.........
                 95 00 69 00 71 02 9F 00 14 00 CD 00 B0 00 81 00  // ..i.q...........
                 B1 02 BB 00 0C 00 E3 02 C4 00 14 00 E3 02 CE 00
                 81 00 B1 02 B5 01 91 00 CD 00 3A 00 08 00 10 00  // ..........:.....
                 2A 00 08 00 14 00 2F 00 27 00 73 00 2F 00 2E 00  // *...../.'.s./...
                 13 00 BF 01 08 00 06 00 C6 01 01 00 10 00 00 00
                 09 00 01 00 24 00 00 00 0A 00 01 00 10 00 00 00  // ....$...........
                 0B 00 01 00 24 00 00 00 0C 00 80 00 1F 00 D5 00  // ....$...........
                 3F 01 3F 01 1F 00 1F 00 43 01 46 01 49 01 4C 01  // ?.?.....C.F.I.L.
                 4F 01 52 01 55 01 58 01 5B 01 5E 01 61 01 64 01  // O.R.U.X.[.^.a.d.
                 68 01 1F 00 1F 00 26 00 6C 01 6F 01 73 01 3F 01  // h.....&.l.o.s.?.
                 76 01 7F 01 89 01 1F 00 89 01 1F 00 1F 00 43 01  // v.............C.
                 46 01 49 01 4C 01 4F 01 52 01 55 01 58 01 5B 01  // F.I.L.O.R.U.X.[.
                 5E 01 61 01 64 01 68 01 1F 00 1F 00 26 00 6C 01  // ^.a.d.h.....&.l.
                 6F 01 73 01 3F 01 1F 00 76 01 1F 00 7F 01 1F 00  // o.s.?...v.......
                 89 01 1F 00 89 01 1F 00 1F 00 8D 01 AD 01 AD 01
                 49 01 1F 00 73 01 6C 01 46 01 4C 01 73 01 1F 00  // I...s.l.F.L.s...
                 AD 01 49 01 B1 01 49 01 49 01 1F 00 73 01 6C 01  // ..I...I.I...s.l.
                 46 01 4C 01 73 01 1F 00 AD 01 1F 00 BA 01 1F 00  // F.L.s...........
                 61 01 06 00 01 00 00 00 10 01 10 00 08 00 07 00  // a...............
                 02 00 10 00 08 00 02 00 8D 00 A7 00 B0 20 00 00  // ............. ..
                 07 00 C0 20 00 00 08 00 E8 20 00 00 09 00 F8 20  // ... ..... ..... 
                 00 00 0A 00 04 80 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 00 00 A1 01 00 00 01 00 00 00 6B 09  // ..............k.
                 00 00 00 00 00 00 01 00 55 00 00 00 0A 00 09 00  // ........U.......
                 08 00 0A 00 08 00 0B 00 08 00 0C 00 08 00 00 00
                 00 56 65 72 73 69 6F 6E 20 6F 66 20 72 75 6E 74  // .Version of runt
                 69 6D 65 20 61 67 61 69 6E 73 74 20 77 68 69 63  // ime against whic
                 68 20 74 68 65 20 62 69 6E 61 72 79 20 69 73 20  // h the binary is 
                 62 75 69 6C 74 20 3A 20 31 2E 30 2E 32 34 31 35  // built : 1.0.2415
                 2E 30 00 3C 4D 6F 64 75 6C 65 3E 00 74 72 79 33  // .0.<Module>.try3
                 2E 65 78 65 00 6D 73 63 6F 72 6C 69 62 00 53 79  // .exe.mscorlib.Sy
                 73 74 65 6D 00 56 61 6C 75 65 54 79 70 65 00 56  // stem.ValueType.V
                 54 00 45 6E 75 6D 00 44 61 79 73 00 4D 75 6C 74  // T.Enum.Days.Mult
                 69 63 61 73 74 44 65 6C 65 67 61 74 65 00 64 65  // icastDelegate.de
                 6C 65 67 61 74 65 31 00 49 54 65 73 74 00 4F 62  // legate1.ITest.Ob
                 6A 65 63 74 00 43 54 65 73 74 00 63 61 74 63 68  // ject.CTest.catch
                 33 00 6D 00 6E 00 2E 63 63 74 6F 72 00 76 61 6C  // 3.m.n..cctor.val
                 75 65 5F 5F 00 53 75 6E 00 4D 6F 6E 00 2E 63 74  // ue__.Sun.Mon..ct
                 6F 72 00 49 6E 76 6F 6B 65 00 49 41 73 79 6E 63  // or.Invoke.IAsync
                 52 65 73 75 6C 74 00 41 73 79 6E 63 43 61 6C 6C  // Result.AsyncCall
                 62 61 63 6B 00 42 65 67 69 6E 49 6E 76 6F 6B 65  // back.BeginInvoke
                 00 45 6E 64 49 6E 76 6F 6B 65 00 74 65 73 74 00  // .EndInvoke.test.
                 64 67 00 61 64 64 5F 64 67 00 72 65 6D 6F 76 65  // dg.add_dg.remove
                 5F 64 67 00 66 00 66 32 35 00 66 32 36 00 4D 61  // _dg.f.f25.f26.Ma
                 69 6E 00 53 79 73 74 65 6D 2E 53 65 63 75 72 69  // in.System.Securi
                 74 79 2E 50 65 72 6D 69 73 73 69 6F 6E 73 00 53  // ty.Permissions.S
                 65 63 75 72 69 74 79 50 65 72 6D 69 73 73 69 6F  // ecurityPermissio
                 6E 41 74 74 72 69 62 75 74 65 00 53 65 63 75 72  // nAttribute.Secur
                 69 74 79 41 63 74 69 6F 6E 00 53 79 73 74 65 6D  // ityAction.System
                 2E 44 69 61 67 6E 6F 73 74 69 63 73 00 44 65 62  // .Diagnostics.Deb
                 75 67 67 61 62 6C 65 41 74 74 72 69 62 75 74 65  // uggableAttribute
                 00 74 72 79 33 00 6F 62 6A 65 63 74 00 6D 65 74  // .try3.object.met
                 68 6F 64 00 63 61 6C 6C 62 61 63 6B 00 72 65 73  // hod.callback.res
                 75 6C 74 00 76 61 6C 75 65 00 44 65 6C 65 67 61  // ult.value.Delega
                 74 65 00 43 6F 6D 62 69 6E 65 00 52 65 6D 6F 76  // te.Combine.Remov
                 65 00 61 72 67 00 44 65 63 69 6D 61 6C 00 49 6E  // e.arg.Decimal.In
                 74 33 32 00 3C 47 6C 6F 62 61 6C 4E 61 6D 65 3E  // t32.<GlobalName>
                 00 24 24 73 74 72 75 63 74 30 78 36 30 30 30 30  // .$$struct0x60000
                 30 63 2D 31 00 24 24 6D 65 74 68 6F 64 30 78 36  // 0c-1.$$method0x6
                 30 30 30 30 30 63 2D 31 00 53 79 73 74 65 6D 2E  // 00000c-1.System.
                 52 75 6E 74 69 6D 65 2E 43 6F 6D 70 69 6C 65 72  // Runtime.Compiler
                 53 65 72 76 69 63 65 73 00 52 75 6E 74 69 6D 65  // Services.Runtime
                 48 65 6C 70 65 72 73 00 41 72 72 61 79 00 52 75  // Helpers.Array.Ru
                 6E 74 69 6D 65 46 69 65 6C 64 48 61 6E 64 6C 65  // ntimeFieldHandle
                 00 49 6E 69 74 69 61 6C 69 7A 65 41 72 72 61 79  // .InitializeArray
                 00 24 24 73 74 72 75 63 74 30 78 36 30 30 30 30  // .$$struct0x60000
                 30 63 2D 32 00 24 24 6D 65 74 68 6F 64 30 78 36  // 0c-2.$$method0x6
                 30 30 30 30 30 63 2D 32 00 43 6F 6E 73 6F 6C 65  // 00000c-2.Console
                 00 57 72 69 74 65 4C 69 6E 65 00 24 24 73 74 72  // .WriteLine.$$str
                 75 63 74 30 78 36 30 30 30 30 30 63 2D 33 00 24  // uct0x600000c-3.$
                 24 6D 65 74 68 6F 64 30 78 36 30 30 30 30 30 63  // $method0x600000c
                 2D 33 00 47 65 74 00 24 24 73 74 72 75 63 74 30  // -3.Get.$$struct0
                 78 36 30 30 30 30 30 63 2D 34 00 24 24 6D 65 74  // x600000c-4.$$met
                 68 6F 64 30 78 36 30 30 30 30 30 63 2D 34 00 61  // hod0x600000c-4.a
                 72 67 31 00 61 72 67 32 00 61 72 67 33 00 61 72  // rg1.arg2.arg3.ar
                 67 34 00 53 74 72 69 6E 67 00 53 79 73 74 65 6D  // g4.String.System
                 2E 53 65 63 75 72 69 74 79 00 55 6E 76 65 72 69  // .Security.Unveri
                 66 69 61 62 6C 65 43 6F 64 65 41 74 74 72 69 62  // fiableCodeAttrib
                 75 74 65 00 00 08 B7 7A 5C 56 19 34 E0 89 14 37  // ute....z\V.4...7
                 FA 5D 33 C8 54 C6 0E DC 49 CF D9 83 91 36 9C 9C  // .]3.T...I....6..
                 D8 9C 2B 02 06 08 03 00 00 01 03 06 11 0C 04 00  // ..+.............
                 00 00 00 04 01 00 00 00 05 20 02 01 1C 19 03 20  // ......... ..... 
                 00 01 07 20 02 12 15 12 19 1C 05 20 01 01 12 15  // ... ....... ....
                 03 20 00 08 03 06 12 10 05 20 01 01 12 10 04 00  // . ....... ......
                 01 01 08 07 00 04 01 07 08 0E 1C 03 00 00 08 05
                 20 01 01 11 21 05 20 02 01 02 02 08 00 02 12 29  //  ...!. ........)
                 12 29 12 29 03 07 01 08 08 20 05 01 08 08 08 02  // .).)..... ......
                 05 07 14 08 02 00 02 00 00 05 20 02 01 08 08 03  // .......... .....
                 06 11 24 07 00 02 01 12 39 11 3D 08 14 08 03 00  // ..$.....9.=.....
                 03 00 00 00 06 20 03 01 08 08 08 03 06 11 28 04  // ..... ........(.
                 00 01 01 1C 03 06 11 2C 05 20 02 08 08 08 03 06  // .......,. ......
                 11 30 06 20 03 08 08 08 08 69 07 3D 08 09 06 07  // .0. .....i.=....
                 05 04 0A 0B 03 0C 0D 02 11 2D 11 08 08 08 11 0C  // .........-......
                 1C 12 1C 0E 1D 08 14 08 02 00 02 00 00 14 08 03
                 00 03 00 00 00 12 18 08 12 18 08 08 09 06 07 05
                 04 0A 0B 03 0C 0D 02 11 2D 11 08 08 08 11 0C 1C  // ........-.......
                 12 1C 0E 1D 08 08 14 08 02 00 02 00 00 08 14 08
                 03 00 03 00 00 00 08 12 18 08 12 18 08 08 1D 08
                 1D 08 08 03 06 1D 08 02 06 09 02 06 06 02 06 07
                 02 06 05 02 06 04 02 06 0A 02 06 0B 02 06 03 02
                 06 0C 02 06 0D 02 06 02 03 06 11 2D 03 06 11 08  // ...........-....
                 02 06 1C 03 06 12 1C 02 06 0E 08 06 14 08 02 00
                 02 00 00 09 06 14 08 03 00 03 00 00 00 03 06 12
                 18 1F 07 18 07 08 0E 1C 06 05 0E 08 1D 0E 07 0F
                 07 07 07 08 0E 1C 06 05 0E 08 1D 0E 08 1D 0E 1D
                 0E 03 06 1D 0E 03 06 0F 07 04 00 01 01 0E 04 07
                 02 02 08 06 01 00 01 01 00 00 81 F6 00 01 00 00
                 00 FF FF FF FF 01 00 00 00 00 00 00 00 02 01 00
                 00 00 1D 53 79 73 74 65 6D 2E 53 65 63 75 72 69  // ...System.Securi
                 74 79 2E 50 65 72 6D 69 73 73 69 6F 6E 53 65 74  // ty.PermissionSet
                 04 00 00 00 0C 72 65 61 64 61 62 6C 65 6F 6E 6C  // .....readableonl
                 79 0E 6D 5F 55 6E 72 65 73 74 72 69 63 74 65 64  // y.m_Unrestricted
                 15 6D 5F 75 6E 72 65 73 74 72 69 63 74 65 64 50  // .m_unrestrictedP
                 65 72 6D 53 65 74 0F 6D 5F 6E 6F 72 6D 61 6C 50  // ermSet.m_normalP
                 65 72 6D 53 65 74 01 00 09 02 00 00 00 09 03 00  // ermSet..........
                 00 00 02 02 00 00 00 22 53 79 73 74 65 6D 2E 53  // ......."System.S
                 65 63 75 72 69 74 79 2E 55 74 69 6C 2E 54 6F 6B  // ecurity.Util.Tok
                 65 6E 42 61 73 65 64 53 65 74 05 00 00 00 08 6D  // enBasedSet.....m
                 5F 6F 62 6A 53 65 74 06 6D 5F 63 45 6C 74 0A 6D  // _objSet.m_cElt.m
                 5F 69 6E 69 74 53 69 7A 65 0B 6D 5F 69 6E 63 72  // _initSize.m_incr
                 65 6D 65 6E 74 0A 6D 5F 6D 61 78 49 6E 64 65 78  // ement.m_maxIndex
                 09 04 00 00 00 01 00 00 00 10 00 00 00 08 00 00
                 00 06 00 00 00 01 03 00 00 00 02 00 00 00 09 05
                 00 00 00 00 00 00 00 08 00 00 00 08 00 00 00 FF
                 FF FF FF 07 04 00 00 00 00 01 00 00 00 10 00 00
                 00 03 0D 53 79 73 74 65 6D 2E 4F 62 6A 65 63 74  // ...System.Object
                 0A 0A 0A 0A 0A 0A 09 06 00 00 00 0A 0A 0A 0A 0A
                 0A 0A 0A 0A 07 05 00 00 00 00 01 00 00 00 08 00
                 00 00 03 0D 53 79 73 74 65 6D 2E 4F 62 6A 65 63  // ....System.Objec
                 74 0A 0A 0A 0A 0A 0A 0A 0A 02 06 00 00 00 2E 53  // t..............S
                 79 73 74 65 6D 2E 53 65 63 75 72 69 74 79 2E 50  // ystem.Security.P
                 65 72 6D 69 73 73 69 6F 6E 73 2E 53 65 63 75 72  // ermissions.Secur
                 69 74 79 50 65 72 6D 69 73 73 69 6F 6E 01 00 00  // ityPermission...
                 00 07 6D 5F 66 6C 61 67 73 02 FF FF FF FF 32 53  // ..m_flags.....2S
                 79 73 74 65 6D 2E 53 65 63 75 72 69 74 79 2E 50  // ystem.Security.P
                 65 72 6D 69 73 73 69 6F 6E 73 2E 53 65 63 75 72  // ermissions.Secur
                 69 74 79 50 65 72 6D 69 73 73 69 6F 6E 46 6C 61  // ityPermissionFla
                 67 01 00 00 00 07 76 61 6C 75 65 5F 5F 04 00 00  // g.....value__...
                 00 0B 00 00 00 17 74 00 65 00 73 00 74 00 20 00  // ......t.e.s.t. .
                 73 00 74 00 72 00 69 00 6E 00 67 00 00 17 73 00  // s.t.r.i.n.g...s.
                 74 00 72 00 69 00 6E 00 67 00 20 00 74 00 65 00  // t.r.i.n.g. .t.e.
                 73 00 74 00 00 07 66 00 32 00 38 00 00 03 61 00  // s.t...f.2.8...a.
                 00 03 62 00 00 07 66 00 32 00 36 00 00 0D 50 00  // ..b...f.2.6...P.
                 41 00 53 00 53 00 45 00 44 00 00 0D 46 00 41 00  // A.S.S.E.D...F.A.
                 49 00 4C 00 45 00 44 00 00 00 00 00 FE BC CB B1  // I.L.E.D.........
                 FE 18 EE 44 8E D3 12 C9 30 BF 40 AA FC 34 00 00  // ...D....0.@..4..
                 00 00 00 00 00 00 00 00 1E 35 00 00 00 20 00 00  // .........5... ..
                 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                 00 00 00 00 10 35 00 00 00 00 00 00 00 00 00 00  // .....5..........
                 00 00 00 00 00 00 00 00 00 00 5F 43 6F 72 45 78  // .........._CorEx
                 65 4D 61 69 6E 00 6D 73 63 6F 72 65 65 2E 64 6C  // eMain.mscoree.dl
                 6C 00 00 00 00 00 FF 25 00 20 40 00)             // l......%. @.
//*********** DISASSEMBLY COMPLETE ***********************
