// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// refanyval.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'refanyval'
{
  
  
}
.assembly extern xunit.core {}
.namespace JitTest_refanyval_operand_il
{
  .class public sequential ansi sealed beforefieldinit Test
         extends [mscorlib]System.ValueType
  {
    .field private int32 m_v
    .method private hidebysig static int32
            refanyval_ldobj() cil managed
    {
      .maxstack  2
      .locals (valuetype JitTest_refanyval_operand_il.Test V_0,
               typedref V_1,
               int32 V_2)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    JitTest_refanyval_operand_il.Test
      IL_0008:  ldloca.s   V_0
      IL_000a:  ldc.i4.1
      IL_000b:  stfld      int32 JitTest_refanyval_operand_il.Test::m_v
      IL_0010:  ldloca.s   V_0
      IL_0012:  mkrefany   JitTest_refanyval_operand_il.Test
      IL_0017:  stloc.1
      IL_0018:  ldloc.1
      IL_0019:  refanyval  JitTest_refanyval_operand_il.Test
      IL_001e:  ldfld      int32 JitTest_refanyval_operand_il.Test::m_v
      IL_0023:  ldc.i4.1
      IL_0024:  sub
      IL_0025:  stloc.2
      IL_0026:  br.s       IL_0028
      IL_0028:  ldloc.2
      IL_0029:  ret
    }
    .method private hidebysig static int32
            refanyval_initobj() cil managed
    {
      .maxstack  2
      .locals (valuetype JitTest_refanyval_operand_il.Test V_0,
               typedref V_1,
               int32 V_2)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    JitTest_refanyval_operand_il.Test
      IL_0008:  ldloca.s   V_0
      IL_000a:  ldc.i4.1
      IL_000b:  stfld      int32 JitTest_refanyval_operand_il.Test::m_v
      IL_0010:  ldloca.s   V_0
      IL_0012:  mkrefany   JitTest_refanyval_operand_il.Test
      IL_0017:  stloc.1
      IL_0018:  ldloc.1
      IL_0019:  refanyval  JitTest_refanyval_operand_il.Test
      IL_001e:  initobj    JitTest_refanyval_operand_il.Test
      IL_0024:  ldloca.s   V_0
      IL_0026:  ldfld      int32 JitTest_refanyval_operand_il.Test::m_v
      IL_002b:  stloc.2
      IL_002c:  br.s       IL_002e
      IL_002e:  ldloc.2
      IL_002f:  ret
    }
    .method private hidebysig static int32
            refanyval_cpobj() cil managed
    {
      .maxstack  2
      .locals (valuetype JitTest_refanyval_operand_il.Test V_0,
               valuetype JitTest_refanyval_operand_il.Test V_1,
               typedref V_2,
               int32 V_3)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    JitTest_refanyval_operand_il.Test
      IL_0008:  ldloca.s   V_0
      IL_000a:  ldc.i4.1
      IL_000b:  stfld      int32 JitTest_refanyval_operand_il.Test::m_v
      IL_0010:  ldloca.s   V_1
      IL_0012:  initobj    JitTest_refanyval_operand_il.Test
      IL_0018:  ldloca.s   V_0
      IL_001a:  mkrefany   JitTest_refanyval_operand_il.Test
      IL_001f:  stloc.2
      IL_0020:  ldloc.2
      IL_0021:  refanyval  JitTest_refanyval_operand_il.Test
      
      IL_0026:  ldloca 1
      IL_0027:  cpobj      JitTest_refanyval_operand_il.Test
      
      IL_002c:  ldloca.s   V_0
      IL_002e:  ldfld      int32 JitTest_refanyval_operand_il.Test::m_v
      IL_0033:  stloc.3
      IL_0034:  br.s       IL_0036
      IL_0036:  ldloc.3
      IL_0037:  ret
    }
    .method private hidebysig static int32
            refanyval_stobj() cil managed
    {
      .maxstack  2
      .locals (valuetype JitTest_refanyval_operand_il.Test V_0,
               valuetype JitTest_refanyval_operand_il.Test V_1,
               typedref V_2,
               int32 V_3)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    JitTest_refanyval_operand_il.Test
      IL_0008:  ldloca.s   V_0
      IL_000a:  ldc.i4.1
      IL_000b:  stfld      int32 JitTest_refanyval_operand_il.Test::m_v
      IL_0010:  ldloca.s   V_1
      IL_0012:  initobj    JitTest_refanyval_operand_il.Test
      IL_0018:  ldloca.s   V_0
      IL_001a:  mkrefany   JitTest_refanyval_operand_il.Test
      IL_001f:  stloc.2
      IL_0020:  ldloc.2
      IL_0021:  refanyval  JitTest_refanyval_operand_il.Test
      IL_0026:  ldloc.1
      IL_0027:  stobj      JitTest_refanyval_operand_il.Test
      IL_002c:  ldloca.s   V_0
      IL_002e:  ldfld      int32 JitTest_refanyval_operand_il.Test::m_v
      IL_0033:  stloc.3
      IL_0034:  br.s       IL_0036
      IL_0036:  ldloc.3
      IL_0037:  ret
    }
    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  1
      .locals (object V_0,
               valuetype JitTest_refanyval_operand_il.Test V_1,
               int32 V_2,
               valuetype JitTest_refanyval_operand_il.Test V_3)
      IL_0000:  ldloca.s   V_3
      IL_0002:  initobj    JitTest_refanyval_operand_il.Test
      IL_0008:  ldloc.3
      IL_0009:  box        JitTest_refanyval_operand_il.Test
      IL_000e:  stloc.0
      IL_000f:  ldloc.0
      IL_0010:  unbox      JitTest_refanyval_operand_il.Test
      IL_0015:  ldobj      JitTest_refanyval_operand_il.Test
      IL_001a:  stloc.1
      IL_001b:  call       int32 JitTest_refanyval_operand_il.Test::refanyval_ldobj()
      IL_0020:  brfalse.s  IL_0031
      IL_0022:  ldstr      "refanyval_ldobj() failed."
      IL_0027:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002c:  ldc.i4.s   101
      IL_002e:  stloc.2
      IL_002f:  br.s       IL_0082
      IL_0031:  call       int32 JitTest_refanyval_operand_il.Test::refanyval_initobj()
      IL_0036:  brfalse.s  IL_0047
      IL_0038:  ldstr      "refanyval_initobj() failed."
      IL_003d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0042:  ldc.i4.s   102
      IL_0044:  stloc.2
      IL_0045:  br.s       IL_0082
      IL_0047:  call       int32 JitTest_refanyval_operand_il.Test::refanyval_stobj()
      IL_004c:  brfalse.s  IL_005d
      IL_004e:  ldstr      "refanyval_stobj() failed."
      IL_0053:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0058:  ldc.i4.s   103
      IL_005a:  stloc.2
      IL_005b:  br.s       IL_0082
      IL_005d:  call       int32 JitTest_refanyval_operand_il.Test::refanyval_cpobj()
      IL_0062:  brfalse.s  IL_0073
      IL_0064:  ldstr      "refanyval_cpobj() failed."
      IL_0069:  call       void [System.Console]System.Console::WriteLine(string)
      IL_006e:  ldc.i4.s   104
      IL_0070:  stloc.2
      IL_0071:  br.s       IL_0082
      IL_0073:  ldstr      "All tests passed!"
      IL_0078:  call       void [System.Console]System.Console::WriteLine(string)
      IL_007d:  ldc.i4.s   100
      IL_007f:  stloc.2
      IL_0080:  br.s       IL_0082
      IL_0082:  ldloc.2
      IL_0083:  ret
    }
  }
}
