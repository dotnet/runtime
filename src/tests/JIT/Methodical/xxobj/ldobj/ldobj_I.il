// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ldobj_I.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ldobj_I { }
.assembly extern xunit.core {}
.namespace JitTest_ldobj_I_ldobj_il
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .field public static native int m_v
    .field public native int m_vv
    .method public hidebysig static void 
            Func(native int v) cil managed
    {
      .maxstack  8
      IL_0000:  ret
    }
    
.method private hidebysig static native int method_0_0(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_0_1(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.IntPtr
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static native int method_0_2(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.IntPtr
stsfld native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static native int method_0_3(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.IntPtr
call void JitTest_ldobj_I_ldobj_il.Test::Func(native int)
ldloc.s V
ret
}
.method private hidebysig static native int method_0_4(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.IntPtr
box [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_0_5(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
ldloca.s V
ldloca.s V
ldobj [mscorlib]System.IntPtr
stobj [mscorlib]System.IntPtr
ldloc.s V
ret
}
.method private hidebysig static native int method_0_6(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.IntPtr
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static native int method_0_7(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldloca.s V
ldobj [mscorlib]System.IntPtr
stfld native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static native int method_0_8(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_0_9(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_0_10(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_0_11(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_1_0(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldsflda native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldobj [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_1_1(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldsflda native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldobj [mscorlib]System.IntPtr
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static native int method_1_2(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldsflda native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldobj [mscorlib]System.IntPtr
stsfld native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static native int method_1_3(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldsflda native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldobj [mscorlib]System.IntPtr
call void JitTest_ldobj_I_ldobj_il.Test::Func(native int)
ldloc.s V
ret
}
.method private hidebysig static native int method_1_4(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldsflda native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldobj [mscorlib]System.IntPtr
box [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_1_5(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
ldloca.s V
ldsflda native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldobj [mscorlib]System.IntPtr
stobj [mscorlib]System.IntPtr
ldloc.s V
ret
}
.method private hidebysig static native int method_1_6(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldsflda native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldobj [mscorlib]System.IntPtr
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static native int method_1_7(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldsflda native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldobj [mscorlib]System.IntPtr
stfld native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static native int method_1_8(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldsflda native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_1_9(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldsflda native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_1_10(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldsflda native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_1_11(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldsflda native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_2_0(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_2_1(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.IntPtr
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static native int method_2_2(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.IntPtr
stsfld native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static native int method_2_3(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.IntPtr
call void JitTest_ldobj_I_ldobj_il.Test::Func(native int)
ldloc.s V
ret
}
.method private hidebysig static native int method_2_4(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.IntPtr
box [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_2_5(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
ldloca.s V
ldarga.s ARG
ldobj [mscorlib]System.IntPtr
stobj [mscorlib]System.IntPtr
ldloc.s V
ret
}
.method private hidebysig static native int method_2_6(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.IntPtr
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static native int method_2_7(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldarga.s ARG
ldobj [mscorlib]System.IntPtr
stfld native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static native int method_2_8(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_2_9(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_2_10(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_2_11(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldarga.s ARG
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_3_0(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_3_1(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static native int method_3_2(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
stsfld native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static native int method_3_3(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
call void JitTest_ldobj_I_ldobj_il.Test::Func(native int)
ldloc.s V
ret
}
.method private hidebysig static native int method_3_4(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
box [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_3_5(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
ldloca.s V
newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
stobj [mscorlib]System.IntPtr
ldloc.s V
ret
}
.method private hidebysig static native int method_3_6(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static native int method_3_7(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
stfld native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static native int method_3_8(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_3_9(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_3_10(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_3_11(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_4_0(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.IntPtr
ldc.i4.1
ldelema [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_4_1(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.IntPtr
ldc.i4.1
ldelema [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static native int method_4_2(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.IntPtr
ldc.i4.1
ldelema [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
stsfld native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static native int method_4_3(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.IntPtr
ldc.i4.1
ldelema [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
call void JitTest_ldobj_I_ldobj_il.Test::Func(native int)
ldloc.s V
ret
}
.method private hidebysig static native int method_4_4(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.IntPtr
ldc.i4.1
ldelema [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
box [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_4_5(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
ldloca.s V
ldc.i4.2
newarr [mscorlib]System.IntPtr
ldc.i4.1
ldelema [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
stobj [mscorlib]System.IntPtr
ldloc.s V
ret
}
.method private hidebysig static native int method_4_6(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.IntPtr
ldc.i4.1
ldelema [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static native int method_4_7(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldc.i4.2
newarr [mscorlib]System.IntPtr
ldc.i4.1
ldelema [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
stfld native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static native int method_4_8(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.IntPtr
ldc.i4.1
ldelema [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_4_9(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.IntPtr
ldc.i4.1
ldelema [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_4_10(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.IntPtr
ldc.i4.1
ldelema [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_4_11(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldc.i4.2
newarr [mscorlib]System.IntPtr
ldc.i4.1
ldelema [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_5_0(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_5_1(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static native int method_5_2(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
stsfld native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static native int method_5_3(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
call void JitTest_ldobj_I_ldobj_il.Test::Func(native int)
ldloc.s V
ret
}
.method private hidebysig static native int method_5_4(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
box [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_5_5(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
ldloca.s V
newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
stobj [mscorlib]System.IntPtr
ldloc.s V
ret
}
.method private hidebysig static native int method_5_6(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static native int method_5_7(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
stfld native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static native int method_5_8(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_5_9(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_5_10(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_5_11(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldflda native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_6_0(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.IntPtr
refanyval [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_6_1(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.IntPtr
refanyval [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
stloc.s V
ldloc.s V
ret
}
.method private hidebysig static native int method_6_2(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.IntPtr
refanyval [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
stsfld native int JitTest_ldobj_I_ldobj_il.Test::m_v
ldloc.s V
ret
}
.method private hidebysig static native int method_6_3(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.IntPtr
refanyval [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
call void JitTest_ldobj_I_ldobj_il.Test::Func(native int)
ldloc.s V
ret
}
.method private hidebysig static native int method_6_4(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.IntPtr
refanyval [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
box [mscorlib]System.IntPtr
pop
ldloc.s V
ret
}
.method private hidebysig static native int method_6_5(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
ldloca.s V
ldloca.s V
mkrefany [mscorlib]System.IntPtr
refanyval [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
stobj [mscorlib]System.IntPtr
ldloc.s V
ret
}
.method private hidebysig static native int method_6_6(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.IntPtr
refanyval [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
starg.s ARG
ldloc.s V
ret
}
.method private hidebysig static native int method_6_7(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V
newobj instance void JitTest_ldobj_I_ldobj_il.Test::.ctor()
ldloca.s V
mkrefany [mscorlib]System.IntPtr
refanyval [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
stfld native int JitTest_ldobj_I_ldobj_il.Test::m_vv
ldloc.s V
ret
}
.method private hidebysig static native int method_6_8(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.IntPtr
refanyval [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_6_9(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.IntPtr
refanyval [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_6_10(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.IntPtr
refanyval [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
ret
}
.method private hidebysig static native int method_6_11(native int ARG) cil managed
{
.maxstack  8
.locals (native int V)
ldc.i4 0
stloc.s V

ldloca.s V
mkrefany [mscorlib]System.IntPtr
refanyval [mscorlib]System.IntPtr
ldobj [mscorlib]System.IntPtr
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

.locals (native int V)
ldc.i4 0
stloc.s V
ldloc.s V
call native int JitTest_ldobj_I_ldobj_il.Test::method_0_0(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_0_1(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_0_2(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_0_3(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_0_4(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_0_5(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_0_6(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_0_7(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_0_8(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_0_9(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_0_10(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_0_11(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_1_0(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_1_1(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_1_2(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_1_3(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_1_4(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_1_5(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_1_6(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_1_7(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_1_8(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_1_9(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_1_10(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_1_11(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_2_0(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_2_1(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_2_2(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_2_3(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_2_4(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_2_5(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_2_6(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_2_7(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_2_8(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_2_9(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_2_10(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_2_11(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_3_0(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_3_1(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_3_2(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_3_3(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_3_4(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_3_5(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_3_6(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_3_7(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_3_8(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_3_9(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_3_10(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_3_11(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_4_0(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_4_1(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_4_2(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_4_3(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_4_4(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_4_5(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_4_6(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_4_7(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_4_8(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_4_9(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_4_10(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_4_11(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_5_0(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_5_1(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_5_2(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_5_3(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_5_4(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_5_5(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_5_6(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_5_7(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_5_8(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_5_9(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_5_10(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_5_11(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_6_0(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_6_1(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_6_2(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_6_3(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_6_4(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_6_5(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_6_6(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_6_7(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_6_8(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_6_9(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_6_10(native int)
call native int JitTest_ldobj_I_ldobj_il.Test::method_6_11(native int)
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
