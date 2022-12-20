// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 's_ldc_div'// as "ldc_div"
{
}
.assembly extern xunit.core {}
// MVID: {907922FB-D56B-4407-A016-0118205F2F38}
.namespace JitTest_s_ldc_div_signed_il
{
  .class public auto ansi Test
         extends ['mscorlib']System.Object
  {
    .method private hidebysig static bool 
            check(int64 op1,
                  int64 op2,
                  int64 product,
                  bool overflow) il managed
    {
      // Code size       63 (0x3f)
      .maxstack  3
      .locals (bool V_0)
      IL_0000:  ldstr      "Dividing {0} by {1}..."
      IL_0005:  ldarg.s   product
      IL_0007:  box     ['mscorlib']System.Int64
      IL_000c:  ldarg.s   op1
      IL_000e:  box     ['mscorlib']System.Int64
      IL_0013:  call       void [System.Console]System.Console::Write(class System.String,
                                                                  class System.Object,
                                                                  class System.Object)
      .try
      {
        IL_0018:  ldarg.2
        IL_0019:  ldarg.0
        IL_001a:  div
        IL_001b:  ldarg.1
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
      catch ['mscorlib']System.DivideByZeroException 
      {
        IL_002e:  pop
        IL_002f:  ldstr      "divide by zero."
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
      // Code size       271 (0x10f)
      .maxstack  4
      .locals (int32 V_0)
      IL_0000:  ldc.i4     0x7fffffff
      IL_0005:  conv.i8
      IL_0006:  ldc.i4     0x7fffffff
      IL_000b:  conv.i8
      IL_000c:  ldc.i8     0x3fffffff00000001
      IL_0015:  ldc.i4.0
      IL_0016:  call       bool JitTest_s_ldc_div_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_001b:  brtrue.s   IL_0022

      IL_001d:  br         IL_00ff

      IL_0022:  ldc.i8     0x100000000
      IL_002b:  ldc.i4     0x7fffffff
      IL_0030:  conv.i8
      IL_0031:  ldc.i8     0x7fffffff00000000
      IL_003a:  ldc.i4.0
      IL_003b:  call       bool JitTest_s_ldc_div_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_0040:  brtrue.s   IL_0047

      IL_0042:  br         IL_00ff

      IL_0047:  ldc.i8     0x3fffffffffffffff
      IL_0050:  ldc.i4.2
      IL_0051:  conv.i8
      IL_0052:  ldc.i8     0x7ffffffffffffffe
      IL_005b:  ldc.i4.0
      IL_005c:  call       bool JitTest_s_ldc_div_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_0061:  brtrue.s   IL_0068

      IL_0063:  br         IL_00ff

      IL_0068:  ldc.i4.m1
      IL_0069:  conv.i8
      IL_006a:  ldc.i4.s   -2
      IL_006c:  conv.i8
      IL_006d:  ldc.i4.2
      IL_006e:  conv.i8
      IL_006f:  ldc.i4.0
      IL_0070:  call       bool JitTest_s_ldc_div_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_0075:  brtrue.s   IL_007c

      IL_0077:  br         IL_00ff

      IL_007c:  ldc.i4     0x100000
      IL_0081:  conv.i8
      IL_0082:  ldc.i8     0x1000000000
      IL_008b:  ldc.i8     0x100000000000000
      IL_0094:  ldc.i4.0
      IL_0095:  call       bool JitTest_s_ldc_div_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_009a:  brtrue.s   IL_009e

      IL_009c:  br.s       IL_00ff

      IL_009e:  ldc.i4.2
      IL_009f:  conv.i8
      IL_00a0:  ldc.i4.0
      IL_00a1:  conv.i8
      IL_00a2:  ldc.i4.1
      IL_00a3:  conv.i8
      IL_00a4:  ldc.i4.0
      IL_00a5:  call       bool JitTest_s_ldc_div_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_00aa:  brtrue.s   IL_00ae

      IL_00ac:  br.s       IL_00ff

      IL_00ae:  ldc.i4.s   -2
      IL_00b0:  conv.i8
      IL_00b1:  ldc.i4.0
      IL_00b2:  conv.i8
      IL_00b3:  ldc.i4.1
      IL_00b4:  conv.i8
      IL_00b5:  ldc.i4.0
      IL_00b6:  call       bool JitTest_s_ldc_div_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_00bb:  brtrue.s   IL_00bf

      IL_00bd:  br.s       IL_00ff

      IL_00bf:  ldc.i4.m1
      IL_00c0:  conv.i8
      IL_00c1:  ldc.i8     0x8000000000000001
      IL_00ca:  ldc.i8     0x7fffffffffffffff
      IL_00d3:  ldc.i4.0
      IL_00d4:  call       bool JitTest_s_ldc_div_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_00d9:  brtrue.s   IL_00dd

      IL_00db:  br.s       IL_00ff

      IL_00dd:  ldc.i4.0
      IL_00de:  conv.i8
      IL_00df:  ldc.i4.0
      IL_00e0:  conv.i8
      IL_00e1:  ldc.i4     0x3e8
      IL_00e6:  conv.i8
      IL_00e7:  ldc.i4.1
      IL_00e8:  call       bool JitTest_s_ldc_div_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_00ed:  brtrue.s   IL_00f1

      IL_00ef:  br.s       IL_00ff

      IL_00f1:  ldstr      "Test passed"
      IL_00f6:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00fb:  ldc.i4    0x64
      IL_00fc:  stloc.0
      IL_00fd:  br.s       IL_010d

      IL_00ff:  ldstr      "Test failed"
      IL_0104:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0109:  ldc.i4.1
      IL_010a:  stloc.0
      IL_010b:  br.s       IL_010d

      IL_010d:  ldloc.0
      IL_010e:  ret
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

} // end of namespace 'JitTest_s_ldc_div_signed_il'

//*********** DISASSEMBLY COMPLETE ***********************
