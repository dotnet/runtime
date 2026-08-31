// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { }
.assembly GitHub_20799 { }
.assembly extern xunit.core {}

// GitHub 20799: a bug is reproduced when
// 1) a method requires GSSecurityCookie (e.g. when allocating unsafe buffer using localloc)
// 2) one of the method parameters is "vulnerable" and requires a shadow variable to be created
//    and used instead of the original parameter variable
// 3) the method parameter has small signed integer type (i.e. int8 or int16)
// 4) the value of the parameter is converted to unsigned type (e.g. uint8 or uint16)
//
// For example, these IL patterns are morphed by JIT into a GT_LCL_FLD(uint8, arg1) node
// * conv.u1(ldfld(ldarga arg1))
// * conv.u1(ldind.i1(ldarga arg1))
//
// In the process of replacement of arg1 with a shadow variable the node type was also changed to TYP_INT and,
// as a consequence, 'mov dstReg, dword ptr [shadowVarLoc]' was generated for such conversion
// instead of 'movzx dstReg, byte ptr [shadowVarLoc]'.

.class private sequential ansi sealed beforefieldinit GitHub_20799.SByte
       extends [System.Runtime]System.ValueType
{
  .field public int8 m_value
}

.class private sequential ansi sealed beforefieldinit GitHub_20799.Int16
       extends [System.Runtime]System.ValueType
{
  .field public int16 m_value
}

