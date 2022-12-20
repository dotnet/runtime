// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// const.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'const'
{
}
.assembly extern xunit.core {}
.namespace JitTest_const_operand_il
{
  .class public sequential ansi sealed beforefieldinit Test
         extends [mscorlib]System.ValueType
  {
    .field private int32 m_v
    .method private hidebysig static int32
            const_ldobj(int8*) cil managed
    {
      .maxstack  8
      ldarg.0
      ldobj unsigned int8
      ldc.i4 77
      sub
      ret
    }
    .method private hidebysig static int32
            const_initobj() cil managed
    {
      .maxstack  1
      ldc.i4.0
      ret
    }
    .method private hidebysig static int32
            const_cpobj(int8*) cil managed
    {
      .maxstack  8
      .locals (int8)
      ldloca.s 0
      ldarg.0
      cpobj int8
      ldc.i4 -77
      ldloc.0
      add
      ret
    }
    .method private hidebysig static int32
            const_stobj() cil managed
    {
      .maxstack  1
      ldc.i4.0
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
      .locals (int32 V_0, int8 V_1)

	// storing 77 in the dummy local
	ldc.i4 77
	stloc V_1

	ldloca.s V_1
	conv.u
      IL_0000:  call       int32 JitTest_const_operand_il.Test::const_ldobj(int8*)
      IL_0005:  brfalse.s  IL_0016
      IL_0007:  ldstr      "const_ldobj() failed."
      IL_000c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0011:  ldc.i4.s   101
      IL_0013:  stloc.0
      IL_0014:  br.s       IL_0067
      IL_0016:  call       int32 JitTest_const_operand_il.Test::const_initobj()
      IL_001b:  brfalse.s  IL_002c
      IL_001d:  ldstr      "const_initobj() failed."
      IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0027:  ldc.i4.s   102
      IL_0029:  stloc.0
      IL_002a:  br.s       IL_0067
      IL_002c:  call       int32 JitTest_const_operand_il.Test::const_stobj()
      IL_0031:  brfalse.s  IL_0042
      IL_0033:  ldstr      "const_stobj() failed."
      IL_0038:  call       void [System.Console]System.Console::WriteLine(string)
      IL_003d:  ldc.i4.s   103
      IL_003f:  stloc.0
      IL_0040:  br.s       IL_0067
      IL_0042:  
		ldloca.s V_1
		conv.u		
		call       int32 JitTest_const_operand_il.Test::const_cpobj(int8*)
      IL_0047:  brfalse.s  IL_0058
      IL_0049:  ldstr      "const_cpobj() failed."
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
