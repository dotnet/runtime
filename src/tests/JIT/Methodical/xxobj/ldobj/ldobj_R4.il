// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ldobj_R4.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_ldobj_R4_ldobj_il
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .field public static float32 m_v
    .field public float32 m_vv
    .method public hidebysig static void 
            Func(float32 v) cil managed
    {
      .maxstack  8
      IL_0000:  ret
    }
    
.method private hidebysig static float32 method_0_0(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_0_1(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Single
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static float32 method_0_2(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Single
stsfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static float32 method_0_3(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Single
call void JitTest_ldobj_R4_ldobj_il.Test::Func(float32)
ldloc.s V
ret
}
.method private hidebysig static float32 method_0_4(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Single
box [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_0_5(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
ldloca.s V
ldloca.s V
ldobj [mscorlib]System.Single
stobj [mscorlib]System.Single
ldloc.s V
ret
}
.method private hidebysig static float32 method_0_6(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Single
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static float32 method_0_7(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldloca.s V
ldobj [mscorlib]System.Single
stfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static float32 method_0_8(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_0_9(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_0_10(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_0_11(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_1_0(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldsflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldobj [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_1_1(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldsflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldobj [mscorlib]System.Single
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static float32 method_1_2(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldsflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldobj [mscorlib]System.Single
stsfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static float32 method_1_3(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldsflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldobj [mscorlib]System.Single
call void JitTest_ldobj_R4_ldobj_il.Test::Func(float32)
ldloc.s V
ret
}
.method private hidebysig static float32 method_1_4(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldsflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldobj [mscorlib]System.Single
box [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_1_5(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
ldloca.s V
ldsflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldobj [mscorlib]System.Single
stobj [mscorlib]System.Single
ldloc.s V
ret
}
.method private hidebysig static float32 method_1_6(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldsflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldobj [mscorlib]System.Single
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static float32 method_1_7(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldsflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldobj [mscorlib]System.Single
stfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static float32 method_1_8(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldsflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_1_9(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldsflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_1_10(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldsflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_1_11(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldsflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_2_0(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_2_1(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Single
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static float32 method_2_2(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Single
stsfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static float32 method_2_3(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Single
call void JitTest_ldobj_R4_ldobj_il.Test::Func(float32)
ldloc.s V
ret
}
.method private hidebysig static float32 method_2_4(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Single
box [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_2_5(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
ldloca.s V
ldarga.s ARG
ldobj [mscorlib]System.Single
stobj [mscorlib]System.Single
ldloc.s V
ret
}
.method private hidebysig static float32 method_2_6(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Single
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static float32 method_2_7(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldarga.s ARG
ldobj [mscorlib]System.Single
stfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static float32 method_2_8(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_2_9(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_2_10(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_2_11(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_3_0(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_3_1(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static float32 method_3_2(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
stsfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static float32 method_3_3(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
call void JitTest_ldobj_R4_ldobj_il.Test::Func(float32)
ldloc.s V
ret
}
.method private hidebysig static float32 method_3_4(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
box [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_3_5(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
ldloca.s V
newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
stobj [mscorlib]System.Single
ldloc.s V
ret
}
.method private hidebysig static float32 method_3_6(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static float32 method_3_7(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
stfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static float32 method_3_8(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_3_9(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_3_10(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_3_11(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_4_0(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Single
ldc.i4.1
ldelema [mscorlib]System.Single
ldobj [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_4_1(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Single
ldc.i4.1
ldelema [mscorlib]System.Single
ldobj [mscorlib]System.Single
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static float32 method_4_2(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Single
ldc.i4.1
ldelema [mscorlib]System.Single
ldobj [mscorlib]System.Single
stsfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static float32 method_4_3(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Single
ldc.i4.1
ldelema [mscorlib]System.Single
ldobj [mscorlib]System.Single
call void JitTest_ldobj_R4_ldobj_il.Test::Func(float32)
ldloc.s V
ret
}
.method private hidebysig static float32 method_4_4(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Single
ldc.i4.1
ldelema [mscorlib]System.Single
ldobj [mscorlib]System.Single
box [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_4_5(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
ldloca.s V
ldc.i4.2
newarr [mscorlib]System.Single
ldc.i4.1
ldelema [mscorlib]System.Single
ldobj [mscorlib]System.Single
stobj [mscorlib]System.Single
ldloc.s V
ret
}
.method private hidebysig static float32 method_4_6(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Single
ldc.i4.1
ldelema [mscorlib]System.Single
ldobj [mscorlib]System.Single
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static float32 method_4_7(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldc.i4.2
newarr [mscorlib]System.Single
ldc.i4.1
ldelema [mscorlib]System.Single
ldobj [mscorlib]System.Single
stfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static float32 method_4_8(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Single
ldc.i4.1
ldelema [mscorlib]System.Single
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_4_9(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Single
ldc.i4.1
ldelema [mscorlib]System.Single
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_4_10(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Single
ldc.i4.1
ldelema [mscorlib]System.Single
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_4_11(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Single
ldc.i4.1
ldelema [mscorlib]System.Single
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_5_0(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_5_1(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static float32 method_5_2(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
stsfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static float32 method_5_3(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
call void JitTest_ldobj_R4_ldobj_il.Test::Func(float32)
ldloc.s V
ret
}
.method private hidebysig static float32 method_5_4(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
box [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_5_5(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
ldloca.s V
newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
stobj [mscorlib]System.Single
ldloc.s V
ret
}
.method private hidebysig static float32 method_5_6(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static float32 method_5_7(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
stfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static float32 method_5_8(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_5_9(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_5_10(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_5_11(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldflda float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_6_0(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Single
refanyval [mscorlib]System.Single
ldobj [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_6_1(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Single
refanyval [mscorlib]System.Single
ldobj [mscorlib]System.Single
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static float32 method_6_2(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Single
refanyval [mscorlib]System.Single
ldobj [mscorlib]System.Single
stsfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static float32 method_6_3(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Single
refanyval [mscorlib]System.Single
ldobj [mscorlib]System.Single
call void JitTest_ldobj_R4_ldobj_il.Test::Func(float32)
ldloc.s V
ret
}
.method private hidebysig static float32 method_6_4(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Single
refanyval [mscorlib]System.Single
ldobj [mscorlib]System.Single
box [mscorlib]System.Single
pop
ldloc.s V
ret
}
.method private hidebysig static float32 method_6_5(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
ldloca.s V
ldloca.s V
mkrefany [mscorlib]System.Single
refanyval [mscorlib]System.Single
ldobj [mscorlib]System.Single
stobj [mscorlib]System.Single
ldloc.s V
ret
}
.method private hidebysig static float32 method_6_6(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Single
refanyval [mscorlib]System.Single
ldobj [mscorlib]System.Single
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static float32 method_6_7(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V
newobj instance void JitTest_ldobj_R4_ldobj_il.Test::.ctor()
ldloca.s V
mkrefany [mscorlib]System.Single
refanyval [mscorlib]System.Single
ldobj [mscorlib]System.Single
stfld float32 JitTest_ldobj_R4_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static float32 method_6_8(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Single
refanyval [mscorlib]System.Single
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_6_9(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Single
refanyval [mscorlib]System.Single
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_6_10(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Single
refanyval [mscorlib]System.Single
ldobj [mscorlib]System.Single
ret
}
.method private hidebysig static float32 method_6_11(float32 ARG) cil managed
{
.maxstack  8
.locals (float32 V)
ldc.r4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Single
refanyval [mscorlib]System.Single
ldobj [mscorlib]System.Single
ret
}
    
    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  8

.locals (float32 V)
ldc.r4 0
stloc.s V
ldloc.s V
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_0_0(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_0_1(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_0_2(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_0_3(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_0_4(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_0_5(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_0_6(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_0_7(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_0_8(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_0_9(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_0_10(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_0_11(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_1_0(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_1_1(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_1_2(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_1_3(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_1_4(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_1_5(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_1_6(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_1_7(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_1_8(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_1_9(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_1_10(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_1_11(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_2_0(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_2_1(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_2_2(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_2_3(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_2_4(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_2_5(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_2_6(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_2_7(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_2_8(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_2_9(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_2_10(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_2_11(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_3_0(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_3_1(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_3_2(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_3_3(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_3_4(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_3_5(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_3_6(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_3_7(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_3_8(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_3_9(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_3_10(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_3_11(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_4_0(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_4_1(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_4_2(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_4_3(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_4_4(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_4_5(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_4_6(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_4_7(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_4_8(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_4_9(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_4_10(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_4_11(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_5_0(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_5_1(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_5_2(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_5_3(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_5_4(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_5_5(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_5_6(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_5_7(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_5_8(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_5_9(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_5_10(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_5_11(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_6_0(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_6_1(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_6_2(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_6_3(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_6_4(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_6_5(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_6_6(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_6_7(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_6_8(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_6_9(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_6_10(float32)
call float32 JitTest_ldobj_R4_ldobj_il.Test::method_6_11(float32)
pop

      ldstr      "Passed"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   100
      ret
    }
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    }
  }
}
