// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 's_ldfld_mul'// as "ldfld_mul"
{
}
.assembly extern xunit.core {}
// MVID: {1194CBFB-CC76-4762-A87D-5001A75D57A4}
.namespace JitTest_s_ldfld_mul_signed_il
{
  .class public auto ansi Test
         extends ['mscorlib']System.Object
  {
    .field private int64 op1
    .field private int64 op2
    .method private hidebysig instance bool 
            check(int64 product,
                  bool overflow) il managed
    {
      // Code size       81 (0x51)
      .maxstack  3
      .locals (bool V_0)
      IL_0000:  ldstr      "Multiplying {0} and {1}..."
      IL_0005:  ldarg.0
      IL_0006:  ldfld     int64 JitTest_s_ldfld_mul_signed_il.Test::op1
      IL_000b:  box     ['mscorlib']System.Int64
      IL_0010:  ldarg.0
      IL_0011:  ldfld     int64 JitTest_s_ldfld_mul_signed_il.Test::op2
      IL_0016:  box     ['mscorlib']System.Int64
      IL_001b:  call       void [System.Console]System.Console::Write(class System.String,
                                                                  class System.Object,
                                                                  class System.Object)
      .try
      {
        IL_0020:  ldarg.0
        IL_0021:  ldfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op1
        IL_0026:  ldarg.0
        IL_0027:  ldfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op2
        IL_002c:  mul
        IL_002d:  ldarg.1
        IL_002e:  beq.s      IL_0034

        IL_0030:  ldc.i4.0
        IL_0031:  stloc.0
        IL_0032:  leave.s    IL_004f

        IL_0034:  call       void [System.Console]System.Console::WriteLine()
        IL_0039:  ldarg.2
        IL_003a:  ldc.i4.0
        IL_003b:  ceq
        IL_003d:  stloc.0
        IL_003e:  leave.s    IL_004f

      }  // end .try
      catch ['mscorlib']System.OverflowException 
      {
        IL_0040:  pop
        IL_0041:  ldstr      "overflow."
        IL_0046:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_004b:  ldarg.2
        IL_004c:  stloc.0
        IL_004d:  leave.s    IL_004f

      }  // end handler
      IL_004f:  ldloc.0
      IL_0050:  ret
    } // end of method 'Test::check'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       425 (0x1a9)
      .maxstack  3
      .locals (class JitTest_s_ldfld_mul_signed_il.Test V_0,
               int32 V_1)
      IL_0000:  newobj     instance void JitTest_s_ldfld_mul_signed_il.Test::.ctor()
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldc.i4     0x7fffffff
      IL_000c:  conv.i8
      IL_000d:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op1
      IL_0012:  ldloc.0
      IL_0013:  ldc.i4     0x7fffffff
      IL_0018:  conv.i8
      IL_0019:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op2
      IL_001e:  ldloc.0
      IL_001f:  ldc.i8     0x3fffffff00000001
      IL_0028:  ldc.i4.0
      IL_0029:  call       instance bool JitTest_s_ldfld_mul_signed_il.Test::check(int64,
                                                             bool)
      IL_002e:  brtrue.s   IL_0035

      IL_0030:  br         IL_0199

      IL_0035:  ldloc.0
      IL_0036:  ldc.i8     0x100000000
      IL_003f:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op1
      IL_0044:  ldloc.0
      IL_0045:  ldc.i4     0x7fffffff
      IL_004a:  conv.i8
      IL_004b:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op2
      IL_0050:  ldloc.0
      IL_0051:  ldc.i8     0x7fffffff00000000
      IL_005a:  ldc.i4.0
      IL_005b:  call       instance bool JitTest_s_ldfld_mul_signed_il.Test::check(int64,
                                                             bool)
      IL_0060:  brtrue.s   IL_0067

      IL_0062:  br         IL_0199

      IL_0067:  ldloc.0
      IL_0068:  ldc.i8     0x100000000
      IL_0071:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op1
      IL_0076:  ldloc.0
      IL_0077:  ldc.i8     0x100000000
      IL_0080:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op2
      IL_0085:  ldloc.0
      IL_0086:  ldc.i4.0
      IL_0087:  conv.i8
      IL_0088:  ldc.i4.0
      IL_0089:  call       instance bool JitTest_s_ldfld_mul_signed_il.Test::check(int64,
                                                             bool)
      IL_008e:  brtrue.s   IL_0095

      IL_0090:  br         IL_0199

      IL_0095:  ldloc.0
      IL_0096:  ldc.i8     0x3fffffffffffffff
      IL_009f:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op1
      IL_00a4:  ldloc.0
      IL_00a5:  ldc.i4.2
      IL_00a6:  conv.i8
      IL_00a7:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op2
      IL_00ac:  ldloc.0
      IL_00ad:  ldc.i8     0x7ffffffffffffffe
      IL_00b6:  ldc.i4.0
      IL_00b7:  call       instance bool JitTest_s_ldfld_mul_signed_il.Test::check(int64,
                                                             bool)
      IL_00bc:  brtrue.s   IL_00c3

      IL_00be:  br         IL_0199

      IL_00c3:  ldloc.0
      IL_00c4:  ldc.i4.m1
      IL_00c5:  conv.i8
      IL_00c6:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op1
      IL_00cb:  ldloc.0
      IL_00cc:  ldc.i4.s   -2
      IL_00ce:  conv.i8
      IL_00cf:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op2
      IL_00d4:  ldloc.0
      IL_00d5:  ldc.i4.2
      IL_00d6:  conv.i8
      IL_00d7:  ldc.i4.0
      IL_00d8:  call       instance bool JitTest_s_ldfld_mul_signed_il.Test::check(int64,
                                                             bool)
      IL_00dd:  brtrue.s   IL_00e4

      IL_00df:  br         IL_0199

      IL_00e4:  ldloc.0
      IL_00e5:  ldc.i4     0x100000
      IL_00ea:  conv.i8
      IL_00eb:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op1
      IL_00f0:  ldloc.0
      IL_00f1:  ldc.i8     0x1000000000
      IL_00fa:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op2
      IL_00ff:  ldloc.0
      IL_0100:  ldc.i8     0x100000000000000
      IL_0109:  ldc.i4.0
      IL_010a:  call       instance bool JitTest_s_ldfld_mul_signed_il.Test::check(int64,
                                                             bool)
      IL_010f:  brtrue.s   IL_0116

      IL_0111:  br         IL_0199

      IL_0116:  ldloc.0
      IL_0117:  ldc.i4.m1
      IL_0118:  conv.i8
      IL_0119:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op1
      IL_011e:  ldloc.0
      IL_011f:  ldc.i8     0x8000000000000001
      IL_0128:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op2
      IL_012d:  ldloc.0
      IL_012e:  ldc.i8     0x7fffffffffffffff
      IL_0137:  ldc.i4.0
      IL_0138:  call       instance bool JitTest_s_ldfld_mul_signed_il.Test::check(int64,
                                                             bool)
      IL_013d:  brtrue.s   IL_0141

      IL_013f:  br.s       IL_0199

      IL_0141:  ldloc.0
      IL_0142:  ldc.i4.s   -2
      IL_0144:  conv.i8
      IL_0145:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op1
      IL_014a:  ldloc.0
      IL_014b:  ldc.i8     0x8000000000000001
      IL_0154:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op2
      IL_0159:  ldloc.0
      IL_015a:  ldc.i4.s   -2
      IL_015c:  conv.i8
      IL_015d:  ldc.i4.0
      IL_015e:  call       instance bool JitTest_s_ldfld_mul_signed_il.Test::check(int64,
                                                             bool)
      IL_0163:  brtrue.s   IL_0167

      IL_0165:  br.s       IL_0199

      IL_0167:  ldloc.0
      IL_0168:  ldc.i4.2
      IL_0169:  conv.i8
      IL_016a:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op1
      IL_016f:  ldloc.0
      IL_0170:  ldc.i8     0x8000000000000001
      IL_0179:  stfld      int64 JitTest_s_ldfld_mul_signed_il.Test::op2
      IL_017e:  ldloc.0
      IL_017f:  ldc.i4.2
      IL_0180:  conv.i8
      IL_0181:  ldc.i4.0
      IL_0182:  call       instance bool JitTest_s_ldfld_mul_signed_il.Test::check(int64,
                                                             bool)
      IL_0187:  brtrue.s   IL_018b

      IL_0189:  br.s       IL_0199

      IL_018b:  ldstr      "Test passed"
      IL_0190:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0195:  ldc.i4    0x64
      IL_0196:  stloc.1
      IL_0197:  br.s       IL_01a7

      IL_0199:  ldstr      "Test failed"
      IL_019e:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_01a3:  ldc.i4.1
      IL_01a4:  stloc.1
      IL_01a5:  br.s       IL_01a7

      IL_01a7:  ldloc.1
      IL_01a8:  ret
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

} // end of namespace 'JitTest_s_ldfld_mul_signed_il'

//*********** DISASSEMBLY COMPLETE ***********************
