// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unbox.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{
  
  
}
.assembly extern xunit.core {}
.namespace JitTest_unbox_operand_il
{
  .class public sequential ansi sealed beforefieldinit Test
         extends [mscorlib]System.ValueType
  {
    .field private int32 m_v
    .method private hidebysig static int32
            unbox_ldobj() cil managed
    {
      .maxstack  2
      .locals (valuetype JitTest_unbox_operand_il.Test V_0,
               object V_1,
               int32 V_2,
               valuetype JitTest_unbox_operand_il.Test V_3)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    JitTest_unbox_operand_il.Test
      IL_0008:  ldloca.s   V_0
      IL_000a:  ldc.i4.1
      IL_000b:  stfld      int32 JitTest_unbox_operand_il.Test::m_v
      IL_0010:  ldloc.0
      IL_0011:  box        JitTest_unbox_operand_il.Test
      IL_0016:  stloc.1
      IL_0017:  ldloc.1
      IL_0018:  unbox      JitTest_unbox_operand_il.Test
      IL_001d:  ldobj      JitTest_unbox_operand_il.Test
      IL_0022:  stloc.3
      IL_0023:  ldloca.s   V_3
      IL_0025:  ldfld      int32 JitTest_unbox_operand_il.Test::m_v
      IL_002a:  ldc.i4.1
      IL_002b:  sub
      IL_002c:  stloc.2
      IL_002d:  br.s       IL_002f
      IL_002f:  ldloc.2
      IL_0030:  ret
    }
    .method private hidebysig static int32
            unbox_initobj() cil managed
    {
      .maxstack  2
      .locals (valuetype JitTest_unbox_operand_il.Test V_0,
               object V_1,
               int32 V_2,
               valuetype JitTest_unbox_operand_il.Test V_3)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    JitTest_unbox_operand_il.Test
      IL_0008:  ldloca.s   V_0
      IL_000a:  ldc.i4.1
      IL_000b:  stfld      int32 JitTest_unbox_operand_il.Test::m_v
      IL_0010:  ldloc.0
      IL_0011:  box        JitTest_unbox_operand_il.Test
      IL_0016:  stloc.1

      			ldloc.1
      			unbox		JitTest_unbox_operand_il.Test
      			initobj		JitTest_unbox_operand_il.Test

      IL_0026:  ldloc.1
      IL_0027:  unbox      JitTest_unbox_operand_il.Test
      IL_002c:  ldobj      JitTest_unbox_operand_il.Test
      IL_0031:  stloc.3
      IL_0032:  ldloca.s   V_3
      IL_0034:  ldfld      int32 JitTest_unbox_operand_il.Test::m_v
      IL_0039:  stloc.2
      IL_003a:  br.s       IL_003c
      IL_003c:  ldloc.2
      IL_003d:  ret
    }
    .method private hidebysig static int32
            unbox_cpobj() cil managed
    {
      .maxstack  2
      .locals (valuetype JitTest_unbox_operand_il.Test V_0,
               valuetype JitTest_unbox_operand_il.Test V_1,
               object V_2,
               int32 V_3,
               valuetype JitTest_unbox_operand_il.Test V_4)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    JitTest_unbox_operand_il.Test
      IL_0008:  ldloca.s   V_0
      IL_000a:  ldc.i4.1
      IL_000b:  stfld      int32 JitTest_unbox_operand_il.Test::m_v
      IL_0010:  ldloca.s   V_1
      IL_0012:  initobj    JitTest_unbox_operand_il.Test
      IL_0018:  ldloc.0
      IL_0019:  box        JitTest_unbox_operand_il.Test
      IL_001e:  stloc.2
      
      			ldloc.2
      			unbox		JitTest_unbox_operand_il.Test
      			ldloca.s 1
      			cpobj		JitTest_unbox_operand_il.Test
      
      IL_0026:  ldloc.2
      IL_0027:  unbox      JitTest_unbox_operand_il.Test
      IL_002c:  ldobj      JitTest_unbox_operand_il.Test
      IL_0031:  stloc.s    V_4
      IL_0033:  ldloca.s   V_4
      IL_0035:  ldfld      int32 JitTest_unbox_operand_il.Test::m_v
      IL_003a:  stloc.3
      IL_003b:  br.s       IL_003d
      IL_003d:  ldloc.3
      IL_003e:  ret
    }
    .method private hidebysig static int32
            unbox_stobj() cil managed
    {
      .maxstack  2
      .locals (valuetype JitTest_unbox_operand_il.Test V_0,
               valuetype JitTest_unbox_operand_il.Test V_1,
               object V_2,
               int32 V_3,
               valuetype JitTest_unbox_operand_il.Test V_4)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    JitTest_unbox_operand_il.Test
      IL_0008:  ldloca.s   V_0
      IL_000a:  ldc.i4.1
      IL_000b:  stfld      int32 JitTest_unbox_operand_il.Test::m_v
      IL_0010:  ldloca.s   V_1
      IL_0012:  initobj    JitTest_unbox_operand_il.Test
      IL_0018:  ldloc.0
      IL_0019:  box        JitTest_unbox_operand_il.Test
      IL_001e:  stloc.2
      
      			ldloc.2
      			unbox		JitTest_unbox_operand_il.Test
      			ldloc.1
      			stobj		JitTest_unbox_operand_il.Test
      
      IL_0026:  ldloc.2
      IL_0027:  unbox      JitTest_unbox_operand_il.Test
      IL_002c:  ldobj      JitTest_unbox_operand_il.Test
      IL_0031:  stloc.s    V_4
      IL_0033:  ldloca.s   V_4
      IL_0035:  ldfld      int32 JitTest_unbox_operand_il.Test::m_v
      IL_003a:  stloc.3
      IL_003b:  br.s       IL_003d
      IL_003d:  ldloc.3
      IL_003e:  ret
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
      IL_0000:  call       int32 JitTest_unbox_operand_il.Test::unbox_ldobj()
      IL_0005:  brfalse.s  IL_0016
      IL_0007:  ldstr      "unbox_ldobj() failed."
      IL_000c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0011:  ldc.i4.s   101
      IL_0013:  stloc.0
      IL_0014:  br.s       IL_0067
      IL_0016:  call       int32 JitTest_unbox_operand_il.Test::unbox_initobj()
      IL_001b:  brfalse.s  IL_002c
      IL_001d:  ldstr      "unbox_initobj() failed."
      IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0027:  ldc.i4.s   102
      IL_0029:  stloc.0
      IL_002a:  br.s       IL_0067
      IL_002c:  call       int32 JitTest_unbox_operand_il.Test::unbox_stobj()
      IL_0031:  brfalse.s  IL_0042
      IL_0033:  ldstr      "unbox_stobj() failed."
      IL_0038:  call       void [System.Console]System.Console::WriteLine(string)
      IL_003d:  ldc.i4.s   103
      IL_003f:  stloc.0
      IL_0040:  br.s       IL_0067
      IL_0042:  call       int32 JitTest_unbox_operand_il.Test::unbox_cpobj()
      IL_0047:  brfalse.s  IL_0058
      IL_0049:  ldstr      "unbox_cpobj() failed."
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
