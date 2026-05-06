// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ldobj_U2.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_ldobj_U2_ldobj_il
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .field public static unsigned int16 m_v
    .field public unsigned int16 m_vv
    .method public hidebysig static void 
            Func(unsigned int16 v) cil managed
    {
      .maxstack  8
      IL_0000:  ret
    }
    
.method private hidebysig static unsigned int16 method_0_0(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_0_1(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.UInt16
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_0_2(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.UInt16
stsfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_0_3(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.UInt16
call void JitTest_ldobj_U2_ldobj_il.Test::Func(unsigned int16)
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_0_4(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.UInt16
box [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_0_5(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
ldloca.s V
ldloca.s V
ldobj [mscorlib]System.UInt16
stobj [mscorlib]System.UInt16
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_0_6(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.UInt16
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_0_7(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldloca.s V
ldobj [mscorlib]System.UInt16
stfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_0_8(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_0_9(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_0_10(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_0_11(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_1_0(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldsflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldobj [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_1_1(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldsflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldobj [mscorlib]System.UInt16
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_1_2(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldsflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldobj [mscorlib]System.UInt16
stsfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_1_3(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldsflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldobj [mscorlib]System.UInt16
call void JitTest_ldobj_U2_ldobj_il.Test::Func(unsigned int16)
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_1_4(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldsflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldobj [mscorlib]System.UInt16
box [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_1_5(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
ldloca.s V
ldsflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldobj [mscorlib]System.UInt16
stobj [mscorlib]System.UInt16
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_1_6(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldsflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldobj [mscorlib]System.UInt16
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_1_7(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldsflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldobj [mscorlib]System.UInt16
stfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_1_8(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldsflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_1_9(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldsflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_1_10(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldsflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_1_11(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldsflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_2_0(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_2_1(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.UInt16
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_2_2(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.UInt16
stsfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_2_3(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.UInt16
call void JitTest_ldobj_U2_ldobj_il.Test::Func(unsigned int16)
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_2_4(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.UInt16
box [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_2_5(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
ldloca.s V
ldarga.s ARG
ldobj [mscorlib]System.UInt16
stobj [mscorlib]System.UInt16
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_2_6(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.UInt16
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_2_7(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldarga.s ARG
ldobj [mscorlib]System.UInt16
stfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_2_8(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_2_9(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_2_10(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_2_11(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_3_0(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_3_1(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_3_2(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
stsfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_3_3(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
call void JitTest_ldobj_U2_ldobj_il.Test::Func(unsigned int16)
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_3_4(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
box [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_3_5(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
ldloca.s V
newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
stobj [mscorlib]System.UInt16
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_3_6(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_3_7(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
stfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_3_8(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_3_9(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_3_10(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_3_11(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_4_0(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.UInt16
ldc.i4.1
ldelema [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_4_1(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.UInt16
ldc.i4.1
ldelema [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_4_2(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.UInt16
ldc.i4.1
ldelema [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
stsfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_4_3(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.UInt16
ldc.i4.1
ldelema [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
call void JitTest_ldobj_U2_ldobj_il.Test::Func(unsigned int16)
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_4_4(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.UInt16
ldc.i4.1
ldelema [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
box [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_4_5(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
ldloca.s V
ldc.i4.2
newarr [mscorlib]System.UInt16
ldc.i4.1
ldelema [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
stobj [mscorlib]System.UInt16
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_4_6(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.UInt16
ldc.i4.1
ldelema [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_4_7(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldc.i4.2
newarr [mscorlib]System.UInt16
ldc.i4.1
ldelema [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
stfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_4_8(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.UInt16
ldc.i4.1
ldelema [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_4_9(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.UInt16
ldc.i4.1
ldelema [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_4_10(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.UInt16
ldc.i4.1
ldelema [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_4_11(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.UInt16
ldc.i4.1
ldelema [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_5_0(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_5_1(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_5_2(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
stsfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_5_3(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
call void JitTest_ldobj_U2_ldobj_il.Test::Func(unsigned int16)
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_5_4(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
box [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_5_5(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
ldloca.s V
newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
stobj [mscorlib]System.UInt16
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_5_6(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_5_7(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
stfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_5_8(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_5_9(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_5_10(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_5_11(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldflda unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_6_0(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.UInt16
refanyval [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_6_1(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.UInt16
refanyval [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_6_2(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.UInt16
refanyval [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
stsfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_6_3(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.UInt16
refanyval [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
call void JitTest_ldobj_U2_ldobj_il.Test::Func(unsigned int16)
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_6_4(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.UInt16
refanyval [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
box [mscorlib]System.UInt16
pop
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_6_5(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
ldloca.s V
ldloca.s V
mkrefany [mscorlib]System.UInt16
refanyval [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
stobj [mscorlib]System.UInt16
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_6_6(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.UInt16
refanyval [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_6_7(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_U2_ldobj_il.Test::.ctor()
ldloca.s V
mkrefany [mscorlib]System.UInt16
refanyval [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
stfld unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static unsigned int16 method_6_8(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.UInt16
refanyval [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_6_9(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.UInt16
refanyval [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_6_10(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.UInt16
refanyval [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
ret
}
.method private hidebysig static unsigned int16 method_6_11(unsigned int16 ARG) cil managed
{
.maxstack  8
.locals (unsigned int16 V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.UInt16
refanyval [mscorlib]System.UInt16
ldobj [mscorlib]System.UInt16
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

.locals (unsigned int16 V)
ldc.i4 0
stloc.s V
ldloc.s V
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_0_0(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_0_1(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_0_2(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_0_3(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_0_4(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_0_5(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_0_6(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_0_7(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_0_8(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_0_9(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_0_10(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_0_11(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_1_0(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_1_1(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_1_2(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_1_3(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_1_4(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_1_5(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_1_6(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_1_7(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_1_8(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_1_9(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_1_10(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_1_11(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_2_0(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_2_1(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_2_2(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_2_3(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_2_4(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_2_5(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_2_6(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_2_7(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_2_8(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_2_9(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_2_10(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_2_11(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_3_0(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_3_1(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_3_2(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_3_3(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_3_4(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_3_5(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_3_6(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_3_7(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_3_8(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_3_9(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_3_10(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_3_11(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_4_0(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_4_1(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_4_2(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_4_3(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_4_4(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_4_5(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_4_6(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_4_7(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_4_8(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_4_9(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_4_10(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_4_11(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_5_0(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_5_1(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_5_2(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_5_3(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_5_4(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_5_5(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_5_6(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_5_7(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_5_8(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_5_9(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_5_10(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_5_11(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_6_0(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_6_1(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_6_2(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_6_3(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_6_4(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_6_5(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_6_6(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_6_7(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_6_8(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_6_9(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_6_10(unsigned int16)
call unsigned int16 JitTest_ldobj_U2_ldobj_il.Test::method_6_11(unsigned int16)
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
