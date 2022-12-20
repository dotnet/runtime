// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ldobj_V.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ldobj_V { }
.assembly extern xunit.core {}
.namespace JitTest_ldobj_V_ldobj_il
{
  .class public sequential ansi sealed beforefieldinit ValueClass
         extends [mscorlib]System.ValueType
  {
    .field public int32[] arr
  }
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .field public static valuetype JitTest_ldobj_V_ldobj_il.ValueClass m_v
    .field public valuetype JitTest_ldobj_V_ldobj_il.ValueClass m_vv
    .method public hidebysig static void 
            Func(valuetype JitTest_ldobj_V_ldobj_il.ValueClass v) cil managed
    {
      .maxstack  8
      IL_0000:  ret
    }
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_0_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_0_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_0_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stsfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_0_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	call void JitTest_ldobj_V_ldobj_il.Test::Func(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_0_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	box JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_0_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloca.s V
	ldloca.s V
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_0_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_0_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldloca.s V
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_0_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_1_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldsflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_1_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldsflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_1_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldsflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stsfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_1_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldsflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	call void JitTest_ldobj_V_ldobj_il.Test::Func(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_1_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldsflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	box JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_1_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloca.s V
	ldsflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_1_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldsflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_1_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldsflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_1_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldsflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_2_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldarga.s ARG
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_2_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldarga.s ARG
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_2_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldarga.s ARG
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stsfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_2_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldarga.s ARG
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	call void JitTest_ldobj_V_ldobj_il.Test::Func(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_2_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldarga.s ARG
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	box JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_2_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloca.s V
	ldarga.s ARG
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_2_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldarga.s ARG
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_2_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldarga.s ARG
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_2_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldarga.s ARG
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_3_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_3_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_3_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stsfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_3_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	call void JitTest_ldobj_V_ldobj_il.Test::Func(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_3_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	box JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_3_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloca.s V
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_3_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_3_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_3_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_4_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldc.i4.2
	newarr JitTest_ldobj_V_ldobj_il.ValueClass
	ldc.i4.1
	ldelema JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_4_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldc.i4.2
	newarr JitTest_ldobj_V_ldobj_il.ValueClass
	ldc.i4.1
	ldelema JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_4_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldc.i4.2
	newarr JitTest_ldobj_V_ldobj_il.ValueClass
	ldc.i4.1
	ldelema JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stsfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_4_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldc.i4.2
	newarr JitTest_ldobj_V_ldobj_il.ValueClass
	ldc.i4.1
	ldelema JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	call void JitTest_ldobj_V_ldobj_il.Test::Func(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_4_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldc.i4.2
	newarr JitTest_ldobj_V_ldobj_il.ValueClass
	ldc.i4.1
	ldelema JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	box JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_4_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloca.s V
	ldc.i4.2
	newarr JitTest_ldobj_V_ldobj_il.ValueClass
	ldc.i4.1
	ldelema JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_4_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldc.i4.2
	newarr JitTest_ldobj_V_ldobj_il.ValueClass
	ldc.i4.1
	ldelema JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_4_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldc.i4.2
	newarr JitTest_ldobj_V_ldobj_il.ValueClass
	ldc.i4.1
	ldelema JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_4_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldc.i4.2
	newarr JitTest_ldobj_V_ldobj_il.ValueClass
	ldc.i4.1
	ldelema JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_5_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_5_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_5_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stsfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_5_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	call void JitTest_ldobj_V_ldobj_il.Test::Func(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_5_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	box JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_5_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloca.s V
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_5_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_5_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_5_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldflda valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_6_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	mkrefany JitTest_ldobj_V_ldobj_il.ValueClass
	refanyval JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_6_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	mkrefany JitTest_ldobj_V_ldobj_il.ValueClass
	refanyval JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_6_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	mkrefany JitTest_ldobj_V_ldobj_il.ValueClass
	refanyval JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stsfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_6_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	mkrefany JitTest_ldobj_V_ldobj_il.ValueClass
	refanyval JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	call void JitTest_ldobj_V_ldobj_il.Test::Func(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_6_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	mkrefany JitTest_ldobj_V_ldobj_il.ValueClass
	refanyval JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	box JitTest_ldobj_V_ldobj_il.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_6_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloca.s V
	ldloca.s V
	mkrefany JitTest_ldobj_V_ldobj_il.ValueClass
	refanyval JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stobj JitTest_ldobj_V_ldobj_il.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_6_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	mkrefany JitTest_ldobj_V_ldobj_il.ValueClass
	refanyval JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_6_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	newobj instance void JitTest_ldobj_V_ldobj_il.Test::.ctor()
	ldloca.s V
	mkrefany JitTest_ldobj_V_ldobj_il.ValueClass
	refanyval JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	stfld valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest_ldobj_V_ldobj_il.ValueClass method_6_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
	ldloca.s V
	initobj JitTest_ldobj_V_ldobj_il.ValueClass
	
	ldloca.s V
	mkrefany JitTest_ldobj_V_ldobj_il.ValueClass
	refanyval JitTest_ldobj_V_ldobj_il.ValueClass
	ldobj JitTest_ldobj_V_ldobj_il.ValueClass
	ret
	}
    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  1
		.locals (valuetype JitTest_ldobj_V_ldobj_il.ValueClass V)
		ldloca.s V
		initobj JitTest_ldobj_V_ldobj_il.ValueClass
		ldloc.s V
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_0_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_0_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_0_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_0_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_0_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_0_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_0_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_0_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_0_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_1_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_1_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_1_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_1_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_1_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_1_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_1_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_1_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_1_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_2_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_2_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_2_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_2_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_2_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_2_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_2_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_2_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_2_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_3_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_3_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_3_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_3_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_3_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_3_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_3_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_3_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_3_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_4_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_4_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_4_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_4_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_4_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_4_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_4_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_4_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_4_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_5_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_5_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_5_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_5_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_5_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_5_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_5_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_5_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_5_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_6_0(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_6_1(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_6_2(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_6_3(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_6_4(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_6_5(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_6_6(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_6_7(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
		call valuetype JitTest_ldobj_V_ldobj_il.ValueClass JitTest_ldobj_V_ldobj_il.Test::method_6_8(valuetype JitTest_ldobj_V_ldobj_il.ValueClass)
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
