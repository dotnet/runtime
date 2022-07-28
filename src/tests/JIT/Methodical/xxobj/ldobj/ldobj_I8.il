// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ldobj_I8.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ldobj_I8 { }
.assembly extern xunit.core {}
.namespace JitTest_ldobj_I8_ldobj_il
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .field public static int64 m_v
    .field public int64 m_vv
    .method public hidebysig static void 
            Func(int64 v) cil managed
    {
      .maxstack  8
      IL_0000:  ret
    }
    
.method private hidebysig static int64 method_0_0(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_0_1(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Int64
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static int64 method_0_2(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Int64
stsfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static int64 method_0_3(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Int64
call void JitTest_ldobj_I8_ldobj_il.Test::Func(int64)
ldloc.s V
ret
}
.method private hidebysig static int64 method_0_4(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Int64
box [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_0_5(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
ldloca.s V
ldloca.s V
ldobj [mscorlib]System.Int64
stobj [mscorlib]System.Int64
ldloc.s V
ret
}
.method private hidebysig static int64 method_0_6(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Int64
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static int64 method_0_7(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldloca.s V
ldobj [mscorlib]System.Int64
stfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static int64 method_0_8(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_0_9(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_0_10(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_0_11(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_1_0(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldsflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldobj [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_1_1(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldsflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldobj [mscorlib]System.Int64
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static int64 method_1_2(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldsflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldobj [mscorlib]System.Int64
stsfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static int64 method_1_3(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldsflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldobj [mscorlib]System.Int64
call void JitTest_ldobj_I8_ldobj_il.Test::Func(int64)
ldloc.s V
ret
}
.method private hidebysig static int64 method_1_4(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldsflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldobj [mscorlib]System.Int64
box [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_1_5(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
ldloca.s V
ldsflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldobj [mscorlib]System.Int64
stobj [mscorlib]System.Int64
ldloc.s V
ret
}
.method private hidebysig static int64 method_1_6(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldsflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldobj [mscorlib]System.Int64
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static int64 method_1_7(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldsflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldobj [mscorlib]System.Int64
stfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static int64 method_1_8(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldsflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_1_9(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldsflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_1_10(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldsflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_1_11(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldsflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_2_0(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_2_1(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Int64
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static int64 method_2_2(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Int64
stsfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static int64 method_2_3(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Int64
call void JitTest_ldobj_I8_ldobj_il.Test::Func(int64)
ldloc.s V
ret
}
.method private hidebysig static int64 method_2_4(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Int64
box [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_2_5(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
ldloca.s V
ldarga.s ARG
ldobj [mscorlib]System.Int64
stobj [mscorlib]System.Int64
ldloc.s V
ret
}
.method private hidebysig static int64 method_2_6(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Int64
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static int64 method_2_7(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldarga.s ARG
ldobj [mscorlib]System.Int64
stfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static int64 method_2_8(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_2_9(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_2_10(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_2_11(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_3_0(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_3_1(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static int64 method_3_2(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
stsfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static int64 method_3_3(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
call void JitTest_ldobj_I8_ldobj_il.Test::Func(int64)
ldloc.s V
ret
}
.method private hidebysig static int64 method_3_4(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
box [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_3_5(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
ldloca.s V
newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
stobj [mscorlib]System.Int64
ldloc.s V
ret
}
.method private hidebysig static int64 method_3_6(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static int64 method_3_7(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
stfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static int64 method_3_8(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_3_9(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_3_10(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_3_11(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_4_0(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Int64
ldc.i4.1
ldelema [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_4_1(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Int64
ldc.i4.1
ldelema [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static int64 method_4_2(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Int64
ldc.i4.1
ldelema [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
stsfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static int64 method_4_3(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Int64
ldc.i4.1
ldelema [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
call void JitTest_ldobj_I8_ldobj_il.Test::Func(int64)
ldloc.s V
ret
}
.method private hidebysig static int64 method_4_4(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Int64
ldc.i4.1
ldelema [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
box [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_4_5(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
ldloca.s V
ldc.i4.2
newarr [mscorlib]System.Int64
ldc.i4.1
ldelema [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
stobj [mscorlib]System.Int64
ldloc.s V
ret
}
.method private hidebysig static int64 method_4_6(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Int64
ldc.i4.1
ldelema [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static int64 method_4_7(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldc.i4.2
newarr [mscorlib]System.Int64
ldc.i4.1
ldelema [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
stfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static int64 method_4_8(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Int64
ldc.i4.1
ldelema [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_4_9(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Int64
ldc.i4.1
ldelema [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_4_10(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Int64
ldc.i4.1
ldelema [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_4_11(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.Int64
ldc.i4.1
ldelema [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_5_0(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_5_1(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static int64 method_5_2(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
stsfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static int64 method_5_3(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
call void JitTest_ldobj_I8_ldobj_il.Test::Func(int64)
ldloc.s V
ret
}
.method private hidebysig static int64 method_5_4(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
box [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_5_5(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
ldloca.s V
newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
stobj [mscorlib]System.Int64
ldloc.s V
ret
}
.method private hidebysig static int64 method_5_6(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static int64 method_5_7(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
stfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static int64 method_5_8(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_5_9(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_5_10(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_5_11(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldflda int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_6_0(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Int64
refanyval [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_6_1(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Int64
refanyval [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static int64 method_6_2(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Int64
refanyval [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
stsfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static int64 method_6_3(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Int64
refanyval [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
call void JitTest_ldobj_I8_ldobj_il.Test::Func(int64)
ldloc.s V
ret
}
.method private hidebysig static int64 method_6_4(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Int64
refanyval [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
box [mscorlib]System.Int64
pop
ldloc.s V
ret
}
.method private hidebysig static int64 method_6_5(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
ldloca.s V
ldloca.s V
mkrefany [mscorlib]System.Int64
refanyval [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
stobj [mscorlib]System.Int64
ldloc.s V
ret
}
.method private hidebysig static int64 method_6_6(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Int64
refanyval [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static int64 method_6_7(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V
newobj instance void JitTest_ldobj_I8_ldobj_il.Test::.ctor()
ldloca.s V
mkrefany [mscorlib]System.Int64
refanyval [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
stfld int64 JitTest_ldobj_I8_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static int64 method_6_8(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Int64
refanyval [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_6_9(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Int64
refanyval [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_6_10(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Int64
refanyval [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
ret
}
.method private hidebysig static int64 method_6_11(int64 ARG) cil managed
{
.maxstack  8
.locals (int64 V)
ldc.i8 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.Int64
refanyval [mscorlib]System.Int64
ldobj [mscorlib]System.Int64
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

.locals (int64 V)
ldc.i8 0
stloc.s V
ldloc.s V
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_0_0(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_0_1(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_0_2(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_0_3(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_0_4(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_0_5(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_0_6(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_0_7(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_0_8(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_0_9(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_0_10(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_0_11(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_1_0(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_1_1(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_1_2(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_1_3(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_1_4(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_1_5(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_1_6(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_1_7(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_1_8(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_1_9(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_1_10(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_1_11(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_2_0(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_2_1(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_2_2(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_2_3(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_2_4(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_2_5(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_2_6(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_2_7(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_2_8(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_2_9(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_2_10(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_2_11(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_3_0(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_3_1(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_3_2(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_3_3(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_3_4(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_3_5(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_3_6(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_3_7(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_3_8(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_3_9(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_3_10(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_3_11(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_4_0(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_4_1(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_4_2(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_4_3(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_4_4(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_4_5(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_4_6(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_4_7(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_4_8(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_4_9(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_4_10(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_4_11(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_5_0(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_5_1(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_5_2(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_5_3(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_5_4(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_5_5(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_5_6(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_5_7(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_5_8(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_5_9(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_5_10(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_5_11(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_6_0(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_6_1(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_6_2(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_6_3(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_6_4(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_6_5(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_6_6(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_6_7(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_6_8(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_6_9(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_6_10(int64)
call int64 JitTest_ldobj_I8_ldobj_il.Test::method_6_11(int64)
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
