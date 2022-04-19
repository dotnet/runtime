// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib { }
.assembly 'localloc_operand' {
}
.assembly extern xunit.core {}
.namespace JitTest_localloc_operand_il
{
  .class public sequential ansi sealed beforefieldinit Test
         extends [mscorlib]System.ValueType
  {
    .field private int32 m_v
    .method private hidebysig static int32
            localloc_ldobj() cil managed
    {
      .maxstack  8
      .locals (native int ptr, value class JitTest_localloc_operand_il.Test elem, int32 dummy)
      sizeof int32
      localloc
      ldobj int32
      stloc.s dummy

      ldloca.s elem
      initobj JitTest_localloc_operand_il.Test

      sizeof JitTest_localloc_operand_il.Test
      ldc.i4.2
      mul
      localloc
      sizeof JitTest_localloc_operand_il.Test
      ldc.i4.2
      div
      add
      stloc.s ptr

      ldloc.s ptr
      ldc.i4 1234
      unaligned. 1
      stfld int32 JitTest_localloc_operand_il.Test::m_v

      ldloc.s ptr
      unaligned. 1
      ldobj JitTest_localloc_operand_il.Test

      stloc.s elem
      ldloca.s elem
      ldfld int32 JitTest_localloc_operand_il.Test::m_v
      ldc.i4 1234
      sub
      ret
    }
    .method private hidebysig static int32
            localloc_initobj() cil managed
    {
      .maxstack  8
      .locals (native int ptr, value class JitTest_localloc_operand_il.Test elem, int32 dummy)
      sizeof JitTest_localloc_operand_il.Test
      localloc
      initobj JitTest_localloc_operand_il.Test

      sizeof JitTest_localloc_operand_il.Test
      localloc
      dup
      initobj JitTest_localloc_operand_il.Test
      ldfld int32 JitTest_localloc_operand_il.Test::m_v
      dup
      brtrue RET
      pop

      sizeof JitTest_localloc_operand_il.Test
      ldc.i4.2
      mul
      localloc
      sizeof JitTest_localloc_operand_il.Test
      ldc.i4.2
      div
      add

      dup
      unaligned. 1
      initobj JitTest_localloc_operand_il.Test
      unaligned. 1
      ldfld int32 JitTest_localloc_operand_il.Test::m_v

    RET:
      ret
    }

    .method private hidebysig static int32
            localloc_cpobj() cil managed
    {
      .maxstack  8
      .locals (native int ptr, value class JitTest_localloc_operand_il.Test elem, int32 dummy)
      sizeof int32
      localloc
      ldobj int32
      stloc.s dummy

      ldloca.s elem
      initobj JitTest_localloc_operand_il.Test

      sizeof JitTest_localloc_operand_il.Test
      ldc.i4.2
      mul
      localloc
      sizeof JitTest_localloc_operand_il.Test
      ldc.i4.2
      div
      add
      stloc.s ptr

      ldloc.s ptr
      ldc.i4 1234
      unaligned. 1
      stfld int32 JitTest_localloc_operand_il.Test::m_v

      ldloca.s elem
      ldloc.s ptr
      unaligned. 1
      cpobj JitTest_localloc_operand_il.Test

      ldloca.s elem
      ldfld int32 JitTest_localloc_operand_il.Test::m_v
      ldc.i4 1234
      sub
      ret
    }

    .method private hidebysig static int32
            localloc_stobj() cil managed
    {
      .maxstack  8
      .locals (value class JitTest_localloc_operand_il.Test[0...10] arr, value class JitTest_localloc_operand_il.Test elem)
      .maxstack  8
      .locals (native int ptr, value class JitTest_localloc_operand_il.Test elem, int32 dummy)
      sizeof int32
      localloc
      ldobj int32
      stloc.s dummy

      ldloca.s elem
      initobj JitTest_localloc_operand_il.Test

      sizeof JitTest_localloc_operand_il.Test
      ldc.i4.2
      mul
      localloc
      sizeof JitTest_localloc_operand_il.Test
      ldc.i4.2
      div
      add
      stloc.s ptr

      ldloc.s ptr
      ldc.i4 1234
      unaligned. 1
      stfld int32 JitTest_localloc_operand_il.Test::m_v

      ldloc.s ptr
      ldloc.s elem
      unaligned. 1
      stobj JitTest_localloc_operand_il.Test

      ldloca.s elem
      ldfld int32 JitTest_localloc_operand_il.Test::m_v
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
      .locals (int32 V_0)
      IL_0000:  call       int32 JitTest_localloc_operand_il.Test::localloc_ldobj()
      IL_0005:  brfalse.s  IL_0016
      IL_0007:  ldstr      "localloc_ldobj() failed."
      IL_000c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0011:  ldc.i4.s   101
      IL_0013:  stloc.0
      IL_0014:  br.s       IL_0067
      IL_0016:  call       int32 JitTest_localloc_operand_il.Test::localloc_initobj()
      IL_001b:  brfalse.s  IL_002c
      IL_001d:  ldstr      "localloc_initobj() failed."
      IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0027:  ldc.i4.s   102
      IL_0029:  stloc.0
      IL_002a:  br.s       IL_0067
      IL_002c:  call       int32 JitTest_localloc_operand_il.Test::localloc_stobj()
      IL_0031:  brfalse.s  IL_0042
      IL_0033:  ldstr      "localloc_stobj() failed."
      IL_0038:  call       void [System.Console]System.Console::WriteLine(string)
      IL_003d:  ldc.i4.s   103
      IL_003f:  stloc.0
      IL_0040:  br.s       IL_0067
      IL_0042:  call       int32 JitTest_localloc_operand_il.Test::localloc_cpobj()
      IL_0047:  brfalse.s  IL_0058
      IL_0049:  ldstr      "localloc_cpobj() failed."
      IL_004e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0053:  ldc.i4.s   104
      IL_0055:  stloc.0
      IL_0056:  br.s       IL_0067
      IL_0058:  ldstr      "All tests passed!"
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  ldc.i4.s   100
      IL_0064:  stloc.0
      IL_0065:  br.s       IL_0067
      IL_0067:  ldloc.0
      IL_0068:  ret
    }
  }
}
