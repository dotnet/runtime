// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// The test was showing some rare cases of `IND(LCL_FLD_ADDR)` under HW intrinsic nodes.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Runtime.Intrinsics {}
.assembly extern System.Runtime.Extensions {}
.assembly extern System.Numerics.Vectors {}
.assembly extern System.Numerics.Extensions {}
.assembly extern System.Runtime.CompilerServices.Unsafe {}

.assembly Runtime_39424 {}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Runtime_39424
       extends [System.Runtime]System.Object
{
  .class sequential ansi sealed nested private beforefieldinit Container
         extends [System.Runtime]System.ValueType
  {
    .field public valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int32> Vector
    .field public float64 Double
  } // end of class Container

  .method private hidebysig static float64 
          TestLclFldAddrIntrinsicsRound(float64 d) cil managed noinlining
  {
    // Code size       30 (0x1e)
    .maxstack  2
    .locals init (valuetype Runtime_39424/Container V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    Runtime_39424/Container
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldarg.0
    IL_000b:  stfld      float64 Runtime_39424/Container::Double
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldflda     float64 Runtime_39424/Container::Double
    IL_0017:  volatile. ldind.r8
    IL_0018:  call       float64 [System.Runtime.Extensions]System.Math::Round(float64)
    IL_001d:  ret
  } // end of method Runtime_39424::TestLclFldAddrIntrinsicsRound
  
  .class sequential ansi sealed nested private beforefieldinit Vector128Wrapped
         extends [System.Runtime]System.ValueType
  {
    .field public int8 i1
    .field public int8 i2
    .field public int8 i3
    .field public int8 i4
    .field public valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<int32> 'vector'
  } // end of class Vector128Wrapped
  
  
  .class sequential ansi sealed nested private beforefieldinit Vector128WrappedDouble
         extends [System.Runtime]System.ValueType
  {
    .field private bool i1
    .field private bool i2
    .field private bool i3
    .field private bool i4
    .field public valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<float64> 'vector'
  } // end of class Vector128WrappedDouble
  
  .method private hidebysig static float64 
          TestLclFldAddrIntrinsicsSSE41_BlendVariable(float64 d) cil managed noinlining
  {
    // Code size       78 (0x4e)
    .maxstack  3
    .locals init (valuetype Runtime_39424/Vector128Wrapped V_0,
             valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<int32>& V_1,
             valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<int32> V_2)
    IL_0000:  call       bool [System.Runtime.Intrinsics]System.Runtime.Intrinsics.X86.Sse41::get_IsSupported()
    IL_0005:  brfalse.s  IL_0044

    IL_0007:  ldc.i4.1
    IL_0008:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<int32> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128::Create(int32)
    IL_000d:  ldloca.s   V_0
    IL_000f:  initobj    Runtime_39424/Vector128Wrapped
    IL_0015:  ldloca.s   V_0
    IL_0017:  ldc.i4.2
    IL_0018:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<int32> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128::Create(int32)
    IL_001d:  stfld      valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<int32> Runtime_39424/Vector128Wrapped::'vector'
    IL_002a:  ldc.i4.3
    IL_002b:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<int32> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128::Create(int32)
    IL_0030:  stloc.2
    IL_0022:  ldloca.s   V_0
    IL_0024:  ldflda     valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<int32> Runtime_39424/Vector128Wrapped::'vector'
    IL_0032:  volatile. ldobj      valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<int32>
    IL_0037:  ldloc.2
    IL_0038:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<int32> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.X86.Sse41::BlendVariable(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<int32>,
                                                                                                                                                                                         valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<int32>,
                                                                                                                                                                                         valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<int32>)
    IL_003d:  call       !!0 [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128::ToScalar<int32>(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<!!0>)
    IL_0042:  conv.r8
    IL_0043:  ret

    IL_0044:  ldc.r8     100.
    IL_004d:  ret
  } // end of method Runtime_39424::TestLclFldAddrIntrinsicsSSE41_BlendVariable
  
  .method private hidebysig static float64 
          TestLclFldAddrIntrinsicsFMA_MulipluAddScalar() cil managed noinlining
  {
    // Code size       101 (0x65)
    .maxstack  3
    .locals init (valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<float64> V_0,
             valuetype Runtime_39424/Vector128WrappedDouble V_1,
             valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<float64>& V_2)
    IL_0000:  call       bool [System.Runtime.Intrinsics]System.Runtime.Intrinsics.X86.Fma::get_IsSupported()
    IL_0005:  brfalse.s  IL_005b

    IL_0007:  ldc.r8     1.
    IL_0010:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<float64> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128::Create(float64)
    IL_0015:  ldc.r8     3.
    IL_001e:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<float64> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128::Create(float64)
    IL_0023:  stloc.0
    IL_0024:  ldloca.s   V_1
    IL_0026:  initobj    Runtime_39424/Vector128WrappedDouble
    IL_002c:  ldloca.s   V_1
    IL_002e:  ldc.r8     2.
    IL_0037:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<float64> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128::Create(float64)
    IL_003c:  stfld      valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<float64> Runtime_39424/Vector128WrappedDouble::'vector'
    IL_0049:  ldloc.0
    IL_0041:  ldloca.s   V_1
    IL_0043:  ldflda     valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<float64> Runtime_39424/Vector128WrappedDouble::'vector'
    IL_004b:  ldobj      valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<float64>
    IL_0050:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<float64> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.X86.Fma::MultiplyAddScalar(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<float64>,
                                                                                                                                                                                             valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<float64>,
                                                                                                                                                                                             valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<float64>)
    IL_0055:  call       !!0 [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128::ToScalar<float64>(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<!!0>)
    IL_005a:  ret

    IL_005b:  ldc.r8     100.
    IL_0064:  ret
  } // end of method Runtime_39424::TestLclFldAddrIntrinsicsFMA_MulipluAddScalar


  .class sequential ansi sealed nested private beforefieldinit IntsWrapped
         extends [System.Runtime]System.ValueType
  {
    .field public int32 i1
    .field public int32 i2
    .field public int32 i3
    .field public int32 i4
    .field public int32 i5
  } // end of class IntsWrapped

  .method public hidebysig static int32  TestLclFldAddr_BinOp(int32 a) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (valuetype Runtime_39424/IntsWrapped V_0,
             int32& V_1)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    Runtime_39424/IntsWrapped
    IL_0010:  ldarg.0
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldflda     int32 Runtime_39424/IntsWrapped::i4
    IL_0012:  volatile. ldind.i4
    IL_0013:  add
    IL_0014:  ret
  } // end of method Runtime_39424::TestLclFldAddr_BinOp

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldc.r8     1.
    IL_0009:  call       float64 Runtime_39424::TestLclFldAddrIntrinsicsRound(float64)
    IL_000e:  pop
    IL_0010:  ldc.r8     2.
    IL_0011:  call       float64 Runtime_39424::TestLclFldAddrIntrinsicsSSE41_BlendVariable(float64)
    IL_0012:  pop
    IL_0013:  call       float64 Runtime_39424::TestLclFldAddrIntrinsicsFMA_MulipluAddScalar()
    IL_0014:  pop
              ldc.i4.1
              call       int32 Runtime_39424::TestLclFldAddr_BinOp(int32)
              pop
    
    IL_0015:  ldc.i4.s   100
    IL_0016:  ret
  } // end of method Runtime_39424::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Runtime_39424::.ctor

} // end of class Runtime_39424