.class public auto ansi beforefieldinit GitHub_20799.Program
       extends [System.Runtime]System.Object
{
  .method private hidebysig static void  Use(bool arg1) cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ret
  }

  .method private hidebysig static void  Use(int32 arg1) cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ret
  }

  .method private hidebysig static int8  Nop(int8 arg1) cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ret
  }

  .method private hidebysig static int16  Nop(int16 arg1) cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ret
  }

  .method private hidebysig static int32
          Ldfld(int8 arg1, int8 arg2, int8 arg3, int8 arg4, int8 arg5, int8 arg6, int8 arg7, int8 arg8, int8 arg9) cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldc.i4.4
    IL_0001:  conv.u

    // Unsafe buffer would require GSSecurityCookie
    IL_0002:  localloc
    IL_0004:  ldind.i4
    IL_0005:  call       void GitHub_20799.Program::Use(int32)
    IL_000a:  ldarga.s   arg9
    IL_000c:  ldarg.s    arg9

    // arg9 becomes a "vulnerable" parameter
    IL_000e:  box        [System.Runtime]System.SByte
    IL_0013:  call       instance bool [System.Runtime]System.SByte::Equals(object)
    IL_0018:  call       void GitHub_20799.Program::Use(bool)

    IL_001d:  ldarga.s   arg1
    IL_001f:  ldfld      int8 GitHub_20799.SByte::m_value
    IL_0024:  conv.u1

    // A shadow copy of arg9 is being used instead
    IL_0025:  ldarga.s   arg9
    IL_0027:  ldfld      int8 GitHub_20799.SByte::m_value
    IL_002c:  conv.u1
    IL_002d:  beq.s      IL_0031

    IL_002f:  ldc.i4.1
    IL_0030:  ret

    IL_0031:  ldc.i4.s   100
    IL_0033:  ret
  }

  .method private hidebysig static int32
          Ldind(int8 arg1, int8 arg2, int8 arg3, int8 arg4, int8 arg5, int8 arg6, int8 arg7, int8 arg8, int8 arg9) cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldc.i4.4
    IL_0001:  conv.u

    // Unsafe buffer would require GSSecurityCookie
    IL_0002:  localloc
    IL_0004:  ldind.i4
    IL_0005:  call       void GitHub_20799.Program::Use(int32)
    IL_000a:  ldarga.s   arg9
    IL_000c:  ldarg.s    arg9

    // arg9 becomes a "vulnerable" parameter
    IL_000e:  box        [System.Runtime]System.SByte
    IL_0013:  call       instance bool [System.Runtime]System.SByte::Equals(object)
    IL_0018:  call       void GitHub_20799.Program::Use(bool)

    IL_001d:  ldarga.s   arg1
    IL_001f:  ldind.i1
    IL_0020:  conv.u1

    // A shadow copy of arg9 is being used instead
    IL_0021:  ldarga.s   arg9
    IL_0023:  ldind.i1
    IL_0024:  conv.u1
    IL_0025:  beq.s      IL_0029

    IL_0027:  ldc.i4.1
    IL_0028:  ret

    IL_0029:  ldc.i4.s   100
    IL_002b:  ret
  }

  .method private hidebysig static int32
          Starg(int8 arg1, int8 arg2, int8 arg3, int8 arg4, int8 arg5, int8 arg6, int8 arg7, int8 arg8, int8 arg9) cil managed noinlining
  {
    .maxstack  8

    IL_0000:  ldc.i4.4
    IL_0001:  conv.u

    // Unsafe buffer would require GSSecurityCookie
    IL_0002:  localloc
    IL_0004:  ldind.i4
    IL_0005:  call       void GitHub_20799.Program::Use(int32)
    IL_000a:  ldarga.s   arg9
    IL_000c:  ldarg.s    arg9

    // arg9 becomes a "vulnerable" parameter
    IL_000e:  box        [System.Runtime]System.SByte
    IL_0013:  call       instance bool [System.Runtime]System.SByte::Equals(object)
    IL_0018:  call       void GitHub_20799.Program::Use(bool)

    IL_001d:  ldarg.0

    // This is needed to not "contaminate" arg1 with "vulnerability" of arg9
    IL_001e:  call       int8 GitHub_20799.Program::Nop(int8)

    // A shadow copy of arg9 is being used instead
    IL_0023:  starg.s    arg9
    IL_0025:  ldarg.0
    IL_0026:  ldarg.s    arg9
    IL_0028:  beq.s      IL_002c

    IL_002a:  ldc.i4.1
    IL_002b:  ret

    IL_002c:  ldc.i4.s   100
    IL_002e:  ret
  }

  .method private hidebysig static int32
          Stind(int8 arg1, int8 arg2, int8 arg3, int8 arg4, int8 arg5, int8 arg6, int8 arg7, int8 arg8, int8 arg9) cil managed noinlining
  {
    .maxstack  8

    IL_0000:  ldc.i4.4
    IL_0001:  conv.u

    // Unsafe buffer would require GSSecurityCookie
    IL_0002:  localloc
    IL_0004:  ldind.i4
    IL_0005:  call       void GitHub_20799.Program::Use(int32)
    IL_000a:  ldarga.s   arg9
    IL_000c:  ldarg.s    arg9

    // arg9 becomes a "vulnerable" parameter
    IL_000e:  box        [System.Runtime]System.SByte
    IL_0013:  call       instance bool [System.Runtime]System.SByte::Equals(object)
    IL_0018:  call       void GitHub_20799.Program::Use(bool)

    // A shadow copy of arg9 is being used instead
    IL_001d:  ldarga.s   arg9
    IL_001f:  ldarg.0

    // This is needed to not "contaminate" arg1 with "vulnerability" of arg9
    IL_0020:  call       int8 GitHub_20799.Program::Nop(int8)
    IL_0025:  stind.i1
    IL_0026:  ldarg.0
    IL_0027:  ldarg.s    arg9
    IL_0029:  beq.s      IL_002d

    IL_002b:  ldc.i4.1
    IL_002c:  ret

    IL_002d:  ldc.i4.s   100
    IL_002f:  ret
  }

  .method private hidebysig static int32
          Ldfld(int16 arg1, int16 arg2, int16 arg3, int16 arg4, int16 arg5, int16 arg6, int16 arg7, int16 arg8, int16 arg9) cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldc.i4.4
    IL_0001:  conv.u

    // Unsafe buffer would require GSSecurityCookie
    IL_0002:  localloc
    IL_0004:  ldind.i4
    IL_0005:  call       void GitHub_20799.Program::Use(int32)
    IL_000a:  ldarga.s   arg9
    IL_000c:  ldarg.0

    // arg9 becomes a "vulnerable" parameter
    IL_000d:  box        [System.Runtime]System.Int16
    IL_0012:  call       instance bool [System.Runtime]System.Int16::Equals(object)
    IL_0017:  call       void GitHub_20799.Program::Use(bool)

    IL_001c:  ldarga.s   arg1
    IL_001e:  ldfld      int16 GitHub_20799.Int16::m_value
    IL_0023:  conv.u2

    // A shadow copy of arg9 is being used instead
    IL_0024:  ldarga.s   arg9
    IL_0026:  ldfld      int16 GitHub_20799.Int16::m_value
    IL_002b:  conv.u2
    IL_002c:  beq.s      IL_0030

    IL_002e:  ldc.i4.1
    IL_002f:  ret

    IL_0030:  ldc.i4.s   100
    IL_0032:  ret
  }

   .method private hidebysig static int32
          Ldind(int16 arg1, int16 arg2, int16 arg3, int16 arg4, int16 arg5, int16 arg6, int16 arg7, int16 arg8, int16 arg9) cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldc.i4.4
    IL_0001:  conv.u

    // Unsafe buffer would require GSSecurityCookie
    IL_0002:  localloc
    IL_0004:  ldind.i4
    IL_0005:  call       void GitHub_20799.Program::Use(int32)
    IL_000a:  ldarga.s   arg9
    IL_000c:  ldarg.0

    // arg9 becomes a "vulnerable" parameter
    IL_000d:  box        [System.Runtime]System.Int16
    IL_0012:  call       instance bool [System.Runtime]System.Int16::Equals(object)
    IL_0017:  call       void GitHub_20799.Program::Use(bool)

    IL_001c:  ldarga.s   arg1
    IL_001e:  ldind.i2
    IL_001f:  conv.u2

    // A shadow copy of arg9 is being used instead
    IL_0020:  ldarga.s   arg9
    IL_0022:  ldind.i2
    IL_0023:  conv.u2
    IL_0024:  beq.s      IL_0028

    IL_0026:  ldc.i4.1
    IL_0027:  ret

    IL_0028:  ldc.i4.s   100
    IL_002a:  ret
  }


  .method private hidebysig static int32
          Starg(int16 arg1, int16 arg2, int16 arg3, int16 arg4, int16 arg5, int16 arg6, int16 arg7, int16 arg8, int16 arg9) cil managed noinlining
  {
    .maxstack  8

    IL_0000:  ldc.i4.4
    IL_0001:  conv.u

    // Unsafe buffer would require GSSecurityCookie
    IL_0002:  localloc
    IL_0004:  ldind.i4
    IL_0005:  call       void GitHub_20799.Program::Use(int32)
    IL_000a:  ldarga.s   arg9
    IL_000c:  ldarg.s    arg9

    // arg9 becomes a "vulnerable" parameter
    IL_000e:  box        [System.Runtime]System.Int16
    IL_0013:  call       instance bool [System.Runtime]System.Int16::Equals(object)
    IL_0018:  call       void GitHub_20799.Program::Use(bool)

    IL_001d:  ldarg.0

    // This is needed to not "contaminate" arg1 with "vulnerability" of arg9
    IL_001e:  call       int16 GitHub_20799.Program::Nop(int16)

    // A shadow copy of arg9 is being used instead
    IL_0023:  starg.s    arg9
    IL_0025:  ldarg.0
    IL_0026:  ldarg.s    arg9
    IL_0028:  beq.s      IL_002c

    IL_002a:  ldc.i4.1
    IL_002b:  ret

    IL_002c:  ldc.i4.s   100
    IL_002e:  ret
  }

  .method private hidebysig static int32
          Stind(int16 arg1, int16 arg2, int16 arg3, int16 arg4, int16 arg5, int16 arg6, int16 arg7, int16 arg8, int16 arg9) cil managed noinlining
  {
    .maxstack  8

    IL_0000:  ldc.i4.4
    IL_0001:  conv.u

    // Unsafe buffer would require GSSecurityCookie
    IL_0002:  localloc
    IL_0004:  ldind.i4
    IL_0005:  call       void GitHub_20799.Program::Use(int32)
    IL_000a:  ldarga.s   arg9
    IL_000c:  ldarg.s    arg9

    // arg9 becomes a "vulnerable" parameter
    IL_000e:  box        [System.Runtime]System.Int16
    IL_0013:  call       instance bool [System.Runtime]System.Int16::Equals(object)
    IL_0018:  call       void GitHub_20799.Program::Use(bool)

    // A shadow copy of arg9 is being used instead
    IL_001d:  ldarga.s   arg9
    IL_001f:  ldarg.0

    // This is needed to not "contaminate" arg1 with "vulnerability" of arg9
    IL_0020:  call       int16 GitHub_20799.Program::Nop(int16)
    IL_0025:  stind.i2
    IL_0026:  ldarg.0
    IL_0027:  ldarg.s    arg9
    IL_0029:  beq.s      IL_002d

    IL_002b:  ldc.i4.1
    IL_002c:  ret

    IL_002d:  ldc.i4.s   100
    IL_002f:  ret
  }

  .method public hidebysig static int32
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    .maxstack  16
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.s   100
    IL_0002:  stloc.0
    IL_0003:  ldc.i4.s   -127
    IL_0005:  ldc.i4.s   -127
    IL_0007:  ldc.i4.s   -127
    IL_0009:  ldc.i4.s   -127
    IL_000b:  ldc.i4.s   -127
    IL_000d:  ldc.i4.s   -127
    IL_000f:  ldc.i4.s   -127
    IL_0011:  ldc.i4.s   -127
    IL_0013:  ldc.i4.s   -127
    IL_0015:  call       int32 GitHub_20799.Program::Ldfld(int8, int8, int8, int8, int8, int8, int8, int8, int8)
    IL_001a:  ldc.i4.s   100
    IL_001c:  beq.s      IL_002a

    IL_001e:  ldstr      "FAILED: ldfld sbyte"
    IL_0023:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0028:  ldc.i4.1
    IL_0029:  stloc.0
    IL_002a:  ldc.i4.s   -127
    IL_002c:  ldc.i4.s   -127
    IL_002e:  ldc.i4.s   -127
    IL_0030:  ldc.i4.s   -127
    IL_0032:  ldc.i4.s   -127
    IL_0034:  ldc.i4.s   -127
    IL_0036:  ldc.i4.s   -127
    IL_0038:  ldc.i4.s   -127
    IL_003a:  ldc.i4.s   -127
    IL_003c:  call       int32 GitHub_20799.Program::Ldind(int8, int8, int8, int8, int8, int8, int8, int8, int8)
    IL_0041:  ldc.i4.s   100
    IL_0043:  beq.s      IL_0051

    IL_0045:  ldstr      "FAILED: ldind sbyte"
    IL_004a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004f:  ldc.i4.1
    IL_0050:  stloc.0
    IL_0051:  ldc.i4.s   -127
    IL_0053:  ldc.i4.s   -127
    IL_0055:  ldc.i4.s   -127
    IL_0057:  ldc.i4.s   -127
    IL_0059:  ldc.i4.s   -127
    IL_005b:  ldc.i4.s   -127
    IL_005d:  ldc.i4.s   -127
    IL_005f:  ldc.i4.s   -127
    IL_0061:  ldc.i4.s   -127
    IL_0063:  call       int32 GitHub_20799.Program::Starg(int8, int8, int8, int8, int8, int8, int8, int8, int8)
    IL_0068:  ldc.i4.s   100
    IL_006a:  beq.s      IL_0078

    IL_006c:  ldstr      "FAILED: starg sbyte"
    IL_0071:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0076:  ldc.i4.1
    IL_0077:  stloc.0
    IL_0078:  ldc.i4.s   -127
    IL_007a:  ldc.i4.s   -127
    IL_007c:  ldc.i4.s   -127
    IL_007e:  ldc.i4.s   -127
    IL_0080:  ldc.i4.s   -127
    IL_0082:  ldc.i4.s   -127
    IL_0084:  ldc.i4.s   -127
    IL_0086:  ldc.i4.s   -127
    IL_0088:  ldc.i4.s   -127
    IL_008a:  call       int32 GitHub_20799.Program::Stind(int8, int8, int8, int8, int8, int8, int8, int8, int8)
    IL_008f:  ldc.i4.s   100
    IL_0091:  beq.s      IL_009f

    IL_0093:  ldstr      "FAILED: stind sbyte"
    IL_0098:  call       void [System.Console]System.Console::WriteLine(string)
    IL_009d:  ldc.i4.1
    IL_009e:  stloc.0
    IL_009f:  ldc.i4     -20799
    IL_00a4:  ldc.i4     -20799
    IL_00a9:  ldc.i4     -20799
    IL_00ae:  ldc.i4     -20799
    IL_00b3:  ldc.i4     -20799
    IL_00b8:  ldc.i4     -20799
    IL_00bd:  ldc.i4     -20799
    IL_00c2:  ldc.i4     -20799
    IL_00c7:  ldc.i4     -20799
    IL_00cc:  call       int32 GitHub_20799.Program::Ldfld(int16, int16, int16, int16, int16, int16, int16, int16, int16)
    IL_00d1:  ldc.i4.s   100
    IL_00d3:  beq.s      IL_00e1

    IL_00d5:  ldstr      "FAILED: ldfld short"
    IL_00da:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00df:  ldc.i4.1
    IL_00e0:  stloc.0
    IL_00e1:  ldc.i4     -20799
    IL_00e6:  ldc.i4     -20799
    IL_00eb:  ldc.i4     -20799
    IL_00f0:  ldc.i4     -20799
    IL_00f5:  ldc.i4     -20799
    IL_00fa:  ldc.i4     -20799
    IL_00ff:  ldc.i4     -20799
    IL_0104:  ldc.i4     -20799
    IL_0109:  ldc.i4     -20799
    IL_010e:  call       int32 GitHub_20799.Program::Ldind(int16, int16, int16, int16, int16, int16, int16, int16, int16)
    IL_0113:  ldc.i4.s   100
    IL_0115:  beq.s      IL_0123

    IL_0117:  ldstr      "FAILED: ldind short"
    IL_011c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0121:  ldc.i4.1
    IL_0122:  stloc.0
    IL_0123:  ldc.i4     -20799
    IL_0128:  ldc.i4     -20799
    IL_012d:  ldc.i4     -20799
    IL_0132:  ldc.i4     -20799
    IL_0137:  ldc.i4     -20799
    IL_013c:  ldc.i4     -20799
    IL_0141:  ldc.i4     -20799
    IL_0146:  ldc.i4     -20799
    IL_014b:  ldc.i4     -20799
    IL_0150:  call       int32 GitHub_20799.Program::Starg(int16, int16, int16, int16, int16, int16, int16, int16, int16)
    IL_0155:  ldc.i4.s   100
    IL_0157:  beq.s      IL_0165

    IL_0159:  ldstr      "FAILED: starg short"
    IL_015e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0163:  ldc.i4.1
    IL_0164:  stloc.0
    IL_0165:  ldc.i4     -20799
    IL_016a:  ldc.i4     -20799
    IL_016f:  ldc.i4     -20799
    IL_0174:  ldc.i4     -20799
    IL_0179:  ldc.i4     -20799
    IL_017e:  ldc.i4     -20799
    IL_0183:  ldc.i4     -20799
    IL_0188:  ldc.i4     -20799
    IL_018d:  ldc.i4     -20799
    IL_0192:  call       int32 GitHub_20799.Program::Stind(int16, int16, int16, int16, int16, int16, int16, int16, int16)
    IL_0197:  ldc.i4.s   100
    IL_0199:  beq.s      IL_01a7

    IL_019b:  ldstr      "FAILED: stind short"
    IL_01a0:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01a5:  ldc.i4.1
    IL_01a6:  stloc.0
    IL_01a7:  ldloc.0
    IL_01a8:  ldc.i4.s   100
    IL_01aa:  bne.un.s   IL_01b6

    IL_01ac:  ldstr      "PASS"
    IL_01b1:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01b6:  ldloc.0
    IL_01b7:  ret
  }

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  }
}
