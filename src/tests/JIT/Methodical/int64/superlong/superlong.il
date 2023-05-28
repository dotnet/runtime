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
// MVID: {571950B7-63D3-4968-948F-25114244E0B8}
.namespace JitTest_superlong_il
{
  .class value public auto ansi sealed superlong
         extends ['mscorlib']System.ValueType
  {
    .field private unsigned int64 lo
    .field private unsigned int64 hi
    .method private hidebysig instance void 
            Assign(value class JitTest_superlong_il.superlong v) il managed
    {
      // Code size       27 (0x1b)
      .maxstack  9
      			ldarg.0
      			dup
      			ldarga.s   v
      			ldfld      unsigned int64 JitTest_superlong_il.superlong::hi
      			stfld      unsigned int64 JitTest_superlong_il.superlong::hi
      			ldarga.s   v
      			ldfld      unsigned int64 JitTest_superlong_il.superlong::lo
      			stfld      unsigned int64 JitTest_superlong_il.superlong::lo
      			ret
    } // end of method 'superlong::Assign'

    .method private hidebysig static value class JitTest_superlong_il.superlong 
            'add'(value class JitTest_superlong_il.superlong op1,
                  value class JitTest_superlong_il.superlong op2) il managed
    {
      // Code size       101 (0x65)
      .maxstack  3
      .locals (value class JitTest_superlong_il.superlong V_0,
               value class JitTest_superlong_il.superlong V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    JitTest_superlong_il.superlong
      IL_0008:  ldloca.s   V_0
      IL_000a:  ldarga.s   op1
      IL_000c:  ldfld      unsigned int64 JitTest_superlong_il.superlong::hi
      IL_0011:  ldarga.s   op2
      IL_0013:  ldfld      unsigned int64 JitTest_superlong_il.superlong::hi
      IL_0018:  add.ovf.un
      IL_0019:  stfld      unsigned int64 JitTest_superlong_il.superlong::hi
      .try
      {
        IL_001e:  ldloca.s   V_0
        IL_0020:  ldarga.s   op1
        IL_0022:  ldfld      unsigned int64 JitTest_superlong_il.superlong::lo
        IL_0027:  ldarga.s   op2
        IL_0029:  ldfld      unsigned int64 JitTest_superlong_il.superlong::lo
        IL_002e:  add.ovf.un
        IL_002f:  stfld      unsigned int64 JitTest_superlong_il.superlong::lo
        IL_0034:  leave.s    IL_005f

      }  // end .try
      catch ['mscorlib']System.OverflowException 
      {
        IL_0036:  pop
        IL_0037:  ldloca.s   V_0
        IL_0039:  ldarga.s   op1
        IL_003b:  ldfld      unsigned int64 JitTest_superlong_il.superlong::lo
        IL_0040:  ldarga.s   op2
        IL_0042:  ldfld      unsigned int64 JitTest_superlong_il.superlong::lo
        IL_0047:  add
        IL_0048:  stfld      unsigned int64 JitTest_superlong_il.superlong::lo
        IL_004d:  ldloca.s   V_0
        IL_004f:  dup
        IL_0050:  ldfld      unsigned int64 JitTest_superlong_il.superlong::hi
        IL_0055:  ldc.i4.1
        IL_0056:  conv.i8
        IL_0057:  add.ovf.un
        IL_0058:  stfld      unsigned int64 JitTest_superlong_il.superlong::hi
        IL_005d:  leave.s    IL_005f

      }  // end handler
      IL_005f:  ldloc.0
      IL_0060:  stloc.1

      IL_0063:  ldloc.1
      IL_0064:  ret
    } // end of method 'superlong::add'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       305 (0x131)
      .maxstack  2
      .locals (value class JitTest_superlong_il.superlong V_0,
               value class JitTest_superlong_il.superlong V_1,
               value class JitTest_superlong_il.superlong V_2,
               int32 V_3)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    JitTest_superlong_il.superlong
      IL_0008:  ldloca.s   V_1
      IL_000a:  initobj    JitTest_superlong_il.superlong
      IL_0010:  ldloca.s   V_0
      IL_0012:  ldc.i8     0x8000000000000000
      IL_001b:  stfld      unsigned int64 JitTest_superlong_il.superlong::hi
      IL_0020:  ldloca.s   V_0
      IL_0022:  ldc.i4.0
      IL_0023:  conv.i8
      IL_0024:  stfld      unsigned int64 JitTest_superlong_il.superlong::lo
      IL_0029:  ldloca.s   V_1
      IL_002b:  ldc.i8     0x7fffffffffffffff
      IL_0034:  stfld      unsigned int64 JitTest_superlong_il.superlong::hi
      IL_0039:  ldloca.s   V_1
      IL_003b:  ldc.i4.m1
      IL_003c:  conv.i8
      IL_003d:  stfld      unsigned int64 JitTest_superlong_il.superlong::lo
      IL_0042:  ldloc.0
      IL_0043:  ldloc.1
      IL_0044:  call       value class JitTest_superlong_il.superlong JitTest_superlong_il.superlong::'add'(value class JitTest_superlong_il.superlong,
                                                                                  value class JitTest_superlong_il.superlong)
      IL_0049:  stloc.2
      IL_004a:  ldloca.s   V_2
      IL_004c:  ldfld      unsigned int64 JitTest_superlong_il.superlong::hi
      IL_0051:  ldc.i4.m1
      IL_0052:  conv.i8
      IL_0053:  bne.un.s   IL_0060

      IL_0055:  ldloca.s   V_2
      IL_0057:  ldfld      unsigned int64 JitTest_superlong_il.superlong::lo
      IL_005c:  ldc.i4.m1
      IL_005d:  conv.i8
      IL_005e:  beq.s      IL_0071

      IL_0060:  ldstr      "Failed."
      IL_0065:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_006a:  ldc.i4.1
      IL_006b:  stloc.3
      IL_006c:  br         IL_012f

      IL_0071:  ldloca.s   V_0
      IL_0073:  ldc.i8     0x8000000000000000
      IL_007c:  stfld      unsigned int64 JitTest_superlong_il.superlong::hi
      IL_0081:  ldloca.s   V_0
      IL_0083:  ldc.i4.0
      IL_0084:  conv.i8
      IL_0085:  stfld      unsigned int64 JitTest_superlong_il.superlong::lo
      IL_008a:  ldloca.s   V_1
      IL_008c:  ldloc.0
      IL_008d:  call       instance void JitTest_superlong_il.superlong::Assign(value class JitTest_superlong_il.superlong)
      .try
      {
        IL_0092:  ldloc.0
        IL_0093:  ldloc.1
        IL_0094:  call       value class JitTest_superlong_il.superlong JitTest_superlong_il.superlong::'add'(value class JitTest_superlong_il.superlong,
                                                                                    value class JitTest_superlong_il.superlong)
        IL_0099:  stloc.2
        IL_009a:  leave      IL_0121

      }  // end .try
      catch ['mscorlib']System.OverflowException 
      {
        IL_009f:  pop
        IL_00a0:  ldloca.s   V_0
        IL_00a2:  ldc.i8     0x1234567876543210
        IL_00ab:  stfld      unsigned int64 JitTest_superlong_il.superlong::hi
        IL_00b0:  ldloca.s   V_0
        IL_00b2:  ldc.i8     0xfdcba98789abcdef
        IL_00bb:  stfld      unsigned int64 JitTest_superlong_il.superlong::lo
        IL_00c0:  ldloca.s   V_1
        IL_00c2:  ldc.i8     0xedcba98789abcdee
        IL_00cb:  stfld      unsigned int64 JitTest_superlong_il.superlong::hi
        IL_00d0:  ldloca.s   V_1
        IL_00d2:  ldc.i8     0x1234567876543210
        IL_00db:  stfld      unsigned int64 JitTest_superlong_il.superlong::lo
        IL_00e0:  ldloc.0
        IL_00e1:  ldloc.1
        IL_00e2:  call       value class JitTest_superlong_il.superlong JitTest_superlong_il.superlong::'add'(value class JitTest_superlong_il.superlong,
                                                                                    value class JitTest_superlong_il.superlong)
        IL_00e7:  stloc.2
        IL_00e8:  ldloca.s   V_2
        IL_00ea:  ldfld      unsigned int64 JitTest_superlong_il.superlong::hi
        IL_00ef:  ldc.i4.m1
        IL_00f0:  conv.i8
        IL_00f1:  bne.un.s   IL_0105

        IL_00f3:  ldloca.s   V_2
        IL_00f5:  ldfld      unsigned int64 JitTest_superlong_il.superlong::lo
        IL_00fa:  ldc.i8     0xfffffffffffffff
        IL_0103:  beq.s      IL_0113

        IL_0105:  ldstr      "Failed (3)."
        IL_010a:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_010f:  ldc.i4.1
        IL_0110:  stloc.3
        IL_0111:  leave.s    IL_012f

        IL_0113:  ldstr      "Passed!"
        IL_0118:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_011d:  ldc.i4    0x64
        IL_011e:  stloc.3
        IL_011f:  leave.s    IL_012f

      }  // end handler
      IL_0121:  ldstr      "Failed (2)."
      IL_0126:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_012b:  ldc.i4.1
      IL_012c:  stloc.3
      IL_012d:  br.s       IL_012f

      IL_012f:  ldloc.3
      IL_0130:  ret
    } // end of method 'superlong::Main'

  } // end of class 'superlong'

} // end of namespace 'JitTest_superlong_il'

//*********** DISASSEMBLY COMPLETE ***********************
