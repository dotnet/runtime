// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.file alignment 512

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
// MVID: {2D7E75FB-9B3C-4E6C-BB98-C8ADCFF67713}
.namespace JitTest_accum_il
{
  .class value public auto ansi sealed CI extends [mscorlib]System.ValueType
  {
    .field private int32 m_v
    .method private hidebysig static int32 
            test(value class JitTest_accum_il.CI c1,
                 value class JitTest_accum_il.CI c2,
                 value class JitTest_accum_il.CI c3,
                 int32 dummy,
                 value class JitTest_accum_il.CI c4,
                 value class JitTest_accum_il.CI c5) il managed
    {
      // Code size       44 (0x2c)
      .maxstack  5
      IL_0000:  ldarga.s   c1
      IL_0002:  ldfld      int32 JitTest_accum_il.CI::m_v
      IL_0007:  ldarga.s   c2
      IL_0009:  ldfld      int32 JitTest_accum_il.CI::m_v
      IL_000f:  ldarga.s   c3
      IL_0011:  ldfld      int32 JitTest_accum_il.CI::m_v
      IL_0017:  ldarga.s   c4
      IL_0019:  ldfld      int32 JitTest_accum_il.CI::m_v
      IL_001f:  ldarga.s   c5
      IL_0021:  ldfld      int32 JitTest_accum_il.CI::m_v
      IL_0026:  add
      IL_001e:  add
      IL_000e:  add
      IL_0016:  add
      IL_002b:  ret
    } // end of method CI::test

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       27 (0x1b)
      .maxstack  8
      .locals (value class JitTest_accum_il.CI V)
      			ldloca.s   V
      			initobj    JitTest_accum_il.CI
      			
      			ldc.i4.s   100
      	
      			ldloca.s   V
      			ldc.i4.1
      			stfld		int32 JitTest_accum_il.CI::m_v
				ldloc.0
      
      			ldloca.s   V
      			ldc.i4.2
      			stfld		int32 JitTest_accum_il.CI::m_v
      			ldloc.0
            
      			ldloca.s   V
      			ldc.i4.3
      			stfld		int32 JitTest_accum_il.CI::m_v
      			ldloc.0
      
      			ldc.i4.0
      
      			ldloca.s   V
      			ldc.i4.4
      			stfld		int32 JitTest_accum_il.CI::m_v
      			ldloc.0
      
      			ldloca.s   V
      			ldc.i4 -10
      			stfld		int32 JitTest_accum_il.CI::m_v
      			ldloc.0
      			
      			call       int32 JitTest_accum_il.CI::test(value class JitTest_accum_il.CI,
                                                  value class JitTest_accum_il.CI,
                                                  value class JitTest_accum_il.CI,
                                                  int32,
                                                  value class JitTest_accum_il.CI,
                                                  value class JitTest_accum_il.CI)
      IL_0015:  add
      IL_001a:  ret
    } // end of method CI::Main

  } // end of class CI

} // end of namespace JitTest_accum_il

//*********** DISASSEMBLY COMPLETE ***********************
