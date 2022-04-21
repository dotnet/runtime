// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
}
.assembly format// as "format"
{
}
.assembly extern xunit.core {}
.namespace JitTest_format_il
{
  .class public auto ansi sealed PlatformID
         extends [mscorlib]System.Enum
  {
    .field public specialname rtspecialname int32 value__
    .field public static literal valuetype JitTest_format_il.PlatformID MacOSX = int32(0x00000006)
    .field public static literal valuetype JitTest_format_il.PlatformID Unix = int32(0x00000004)
    .field public static literal valuetype JitTest_format_il.PlatformID Win32NT = int32(0x00000002)
    .field public static literal valuetype JitTest_format_il.PlatformID Win32S = int32(0x00000000)
    .field public static literal valuetype JitTest_format_il.PlatformID Win32Windows = int32(0x00000001)
    .field public static literal valuetype JitTest_format_il.PlatformID WinCE = int32(0x00000003)
    .field public static literal valuetype JitTest_format_il.PlatformID Xbox = int32(0x00000005)
  } // end of class JitTest_format_il.PlatformID
  .class value private auto ansi serializable sealed Mood
         extends [mscorlib]System.Enum
  {
    .field public specialname rtspecialname int32 value__
    .field public static literal value class JitTest_format_il.Mood good = int32(0x00000000)
    .field public static literal value class JitTest_format_il.Mood bad = int32(0x00000001)
    .field public static literal value class JitTest_format_il.Mood worse = int32(0x00000002)
  } // end of class Mood

  .class public auto ansi TestClass
         extends [mscorlib]System.Object
  {
    .field private int32 m_d
    .field private static unsigned int32 m_u
    .field private int64 m_l
    .field private static unsigned int64 m_ul
    .field private float32 m_f
    .field private float64 m_dbl
    .field private bool m_b
    .field private static value class [mscorlib]System.DateTime m_t
    .field private value class JitTest_format_il.PlatformID m_pid
    .field private value class JitTest_format_il.Mood m_mood
    .method private hidebysig static string 
            Format(typedref format,
                   typedref _ref) il managed
    {
      .maxstack  3
      .locals (int32 V_0,
               wchar[] V_1,
               string V_2,
               int32 V_3,
               int32 V_4,
               bool V_5,
               bool V_6,
               typedref V_7,
               wchar V_8,
               value class JitTest_format_il.Mood V_9)
      IL_0000:  ldarg.0
      IL_0001:  refanyval  [mscorlib]System.String
      IL_0006:  ldind.ref
      IL_0007:  call       instance int32 [mscorlib]System.String::get_Length()
      IL_000c:  stloc.0
      IL_000d:  ldarg.0
      IL_000e:  refanyval  [mscorlib]System.String
      IL_0013:  ldind.ref
      IL_0014:  call       instance wchar[] [mscorlib]System.String::ToCharArray()
      IL_0019:  stloc.1
      IL_001a:  ldstr      ""
      IL_001f:  stloc.2
      IL_0020:  ldc.i4.0
      IL_0021:  stloc.3
      IL_0022:  ldc.i4.0
      IL_0023:  stloc.s    V_4
      IL_0025:  br         IL_03d4

      IL_002a:  ldloc.1
      IL_002b:  ldloc.s    V_4
      IL_002d:  ldelem.u2
      IL_002e:  ldc.i4.s   37
      IL_0030:  beq.s      IL_004b

      IL_0032:  ldloc.2
      IL_0033:  ldloc.1
      IL_0034:  ldloc.s    V_4
      IL_0036:  ldelem.u2
      IL_003b:  box        [mscorlib]System.Char
      IL_0040:  call       string [mscorlib]System.String::Concat(class [mscorlib]System.Object,
                                                                               class [mscorlib]System.Object)
      IL_0045:  stloc.2
      IL_0046:  br         IL_03ce

      IL_004b:  ldloc.s    V_4
      IL_004d:  ldc.i4.1
      IL_004e:  add
      IL_004f:  stloc.s    V_4
      IL_0051:  ldloc.s    V_4
      IL_0053:  ldloc.0
      IL_0054:  blt.s      IL_005c

      IL_0056:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_005b:  throw

      IL_005c:  ldc.i4.0
      IL_005d:  stloc.s    V_5
      IL_005f:  ldc.i4.1
      IL_0060:  stloc.s    V_6
      IL_0062:  ldloca.s   V_5
      IL_0064:  mkrefany   [mscorlib]System.Boolean
      IL_0069:  stloc.s    V_7
      IL_006b:  ldloc.1
      IL_006c:  ldloc.s    V_4
      IL_006e:  ldelem.u2
      IL_006f:  ldc.i4.s   108
      IL_0071:  bne.un.s   IL_008d

      IL_0073:  ldloca.s   V_6
      IL_0075:  mkrefany   [mscorlib]System.Boolean
      IL_007a:  stloc.s    V_7
      IL_007c:  ldloc.s    V_4
      IL_007e:  ldc.i4.1
      IL_007f:  add
      IL_0080:  stloc.s    V_4
      IL_0082:  ldloc.s    V_4
      IL_0084:  ldloc.0
      IL_0085:  blt.s      IL_008d

      IL_0087:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_008c:  throw

      IL_008d:  ldloc.3
      IL_008e:  dup
      IL_008f:  ldc.i4.1
      IL_0090:  add
      IL_0091:  stloc.3
      IL_0092:  ldc.i4.1
      IL_0093:  bne.un.s   IL_009b

      IL_0095:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_009a:  throw

      IL_009b:  ldloc.1
      IL_009c:  ldloc.s    V_4
      IL_009e:  ldelem.u2
      IL_009f:  stloc.s    V_8
      IL_00a1:  ldloc.s    V_8
      IL_00a3:  ldc.i4.s   98
      IL_00a5:  sub
      IL_00a6:  switch     ( 
                            IL_00e6,
                            IL_03c8,
                            IL_013b,
                            IL_0350,
                            IL_0213)
      IL_00bf:  ldloc.s    V_8
      IL_00c1:  ldc.i4.s   112
      IL_00c3:  sub
      IL_00c4:  switch     ( 
                            IL_030d,
                            IL_03c8,
                            IL_03c8,
                            IL_027f,
                            IL_02cc,
                            IL_01a7)
      IL_00e1:  br         IL_03c8

      IL_00e6:  ldloc.s    V_7
      IL_00e8:  refanyval  [mscorlib]System.Boolean
      IL_00ed:  ldind.i1
      IL_00ee:  brfalse.s  IL_00f6

      IL_00f0:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_00f5:  throw

      IL_00f6:  ldarg.1
      IL_00f7:  refanytype
      IL_00f9:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_00fe:  ldtoken    [mscorlib]System.Boolean
      IL_0103:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0108:  beq.s      IL_0110

      IL_010a:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_010f:  throw

      IL_0110:  ldarg.1
      IL_0111:  refanyval  [mscorlib]System.Boolean
      IL_0116:  ldind.i1
      IL_0117:  brfalse.s  IL_012a

      IL_0119:  ldloc.2
      IL_011a:  ldstr      "true"
      IL_011f:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_0124:  stloc.2
      IL_0125:  br         IL_03ce

      IL_012a:  ldloc.2
      IL_012b:  ldstr      "false"
      IL_0130:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_0135:  stloc.2
      IL_0136:  br         IL_03ce

      IL_013b:  ldloc.s    V_7
      IL_013d:  refanyval  [mscorlib]System.Boolean
      IL_0142:  ldind.i1
      IL_0143:  brfalse.s  IL_0176

      IL_0145:  ldarg.1
      IL_0146:  refanytype
      IL_0148:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_014d:  ldtoken    [mscorlib]System.Int64
      IL_0152:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0157:  beq.s      IL_015f

      IL_0159:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_015e:  throw

      IL_015f:  ldloc.2
      IL_0160:  ldarg.1
      IL_0161:  refanyval  [mscorlib]System.Int64
      IL_0166:  call       instance string [mscorlib]System.Int64::ToString()
      IL_016b:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_0170:  stloc.2
      IL_0171:  br         IL_03ce

      IL_0176:  ldarg.1
      IL_0177:  refanytype
      IL_0179:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_017e:  ldtoken    [mscorlib]System.Int32
      IL_0183:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0188:  beq.s      IL_0190

      IL_018a:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_018f:  throw

      IL_0190:  ldloc.2
      IL_0191:  ldarg.1
      IL_0192:  refanyval  [mscorlib]System.Int32
      IL_0197:  call       instance string [mscorlib]System.Int32::ToString()
      IL_019c:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_01a1:  stloc.2
      IL_01a2:  br         IL_03ce

      IL_01a7:  ldloc.s    V_7
      IL_01a9:  refanyval  [mscorlib]System.Boolean
      IL_01ae:  ldind.i1
      IL_01af:  brfalse.s  IL_01e2

      IL_01b1:  ldarg.1
      IL_01b2:  refanytype
      IL_01b4:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_01b9:  ldtoken    [mscorlib]System.UInt64
      IL_01be:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_01c3:  beq.s      IL_01cb

      IL_01c5:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_01ca:  throw

      IL_01cb:  ldloc.2
      IL_01cc:  ldarg.1
      IL_01cd:  refanyval  [mscorlib]System.UInt64
      IL_01d2:  call       instance string [mscorlib]System.UInt64::ToString()
      IL_01d7:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_01dc:  stloc.2
      IL_01dd:  br         IL_03ce

      IL_01e2:  ldarg.1
      IL_01e3:  refanytype
      IL_01e5:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_01ea:  ldtoken    [mscorlib]System.UInt32
      IL_01ef:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_01f4:  beq.s      IL_01fc

      IL_01f6:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_01fb:  throw

      IL_01fc:  ldloc.2
      IL_01fd:  ldarg.1
      IL_01fe:  refanyval  [mscorlib]System.UInt32
      IL_0203:  call       instance string [mscorlib]System.UInt32::ToString()
      IL_0208:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_020d:  stloc.2
      IL_020e:  br         IL_03ce

      IL_0213:  ldloc.s    V_7
      IL_0215:  refanyval  [mscorlib]System.Boolean
      IL_021a:  ldind.i1
      IL_021b:  brfalse.s  IL_024e

      IL_021d:  ldarg.1
      IL_021e:  refanytype
      IL_0220:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0225:  ldtoken    [mscorlib]System.Double
      IL_022a:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_022f:  beq.s      IL_0237

      IL_0231:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0236:  throw

      IL_0237:  ldloc.2
      IL_0238:  ldarg.1
      IL_0239:  refanyval  [mscorlib]System.Double
      IL_023e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0243:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_0248:  stloc.2
      IL_0249:  br         IL_03ce

      IL_024e:  ldarg.1
      IL_024f:  refanytype
      IL_0251:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0256:  ldtoken    [mscorlib]System.Single
      IL_025b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0260:  beq.s      IL_0268

      IL_0262:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0267:  throw

      IL_0268:  ldloc.2
      IL_0269:  ldarg.1
      IL_026a:  refanyval  [mscorlib]System.Single
      IL_026f:  call       instance string [mscorlib]System.Single::ToString()
      IL_0274:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_0279:  stloc.2
      IL_027a:  br         IL_03ce

      IL_027f:  ldloc.s    V_7
      IL_0281:  refanyval  [mscorlib]System.Boolean
      IL_0286:  ldind.i1
      IL_0287:  brfalse.s  IL_028f

      IL_0289:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_028e:  throw

      IL_028f:  ldarg.1
      IL_0290:  refanytype
      IL_0292:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0297:  ldtoken    [mscorlib]System.String
      IL_029c:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_02a1:  beq.s      IL_02a9

      IL_02a3:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_02a8:  throw

      IL_02a9:  ldloc.2
      IL_02aa:  ldarg.1
      IL_02ab:  refanyval  [mscorlib]System.String
      IL_02b0:  ldind.ref
      IL_02b1:  brtrue.s   IL_02ba

      IL_02b3:  ldstr      "(null)"
      IL_02b8:  br.s       IL_02c1

      IL_02ba:  ldarg.1
      IL_02bb:  refanyval  [mscorlib]System.String
      IL_02c0:  ldind.ref
      IL_02c1:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_02c6:  stloc.2
      IL_02c7:  br         IL_03ce

      IL_02cc:  ldloc.s    V_7
      IL_02ce:  refanyval  [mscorlib]System.Boolean
      IL_02d3:  ldind.i1
      IL_02d4:  brfalse.s  IL_02dc

      IL_02d6:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_02db:  throw

      IL_02dc:  ldarg.1
      IL_02dd:  refanytype
      IL_02df:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_02e4:  ldtoken    [mscorlib]System.DateTime
      IL_02e9:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_02ee:  beq.s      IL_02f6

      IL_02f0:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_02f5:  throw

      IL_02f6:  ldloc.2
      IL_02f7:  ldarg.1
      IL_02f8:  refanyval  [mscorlib]System.DateTime
      IL_02fd:  call       instance string [mscorlib]System.DateTime::ToString()
      IL_0302:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_0307:  stloc.2
      IL_0308:  br         IL_03ce

      IL_030d:  ldloc.s    V_7
      IL_030f:  refanyval  [mscorlib]System.Boolean
      IL_0314:  ldind.i1
      IL_0315:  brfalse.s  IL_031d

      IL_0317:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_031c:  throw

      IL_031d:  ldarg.1
      IL_031e:  refanytype
      IL_0320:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0325:  ldtoken    JitTest_format_il.PlatformID
      IL_032a:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_032f:  beq.s      IL_0337

      IL_0331:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0336:  throw

      IL_0337:  ldloc.2
      IL_0338:  ldarg.1
      IL_0339:  refanyval  JitTest_format_il.PlatformID
	      	ldobj  JitTest_format_il.PlatformID
      IL_033e:  box        JitTest_format_il.PlatformID
      IL_0343:  call       instance string [mscorlib]System.Enum::ToString()
      IL_0348:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_034d:  stloc.2
      IL_034e:  br.s       IL_03ce

      IL_0350:  ldloc.s    V_7
      IL_0352:  refanyval  [mscorlib]System.Boolean
      IL_0357:  ldind.i1
      IL_0358:  brfalse.s  IL_0360

      IL_035a:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_035f:  throw

      IL_0360:  ldarg.1
      IL_0361:  refanytype
      IL_0363:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0368:  ldtoken    JitTest_format_il.Mood
      IL_036d:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0372:  beq.s      IL_037a

      IL_0374:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0379:  throw

      IL_037a:  ldarg.1
      IL_037b:  refanyval  JitTest_format_il.Mood
      IL_0380:  ldind.i4
      IL_0381:  stloc.s    V_9
      IL_0383:  ldloc.s    V_9
      IL_0385:  switch     ( 
                            IL_0398,
                            IL_03a6,
                            IL_03b4)
      IL_0396:  br.s       IL_03c2

      IL_0398:  ldloc.2
      IL_0399:  ldstr      "good"
      IL_039e:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_03a3:  stloc.2
      IL_03a4:  br.s       IL_03ce

      IL_03a6:  ldloc.2
      IL_03a7:  ldstr      "bad"
      IL_03ac:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_03b1:  stloc.2
      IL_03b2:  br.s       IL_03ce

      IL_03b4:  ldloc.2
      IL_03b5:  ldstr      "worse"
      IL_03ba:  call       string [mscorlib]System.String::Concat(string,
                                                                               string)
      IL_03bf:  stloc.2
      IL_03c0:  br.s       IL_03ce

      IL_03c2:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_03c7:  throw

      IL_03c8:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_03cd:  throw

      IL_03ce:  ldloc.s    V_4
      IL_03d0:  ldc.i4.1
      IL_03d1:  add
      IL_03d2:  stloc.s    V_4
      IL_03d4:  ldloc.s    V_4
      IL_03d6:  ldloc.0
      IL_03d7:  blt        IL_002a

      IL_03dc:  ldloc.2
      IL_03dd:  ret
    } // end of method TestClass::Format

    .method private hidebysig static void 
            Test(string format,
                 typedref arg,
                 string result) il managed
    {
      .maxstack  3
      .locals (string V_0,
               string[] V_1)
      IL_0000:  ldarga.s   format
      IL_0002:  mkrefany   [mscorlib]System.String
      IL_0007:  ldarg.1
      IL_0008:  call       string JitTest_format_il.TestClass::Format(typedref,
                                                                         typedref)
      IL_000d:  stloc.0
      IL_000e:  ldloc.0
      IL_000f:  ldarg.2
      IL_0010:  call       bool [mscorlib]System.String::Equals(string,
                                                                string)
      IL_0015:  brtrue.s   IL_004f

      IL_0017:  ldc.i4.5
      IL_0018:  newarr     [mscorlib]System.String
      IL_001d:  stloc.1
      IL_001e:  ldloc.1
      IL_001f:  ldc.i4.0
      IL_0020:  ldstr      "WRONG! expected: '"
      IL_0025:  stelem.ref
      IL_0026:  ldloc.1
      IL_0027:  ldc.i4.1
      IL_0028:  ldarg.2
      IL_0029:  stelem.ref
      IL_002a:  ldloc.1
      IL_002b:  ldc.i4.2
      IL_002c:  ldstr      "', got '"
      IL_0031:  stelem.ref
      IL_0032:  ldloc.1
      IL_0033:  ldc.i4.3
      IL_0034:  ldloc.0
      IL_0035:  stelem.ref
      IL_0036:  ldloc.1
      IL_0037:  ldc.i4.4
      IL_0038:  ldstr      "'"
      IL_003d:  stelem.ref
      IL_003e:  ldloc.1
      IL_003f:  call       string [mscorlib]System.String::Concat(string[])
      IL_0044:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0049:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_004e:  throw

      IL_004f:  ret
    } // end of method TestClass::Test

    .method private hidebysig static void
            TestLocals() il managed
    {
      .maxstack  5
      .locals (int32 V_0,
               unsigned int32 V_1,
               int64 V_2,
               unsigned int64 V_3,
               float32 V_4,
               float64 V_5,
               bool V_6,
               value class [mscorlib]System.DateTime V_7,
               value class JitTest_format_il.PlatformID V_8,
               value class JitTest_format_il.Mood V_9)
      IL_0000:  ldc.i4.s   10
      IL_0002:  stloc.0
      IL_0003:  ldc.i4.s   11
      IL_0005:  stloc.1
      IL_0006:  ldc.i4.s   12
      IL_0008:  conv.i8
      IL_0009:  stloc.2
      IL_000a:  ldc.i4.s   13
      IL_000c:  conv.i8
      IL_000d:  stloc.3
      IL_000e:  ldc.r4     14.
      IL_0013:  stloc.s    V_4
      IL_0015:  ldc.r8     15.
      IL_001e:  stloc.s    V_5
      IL_0020:  ldc.i4.1
      IL_0021:  stloc.s    V_6
      IL_0023:  ldloca.s   V_7
      IL_0025:  ldc.i4.s   100
      IL_0027:  ldc.i4.s   10
      IL_0029:  ldc.i4.1
      IL_002a:  call       instance void [mscorlib]System.DateTime::.ctor(int32,
                                                                          int32,
                                                                          int32)
      IL_002f:  ldc.i4.2
      IL_0030:  stloc.s    V_8
      IL_0032:  ldc.i4.0
      IL_0033:  stloc.s    V_9
      IL_0035:  ldstr      "{%d}"
      IL_003a:  ldloca.s   V_0
      IL_003c:  mkrefany   [mscorlib]System.Int32
      IL_0041:  ldstr      "{10}"
      IL_0046:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_004b:  ldstr      "{%u}"
      IL_0050:  ldloca.s   V_1
      IL_0052:  mkrefany   [mscorlib]System.UInt32
      IL_0057:  ldstr      "{11}"
      IL_005c:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0061:  ldstr      "{%ld}"
      IL_0066:  ldloca.s   V_2
      IL_0068:  mkrefany   [mscorlib]System.Int64
      IL_006d:  ldstr      "{12}"
      IL_0072:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0077:  ldstr      "{%lu}"
      IL_007c:  ldloca.s   V_3
      IL_007e:  mkrefany   [mscorlib]System.UInt64
      IL_0083:  ldstr      "{13}"
      IL_0088:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_008d:  ldstr      "{%f}"
      IL_0092:  ldloca.s   V_4
      IL_0094:  mkrefany   [mscorlib]System.Single
      IL_0099:  ldstr      "{14}"
      IL_009e:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_00a3:  ldstr      "{%lf}"
      IL_00a8:  ldloca.s   V_5
      IL_00aa:  mkrefany   [mscorlib]System.Double
      IL_00af:  ldstr      "{15}"
      IL_00b4:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_00b9:  ldstr      "{%b}"
      IL_00be:  ldloca.s   V_6
      IL_00c0:  mkrefany   [mscorlib]System.Boolean
      IL_00c5:  ldstr      "{true}"
      IL_00ca:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_00cf:  ldstr      "{%t}"
      IL_00d4:  ldloca.s   V_7
      IL_00d6:  mkrefany   [mscorlib]System.DateTime
      IL_00db:  ldstr      "{"
      IL_00e0:  ldloca.s   V_7
      IL_00e2:  call       instance string [mscorlib]System.DateTime::ToString()
      IL_00e7:  ldstr      "}"
      IL_00ec:  call       string [mscorlib]System.String::Concat(string,
                                                                               string,
                                                                               string)
      IL_00f1:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_00f6:  ldstr      "{%p}"
      IL_00fb:  ldloca.s   V_8
      IL_00fd:  mkrefany   JitTest_format_il.PlatformID
      IL_0102:  ldstr      "{Win32NT}"
      IL_0107:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_010c:  ldstr      "{%e}"
      IL_0111:  ldloca.s   V_9
      IL_0113:  mkrefany   JitTest_format_il.Mood
      IL_0118:  ldstr      "{good}"
      IL_011d:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0122:  ret
    } // end of method TestClass::TestLocals

    .method private hidebysig instance void
            TestFields() il managed
    {
      .maxstack  5
      IL_0000:  ldstr      "{%d}"
      IL_0005:  ldarg.0
      IL_0006:  ldflda     int32 JitTest_format_il.TestClass::m_d
      IL_000b:  mkrefany   [mscorlib]System.Int32
      IL_0010:  ldstr      "{20}"
      IL_0015:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_001a:  ldstr      "{%u}"
      IL_001f:  ldsflda    unsigned int32 JitTest_format_il.TestClass::m_u
      IL_0024:  mkrefany   [mscorlib]System.UInt32
      IL_0029:  ldstr      "{21}"
      IL_002e:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0033:  ldstr      "{%ld}"
      IL_0038:  ldarg.0
      IL_0039:  ldflda     int64 JitTest_format_il.TestClass::m_l
      IL_003e:  mkrefany   [mscorlib]System.Int64
      IL_0043:  ldstr      "{22}"
      IL_0048:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_004d:  ldstr      "{%lu}"
      IL_0052:  ldsflda    unsigned int64 JitTest_format_il.TestClass::m_ul
      IL_0057:  mkrefany   [mscorlib]System.UInt64
      IL_005c:  ldstr      "{23}"
      IL_0061:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0066:  ldstr      "{%f}"
      IL_006b:  ldarg.0
      IL_006c:  ldflda     float32 JitTest_format_il.TestClass::m_f
      IL_0071:  mkrefany   [mscorlib]System.Single
      IL_0076:  ldstr      "{24}"
      IL_007b:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0080:  ldstr      "{%lf}"
      IL_0085:  ldarg.0
      IL_0086:  ldflda     float64 JitTest_format_il.TestClass::m_dbl
      IL_008b:  mkrefany   [mscorlib]System.Double
      IL_0090:  ldstr      "{25}"
      IL_0095:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_009a:  ldstr      "{%b}"
      IL_009f:  ldarg.0
      IL_00a0:  ldflda     bool JitTest_format_il.TestClass::m_b
      IL_00a5:  mkrefany   [mscorlib]System.Boolean
      IL_00aa:  ldstr      "{false}"
      IL_00af:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_00b4:  ldstr      "{%t}"
      IL_00b9:  ldsflda    value class [mscorlib]System.DateTime JitTest_format_il.TestClass::m_t
      IL_00be:  mkrefany   [mscorlib]System.DateTime
      IL_00c3:  ldstr      "{"
      IL_00c8:  ldsflda    value class [mscorlib]System.DateTime JitTest_format_il.TestClass::m_t
      IL_00cd:  call       instance string [mscorlib]System.DateTime::ToString()
      IL_00d2:  ldstr      "}"
      IL_00d7:  call       string [mscorlib]System.String::Concat(string,
                                                                               string,
                                                                               string)
      IL_00dc:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_00e1:  ldstr      "{%p}"
      IL_00e6:  ldarg.0
      IL_00e7:  ldflda     value class JitTest_format_il.PlatformID JitTest_format_il.TestClass::m_pid
      IL_00ec:  mkrefany   JitTest_format_il.PlatformID
      IL_00f1:  ldstr      "{Win32NT}"
      IL_00f6:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_00fb:  ldstr      "{%e}"
      IL_0100:  ldarg.0
      IL_0101:  ldflda     value class JitTest_format_il.Mood JitTest_format_il.TestClass::m_mood
      IL_0106:  mkrefany   JitTest_format_il.Mood
      IL_010b:  ldstr      "{good}"
      IL_0110:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0115:  ret
    } // end of method TestClass::TestFields

    .method private hidebysig static void 
            DoTestArgSlots(int32& d,
                           unsigned int32& u,
                           int64& l,
                           unsigned int64& ul,
                           float32& f,
                           float64& dbl,
                           bool& b,
                           value class [mscorlib]System.DateTime& t,
                           value class JitTest_format_il.PlatformID& pid) il managed
    {
      .maxstack  5
      IL_0000:  ldstr      "{%d}"
      IL_0005:  ldarg.0
      IL_0006:  mkrefany   [mscorlib]System.Int32
      IL_000b:  ldstr      "{20}"
      IL_0010:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0015:  ldstr      "{%u}"
      IL_001a:  ldarg.1
      IL_001b:  mkrefany   [mscorlib]System.UInt32
      IL_0020:  ldstr      "{21}"
      IL_0025:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_002a:  ldstr      "{%ld}"
      IL_002f:  ldarg.2
      IL_0030:  mkrefany   [mscorlib]System.Int64
      IL_0035:  ldstr      "{22}"
      IL_003a:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_003f:  ldstr      "{%lu}"
      IL_0044:  ldarg.3
      IL_0045:  mkrefany   [mscorlib]System.UInt64
      IL_004a:  ldstr      "{23}"
      IL_004f:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0054:  ldstr      "{%f}"
      IL_0059:  ldarg.s    f
      IL_005b:  mkrefany   [mscorlib]System.Single
      IL_0060:  ldstr      "{24}"
      IL_0065:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_006a:  ldstr      "{%lf}"
      IL_006f:  ldarg.s    dbl
      IL_0071:  mkrefany   [mscorlib]System.Double
      IL_0076:  ldstr      "{25}"
      IL_007b:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0080:  ldstr      "{%b}"
      IL_0085:  ldarg.s    b
      IL_0087:  mkrefany   [mscorlib]System.Boolean
      IL_008c:  ldstr      "{false}"
      IL_0091:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0096:  ldstr      "{%t}"
      IL_009b:  ldarg.s    t
      IL_009d:  mkrefany   [mscorlib]System.DateTime
      IL_00a2:  ldstr      "{"
      IL_00a7:  ldarg.s    t
      IL_00a9:  call       instance string [mscorlib]System.DateTime::ToString()
      IL_00ae:  ldstr      "}"
      IL_00b3:  call       string [mscorlib]System.String::Concat(string,
                                                                               string,
                                                                               string)
      IL_00b8:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_00bd:  ldstr      "{%p}"
      IL_00c2:  ldarg.s    pid
      IL_00c4:  mkrefany   JitTest_format_il.PlatformID
      IL_00c9:  ldstr      "{Win32NT}"
      IL_00ce:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_00d3:  ret
    } // end of method TestClass::DoTestArgSlots

    .method private hidebysig static void
            TestArgSlots() il managed
    {
      .maxstack  9
      .locals (int32 V_0,
               unsigned int32 V_1,
               int64 V_2,
               unsigned int64 V_3,
               float32 V_4,
               float64 V_5,
               bool V_6,
               value class [mscorlib]System.DateTime V_7,
               value class JitTest_format_il.PlatformID V_8)
      IL_0000:  ldc.i4.s   20
      IL_0002:  stloc.0
      IL_0003:  ldc.i4.s   21
      IL_0005:  stloc.1
      IL_0006:  ldc.i4.s   22
      IL_0008:  conv.i8
      IL_0009:  stloc.2
      IL_000a:  ldc.i4.s   23
      IL_000c:  conv.i8
      IL_000d:  stloc.3
      IL_000e:  ldc.r4     24.
      IL_0013:  stloc.s    V_4
      IL_0015:  ldc.r8     25.
      IL_001e:  stloc.s    V_5
      IL_0020:  ldc.i4.0
      IL_0021:  stloc.s    V_6
      IL_0023:  ldloca.s   V_7
      IL_0025:  ldc.i4.s   100
      IL_0027:  ldc.i4.s   10
      IL_0029:  ldc.i4.1
      IL_002a:  call       instance void [mscorlib]System.DateTime::.ctor(int32,
                                                                          int32,
                                                                          int32)
      IL_002f:  ldc.i4.2
      IL_0030:  stloc.s    V_8
      IL_0032:  ldloca.s   V_0
      IL_0034:  ldloca.s   V_1
      IL_0036:  ldloca.s   V_2
      IL_0038:  ldloca.s   V_3
      IL_003a:  ldloca.s   V_4
      IL_003c:  ldloca.s   V_5
      IL_003e:  ldloca.s   V_6
      IL_0040:  ldloca.s   V_7
      IL_0042:  ldloca.s   V_8
      IL_0044:  call       void JitTest_format_il.TestClass::DoTestArgSlots(int32&,
                                                                  unsigned int32&,
                                                                  int64&,
                                                                  unsigned int64&,
                                                                  float32&,
                                                                  float64&,
                                                                  bool&,
                                                                  value class [mscorlib]System.DateTime&,
                                                                  value class JitTest_format_il.PlatformID&)
      IL_0049:  ret
    } // end of method TestClass::TestArgSlots

    .method private hidebysig static void
            TestArrayElem() il managed
    {
      .maxstack  5
      .locals (int32[] V_0,
               unsigned int32[] V_1,
               int64[] V_2,
               unsigned int64[] V_3,
               float32[] V_4,
               float64[] V_5,
               bool[] V_6,
               value class [mscorlib]System.DateTime[] V_7,
               value class JitTest_format_il.PlatformID[] V_8,
               value class JitTest_format_il.Mood[] V_9,
               int32[] V_10,
               unsigned int32[] V_11,
               int64[] V_12,
               unsigned int64[] V_13,
               float32[] V_14,
               float64[] V_15,
               bool[] V_16,
               value class JitTest_format_il.PlatformID[] V_17,
               value class JitTest_format_il.Mood[] V_18)
      IL_0000:  ldc.i4.1
      IL_0001:  newarr     [mscorlib]System.Int32
      IL_0006:  stloc.s    V_10
      IL_0008:  ldloc.s    V_10
      IL_000a:  ldc.i4.0
      IL_000b:  ldc.i4.s   10
      IL_000d:  stelem.i4
      IL_000e:  ldloc.s    V_10
      IL_0010:  stloc.0
      IL_0011:  ldc.i4.1
      IL_0012:  newarr     [mscorlib]System.UInt32
      IL_0017:  stloc.s    V_11
      IL_0019:  ldloc.s    V_11
      IL_001b:  ldc.i4.0
      IL_001c:  ldc.i4.s   11
      IL_001e:  stelem.i4
      IL_001f:  ldloc.s    V_11
      IL_0021:  stloc.1
      IL_0022:  ldc.i4.1
      IL_0023:  newarr     [mscorlib]System.Int64
      IL_0028:  stloc.s    V_12
      IL_002a:  ldloc.s    V_12
      IL_002c:  ldc.i4.0
      IL_002d:  ldc.i4.s   12
      IL_002f:  conv.i8
      IL_0030:  stelem.i8
      IL_0031:  ldloc.s    V_12
      IL_0033:  stloc.2
      IL_0034:  ldc.i4.1
      IL_0035:  newarr     [mscorlib]System.UInt64
      IL_003a:  stloc.s    V_13
      IL_003c:  ldloc.s    V_13
      IL_003e:  ldc.i4.0
      IL_003f:  ldc.i4.s   13
      IL_0041:  conv.i8
      IL_0042:  stelem.i8
      IL_0043:  ldloc.s    V_13
      IL_0045:  stloc.3
      IL_0046:  ldc.i4.1
      IL_0047:  newarr     [mscorlib]System.Single
      IL_004c:  stloc.s    V_14
      IL_004e:  ldloc.s    V_14
      IL_0050:  ldc.i4.0
      IL_0051:  ldc.r4     14.
      IL_0056:  stelem.r4
      IL_0057:  ldloc.s    V_14
      IL_0059:  stloc.s    V_4
      IL_005b:  ldc.i4.1
      IL_005c:  newarr     [mscorlib]System.Double
      IL_0061:  stloc.s    V_15
      IL_0063:  ldloc.s    V_15
      IL_0065:  ldc.i4.0
      IL_0066:  ldc.r8     15.
      IL_006f:  stelem.r8
      IL_0070:  ldloc.s    V_15
      IL_0072:  stloc.s    V_5
      IL_0074:  ldc.i4.1
      IL_0075:  newarr     [mscorlib]System.Boolean
      IL_007a:  stloc.s    V_16
      IL_007c:  ldloc.s    V_16
      IL_007e:  ldc.i4.0
      IL_007f:  ldc.i4.1
      IL_0080:  stelem.i1
      IL_0081:  ldloc.s    V_16
      IL_0083:  stloc.s    V_6
      IL_0085:  ldc.i4     0xc8
      IL_008a:  newarr     [mscorlib]System.DateTime
      IL_008f:  stloc.s    V_7
      IL_0091:  ldloc.s    V_7
      IL_0093:  ldc.i4.1
      IL_0094:  ldelema    [mscorlib]System.DateTime
      IL_0099:  ldc.i4.s   100
      IL_009b:  ldc.i4.s   10
      IL_009d:  ldc.i4.1
      IL_009e:  call       instance void [mscorlib]System.DateTime::.ctor(int32,
                                                                          int32,
                                                                          int32)
      IL_00a3:  ldc.i4.1
      IL_00a4:  newarr     JitTest_format_il.PlatformID
      IL_00a9:  stloc.s    V_17
      IL_00ab:  ldloc.s    V_17
      IL_00ad:  ldc.i4.0
      IL_00ae:  ldc.i4.2
      IL_00af:  stelem.i4
      IL_00b0:  ldloc.s    V_17
      IL_00b2:  stloc.s    V_8
      IL_00b4:  ldc.i4.1
      IL_00b5:  newarr     JitTest_format_il.Mood
      IL_00ba:  stloc.s    V_18
      IL_00bc:  ldloc.s    V_18
      IL_00be:  stloc.s    V_9
      IL_00c0:  ldstr      "{%d}"
      IL_00c5:  ldloc.0
      IL_00c6:  ldc.i4.0
      IL_00c7:  ldelema    [mscorlib]System.Int32
      IL_00cc:  mkrefany   [mscorlib]System.Int32
      IL_00d1:  ldstr      "{10}"
      IL_00d6:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_00db:  ldstr      "{%u}"
      IL_00e0:  ldloc.1
      IL_00e1:  ldc.i4.0
      IL_00e2:  ldelema    [mscorlib]System.UInt32
      IL_00e7:  mkrefany   [mscorlib]System.UInt32
      IL_00ec:  ldstr      "{11}"
      IL_00f1:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_00f6:  ldstr      "{%ld}"
      IL_00fb:  ldloc.2
      IL_00fc:  ldc.i4.0
      IL_00fd:  ldelema    [mscorlib]System.Int64
      IL_0102:  mkrefany   [mscorlib]System.Int64
      IL_0107:  ldstr      "{12}"
      IL_010c:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0111:  ldstr      "{%lu}"
      IL_0116:  ldloc.3
      IL_0117:  ldc.i4.0
      IL_0118:  ldelema    [mscorlib]System.UInt64
      IL_011d:  mkrefany   [mscorlib]System.UInt64
      IL_0122:  ldstr      "{13}"
      IL_0127:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_012c:  ldstr      "{%f}"
      IL_0131:  ldloc.s    V_4
      IL_0133:  ldc.i4.0
      IL_0134:  ldelema    [mscorlib]System.Single
      IL_0139:  mkrefany   [mscorlib]System.Single
      IL_013e:  ldstr      "{14}"
      IL_0143:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0148:  ldstr      "{%lf}"
      IL_014d:  ldloc.s    V_5
      IL_014f:  ldc.i4.0
      IL_0150:  ldelema    [mscorlib]System.Double
      IL_0155:  mkrefany   [mscorlib]System.Double
      IL_015a:  ldstr      "{15}"
      IL_015f:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0164:  ldstr      "{%b}"
      IL_0169:  ldloc.s    V_6
      IL_016b:  ldc.i4.0
      IL_016c:  ldelema    [mscorlib]System.Boolean
      IL_0171:  mkrefany   [mscorlib]System.Boolean
      IL_0176:  ldstr      "{true}"
      IL_017b:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_0180:  ldstr      "{%t}"
      IL_0185:  ldloc.s    V_7
      IL_0187:  ldc.i4.1
      IL_0188:  ldelema    [mscorlib]System.DateTime
      IL_018d:  mkrefany   [mscorlib]System.DateTime
      IL_0192:  ldstr      "{"
      IL_0197:  ldloc.s    V_7
      IL_0199:  ldc.i4.1
      IL_019a:  ldelema    [mscorlib]System.DateTime
      IL_019f:  call       instance string [mscorlib]System.DateTime::ToString()
      IL_01a4:  ldstr      "}"
      IL_01a9:  call       string [mscorlib]System.String::Concat(string,
                                                                               string,
                                                                               string)
      IL_01ae:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_01b3:  ldstr      "{%p}"
      IL_01b8:  ldloc.s    V_8
      IL_01ba:  ldc.i4.0
      IL_01bb:  ldelema    JitTest_format_il.PlatformID
      IL_01c0:  mkrefany   JitTest_format_il.PlatformID
      IL_01c5:  ldstr      "{Win32NT}"
      IL_01ca:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_01cf:  ldstr      "{%e}"
      IL_01d4:  ldloc.s    V_9
      IL_01d6:  ldc.i4.0
      IL_01d7:  ldelema    JitTest_format_il.Mood
      IL_01dc:  mkrefany   JitTest_format_il.Mood
      IL_01e1:  ldstr      "{good}"
      IL_01e6:  call       void JitTest_format_il.TestClass::Test(string,
                                                        typedref,
                                                        string)
      IL_01eb:  ret
    } // end of method TestClass::TestArrayElem

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  8
      IL_0000:  call       void JitTest_format_il.TestClass::TestLocals()
      IL_0005:  newobj     instance void JitTest_format_il.TestClass::.ctor()
      IL_000a:  call       instance void JitTest_format_il.TestClass::TestFields()
      IL_000f:  call       void JitTest_format_il.TestClass::TestArrayElem()
      IL_0014:  ldstr      "*** passed ***"
      IL_0019:  call       void [System.Console]System.Console::WriteLine(string)
      IL_001e:  ldc.i4 100
      IL_001f:  ret
    } // end of method TestClass::Main

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      .maxstack  8
      IL_0000:  ldc.i4.s   21
      IL_0002:  stsfld     unsigned int32 JitTest_format_il.TestClass::m_u
      IL_0007:  ldc.i4.s   23
      IL_0009:  conv.i8
      IL_000a:  stsfld     unsigned int64 JitTest_format_il.TestClass::m_ul
      IL_000f:  ldsflda    value class [mscorlib]System.DateTime JitTest_format_il.TestClass::m_t
      IL_0014:  ldc.i4.s   100
      IL_0016:  ldc.i4.s   10
      IL_0018:  ldc.i4.1
      IL_0019:  call       instance void [mscorlib]System.DateTime::.ctor(int32,
                                                                          int32,
                                                                          int32)
      IL_001e:  ret
    } // end of method TestClass::.cctor

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.s   20
      IL_0003:  stfld      int32 JitTest_format_il.TestClass::m_d
      IL_0008:  ldarg.0
      IL_0009:  ldc.i4.s   22
      IL_000b:  conv.i8
      IL_000c:  stfld      int64 JitTest_format_il.TestClass::m_l
      IL_0011:  ldarg.0
      IL_0012:  ldc.r4     24.
      IL_0017:  stfld      float32 JitTest_format_il.TestClass::m_f
      IL_001c:  ldarg.0
      IL_001d:  ldc.r8     25.
      IL_0026:  stfld      float64 JitTest_format_il.TestClass::m_dbl
      IL_002b:  ldarg.0
      IL_002c:  ldc.i4.0
      IL_002d:  stfld      bool JitTest_format_il.TestClass::m_b
      IL_0032:  ldarg.0
      IL_0033:  ldc.i4.2
      IL_0034:  stfld      value class JitTest_format_il.PlatformID JitTest_format_il.TestClass::m_pid
      IL_0039:  ldarg.0
      IL_003a:  ldc.i4.0
      IL_003b:  stfld      value class JitTest_format_il.Mood JitTest_format_il.TestClass::m_mood
      IL_0040:  ldarg.0
      IL_0041:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0046:  ret
    } // end of method TestClass::.ctor

  } // end of class TestClass

} // end of namespace JitTest_format_il

