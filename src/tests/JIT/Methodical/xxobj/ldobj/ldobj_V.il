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
.namespace JitTest
{
  .class public sequential ansi sealed beforefieldinit ValueClass
         extends [mscorlib]System.ValueType
  {
    .field public int32[] arr
  }
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .field public static valuetype JitTest.ValueClass m_v
    .field public valuetype JitTest.ValueClass m_vv
    .method public hidebysig static void 
            Func(valuetype JitTest.ValueClass v) cil managed
    {
      .maxstack  8
      IL_0000:  ret
    }
	.method private hidebysig static valuetype JitTest.ValueClass method_0_0(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	ldobj JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_0_1(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	ldobj JitTest.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_0_2(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	ldobj JitTest.ValueClass
	stsfld valuetype JitTest.ValueClass JitTest.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_0_3(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	ldobj JitTest.ValueClass
	call void JitTest.Test::Func(valuetype JitTest.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_0_4(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	ldobj JitTest.ValueClass
	box JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_0_5(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	ldloca.s V
	ldloca.s V
	ldobj JitTest.ValueClass
	stobj JitTest.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_0_6(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	ldobj JitTest.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_0_7(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	newobj instance void JitTest.Test::.ctor()
	ldloca.s V
	ldobj JitTest.ValueClass
	stfld valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_0_8(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	ldobj JitTest.ValueClass
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_1_0(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldsflda valuetype JitTest.ValueClass JitTest.Test::m_v
	ldobj JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_1_1(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldsflda valuetype JitTest.ValueClass JitTest.Test::m_v
	ldobj JitTest.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_1_2(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldsflda valuetype JitTest.ValueClass JitTest.Test::m_v
	ldobj JitTest.ValueClass
	stsfld valuetype JitTest.ValueClass JitTest.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_1_3(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldsflda valuetype JitTest.ValueClass JitTest.Test::m_v
	ldobj JitTest.ValueClass
	call void JitTest.Test::Func(valuetype JitTest.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_1_4(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldsflda valuetype JitTest.ValueClass JitTest.Test::m_v
	ldobj JitTest.ValueClass
	box JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_1_5(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	ldloca.s V
	ldsflda valuetype JitTest.ValueClass JitTest.Test::m_v
	ldobj JitTest.ValueClass
	stobj JitTest.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_1_6(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldsflda valuetype JitTest.ValueClass JitTest.Test::m_v
	ldobj JitTest.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_1_7(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	newobj instance void JitTest.Test::.ctor()
	ldsflda valuetype JitTest.ValueClass JitTest.Test::m_v
	ldobj JitTest.ValueClass
	stfld valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_1_8(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldsflda valuetype JitTest.ValueClass JitTest.Test::m_v
	ldobj JitTest.ValueClass
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_2_0(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldarga.s ARG
	ldobj JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_2_1(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldarga.s ARG
	ldobj JitTest.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_2_2(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldarga.s ARG
	ldobj JitTest.ValueClass
	stsfld valuetype JitTest.ValueClass JitTest.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_2_3(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldarga.s ARG
	ldobj JitTest.ValueClass
	call void JitTest.Test::Func(valuetype JitTest.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_2_4(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldarga.s ARG
	ldobj JitTest.ValueClass
	box JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_2_5(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	ldloca.s V
	ldarga.s ARG
	ldobj JitTest.ValueClass
	stobj JitTest.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_2_6(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldarga.s ARG
	ldobj JitTest.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_2_7(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	newobj instance void JitTest.Test::.ctor()
	ldarga.s ARG
	ldobj JitTest.ValueClass
	stfld valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_2_8(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldarga.s ARG
	ldobj JitTest.ValueClass
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_3_0(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_3_1(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_3_2(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	stsfld valuetype JitTest.ValueClass JitTest.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_3_3(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	call void JitTest.Test::Func(valuetype JitTest.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_3_4(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	box JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_3_5(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	ldloca.s V
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	stobj JitTest.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_3_6(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_3_7(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	newobj instance void JitTest.Test::.ctor()
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	stfld valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_3_8(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_4_0(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldc.i4.2
	newarr JitTest.ValueClass
	ldc.i4.1
	ldelema JitTest.ValueClass
	ldobj JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_4_1(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldc.i4.2
	newarr JitTest.ValueClass
	ldc.i4.1
	ldelema JitTest.ValueClass
	ldobj JitTest.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_4_2(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldc.i4.2
	newarr JitTest.ValueClass
	ldc.i4.1
	ldelema JitTest.ValueClass
	ldobj JitTest.ValueClass
	stsfld valuetype JitTest.ValueClass JitTest.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_4_3(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldc.i4.2
	newarr JitTest.ValueClass
	ldc.i4.1
	ldelema JitTest.ValueClass
	ldobj JitTest.ValueClass
	call void JitTest.Test::Func(valuetype JitTest.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_4_4(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldc.i4.2
	newarr JitTest.ValueClass
	ldc.i4.1
	ldelema JitTest.ValueClass
	ldobj JitTest.ValueClass
	box JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_4_5(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	ldloca.s V
	ldc.i4.2
	newarr JitTest.ValueClass
	ldc.i4.1
	ldelema JitTest.ValueClass
	ldobj JitTest.ValueClass
	stobj JitTest.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_4_6(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldc.i4.2
	newarr JitTest.ValueClass
	ldc.i4.1
	ldelema JitTest.ValueClass
	ldobj JitTest.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_4_7(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	newobj instance void JitTest.Test::.ctor()
	ldc.i4.2
	newarr JitTest.ValueClass
	ldc.i4.1
	ldelema JitTest.ValueClass
	ldobj JitTest.ValueClass
	stfld valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_4_8(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldc.i4.2
	newarr JitTest.ValueClass
	ldc.i4.1
	ldelema JitTest.ValueClass
	ldobj JitTest.ValueClass
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_5_0(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_5_1(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_5_2(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	stsfld valuetype JitTest.ValueClass JitTest.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_5_3(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	call void JitTest.Test::Func(valuetype JitTest.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_5_4(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	box JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_5_5(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	ldloca.s V
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	stobj JitTest.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_5_6(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_5_7(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	newobj instance void JitTest.Test::.ctor()
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	stfld valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_5_8(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	newobj instance void JitTest.Test::.ctor()
	ldflda valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldobj JitTest.ValueClass
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_6_0(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	mkrefany JitTest.ValueClass
	refanyval JitTest.ValueClass
	ldobj JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_6_1(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	mkrefany JitTest.ValueClass
	refanyval JitTest.ValueClass
	ldobj JitTest.ValueClass
	stloc.s V
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_6_2(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	mkrefany JitTest.ValueClass
	refanyval JitTest.ValueClass
	ldobj JitTest.ValueClass
	stsfld valuetype JitTest.ValueClass JitTest.Test::m_v
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_6_3(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	mkrefany JitTest.ValueClass
	refanyval JitTest.ValueClass
	ldobj JitTest.ValueClass
	call void JitTest.Test::Func(valuetype JitTest.ValueClass)
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_6_4(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	mkrefany JitTest.ValueClass
	refanyval JitTest.ValueClass
	ldobj JitTest.ValueClass
	box JitTest.ValueClass
	pop
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_6_5(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	ldloca.s V
	ldloca.s V
	mkrefany JitTest.ValueClass
	refanyval JitTest.ValueClass
	ldobj JitTest.ValueClass
	stobj JitTest.ValueClass
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_6_6(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	mkrefany JitTest.ValueClass
	refanyval JitTest.ValueClass
	ldobj JitTest.ValueClass
	starg.s ARG
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_6_7(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	newobj instance void JitTest.Test::.ctor()
	ldloca.s V
	mkrefany JitTest.ValueClass
	refanyval JitTest.ValueClass
	ldobj JitTest.ValueClass
	stfld valuetype JitTest.ValueClass JitTest.Test::m_vv
	ldloc.s V
	ret
	}
	.method private hidebysig static valuetype JitTest.ValueClass method_6_8(valuetype JitTest.ValueClass ARG) cil managed
	{
	.maxstack  8
	.locals (valuetype JitTest.ValueClass V)
	ldloca.s V
	initobj JitTest.ValueClass
	
	ldloca.s V
	mkrefany JitTest.ValueClass
	refanyval JitTest.ValueClass
	ldobj JitTest.ValueClass
	ret
	}
    .method public hidebysig static int32
            Main() cil managed
    {
      .entrypoint
      .maxstack  1
		.locals (valuetype JitTest.ValueClass V)
		ldloca.s V
		initobj JitTest.ValueClass
		ldloc.s V
		call valuetype JitTest.ValueClass JitTest.Test::method_0_0(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_0_1(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_0_2(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_0_3(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_0_4(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_0_5(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_0_6(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_0_7(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_0_8(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_1_0(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_1_1(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_1_2(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_1_3(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_1_4(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_1_5(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_1_6(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_1_7(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_1_8(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_2_0(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_2_1(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_2_2(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_2_3(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_2_4(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_2_5(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_2_6(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_2_7(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_2_8(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_3_0(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_3_1(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_3_2(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_3_3(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_3_4(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_3_5(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_3_6(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_3_7(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_3_8(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_4_0(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_4_1(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_4_2(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_4_3(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_4_4(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_4_5(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_4_6(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_4_7(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_4_8(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_5_0(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_5_1(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_5_2(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_5_3(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_5_4(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_5_5(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_5_6(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_5_7(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_5_8(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_6_0(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_6_1(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_6_2(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_6_3(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_6_4(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_6_5(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_6_6(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_6_7(valuetype JitTest.ValueClass)
		call valuetype JitTest.ValueClass JitTest.Test::method_6_8(valuetype JitTest.ValueClass)
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
