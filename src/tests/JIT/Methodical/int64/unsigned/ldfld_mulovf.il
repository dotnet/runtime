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
// MVID: {5E7A07A8-98C6-40F0-BD60-5BEF6B39E28B}
.namespace JitTest_ldfld_mulovf_unsigned_il
{
  .class public auto ansi Test
         extends ['mscorlib']System.Object
  {
    .field private unsigned int64 op1
    .field private unsigned int64 op2
    .method private hidebysig instance bool 
            check(unsigned int64 product,
                  bool overflow) il managed
    {
      // Code size       81 (0x51)
      .maxstack  3
      .locals (bool V_0)
      IL_0000:  ldstr      "Multiplying {0} and {1}..."
      IL_0005:  ldarg.0
      IL_0006:  ldfld     unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op1
      IL_000b:  box     ['mscorlib']System.UInt64
      IL_0010:  ldarg.0
      IL_0011:  ldfld     unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op2
      IL_0016:  box     ['mscorlib']System.UInt64
      IL_001b:  call       void [System.Console]System.Console::Write(class System.String,
                                                                  class System.Object,
                                                                  class System.Object)
      .try
      {
        IL_0020:  ldarg.0
        IL_0021:  ldfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op1
        IL_0026:  ldarg.0
        IL_0027:  ldfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op2
        IL_002c:  mul.ovf.un
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
      // Code size       287 (0x11f)
      .maxstack  3
      .locals (class JitTest_ldfld_mulovf_unsigned_il.Test V_0,
               int32 V_1)
      IL_0000:  newobj     instance void JitTest_ldfld_mulovf_unsigned_il.Test::.ctor()
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldc.i4.m1
      IL_0008:  conv.u8
      IL_0009:  stfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op1
      IL_000e:  ldloc.0
      IL_000f:  ldc.i4.m1
      IL_0010:  conv.u8
      IL_0011:  stfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op2
      IL_0016:  ldloc.0
      IL_0017:  ldc.i8     0xfffffffe00000001
      IL_0020:  ldc.i4.0
      IL_0021:  call       instance bool JitTest_ldfld_mulovf_unsigned_il.Test::check(unsigned int64,
                                                             bool)
      IL_0026:  brtrue.s   IL_002d

      IL_0028:  br         IL_010f

      IL_002d:  ldloc.0
      IL_002e:  ldc.i8     0x100000000
      IL_0037:  stfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op1
      IL_003c:  ldloc.0
      IL_003d:  ldc.i4.m1
      IL_003e:  conv.u8
      IL_003f:  stfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op2
      IL_0044:  ldloc.0
      IL_0045:  ldc.i8     0xffffffff00000000
      IL_004e:  ldc.i4.0
      IL_004f:  call       instance bool JitTest_ldfld_mulovf_unsigned_il.Test::check(unsigned int64,
                                                             bool)
      IL_0054:  brtrue.s   IL_005b

      IL_0056:  br         IL_010f

      IL_005b:  ldloc.0
      IL_005c:  ldc.i8     0x100000000
      IL_0065:  stfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op1
      IL_006a:  ldloc.0
      IL_006b:  ldc.i8     0x100000000
      IL_0074:  stfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op2
      IL_0079:  ldloc.0
      IL_007a:  ldc.i4.0
      IL_007b:  conv.i8
      IL_007c:  ldc.i4.1
      IL_007d:  call       instance bool JitTest_ldfld_mulovf_unsigned_il.Test::check(unsigned int64,
                                                             bool)
      IL_0082:  brtrue.s   IL_0089

      IL_0084:  br         IL_010f

      IL_0089:  ldloc.0
      IL_008a:  ldc.i8     0x7fffffffffffffff
      IL_0093:  stfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op1
      IL_0098:  ldloc.0
      IL_0099:  ldc.i4.2
      IL_009a:  conv.i8
      IL_009b:  stfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op2
      IL_00a0:  ldloc.0
      IL_00a1:  ldc.i4.s   -2
      IL_00a3:  conv.i8
      IL_00a4:  ldc.i4.0
      IL_00a5:  call       instance bool JitTest_ldfld_mulovf_unsigned_il.Test::check(unsigned int64,
                                                             bool)
      IL_00aa:  brtrue.s   IL_00ae

      IL_00ac:  br.s       IL_010f

      IL_00ae:  ldloc.0
      IL_00af:  ldc.i8     0x8000000000000000
      IL_00b8:  stfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op1
      IL_00bd:  ldloc.0
      IL_00be:  ldc.i4.2
      IL_00bf:  conv.i8
      IL_00c0:  stfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op2
      IL_00c5:  ldloc.0
      IL_00c6:  ldc.i4.0
      IL_00c7:  conv.i8
      IL_00c8:  ldc.i4.1
      IL_00c9:  call       instance bool JitTest_ldfld_mulovf_unsigned_il.Test::check(unsigned int64,
                                                             bool)
      IL_00ce:  brtrue.s   IL_00d2

      IL_00d0:  br.s       IL_010f

      IL_00d2:  ldloc.0
      IL_00d3:  ldc.i4     0x100000
      IL_00d8:  conv.i8
      IL_00d9:  stfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op1
      IL_00de:  ldloc.0
      IL_00df:  ldc.i8     0x1000000000
      IL_00e8:  stfld      unsigned int64 JitTest_ldfld_mulovf_unsigned_il.Test::op2
      IL_00ed:  ldloc.0
      IL_00ee:  ldc.i8     0x100000000000000
      IL_00f7:  ldc.i4.0
      IL_00f8:  call       instance bool JitTest_ldfld_mulovf_unsigned_il.Test::check(unsigned int64,
                                                             bool)
      IL_00fd:  brtrue.s   IL_0101

      IL_00ff:  br.s       IL_010f

      IL_0101:  ldstr      "Test passed"
      IL_0106:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_010b:  ldc.i4    0x64
      IL_010c:  stloc.1
      IL_010d:  br.s       IL_011d

      IL_010f:  ldstr      "Test failed"
      IL_0114:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0119:  ldc.i4.1
      IL_011a:  stloc.1
      IL_011b:  br.s       IL_011d

      IL_011d:  ldloc.1
      IL_011e:  ret
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

} // end of namespace 'JitTest_ldfld_mulovf_unsigned_il'

//*********** DISASSEMBLY COMPLETE ***********************
