// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.






// Metadata version: v4.0.21218
.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly Dev10_844071
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 4:0:30105:0
}
// MVID: {234C628E-92DC-420F-A9F2-3F461436B9D9}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00000000002B0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public sequential ansi sealed beforefieldinit MS.VT
       extends [mscorlib]System.ValueType
{
  .field private string path
  .field private int32 target
  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       104 (0x68)
    .maxstack  3
    .locals init (string V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      string MS.VT::path
    IL_0008:  ldstr      "->ToString"
    IL_000d:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0012:  stfld      string MS.VT::path
    IL_0017:  ldarg.0
    IL_0018:  ldfld      int32 MS.VT::target
    IL_001d:  stloc.1
    IL_001e:  ldloc.1
    IL_001f:  switch     ( 
                          IL_0032,
                          IL_003a,
                          IL_004a)
    IL_0030:  br.s       IL_0060

    IL_0032:  ldstr      "VT"
    IL_0037:  stloc.0
    IL_0038:  br.s       IL_0066

    IL_003a:  ldarg.0
    IL_003b:  ldc.i4.0
    IL_003c:  stfld      int32 MS.VT::target
    IL_0041:  ldarg.0
    IL_0042:  call       instance string MS.VT::ToStringHelper()
    IL_0047:  stloc.0
    IL_0048:  br.s       IL_0066

    IL_004a:  ldarg.0
    IL_004b:  ldc.i4.0
    IL_004c:  stfld      int32 MS.VT::target
    IL_0051:  ldarg.0
    IL_0052:  constrained. MS.VT
    IL_0058:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_005d:  stloc.0
    IL_005e:  br.s       IL_0066

    IL_0060:  newobj     instance void [mscorlib]System.Exception::.ctor()
    IL_0065:  throw

    IL_0066:  ldloc.0
    IL_0067:  ret
  } // end of method VT::ToString

.method public hidebysig instance string 
          ToStringHelper() cil managed
  {
    // Code size       148 (0x94)
    .maxstack  19
    .locals init (string V_0,
             valuetype StructInt32 V_1,
             int32 V_2)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      string MS.VT::path
    IL_0008:  ldstr      "->ToStringHelper"
    IL_000d:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0012:  stfld      string MS.VT::path
    IL_0017:  ldarg.0
    IL_0018:  ldfld      int32 MS.VT::target
    IL_001d:  stloc      V_2
    IL_0021:  ldloca.s   V_1
    IL_0023:  ldloc      V_2
    IL_0027:  stfld      int32 StructInt32::b0
    IL_002c:  ldloc      V_2
    IL_0030:  stloc      V_2
    IL_0034:  ldloca.s   V_1
    IL_0036:  ldfld      int32 StructInt32::b0
    IL_003b:  switch     ( 
                          IL_0051,
                          IL_005d,
                          IL_0071)
    IL_004c:  br         IL_008b

    IL_0051:  ldstr      "VT"
    IL_0056:  stloc.s    V_0
    IL_0058:  br         IL_0091

    IL_005d:  ldarg.0
    IL_005e:  ldc.i4.0
    IL_005f:  stfld      int32 MS.VT::target
    IL_0064:  ldarg.0
    IL_0065:  call       instance string MS.VT::ToStringHelper()
    IL_006a:  stloc.s    V_0
    IL_006c:  br         IL_0091

    IL_0071:  ldarg.0
    IL_0072:  ldc.i4.0
    IL_0073:  stfld      int32 MS.VT::target
    IL_0078:  ldarg.0
    IL_0079:  constrained. MS.VT
    IL_007f:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0084:  stloc.s    V_0
    IL_0086:  br         IL_0091

    IL_008b:  newobj     instance void [mscorlib]System.Exception::.ctor()
    IL_0090:  throw

    IL_0091:  ldloc.s    V_0
    IL_0093:  ret
  } // end of method VT::ToStringHelper

  .method private hidebysig instance bool 
          RunTests() cil managed
  {
    // Code size       451 (0x1c3)
    .maxstack  3
    .locals init (bool V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  stfld      int32 MS.VT::target
    IL_0008:  ldarg.0
    IL_0009:  ldnull
    IL_000a:  stfld      string MS.VT::path
    IL_000f:  ldarg.0
    IL_0010:  call       instance string MS.VT::ToStringHelper()
    IL_0015:  ldstr      " : "
    IL_001a:  ldarg.0
    IL_001b:  ldfld      string MS.VT::path
    IL_0020:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0025:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002a:  nop
    IL_002b:  ldarg.0
    IL_002c:  ldfld      string MS.VT::path
    IL_0031:  ldstr      "->ToStringHelper"
    IL_0036:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_003b:  ldc.i4.0
    IL_003c:  ceq
    IL_003e:  stloc.1
    IL_003f:  ldloc.1
    IL_0040:  brtrue.s   IL_0049

    IL_0042:  ldc.i4.0
    IL_0043:  stloc.0
    IL_0044:  br         IL_01c1

    IL_0049:  ldarg.0
    IL_004a:  ldc.i4.1
    IL_004b:  stfld      int32 MS.VT::target
    IL_0050:  ldarg.0
    IL_0051:  ldnull
    IL_0052:  stfld      string MS.VT::path
    IL_0057:  ldarg.0
    IL_0058:  call       instance string MS.VT::ToStringHelper()
    IL_005d:  ldstr      " : "
    IL_0062:  ldarg.0
    IL_0063:  ldfld      string MS.VT::path
    IL_0068:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_006d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0072:  nop
    IL_0073:  ldarg.0
    IL_0074:  ldfld      string MS.VT::path
    IL_0079:  ldstr      "->ToStringHelper->ToStringHelper"
    IL_007e:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0083:  ldc.i4.0
    IL_0084:  ceq
    IL_0086:  stloc.1
    IL_0087:  ldloc.1
    IL_0088:  brtrue.s   IL_0091

    IL_008a:  ldc.i4.0
    IL_008b:  stloc.0
    IL_008c:  br         IL_01c1

    IL_0091:  ldarg.0
    IL_0092:  ldc.i4.2
    IL_0093:  stfld      int32 MS.VT::target
    IL_0098:  ldarg.0
    IL_0099:  ldnull
    IL_009a:  stfld      string MS.VT::path
    IL_009f:  ldarg.0
    IL_00a0:  call       instance string MS.VT::ToStringHelper()
    IL_00a5:  ldstr      " : "
    IL_00aa:  ldarg.0
    IL_00ab:  ldfld      string MS.VT::path
    IL_00b0:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_00b5:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00ba:  nop
    IL_00bb:  ldarg.0
    IL_00bc:  ldfld      string MS.VT::path
    IL_00c1:  ldstr      "->ToStringHelper->ToString"
    IL_00c6:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_00cb:  ldc.i4.0
    IL_00cc:  ceq
    IL_00ce:  stloc.1
    IL_00cf:  ldloc.1
    IL_00d0:  brtrue.s   IL_00d9

    IL_00d2:  ldc.i4.0
    IL_00d3:  stloc.0
    IL_00d4:  br         IL_01c1

    IL_00d9:  ldarg.0
    IL_00da:  ldc.i4.0
    IL_00db:  stfld      int32 MS.VT::target
    IL_00e0:  ldarg.0
    IL_00e1:  ldnull
    IL_00e2:  stfld      string MS.VT::path
    IL_00e7:  ldarg.0
    IL_00e8:  constrained. MS.VT
    IL_00ee:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00f3:  ldstr      " : "
    IL_00f8:  ldarg.0
    IL_00f9:  ldfld      string MS.VT::path
    IL_00fe:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0103:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0108:  nop
    IL_0109:  ldarg.0
    IL_010a:  ldfld      string MS.VT::path
    IL_010f:  ldstr      "->ToString"
    IL_0114:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0119:  ldc.i4.0
    IL_011a:  ceq
    IL_011c:  stloc.1
    IL_011d:  ldloc.1
    IL_011e:  brtrue.s   IL_0127

    IL_0120:  ldc.i4.0
    IL_0121:  stloc.0
    IL_0122:  br         IL_01c1

    IL_0127:  ldarg.0
    IL_0128:  ldc.i4.1
    IL_0129:  stfld      int32 MS.VT::target
    IL_012e:  ldarg.0
    IL_012f:  ldnull
    IL_0130:  stfld      string MS.VT::path
    IL_0135:  ldarg.0
    IL_0136:  constrained. MS.VT
    IL_013c:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0141:  ldstr      " : "
    IL_0146:  ldarg.0
    IL_0147:  ldfld      string MS.VT::path
    IL_014c:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0151:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0156:  nop
    IL_0157:  ldarg.0
    IL_0158:  ldfld      string MS.VT::path
    IL_015d:  ldstr      "->ToString->ToStringHelper"
    IL_0162:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0167:  ldc.i4.0
    IL_0168:  ceq
    IL_016a:  stloc.1
    IL_016b:  ldloc.1
    IL_016c:  brtrue.s   IL_0172

    IL_016e:  ldc.i4.0
    IL_016f:  stloc.0
    IL_0170:  br.s       IL_01c1

    IL_0172:  ldarg.0
    IL_0173:  ldc.i4.2
    IL_0174:  stfld      int32 MS.VT::target
    IL_0179:  ldarg.0
    IL_017a:  ldnull
    IL_017b:  stfld      string MS.VT::path
    IL_0180:  ldarg.0
    IL_0181:  constrained. MS.VT
    IL_0187:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_018c:  ldstr      " : "
    IL_0191:  ldarg.0
    IL_0192:  ldfld      string MS.VT::path
    IL_0197:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_019c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01a1:  nop
    IL_01a2:  ldarg.0
    IL_01a3:  ldfld      string MS.VT::path
    IL_01a8:  ldstr      "->ToString->ToString"
    IL_01ad:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_01b2:  ldc.i4.0
    IL_01b3:  ceq
    IL_01b5:  stloc.1
    IL_01b6:  ldloc.1
    IL_01b7:  brtrue.s   IL_01bd

    IL_01b9:  ldc.i4.0
    IL_01ba:  stloc.0
    IL_01bb:  br.s       IL_01c1

    IL_01bd:  ldc.i4.1
    IL_01be:  stloc.0
    IL_01bf:  br.s       IL_01c1

    IL_01c1:  ldloc.0
    IL_01c2:  ret
  } // end of method VT::RunTests

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       60 (0x3c)
    .maxstack  2
    .locals init (int32 V_0,
             valuetype MS.VT V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_1
    IL_0003:  initobj    MS.VT
    IL_0009:  ldloc.1
    IL_000a:  stloc.1
    IL_000b:  ldloca.s   V_1
    IL_000d:  call       instance bool MS.VT::RunTests()
    IL_0012:  ldc.i4.0
    IL_0013:  ceq
    IL_0015:  stloc.2
    IL_0016:  ldloc.2
    IL_0017:  brtrue.s   IL_002a

    IL_0019:  nop
    IL_001a:  ldstr      "PASSED."
    IL_001f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0024:  nop
    IL_0025:  ldc.i4.s   100
    IL_0027:  stloc.0
    IL_0028:  br.s       IL_003a

    IL_002a:  ldstr      "FAILED."
    IL_002f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0034:  nop
    IL_0035:  ldc.i4.s   101
    IL_0037:  stloc.0
    IL_0038:  br.s       IL_003a

    IL_003a:  ldloc.0
    IL_003b:  ret
  } // end of method VT::Main

} // end of class MS.VT

.class public sequential ansi sealed beforefieldinit StructInt32
       extends [mscorlib]System.ValueType
{
  .field public int32 b0
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  16
    .locals init (valuetype [*]StructInt32 V_0,
             int32 V_1)
    IL_0000:  ldloca     V_0
    IL_0004:  ldc.i4     0x457
    IL_0009:  conv.u1
    IL_000a:  stfld      int32 StructInt32::b0
    IL_000f:  ret
  } // end of method StructInt32::.ctor

} // end of class StructInt32

// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file p.res

