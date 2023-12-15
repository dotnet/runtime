// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME// as "ldc_mul"
{
}
.assembly extern xunit.core {}
// MVID: {DD9661FF-93A5-4664-8EAA-E345054EF8F6}
.namespace JitTest_s_ldc_mul_signed_il
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
      IL_0000:  ldstr      "Multiplying {0} and {1}..."
      IL_0005:  ldarg.s   op1
      IL_0007:  box     ['mscorlib']System.Int64
      IL_000c:  ldarg.s   op2
      IL_000e:  box     ['mscorlib']System.Int64
      IL_0013:  call       void [System.Console]System.Console::Write(class System.String,
                                                                  class System.Object,
                                                                  class System.Object)
      .try
      {
        IL_0018:  ldarg.0
        IL_0019:  ldarg.1
        IL_001a:  mul
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
      // Code size       242 (0xf2)
      .maxstack  4
      .locals (int32 V_0)
      IL_0000:  ldc.i4     0x7fffffff
      IL_0005:  conv.i8
      IL_0006:  ldc.i4     0x7fffffff
      IL_000b:  conv.i8
      IL_000c:  ldc.i8     0x3fffffff00000001
      IL_0015:  ldc.i4.0
      IL_0016:  call       bool JitTest_s_ldc_mul_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_001b:  brtrue.s   IL_0022

      IL_001d:  br         IL_00e2

      IL_0022:  ldc.i8     0x100000000
      IL_002b:  ldc.i4     0x7fffffff
      IL_0030:  conv.i8
      IL_0031:  ldc.i8     0x7fffffff00000000
      IL_003a:  ldc.i4.0
      IL_003b:  call       bool JitTest_s_ldc_mul_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_0040:  brtrue.s   IL_0047

      IL_0042:  br         IL_00e2

      IL_0047:  ldc.i8     0x100000000
      IL_0050:  ldc.i8     0x100000000
      IL_0059:  ldc.i4.0
      IL_005a:  conv.i8
      IL_005b:  ldc.i4.0
      IL_005c:  call       bool JitTest_s_ldc_mul_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_0061:  brtrue.s   IL_0065

      IL_0063:  br       IL_00e2

      IL_0065:  ldc.i8     0x3fffffffffffffff
      IL_006e:  ldc.i4.2
      IL_006f:  conv.i8
      IL_0070:  ldc.i8     0x7ffffffffffffffe
      IL_0079:  ldc.i4.0
      IL_007a:  call       bool JitTest_s_ldc_mul_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_007f:  brtrue.s   IL_0083

      IL_0081:  br       IL_00e2

      IL_0083:  ldc.i4.m1
      IL_0084:  conv.i8
      IL_0085:  ldc.i4.s   -2
      IL_0087:  conv.i8
      IL_0088:  ldc.i4.2
      IL_0089:  conv.i8
      IL_008a:  ldc.i4.0
      IL_008b:  call       bool JitTest_s_ldc_mul_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_0090:  brtrue.s   IL_0094

      IL_0092:  br       IL_00e2

      IL_0094:  ldc.i4     0x100000
      IL_0099:  conv.i8
      IL_009a:  ldc.i8     0x1000000000
      IL_00a3:  ldc.i8     0x100000000000000
      IL_00ac:  ldc.i4.0
      IL_00ad:  call       bool JitTest_s_ldc_mul_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_00b2:  brtrue.s   IL_00b6

      IL_00b4:  br       IL_00e2

      IL_00b6:  ldc.i4.m1
      IL_00b7:  conv.i8
      IL_00b8:  ldc.i8     0x8000000000000001
      IL_00c1:  ldc.i8     0x7fffffffffffffff
      IL_00ca:  ldc.i4.0
      IL_00cb:  call       bool JitTest_s_ldc_mul_signed_il.Test::check(int64,
                                                    int64,
                                                    int64,
                                                    bool)
      IL_00d0:  brtrue.s   IL_00d4

      IL_00d2:  br       IL_00e2

      IL_00d4:  ldstr      "Test passed"
      IL_00d9:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00de:  ldc.i4    0x64
      IL_00df:  stloc.0
      IL_00e0:  br       IL_00f0

      IL_00e2:  ldstr      "Test failed"
      IL_00e7:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00ec:  ldc.i4.1
      IL_00ed:  stloc.0
      IL_00ee:  br       IL_00f0

      IL_00f0:  ldloc.0
      IL_00f1:  ret
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

} // end of namespace 'JitTest_s_ldc_mul_signed_il'

//*********** DISASSEMBLY COMPLETE ***********************
