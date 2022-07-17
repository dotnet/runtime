// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
  .ver 4:0:0:0
}
.assembly initializearray
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    // ILONLY

.class public auto ansi beforefieldinit InitializeArray extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack 8

    call bool InitializeArray::SZArray_Int64_5_ValidInitializationTest()
    brfalse.s fail
    
    call bool InitializeArray::SZArray_Int64_6_InvalidInitializationTest()
    brfalse.s fail
    
    call bool InitializeArray::MDArray_Int16_10_ValidInitializationTest()
    brfalse.s fail
    
    call bool InitializeArray::MDArray_Int16_0_10_ValidInitializationTest()
    brfalse.s fail

    call bool InitializeArray::MDArray_Int16_11_InvalidInitializationTest()
    brfalse.s fail
    
    call bool InitializeArray::MDArray_Int8_8x10_ValidInitializationTest()
    brfalse.s fail
    
    call bool InitializeArray::MDArray_Int8_10x10_InvalidInitializationTest()
    brfalse.s fail
    
    call bool InitializeArray::MDArray_Float32_4x5_ValidInitializationTest()
    brfalse.s fail
    
    call bool InitializeArray::MDArray_Float32_5x5_InvalidInitializationTest()
    brfalse.s fail
    
    call bool InitializeArray::MDArray_Int8_2_4x3_5_ValidInitializationTest()
    brfalse.s fail

    call bool InitializeArray::MDArray_Int8_2_5x3_5_InvalidInitializationTest()
    brfalse.s fail

    ldc.i4.s 100
    ret
    
  fail:
    ldc.i4.s 0
    ret
  }

  .method private hidebysig static bool SZArray_Int64_5_ValidInitializationTest() cil managed noinlining
  {
    .maxstack 8
    call int64[] InitializeArray::SZArray_Initialize_Int64_5()
    call int64[] InitializeArray::SZArray_Initialize_Int64_5_Optimized()
    call bool InitializeArray::ArrayEquals(class [mscorlib]System.Array, class [mscorlib]System.Array)
    ret
  }
  
  .method private hidebysig static int64[] SZArray_Initialize_Int64_5() cil managed noinlining nooptimization
  {
    .maxstack 8
    ldc.i4 5
    newarr int64
    dup
    ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize40' 'ArrayInitializers'::'InitializerField40'
    call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
    ret
  }

  .method private hidebysig static int64[] SZArray_Initialize_Int64_5_Optimized() cil managed noinlining
  {
    .maxstack 8
    ldc.i4 5
    newarr int64
    dup
    ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize40' 'ArrayInitializers'::'InitializerField40'
    call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
    ret
  }

  .method private hidebysig static bool SZArray_Int64_6_InvalidInitializationTest() cil managed noinlining
  {
    .maxstack 8
    .locals init (int32 exitCode)
    .try
    {
      ldc.i4 11
      newarr int64
      dup
      ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize20' 'ArrayInitializers'::'InitializerField20'
      call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
      pop
      ldc.i4 0
      stloc.0
      leave exit
    }
    catch [mscorlib]System.ArgumentException
    {
      pop
      ldc.i4 1
      stloc.0
      leave exit
    }
    
  exit:
    ldloc.0
    ret
  }

  .method private hidebysig static bool MDArray_Int16_10_ValidInitializationTest() cil managed noinlining
  {
    .maxstack 8
    call int16[0...] InitializeArray::MDArray_Initialize_Int16_10()
    call int16[0...] InitializeArray::MDArray_Initialize_Int16_10_Optimized()
    call bool InitializeArray::ArrayEquals(class [mscorlib]System.Array, class [mscorlib]System.Array)
    ret
  }
  
  .method private hidebysig static int16[0...] MDArray_Initialize_Int16_10() cil managed noinlining nooptimization
  {
    .maxstack 8
    ldc.i4 10
    newobj instance void int16[0...]::.ctor(int32)
    dup
    ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize20' 'ArrayInitializers'::'InitializerField20'
    call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
    ret
  }

  .method private hidebysig static int16[0...] MDArray_Initialize_Int16_10_Optimized() cil managed noinlining
  {
    .maxstack 8
    ldc.i4 10
    newobj instance void int16[0...]::.ctor(int32)
    dup
    ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize20' 'ArrayInitializers'::'InitializerField20'
    call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
    ret
  }

  .method private hidebysig static bool MDArray_Int16_11_InvalidInitializationTest() cil managed noinlining
  {
    .maxstack 8
    .locals init (int32 exitCode)
    .try
    {
      ldc.i4 11
      newobj instance void int16[0...]::.ctor(int32)
      dup
      ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize20' 'ArrayInitializers'::'InitializerField20'
      call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
      pop
      ldc.i4 0
      stloc.0
      leave exit
    }
    catch [mscorlib]System.ArgumentException
    {
      pop
      ldc.i4 1
      stloc.0
      leave exit
    }
    
  exit:
    ldloc.0
    ret
  }

  .method private hidebysig static bool MDArray_Int16_0_10_ValidInitializationTest() cil managed noinlining
  {
    .maxstack 8
    call int16[...] InitializeArray::MDArray_Initialize_Int16_0_10()
    call int16[...] InitializeArray::MDArray_Initialize_Int16_0_10_Optimized()
    call bool InitializeArray::ArrayEquals(class [mscorlib]System.Array, class [mscorlib]System.Array)
    ret
  }
  
  .method private hidebysig static int16[...] MDArray_Initialize_Int16_0_10() cil managed noinlining nooptimization
  {
    .maxstack 8
    ldc.i4 0
    ldc.i4 10
    newobj instance void int16[...]::.ctor(int32, int32)
    dup
    ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize20' 'ArrayInitializers'::'InitializerField20'
    call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
    ret
  }

  .method private hidebysig static int16[...] MDArray_Initialize_Int16_0_10_Optimized() cil managed noinlining
  {
    .maxstack 8
    ldc.i4 0
    ldc.i4 10
    newobj instance void int16[...]::.ctor(int32, int32)
    dup
    ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize20' 'ArrayInitializers'::'InitializerField20'
    call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
    ret
  }


  .method private hidebysig static bool MDArray_Int8_8x10_ValidInitializationTest() cil managed noinlining
  {
    .maxstack 8
    call int8[0..., 0...] InitializeArray::MDArray_Initialize_Int8_8x10()
    call int8[0..., 0...] InitializeArray::MDArray_Initialize_Int8_8x10_Optimized()
    call bool InitializeArray::ArrayEquals(class [mscorlib]System.Array, class [mscorlib]System.Array)
    ret
  }
  
  .method private hidebysig static int8[0..., 0...] MDArray_Initialize_Int8_8x10() cil managed noinlining nooptimization
  {
    .maxstack 8
    ldc.i4 8
    ldc.i4 10
    newobj instance void int8 [0..., 0...]::.ctor(int32, int32)
    dup
    ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize80' 'ArrayInitializers'::'InitializerField80'
    call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
    ret
  }

  .method private hidebysig static int8[0..., 0...] MDArray_Initialize_Int8_8x10_Optimized() cil managed noinlining
  {
    .maxstack 8
    ldc.i4 8
    ldc.i4 10
    newobj instance void int8[0..., 0...]::.ctor(int32, int32)
    dup
    ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize80' 'ArrayInitializers'::'InitializerField80'
    call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
    ret
  }

  .method private hidebysig static bool MDArray_Int8_10x10_InvalidInitializationTest() cil managed noinlining
  {
    .maxstack 8
    .locals init (int32 exitCode)
    .try
    {
      ldc.i4 10
      ldc.i4 10
      newobj instance void int8[0..., 0...]::.ctor(int32, int32)
      dup
      ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize80' 'ArrayInitializers'::'InitializerField80'
      call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
      pop
      ldc.i4 0
      stloc.0
      leave exit
    }
    catch [mscorlib]System.ArgumentException
    {
      pop
      ldc.i4 1
      stloc.0
      leave exit
    }
    
  exit:
    ldloc.0
    ret
  }
  
  .method private hidebysig static bool MDArray_Float32_4x5_ValidInitializationTest() cil managed noinlining
  {
    .maxstack 8
    call float32[0..., 0...] InitializeArray::MDArray_Initialize_Float32_4x5()
    call float32[0..., 0...] InitializeArray::MDArray_Initialize_Float32_4x5_Optimized()
    call bool InitializeArray::ArrayEquals(class [mscorlib]System.Array, class [mscorlib]System.Array)
    ret
  }
  
  .method private hidebysig static float32[0..., 0...] MDArray_Initialize_Float32_4x5_Optimized() cil managed noinlining
  {
    .maxstack 8
    ldc.i4.4
    ldc.i4.5
    newobj instance void float32[0..., 0...]::.ctor(int32, int32)
    dup
    ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize80' 'ArrayInitializers'::'InitializerField80'
    call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
    ret
  }

  .method private hidebysig static float32[0..., 0...] MDArray_Initialize_Float32_4x5() cil managed noinlining nooptimization
  {
    .maxstack 8
    ldc.i4 4
    ldc.i4 5
    newobj instance void float32[0..., 0...]::.ctor(int32, int32)
    dup
    ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize80' 'ArrayInitializers'::'InitializerField80'
    call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
    ret
  }

  .method private hidebysig static bool MDArray_Float32_5x5_InvalidInitializationTest() cil managed noinlining
  {
    .maxstack 8
    .locals init (int32 exitCode)
    .try
    {
      ldc.i4 5
      ldc.i4 5
      newobj instance void float32[0..., 0...]::.ctor(int32, int32)
      dup
      ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize80' 'ArrayInitializers'::'InitializerField80'
      call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
      pop
      ldc.i4 0
      stloc.0
      leave exit
    }
    catch [mscorlib]System.ArgumentException
    {
      pop
      ldc.i4 1
      stloc.0
      leave exit
    }
    
  exit:
    ldloc.0
    ret
  }

  .method private hidebysig static bool MDArray_Int8_2_4x3_5_ValidInitializationTest() cil managed noinlining
  {
    .maxstack 8
    call int8[..., ...] InitializeArray::MDArray_Initialize_Int8_2_4x3_5()
    call int8[..., ...] InitializeArray::MDArray_Initialize_Int8_2_4x3_5_Optimized()
    call bool InitializeArray::ArrayEquals(class [mscorlib]System.Array, class [mscorlib]System.Array)
    ret
  }
  
  .method private hidebysig static int8[..., ...] MDArray_Initialize_Int8_2_4x3_5() cil managed noinlining nooptimization
  {
    .maxstack 8
    ldc.i4.2
    ldc.i4.4
    ldc.i4.3
    ldc.i4.5
    newobj instance void int8 [..., ...]::.ctor(int32, int32, int32, int32)
    dup
    ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize20' 'ArrayInitializers'::'InitializerField20'
    call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
    ret
  }

  .method private hidebysig static int8[..., ...] MDArray_Initialize_Int8_2_4x3_5_Optimized() cil managed noinlining
  {
    .maxstack 8
    ldc.i4.2
    ldc.i4.4
    ldc.i4.3
    ldc.i4.5
    newobj instance void int8[..., ...]::.ctor(int32, int32, int32, int32)
    dup
    ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize20' 'ArrayInitializers'::'InitializerField20'
    call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
    ret
  }

  .method private hidebysig static bool MDArray_Int8_2_5x3_5_InvalidInitializationTest() cil managed noinlining
  {
    .maxstack 8
    .locals init (int32 exitCode)
    .try
    {
      ldc.i4.2
      ldc.i4.5
      ldc.i4.3
      ldc.i4.5
      newobj instance void int8[..., ...]::.ctor(int32, int32, int32, int32)
      dup
      ldtoken field valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize20' 'ArrayInitializers'::'InitializerField20'
      call void[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype[mscorlib] System.RuntimeFieldHandle)
      pop
      ldc.i4 0
      stloc.0
      leave exit
    }
    catch [mscorlib]System.ArgumentException
    {
      pop
      ldc.i4 1
      stloc.0
      leave exit
    }
    
  exit:
    ldloc.0
    ret
  }

