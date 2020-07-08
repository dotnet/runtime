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
.assembly ericswitch
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module ericswitch.exe
// MVID: {392F7FED-ADA8-4853-8A34-B9F492F92525}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
// Image base: 0x000006FBFEE80000

// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit MyApp
       extends [mscorlib]System.Object
{
  .method private hidebysig instance void 
          foo() cil managed
  {
    // Code size       595 (0x253)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1,
             int32 V_2)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldc.i4.0
    IL_0003:  stloc.1
    IL_0004:  ldloc.1
    IL_0005:  stloc.2
    IL_0006:  ldloc.2
    IL_0007:  switch     ( 
                          IL_00a1,
                          IL_00ac,
                          IL_00b7,
                          IL_00c2,
                          IL_00cd,
                          IL_00d9,
                          IL_00e8,
                          IL_00f3,
                          IL_00fe,
                          IL_0109,
                          IL_0114,
                          IL_011f,
                          IL_012a,
                          IL_0135,
                          IL_0140,
                          IL_014b,
                          IL_0156,
                          IL_0161,
                          IL_016c,
                          IL_0177,
                          IL_0182,
                          IL_018d,
                          IL_0198,
                          IL_01a3,
                          IL_01ae,
                          IL_01b9,
                          IL_01c4,
                          IL_01cf,
                          IL_01da,
                          IL_01e5,
                          IL_01f0,
                          IL_01fb,
                          IL_0206,
                          IL_0211,
                          IL_021c,
                          IL_0227)
    IL_009c:  br         IL_0232

    IL_00a1:  ldloc.0
    IL_00a2:  ldc.i4.1
    IL_00a3:  add
    IL_00a4:  stloc.0
    IL_00a5:  ldc.i4.7
//    IL_00a6:  stloc.1
    IL_00a7:  br         IL_0007

    IL_00ac:  ldloc.0
    IL_00ad:  ldc.i4.1
    IL_00ae:  add
    IL_00af:  stloc.0
    IL_00b0:  ldc.i4.6
//    IL_00b1:  stloc.1
    IL_00b2:  br         IL_0007

    IL_00b7:  ldloc.0
    IL_00b8:  ldc.i4.1
    IL_00b9:  add
    IL_00ba:  stloc.0
    IL_00bb:  ldc.i4.1
//    IL_00bc:  stloc.1
    IL_00bd:  br         IL_0007

    IL_00c2:  ldloc.0
    IL_00c3:  ldc.i4.1
    IL_00c4:  add
    IL_00c5:  stloc.0
    IL_00c6:  ldc.i4.8
//    IL_00c7:  stloc.1
    IL_00c8:  br         IL_0007

    IL_00cd:  ldloc.0
    IL_00ce:  ldc.i4.1
    IL_00cf:  add
    IL_00d0:  stloc.0
    IL_00d1:  ldc.i4.s   100
//    IL_00d3:  stloc.1
    IL_00d4:  br         IL_0007

    IL_00d9:  ldloc.0
    IL_00da:  ldc.i4.1
    IL_00db:  add
    IL_00dc:  stloc.0
    IL_00dd:  ldc.i4     0x3e8
//    IL_00e2:  stloc.1
    IL_00e3:  br         IL_0007

    IL_00e8:  ldloc.0
    IL_00e9:  ldc.i4.1
    IL_00ea:  add
    IL_00eb:  stloc.0
    IL_00ec:  ldc.i4.5
//    IL_00ed:  stloc.1
    IL_00ee:  br         IL_0007

    IL_00f3:  ldloc.0
    IL_00f4:  ldc.i4.1
    IL_00f5:  add
    IL_00f6:  stloc.0
    IL_00f7:  ldc.i4.3
//    IL_00f8:  stloc.1
    IL_00f9:  br         IL_0007

    IL_00fe:  ldloc.0
    IL_00ff:  ldc.i4.1
    IL_0100:  add
    IL_0101:  stloc.0
    IL_0102:  ldc.i4.1
//    IL_0103:  stloc.1
    IL_0104:  br         IL_0007

    IL_0109:  ldloc.0
    IL_010a:  ldc.i4.1
    IL_010b:  add
    IL_010c:  stloc.0
    IL_010d:  ldc.i4.2
//    IL_010e:  stloc.1
    IL_010f:  br         IL_0007

    IL_0114:  ldloc.0
    IL_0115:  ldc.i4.1
    IL_0116:  add
    IL_0117:  stloc.0
    IL_0118:  ldc.i4.2
//    IL_0119:  stloc.1
    IL_011a:  br         IL_0007

    IL_011f:  ldloc.0
    IL_0120:  ldc.i4.1
    IL_0121:  add
    IL_0122:  stloc.0
    IL_0123:  ldc.i4.2
//    IL_0124:  stloc.1
    IL_0125:  br         IL_0007

    IL_012a:  ldloc.0
    IL_012b:  ldc.i4.1
    IL_012c:  add
    IL_012d:  stloc.0
    IL_012e:  ldc.i4.2
//    IL_012f:  stloc.1
    IL_0130:  br         IL_0007

    IL_0135:  ldloc.0
    IL_0136:  ldc.i4.1
    IL_0137:  add
    IL_0138:  stloc.0
    IL_0139:  ldc.i4.2
//    IL_013a:  stloc.1
    IL_013b:  br         IL_0007

    IL_0140:  ldloc.0
    IL_0141:  ldc.i4.1
    IL_0142:  add
    IL_0143:  stloc.0
    IL_0144:  ldc.i4.2
//    IL_0145:  stloc.1
    IL_0146:  br         IL_0007

    IL_014b:  ldloc.0
    IL_014c:  ldc.i4.1
    IL_014d:  add
    IL_014e:  stloc.0
    IL_014f:  ldc.i4.2
//    IL_0150:  stloc.1
    IL_0151:  br         IL_0007

    IL_0156:  ldloc.0
    IL_0157:  ldc.i4.1
    IL_0158:  add
    IL_0159:  stloc.0
    IL_015a:  ldc.i4.2
//    IL_015b:  stloc.1
    IL_015c:  br         IL_0007

    IL_0161:  ldloc.0
    IL_0162:  ldc.i4.1
    IL_0163:  add
    IL_0164:  stloc.0
    IL_0165:  ldc.i4.2
//    IL_0166:  stloc.1
    IL_0167:  br         IL_0007

    IL_016c:  ldloc.0
    IL_016d:  ldc.i4.1
    IL_016e:  add
    IL_016f:  stloc.0
    IL_0170:  ldc.i4.2
//    IL_0171:  stloc.1
    IL_0172:  br         IL_0007

    IL_0177:  ldloc.0
    IL_0178:  ldc.i4.1
    IL_0179:  add
    IL_017a:  stloc.0
    IL_017b:  ldc.i4.2
//    IL_017c:  stloc.1
    IL_017d:  br         IL_0007

    IL_0182:  ldloc.0
    IL_0183:  ldc.i4.1
    IL_0184:  add
    IL_0185:  stloc.0
    IL_0186:  ldc.i4.2
//    IL_0187:  stloc.1
    IL_0188:  br         IL_0007

    IL_018d:  ldloc.0
    IL_018e:  ldc.i4.1
    IL_018f:  add
    IL_0190:  stloc.0
    IL_0191:  ldc.i4.2
//    IL_0192:  stloc.1
    IL_0193:  br         IL_0007

    IL_0198:  ldloc.0
    IL_0199:  ldc.i4.1
    IL_019a:  add
    IL_019b:  stloc.0
    IL_019c:  ldc.i4.2
//    IL_019d:  stloc.1
    IL_019e:  br         IL_0007

    IL_01a3:  ldloc.0
    IL_01a4:  ldc.i4.1
    IL_01a5:  add
    IL_01a6:  stloc.0
    IL_01a7:  ldc.i4.2
//    IL_01a8:  stloc.1
    IL_01a9:  br         IL_0007

    IL_01ae:  ldloc.0
    IL_01af:  ldc.i4.1
    IL_01b0:  add
    IL_01b1:  stloc.0
    IL_01b2:  ldc.i4.2
//    IL_01b3:  stloc.1
    IL_01b4:  br         IL_0007

    IL_01b9:  ldloc.0
    IL_01ba:  ldc.i4.1
    IL_01bb:  add
    IL_01bc:  stloc.0
    IL_01bd:  ldc.i4.2
//    IL_01be:  stloc.1
    IL_01bf:  br         IL_0007

    IL_01c4:  ldloc.0
    IL_01c5:  ldc.i4.1
    IL_01c6:  add
    IL_01c7:  stloc.0
    IL_01c8:  ldc.i4.2
//    IL_01c9:  stloc.1
    IL_01ca:  br         IL_0007

    IL_01cf:  ldloc.0
    IL_01d0:  ldc.i4.1
    IL_01d1:  add
    IL_01d2:  stloc.0
    IL_01d3:  ldc.i4.2
//    IL_01d4:  stloc.1
    IL_01d5:  br         IL_0007

    IL_01da:  ldloc.0
    IL_01db:  ldc.i4.1
    IL_01dc:  add
    IL_01dd:  stloc.0
    IL_01de:  ldc.i4.2
//    IL_01df:  stloc.1
    IL_01e0:  br         IL_0007

    IL_01e5:  ldloc.0
    IL_01e6:  ldc.i4.1
    IL_01e7:  add
    IL_01e8:  stloc.0
    IL_01e9:  ldc.i4.2
//    IL_01ea:  stloc.1
    IL_01eb:  br         IL_0007

    IL_01f0:  ldloc.0
    IL_01f1:  ldc.i4.1
    IL_01f2:  add
    IL_01f3:  stloc.0
    IL_01f4:  ldc.i4.2
//    IL_01f5:  stloc.1
    IL_01f6:  br         IL_0007

    IL_01fb:  ldloc.0
    IL_01fc:  ldc.i4.1
    IL_01fd:  add
    IL_01fe:  stloc.0
    IL_01ff:  ldc.i4.2
//    IL_0200:  stloc.1
    IL_0201:  br         IL_0007

    IL_0206:  ldloc.0
    IL_0207:  ldc.i4.1
    IL_0208:  add
    IL_0209:  stloc.0
    IL_020a:  ldc.i4.2
//    IL_020b:  stloc.1
    IL_020c:  br         IL_0007

    IL_0211:  ldloc.0
    IL_0212:  ldc.i4.1
    IL_0213:  add
    IL_0214:  stloc.0
    IL_0215:  ldc.i4.2
//    IL_0216:  stloc.1
    IL_0217:  br         IL_0007

    IL_021c:  ldloc.0
    IL_021d:  ldc.i4.1
    IL_021e:  add
    IL_021f:  stloc.0
    IL_0220:  ldc.i4.2
//    IL_0221:  stloc.1
    IL_0222:  br         IL_0007

    IL_0227:  ldloc.0
    IL_0228:  ldc.i4.1
    IL_0229:  add
    IL_022a:  stloc.0
    IL_022b:  ldc.i4.2
//    IL_022c:  stloc.1
    IL_022d:  br         IL_0007

    IL_0232:  ldstr      "r is {0}"
    IL_0237:  ldloc.1
    IL_0238:  box        [mscorlib]System.Int32
    IL_023d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0242:  ldstr      "i is {0}"
    IL_0247:  ldloc.0
    IL_0248:  box        [mscorlib]System.Int32
    IL_024d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0252:  ret
  } // end of method MyApp::foo

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       13 (0xd)
    .maxstack  1
    .locals init (class MyApp V_0)
    IL_0000:  newobj     instance void MyApp::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  callvirt   instance void MyApp::foo()
		ldc.i4 100
    IL_000c:  ret
  } // end of method MyApp::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyApp::.ctor

} // end of class MyApp


// =============================================================


//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file ericswitch.res
