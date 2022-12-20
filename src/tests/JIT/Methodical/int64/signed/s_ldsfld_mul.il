// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 's_ldsfld_mul'// as "ldsfld_mul"
{
}
.assembly extern xunit.core {}
// MVID: {72B54BA1-0DAB-469F-8753-50AD9A199FE2}
.namespace JitTest_s_ldsfld_mul_signed_il
{
  .class public auto ansi Test
         extends ['mscorlib']System.Object
  {
    .field private static int64 op1
    .field private static int64 op2
    .method private hidebysig static bool 
            check(int64 product,
                  bool overflow) il managed
    {
      // Code size       77 (0x4d)
      .maxstack  3
      .locals (bool V_0)
      IL_0000:  ldstr      "Multiplying {0} and {1}..."
      IL_0005:  ldsfld    int64 JitTest_s_ldsfld_mul_signed_il.Test::op1
      IL_000a:  box     ['mscorlib']System.Int64
      IL_000f:  ldsfld    int64 JitTest_s_ldsfld_mul_signed_il.Test::op2
      IL_0014:  box     ['mscorlib']System.Int64
      IL_0019:  call       void [System.Console]System.Console::Write(class System.String,
                                                                  class System.Object,
                                                                  class System.Object)
      .try
      {
        IL_001e:  ldsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op1
        IL_0023:  ldsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op2
        IL_0028:  mul
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
      // Code size       389 (0x185)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldc.i4     0x7fffffff
      IL_0005:  conv.i8
      IL_0006:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op1
      IL_000b:  ldc.i4     0x7fffffff
      IL_0010:  conv.i8
      IL_0011:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op2
      IL_0016:  ldc.i8     0x3fffffff00000001
      IL_001f:  ldc.i4.0
      IL_0020:  call       bool JitTest_s_ldsfld_mul_signed_il.Test::check(int64,
                                                    bool)
      IL_0025:  brtrue.s   IL_002c

      IL_0027:  br         IL_0175

      IL_002c:  ldc.i8     0x100000000
      IL_0035:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op1
      IL_003a:  ldc.i4     0x7fffffff
      IL_003f:  conv.i8
      IL_0040:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op2
      IL_0045:  ldc.i8     0x7fffffff00000000
      IL_004e:  ldc.i4.0
      IL_004f:  call       bool JitTest_s_ldsfld_mul_signed_il.Test::check(int64,
                                                    bool)
      IL_0054:  brtrue.s   IL_005b

      IL_0056:  br         IL_0175

      IL_005b:  ldc.i8     0x100000000
      IL_0064:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op1
      IL_0069:  ldc.i8     0x100000000
      IL_0072:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op2
      IL_0077:  ldc.i4.0
      IL_0078:  conv.i8
      IL_0079:  ldc.i4.0
      IL_007a:  call       bool JitTest_s_ldsfld_mul_signed_il.Test::check(int64,
                                                    bool)
      IL_007f:  brtrue.s   IL_0086

      IL_0081:  br         IL_0175

      IL_0086:  ldc.i8     0x3fffffffffffffff
      IL_008f:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op1
      IL_0094:  ldc.i4.2
      IL_0095:  conv.i8
      IL_0096:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op2
      IL_009b:  ldc.i8     0x7ffffffffffffffe
      IL_00a4:  ldc.i4.0
      IL_00a5:  call       bool JitTest_s_ldsfld_mul_signed_il.Test::check(int64,
                                                    bool)
      IL_00aa:  brtrue.s   IL_00b1

      IL_00ac:  br         IL_0175

      IL_00b1:  ldc.i4.m1
      IL_00b2:  conv.i8
      IL_00b3:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op1
      IL_00b8:  ldc.i4.s   -2
      IL_00ba:  conv.i8
      IL_00bb:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op2
      IL_00c0:  ldc.i4.2
      IL_00c1:  conv.i8
      IL_00c2:  ldc.i4.0
      IL_00c3:  call       bool JitTest_s_ldsfld_mul_signed_il.Test::check(int64,
                                                    bool)
      IL_00c8:  brtrue.s   IL_00cf

      IL_00ca:  br         IL_0175

      IL_00cf:  ldc.i4     0x100000
      IL_00d4:  conv.i8
      IL_00d5:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op1
      IL_00da:  ldc.i8     0x1000000000
      IL_00e3:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op2
      IL_00e8:  ldc.i8     0x100000000000000
      IL_00f1:  ldc.i4.0
      IL_00f2:  call       bool JitTest_s_ldsfld_mul_signed_il.Test::check(int64,
                                                    bool)
      IL_00f7:  brtrue.s   IL_00fb

      IL_00f9:  br.s       IL_0175

      IL_00fb:  ldc.i4.m1
      IL_00fc:  conv.i8
      IL_00fd:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op1
      IL_0102:  ldc.i8     0x8000000000000001
      IL_010b:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op2
      IL_0110:  ldc.i8     0x7fffffffffffffff
      IL_0119:  ldc.i4.0
      IL_011a:  call       bool JitTest_s_ldsfld_mul_signed_il.Test::check(int64,
                                                    bool)
      IL_011f:  brtrue.s   IL_0123

      IL_0121:  br.s       IL_0175

      IL_0123:  ldc.i4.s   -2
      IL_0125:  conv.i8
      IL_0126:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op1
      IL_012b:  ldc.i8     0x8000000000000001
      IL_0134:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op2
      IL_0139:  ldc.i4.s   -2
      IL_013b:  conv.i8
      IL_013c:  ldc.i4.0
      IL_013d:  call       bool JitTest_s_ldsfld_mul_signed_il.Test::check(int64,
                                                    bool)
      IL_0142:  brtrue.s   IL_0146

      IL_0144:  br.s       IL_0175

      IL_0146:  ldc.i4.2
      IL_0147:  conv.i8
      IL_0148:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op1
      IL_014d:  ldc.i8     0x8000000000000001
      IL_0156:  stsfld     int64 JitTest_s_ldsfld_mul_signed_il.Test::op2
      IL_015b:  ldc.i4.2
      IL_015c:  conv.i8
      IL_015d:  ldc.i4.0
      IL_015e:  call       bool JitTest_s_ldsfld_mul_signed_il.Test::check(int64,
                                                    bool)
      IL_0163:  brtrue.s   IL_0167

      IL_0165:  br.s       IL_0175

      IL_0167:  ldstr      "Test passed"
      IL_016c:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0171:  ldc.i4    0x64
      IL_0172:  stloc.0
      IL_0173:  br.s       IL_0183

      IL_0175:  ldstr      "Test failed"
      IL_017a:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_017f:  ldc.i4.1
      IL_0180:  stloc.0
      IL_0181:  br.s       IL_0183

      IL_0183:  ldloc.0
      IL_0184:  ret
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

} // end of namespace 'JitTest_s_ldsfld_mul_signed_il'

//*********** DISASSEMBLY COMPLETE ***********************
