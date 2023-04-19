// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







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
// MVID: {C6227192-F885-4DCA-9BE0-1B2B2204AB9E}
.namespace JitTest_ldc_mulovf_unsigned_il
{
  .class public auto ansi Test
         extends ['mscorlib']System.Object
  {
    .method private hidebysig static bool 
            check(unsigned int64 op1,
                  unsigned int64 op2,
                  unsigned int64 product,
                  bool overflow) il managed
    {
      // Code size       63 (0x3f)
      .maxstack  3
      .locals (bool V_0)
      IL_0000:  ldstr      "Multiplying {0} and {1}..."
      IL_0005:  ldarg.s   op1
      IL_0007:  box     ['mscorlib']System.UInt64
      IL_000c:  ldarg.s   op2
      IL_000e:  box     ['mscorlib']System.UInt64
      IL_0013:  call       void [System.Console]System.Console::Write(class System.String,
                                                                  class System.Object,
                                                                  class System.Object)
      .try
      {
        IL_0018:  ldarg.0
        IL_0019:  ldarg.1
        IL_001a:  mul.ovf.un
        IL_001b:  ldarg.2
        IL_001c:  beq.s      IL_0022

        IL_001e:  ldc.i4.0
        IL_001f:  stloc.0
        IL_0020:  leave.s    IL_003d

        IL_0022:  call       void [System.Console]System.Console::WriteLine()
        IL_0027:  ldarg.3
        IL_0028:  ldc.i4.0
        IL_0029:  ceq
        IL_002b:  stloc.0
        IL_002c:  leave.s    IL_003d

      }  // end .try
      catch ['mscorlib']System.OverflowException 
      {
        IL_002e:  pop
        IL_002f:  ldstr      "overflow."
        IL_0034:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_0039:  ldarg.3
        IL_003a:  stloc.0
        IL_003b:  leave.s    IL_003d

      }  // end handler
      IL_003d:  ldloc.0
      IL_003e:  ret
    } // end of method 'Test::check'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       197 (0xc5)
      .maxstack  4
      .locals (int32 V_0)
      IL_0000:  ldc.i4.m1
      IL_0001:  conv.u8
      IL_0002:  ldc.i4.m1
      IL_0003:  conv.u8
      IL_0004:  ldc.i8     0xfffffffe00000001
      IL_000d:  ldc.i4.0
      IL_000e:  call       bool JitTest_ldc_mulovf_unsigned_il.Test::check(unsigned int64,
                                                    unsigned int64,
                                                    unsigned int64,
                                                    bool)
      IL_0013:  brtrue.s   IL_001a

      IL_0015:  br         IL_00b5

      IL_001a:  ldc.i8     0x100000000
      IL_0023:  ldc.i4.m1
      IL_0024:  conv.u8
      IL_0025:  ldc.i8     0xffffffff00000000
      IL_002e:  ldc.i4.0
      IL_002f:  call       bool JitTest_ldc_mulovf_unsigned_il.Test::check(unsigned int64,
                                                    unsigned int64,
                                                    unsigned int64,
                                                    bool)
      IL_0034:  brtrue.s   IL_0038

      IL_0036:  br       IL_00b5

      IL_0038:  ldc.i8     0x100000000
      IL_0041:  ldc.i8     0x100000000
      IL_004a:  ldc.i4.0
      IL_004b:  conv.i8
      IL_004c:  ldc.i4.1
      IL_004d:  call       bool JitTest_ldc_mulovf_unsigned_il.Test::check(unsigned int64,
                                                    unsigned int64,
                                                    unsigned int64,
                                                    bool)
      IL_0052:  brtrue.s   IL_0056

      IL_0054:  br       IL_00b5

      IL_0056:  ldc.i8     0x7fffffffffffffff
      IL_005f:  ldc.i4.2
      IL_0060:  conv.i8
      IL_0061:  ldc.i4.s   -2
      IL_0063:  conv.i8
      IL_0064:  ldc.i4.0
      IL_0065:  call       bool JitTest_ldc_mulovf_unsigned_il.Test::check(unsigned int64,
                                                    unsigned int64,
                                                    unsigned int64,
                                                    bool)
      IL_006a:  brtrue.s   IL_006e

      IL_006c:  br       IL_00b5

      IL_006e:  ldc.i8     0x8000000000000000
      IL_0077:  ldc.i4.2
      IL_0078:  conv.i8
      IL_0079:  ldc.i4.0
      IL_007a:  conv.i8
      IL_007b:  ldc.i4.1
      IL_007c:  call       bool JitTest_ldc_mulovf_unsigned_il.Test::check(unsigned int64,
                                                    unsigned int64,
                                                    unsigned int64,
                                                    bool)
      IL_0081:  brtrue.s   IL_0085

      IL_0083:  br       IL_00b5

      IL_0085:  ldc.i4     0x100000
      IL_008a:  conv.i8
      IL_008b:  ldc.i8     0x1000000000
      IL_0094:  ldc.i8     0x100000000000000
      IL_009d:  ldc.i4.0
      IL_009e:  call       bool JitTest_ldc_mulovf_unsigned_il.Test::check(unsigned int64,
                                                    unsigned int64,
                                                    unsigned int64,
                                                    bool)
      IL_00a3:  brtrue.s   IL_00a7

      IL_00a5:  br       IL_00b5

      IL_00a7:  ldstr      "Test passed"
      IL_00ac:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00b1:  ldc.i4    0x64
      IL_00b2:  stloc.0
      IL_00b3:  br       IL_00c3

      IL_00b5:  ldstr      "Test failed"
      IL_00ba:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00bf:  ldc.i4.1
      IL_00c0:  stloc.0
      IL_00c1:  br       IL_00c3

      IL_00c3:  ldloc.0
      IL_00c4:  ret
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

} // end of namespace 'JitTest_ldc_mulovf_unsigned_il'

//*********** DISASSEMBLY COMPLETE ***********************
