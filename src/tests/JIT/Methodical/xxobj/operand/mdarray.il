// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// mdarray.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly mdarray { }
.assembly extern xunit.core {}
.namespace JitTest_mdarray_operand_il
{
  .class public sequential ansi sealed beforefieldinit Test
         extends [mscorlib]System.ValueType
  {
    .field private int32 m_v
    .method private hidebysig static int32
            mdarray_ldobj() cil managed
    {
      .maxstack  8
      .locals (value class JitTest_mdarray_operand_il.Test[0...10] arr, value class JitTest_mdarray_operand_il.Test elem)
      // arr = new Test[0...10];
      ldc.i4 11
      newobj instance void value class JitTest_mdarray_operand_il.Test[0...10]::.ctor(int32)
      stloc.s arr

      // arr[5].m_v = 1234;
      ldloc.s arr
      ldc.i4.5
      call instance value class JitTest_mdarray_operand_il.Test& value class JitTest_mdarray_operand_il.Test[0...10]::Address(int32)
      ldc.i4 1234
      stfld int32 JitTest_mdarray_operand_il.Test::m_v
      
      // elem = arr[5];
      ldloc.s arr
      ldc.i4.5
      call instance value class JitTest_mdarray_operand_il.Test& value class JitTest_mdarray_operand_il.Test[0...10]::Address(int32)
      ldobj JitTest_mdarray_operand_il.Test
      stloc.s elem

      // return elem.m_v - 1234;
      ldloca.s elem
      ldfld int32 JitTest_mdarray_operand_il.Test::m_v
      ldc.i4 1234
      xor
      ret
    }
    .method private hidebysig static int32
            mdarray_initobj() cil managed
    {
      .maxstack  8
      .locals (value class JitTest_mdarray_operand_il.Test[0...10] arr)
      ldc.i4 11
      newobj instance void value class JitTest_mdarray_operand_il.Test[0...10]::.ctor(int32)
      stloc.s arr

      ldloc.s arr
      ldc.i4.5
      call instance value class JitTest_mdarray_operand_il.Test& value class JitTest_mdarray_operand_il.Test[0...10]::Address(int32)
      ldc.i4 1234
      stfld int32 JitTest_mdarray_operand_il.Test::m_v

      ldloc.s arr
      ldc.i4.5
      call instance value class JitTest_mdarray_operand_il.Test& value class JitTest_mdarray_operand_il.Test[0...10]::Address(int32)
      initobj JitTest_mdarray_operand_il.Test

      ldloc.s arr
      ldc.i4.5
      call instance value class JitTest_mdarray_operand_il.Test& value class JitTest_mdarray_operand_il.Test[0...10]::Address(int32)
      ldfld int32 JitTest_mdarray_operand_il.Test::m_v
      ret
    }

    .method private hidebysig static int32
            mdarray_cpobj() cil managed
    {
      .maxstack  8
      .locals (value class JitTest_mdarray_operand_il.Test[0...10] arr, value class JitTest_mdarray_operand_il.Test elem)
      ldloca.s elem
      initobj JitTest_mdarray_operand_il.Test

      ldc.i4 11
      newobj instance void value class JitTest_mdarray_operand_il.Test[0...10]::.ctor(int32)
      stloc.s arr

      ldloc.s arr
      ldc.i4.5
      call instance value class JitTest_mdarray_operand_il.Test& value class JitTest_mdarray_operand_il.Test[0...10]::Address(int32)
      ldc.i4 1234
      stfld int32 JitTest_mdarray_operand_il.Test::m_v

      ldloc.s arr
      ldc.i4.5
      call instance value class JitTest_mdarray_operand_il.Test& value class JitTest_mdarray_operand_il.Test[0...10]::Address(int32)
      ldloca.s elem
      cpobj JitTest_mdarray_operand_il.Test

      ldloc.s arr
      ldc.i4.5
      call instance value class JitTest_mdarray_operand_il.Test& value class JitTest_mdarray_operand_il.Test[0...10]::Address(int32)
      ldfld int32 JitTest_mdarray_operand_il.Test::m_v
      dup
      brtrue RET
      pop
      
      ldloc.s arr
      ldc.i4.7
      call instance value class JitTest_mdarray_operand_il.Test& value class JitTest_mdarray_operand_il.Test[0...10]::Address(int32)
      ldc.i4 4321
      stfld int32 JitTest_mdarray_operand_il.Test::m_v
      
      ldloca.s elem
      ldloc.s arr
      ldc.i4.7
      call instance value class JitTest_mdarray_operand_il.Test& value class JitTest_mdarray_operand_il.Test[0...10]::Address(int32)
      cpobj JitTest_mdarray_operand_il.Test

      ldloca.s elem
      ldfld int32 JitTest_mdarray_operand_il.Test::m_v
      ldc.i4 4321
      xor

    RET:
      ret
    }

    .method private hidebysig static int32
            mdarray_stobj() cil managed
    {
      .maxstack  8
      .locals (value class JitTest_mdarray_operand_il.Test[0...10] arr, value class JitTest_mdarray_operand_il.Test elem)
      ldloca.s elem
      initobj JitTest_mdarray_operand_il.Test
      
      ldc.i4 11
      newobj instance void value class JitTest_mdarray_operand_il.Test[0...10]::.ctor(int32)
      stloc.s arr

      ldloc.s arr
      ldc.i4.5
      call instance value class JitTest_mdarray_operand_il.Test& value class JitTest_mdarray_operand_il.Test[0...10]::Address(int32)
      ldc.i4 1234
      stfld int32 JitTest_mdarray_operand_il.Test::m_v
      
      ldloc.s arr
      ldc.i4.5
      call instance value class JitTest_mdarray_operand_il.Test& value class JitTest_mdarray_operand_il.Test[0...10]::Address(int32)
      ldloc.s elem
      stobj JitTest_mdarray_operand_il.Test
      
      ldloc.s arr
      ldc.i4.5
      call instance value class JitTest_mdarray_operand_il.Test& value class JitTest_mdarray_operand_il.Test[0...10]::Address(int32)
      ldfld int32 JitTest_mdarray_operand_il.Test::m_v
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
      IL_0000:  call       int32 JitTest_mdarray_operand_il.Test::mdarray_ldobj()
      IL_0005:  brfalse.s  IL_0016
      IL_0007:  ldstr      "mdarray_ldobj() failed."
      IL_000c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0011:  ldc.i4.s   101
      IL_0013:  stloc.0
      IL_0014:  br.s       IL_0067
      IL_0016:  call       int32 JitTest_mdarray_operand_il.Test::mdarray_initobj()
      IL_001b:  brfalse.s  IL_002c
      IL_001d:  ldstr      "mdarray_initobj() failed."
      IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0027:  ldc.i4.s   102
      IL_0029:  stloc.0
      IL_002a:  br.s       IL_0067
      IL_002c:  call       int32 JitTest_mdarray_operand_il.Test::mdarray_stobj()
      IL_0031:  brfalse.s  IL_0042
      IL_0033:  ldstr      "mdarray_stobj() failed."
      IL_0038:  call       void [System.Console]System.Console::WriteLine(string)
      IL_003d:  ldc.i4.s   103
      IL_003f:  stloc.0
      IL_0040:  br.s       IL_0067
      IL_0042:  call       int32 JitTest_mdarray_operand_il.Test::mdarray_cpobj()
      IL_0047:  brfalse.s  IL_0058
      IL_0049:  ldstr      "mdarray_cpobj() failed."
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
