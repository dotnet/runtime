// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly 'TypeLoadInitExcepBFI'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {A7BE6B3C-FF8D-43DB-B091-60CBA24AD082}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03090000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .field public static int32 i
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "In A.cctor"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.5
    IL_000d:  stsfld     int32 A::i
    IL_0012:  newobj     instance void [mscorlib]System.Exception::.ctor()
    IL_0017:  throw
  } // end of method A::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A::.ctor

} // end of class A

.class public sequential ansi sealed B
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
  .field public static int32 i
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "In B.cctor"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.5
    IL_000d:  stsfld     int32 B::i
    IL_0012:  newobj     instance void [mscorlib]System.Exception::.ctor()
    IL_0017:  throw
  } // end of method B::.cctor

} // end of class B

.class public auto ansi beforefieldinit Test_TypeLoadInitExcepBFI
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       405 (0x195)
    .maxstack  2
    .locals init (bool V_0,
             class [mscorlib]System.Exception V_1,
             int32 V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    .try
    {
      IL_0003:  nop
      IL_0004:  ldstr      "Accessing class's static field"
      IL_0009:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000e:  nop
      IL_000f:  ldstr      "A.i: "
      IL_0014:  ldsfld     int32 A::i
      IL_0019:  box        [mscorlib]System.Int32
      IL_001e:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0023:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0028:  nop
      IL_0029:  ldstr      "Did not catch expected TypeInitializationException"
      + " exception"
      IL_002e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0033:  nop
      IL_0034:  ldc.i4.0
      IL_0035:  stloc.0
      IL_0036:  nop
      IL_0037:  leave.s    IL_0061

    }  // end .try
    catch [mscorlib]System.TypeInitializationException 
    {
      IL_0039:  pop
      IL_003a:  nop
      IL_003b:  ldstr      "Caught expected exception 1st time"
      IL_0040:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0045:  nop
      IL_0046:  nop
      IL_0047:  leave.s    IL_0061

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_0049:  stloc.1
      IL_004a:  nop
      IL_004b:  ldstr      "Caught unexpected exception: "
      IL_0050:  ldloc.1
      IL_0051:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0056:  call       void [System.Console]System.Console::WriteLine(string)
      IL_005b:  nop
      IL_005c:  ldc.i4.0
      IL_005d:  stloc.0
      IL_005e:  nop
      IL_005f:  leave.s    IL_0061

    }  // end handler
    IL_0061:  nop
    .try
    {
      IL_0062:  nop
      IL_0063:  ldstr      "A.i: "
      IL_0068:  ldsfld     int32 A::i
      IL_006d:  box        [mscorlib]System.Int32
      IL_0072:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0077:  call       void [System.Console]System.Console::WriteLine(string)
      IL_007c:  nop
      IL_007d:  ldstr      "Did not catch expected TypeInitializationException"
      + " exception"
      IL_0082:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0087:  nop
      IL_0088:  ldc.i4.0
      IL_0089:  stloc.0
      IL_008a:  nop
      IL_008b:  leave.s    IL_00b5

    }  // end .try
    catch [mscorlib]System.TypeInitializationException 
    {
      IL_008d:  pop
      IL_008e:  nop
      IL_008f:  ldstr      "Caught expected exception 2nd time\n"
      IL_0094:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0099:  nop
      IL_009a:  nop
      IL_009b:  leave.s    IL_00b5

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_009d:  stloc.1
      IL_009e:  nop
      IL_009f:  ldstr      "Caught unexpected exception: "
      IL_00a4:  ldloc.1
      IL_00a5:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_00aa:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00af:  nop
      IL_00b0:  ldc.i4.0
      IL_00b1:  stloc.0
      IL_00b2:  nop
      IL_00b3:  leave.s    IL_00b5

    }  // end handler
    IL_00b5:  nop
    IL_00b6:  ldstr      "Accessing struct's static field"
    IL_00bb:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c0:  nop
    .try
    {
      IL_00c1:  nop
      IL_00c2:  ldstr      "B.i: "
      IL_00c7:  ldsfld     int32 B::i
      IL_00cc:  box        [mscorlib]System.Int32
      IL_00d1:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_00d6:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00db:  nop
      IL_00dc:  ldstr      "Did not catch expected TypeInitializationException"
      + " exception"
      IL_00e1:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00e6:  nop
      IL_00e7:  ldc.i4.0
      IL_00e8:  stloc.0
      IL_00e9:  nop
      IL_00ea:  leave.s    IL_0114

    }  // end .try
    catch [mscorlib]System.TypeInitializationException 
    {
      IL_00ec:  pop
      IL_00ed:  nop
      IL_00ee:  ldstr      "Caught expected exception 1st time"
      IL_00f3:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00f8:  nop
      IL_00f9:  nop
      IL_00fa:  leave.s    IL_0114

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_00fc:  stloc.1
      IL_00fd:  nop
      IL_00fe:  ldstr      "Caught unexpected exception: "
      IL_0103:  ldloc.1
      IL_0104:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0109:  call       void [System.Console]System.Console::WriteLine(string)
      IL_010e:  nop
      IL_010f:  ldc.i4.0
      IL_0110:  stloc.0
      IL_0111:  nop
      IL_0112:  leave.s    IL_0114

    }  // end handler
    IL_0114:  nop
    .try
    {
      IL_0115:  nop
      IL_0116:  ldstr      "B.i: "
      IL_011b:  ldsfld     int32 B::i
      IL_0120:  box        [mscorlib]System.Int32
      IL_0125:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_012a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_012f:  nop
      IL_0130:  ldstr      "Did not catch expected TypeInitializationException"
      + " exception"
      IL_0135:  call       void [System.Console]System.Console::WriteLine(string)
      IL_013a:  nop
      IL_013b:  ldc.i4.0
      IL_013c:  stloc.0
      IL_013d:  nop
      IL_013e:  leave.s    IL_0168

    }  // end .try
    catch [mscorlib]System.TypeInitializationException 
    {
      IL_0140:  pop
      IL_0141:  nop
      IL_0142:  ldstr      "Caught expected exception 2nd time\n"
      IL_0147:  call       void [System.Console]System.Console::WriteLine(string)
      IL_014c:  nop
      IL_014d:  nop
      IL_014e:  leave.s    IL_0168

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_0150:  stloc.1
      IL_0151:  nop
      IL_0152:  ldstr      "Caught unexpected exception: "
      IL_0157:  ldloc.1
      IL_0158:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_015d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0162:  nop
      IL_0163:  ldc.i4.0
      IL_0164:  stloc.0
      IL_0165:  nop
      IL_0166:  leave.s    IL_0168

    }  // end handler
    IL_0168:  nop
    IL_0169:  ldloc.0
    IL_016a:  ldc.i4.0
    IL_016b:  ceq
    IL_016d:  stloc.3
    IL_016e:  ldloc.3
    IL_016f:  brtrue.s   IL_0182

    IL_0171:  nop
    IL_0172:  ldstr      "PASS"
    IL_0177:  call       void [System.Console]System.Console::WriteLine(string)
    IL_017c:  nop
    IL_017d:  ldc.i4.s   100
    IL_017f:  stloc.2
    IL_0180:  br.s       IL_0193

    IL_0182:  nop
    IL_0183:  ldstr      "FAIL"
    IL_0188:  call       void [System.Console]System.Console::WriteLine(string)
    IL_018d:  nop
    IL_018e:  ldc.i4.s   101
    IL_0190:  stloc.2
    IL_0191:  br.s       IL_0193

    IL_0193:  ldloc.2
    IL_0194:  ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file TypeLoadInitExcepBFI.res