//
// ArrayEquals was generated from the following C# code:
//
//static bool ArrayEquals(Array x, Array y)
//{
//    if (x.Rank != y.Rank)
//        return false;

//    var lower = new int[x.Rank];
//    var indices = new int[x.Rank];
//    var upper = new int[x.Rank];

//    for (int i = 0; i < indices.Length; i++)
//    {
//        if (x.GetLowerBound(i) != y.GetLowerBound(i))
//            return false;
//        if (x.GetLength(i) != y.GetLength(i))
//            return false;
//        lower[i] = x.GetLowerBound(i);
//        indices[i] = lower[i];
//        upper[i] = lower[i] + x.GetLength(i);
//    }

//    while (true)
//    {
//        if (!object.Equals(x.GetValue(indices), y.GetValue(indices)))
//            return false;

//        int i = 0;
//        indices[i]++;

//        while (indices[i] >= upper[i])
//        {
//            indices[i] = lower[i];
//            i++;

//            if (i >= indices.Length)
//                return true;

//            indices[i]++;
//        }
//    }
//}
  
  .method private hidebysig static bool ArrayEquals(class [mscorlib]System.Array x, class [mscorlib]System.Array y) cil managed
  {
      .maxstack 5
      .locals init (
          [0] int32[] lower,
          [1] int32[] indices,
          [2] int32[] upper,
          [3] int32 i,
          [4] int32 V_4)
      L_0000: ldarg.0 
      L_0001: callvirt instance int32 [mscorlib]System.Array::get_Rank()
      L_0006: ldarg.1 
      L_0007: callvirt instance int32 [mscorlib]System.Array::get_Rank()
      L_000c: beq.s L_0010
      L_000e: ldc.i4.0 
      L_000f: ret 
      L_0010: ldarg.0 
      L_0011: callvirt instance int32 [mscorlib]System.Array::get_Rank()
      L_0016: newarr int32
      L_001b: stloc.0 
      L_001c: ldarg.0 
      L_001d: callvirt instance int32 [mscorlib]System.Array::get_Rank()
      L_0022: newarr int32
      L_0027: stloc.1 
      L_0028: ldarg.0 
      L_0029: callvirt instance int32 [mscorlib]System.Array::get_Rank()
      L_002e: newarr int32
      L_0033: stloc.2 
      L_0034: ldc.i4.0 
      L_0035: stloc.3 
      L_0036: br.s L_007e
      L_0038: ldarg.0 
      L_0039: ldloc.3 
      L_003a: callvirt instance int32 [mscorlib]System.Array::GetLowerBound(int32)
      L_003f: ldarg.1 
      L_0040: ldloc.3 
      L_0041: callvirt instance int32 [mscorlib]System.Array::GetLowerBound(int32)
      L_0046: beq.s L_004a
      L_0048: ldc.i4.0 
      L_0049: ret 
      L_004a: ldarg.0 
      L_004b: ldloc.3 
      L_004c: callvirt instance int32 [mscorlib]System.Array::GetLength(int32)
      L_0051: ldarg.1 
      L_0052: ldloc.3 
      L_0053: callvirt instance int32 [mscorlib]System.Array::GetLength(int32)
      L_0058: beq.s L_005c
      L_005a: ldc.i4.0 
      L_005b: ret 
      L_005c: ldloc.0 
      L_005d: ldloc.3 
      L_005e: ldarg.0 
      L_005f: ldloc.3 
      L_0060: callvirt instance int32 [mscorlib]System.Array::GetLowerBound(int32)
      L_0065: stelem.i4 
      L_0066: ldloc.1 
      L_0067: ldloc.3 
      L_0068: ldloc.0 
      L_0069: ldloc.3 
      L_006a: ldelem.i4 
      L_006b: stelem.i4 
      L_006c: ldloc.2 
      L_006d: ldloc.3 
      L_006e: ldloc.0 
      L_006f: ldloc.3 
      L_0070: ldelem.i4 
      L_0071: ldarg.0 
      L_0072: ldloc.3 
      L_0073: callvirt instance int32 [mscorlib]System.Array::GetLength(int32)
      L_0078: add 
      L_0079: stelem.i4 
      L_007a: ldloc.3 
      L_007b: ldc.i4.1 
      L_007c: add 
      L_007d: stloc.3 
      L_007e: ldloc.3 
      L_007f: ldloc.1 
      L_0080: ldlen 
      L_0081: conv.i4 
      L_0082: blt.s L_0038
      L_0084: ldarg.0 
      L_0085: ldloc.1 
      L_0086: callvirt instance object [mscorlib]System.Array::GetValue(int32[])
      L_008b: ldarg.1 
      L_008c: ldloc.1 
      L_008d: callvirt instance object [mscorlib]System.Array::GetValue(int32[])
      L_0092: call bool [mscorlib]System.Object::Equals(object, object)
      L_0097: brtrue.s L_009b
      L_0099: ldc.i4.0 
      L_009a: ret 
      L_009b: ldc.i4.0 
      L_009c: stloc.s V_4
      L_009e: ldloc.1 
      L_009f: ldloc.s V_4
      L_00a1: ldelema int32
      L_00a6: dup 
      L_00a7: ldind.i4 
      L_00a8: ldc.i4.1 
      L_00a9: add 
      L_00aa: stind.i4 
      L_00ab: br.s L_00d1
      L_00ad: ldloc.1 
      L_00ae: ldloc.s V_4
      L_00b0: ldloc.0 
      L_00b1: ldloc.s V_4
      L_00b3: ldelem.i4 
      L_00b4: stelem.i4 
      L_00b5: ldloc.s V_4
      L_00b7: ldc.i4.1 
      L_00b8: add 
      L_00b9: stloc.s V_4
      L_00bb: ldloc.s V_4
      L_00bd: ldloc.1 
      L_00be: ldlen 
      L_00bf: conv.i4 
      L_00c0: blt.s L_00c4
      L_00c2: ldc.i4.1 
      L_00c3: ret 
      L_00c4: ldloc.1 
      L_00c5: ldloc.s V_4
      L_00c7: ldelema int32
      L_00cc: dup 
      L_00cd: ldind.i4 
      L_00ce: ldc.i4.1 
      L_00cf: add 
      L_00d0: stind.i4 
      L_00d1: ldloc.1 
      L_00d2: ldloc.s V_4
      L_00d4: ldelem.i4 
      L_00d5: ldloc.2 
      L_00d6: ldloc.s V_4
      L_00d8: ldelem.i4 
      L_00d9: bge.s L_00ad
      L_00db: br.s L_0084
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class private auto ansi 'ArrayInitializers' extends [mscorlib]System.Object
{
  .class explicit ansi sealed nested private 'StaticArrayInitTypeSize20' extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 20
  }

  .class explicit ansi sealed nested private 'StaticArrayInitTypeSize40' extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 40
  }
  
  .class explicit ansi sealed nested private 'StaticArrayInitTypeSize80' extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 80
  }
  
  .field static assembly valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize20' 'InitializerField20' at InitializerField20Data
  .field static assembly valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize40' 'InitializerField40' at InitializerField40Data
  .field static assembly valuetype 'ArrayInitializers'/'StaticArrayInitTypeSize80' 'InitializerField80' at InitializerField80Data
}

.data cil InitializerField20Data = bytearray (01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 11 12 13 14)

.data cil InitializerField40Data = bytearray (01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 11 12 13 14
                                              15 16 17 18 19 1A 1B 1C 1D 1E 1F 20 21 22 23 24 25 26 27 28)

.data cil InitializerField80Data = bytearray (01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 11 12 13 14
                                              15 16 17 18 19 1A 1B 1C 1D 1E 1F 20 21 22 23 24 25 26 27 28
                                              29 2A 2B 2C 2D 2E 2F 30 31 32 33 34 35 36 37 38 39 3A 3B 3C
                                              3D 3E 3F 40 41 42 43 44 45 46 47 48 49 4A 4B 4C 4D 4E 4F 50)
