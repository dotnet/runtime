// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'ldsfld_mulovf'// as "ldsfld_mulovf"
{
}
.assembly extern xunit.core {}
// MVID: {F167E31C-7032-4D75-B802-EE8B8384CBD2}
.namespace JitTest_ldsfld_mulovf_unsigned_il
{
  .class public auto ansi Test
         extends ['mscorlib']System.Object
  {
    .field private static unsigned int64 op1
    .field private static unsigned int64 op2
    .method private hidebysig static bool 
            check(unsigned int64 product,
                  bool overflow) il managed
    {
      // Code size       77 (0x4d)
      .maxstack  3
      .locals (bool V_0)
      IL_0000:  ldstr      "Multiplying {0} and {1}..."
      IL_0005:  ldsfld    unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op1
      IL_000a:  box     ['mscorlib']System.UInt64
      IL_000f:  ldsfld    unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op2
      IL_0014:  box     ['mscorlib']System.UInt64
      IL_0019:  call       void [System.Console]System.Console::Write(class System.String,
                                                                  class System.Object,
                                                                  class System.Object)
      .try
      {
        IL_001e:  ldsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op1
        IL_0023:  ldsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op2
        IL_0028:  mul.ovf.un
        IL_0029:  ldarg.0
        IL_002a:  beq.s      IL_0030

        IL_002c:  ldc.i4.0
        IL_002d:  stloc.0
        IL_002e:  leave.s    IL_004b

        IL_0030:  call       void [System.Console]System.Console::WriteLine()
        IL_0035:  ldarg.1
        IL_0036:  ldc.i4.0
        IL_0037:  ceq
        IL_0039:  stloc.0
        IL_003a:  leave.s    IL_004b

      }  // end .try
      catch ['mscorlib']System.OverflowException 
      {
        IL_003c:  pop
        IL_003d:  ldstr      "overflow."
        IL_0042:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_0047:  ldarg.1
        IL_0048:  stloc.0
        IL_0049:  leave.s    IL_004b

      }  // end handler
      IL_004b:  ldloc.0
      IL_004c:  ret
    } // end of method 'Test::check'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       260 (0x104)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldc.i4.m1
      IL_0001:  conv.u8
      IL_0002:  stsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op1
      IL_0007:  ldc.i4.m1
      IL_0008:  conv.u8
      IL_0009:  stsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op2
      IL_000e:  ldc.i8     0xfffffffe00000001
      IL_0017:  ldc.i4.0
      IL_0018:  call       bool JitTest_ldsfld_mulovf_unsigned_il.Test::check(unsigned int64,
                                                    bool)
      IL_001d:  brtrue.s   IL_0024

      IL_001f:  br         IL_00f4

      IL_0024:  ldc.i8     0x100000000
      IL_002d:  stsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op1
      IL_0032:  ldc.i4.m1
      IL_0033:  conv.u8
      IL_0034:  stsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op2
      IL_0039:  ldc.i8     0xffffffff00000000
      IL_0042:  ldc.i4.0
      IL_0043:  call       bool JitTest_ldsfld_mulovf_unsigned_il.Test::check(unsigned int64,
                                                    bool)
      IL_0048:  brtrue.s   IL_004f

      IL_004a:  br         IL_00f4

      IL_004f:  ldc.i8     0x100000000
      IL_0058:  stsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op1
      IL_005d:  ldc.i8     0x100000000
      IL_0066:  stsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op2
      IL_006b:  ldc.i4.0
      IL_006c:  conv.i8
      IL_006d:  ldc.i4.1
      IL_006e:  call       bool JitTest_ldsfld_mulovf_unsigned_il.Test::check(unsigned int64,
                                                    bool)
      IL_0073:  brtrue.s   IL_0077

      IL_0075:  br       IL_00f4

      IL_0077:  ldc.i8     0x7fffffffffffffff
      IL_0080:  stsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op1
      IL_0085:  ldc.i4.2
      IL_0086:  conv.i8
      IL_0087:  stsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op2
      IL_008c:  ldc.i4.s   -2
      IL_008e:  conv.i8
      IL_008f:  ldc.i4.0
      IL_0090:  call       bool JitTest_ldsfld_mulovf_unsigned_il.Test::check(unsigned int64,
                                                    bool)
      IL_0095:  brtrue.s   IL_0099

      IL_0097:  br       IL_00f4

      IL_0099:  ldc.i8     0x8000000000000000
      IL_00a2:  stsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op1
      IL_00a7:  ldc.i4.2
      IL_00a8:  conv.i8
      IL_00a9:  stsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op2
      IL_00ae:  ldc.i4.0
      IL_00af:  conv.i8
      IL_00b0:  ldc.i4.1
      IL_00b1:  call       bool JitTest_ldsfld_mulovf_unsigned_il.Test::check(unsigned int64,
                                                    bool)
      IL_00b6:  brtrue.s   IL_00ba

      IL_00b8:  br       IL_00f4

      IL_00ba:  ldc.i4     0x100000
      IL_00bf:  conv.i8
      IL_00c0:  stsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op1
      IL_00c5:  ldc.i8     0x1000000000
      IL_00ce:  stsfld     unsigned int64 JitTest_ldsfld_mulovf_unsigned_il.Test::op2
      IL_00d3:  ldc.i8     0x100000000000000
      IL_00dc:  ldc.i4.0
      IL_00dd:  call       bool JitTest_ldsfld_mulovf_unsigned_il.Test::check(unsigned int64,
                                                    bool)
      IL_00e2:  brtrue.s   IL_00e6

      IL_00e4:  br       IL_00f4

      IL_00e6:  ldstr      "Test passed"
      IL_00eb:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00f0:  ldc.i4     0x64
      IL_00f1:  stloc.0
      IL_00f2:  br       IL_0102

      IL_00f4:  ldstr      "Test failed"
      IL_00f9:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00fe:  ldc.i4.1
      IL_00ff:  stloc.0
      IL_0100:  br       IL_0102

      IL_0102:  ldloc.0
      IL_0103:  ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'JitTest_ldsfld_mulovf_unsigned_il'

//*********** DISASSEMBLY COMPLETE ***********************
