// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly gcval_sideeffect { }
.assembly extern xunit.core {}
.namespace JitTest_compat_gcval_sideeffect
{
  .class public sequential ansi sealed beforefieldinit LargeVT
         extends [mscorlib]System.ValueType
  {
    .field private int32[] 'value'
    .method private hidebysig static value class JitTest_compat_gcval_sideeffect.LargeVT 
            callee(int32 retcode) il managed
    {
      // Code size       33 (0x21)
      .maxstack  8
      .locals (value class JitTest_compat_gcval_sideeffect.LargeVT lv)
      ldloca.s   lv
      initobj    JitTest_compat_gcval_sideeffect.LargeVT
      ldloca.s   lv
      ldc.i4.1
      newarr     [mscorlib]System.Int32
      dup
      ldc.i4.0
      ldarg.0
      stelem.i4
      stfld      int32[] JitTest_compat_gcval_sideeffect.LargeVT::'value'
      ldloc.0
      ldloca lv
      ldnull
      stfld      int32[] JitTest_compat_gcval_sideeffect.LargeVT::'value'
      ret
    } // end of method LargeVT::callee

    .method private hidebysig static value class JitTest_compat_gcval_sideeffect.LargeVT 
            caller(int32 retcode) il managed
    {
		// Code size       11 (0xb)
		.maxstack  8
		ldarg.0
		tail. call value class JitTest_compat_gcval_sideeffect.LargeVT JitTest_compat_gcval_sideeffect.LargeVT::callee(int32)
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
               value class JitTest_compat_gcval_sideeffect.LargeVT V_2)
      IL_0000:  ldc.i4.s   100
      IL_0002:  call       value class JitTest_compat_gcval_sideeffect.LargeVT JitTest_compat_gcval_sideeffect.LargeVT::caller(int32)
      IL_0007:  stloc.2
      IL_0008:  ldloca.s   V_2
      IL_000a:  ldfld      int32[] JitTest_compat_gcval_sideeffect.LargeVT::'value'
      IL_000f:  ldc.i4.0
      IL_0010:  ldelem.i4
      IL_0011:  stloc.0
      IL_0012:  ldloc.0
      IL_0013:  ldc.i4.s   100
      IL_0015:  beq.s      IL_001e

      IL_0017:  ldstr      "failed"
      IL_001c:  br.s       IL_0023

      IL_001e:  ldstr      "passed"
      IL_0023:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0028:  ldloc.0
      IL_0029:  stloc.1
      IL_002a:  br.s       IL_002c

      IL_002c:  ldloc.1
      IL_002d:  ret
    } // end of method LargeVT::Main

  } // end of class LargeVT

} // end of namespace JitTest_compat_gcval_sideeffect

//*********** DISASSEMBLY COMPLETE ***********************
