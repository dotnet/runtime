// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_compat_gcval_nested
{
  .class public sequential ansi sealed beforefieldinit LargeVT
         extends [mscorlib]System.ValueType
  {
    .field private int32[] 'value'
    .field private valuetype JitTest_compat_gcval_nested.LargeVT[] next
    
    .method private hidebysig static value class JitTest_compat_gcval_nested.LargeVT 
            callee(int32 retcode) il managed
    {
      // Code size       33 (0x21)
      .maxstack  8
      .locals (value class JitTest_compat_gcval_nested.LargeVT lv)
      ldloca lv
      initobj JitTest_compat_gcval_nested.LargeVT
      ldloca lv
      
      ldc.i4.1
      newarr value class JitTest_compat_gcval_nested.LargeVT
      dup
      ldc.i4.0
      ldelema value class JitTest_compat_gcval_nested.LargeVT
      dup
      initobj    JitTest_compat_gcval_nested.LargeVT
      ldc.i4.1
      newarr     [mscorlib]System.Int32
      dup
      ldc.i4.0
      ldarg.0
      stelem.i4
      stfld      int32[] JitTest_compat_gcval_nested.LargeVT::'value'

      stfld      valuetype JitTest_compat_gcval_nested.LargeVT[] JitTest_compat_gcval_nested.LargeVT::next
      ldloc.0
      ret
    } // end of method LargeVT::callee

    .method private hidebysig static value class JitTest_compat_gcval_nested.LargeVT 
            caller(int32 retcode) il managed
    {
		// Code size       11 (0xb)
		.maxstack  8
		ldarg.0
		tail. call value class JitTest_compat_gcval_nested.LargeVT JitTest_compat_gcval_nested.LargeVT::callee(int32)
		ret
    } // end of method LargeVT::caller

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       46 (0x2e)
      .maxstack  2
      .locals (int32 V_0,
               int32 V_1,
               value class JitTest_compat_gcval_nested.LargeVT V_2)
      ldc.i4.s   100
      call       value class JitTest_compat_gcval_nested.LargeVT JitTest_compat_gcval_nested.LargeVT::caller(int32)
      stloc.2
      ldloca.s   V_2
      ldfld      value class JitTest_compat_gcval_nested.LargeVT[] JitTest_compat_gcval_nested.LargeVT::next
      ldc.i4.0
      ldelema value class JitTest_compat_gcval_nested.LargeVT
      ldfld      int32[] JitTest_compat_gcval_nested.LargeVT::'value'
      
      ldc.i4.0
      ldelem.i4
      stloc.0
      ldloc.0
      ldc.i4.s   100
      beq.s      IL_001e

      ldstr      "failed"
      br.s       IL_0023

      IL_001e:  ldstr      "passed"
      IL_0023:  call       void [System.Console]System.Console::WriteLine(class System.String)
      ldloc.0
      stloc.1
      ldloc.1
      ret
    } // end of method LargeVT::Main

  } // end of class LargeVT

} // end of namespace JitTest_compat_gcval_nested

//*********** DISASSEMBLY COMPLETE ***********************
