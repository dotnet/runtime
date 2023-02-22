// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//testing .zeroinit
.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly zeroinit {}
.class private auto ansi beforefieldinit CL
       extends [mscorlib]System.Object
{
  .field public int32 'nop'
  .method public hidebysig specialname rtspecialname 
          instance void .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CL::.ctor

} // end of class CL

.class private sequential ansi sealed beforefieldinit VT
       extends [mscorlib]System.ValueType
{
  .field public int32 'nop'
} // end of class VT

.class public auto ZeroInit
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .zeroinit
    // Code size       569 (0x239)
    .maxstack  2
    .locals (bool V_0,
             bool V_1,
             char V_2,
             string V_3,
             float32 V_4,
             float64 V_5,
             int8 V_6,
             int16 V_7,
             int32 V_8,
             int64 V_9,
             native int V_10,
             native unsigned int V_11,
             unsigned int8 V_12,
             unsigned int16 V_13,
             unsigned int32 V_14,
             unsigned int64 V_15,
             object V_16,
             class CL V_17,
             valuetype VT V_18,
             int32 V_19)

    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0

    IL_005d:  ldloc.1
    IL_005e:  brfalse.s  IL_0072

    IL_0060:  ldstr      "b_locvar is {0}"
    IL_0065:  ldloc.1
    IL_0066:  box        [mscorlib]System.Boolean
    IL_006b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0070:  ldc.i4.0
    IL_0071:  stloc.0
    IL_0072:  ldloc.2
    IL_0073:  ldc.i4.s   0
    IL_0075:  beq.s      IL_0089

    IL_0077:  ldstr      "c_locvar is {0}"
    IL_007c:  ldloc.2
    IL_007d:  box        [mscorlib]System.Char
    IL_0082:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0087:  ldc.i4.0
    IL_0088:  stloc.0
    IL_0089:  ldloc.3
    IL_008a:  //ldstr      ""
    IL_008f:  //call       bool [mscorlib]System.String::op_Inequality(string,
              //                                                       string)
    IL_0094:  brfalse.s  IL_00a3

    IL_0096:  ldstr      "str_locvar is {0}"
    IL_009b:  ldloc.3
    IL_009c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_00a1:  ldc.i4.0
    IL_00a2:  stloc.0
    IL_00a3:  ldloc.s    V_4
    IL_00a5:  ldc.r4     0.
    IL_00aa:  beq.s      IL_00bf

    IL_00ac:  ldstr      "r4_locvar is {0}"
    IL_00b1:  ldloc.s    V_4
    IL_00b3:  box        [mscorlib]System.Single
    IL_00b8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_00bd:  ldc.i4.0
    IL_00be:  stloc.0
    IL_00bf:  ldloc.s    V_5
    IL_00c1:  ldc.r8     0.
    IL_00ca:  beq.s      IL_00df

    IL_00cc:  ldstr      "r8_locvar is {0}"
    IL_00d1:  ldloc.s    V_5
    IL_00d3:  box        [mscorlib]System.Double
    IL_00d8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_00dd:  ldc.i4.0
    IL_00de:  stloc.0
    IL_00df:  ldloc.s    V_6
    IL_00e1:  conv.i4
    IL_00e2:  brfalse.s  IL_00f7

    IL_00e4:  ldstr      "i1_locvar is {0}"
    IL_00e9:  ldloc.s    V_6
    IL_00eb:  box        [mscorlib]System.SByte
    IL_00f0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_00f5:  ldc.i4.0
    IL_00f6:  stloc.0
    IL_00f7:  ldloc.s    V_7
    IL_00f9:  brfalse.s  IL_010e

    IL_00fb:  ldstr      "i2_locvar is {0}"
    IL_0100:  ldloc.s    V_7
    IL_0102:  box        [mscorlib]System.Int16
    IL_0107:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_010c:  ldc.i4.0
    IL_010d:  stloc.0
    IL_010e:  ldloc.s    V_8
    IL_0110:  brfalse.s  IL_0125

    IL_0112:  ldstr      "i4_locvar is {0}"
    IL_0117:  ldloc.s    V_8
    IL_0119:  box        [mscorlib]System.Int32
    IL_011e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0123:  ldc.i4.0
    IL_0124:  stloc.0
    IL_0125:  ldloc.s    V_9
    IL_0127:  ldc.i4.0
    IL_0128:  conv.i8
    IL_0129:  beq.s      IL_013e

    IL_012b:  ldstr      "i8_locvar is {0}"
    IL_0130:  ldloc.s    V_9
    IL_0132:  box        [mscorlib]System.Int64
    IL_0137:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_013c:  ldc.i4.0
    IL_013d:  stloc.0
    IL_013e:  ldloc.s    V_10
    IL_0140:  ldsfld     native int [mscorlib]System.IntPtr::Zero
    IL_0145:  call       bool [mscorlib]System.IntPtr::op_Inequality(native int,
                                                                     native int)
    IL_014a:  brfalse.s  IL_015f

    IL_014c:  ldstr      "i_locvar is {0}"
    IL_0151:  ldloc.s    V_10
    IL_0153:  box        [mscorlib]System.IntPtr
    IL_0158:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_015d:  ldc.i4.0
    IL_015e:  stloc.0
    IL_015f:  ldloc.s    V_11
    IL_0161:  ldsfld     native unsigned int [mscorlib]System.UIntPtr::Zero
    IL_0166:  call       bool [mscorlib]System.UIntPtr::op_Inequality(native unsigned int,
                                                                      native unsigned int)
    IL_016b:  brfalse.s  IL_0180

    IL_016d:  ldstr      "u_locvar is {0}"
    IL_0172:  ldloc.s    V_11
    IL_0174:  box        [mscorlib]System.UIntPtr
    IL_0179:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_017e:  ldc.i4.0
    IL_017f:  stloc.0
    IL_0180:  ldloc.s    V_12
    IL_0182:  brfalse.s  IL_0197

    IL_0184:  ldstr      "u1_locvar is {0}"
    IL_0189:  ldloc.s    V_12
    IL_018b:  box        [mscorlib]System.Byte
    IL_0190:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0195:  ldc.i4.0
    IL_0196:  stloc.0
    IL_0197:  ldloc.s    V_13
    IL_0199:  conv.i4
    IL_019a:  brfalse.s  IL_01af

    IL_019c:  ldstr      "u2_locvar is {0}"
    IL_01a1:  ldloc.s    V_13
    IL_01a3:  box        [mscorlib]System.UInt16
    IL_01a8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_01ad:  ldc.i4.0
    IL_01ae:  stloc.0
    IL_01af:  ldloc.s    V_14
    IL_01b1:  brfalse.s  IL_01c6

    IL_01b3:  ldstr      "u4_locvar is {0}"
    IL_01b8:  ldloc.s    V_14
    IL_01ba:  box        [mscorlib]System.UInt32
    IL_01bf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_01c4:  ldc.i4.0
    IL_01c5:  stloc.0
    IL_01c6:  ldloc.s    V_15
    IL_01c8:  ldc.i4.0
    IL_01c9:  conv.i8
    IL_01ca:  beq.s      IL_01df

    IL_01cc:  ldstr      "u8_locvar is {0}"
    IL_01d1:  ldloc.s    V_15
    IL_01d3:  box        [mscorlib]System.UInt64
    IL_01d8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_01dd:  ldc.i4.0
    IL_01de:  stloc.0
    IL_01df:  ldloc.s    V_16
    IL_01e1:  brfalse.s  IL_01f1

    IL_01e3:  ldstr      "o_locvar is {0}"
    IL_01e8:  ldloc.s    V_16
    IL_01ea:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_01ef:  ldc.i4.0
    IL_01f0:  stloc.0
    IL_01f1:  ldloc.s    V_17
    IL_01f3:  brfalse.s  IL_0224

    IL_01f5:  ldstr      "cl_locvar is {0}"
    IL_01fa:  ldloc.s    V_17
    IL_01fc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0201:  ldc.i4.0
    IL_0202:  stloc.0

  IL_0224:  ldloca.s   V_18
  IL_0226:  ldfld      int32 VT::'nop'
  IL_022b:  brfalse.s  IL_0254
  IL_022d:  ldstr      "vt_locvar.nop is {0}"
  IL_0232:  ldloca.s   V_18
  IL_0234:  ldfld      int32 VT::'nop'
  IL_0239:  box        [mscorlib]System.Int32
  IL_023e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                object)
  IL_0243:  ldc.i4.0
  IL_0244:  stloc.0

    IL_0254:  ldloc.0
    IL_0255:  brtrue.s   IL_0266

    IL_0257:  ldstr      "FAILED"
    IL_025c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0261:  ldc.i4.1
    IL_0262:  stloc.s    V_19
    IL_0264:  br.s       IL_0276

    IL_0266:  ldstr      "PASSED"
    IL_026b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0270:  ldc.i4.s   100
    IL_0272:  stloc.s    V_19
    IL_0274:  br.s       IL_0276

    IL_0276:  ldloc.s    V_19
    IL_0278:  ret
  }

}
