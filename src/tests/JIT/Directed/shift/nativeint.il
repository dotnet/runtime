// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}



.assembly extern mscorlib
{
}
.assembly 'nativeint'
{
  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}

.namespace ShiftTest
{
  .class public auto ansi beforefieldinit CL
         extends [mscorlib]System.Object
  {
    .field public native int clm_data
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       18 (0x12)
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4     0xff
      IL_0006:  stfld      native int ShiftTest.CL::clm_data
      IL_000b:  ldarg.0
      IL_000c:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0011:  ret
    } // end of method CL::.ctor

  } // end of class CL

  .class public sequential ansi sealed beforefieldinit VT
         extends [mscorlib]System.ValueType
  {
    .field public native int vtm_data
  } // end of class VT

  .class public auto ansi beforefieldinit byte32Test
         extends [mscorlib]System.Object
  {
    .field private static native int s_data
    .method public hidebysig static native int
            f1(native int arg_data) cil managed
    {
      // Code size       12 (0xc)
      .maxstack  2
      .locals init (native int V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.4
      IL_0002:  shr
      IL_0003:  conv.u1
      IL_0004:  starg.s    arg_data
      IL_0006:  ldarg.0
      IL_0007:  stloc.0
      IL_0008:  br.s       IL_000a

      IL_000a:  ldloc.0
      IL_000b:  ret
    } // end of method byte32Test::f1

    .method public hidebysig static native int
            f2(native int arg_data) cil managed
    {
      // Code size       12 (0xc)
      .maxstack  2
      .locals init (native int V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.4
      IL_0002:  shl
      IL_0003:  conv.u1
      IL_0004:  starg.s    arg_data
      IL_0006:  ldarg.0
      IL_0007:  stloc.0
      IL_0008:  br.s       IL_000a

      IL_000a:  ldloc.0
      IL_000b:  ret
    } // end of method byte32Test::f2

    .method public hidebysig static int32 Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       839 (0x347)
      .maxstack  4
      .locals init (native int V_0,
               native int[] V_1,
               class ShiftTest.CL V_2,
               valuetype ShiftTest.VT V_3,
               int32 V_4,
               native int[] V_5)
      IL_0000:  ldc.i4     0xff
      IL_0005:  stloc.0
      IL_0006:  ldc.i4.1
      IL_0007:  newarr     [mscorlib]System.Byte
      IL_000c:  stloc.1
      IL_000d:  newobj     instance void ShiftTest.CL::.ctor()
      IL_0012:  stloc.2
      IL_0013:  ldc.i4     0xff
      IL_0018:  stsfld     native int ShiftTest.byte32Test::s_data
      IL_001d:  ldc.i4     0xff
      IL_0022:  stloc.0
      IL_0023:  ldloc.1
      IL_0024:  ldc.i4.0
      IL_0025:  ldc.i4     0xff
      IL_002a:  stelem.i1
      IL_002b:  ldloc.2
      IL_002c:  ldc.i4     0xff
      IL_0031:  stfld      native int ShiftTest.CL::clm_data
      IL_0036:  ldloca.s   V_3
      IL_0038:  ldc.i4     0xff
      IL_003d:  stfld      native int ShiftTest.VT::vtm_data
      IL_0042:  ldstr      "The expected result of (0xFF>>4) is: {0}"
      IL_0047:  ldc.i4.s   15
      IL_0049:  box        [mscorlib]System.Int32
      IL_004e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0053:  call       void [System.Console]System.Console::WriteLine()
      IL_0058:  ldstr      "The actual result for function argument is: {0}"
      IL_005d:  ldc.i4     0xff
      IL_0062:  call       native int ShiftTest.byte32Test::f1(native int)
      IL_0067:  box        [mscorlib]System.Byte
      IL_006c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0071:  ldloc.0
      IL_0072:  ldc.i4.4
      IL_0073:  shr
      IL_0074:  conv.u1
      IL_0075:  stloc.0
      IL_0076:  ldstr      "The actual result for local variable is: {0}"
      IL_007b:  ldloc.0
      IL_007c:  box        [mscorlib]System.Byte
      IL_0081:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0086:  ldsfld     native int ShiftTest.byte32Test::s_data
      IL_008b:  ldc.i4.4
      IL_008c:  shr
      IL_008d:  conv.u1
      IL_008e:  stsfld     native int ShiftTest.byte32Test::s_data
      IL_0093:  ldstr      "The actual result for static field is: {0}"
      IL_0098:  ldsfld     native int ShiftTest.byte32Test::s_data
      IL_009d:  box        [mscorlib]System.Byte
      IL_00a2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_00a7:  ldloc.1
      IL_00a8:  dup
      IL_00a9:  stloc.s    V_5
      IL_00ab:  ldc.i4.0
      IL_00ac:  ldloc.s    V_5
      IL_00ae:  ldc.i4.0
      IL_00af:  ldelem.u1
      IL_00b0:  ldc.i4.4
      IL_00b1:  shr
      IL_00b2:  conv.u1
      IL_00b3:  stelem.i1
      IL_00b4:  ldstr      "The actual result for array element is: {0}"
      IL_00b9:  ldloc.1
      IL_00ba:  ldc.i4.0
      IL_00bb:  ldelem.u1
      IL_00bc:  box        [mscorlib]System.Byte
      IL_00c1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_00c6:  ldloc.2
      IL_00c7:  dup
      IL_00c8:  ldfld      native int ShiftTest.CL::clm_data
      IL_00cd:  ldc.i4.4
      IL_00ce:  shr
      IL_00cf:  conv.u1
      IL_00d0:  stfld      native int ShiftTest.CL::clm_data
      IL_00d5:  ldstr      "The actual result for class member is: {0}"
      IL_00da:  ldloc.2
      IL_00db:  ldfld      native int ShiftTest.CL::clm_data
      IL_00e0:  box        [mscorlib]System.Byte
      IL_00e5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_00ea:  ldloca.s   V_3
      IL_00ec:  dup
      IL_00ed:  ldfld      native int ShiftTest.VT::vtm_data
      IL_00f2:  ldc.i4.4
      IL_00f3:  shr
      IL_00f4:  conv.u1
      IL_00f5:  stfld      native int ShiftTest.VT::vtm_data
      IL_00fa:  ldstr      "The actual result for valuestruct member is: {0}"
      IL_00ff:  ldloca.s   V_3
      IL_0101:  ldfld      native int ShiftTest.VT::vtm_data
      IL_0106:  box        [mscorlib]System.Byte
      IL_010b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0110:  call       void [System.Console]System.Console::WriteLine()
      IL_0115:  ldloc.0
      IL_0116:  ldc.i4.s   15
      IL_0118:  beq.s      IL_012c

      IL_011a:  ldstr      "FAILED for local variable"
      IL_011f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0124:  ldc.i4.m1
      IL_0125:  stloc.s    V_4
      IL_0127:  br         IL_0344

      IL_012c:  ldc.i4     0xff
      IL_0131:  call       native int ShiftTest.byte32Test::f1(native int)
      IL_0136:  ldc.i4.s   15
      IL_0138:  beq.s      IL_014c

      IL_013a:  ldstr      "FAILED for function argument"
      IL_013f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0144:  ldc.i4.m1
      IL_0145:  stloc.s    V_4
      IL_0147:  br         IL_0344

      IL_014c:  ldsfld     native int ShiftTest.byte32Test::s_data
      IL_0151:  ldc.i4.s   15
      IL_0153:  beq.s      IL_0167

      IL_0155:  ldstr      "FAILED for static field"
      IL_015a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_015f:  ldc.i4.m1
      IL_0160:  stloc.s    V_4
      IL_0162:  br         IL_0344

      IL_0167:  ldloc.1
      IL_0168:  ldc.i4.0
      IL_0169:  ldelem.u1
      IL_016a:  ldc.i4.s   15
      IL_016c:  beq.s      IL_0180

      IL_016e:  ldstr      "FAILED for array element"
      IL_0173:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0178:  ldc.i4.m1
      IL_0179:  stloc.s    V_4
      IL_017b:  br         IL_0344

      IL_0180:  ldloc.2
      IL_0181:  ldfld      native int ShiftTest.CL::clm_data
      IL_0186:  ldc.i4.s   15
      IL_0188:  beq.s      IL_019c

      IL_018a:  ldstr      "FAILED for class member"
      IL_018f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0194:  ldc.i4.m1
      IL_0195:  stloc.s    V_4
      IL_0197:  br         IL_0344

      IL_019c:  ldloca.s   V_3
      IL_019e:  ldfld      native int ShiftTest.VT::vtm_data
      IL_01a3:  ldc.i4.s   15
      IL_01a5:  beq.s      IL_01b9

      IL_01a7:  ldstr      "FAILED for valuestruct member"
      IL_01ac:  call       void [System.Console]System.Console::WriteLine(string)
      IL_01b1:  ldc.i4.m1
      IL_01b2:  stloc.s    V_4
      IL_01b4:  br         IL_0344

      IL_01b9:  ldc.i4.1
      IL_01ba:  stsfld     native int ShiftTest.byte32Test::s_data
      IL_01bf:  ldc.i4.1
      IL_01c0:  stloc.0
      IL_01c1:  ldloc.1
      IL_01c2:  ldc.i4.0
      IL_01c3:  ldc.i4.1
      IL_01c4:  stelem.i1
      IL_01c5:  ldloc.2
      IL_01c6:  ldc.i4.1
      IL_01c7:  stfld      native int ShiftTest.CL::clm_data
      IL_01cc:  ldloca.s   V_3
      IL_01ce:  ldc.i4.1
      IL_01cf:  stfld      native int ShiftTest.VT::vtm_data
      IL_01d4:  ldstr      "The expected result of (0x1<<4) is: {0}"
      IL_01d9:  ldc.i4.s   16
      IL_01db:  box        [mscorlib]System.Int32
      IL_01e0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_01e5:  call       void [System.Console]System.Console::WriteLine()
      IL_01ea:  ldstr      "The actual result for function argument is: {0}"
      IL_01ef:  ldc.i4.1
      IL_01f0:  call       native int ShiftTest.byte32Test::f2(native int)
      IL_01f5:  box        [mscorlib]System.Byte
      IL_01fa:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_01ff:  ldloc.0
      IL_0200:  ldc.i4.4
      IL_0201:  shl
      IL_0202:  conv.u1
      IL_0203:  stloc.0
      IL_0204:  ldstr      "The actual result for local variable is: {0}"
      IL_0209:  ldloc.0
      IL_020a:  box        [mscorlib]System.Byte
      IL_020f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0214:  ldsfld     native int ShiftTest.byte32Test::s_data
      IL_0219:  ldc.i4.4
      IL_021a:  shl
      IL_021b:  conv.u1
      IL_021c:  stsfld     native int ShiftTest.byte32Test::s_data
      IL_0221:  ldstr      "The actual result for static field is: {0}"
      IL_0226:  ldsfld     native int ShiftTest.byte32Test::s_data
      IL_022b:  box        [mscorlib]System.Byte
      IL_0230:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0235:  ldloc.1
      IL_0236:  dup
      IL_0237:  stloc.s    V_5
      IL_0239:  ldc.i4.0
      IL_023a:  ldloc.s    V_5
      IL_023c:  ldc.i4.0
      IL_023d:  ldelem.u1
      IL_023e:  ldc.i4.4
      IL_023f:  shl
      IL_0240:  conv.u1
      IL_0241:  stelem.i1
      IL_0242:  ldstr      "The actual result for array element is: {0}"
      IL_0247:  ldloc.1
      IL_0248:  ldc.i4.0
      IL_0249:  ldelem.u1
      IL_024a:  box        [mscorlib]System.Byte
      IL_024f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0254:  ldloc.2
      IL_0255:  dup
      IL_0256:  ldfld      native int ShiftTest.CL::clm_data
      IL_025b:  ldc.i4.4
      IL_025c:  shl
      IL_025d:  conv.u1
      IL_025e:  stfld      native int ShiftTest.CL::clm_data
      IL_0263:  ldstr      "The actual result for class member is: {0}"
      IL_0268:  ldloc.2
      IL_0269:  ldfld      native int ShiftTest.CL::clm_data
      IL_026e:  box        [mscorlib]System.Byte
      IL_0273:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0278:  ldloca.s   V_3
      IL_027a:  dup
      IL_027b:  ldfld      native int ShiftTest.VT::vtm_data
      IL_0280:  ldc.i4.4
      IL_0281:  shl
      IL_0282:  conv.u1
      IL_0283:  stfld      native int ShiftTest.VT::vtm_data
      IL_0288:  ldstr      "The actual result for valuestruct member is: {0}"
      IL_028d:  ldloca.s   V_3
      IL_028f:  ldfld      native int ShiftTest.VT::vtm_data
      IL_0294:  box        [mscorlib]System.Byte
      IL_0299:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_029e:  call       void [System.Console]System.Console::WriteLine()
      IL_02a3:  ldloc.0
      IL_02a4:  ldc.i4.s   16
      IL_02a6:  beq.s      IL_02ba

      IL_02a8:  ldstr      "FAILED for local variable"
      IL_02ad:  call       void [System.Console]System.Console::WriteLine(string)
      IL_02b2:  ldc.i4.m1
      IL_02b3:  stloc.s    V_4
      IL_02b5:  br         IL_0344

      IL_02ba:  ldc.i4.1
      IL_02bb:  call       native int ShiftTest.byte32Test::f2(native int)
      IL_02c0:  ldc.i4.s   16
      IL_02c2:  beq.s      IL_02d3

      IL_02c4:  ldstr      "FAILED for function argument"
      IL_02c9:  call       void [System.Console]System.Console::WriteLine(string)
      IL_02ce:  ldc.i4.m1
      IL_02cf:  stloc.s    V_4
      IL_02d1:  br.s       IL_0344

      IL_02d3:  ldsfld     native int ShiftTest.byte32Test::s_data
      IL_02d8:  ldc.i4.s   16
      IL_02da:  beq.s      IL_02eb

      IL_02dc:  ldstr      "FAILED for static field"
      IL_02e1:  call       void [System.Console]System.Console::WriteLine(string)
      IL_02e6:  ldc.i4.m1
      IL_02e7:  stloc.s    V_4
      IL_02e9:  br.s       IL_0344

      IL_02eb:  ldloc.1
      IL_02ec:  ldc.i4.0
      IL_02ed:  ldelem.u1
      IL_02ee:  ldc.i4.s   16
      IL_02f0:  beq.s      IL_0301

      IL_02f2:  ldstr      "FAILED for array element"
      IL_02f7:  call       void [System.Console]System.Console::WriteLine(string)
      IL_02fc:  ldc.i4.m1
      IL_02fd:  stloc.s    V_4
      IL_02ff:  br.s       IL_0344

      IL_0301:  ldloc.2
      IL_0302:  ldfld      native int ShiftTest.CL::clm_data
      IL_0307:  ldc.i4.s   16
      IL_0309:  beq.s      IL_031a

      IL_030b:  ldstr      "FAILED for class member"
      IL_0310:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0315:  ldc.i4.m1
      IL_0316:  stloc.s    V_4
      IL_0318:  br.s       IL_0344

      IL_031a:  ldloca.s   V_3
      IL_031c:  ldfld      native int ShiftTest.VT::vtm_data
      IL_0321:  ldc.i4.s   16
      IL_0323:  beq.s      IL_0334

      IL_0325:  ldstr      "FAILED for valuestruct member"
      IL_032a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_032f:  ldc.i4.m1
      IL_0330:  stloc.s    V_4
      IL_0332:  br.s       IL_0344

      IL_0334:  ldstr      "PASSED"
      IL_0339:  call       void [System.Console]System.Console::WriteLine(string)
      IL_033e:  ldc.i4.s   100
      IL_0340:  stloc.s    V_4
      IL_0342:  br.s       IL_0344

      IL_0344:  ldloc.s    V_4
      IL_0346:  ret
    } // end of method byte32Test::Main

    .method private hidebysig specialname rtspecialname static
            void  .cctor() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  1
      IL_0000:  ldc.i4     0xff
      IL_0005:  stsfld     native int ShiftTest.byte32Test::s_data
      IL_000a:  ret
    } // end of method byte32Test::.cctor

    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method byte32Test::.ctor

  } // end of class byte32Test


// =============================================================

} // end of namespace ShiftTest

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file native int.res
