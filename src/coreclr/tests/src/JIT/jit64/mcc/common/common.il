// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// Metadata version: v2.0.50727
.assembly extern mscorlib { auto }
.assembly extern System.Runtime.Extensions { auto }


.module common.netmodule
// MVID: {8A774AEE-5C13-4F6C-8345-881D79617488}
.imagebase 0x00400000
.file alignment 0x00001000
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x02DF0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit MCCTest.FormatUtils
       extends [mscorlib]System.Object
{
  .method public hidebysig static string
          GetPadding(int32 level) cil managed
  {
    // Code size       32 (0x20)
    .maxstack  4
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  bgt.s      IL_000c

    IL_0005:  ldsfld     string [mscorlib]System.String::Empty
    IL_000a:  br.s       IL_001b

    IL_000c:  ldsfld     string [mscorlib]System.String::Empty
    IL_0011:  ldc.i4.2
    IL_0012:  ldarg.0
    IL_0013:  mul
    IL_0014:  ldc.i4.s   32
    IL_0016:  callvirt   instance string [mscorlib]System.String::PadLeft(int32,
                                                                          char)
    IL_001b:  stloc.0
    IL_001c:  br.s       IL_001e

    IL_001e:  ldloc.0
    IL_001f:  ret
  } // end of method FormatUtils::GetPadding

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method FormatUtils::.ctor

} // end of class MCCTest.FormatUtils

.class public auto ansi beforefieldinit MCCTest.ResultVerificationException
       extends [mscorlib]System.Exception
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(string fieldName,
                               int64 actual,
                               int64 expected) cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  conv.r8
    IL_0004:  ldarg.3
    IL_0005:  conv.r8
    IL_0006:  call       instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_000b:  nop
    IL_000c:  nop
    IL_000d:  nop
    IL_000e:  ret
  } // end of method ResultVerificationException::.ctor

  .method public hidebysig specialname rtspecialname
          instance void  .ctor(string fieldName,
                               float64 actual,
                               float64 expected) cil managed
  {
    // Code size       34 (0x22)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldstr      "FAILED: [field {0}] => expected = {1}, actual = {2}"
    IL_0006:  ldarg.1
    IL_0007:  ldarg.3
    IL_0008:  box        [mscorlib]System.Double
    IL_000d:  ldarg.2
    IL_000e:  box        [mscorlib]System.Double
    IL_0013:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object,
                                                                object)
    IL_0018:  ldnull
    IL_0019:  call       instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  class MCCTest.ResultVerificationException)
    IL_001e:  nop
    IL_001f:  nop
    IL_0020:  nop
    IL_0021:  ret
  } // end of method ResultVerificationException::.ctor

  .method public hidebysig specialname rtspecialname
          instance void  .ctor(string fieldName,
                               class [mscorlib]System.Type 'type',
                               class MCCTest.ResultVerificationException innerException) cil managed
  {
    // Code size       28 (0x1c)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldstr      "FAILED: [field {0}] [type '{1}']"
    IL_0006:  ldarg.1
    IL_0007:  ldarg.2
    IL_0008:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_000d:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object)
    IL_0012:  ldarg.3
    IL_0013:  call       instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  class MCCTest.ResultVerificationException)
    IL_0018:  nop
    IL_0019:  nop
    IL_001a:  nop
    IL_001b:  ret
  } // end of method ResultVerificationException::.ctor

  .method public hidebysig specialname rtspecialname
          instance void  .ctor(string message) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldnull
    IL_0003:  call       instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  class MCCTest.ResultVerificationException)
    IL_0008:  nop
    IL_0009:  nop
    IL_000a:  nop
    IL_000b:  ret
  } // end of method ResultVerificationException::.ctor

  .method public hidebysig specialname rtspecialname
          instance void  .ctor(string message,
                               class MCCTest.ResultVerificationException innerException) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  call       instance void [mscorlib]System.Exception::.ctor(string,
                                                                                    class [mscorlib]System.Exception)
    IL_0008:  nop
    IL_0009:  nop
    IL_000a:  nop
    IL_000b:  ret
  } // end of method ResultVerificationException::.ctor

  .method public hidebysig instance string
          GetMessage() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.ResultVerificationException::GetMessage(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method ResultVerificationException::GetMessage

  .method public hidebysig instance string
          GetMessage(int32 level) cil managed
  {
    // Code size       67 (0x43)
    .maxstack  7
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  ldarg.0
    IL_0008:  callvirt   instance string [mscorlib]System.Exception::get_Message()
    IL_000d:  ldarg.0
    IL_000e:  call       instance class [mscorlib]System.Exception [mscorlib]System.Exception::get_InnerException()
    IL_0013:  brtrue.s   IL_001c

    IL_0015:  ldsfld     string [mscorlib]System.String::Empty
    IL_001a:  br.s       IL_0039

    IL_001c:  call       string [System.Runtime.Extensions]System.Environment::get_NewLine()
    IL_0021:  ldarg.0
    IL_0022:  call       instance class [mscorlib]System.Exception [mscorlib]System.Exception::get_InnerException()
    IL_0027:  castclass  MCCTest.ResultVerificationException
    IL_002c:  ldarg.1
    IL_002d:  ldc.i4.1
    IL_002e:  add
    IL_002f:  callvirt   instance string MCCTest.ResultVerificationException::GetMessage(int32)
    IL_0034:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0039:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_003e:  stloc.0
    IL_003f:  br.s       IL_0041

    IL_0041:  ldloc.0
    IL_0042:  ret
  } // end of method ResultVerificationException::GetMessage

} // end of class MCCTest.ResultVerificationException

.class interface public abstract auto ansi MCCTest.CType`1<T>
{
  .method public hidebysig newslot abstract virtual
          instance void  Init(int32 count) cil managed
  {
  } // end of method CType`1::Init

  .method public hidebysig newslot abstract virtual
          instance void  Init() cil managed
  {
  } // end of method CType`1::Init

  .method public hidebysig newslot abstract virtual
          instance void  Zero() cil managed
  {
  } // end of method CType`1::Zero

  .method public hidebysig newslot abstract virtual
          instance void  Check(!T expected) cil managed
  {
  } // end of method CType`1::Check

} // end of class MCCTest.CType`1

.class public sequential ansi sealed beforefieldinit MCCTest.VType0
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VType0>
{
  .field public int32 count
  .field public int64 sum
  .field public float64 average
  .field public int64 dummy1
  .field public float64 dummy2
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       54 (0x36)
    .maxstack  3
    .locals init (int32 V_0,
             int64 V_1,
             float64 V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  stloc.0
    IL_0003:  ldarg.1
    IL_0004:  ldarg.1
    IL_0005:  ldc.i4.1
    IL_0006:  add
    IL_0007:  mul
    IL_0008:  ldc.i4.2
    IL_0009:  div
    IL_000a:  conv.i8
    IL_000b:  stloc.1
    IL_000c:  ldloc.1
    IL_000d:  conv.r8
    IL_000e:  ldloc.0
    IL_000f:  conv.r8
    IL_0010:  div
    IL_0011:  stloc.2
    IL_0012:  ldarg.0
    IL_0013:  ldloc.0
    IL_0014:  stfld      int32 MCCTest.VType0::count
    IL_0019:  ldarg.0
    IL_001a:  ldloc.1
    IL_001b:  stfld      int64 MCCTest.VType0::sum
    IL_0020:  ldarg.0
    IL_0021:  ldloc.2
    IL_0022:  stfld      float64 MCCTest.VType0::average
    IL_0027:  ldarg.0
    IL_0028:  ldloc.1
    IL_0029:  stfld      int64 MCCTest.VType0::dummy1
    IL_002e:  ldarg.0
    IL_002f:  ldloc.2
    IL_0030:  stfld      float64 MCCTest.VType0::dummy2
    IL_0035:  ret
  } // end of method VType0::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VType0::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType0::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VType0::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType0::Zero

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VType0 expected) cil managed
  {
    // Code size       224 (0xe0)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      int32 MCCTest.VType0::count
    IL_0007:  ldarga.s   expected
    IL_0009:  ldfld      int32 MCCTest.VType0::count
    IL_000e:  ceq
    IL_0010:  stloc.0
    IL_0011:  ldloc.0
    IL_0012:  brtrue.s   IL_002f

    IL_0014:  nop
    IL_0015:  ldstr      "count"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      int32 MCCTest.VType0::count
    IL_0020:  conv.i8
    IL_0021:  ldarga.s   expected
    IL_0023:  ldfld      int32 MCCTest.VType0::count
    IL_0028:  conv.i8
    IL_0029:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_002e:  throw

    IL_002f:  ldarg.0
    IL_0030:  ldfld      int64 MCCTest.VType0::sum
    IL_0035:  ldarga.s   expected
    IL_0037:  ldfld      int64 MCCTest.VType0::sum
    IL_003c:  ceq
    IL_003e:  stloc.0
    IL_003f:  ldloc.0
    IL_0040:  brtrue.s   IL_005b

    IL_0042:  nop
    IL_0043:  ldstr      "sum"
    IL_0048:  ldarg.0
    IL_0049:  ldfld      int64 MCCTest.VType0::sum
    IL_004e:  ldarga.s   expected
    IL_0050:  ldfld      int64 MCCTest.VType0::sum
    IL_0055:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_005a:  throw

    IL_005b:  ldarg.0
    IL_005c:  ldfld      float64 MCCTest.VType0::average
    IL_0061:  ldarga.s   expected
    IL_0063:  ldfld      float64 MCCTest.VType0::average
    IL_0068:  ceq
    IL_006a:  stloc.0
    IL_006b:  ldloc.0
    IL_006c:  brtrue.s   IL_0087

    IL_006e:  nop
    IL_006f:  ldstr      "average"
    IL_0074:  ldarg.0
    IL_0075:  ldfld      float64 MCCTest.VType0::average
    IL_007a:  ldarga.s   expected
    IL_007c:  ldfld      float64 MCCTest.VType0::average
    IL_0081:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0086:  throw

    IL_0087:  ldarg.0
    IL_0088:  ldfld      int64 MCCTest.VType0::dummy1
    IL_008d:  ldarga.s   expected
    IL_008f:  ldfld      int64 MCCTest.VType0::dummy1
    IL_0094:  ceq
    IL_0096:  stloc.0
    IL_0097:  ldloc.0
    IL_0098:  brtrue.s   IL_00b3

    IL_009a:  nop
    IL_009b:  ldstr      "dummy1"
    IL_00a0:  ldarg.0
    IL_00a1:  ldfld      int64 MCCTest.VType0::dummy1
    IL_00a6:  ldarga.s   expected
    IL_00a8:  ldfld      int64 MCCTest.VType0::dummy1
    IL_00ad:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_00b2:  throw

    IL_00b3:  ldarg.0
    IL_00b4:  ldfld      float64 MCCTest.VType0::dummy2
    IL_00b9:  ldarga.s   expected
    IL_00bb:  ldfld      float64 MCCTest.VType0::dummy2
    IL_00c0:  ceq
    IL_00c2:  stloc.0
    IL_00c3:  ldloc.0
    IL_00c4:  brtrue.s   IL_00df

    IL_00c6:  nop
    IL_00c7:  ldstr      "dummy2"
    IL_00cc:  ldarg.0
    IL_00cd:  ldfld      float64 MCCTest.VType0::dummy2
    IL_00d2:  ldarga.s   expected
    IL_00d4:  ldfld      float64 MCCTest.VType0::dummy2
    IL_00d9:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00de:  throw

    IL_00df:  ret
  } // end of method VType0::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       170 (0xaa)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "count    = "
    IL_0015:  ldarg.0
    IL_0016:  ldfld      int32 MCCTest.VType0::count
    IL_001b:  box        [mscorlib]System.Int32
    IL_0020:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0025:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_002a:  nop
    IL_002b:  ldloc.1
    IL_002c:  ldloc.0
    IL_002d:  ldstr      "sum      = "
    IL_0032:  ldarg.0
    IL_0033:  ldfld      int64 MCCTest.VType0::sum
    IL_0038:  box        [mscorlib]System.Int64
    IL_003d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0042:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0047:  nop
    IL_0048:  ldloc.1
    IL_0049:  ldloc.0
    IL_004a:  ldstr      "average  = "
    IL_004f:  ldarg.0
    IL_0050:  ldfld      float64 MCCTest.VType0::average
    IL_0055:  box        [mscorlib]System.Double
    IL_005a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_005f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0064:  nop
    IL_0065:  ldloc.1
    IL_0066:  ldloc.0
    IL_0067:  ldstr      "dummy1   = "
    IL_006c:  ldarg.0
    IL_006d:  ldfld      int64 MCCTest.VType0::dummy1
    IL_0072:  box        [mscorlib]System.Int64
    IL_0077:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_007c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0081:  nop
    IL_0082:  ldloc.1
    IL_0083:  ldloc.0
    IL_0084:  ldstr      "dummy2   = "
    IL_0089:  ldarg.0
    IL_008a:  ldfld      float64 MCCTest.VType0::dummy2
    IL_008f:  box        [mscorlib]System.Double
    IL_0094:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0099:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_009e:  nop
    IL_009f:  ldloc.1
    IL_00a0:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00a5:  stloc.2
    IL_00a6:  br.s       IL_00a8

    IL_00a8:  ldloc.2
    IL_00a9:  ret
  } // end of method VType0::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VType0::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VType0::Dump

} // end of class MCCTest.VType0

.class public sequential ansi sealed beforefieldinit MCCTest.VType1
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VType1>
{
  .field public float32 count
  .field public float32 sum
  .field public float32 average
  .field public float32 count1
  .field public float32 sum1
  .field public float32 average1
  .field public float32 count2
  .field public float32 sum2
  .field public float32 average2
  .field public float32 count3
  .field public float32 sum3
  .field public float32 average3
  .field public float32 count4
  .field public float32 sum4
  .field public float32 average4
  .field public float32 count5
  .field public float32 sum5
  .field public float32 average5
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       159 (0x9f)
    .maxstack  3
    .locals init (float32 V_0,
             float32 V_1,
             float32 V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  conv.r4
    IL_0003:  stloc.0
    IL_0004:  ldarg.1
    IL_0005:  ldarg.1
    IL_0006:  ldc.i4.1
    IL_0007:  add
    IL_0008:  mul
    IL_0009:  ldc.i4.2
    IL_000a:  div
    IL_000b:  conv.r4
    IL_000c:  stloc.1
    IL_000d:  ldloc.0
    IL_000e:  ldc.r4     0.0
    IL_0013:  bgt.s      IL_001c

    IL_0015:  ldc.r4     0.0
    IL_001a:  br.s       IL_001f

    IL_001c:  ldloc.1
    IL_001d:  ldloc.0
    IL_001e:  div
    IL_001f:  stloc.2
    IL_0020:  ldarg.0
    IL_0021:  ldloc.0
    IL_0022:  stfld      float32 MCCTest.VType1::count
    IL_0027:  ldarg.0
    IL_0028:  ldloc.1
    IL_0029:  stfld      float32 MCCTest.VType1::sum
    IL_002e:  ldarg.0
    IL_002f:  ldloc.2
    IL_0030:  stfld      float32 MCCTest.VType1::average
    IL_0035:  ldarg.0
    IL_0036:  ldloc.0
    IL_0037:  stfld      float32 MCCTest.VType1::count1
    IL_003c:  ldarg.0
    IL_003d:  ldloc.1
    IL_003e:  stfld      float32 MCCTest.VType1::sum1
    IL_0043:  ldarg.0
    IL_0044:  ldloc.2
    IL_0045:  stfld      float32 MCCTest.VType1::average1
    IL_004a:  ldarg.0
    IL_004b:  ldloc.0
    IL_004c:  stfld      float32 MCCTest.VType1::count2
    IL_0051:  ldarg.0
    IL_0052:  ldloc.1
    IL_0053:  stfld      float32 MCCTest.VType1::sum2
    IL_0058:  ldarg.0
    IL_0059:  ldloc.2
    IL_005a:  stfld      float32 MCCTest.VType1::average2
    IL_005f:  ldarg.0
    IL_0060:  ldloc.0
    IL_0061:  stfld      float32 MCCTest.VType1::count3
    IL_0066:  ldarg.0
    IL_0067:  ldloc.1
    IL_0068:  stfld      float32 MCCTest.VType1::sum3
    IL_006d:  ldarg.0
    IL_006e:  ldloc.2
    IL_006f:  stfld      float32 MCCTest.VType1::average3
    IL_0074:  ldarg.0
    IL_0075:  ldloc.0
    IL_0076:  stfld      float32 MCCTest.VType1::count4
    IL_007b:  ldarg.0
    IL_007c:  ldloc.1
    IL_007d:  stfld      float32 MCCTest.VType1::sum4
    IL_0082:  ldarg.0
    IL_0083:  ldloc.2
    IL_0084:  stfld      float32 MCCTest.VType1::average4
    IL_0089:  ldarg.0
    IL_008a:  ldloc.0
    IL_008b:  stfld      float32 MCCTest.VType1::count5
    IL_0090:  ldarg.0
    IL_0091:  ldloc.1
    IL_0092:  stfld      float32 MCCTest.VType1::sum5
    IL_0097:  ldarg.0
    IL_0098:  ldloc.2
    IL_0099:  stfld      float32 MCCTest.VType1::average5
    IL_009e:  ret
  } // end of method VType1::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VType1::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType1::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VType1::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType1::Zero

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VType1 expected) cil managed
  {
    // Code size       830 (0x33e)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      float32 MCCTest.VType1::count
    IL_0007:  ldarga.s   expected
    IL_0009:  ldfld      float32 MCCTest.VType1::count
    IL_000e:  ceq
    IL_0010:  stloc.0
    IL_0011:  ldloc.0
    IL_0012:  brtrue.s   IL_002f

    IL_0014:  nop
    IL_0015:  ldstr      "count"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      float32 MCCTest.VType1::count
    IL_0020:  conv.r8
    IL_0021:  ldarga.s   expected
    IL_0023:  ldfld      float32 MCCTest.VType1::count
    IL_0028:  conv.r8
    IL_0029:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_002e:  throw

    IL_002f:  ldarg.0
    IL_0030:  ldfld      float32 MCCTest.VType1::sum
    IL_0035:  ldarga.s   expected
    IL_0037:  ldfld      float32 MCCTest.VType1::sum
    IL_003c:  ceq
    IL_003e:  stloc.0
    IL_003f:  ldloc.0
    IL_0040:  brtrue.s   IL_005d

    IL_0042:  nop
    IL_0043:  ldstr      "sum"
    IL_0048:  ldarg.0
    IL_0049:  ldfld      float32 MCCTest.VType1::sum
    IL_004e:  conv.r8
    IL_004f:  ldarga.s   expected
    IL_0051:  ldfld      float32 MCCTest.VType1::sum
    IL_0056:  conv.r8
    IL_0057:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_005c:  throw

    IL_005d:  ldarg.0
    IL_005e:  ldfld      float32 MCCTest.VType1::average
    IL_0063:  ldarga.s   expected
    IL_0065:  ldfld      float32 MCCTest.VType1::average
    IL_006a:  ceq
    IL_006c:  stloc.0
    IL_006d:  ldloc.0
    IL_006e:  brtrue.s   IL_008b

    IL_0070:  nop
    IL_0071:  ldstr      "average"
    IL_0076:  ldarg.0
    IL_0077:  ldfld      float32 MCCTest.VType1::average
    IL_007c:  conv.r8
    IL_007d:  ldarga.s   expected
    IL_007f:  ldfld      float32 MCCTest.VType1::average
    IL_0084:  conv.r8
    IL_0085:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_008a:  throw

    IL_008b:  ldarg.0
    IL_008c:  ldfld      float32 MCCTest.VType1::count1
    IL_0091:  ldarga.s   expected
    IL_0093:  ldfld      float32 MCCTest.VType1::count1
    IL_0098:  ceq
    IL_009a:  stloc.0
    IL_009b:  ldloc.0
    IL_009c:  brtrue.s   IL_00b9

    IL_009e:  nop
    IL_009f:  ldstr      "count1"
    IL_00a4:  ldarg.0
    IL_00a5:  ldfld      float32 MCCTest.VType1::count1
    IL_00aa:  conv.r8
    IL_00ab:  ldarga.s   expected
    IL_00ad:  ldfld      float32 MCCTest.VType1::count1
    IL_00b2:  conv.r8
    IL_00b3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00b8:  throw

    IL_00b9:  ldarg.0
    IL_00ba:  ldfld      float32 MCCTest.VType1::sum1
    IL_00bf:  ldarga.s   expected
    IL_00c1:  ldfld      float32 MCCTest.VType1::sum1
    IL_00c6:  ceq
    IL_00c8:  stloc.0
    IL_00c9:  ldloc.0
    IL_00ca:  brtrue.s   IL_00e7

    IL_00cc:  nop
    IL_00cd:  ldstr      "sum1"
    IL_00d2:  ldarg.0
    IL_00d3:  ldfld      float32 MCCTest.VType1::sum1
    IL_00d8:  conv.r8
    IL_00d9:  ldarga.s   expected
    IL_00db:  ldfld      float32 MCCTest.VType1::sum1
    IL_00e0:  conv.r8
    IL_00e1:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00e6:  throw

    IL_00e7:  ldarg.0
    IL_00e8:  ldfld      float32 MCCTest.VType1::average1
    IL_00ed:  ldarga.s   expected
    IL_00ef:  ldfld      float32 MCCTest.VType1::average1
    IL_00f4:  ceq
    IL_00f6:  stloc.0
    IL_00f7:  ldloc.0
    IL_00f8:  brtrue.s   IL_0115

    IL_00fa:  nop
    IL_00fb:  ldstr      "average1"
    IL_0100:  ldarg.0
    IL_0101:  ldfld      float32 MCCTest.VType1::average1
    IL_0106:  conv.r8
    IL_0107:  ldarga.s   expected
    IL_0109:  ldfld      float32 MCCTest.VType1::average1
    IL_010e:  conv.r8
    IL_010f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0114:  throw

    IL_0115:  ldarg.0
    IL_0116:  ldfld      float32 MCCTest.VType1::count2
    IL_011b:  ldarga.s   expected
    IL_011d:  ldfld      float32 MCCTest.VType1::count2
    IL_0122:  ceq
    IL_0124:  stloc.0
    IL_0125:  ldloc.0
    IL_0126:  brtrue.s   IL_0143

    IL_0128:  nop
    IL_0129:  ldstr      "count2"
    IL_012e:  ldarg.0
    IL_012f:  ldfld      float32 MCCTest.VType1::count2
    IL_0134:  conv.r8
    IL_0135:  ldarga.s   expected
    IL_0137:  ldfld      float32 MCCTest.VType1::count2
    IL_013c:  conv.r8
    IL_013d:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0142:  throw

    IL_0143:  ldarg.0
    IL_0144:  ldfld      float32 MCCTest.VType1::sum2
    IL_0149:  ldarga.s   expected
    IL_014b:  ldfld      float32 MCCTest.VType1::sum2
    IL_0150:  ceq
    IL_0152:  stloc.0
    IL_0153:  ldloc.0
    IL_0154:  brtrue.s   IL_0171

    IL_0156:  nop
    IL_0157:  ldstr      "sum2"
    IL_015c:  ldarg.0
    IL_015d:  ldfld      float32 MCCTest.VType1::sum2
    IL_0162:  conv.r8
    IL_0163:  ldarga.s   expected
    IL_0165:  ldfld      float32 MCCTest.VType1::sum2
    IL_016a:  conv.r8
    IL_016b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0170:  throw

    IL_0171:  ldarg.0
    IL_0172:  ldfld      float32 MCCTest.VType1::average2
    IL_0177:  ldarga.s   expected
    IL_0179:  ldfld      float32 MCCTest.VType1::average2
    IL_017e:  ceq
    IL_0180:  stloc.0
    IL_0181:  ldloc.0
    IL_0182:  brtrue.s   IL_019f

    IL_0184:  nop
    IL_0185:  ldstr      "average2"
    IL_018a:  ldarg.0
    IL_018b:  ldfld      float32 MCCTest.VType1::average2
    IL_0190:  conv.r8
    IL_0191:  ldarga.s   expected
    IL_0193:  ldfld      float32 MCCTest.VType1::average2
    IL_0198:  conv.r8
    IL_0199:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_019e:  throw

    IL_019f:  ldarg.0
    IL_01a0:  ldfld      float32 MCCTest.VType1::count3
    IL_01a5:  ldarga.s   expected
    IL_01a7:  ldfld      float32 MCCTest.VType1::count3
    IL_01ac:  ceq
    IL_01ae:  stloc.0
    IL_01af:  ldloc.0
    IL_01b0:  brtrue.s   IL_01cd

    IL_01b2:  nop
    IL_01b3:  ldstr      "count3"
    IL_01b8:  ldarg.0
    IL_01b9:  ldfld      float32 MCCTest.VType1::count3
    IL_01be:  conv.r8
    IL_01bf:  ldarga.s   expected
    IL_01c1:  ldfld      float32 MCCTest.VType1::count3
    IL_01c6:  conv.r8
    IL_01c7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_01cc:  throw

    IL_01cd:  ldarg.0
    IL_01ce:  ldfld      float32 MCCTest.VType1::sum3
    IL_01d3:  ldarga.s   expected
    IL_01d5:  ldfld      float32 MCCTest.VType1::sum3
    IL_01da:  ceq
    IL_01dc:  stloc.0
    IL_01dd:  ldloc.0
    IL_01de:  brtrue.s   IL_01fb

    IL_01e0:  nop
    IL_01e1:  ldstr      "sum3"
    IL_01e6:  ldarg.0
    IL_01e7:  ldfld      float32 MCCTest.VType1::sum3
    IL_01ec:  conv.r8
    IL_01ed:  ldarga.s   expected
    IL_01ef:  ldfld      float32 MCCTest.VType1::sum3
    IL_01f4:  conv.r8
    IL_01f5:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_01fa:  throw

    IL_01fb:  ldarg.0
    IL_01fc:  ldfld      float32 MCCTest.VType1::average3
    IL_0201:  ldarga.s   expected
    IL_0203:  ldfld      float32 MCCTest.VType1::average3
    IL_0208:  ceq
    IL_020a:  stloc.0
    IL_020b:  ldloc.0
    IL_020c:  brtrue.s   IL_0229

    IL_020e:  nop
    IL_020f:  ldstr      "average3"
    IL_0214:  ldarg.0
    IL_0215:  ldfld      float32 MCCTest.VType1::average3
    IL_021a:  conv.r8
    IL_021b:  ldarga.s   expected
    IL_021d:  ldfld      float32 MCCTest.VType1::average3
    IL_0222:  conv.r8
    IL_0223:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0228:  throw

    IL_0229:  ldarg.0
    IL_022a:  ldfld      float32 MCCTest.VType1::count4
    IL_022f:  ldarga.s   expected
    IL_0231:  ldfld      float32 MCCTest.VType1::count4
    IL_0236:  ceq
    IL_0238:  stloc.0
    IL_0239:  ldloc.0
    IL_023a:  brtrue.s   IL_0257

    IL_023c:  nop
    IL_023d:  ldstr      "count4"
    IL_0242:  ldarg.0
    IL_0243:  ldfld      float32 MCCTest.VType1::count4
    IL_0248:  conv.r8
    IL_0249:  ldarga.s   expected
    IL_024b:  ldfld      float32 MCCTest.VType1::count4
    IL_0250:  conv.r8
    IL_0251:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0256:  throw

    IL_0257:  ldarg.0
    IL_0258:  ldfld      float32 MCCTest.VType1::sum4
    IL_025d:  ldarga.s   expected
    IL_025f:  ldfld      float32 MCCTest.VType1::sum4
    IL_0264:  ceq
    IL_0266:  stloc.0
    IL_0267:  ldloc.0
    IL_0268:  brtrue.s   IL_0285

    IL_026a:  nop
    IL_026b:  ldstr      "sum4"
    IL_0270:  ldarg.0
    IL_0271:  ldfld      float32 MCCTest.VType1::sum4
    IL_0276:  conv.r8
    IL_0277:  ldarga.s   expected
    IL_0279:  ldfld      float32 MCCTest.VType1::sum4
    IL_027e:  conv.r8
    IL_027f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0284:  throw

    IL_0285:  ldarg.0
    IL_0286:  ldfld      float32 MCCTest.VType1::average4
    IL_028b:  ldarga.s   expected
    IL_028d:  ldfld      float32 MCCTest.VType1::average4
    IL_0292:  ceq
    IL_0294:  stloc.0
    IL_0295:  ldloc.0
    IL_0296:  brtrue.s   IL_02b3

    IL_0298:  nop
    IL_0299:  ldstr      "average4"
    IL_029e:  ldarg.0
    IL_029f:  ldfld      float32 MCCTest.VType1::average4
    IL_02a4:  conv.r8
    IL_02a5:  ldarga.s   expected
    IL_02a7:  ldfld      float32 MCCTest.VType1::average4
    IL_02ac:  conv.r8
    IL_02ad:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_02b2:  throw

    IL_02b3:  ldarg.0
    IL_02b4:  ldfld      float32 MCCTest.VType1::count5
    IL_02b9:  ldarga.s   expected
    IL_02bb:  ldfld      float32 MCCTest.VType1::count5
    IL_02c0:  ceq
    IL_02c2:  stloc.0
    IL_02c3:  ldloc.0
    IL_02c4:  brtrue.s   IL_02e1

    IL_02c6:  nop
    IL_02c7:  ldstr      "count5"
    IL_02cc:  ldarg.0
    IL_02cd:  ldfld      float32 MCCTest.VType1::count5
    IL_02d2:  conv.r8
    IL_02d3:  ldarga.s   expected
    IL_02d5:  ldfld      float32 MCCTest.VType1::count5
    IL_02da:  conv.r8
    IL_02db:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_02e0:  throw

    IL_02e1:  ldarg.0
    IL_02e2:  ldfld      float32 MCCTest.VType1::sum5
    IL_02e7:  ldarga.s   expected
    IL_02e9:  ldfld      float32 MCCTest.VType1::sum5
    IL_02ee:  ceq
    IL_02f0:  stloc.0
    IL_02f1:  ldloc.0
    IL_02f2:  brtrue.s   IL_030f

    IL_02f4:  nop
    IL_02f5:  ldstr      "sum5"
    IL_02fa:  ldarg.0
    IL_02fb:  ldfld      float32 MCCTest.VType1::sum5
    IL_0300:  conv.r8
    IL_0301:  ldarga.s   expected
    IL_0303:  ldfld      float32 MCCTest.VType1::sum5
    IL_0308:  conv.r8
    IL_0309:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_030e:  throw

    IL_030f:  ldarg.0
    IL_0310:  ldfld      float32 MCCTest.VType1::average5
    IL_0315:  ldarga.s   expected
    IL_0317:  ldfld      float32 MCCTest.VType1::average5
    IL_031c:  ceq
    IL_031e:  stloc.0
    IL_031f:  ldloc.0
    IL_0320:  brtrue.s   IL_033d

    IL_0322:  nop
    IL_0323:  ldstr      "average5"
    IL_0328:  ldarg.0
    IL_0329:  ldfld      float32 MCCTest.VType1::average5
    IL_032e:  conv.r8
    IL_032f:  ldarga.s   expected
    IL_0331:  ldfld      float32 MCCTest.VType1::average5
    IL_0336:  conv.r8
    IL_0337:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_033c:  throw

    IL_033d:  ret
  } // end of method VType1::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       547 (0x223)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "count    = "
    IL_0015:  ldarg.0
    IL_0016:  ldfld      float32 MCCTest.VType1::count
    IL_001b:  box        [mscorlib]System.Single
    IL_0020:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0025:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_002a:  nop
    IL_002b:  ldloc.1
    IL_002c:  ldloc.0
    IL_002d:  ldstr      "sum      = "
    IL_0032:  ldarg.0
    IL_0033:  ldfld      float32 MCCTest.VType1::sum
    IL_0038:  box        [mscorlib]System.Single
    IL_003d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0042:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0047:  nop
    IL_0048:  ldloc.1
    IL_0049:  ldloc.0
    IL_004a:  ldstr      "average  = "
    IL_004f:  ldarg.0
    IL_0050:  ldfld      float32 MCCTest.VType1::average
    IL_0055:  box        [mscorlib]System.Single
    IL_005a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_005f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0064:  nop
    IL_0065:  ldloc.1
    IL_0066:  ldloc.0
    IL_0067:  ldstr      "count1   = "
    IL_006c:  ldarg.0
    IL_006d:  ldfld      float32 MCCTest.VType1::count1
    IL_0072:  box        [mscorlib]System.Single
    IL_0077:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_007c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0081:  nop
    IL_0082:  ldloc.1
    IL_0083:  ldloc.0
    IL_0084:  ldstr      "sum1     = "
    IL_0089:  ldarg.0
    IL_008a:  ldfld      float32 MCCTest.VType1::sum1
    IL_008f:  box        [mscorlib]System.Single
    IL_0094:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0099:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_009e:  nop
    IL_009f:  ldloc.1
    IL_00a0:  ldloc.0
    IL_00a1:  ldstr      "average1 = "
    IL_00a6:  ldarg.0
    IL_00a7:  ldfld      float32 MCCTest.VType1::average1
    IL_00ac:  box        [mscorlib]System.Single
    IL_00b1:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00b6:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00bb:  nop
    IL_00bc:  ldloc.1
    IL_00bd:  ldloc.0
    IL_00be:  ldstr      "count2   = "
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      float32 MCCTest.VType1::count2
    IL_00c9:  box        [mscorlib]System.Single
    IL_00ce:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00d3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00d8:  nop
    IL_00d9:  ldloc.1
    IL_00da:  ldloc.0
    IL_00db:  ldstr      "sum2     = "
    IL_00e0:  ldarg.0
    IL_00e1:  ldfld      float32 MCCTest.VType1::sum2
    IL_00e6:  box        [mscorlib]System.Single
    IL_00eb:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00f0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00f5:  nop
    IL_00f6:  ldloc.1
    IL_00f7:  ldloc.0
    IL_00f8:  ldstr      "average2 = "
    IL_00fd:  ldarg.0
    IL_00fe:  ldfld      float32 MCCTest.VType1::average2
    IL_0103:  box        [mscorlib]System.Single
    IL_0108:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_010d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0112:  nop
    IL_0113:  ldloc.1
    IL_0114:  ldloc.0
    IL_0115:  ldstr      "count3   = "
    IL_011a:  ldarg.0
    IL_011b:  ldfld      float32 MCCTest.VType1::count3
    IL_0120:  box        [mscorlib]System.Single
    IL_0125:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_012a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_012f:  nop
    IL_0130:  ldloc.1
    IL_0131:  ldloc.0
    IL_0132:  ldstr      "sum3     = "
    IL_0137:  ldarg.0
    IL_0138:  ldfld      float32 MCCTest.VType1::sum3
    IL_013d:  box        [mscorlib]System.Single
    IL_0142:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0147:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_014c:  nop
    IL_014d:  ldloc.1
    IL_014e:  ldloc.0
    IL_014f:  ldstr      "average3 = "
    IL_0154:  ldarg.0
    IL_0155:  ldfld      float32 MCCTest.VType1::average3
    IL_015a:  box        [mscorlib]System.Single
    IL_015f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0164:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0169:  nop
    IL_016a:  ldloc.1
    IL_016b:  ldloc.0
    IL_016c:  ldstr      "count4   = "
    IL_0171:  ldarg.0
    IL_0172:  ldfld      float32 MCCTest.VType1::count4
    IL_0177:  box        [mscorlib]System.Single
    IL_017c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0181:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0186:  nop
    IL_0187:  ldloc.1
    IL_0188:  ldloc.0
    IL_0189:  ldstr      "sum4     = "
    IL_018e:  ldarg.0
    IL_018f:  ldfld      float32 MCCTest.VType1::sum4
    IL_0194:  box        [mscorlib]System.Single
    IL_0199:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_019e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01a3:  nop
    IL_01a4:  ldloc.1
    IL_01a5:  ldloc.0
    IL_01a6:  ldstr      "average4 = "
    IL_01ab:  ldarg.0
    IL_01ac:  ldfld      float32 MCCTest.VType1::average4
    IL_01b1:  box        [mscorlib]System.Single
    IL_01b6:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01bb:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01c0:  nop
    IL_01c1:  ldloc.1
    IL_01c2:  ldloc.0
    IL_01c3:  ldstr      "count5   = "
    IL_01c8:  ldarg.0
    IL_01c9:  ldfld      float32 MCCTest.VType1::count5
    IL_01ce:  box        [mscorlib]System.Single
    IL_01d3:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01d8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01dd:  nop
    IL_01de:  ldloc.1
    IL_01df:  ldloc.0
    IL_01e0:  ldstr      "sum5     = "
    IL_01e5:  ldarg.0
    IL_01e6:  ldfld      float32 MCCTest.VType1::sum5
    IL_01eb:  box        [mscorlib]System.Single
    IL_01f0:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01f5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01fa:  nop
    IL_01fb:  ldloc.1
    IL_01fc:  ldloc.0
    IL_01fd:  ldstr      "average5 = "
    IL_0202:  ldarg.0
    IL_0203:  ldfld      float32 MCCTest.VType1::average5
    IL_0208:  box        [mscorlib]System.Single
    IL_020d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0212:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0217:  nop
    IL_0218:  ldloc.1
    IL_0219:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_021e:  stloc.2
    IL_021f:  br.s       IL_0221

    IL_0221:  ldloc.2
    IL_0222:  ret
  } // end of method VType1::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VType1::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VType1::Dump

} // end of class MCCTest.VType1

.class public sequential ansi sealed beforefieldinit MCCTest.VType2
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VType2>
{
  .field public int32 f1
  .field public float32 f2
  .field public int16 f3
  .field public float64 f4
  .field public int64 f5
  .field public float32 f6
  .field public float64 f7
  .field public char f8
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       65 (0x41)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  stfld      int32 MCCTest.VType2::f1
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  conv.r4
    IL_000b:  stfld      float32 MCCTest.VType2::f2
    IL_0010:  ldarg.0
    IL_0011:  ldarg.1
    IL_0012:  conv.i2
    IL_0013:  stfld      int16 MCCTest.VType2::f3
    IL_0018:  ldarg.0
    IL_0019:  ldarg.1
    IL_001a:  conv.r8
    IL_001b:  stfld      float64 MCCTest.VType2::f4
    IL_0020:  ldarg.0
    IL_0021:  ldarg.1
    IL_0022:  conv.i8
    IL_0023:  stfld      int64 MCCTest.VType2::f5
    IL_0028:  ldarg.0
    IL_0029:  ldarg.1
    IL_002a:  conv.r4
    IL_002b:  stfld      float32 MCCTest.VType2::f6
    IL_0030:  ldarg.0
    IL_0031:  ldarg.1
    IL_0032:  conv.r8
    IL_0033:  stfld      float64 MCCTest.VType2::f7
    IL_0038:  ldarg.0
    IL_0039:  ldarg.1
    IL_003a:  conv.u2
    IL_003b:  stfld      char MCCTest.VType2::f8
    IL_0040:  ret
  } // end of method VType2::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VType2::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType2::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VType2::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType2::Zero

  .method public hidebysig instance void
          Add(valuetype MCCTest.VType2 val) cil managed
  {
    // Code size       164 (0xa4)
    .maxstack  3
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 MCCTest.VType2::f1
    IL_0008:  ldarga.s   val
    IL_000a:  ldfld      int32 MCCTest.VType2::f1
    IL_000f:  add
    IL_0010:  stfld      int32 MCCTest.VType2::f1
    IL_0015:  ldarg.0
    IL_0016:  dup
    IL_0017:  ldfld      float32 MCCTest.VType2::f2
    IL_001c:  ldarga.s   val
    IL_001e:  ldfld      float32 MCCTest.VType2::f2
    IL_0023:  add
    IL_0024:  stfld      float32 MCCTest.VType2::f2
    IL_0029:  ldarg.0
    IL_002a:  dup
    IL_002b:  ldfld      int16 MCCTest.VType2::f3
    IL_0030:  ldarga.s   val
    IL_0032:  ldfld      int16 MCCTest.VType2::f3
    IL_0037:  add
    IL_0038:  conv.i2
    IL_0039:  stfld      int16 MCCTest.VType2::f3
    IL_003e:  ldarg.0
    IL_003f:  dup
    IL_0040:  ldfld      float64 MCCTest.VType2::f4
    IL_0045:  ldarga.s   val
    IL_0047:  ldfld      float64 MCCTest.VType2::f4
    IL_004c:  add
    IL_004d:  stfld      float64 MCCTest.VType2::f4
    IL_0052:  ldarg.0
    IL_0053:  dup
    IL_0054:  ldfld      int64 MCCTest.VType2::f5
    IL_0059:  ldarga.s   val
    IL_005b:  ldfld      int64 MCCTest.VType2::f5
    IL_0060:  add
    IL_0061:  stfld      int64 MCCTest.VType2::f5
    IL_0066:  ldarg.0
    IL_0067:  dup
    IL_0068:  ldfld      float32 MCCTest.VType2::f6
    IL_006d:  ldarga.s   val
    IL_006f:  ldfld      float32 MCCTest.VType2::f6
    IL_0074:  add
    IL_0075:  stfld      float32 MCCTest.VType2::f6
    IL_007a:  ldarg.0
    IL_007b:  dup
    IL_007c:  ldfld      float64 MCCTest.VType2::f7
    IL_0081:  ldarga.s   val
    IL_0083:  ldfld      float64 MCCTest.VType2::f7
    IL_0088:  add
    IL_0089:  stfld      float64 MCCTest.VType2::f7
    IL_008e:  ldarg.0
    IL_008f:  dup
    IL_0090:  ldfld      char MCCTest.VType2::f8
    IL_0095:  ldarga.s   val
    IL_0097:  ldfld      char MCCTest.VType2::f8
    IL_009c:  add
    IL_009d:  conv.u2
    IL_009e:  stfld      char MCCTest.VType2::f8
    IL_00a3:  ret
  } // end of method VType2::Add

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VType2 expected) cil managed
  {
    // Code size       364 (0x16c)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      int32 MCCTest.VType2::f1
    IL_0007:  ldarga.s   expected
    IL_0009:  ldfld      int32 MCCTest.VType2::f1
    IL_000e:  ceq
    IL_0010:  stloc.0
    IL_0011:  ldloc.0
    IL_0012:  brtrue.s   IL_002f

    IL_0014:  nop
    IL_0015:  ldstr      "f1"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      int32 MCCTest.VType2::f1
    IL_0020:  conv.i8
    IL_0021:  ldarga.s   expected
    IL_0023:  ldfld      int32 MCCTest.VType2::f1
    IL_0028:  conv.i8
    IL_0029:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_002e:  throw

    IL_002f:  ldarg.0
    IL_0030:  ldfld      float32 MCCTest.VType2::f2
    IL_0035:  ldarga.s   expected
    IL_0037:  ldfld      float32 MCCTest.VType2::f2
    IL_003c:  ceq
    IL_003e:  stloc.0
    IL_003f:  ldloc.0
    IL_0040:  brtrue.s   IL_005d

    IL_0042:  nop
    IL_0043:  ldstr      "f2"
    IL_0048:  ldarg.0
    IL_0049:  ldfld      float32 MCCTest.VType2::f2
    IL_004e:  conv.r8
    IL_004f:  ldarga.s   expected
    IL_0051:  ldfld      float32 MCCTest.VType2::f2
    IL_0056:  conv.r8
    IL_0057:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_005c:  throw

    IL_005d:  ldarg.0
    IL_005e:  ldfld      int16 MCCTest.VType2::f3
    IL_0063:  ldarga.s   expected
    IL_0065:  ldfld      int16 MCCTest.VType2::f3
    IL_006a:  ceq
    IL_006c:  stloc.0
    IL_006d:  ldloc.0
    IL_006e:  brtrue.s   IL_008b

    IL_0070:  nop
    IL_0071:  ldstr      "f3"
    IL_0076:  ldarg.0
    IL_0077:  ldfld      int16 MCCTest.VType2::f3
    IL_007c:  conv.i8
    IL_007d:  ldarga.s   expected
    IL_007f:  ldfld      int16 MCCTest.VType2::f3
    IL_0084:  conv.i8
    IL_0085:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_008a:  throw

    IL_008b:  ldarg.0
    IL_008c:  ldfld      float64 MCCTest.VType2::f4
    IL_0091:  ldarga.s   expected
    IL_0093:  ldfld      float64 MCCTest.VType2::f4
    IL_0098:  ceq
    IL_009a:  stloc.0
    IL_009b:  ldloc.0
    IL_009c:  brtrue.s   IL_00b7

    IL_009e:  nop
    IL_009f:  ldstr      "f4"
    IL_00a4:  ldarg.0
    IL_00a5:  ldfld      float64 MCCTest.VType2::f4
    IL_00aa:  ldarga.s   expected
    IL_00ac:  ldfld      float64 MCCTest.VType2::f4
    IL_00b1:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00b6:  throw

    IL_00b7:  ldarg.0
    IL_00b8:  ldfld      int64 MCCTest.VType2::f5
    IL_00bd:  ldarga.s   expected
    IL_00bf:  ldfld      int64 MCCTest.VType2::f5
    IL_00c4:  ceq
    IL_00c6:  stloc.0
    IL_00c7:  ldloc.0
    IL_00c8:  brtrue.s   IL_00e3

    IL_00ca:  nop
    IL_00cb:  ldstr      "f5"
    IL_00d0:  ldarg.0
    IL_00d1:  ldfld      int64 MCCTest.VType2::f5
    IL_00d6:  ldarga.s   expected
    IL_00d8:  ldfld      int64 MCCTest.VType2::f5
    IL_00dd:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_00e2:  throw

    IL_00e3:  ldarg.0
    IL_00e4:  ldfld      float32 MCCTest.VType2::f6
    IL_00e9:  ldarga.s   expected
    IL_00eb:  ldfld      float32 MCCTest.VType2::f6
    IL_00f0:  ceq
    IL_00f2:  stloc.0
    IL_00f3:  ldloc.0
    IL_00f4:  brtrue.s   IL_0111

    IL_00f6:  nop
    IL_00f7:  ldstr      "f6"
    IL_00fc:  ldarg.0
    IL_00fd:  ldfld      float32 MCCTest.VType2::f6
    IL_0102:  conv.r8
    IL_0103:  ldarga.s   expected
    IL_0105:  ldfld      float32 MCCTest.VType2::f6
    IL_010a:  conv.r8
    IL_010b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0110:  throw

    IL_0111:  ldarg.0
    IL_0112:  ldfld      float64 MCCTest.VType2::f7
    IL_0117:  ldarga.s   expected
    IL_0119:  ldfld      float64 MCCTest.VType2::f7
    IL_011e:  ceq
    IL_0120:  stloc.0
    IL_0121:  ldloc.0
    IL_0122:  brtrue.s   IL_013d

    IL_0124:  nop
    IL_0125:  ldstr      "f7"
    IL_012a:  ldarg.0
    IL_012b:  ldfld      float64 MCCTest.VType2::f7
    IL_0130:  ldarga.s   expected
    IL_0132:  ldfld      float64 MCCTest.VType2::f7
    IL_0137:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_013c:  throw

    IL_013d:  ldarg.0
    IL_013e:  ldfld      char MCCTest.VType2::f8
    IL_0143:  ldarga.s   expected
    IL_0145:  ldfld      char MCCTest.VType2::f8
    IL_014a:  ceq
    IL_014c:  stloc.0
    IL_014d:  ldloc.0
    IL_014e:  brtrue.s   IL_016b

    IL_0150:  nop
    IL_0151:  ldstr      "f8"
    IL_0156:  ldarg.0
    IL_0157:  ldfld      char MCCTest.VType2::f8
    IL_015c:  conv.u8
    IL_015d:  ldarga.s   expected
    IL_015f:  ldfld      char MCCTest.VType2::f8
    IL_0164:  conv.u8
    IL_0165:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_016a:  throw

    IL_016b:  ret
  } // end of method VType2::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       257 (0x101)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "f1    = "
    IL_0015:  ldarg.0
    IL_0016:  ldfld      int32 MCCTest.VType2::f1
    IL_001b:  box        [mscorlib]System.Int32
    IL_0020:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0025:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_002a:  nop
    IL_002b:  ldloc.1
    IL_002c:  ldloc.0
    IL_002d:  ldstr      "f2    = "
    IL_0032:  ldarg.0
    IL_0033:  ldfld      float32 MCCTest.VType2::f2
    IL_0038:  box        [mscorlib]System.Single
    IL_003d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0042:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0047:  nop
    IL_0048:  ldloc.1
    IL_0049:  ldloc.0
    IL_004a:  ldstr      "f3    = "
    IL_004f:  ldarg.0
    IL_0050:  ldfld      int16 MCCTest.VType2::f3
    IL_0055:  box        [mscorlib]System.Int16
    IL_005a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_005f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0064:  nop
    IL_0065:  ldloc.1
    IL_0066:  ldloc.0
    IL_0067:  ldstr      "f4    = "
    IL_006c:  ldarg.0
    IL_006d:  ldfld      float64 MCCTest.VType2::f4
    IL_0072:  box        [mscorlib]System.Double
    IL_0077:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_007c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0081:  nop
    IL_0082:  ldloc.1
    IL_0083:  ldloc.0
    IL_0084:  ldstr      "f5    = "
    IL_0089:  ldarg.0
    IL_008a:  ldfld      int64 MCCTest.VType2::f5
    IL_008f:  box        [mscorlib]System.Int64
    IL_0094:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0099:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_009e:  nop
    IL_009f:  ldloc.1
    IL_00a0:  ldloc.0
    IL_00a1:  ldstr      "f6    = "
    IL_00a6:  ldarg.0
    IL_00a7:  ldfld      float32 MCCTest.VType2::f6
    IL_00ac:  box        [mscorlib]System.Single
    IL_00b1:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00b6:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00bb:  nop
    IL_00bc:  ldloc.1
    IL_00bd:  ldloc.0
    IL_00be:  ldstr      "f7    = "
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      float64 MCCTest.VType2::f7
    IL_00c9:  box        [mscorlib]System.Double
    IL_00ce:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00d3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00d8:  nop
    IL_00d9:  ldloc.1
    IL_00da:  ldloc.0
    IL_00db:  ldstr      "f8    = "
    IL_00e0:  ldarg.0
    IL_00e1:  ldfld      char MCCTest.VType2::f8
    IL_00e6:  box        [mscorlib]System.Char
    IL_00eb:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00f0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00f5:  nop
    IL_00f6:  ldloc.1
    IL_00f7:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00fc:  stloc.2
    IL_00fd:  br.s       IL_00ff

    IL_00ff:  ldloc.2
    IL_0100:  ret
  } // end of method VType2::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VType2::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VType2::Dump

} // end of class MCCTest.VType2

.class public sequential ansi sealed beforefieldinit MCCTest.VType3
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VType3>
{
  .field public float32 f1
  .field public float32 f2
  .field public float32 f3
  .field public float32 f4
  .field public float32 f5
  .field public float32 f6
  .field public float32 f7
  .field public float32 f8
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       66 (0x42)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  conv.r4
    IL_0004:  stfld      float32 MCCTest.VType3::f1
    IL_0009:  ldarg.0
    IL_000a:  ldarg.1
    IL_000b:  conv.r4
    IL_000c:  stfld      float32 MCCTest.VType3::f2
    IL_0011:  ldarg.0
    IL_0012:  ldarg.1
    IL_0013:  conv.r4
    IL_0014:  stfld      float32 MCCTest.VType3::f3
    IL_0019:  ldarg.0
    IL_001a:  ldarg.1
    IL_001b:  conv.r4
    IL_001c:  stfld      float32 MCCTest.VType3::f4
    IL_0021:  ldarg.0
    IL_0022:  ldarg.1
    IL_0023:  conv.r4
    IL_0024:  stfld      float32 MCCTest.VType3::f5
    IL_0029:  ldarg.0
    IL_002a:  ldarg.1
    IL_002b:  conv.r4
    IL_002c:  stfld      float32 MCCTest.VType3::f6
    IL_0031:  ldarg.0
    IL_0032:  ldarg.1
    IL_0033:  conv.r4
    IL_0034:  stfld      float32 MCCTest.VType3::f7
    IL_0039:  ldarg.0
    IL_003a:  ldarg.1
    IL_003b:  conv.r4
    IL_003c:  stfld      float32 MCCTest.VType3::f8
    IL_0041:  ret
  } // end of method VType3::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VType3::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType3::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VType3::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType3::Zero

  .method public hidebysig instance void
          Add(valuetype MCCTest.VType3 val) cil managed
  {
    // Code size       162 (0xa2)
    .maxstack  3
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      float32 MCCTest.VType3::f1
    IL_0008:  ldarga.s   val
    IL_000a:  ldfld      float32 MCCTest.VType3::f1
    IL_000f:  add
    IL_0010:  stfld      float32 MCCTest.VType3::f1
    IL_0015:  ldarg.0
    IL_0016:  dup
    IL_0017:  ldfld      float32 MCCTest.VType3::f2
    IL_001c:  ldarga.s   val
    IL_001e:  ldfld      float32 MCCTest.VType3::f2
    IL_0023:  add
    IL_0024:  stfld      float32 MCCTest.VType3::f2
    IL_0029:  ldarg.0
    IL_002a:  dup
    IL_002b:  ldfld      float32 MCCTest.VType3::f3
    IL_0030:  ldarga.s   val
    IL_0032:  ldfld      float32 MCCTest.VType3::f3
    IL_0037:  add
    IL_0038:  stfld      float32 MCCTest.VType3::f3
    IL_003d:  ldarg.0
    IL_003e:  dup
    IL_003f:  ldfld      float32 MCCTest.VType3::f4
    IL_0044:  ldarga.s   val
    IL_0046:  ldfld      float32 MCCTest.VType3::f4
    IL_004b:  add
    IL_004c:  stfld      float32 MCCTest.VType3::f4
    IL_0051:  ldarg.0
    IL_0052:  dup
    IL_0053:  ldfld      float32 MCCTest.VType3::f5
    IL_0058:  ldarga.s   val
    IL_005a:  ldfld      float32 MCCTest.VType3::f5
    IL_005f:  add
    IL_0060:  stfld      float32 MCCTest.VType3::f5
    IL_0065:  ldarg.0
    IL_0066:  dup
    IL_0067:  ldfld      float32 MCCTest.VType3::f6
    IL_006c:  ldarga.s   val
    IL_006e:  ldfld      float32 MCCTest.VType3::f6
    IL_0073:  add
    IL_0074:  stfld      float32 MCCTest.VType3::f6
    IL_0079:  ldarg.0
    IL_007a:  dup
    IL_007b:  ldfld      float32 MCCTest.VType3::f7
    IL_0080:  ldarga.s   val
    IL_0082:  ldfld      float32 MCCTest.VType3::f7
    IL_0087:  add
    IL_0088:  stfld      float32 MCCTest.VType3::f7
    IL_008d:  ldarg.0
    IL_008e:  dup
    IL_008f:  ldfld      float32 MCCTest.VType3::f8
    IL_0094:  ldarga.s   val
    IL_0096:  ldfld      float32 MCCTest.VType3::f8
    IL_009b:  add
    IL_009c:  stfld      float32 MCCTest.VType3::f8
    IL_00a1:  ret
  } // end of method VType3::Add

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VType3 expected) cil managed
  {
    // Code size       370 (0x172)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      float32 MCCTest.VType3::f1
    IL_0007:  ldarga.s   expected
    IL_0009:  ldfld      float32 MCCTest.VType3::f1
    IL_000e:  ceq
    IL_0010:  stloc.0
    IL_0011:  ldloc.0
    IL_0012:  brtrue.s   IL_002f

    IL_0014:  nop
    IL_0015:  ldstr      "f1"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      float32 MCCTest.VType3::f1
    IL_0020:  conv.r8
    IL_0021:  ldarga.s   expected
    IL_0023:  ldfld      float32 MCCTest.VType3::f1
    IL_0028:  conv.r8
    IL_0029:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_002e:  throw

    IL_002f:  ldarg.0
    IL_0030:  ldfld      float32 MCCTest.VType3::f2
    IL_0035:  ldarga.s   expected
    IL_0037:  ldfld      float32 MCCTest.VType3::f2
    IL_003c:  ceq
    IL_003e:  stloc.0
    IL_003f:  ldloc.0
    IL_0040:  brtrue.s   IL_005d

    IL_0042:  nop
    IL_0043:  ldstr      "f2"
    IL_0048:  ldarg.0
    IL_0049:  ldfld      float32 MCCTest.VType3::f2
    IL_004e:  conv.r8
    IL_004f:  ldarga.s   expected
    IL_0051:  ldfld      float32 MCCTest.VType3::f2
    IL_0056:  conv.r8
    IL_0057:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_005c:  throw

    IL_005d:  ldarg.0
    IL_005e:  ldfld      float32 MCCTest.VType3::f3
    IL_0063:  ldarga.s   expected
    IL_0065:  ldfld      float32 MCCTest.VType3::f3
    IL_006a:  ceq
    IL_006c:  stloc.0
    IL_006d:  ldloc.0
    IL_006e:  brtrue.s   IL_008b

    IL_0070:  nop
    IL_0071:  ldstr      "f3"
    IL_0076:  ldarg.0
    IL_0077:  ldfld      float32 MCCTest.VType3::f3
    IL_007c:  conv.r8
    IL_007d:  ldarga.s   expected
    IL_007f:  ldfld      float32 MCCTest.VType3::f3
    IL_0084:  conv.r8
    IL_0085:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_008a:  throw

    IL_008b:  ldarg.0
    IL_008c:  ldfld      float32 MCCTest.VType3::f4
    IL_0091:  ldarga.s   expected
    IL_0093:  ldfld      float32 MCCTest.VType3::f4
    IL_0098:  ceq
    IL_009a:  stloc.0
    IL_009b:  ldloc.0
    IL_009c:  brtrue.s   IL_00b9

    IL_009e:  nop
    IL_009f:  ldstr      "f4"
    IL_00a4:  ldarg.0
    IL_00a5:  ldfld      float32 MCCTest.VType3::f4
    IL_00aa:  conv.r8
    IL_00ab:  ldarga.s   expected
    IL_00ad:  ldfld      float32 MCCTest.VType3::f4
    IL_00b2:  conv.r8
    IL_00b3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00b8:  throw

    IL_00b9:  ldarg.0
    IL_00ba:  ldfld      float32 MCCTest.VType3::f5
    IL_00bf:  ldarga.s   expected
    IL_00c1:  ldfld      float32 MCCTest.VType3::f5
    IL_00c6:  ceq
    IL_00c8:  stloc.0
    IL_00c9:  ldloc.0
    IL_00ca:  brtrue.s   IL_00e7

    IL_00cc:  nop
    IL_00cd:  ldstr      "f5"
    IL_00d2:  ldarg.0
    IL_00d3:  ldfld      float32 MCCTest.VType3::f5
    IL_00d8:  conv.r8
    IL_00d9:  ldarga.s   expected
    IL_00db:  ldfld      float32 MCCTest.VType3::f5
    IL_00e0:  conv.r8
    IL_00e1:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00e6:  throw

    IL_00e7:  ldarg.0
    IL_00e8:  ldfld      float32 MCCTest.VType3::f6
    IL_00ed:  ldarga.s   expected
    IL_00ef:  ldfld      float32 MCCTest.VType3::f6
    IL_00f4:  ceq
    IL_00f6:  stloc.0
    IL_00f7:  ldloc.0
    IL_00f8:  brtrue.s   IL_0115

    IL_00fa:  nop
    IL_00fb:  ldstr      "f6"
    IL_0100:  ldarg.0
    IL_0101:  ldfld      float32 MCCTest.VType3::f6
    IL_0106:  conv.r8
    IL_0107:  ldarga.s   expected
    IL_0109:  ldfld      float32 MCCTest.VType3::f6
    IL_010e:  conv.r8
    IL_010f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0114:  throw

    IL_0115:  ldarg.0
    IL_0116:  ldfld      float32 MCCTest.VType3::f7
    IL_011b:  ldarga.s   expected
    IL_011d:  ldfld      float32 MCCTest.VType3::f7
    IL_0122:  ceq
    IL_0124:  stloc.0
    IL_0125:  ldloc.0
    IL_0126:  brtrue.s   IL_0143

    IL_0128:  nop
    IL_0129:  ldstr      "f7"
    IL_012e:  ldarg.0
    IL_012f:  ldfld      float32 MCCTest.VType3::f7
    IL_0134:  conv.r8
    IL_0135:  ldarga.s   expected
    IL_0137:  ldfld      float32 MCCTest.VType3::f7
    IL_013c:  conv.r8
    IL_013d:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0142:  throw

    IL_0143:  ldarg.0
    IL_0144:  ldfld      float32 MCCTest.VType3::f8
    IL_0149:  ldarga.s   expected
    IL_014b:  ldfld      float32 MCCTest.VType3::f8
    IL_0150:  ceq
    IL_0152:  stloc.0
    IL_0153:  ldloc.0
    IL_0154:  brtrue.s   IL_0171

    IL_0156:  nop
    IL_0157:  ldstr      "f8"
    IL_015c:  ldarg.0
    IL_015d:  ldfld      float32 MCCTest.VType3::f8
    IL_0162:  conv.r8
    IL_0163:  ldarga.s   expected
    IL_0165:  ldfld      float32 MCCTest.VType3::f8
    IL_016a:  conv.r8
    IL_016b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0170:  throw

    IL_0171:  ret
  } // end of method VType3::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       257 (0x101)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "f1    = "
    IL_0015:  ldarg.0
    IL_0016:  ldfld      float32 MCCTest.VType3::f1
    IL_001b:  box        [mscorlib]System.Single
    IL_0020:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0025:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_002a:  nop
    IL_002b:  ldloc.1
    IL_002c:  ldloc.0
    IL_002d:  ldstr      "f2    = "
    IL_0032:  ldarg.0
    IL_0033:  ldfld      float32 MCCTest.VType3::f2
    IL_0038:  box        [mscorlib]System.Single
    IL_003d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0042:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0047:  nop
    IL_0048:  ldloc.1
    IL_0049:  ldloc.0
    IL_004a:  ldstr      "f3    = "
    IL_004f:  ldarg.0
    IL_0050:  ldfld      float32 MCCTest.VType3::f3
    IL_0055:  box        [mscorlib]System.Single
    IL_005a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_005f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0064:  nop
    IL_0065:  ldloc.1
    IL_0066:  ldloc.0
    IL_0067:  ldstr      "f4    = "
    IL_006c:  ldarg.0
    IL_006d:  ldfld      float32 MCCTest.VType3::f4
    IL_0072:  box        [mscorlib]System.Single
    IL_0077:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_007c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0081:  nop
    IL_0082:  ldloc.1
    IL_0083:  ldloc.0
    IL_0084:  ldstr      "f5    = "
    IL_0089:  ldarg.0
    IL_008a:  ldfld      float32 MCCTest.VType3::f5
    IL_008f:  box        [mscorlib]System.Single
    IL_0094:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0099:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_009e:  nop
    IL_009f:  ldloc.1
    IL_00a0:  ldloc.0
    IL_00a1:  ldstr      "f6    = "
    IL_00a6:  ldarg.0
    IL_00a7:  ldfld      float32 MCCTest.VType3::f6
    IL_00ac:  box        [mscorlib]System.Single
    IL_00b1:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00b6:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00bb:  nop
    IL_00bc:  ldloc.1
    IL_00bd:  ldloc.0
    IL_00be:  ldstr      "f7    = "
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      float32 MCCTest.VType3::f7
    IL_00c9:  box        [mscorlib]System.Single
    IL_00ce:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00d3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00d8:  nop
    IL_00d9:  ldloc.1
    IL_00da:  ldloc.0
    IL_00db:  ldstr      "f8    = "
    IL_00e0:  ldarg.0
    IL_00e1:  ldfld      float32 MCCTest.VType3::f8
    IL_00e6:  box        [mscorlib]System.Single
    IL_00eb:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00f0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00f5:  nop
    IL_00f6:  ldloc.1
    IL_00f7:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00fc:  stloc.2
    IL_00fd:  br.s       IL_00ff

    IL_00ff:  ldloc.2
    IL_0100:  ret
  } // end of method VType3::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VType3::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VType3::Dump

} // end of class MCCTest.VType3

.class public sequential ansi beforefieldinit MCCTest.RType4
       extends [mscorlib]System.Object
       implements class MCCTest.CType`1<class MCCTest.RType4>
{
  .field public float32 f1
  .field public float32 f2
  .field public float32 f3
  .field public float32 f4
  .field public float32 f5
  .field public float32 f6
  .field public float32 f7
  .field public float32 f8
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       66 (0x42)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  conv.r4
    IL_0004:  stfld      float32 MCCTest.RType4::f1
    IL_0009:  ldarg.0
    IL_000a:  ldarg.1
    IL_000b:  conv.r4
    IL_000c:  stfld      float32 MCCTest.RType4::f2
    IL_0011:  ldarg.0
    IL_0012:  ldarg.1
    IL_0013:  conv.r4
    IL_0014:  stfld      float32 MCCTest.RType4::f3
    IL_0019:  ldarg.0
    IL_001a:  ldarg.1
    IL_001b:  conv.r4
    IL_001c:  stfld      float32 MCCTest.RType4::f4
    IL_0021:  ldarg.0
    IL_0022:  ldarg.1
    IL_0023:  conv.r4
    IL_0024:  stfld      float32 MCCTest.RType4::f5
    IL_0029:  ldarg.0
    IL_002a:  ldarg.1
    IL_002b:  conv.r4
    IL_002c:  stfld      float32 MCCTest.RType4::f6
    IL_0031:  ldarg.0
    IL_0032:  ldarg.1
    IL_0033:  conv.r4
    IL_0034:  stfld      float32 MCCTest.RType4::f7
    IL_0039:  ldarg.0
    IL_003a:  ldarg.1
    IL_003b:  conv.r4
    IL_003c:  stfld      float32 MCCTest.RType4::f8
    IL_0041:  ret
  } // end of method RType4::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.RType4::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method RType4::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.RType4::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method RType4::Zero

  .method public hidebysig instance void
          Add(class MCCTest.RType4 val) cil managed
  {
    // Code size       154 (0x9a)
    .maxstack  3
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      float32 MCCTest.RType4::f1
    IL_0008:  ldarg.1
    IL_0009:  ldfld      float32 MCCTest.RType4::f1
    IL_000e:  add
    IL_000f:  stfld      float32 MCCTest.RType4::f1
    IL_0014:  ldarg.0
    IL_0015:  dup
    IL_0016:  ldfld      float32 MCCTest.RType4::f2
    IL_001b:  ldarg.1
    IL_001c:  ldfld      float32 MCCTest.RType4::f2
    IL_0021:  add
    IL_0022:  stfld      float32 MCCTest.RType4::f2
    IL_0027:  ldarg.0
    IL_0028:  dup
    IL_0029:  ldfld      float32 MCCTest.RType4::f3
    IL_002e:  ldarg.1
    IL_002f:  ldfld      float32 MCCTest.RType4::f3
    IL_0034:  add
    IL_0035:  stfld      float32 MCCTest.RType4::f3
    IL_003a:  ldarg.0
    IL_003b:  dup
    IL_003c:  ldfld      float32 MCCTest.RType4::f4
    IL_0041:  ldarg.1
    IL_0042:  ldfld      float32 MCCTest.RType4::f4
    IL_0047:  add
    IL_0048:  stfld      float32 MCCTest.RType4::f4
    IL_004d:  ldarg.0
    IL_004e:  dup
    IL_004f:  ldfld      float32 MCCTest.RType4::f5
    IL_0054:  ldarg.1
    IL_0055:  ldfld      float32 MCCTest.RType4::f5
    IL_005a:  add
    IL_005b:  stfld      float32 MCCTest.RType4::f5
    IL_0060:  ldarg.0
    IL_0061:  dup
    IL_0062:  ldfld      float32 MCCTest.RType4::f6
    IL_0067:  ldarg.1
    IL_0068:  ldfld      float32 MCCTest.RType4::f6
    IL_006d:  add
    IL_006e:  stfld      float32 MCCTest.RType4::f6
    IL_0073:  ldarg.0
    IL_0074:  dup
    IL_0075:  ldfld      float32 MCCTest.RType4::f7
    IL_007a:  ldarg.1
    IL_007b:  ldfld      float32 MCCTest.RType4::f7
    IL_0080:  add
    IL_0081:  stfld      float32 MCCTest.RType4::f7
    IL_0086:  ldarg.0
    IL_0087:  dup
    IL_0088:  ldfld      float32 MCCTest.RType4::f8
    IL_008d:  ldarg.1
    IL_008e:  ldfld      float32 MCCTest.RType4::f8
    IL_0093:  add
    IL_0094:  stfld      float32 MCCTest.RType4::f8
    IL_0099:  ret
  } // end of method RType4::Add

  .method public hidebysig newslot virtual final
          instance void  Check(class MCCTest.RType4 expected) cil managed
  {
    // Code size       354 (0x162)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      float32 MCCTest.RType4::f1
    IL_0007:  ldarg.1
    IL_0008:  ldfld      float32 MCCTest.RType4::f1
    IL_000d:  ceq
    IL_000f:  stloc.0
    IL_0010:  ldloc.0
    IL_0011:  brtrue.s   IL_002d

    IL_0013:  nop
    IL_0014:  ldstr      "f1"
    IL_0019:  ldarg.0
    IL_001a:  ldfld      float32 MCCTest.RType4::f1
    IL_001f:  conv.r8
    IL_0020:  ldarg.1
    IL_0021:  ldfld      float32 MCCTest.RType4::f1
    IL_0026:  conv.r8
    IL_0027:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_002c:  throw

    IL_002d:  ldarg.0
    IL_002e:  ldfld      float32 MCCTest.RType4::f2
    IL_0033:  ldarg.1
    IL_0034:  ldfld      float32 MCCTest.RType4::f2
    IL_0039:  ceq
    IL_003b:  stloc.0
    IL_003c:  ldloc.0
    IL_003d:  brtrue.s   IL_0059

    IL_003f:  nop
    IL_0040:  ldstr      "f2"
    IL_0045:  ldarg.0
    IL_0046:  ldfld      float32 MCCTest.RType4::f2
    IL_004b:  conv.r8
    IL_004c:  ldarg.1
    IL_004d:  ldfld      float32 MCCTest.RType4::f2
    IL_0052:  conv.r8
    IL_0053:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0058:  throw

    IL_0059:  ldarg.0
    IL_005a:  ldfld      float32 MCCTest.RType4::f3
    IL_005f:  ldarg.1
    IL_0060:  ldfld      float32 MCCTest.RType4::f3
    IL_0065:  ceq
    IL_0067:  stloc.0
    IL_0068:  ldloc.0
    IL_0069:  brtrue.s   IL_0085

    IL_006b:  nop
    IL_006c:  ldstr      "f3"
    IL_0071:  ldarg.0
    IL_0072:  ldfld      float32 MCCTest.RType4::f3
    IL_0077:  conv.r8
    IL_0078:  ldarg.1
    IL_0079:  ldfld      float32 MCCTest.RType4::f3
    IL_007e:  conv.r8
    IL_007f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0084:  throw

    IL_0085:  ldarg.0
    IL_0086:  ldfld      float32 MCCTest.RType4::f4
    IL_008b:  ldarg.1
    IL_008c:  ldfld      float32 MCCTest.RType4::f4
    IL_0091:  ceq
    IL_0093:  stloc.0
    IL_0094:  ldloc.0
    IL_0095:  brtrue.s   IL_00b1

    IL_0097:  nop
    IL_0098:  ldstr      "f4"
    IL_009d:  ldarg.0
    IL_009e:  ldfld      float32 MCCTest.RType4::f4
    IL_00a3:  conv.r8
    IL_00a4:  ldarg.1
    IL_00a5:  ldfld      float32 MCCTest.RType4::f4
    IL_00aa:  conv.r8
    IL_00ab:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00b0:  throw

    IL_00b1:  ldarg.0
    IL_00b2:  ldfld      float32 MCCTest.RType4::f5
    IL_00b7:  ldarg.1
    IL_00b8:  ldfld      float32 MCCTest.RType4::f5
    IL_00bd:  ceq
    IL_00bf:  stloc.0
    IL_00c0:  ldloc.0
    IL_00c1:  brtrue.s   IL_00dd

    IL_00c3:  nop
    IL_00c4:  ldstr      "f5"
    IL_00c9:  ldarg.0
    IL_00ca:  ldfld      float32 MCCTest.RType4::f5
    IL_00cf:  conv.r8
    IL_00d0:  ldarg.1
    IL_00d1:  ldfld      float32 MCCTest.RType4::f5
    IL_00d6:  conv.r8
    IL_00d7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00dc:  throw

    IL_00dd:  ldarg.0
    IL_00de:  ldfld      float32 MCCTest.RType4::f6
    IL_00e3:  ldarg.1
    IL_00e4:  ldfld      float32 MCCTest.RType4::f6
    IL_00e9:  ceq
    IL_00eb:  stloc.0
    IL_00ec:  ldloc.0
    IL_00ed:  brtrue.s   IL_0109

    IL_00ef:  nop
    IL_00f0:  ldstr      "f6"
    IL_00f5:  ldarg.0
    IL_00f6:  ldfld      float32 MCCTest.RType4::f6
    IL_00fb:  conv.r8
    IL_00fc:  ldarg.1
    IL_00fd:  ldfld      float32 MCCTest.RType4::f6
    IL_0102:  conv.r8
    IL_0103:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0108:  throw

    IL_0109:  ldarg.0
    IL_010a:  ldfld      float32 MCCTest.RType4::f7
    IL_010f:  ldarg.1
    IL_0110:  ldfld      float32 MCCTest.RType4::f7
    IL_0115:  ceq
    IL_0117:  stloc.0
    IL_0118:  ldloc.0
    IL_0119:  brtrue.s   IL_0135

    IL_011b:  nop
    IL_011c:  ldstr      "f7"
    IL_0121:  ldarg.0
    IL_0122:  ldfld      float32 MCCTest.RType4::f7
    IL_0127:  conv.r8
    IL_0128:  ldarg.1
    IL_0129:  ldfld      float32 MCCTest.RType4::f7
    IL_012e:  conv.r8
    IL_012f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0134:  throw

    IL_0135:  ldarg.0
    IL_0136:  ldfld      float32 MCCTest.RType4::f8
    IL_013b:  ldarg.1
    IL_013c:  ldfld      float32 MCCTest.RType4::f8
    IL_0141:  ceq
    IL_0143:  stloc.0
    IL_0144:  ldloc.0
    IL_0145:  brtrue.s   IL_0161

    IL_0147:  nop
    IL_0148:  ldstr      "f8"
    IL_014d:  ldarg.0
    IL_014e:  ldfld      float32 MCCTest.RType4::f8
    IL_0153:  conv.r8
    IL_0154:  ldarg.1
    IL_0155:  ldfld      float32 MCCTest.RType4::f8
    IL_015a:  conv.r8
    IL_015b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0160:  throw

    IL_0161:  ret
  } // end of method RType4::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       257 (0x101)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "f1    = "
    IL_0015:  ldarg.0
    IL_0016:  ldfld      float32 MCCTest.RType4::f1
    IL_001b:  box        [mscorlib]System.Single
    IL_0020:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0025:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_002a:  nop
    IL_002b:  ldloc.1
    IL_002c:  ldloc.0
    IL_002d:  ldstr      "f2    = "
    IL_0032:  ldarg.0
    IL_0033:  ldfld      float32 MCCTest.RType4::f2
    IL_0038:  box        [mscorlib]System.Single
    IL_003d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0042:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0047:  nop
    IL_0048:  ldloc.1
    IL_0049:  ldloc.0
    IL_004a:  ldstr      "f3    = "
    IL_004f:  ldarg.0
    IL_0050:  ldfld      float32 MCCTest.RType4::f3
    IL_0055:  box        [mscorlib]System.Single
    IL_005a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_005f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0064:  nop
    IL_0065:  ldloc.1
    IL_0066:  ldloc.0
    IL_0067:  ldstr      "f4    = "
    IL_006c:  ldarg.0
    IL_006d:  ldfld      float32 MCCTest.RType4::f4
    IL_0072:  box        [mscorlib]System.Single
    IL_0077:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_007c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0081:  nop
    IL_0082:  ldloc.1
    IL_0083:  ldloc.0
    IL_0084:  ldstr      "f5    = "
    IL_0089:  ldarg.0
    IL_008a:  ldfld      float32 MCCTest.RType4::f5
    IL_008f:  box        [mscorlib]System.Single
    IL_0094:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0099:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_009e:  nop
    IL_009f:  ldloc.1
    IL_00a0:  ldloc.0
    IL_00a1:  ldstr      "f6    = "
    IL_00a6:  ldarg.0
    IL_00a7:  ldfld      float32 MCCTest.RType4::f6
    IL_00ac:  box        [mscorlib]System.Single
    IL_00b1:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00b6:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00bb:  nop
    IL_00bc:  ldloc.1
    IL_00bd:  ldloc.0
    IL_00be:  ldstr      "f7    = "
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      float32 MCCTest.RType4::f7
    IL_00c9:  box        [mscorlib]System.Single
    IL_00ce:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00d3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00d8:  nop
    IL_00d9:  ldloc.1
    IL_00da:  ldloc.0
    IL_00db:  ldstr      "f8    = "
    IL_00e0:  ldarg.0
    IL_00e1:  ldfld      float32 MCCTest.RType4::f8
    IL_00e6:  box        [mscorlib]System.Single
    IL_00eb:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00f0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00f5:  nop
    IL_00f6:  ldloc.1
    IL_00f7:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00fc:  stloc.2
    IL_00fd:  br.s       IL_00ff

    IL_00ff:  ldloc.2
    IL_0100:  ret
  } // end of method RType4::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.RType4::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method RType4::Dump

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method RType4::.ctor

} // end of class MCCTest.RType4

.class public sequential ansi sealed beforefieldinit MCCTest.VType5
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VType5>
{
  .field public int64 f1
  .field public int64 f2
  .field public int64 f3
  .field public int64 f4
  .field public int64 f5
  .field public int64 f6
  .field public int64 f7
  .field public int64 f8
  .field public int64 f9
  .field public int64 f10
  .field public int64 f11
  .field public int64 f12
  .field public int64 f13
  .field public int64 f14
  .field public int64 f15
  .field public int64 f16
  .field public int64 f17
  .field public int64 f18
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       146 (0x92)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  conv.i8
    IL_0004:  stfld      int64 MCCTest.VType5::f1
    IL_0009:  ldarg.0
    IL_000a:  ldarg.1
    IL_000b:  conv.i8
    IL_000c:  stfld      int64 MCCTest.VType5::f2
    IL_0011:  ldarg.0
    IL_0012:  ldarg.1
    IL_0013:  conv.i8
    IL_0014:  stfld      int64 MCCTest.VType5::f3
    IL_0019:  ldarg.0
    IL_001a:  ldarg.1
    IL_001b:  conv.i8
    IL_001c:  stfld      int64 MCCTest.VType5::f4
    IL_0021:  ldarg.0
    IL_0022:  ldarg.1
    IL_0023:  conv.i8
    IL_0024:  stfld      int64 MCCTest.VType5::f5
    IL_0029:  ldarg.0
    IL_002a:  ldarg.1
    IL_002b:  conv.i8
    IL_002c:  stfld      int64 MCCTest.VType5::f6
    IL_0031:  ldarg.0
    IL_0032:  ldarg.1
    IL_0033:  conv.i8
    IL_0034:  stfld      int64 MCCTest.VType5::f7
    IL_0039:  ldarg.0
    IL_003a:  ldarg.1
    IL_003b:  conv.i8
    IL_003c:  stfld      int64 MCCTest.VType5::f8
    IL_0041:  ldarg.0
    IL_0042:  ldarg.1
    IL_0043:  conv.i8
    IL_0044:  stfld      int64 MCCTest.VType5::f9
    IL_0049:  ldarg.0
    IL_004a:  ldarg.1
    IL_004b:  conv.i8
    IL_004c:  stfld      int64 MCCTest.VType5::f10
    IL_0051:  ldarg.0
    IL_0052:  ldarg.1
    IL_0053:  conv.i8
    IL_0054:  stfld      int64 MCCTest.VType5::f11
    IL_0059:  ldarg.0
    IL_005a:  ldarg.1
    IL_005b:  conv.i8
    IL_005c:  stfld      int64 MCCTest.VType5::f12
    IL_0061:  ldarg.0
    IL_0062:  ldarg.1
    IL_0063:  conv.i8
    IL_0064:  stfld      int64 MCCTest.VType5::f13
    IL_0069:  ldarg.0
    IL_006a:  ldarg.1
    IL_006b:  conv.i8
    IL_006c:  stfld      int64 MCCTest.VType5::f14
    IL_0071:  ldarg.0
    IL_0072:  ldarg.1
    IL_0073:  conv.i8
    IL_0074:  stfld      int64 MCCTest.VType5::f15
    IL_0079:  ldarg.0
    IL_007a:  ldarg.1
    IL_007b:  conv.i8
    IL_007c:  stfld      int64 MCCTest.VType5::f16
    IL_0081:  ldarg.0
    IL_0082:  ldarg.1
    IL_0083:  conv.i8
    IL_0084:  stfld      int64 MCCTest.VType5::f17
    IL_0089:  ldarg.0
    IL_008a:  ldarg.1
    IL_008b:  conv.i8
    IL_008c:  stfld      int64 MCCTest.VType5::f18
    IL_0091:  ret
  } // end of method VType5::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VType5::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType5::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VType5::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType5::Zero

  .method public hidebysig instance void
          Add(valuetype MCCTest.VType5 val) cil managed
  {
    // Code size       362 (0x16a)
    .maxstack  3
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int64 MCCTest.VType5::f1
    IL_0008:  ldarga.s   val
    IL_000a:  ldfld      int64 MCCTest.VType5::f1
    IL_000f:  add
    IL_0010:  stfld      int64 MCCTest.VType5::f1
    IL_0015:  ldarg.0
    IL_0016:  dup
    IL_0017:  ldfld      int64 MCCTest.VType5::f2
    IL_001c:  ldarga.s   val
    IL_001e:  ldfld      int64 MCCTest.VType5::f2
    IL_0023:  add
    IL_0024:  stfld      int64 MCCTest.VType5::f2
    IL_0029:  ldarg.0
    IL_002a:  dup
    IL_002b:  ldfld      int64 MCCTest.VType5::f3
    IL_0030:  ldarga.s   val
    IL_0032:  ldfld      int64 MCCTest.VType5::f3
    IL_0037:  add
    IL_0038:  stfld      int64 MCCTest.VType5::f3
    IL_003d:  ldarg.0
    IL_003e:  dup
    IL_003f:  ldfld      int64 MCCTest.VType5::f4
    IL_0044:  ldarga.s   val
    IL_0046:  ldfld      int64 MCCTest.VType5::f4
    IL_004b:  add
    IL_004c:  stfld      int64 MCCTest.VType5::f4
    IL_0051:  ldarg.0
    IL_0052:  dup
    IL_0053:  ldfld      int64 MCCTest.VType5::f5
    IL_0058:  ldarga.s   val
    IL_005a:  ldfld      int64 MCCTest.VType5::f5
    IL_005f:  add
    IL_0060:  stfld      int64 MCCTest.VType5::f5
    IL_0065:  ldarg.0
    IL_0066:  dup
    IL_0067:  ldfld      int64 MCCTest.VType5::f6
    IL_006c:  ldarga.s   val
    IL_006e:  ldfld      int64 MCCTest.VType5::f6
    IL_0073:  add
    IL_0074:  stfld      int64 MCCTest.VType5::f6
    IL_0079:  ldarg.0
    IL_007a:  dup
    IL_007b:  ldfld      int64 MCCTest.VType5::f7
    IL_0080:  ldarga.s   val
    IL_0082:  ldfld      int64 MCCTest.VType5::f7
    IL_0087:  add
    IL_0088:  stfld      int64 MCCTest.VType5::f7
    IL_008d:  ldarg.0
    IL_008e:  dup
    IL_008f:  ldfld      int64 MCCTest.VType5::f8
    IL_0094:  ldarga.s   val
    IL_0096:  ldfld      int64 MCCTest.VType5::f8
    IL_009b:  add
    IL_009c:  stfld      int64 MCCTest.VType5::f8
    IL_00a1:  ldarg.0
    IL_00a2:  dup
    IL_00a3:  ldfld      int64 MCCTest.VType5::f9
    IL_00a8:  ldarga.s   val
    IL_00aa:  ldfld      int64 MCCTest.VType5::f9
    IL_00af:  add
    IL_00b0:  stfld      int64 MCCTest.VType5::f9
    IL_00b5:  ldarg.0
    IL_00b6:  dup
    IL_00b7:  ldfld      int64 MCCTest.VType5::f10
    IL_00bc:  ldarga.s   val
    IL_00be:  ldfld      int64 MCCTest.VType5::f10
    IL_00c3:  add
    IL_00c4:  stfld      int64 MCCTest.VType5::f10
    IL_00c9:  ldarg.0
    IL_00ca:  dup
    IL_00cb:  ldfld      int64 MCCTest.VType5::f11
    IL_00d0:  ldarga.s   val
    IL_00d2:  ldfld      int64 MCCTest.VType5::f11
    IL_00d7:  add
    IL_00d8:  stfld      int64 MCCTest.VType5::f11
    IL_00dd:  ldarg.0
    IL_00de:  dup
    IL_00df:  ldfld      int64 MCCTest.VType5::f12
    IL_00e4:  ldarga.s   val
    IL_00e6:  ldfld      int64 MCCTest.VType5::f12
    IL_00eb:  add
    IL_00ec:  stfld      int64 MCCTest.VType5::f12
    IL_00f1:  ldarg.0
    IL_00f2:  dup
    IL_00f3:  ldfld      int64 MCCTest.VType5::f13
    IL_00f8:  ldarga.s   val
    IL_00fa:  ldfld      int64 MCCTest.VType5::f13
    IL_00ff:  add
    IL_0100:  stfld      int64 MCCTest.VType5::f13
    IL_0105:  ldarg.0
    IL_0106:  dup
    IL_0107:  ldfld      int64 MCCTest.VType5::f14
    IL_010c:  ldarga.s   val
    IL_010e:  ldfld      int64 MCCTest.VType5::f14
    IL_0113:  add
    IL_0114:  stfld      int64 MCCTest.VType5::f14
    IL_0119:  ldarg.0
    IL_011a:  dup
    IL_011b:  ldfld      int64 MCCTest.VType5::f15
    IL_0120:  ldarga.s   val
    IL_0122:  ldfld      int64 MCCTest.VType5::f15
    IL_0127:  add
    IL_0128:  stfld      int64 MCCTest.VType5::f15
    IL_012d:  ldarg.0
    IL_012e:  dup
    IL_012f:  ldfld      int64 MCCTest.VType5::f16
    IL_0134:  ldarga.s   val
    IL_0136:  ldfld      int64 MCCTest.VType5::f16
    IL_013b:  add
    IL_013c:  stfld      int64 MCCTest.VType5::f16
    IL_0141:  ldarg.0
    IL_0142:  dup
    IL_0143:  ldfld      int64 MCCTest.VType5::f17
    IL_0148:  ldarga.s   val
    IL_014a:  ldfld      int64 MCCTest.VType5::f17
    IL_014f:  add
    IL_0150:  stfld      int64 MCCTest.VType5::f17
    IL_0155:  ldarg.0
    IL_0156:  dup
    IL_0157:  ldfld      int64 MCCTest.VType5::f18
    IL_015c:  ldarga.s   val
    IL_015e:  ldfld      int64 MCCTest.VType5::f18
    IL_0163:  add
    IL_0164:  stfld      int64 MCCTest.VType5::f18
    IL_0169:  ret
  } // end of method VType5::Add

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VType5 expected) cil managed
  {
    // Code size       794 (0x31a)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      int64 MCCTest.VType5::f1
    IL_0007:  ldarga.s   expected
    IL_0009:  ldfld      int64 MCCTest.VType5::f1
    IL_000e:  ceq
    IL_0010:  stloc.0
    IL_0011:  ldloc.0
    IL_0012:  brtrue.s   IL_002d

    IL_0014:  nop
    IL_0015:  ldstr      "f1"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      int64 MCCTest.VType5::f1
    IL_0020:  ldarga.s   expected
    IL_0022:  ldfld      int64 MCCTest.VType5::f1
    IL_0027:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_002c:  throw

    IL_002d:  ldarg.0
    IL_002e:  ldfld      int64 MCCTest.VType5::f2
    IL_0033:  ldarga.s   expected
    IL_0035:  ldfld      int64 MCCTest.VType5::f2
    IL_003a:  ceq
    IL_003c:  stloc.0
    IL_003d:  ldloc.0
    IL_003e:  brtrue.s   IL_0059

    IL_0040:  nop
    IL_0041:  ldstr      "f2"
    IL_0046:  ldarg.0
    IL_0047:  ldfld      int64 MCCTest.VType5::f2
    IL_004c:  ldarga.s   expected
    IL_004e:  ldfld      int64 MCCTest.VType5::f2
    IL_0053:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_0058:  throw

    IL_0059:  ldarg.0
    IL_005a:  ldfld      int64 MCCTest.VType5::f3
    IL_005f:  ldarga.s   expected
    IL_0061:  ldfld      int64 MCCTest.VType5::f3
    IL_0066:  ceq
    IL_0068:  stloc.0
    IL_0069:  ldloc.0
    IL_006a:  brtrue.s   IL_0085

    IL_006c:  nop
    IL_006d:  ldstr      "f3"
    IL_0072:  ldarg.0
    IL_0073:  ldfld      int64 MCCTest.VType5::f3
    IL_0078:  ldarga.s   expected
    IL_007a:  ldfld      int64 MCCTest.VType5::f3
    IL_007f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_0084:  throw

    IL_0085:  ldarg.0
    IL_0086:  ldfld      int64 MCCTest.VType5::f4
    IL_008b:  ldarga.s   expected
    IL_008d:  ldfld      int64 MCCTest.VType5::f4
    IL_0092:  ceq
    IL_0094:  stloc.0
    IL_0095:  ldloc.0
    IL_0096:  brtrue.s   IL_00b1

    IL_0098:  nop
    IL_0099:  ldstr      "f4"
    IL_009e:  ldarg.0
    IL_009f:  ldfld      int64 MCCTest.VType5::f4
    IL_00a4:  ldarga.s   expected
    IL_00a6:  ldfld      int64 MCCTest.VType5::f4
    IL_00ab:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_00b0:  throw

    IL_00b1:  ldarg.0
    IL_00b2:  ldfld      int64 MCCTest.VType5::f5
    IL_00b7:  ldarga.s   expected
    IL_00b9:  ldfld      int64 MCCTest.VType5::f5
    IL_00be:  ceq
    IL_00c0:  stloc.0
    IL_00c1:  ldloc.0
    IL_00c2:  brtrue.s   IL_00dd

    IL_00c4:  nop
    IL_00c5:  ldstr      "f5"
    IL_00ca:  ldarg.0
    IL_00cb:  ldfld      int64 MCCTest.VType5::f5
    IL_00d0:  ldarga.s   expected
    IL_00d2:  ldfld      int64 MCCTest.VType5::f5
    IL_00d7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_00dc:  throw

    IL_00dd:  ldarg.0
    IL_00de:  ldfld      int64 MCCTest.VType5::f6
    IL_00e3:  ldarga.s   expected
    IL_00e5:  ldfld      int64 MCCTest.VType5::f6
    IL_00ea:  ceq
    IL_00ec:  stloc.0
    IL_00ed:  ldloc.0
    IL_00ee:  brtrue.s   IL_0109

    IL_00f0:  nop
    IL_00f1:  ldstr      "f6"
    IL_00f6:  ldarg.0
    IL_00f7:  ldfld      int64 MCCTest.VType5::f6
    IL_00fc:  ldarga.s   expected
    IL_00fe:  ldfld      int64 MCCTest.VType5::f6
    IL_0103:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_0108:  throw

    IL_0109:  ldarg.0
    IL_010a:  ldfld      int64 MCCTest.VType5::f7
    IL_010f:  ldarga.s   expected
    IL_0111:  ldfld      int64 MCCTest.VType5::f7
    IL_0116:  ceq
    IL_0118:  stloc.0
    IL_0119:  ldloc.0
    IL_011a:  brtrue.s   IL_0135

    IL_011c:  nop
    IL_011d:  ldstr      "f7"
    IL_0122:  ldarg.0
    IL_0123:  ldfld      int64 MCCTest.VType5::f7
    IL_0128:  ldarga.s   expected
    IL_012a:  ldfld      int64 MCCTest.VType5::f7
    IL_012f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_0134:  throw

    IL_0135:  ldarg.0
    IL_0136:  ldfld      int64 MCCTest.VType5::f8
    IL_013b:  ldarga.s   expected
    IL_013d:  ldfld      int64 MCCTest.VType5::f8
    IL_0142:  ceq
    IL_0144:  stloc.0
    IL_0145:  ldloc.0
    IL_0146:  brtrue.s   IL_0161

    IL_0148:  nop
    IL_0149:  ldstr      "f8"
    IL_014e:  ldarg.0
    IL_014f:  ldfld      int64 MCCTest.VType5::f8
    IL_0154:  ldarga.s   expected
    IL_0156:  ldfld      int64 MCCTest.VType5::f8
    IL_015b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_0160:  throw

    IL_0161:  ldarg.0
    IL_0162:  ldfld      int64 MCCTest.VType5::f9
    IL_0167:  ldarga.s   expected
    IL_0169:  ldfld      int64 MCCTest.VType5::f9
    IL_016e:  ceq
    IL_0170:  stloc.0
    IL_0171:  ldloc.0
    IL_0172:  brtrue.s   IL_018d

    IL_0174:  nop
    IL_0175:  ldstr      "f9"
    IL_017a:  ldarg.0
    IL_017b:  ldfld      int64 MCCTest.VType5::f9
    IL_0180:  ldarga.s   expected
    IL_0182:  ldfld      int64 MCCTest.VType5::f9
    IL_0187:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_018c:  throw

    IL_018d:  ldarg.0
    IL_018e:  ldfld      int64 MCCTest.VType5::f10
    IL_0193:  ldarga.s   expected
    IL_0195:  ldfld      int64 MCCTest.VType5::f10
    IL_019a:  ceq
    IL_019c:  stloc.0
    IL_019d:  ldloc.0
    IL_019e:  brtrue.s   IL_01b9

    IL_01a0:  nop
    IL_01a1:  ldstr      "f10"
    IL_01a6:  ldarg.0
    IL_01a7:  ldfld      int64 MCCTest.VType5::f10
    IL_01ac:  ldarga.s   expected
    IL_01ae:  ldfld      int64 MCCTest.VType5::f10
    IL_01b3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_01b8:  throw

    IL_01b9:  ldarg.0
    IL_01ba:  ldfld      int64 MCCTest.VType5::f11
    IL_01bf:  ldarga.s   expected
    IL_01c1:  ldfld      int64 MCCTest.VType5::f11
    IL_01c6:  ceq
    IL_01c8:  stloc.0
    IL_01c9:  ldloc.0
    IL_01ca:  brtrue.s   IL_01e5

    IL_01cc:  nop
    IL_01cd:  ldstr      "f11"
    IL_01d2:  ldarg.0
    IL_01d3:  ldfld      int64 MCCTest.VType5::f11
    IL_01d8:  ldarga.s   expected
    IL_01da:  ldfld      int64 MCCTest.VType5::f11
    IL_01df:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_01e4:  throw

    IL_01e5:  ldarg.0
    IL_01e6:  ldfld      int64 MCCTest.VType5::f12
    IL_01eb:  ldarga.s   expected
    IL_01ed:  ldfld      int64 MCCTest.VType5::f12
    IL_01f2:  ceq
    IL_01f4:  stloc.0
    IL_01f5:  ldloc.0
    IL_01f6:  brtrue.s   IL_0211

    IL_01f8:  nop
    IL_01f9:  ldstr      "f12"
    IL_01fe:  ldarg.0
    IL_01ff:  ldfld      int64 MCCTest.VType5::f12
    IL_0204:  ldarga.s   expected
    IL_0206:  ldfld      int64 MCCTest.VType5::f12
    IL_020b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_0210:  throw

    IL_0211:  ldarg.0
    IL_0212:  ldfld      int64 MCCTest.VType5::f13
    IL_0217:  ldarga.s   expected
    IL_0219:  ldfld      int64 MCCTest.VType5::f13
    IL_021e:  ceq
    IL_0220:  stloc.0
    IL_0221:  ldloc.0
    IL_0222:  brtrue.s   IL_023d

    IL_0224:  nop
    IL_0225:  ldstr      "f13"
    IL_022a:  ldarg.0
    IL_022b:  ldfld      int64 MCCTest.VType5::f13
    IL_0230:  ldarga.s   expected
    IL_0232:  ldfld      int64 MCCTest.VType5::f13
    IL_0237:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_023c:  throw

    IL_023d:  ldarg.0
    IL_023e:  ldfld      int64 MCCTest.VType5::f14
    IL_0243:  ldarga.s   expected
    IL_0245:  ldfld      int64 MCCTest.VType5::f14
    IL_024a:  ceq
    IL_024c:  stloc.0
    IL_024d:  ldloc.0
    IL_024e:  brtrue.s   IL_0269

    IL_0250:  nop
    IL_0251:  ldstr      "f14"
    IL_0256:  ldarg.0
    IL_0257:  ldfld      int64 MCCTest.VType5::f14
    IL_025c:  ldarga.s   expected
    IL_025e:  ldfld      int64 MCCTest.VType5::f14
    IL_0263:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_0268:  throw

    IL_0269:  ldarg.0
    IL_026a:  ldfld      int64 MCCTest.VType5::f15
    IL_026f:  ldarga.s   expected
    IL_0271:  ldfld      int64 MCCTest.VType5::f15
    IL_0276:  ceq
    IL_0278:  stloc.0
    IL_0279:  ldloc.0
    IL_027a:  brtrue.s   IL_0295

    IL_027c:  nop
    IL_027d:  ldstr      "f15"
    IL_0282:  ldarg.0
    IL_0283:  ldfld      int64 MCCTest.VType5::f15
    IL_0288:  ldarga.s   expected
    IL_028a:  ldfld      int64 MCCTest.VType5::f15
    IL_028f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_0294:  throw

    IL_0295:  ldarg.0
    IL_0296:  ldfld      int64 MCCTest.VType5::f16
    IL_029b:  ldarga.s   expected
    IL_029d:  ldfld      int64 MCCTest.VType5::f16
    IL_02a2:  ceq
    IL_02a4:  stloc.0
    IL_02a5:  ldloc.0
    IL_02a6:  brtrue.s   IL_02c1

    IL_02a8:  nop
    IL_02a9:  ldstr      "f16"
    IL_02ae:  ldarg.0
    IL_02af:  ldfld      int64 MCCTest.VType5::f16
    IL_02b4:  ldarga.s   expected
    IL_02b6:  ldfld      int64 MCCTest.VType5::f16
    IL_02bb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_02c0:  throw

    IL_02c1:  ldarg.0
    IL_02c2:  ldfld      int64 MCCTest.VType5::f17
    IL_02c7:  ldarga.s   expected
    IL_02c9:  ldfld      int64 MCCTest.VType5::f17
    IL_02ce:  ceq
    IL_02d0:  stloc.0
    IL_02d1:  ldloc.0
    IL_02d2:  brtrue.s   IL_02ed

    IL_02d4:  nop
    IL_02d5:  ldstr      "f17"
    IL_02da:  ldarg.0
    IL_02db:  ldfld      int64 MCCTest.VType5::f17
    IL_02e0:  ldarga.s   expected
    IL_02e2:  ldfld      int64 MCCTest.VType5::f17
    IL_02e7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_02ec:  throw

    IL_02ed:  ldarg.0
    IL_02ee:  ldfld      int64 MCCTest.VType5::f18
    IL_02f3:  ldarga.s   expected
    IL_02f5:  ldfld      int64 MCCTest.VType5::f18
    IL_02fa:  ceq
    IL_02fc:  stloc.0
    IL_02fd:  ldloc.0
    IL_02fe:  brtrue.s   IL_0319

    IL_0300:  nop
    IL_0301:  ldstr      "f18"
    IL_0306:  ldarg.0
    IL_0307:  ldfld      int64 MCCTest.VType5::f18
    IL_030c:  ldarga.s   expected
    IL_030e:  ldfld      int64 MCCTest.VType5::f18
    IL_0313:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_0318:  throw

    IL_0319:  ret
  } // end of method VType5::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       547 (0x223)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "f1    = "
    IL_0015:  ldarg.0
    IL_0016:  ldfld      int64 MCCTest.VType5::f1
    IL_001b:  box        [mscorlib]System.Int64
    IL_0020:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0025:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_002a:  nop
    IL_002b:  ldloc.1
    IL_002c:  ldloc.0
    IL_002d:  ldstr      "f2    = "
    IL_0032:  ldarg.0
    IL_0033:  ldfld      int64 MCCTest.VType5::f2
    IL_0038:  box        [mscorlib]System.Int64
    IL_003d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0042:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0047:  nop
    IL_0048:  ldloc.1
    IL_0049:  ldloc.0
    IL_004a:  ldstr      "f3    = "
    IL_004f:  ldarg.0
    IL_0050:  ldfld      int64 MCCTest.VType5::f3
    IL_0055:  box        [mscorlib]System.Int64
    IL_005a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_005f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0064:  nop
    IL_0065:  ldloc.1
    IL_0066:  ldloc.0
    IL_0067:  ldstr      "f4    = "
    IL_006c:  ldarg.0
    IL_006d:  ldfld      int64 MCCTest.VType5::f4
    IL_0072:  box        [mscorlib]System.Int64
    IL_0077:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_007c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0081:  nop
    IL_0082:  ldloc.1
    IL_0083:  ldloc.0
    IL_0084:  ldstr      "f5    = "
    IL_0089:  ldarg.0
    IL_008a:  ldfld      int64 MCCTest.VType5::f5
    IL_008f:  box        [mscorlib]System.Int64
    IL_0094:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0099:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_009e:  nop
    IL_009f:  ldloc.1
    IL_00a0:  ldloc.0
    IL_00a1:  ldstr      "f6    = "
    IL_00a6:  ldarg.0
    IL_00a7:  ldfld      int64 MCCTest.VType5::f6
    IL_00ac:  box        [mscorlib]System.Int64
    IL_00b1:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00b6:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00bb:  nop
    IL_00bc:  ldloc.1
    IL_00bd:  ldloc.0
    IL_00be:  ldstr      "f7    = "
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      int64 MCCTest.VType5::f7
    IL_00c9:  box        [mscorlib]System.Int64
    IL_00ce:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00d3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00d8:  nop
    IL_00d9:  ldloc.1
    IL_00da:  ldloc.0
    IL_00db:  ldstr      "f8    = "
    IL_00e0:  ldarg.0
    IL_00e1:  ldfld      int64 MCCTest.VType5::f8
    IL_00e6:  box        [mscorlib]System.Int64
    IL_00eb:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00f0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00f5:  nop
    IL_00f6:  ldloc.1
    IL_00f7:  ldloc.0
    IL_00f8:  ldstr      "f9    = "
    IL_00fd:  ldarg.0
    IL_00fe:  ldfld      int64 MCCTest.VType5::f9
    IL_0103:  box        [mscorlib]System.Int64
    IL_0108:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_010d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0112:  nop
    IL_0113:  ldloc.1
    IL_0114:  ldloc.0
    IL_0115:  ldstr      "f10    = "
    IL_011a:  ldarg.0
    IL_011b:  ldfld      int64 MCCTest.VType5::f10
    IL_0120:  box        [mscorlib]System.Int64
    IL_0125:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_012a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_012f:  nop
    IL_0130:  ldloc.1
    IL_0131:  ldloc.0
    IL_0132:  ldstr      "f11    = "
    IL_0137:  ldarg.0
    IL_0138:  ldfld      int64 MCCTest.VType5::f11
    IL_013d:  box        [mscorlib]System.Int64
    IL_0142:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0147:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_014c:  nop
    IL_014d:  ldloc.1
    IL_014e:  ldloc.0
    IL_014f:  ldstr      "f12    = "
    IL_0154:  ldarg.0
    IL_0155:  ldfld      int64 MCCTest.VType5::f12
    IL_015a:  box        [mscorlib]System.Int64
    IL_015f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0164:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0169:  nop
    IL_016a:  ldloc.1
    IL_016b:  ldloc.0
    IL_016c:  ldstr      "f13    = "
    IL_0171:  ldarg.0
    IL_0172:  ldfld      int64 MCCTest.VType5::f13
    IL_0177:  box        [mscorlib]System.Int64
    IL_017c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0181:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0186:  nop
    IL_0187:  ldloc.1
    IL_0188:  ldloc.0
    IL_0189:  ldstr      "f14    = "
    IL_018e:  ldarg.0
    IL_018f:  ldfld      int64 MCCTest.VType5::f14
    IL_0194:  box        [mscorlib]System.Int64
    IL_0199:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_019e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01a3:  nop
    IL_01a4:  ldloc.1
    IL_01a5:  ldloc.0
    IL_01a6:  ldstr      "f15    = "
    IL_01ab:  ldarg.0
    IL_01ac:  ldfld      int64 MCCTest.VType5::f15
    IL_01b1:  box        [mscorlib]System.Int64
    IL_01b6:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01bb:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01c0:  nop
    IL_01c1:  ldloc.1
    IL_01c2:  ldloc.0
    IL_01c3:  ldstr      "f16    = "
    IL_01c8:  ldarg.0
    IL_01c9:  ldfld      int64 MCCTest.VType5::f16
    IL_01ce:  box        [mscorlib]System.Int64
    IL_01d3:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01d8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01dd:  nop
    IL_01de:  ldloc.1
    IL_01df:  ldloc.0
    IL_01e0:  ldstr      "f17    = "
    IL_01e5:  ldarg.0
    IL_01e6:  ldfld      int64 MCCTest.VType5::f17
    IL_01eb:  box        [mscorlib]System.Int64
    IL_01f0:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01f5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01fa:  nop
    IL_01fb:  ldloc.1
    IL_01fc:  ldloc.0
    IL_01fd:  ldstr      "f18    = "
    IL_0202:  ldarg.0
    IL_0203:  ldfld      int64 MCCTest.VType5::f18
    IL_0208:  box        [mscorlib]System.Int64
    IL_020d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0212:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0217:  nop
    IL_0218:  ldloc.1
    IL_0219:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_021e:  stloc.2
    IL_021f:  br.s       IL_0221

    IL_0221:  ldloc.2
    IL_0222:  ret
  } // end of method VType5::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VType5::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VType5::Dump

} // end of class MCCTest.VType5

.class public sequential ansi sealed beforefieldinit MCCTest.VType6
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VType6>
{
  .field public float64 f1
  .field public float64 f2
  .field public float64 f3
  .field public float64 f4
  .field public float64 f5
  .field public float64 f6
  .field public float64 f7
  .field public float64 f8
  .field public float64 f9
  .field public float64 f10
  .field public float64 f11
  .field public float64 f12
  .field public float64 f13
  .field public float64 f14
  .field public float64 f15
  .field public float64 f16
  .field public float64 f17
  .field public float64 f18
  .field public float64 f19
  .field public float64 f20
  .field public float64 f21
  .field public float64 f22
  .field public float64 f23
  .field public float64 f24
  .field public float64 f25
  .field public float64 f26
  .field public float64 f27
  .field public float64 f28
  .field public float64 f29
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       234 (0xea)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  conv.r8
    IL_0004:  stfld      float64 MCCTest.VType6::f1
    IL_0009:  ldarg.0
    IL_000a:  ldarg.1
    IL_000b:  conv.r8
    IL_000c:  stfld      float64 MCCTest.VType6::f2
    IL_0011:  ldarg.0
    IL_0012:  ldarg.1
    IL_0013:  conv.r8
    IL_0014:  stfld      float64 MCCTest.VType6::f3
    IL_0019:  ldarg.0
    IL_001a:  ldarg.1
    IL_001b:  conv.r8
    IL_001c:  stfld      float64 MCCTest.VType6::f4
    IL_0021:  ldarg.0
    IL_0022:  ldarg.1
    IL_0023:  conv.r8
    IL_0024:  stfld      float64 MCCTest.VType6::f5
    IL_0029:  ldarg.0
    IL_002a:  ldarg.1
    IL_002b:  conv.r8
    IL_002c:  stfld      float64 MCCTest.VType6::f6
    IL_0031:  ldarg.0
    IL_0032:  ldarg.1
    IL_0033:  conv.r8
    IL_0034:  stfld      float64 MCCTest.VType6::f7
    IL_0039:  ldarg.0
    IL_003a:  ldarg.1
    IL_003b:  conv.r8
    IL_003c:  stfld      float64 MCCTest.VType6::f8
    IL_0041:  ldarg.0
    IL_0042:  ldarg.1
    IL_0043:  conv.r8
    IL_0044:  stfld      float64 MCCTest.VType6::f9
    IL_0049:  ldarg.0
    IL_004a:  ldarg.1
    IL_004b:  conv.r8
    IL_004c:  stfld      float64 MCCTest.VType6::f10
    IL_0051:  ldarg.0
    IL_0052:  ldarg.1
    IL_0053:  conv.r8
    IL_0054:  stfld      float64 MCCTest.VType6::f11
    IL_0059:  ldarg.0
    IL_005a:  ldarg.1
    IL_005b:  conv.r8
    IL_005c:  stfld      float64 MCCTest.VType6::f12
    IL_0061:  ldarg.0
    IL_0062:  ldarg.1
    IL_0063:  conv.r8
    IL_0064:  stfld      float64 MCCTest.VType6::f13
    IL_0069:  ldarg.0
    IL_006a:  ldarg.1
    IL_006b:  conv.r8
    IL_006c:  stfld      float64 MCCTest.VType6::f14
    IL_0071:  ldarg.0
    IL_0072:  ldarg.1
    IL_0073:  conv.r8
    IL_0074:  stfld      float64 MCCTest.VType6::f15
    IL_0079:  ldarg.0
    IL_007a:  ldarg.1
    IL_007b:  conv.r8
    IL_007c:  stfld      float64 MCCTest.VType6::f16
    IL_0081:  ldarg.0
    IL_0082:  ldarg.1
    IL_0083:  conv.r8
    IL_0084:  stfld      float64 MCCTest.VType6::f17
    IL_0089:  ldarg.0
    IL_008a:  ldarg.1
    IL_008b:  conv.r8
    IL_008c:  stfld      float64 MCCTest.VType6::f18
    IL_0091:  ldarg.0
    IL_0092:  ldarg.1
    IL_0093:  conv.r8
    IL_0094:  stfld      float64 MCCTest.VType6::f19
    IL_0099:  ldarg.0
    IL_009a:  ldarg.1
    IL_009b:  conv.r8
    IL_009c:  stfld      float64 MCCTest.VType6::f20
    IL_00a1:  ldarg.0
    IL_00a2:  ldarg.1
    IL_00a3:  conv.r8
    IL_00a4:  stfld      float64 MCCTest.VType6::f21
    IL_00a9:  ldarg.0
    IL_00aa:  ldarg.1
    IL_00ab:  conv.r8
    IL_00ac:  stfld      float64 MCCTest.VType6::f22
    IL_00b1:  ldarg.0
    IL_00b2:  ldarg.1
    IL_00b3:  conv.r8
    IL_00b4:  stfld      float64 MCCTest.VType6::f23
    IL_00b9:  ldarg.0
    IL_00ba:  ldarg.1
    IL_00bb:  conv.r8
    IL_00bc:  stfld      float64 MCCTest.VType6::f24
    IL_00c1:  ldarg.0
    IL_00c2:  ldarg.1
    IL_00c3:  conv.r8
    IL_00c4:  stfld      float64 MCCTest.VType6::f25
    IL_00c9:  ldarg.0
    IL_00ca:  ldarg.1
    IL_00cb:  conv.r8
    IL_00cc:  stfld      float64 MCCTest.VType6::f26
    IL_00d1:  ldarg.0
    IL_00d2:  ldarg.1
    IL_00d3:  conv.r8
    IL_00d4:  stfld      float64 MCCTest.VType6::f27
    IL_00d9:  ldarg.0
    IL_00da:  ldarg.1
    IL_00db:  conv.r8
    IL_00dc:  stfld      float64 MCCTest.VType6::f28
    IL_00e1:  ldarg.0
    IL_00e2:  ldarg.1
    IL_00e3:  conv.r8
    IL_00e4:  stfld      float64 MCCTest.VType6::f29
    IL_00e9:  ret
  } // end of method VType6::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VType6::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType6::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VType6::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType6::Zero

  .method public hidebysig instance void
          Add(valuetype MCCTest.VType6 val) cil managed
  {
    // Code size       582 (0x246)
    .maxstack  3
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      float64 MCCTest.VType6::f1
    IL_0008:  ldarga.s   val
    IL_000a:  ldfld      float64 MCCTest.VType6::f1
    IL_000f:  add
    IL_0010:  stfld      float64 MCCTest.VType6::f1
    IL_0015:  ldarg.0
    IL_0016:  dup
    IL_0017:  ldfld      float64 MCCTest.VType6::f2
    IL_001c:  ldarga.s   val
    IL_001e:  ldfld      float64 MCCTest.VType6::f2
    IL_0023:  add
    IL_0024:  stfld      float64 MCCTest.VType6::f2
    IL_0029:  ldarg.0
    IL_002a:  dup
    IL_002b:  ldfld      float64 MCCTest.VType6::f3
    IL_0030:  ldarga.s   val
    IL_0032:  ldfld      float64 MCCTest.VType6::f3
    IL_0037:  add
    IL_0038:  stfld      float64 MCCTest.VType6::f3
    IL_003d:  ldarg.0
    IL_003e:  dup
    IL_003f:  ldfld      float64 MCCTest.VType6::f4
    IL_0044:  ldarga.s   val
    IL_0046:  ldfld      float64 MCCTest.VType6::f4
    IL_004b:  add
    IL_004c:  stfld      float64 MCCTest.VType6::f4
    IL_0051:  ldarg.0
    IL_0052:  dup
    IL_0053:  ldfld      float64 MCCTest.VType6::f5
    IL_0058:  ldarga.s   val
    IL_005a:  ldfld      float64 MCCTest.VType6::f5
    IL_005f:  add
    IL_0060:  stfld      float64 MCCTest.VType6::f5
    IL_0065:  ldarg.0
    IL_0066:  dup
    IL_0067:  ldfld      float64 MCCTest.VType6::f6
    IL_006c:  ldarga.s   val
    IL_006e:  ldfld      float64 MCCTest.VType6::f6
    IL_0073:  add
    IL_0074:  stfld      float64 MCCTest.VType6::f6
    IL_0079:  ldarg.0
    IL_007a:  dup
    IL_007b:  ldfld      float64 MCCTest.VType6::f7
    IL_0080:  ldarga.s   val
    IL_0082:  ldfld      float64 MCCTest.VType6::f7
    IL_0087:  add
    IL_0088:  stfld      float64 MCCTest.VType6::f7
    IL_008d:  ldarg.0
    IL_008e:  dup
    IL_008f:  ldfld      float64 MCCTest.VType6::f8
    IL_0094:  ldarga.s   val
    IL_0096:  ldfld      float64 MCCTest.VType6::f8
    IL_009b:  add
    IL_009c:  stfld      float64 MCCTest.VType6::f8
    IL_00a1:  ldarg.0
    IL_00a2:  dup
    IL_00a3:  ldfld      float64 MCCTest.VType6::f9
    IL_00a8:  ldarga.s   val
    IL_00aa:  ldfld      float64 MCCTest.VType6::f9
    IL_00af:  add
    IL_00b0:  stfld      float64 MCCTest.VType6::f9
    IL_00b5:  ldarg.0
    IL_00b6:  dup
    IL_00b7:  ldfld      float64 MCCTest.VType6::f10
    IL_00bc:  ldarga.s   val
    IL_00be:  ldfld      float64 MCCTest.VType6::f10
    IL_00c3:  add
    IL_00c4:  stfld      float64 MCCTest.VType6::f10
    IL_00c9:  ldarg.0
    IL_00ca:  dup
    IL_00cb:  ldfld      float64 MCCTest.VType6::f11
    IL_00d0:  ldarga.s   val
    IL_00d2:  ldfld      float64 MCCTest.VType6::f11
    IL_00d7:  add
    IL_00d8:  stfld      float64 MCCTest.VType6::f11
    IL_00dd:  ldarg.0
    IL_00de:  dup
    IL_00df:  ldfld      float64 MCCTest.VType6::f12
    IL_00e4:  ldarga.s   val
    IL_00e6:  ldfld      float64 MCCTest.VType6::f12
    IL_00eb:  add
    IL_00ec:  stfld      float64 MCCTest.VType6::f12
    IL_00f1:  ldarg.0
    IL_00f2:  dup
    IL_00f3:  ldfld      float64 MCCTest.VType6::f13
    IL_00f8:  ldarga.s   val
    IL_00fa:  ldfld      float64 MCCTest.VType6::f13
    IL_00ff:  add
    IL_0100:  stfld      float64 MCCTest.VType6::f13
    IL_0105:  ldarg.0
    IL_0106:  dup
    IL_0107:  ldfld      float64 MCCTest.VType6::f14
    IL_010c:  ldarga.s   val
    IL_010e:  ldfld      float64 MCCTest.VType6::f14
    IL_0113:  add
    IL_0114:  stfld      float64 MCCTest.VType6::f14
    IL_0119:  ldarg.0
    IL_011a:  dup
    IL_011b:  ldfld      float64 MCCTest.VType6::f15
    IL_0120:  ldarga.s   val
    IL_0122:  ldfld      float64 MCCTest.VType6::f15
    IL_0127:  add
    IL_0128:  stfld      float64 MCCTest.VType6::f15
    IL_012d:  ldarg.0
    IL_012e:  dup
    IL_012f:  ldfld      float64 MCCTest.VType6::f16
    IL_0134:  ldarga.s   val
    IL_0136:  ldfld      float64 MCCTest.VType6::f16
    IL_013b:  add
    IL_013c:  stfld      float64 MCCTest.VType6::f16
    IL_0141:  ldarg.0
    IL_0142:  dup
    IL_0143:  ldfld      float64 MCCTest.VType6::f17
    IL_0148:  ldarga.s   val
    IL_014a:  ldfld      float64 MCCTest.VType6::f17
    IL_014f:  add
    IL_0150:  stfld      float64 MCCTest.VType6::f17
    IL_0155:  ldarg.0
    IL_0156:  dup
    IL_0157:  ldfld      float64 MCCTest.VType6::f18
    IL_015c:  ldarga.s   val
    IL_015e:  ldfld      float64 MCCTest.VType6::f18
    IL_0163:  add
    IL_0164:  stfld      float64 MCCTest.VType6::f18
    IL_0169:  ldarg.0
    IL_016a:  dup
    IL_016b:  ldfld      float64 MCCTest.VType6::f19
    IL_0170:  ldarga.s   val
    IL_0172:  ldfld      float64 MCCTest.VType6::f19
    IL_0177:  add
    IL_0178:  stfld      float64 MCCTest.VType6::f19
    IL_017d:  ldarg.0
    IL_017e:  dup
    IL_017f:  ldfld      float64 MCCTest.VType6::f20
    IL_0184:  ldarga.s   val
    IL_0186:  ldfld      float64 MCCTest.VType6::f20
    IL_018b:  add
    IL_018c:  stfld      float64 MCCTest.VType6::f20
    IL_0191:  ldarg.0
    IL_0192:  dup
    IL_0193:  ldfld      float64 MCCTest.VType6::f21
    IL_0198:  ldarga.s   val
    IL_019a:  ldfld      float64 MCCTest.VType6::f21
    IL_019f:  add
    IL_01a0:  stfld      float64 MCCTest.VType6::f21
    IL_01a5:  ldarg.0
    IL_01a6:  dup
    IL_01a7:  ldfld      float64 MCCTest.VType6::f22
    IL_01ac:  ldarga.s   val
    IL_01ae:  ldfld      float64 MCCTest.VType6::f22
    IL_01b3:  add
    IL_01b4:  stfld      float64 MCCTest.VType6::f22
    IL_01b9:  ldarg.0
    IL_01ba:  dup
    IL_01bb:  ldfld      float64 MCCTest.VType6::f23
    IL_01c0:  ldarga.s   val
    IL_01c2:  ldfld      float64 MCCTest.VType6::f23
    IL_01c7:  add
    IL_01c8:  stfld      float64 MCCTest.VType6::f23
    IL_01cd:  ldarg.0
    IL_01ce:  dup
    IL_01cf:  ldfld      float64 MCCTest.VType6::f24
    IL_01d4:  ldarga.s   val
    IL_01d6:  ldfld      float64 MCCTest.VType6::f24
    IL_01db:  add
    IL_01dc:  stfld      float64 MCCTest.VType6::f24
    IL_01e1:  ldarg.0
    IL_01e2:  dup
    IL_01e3:  ldfld      float64 MCCTest.VType6::f25
    IL_01e8:  ldarga.s   val
    IL_01ea:  ldfld      float64 MCCTest.VType6::f25
    IL_01ef:  add
    IL_01f0:  stfld      float64 MCCTest.VType6::f25
    IL_01f5:  ldarg.0
    IL_01f6:  dup
    IL_01f7:  ldfld      float64 MCCTest.VType6::f26
    IL_01fc:  ldarga.s   val
    IL_01fe:  ldfld      float64 MCCTest.VType6::f26
    IL_0203:  add
    IL_0204:  stfld      float64 MCCTest.VType6::f26
    IL_0209:  ldarg.0
    IL_020a:  dup
    IL_020b:  ldfld      float64 MCCTest.VType6::f27
    IL_0210:  ldarga.s   val
    IL_0212:  ldfld      float64 MCCTest.VType6::f27
    IL_0217:  add
    IL_0218:  stfld      float64 MCCTest.VType6::f27
    IL_021d:  ldarg.0
    IL_021e:  dup
    IL_021f:  ldfld      float64 MCCTest.VType6::f28
    IL_0224:  ldarga.s   val
    IL_0226:  ldfld      float64 MCCTest.VType6::f28
    IL_022b:  add
    IL_022c:  stfld      float64 MCCTest.VType6::f28
    IL_0231:  ldarg.0
    IL_0232:  dup
    IL_0233:  ldfld      float64 MCCTest.VType6::f29
    IL_0238:  ldarga.s   val
    IL_023a:  ldfld      float64 MCCTest.VType6::f29
    IL_023f:  add
    IL_0240:  stfld      float64 MCCTest.VType6::f29
    IL_0245:  ret
  } // end of method VType6::Add

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VType6 expected) cil managed
  {
    // Code size       1278 (0x4fe)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      float64 MCCTest.VType6::f1
    IL_0007:  ldarga.s   expected
    IL_0009:  ldfld      float64 MCCTest.VType6::f1
    IL_000e:  ceq
    IL_0010:  stloc.0
    IL_0011:  ldloc.0
    IL_0012:  brtrue.s   IL_002d

    IL_0014:  nop
    IL_0015:  ldstr      "f1"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      float64 MCCTest.VType6::f1
    IL_0020:  ldarga.s   expected
    IL_0022:  ldfld      float64 MCCTest.VType6::f1
    IL_0027:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_002c:  throw

    IL_002d:  ldarg.0
    IL_002e:  ldfld      float64 MCCTest.VType6::f2
    IL_0033:  ldarga.s   expected
    IL_0035:  ldfld      float64 MCCTest.VType6::f2
    IL_003a:  ceq
    IL_003c:  stloc.0
    IL_003d:  ldloc.0
    IL_003e:  brtrue.s   IL_0059

    IL_0040:  nop
    IL_0041:  ldstr      "f2"
    IL_0046:  ldarg.0
    IL_0047:  ldfld      float64 MCCTest.VType6::f2
    IL_004c:  ldarga.s   expected
    IL_004e:  ldfld      float64 MCCTest.VType6::f2
    IL_0053:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0058:  throw

    IL_0059:  ldarg.0
    IL_005a:  ldfld      float64 MCCTest.VType6::f3
    IL_005f:  ldarga.s   expected
    IL_0061:  ldfld      float64 MCCTest.VType6::f3
    IL_0066:  ceq
    IL_0068:  stloc.0
    IL_0069:  ldloc.0
    IL_006a:  brtrue.s   IL_0085

    IL_006c:  nop
    IL_006d:  ldstr      "f3"
    IL_0072:  ldarg.0
    IL_0073:  ldfld      float64 MCCTest.VType6::f3
    IL_0078:  ldarga.s   expected
    IL_007a:  ldfld      float64 MCCTest.VType6::f3
    IL_007f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0084:  throw

    IL_0085:  ldarg.0
    IL_0086:  ldfld      float64 MCCTest.VType6::f4
    IL_008b:  ldarga.s   expected
    IL_008d:  ldfld      float64 MCCTest.VType6::f4
    IL_0092:  ceq
    IL_0094:  stloc.0
    IL_0095:  ldloc.0
    IL_0096:  brtrue.s   IL_00b1

    IL_0098:  nop
    IL_0099:  ldstr      "f4"
    IL_009e:  ldarg.0
    IL_009f:  ldfld      float64 MCCTest.VType6::f4
    IL_00a4:  ldarga.s   expected
    IL_00a6:  ldfld      float64 MCCTest.VType6::f4
    IL_00ab:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00b0:  throw

    IL_00b1:  ldarg.0
    IL_00b2:  ldfld      float64 MCCTest.VType6::f5
    IL_00b7:  ldarga.s   expected
    IL_00b9:  ldfld      float64 MCCTest.VType6::f5
    IL_00be:  ceq
    IL_00c0:  stloc.0
    IL_00c1:  ldloc.0
    IL_00c2:  brtrue.s   IL_00dd

    IL_00c4:  nop
    IL_00c5:  ldstr      "f5"
    IL_00ca:  ldarg.0
    IL_00cb:  ldfld      float64 MCCTest.VType6::f5
    IL_00d0:  ldarga.s   expected
    IL_00d2:  ldfld      float64 MCCTest.VType6::f5
    IL_00d7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00dc:  throw

    IL_00dd:  ldarg.0
    IL_00de:  ldfld      float64 MCCTest.VType6::f6
    IL_00e3:  ldarga.s   expected
    IL_00e5:  ldfld      float64 MCCTest.VType6::f6
    IL_00ea:  ceq
    IL_00ec:  stloc.0
    IL_00ed:  ldloc.0
    IL_00ee:  brtrue.s   IL_0109

    IL_00f0:  nop
    IL_00f1:  ldstr      "f6"
    IL_00f6:  ldarg.0
    IL_00f7:  ldfld      float64 MCCTest.VType6::f6
    IL_00fc:  ldarga.s   expected
    IL_00fe:  ldfld      float64 MCCTest.VType6::f6
    IL_0103:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0108:  throw

    IL_0109:  ldarg.0
    IL_010a:  ldfld      float64 MCCTest.VType6::f7
    IL_010f:  ldarga.s   expected
    IL_0111:  ldfld      float64 MCCTest.VType6::f7
    IL_0116:  ceq
    IL_0118:  stloc.0
    IL_0119:  ldloc.0
    IL_011a:  brtrue.s   IL_0135

    IL_011c:  nop
    IL_011d:  ldstr      "f7"
    IL_0122:  ldarg.0
    IL_0123:  ldfld      float64 MCCTest.VType6::f7
    IL_0128:  ldarga.s   expected
    IL_012a:  ldfld      float64 MCCTest.VType6::f7
    IL_012f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0134:  throw

    IL_0135:  ldarg.0
    IL_0136:  ldfld      float64 MCCTest.VType6::f8
    IL_013b:  ldarga.s   expected
    IL_013d:  ldfld      float64 MCCTest.VType6::f8
    IL_0142:  ceq
    IL_0144:  stloc.0
    IL_0145:  ldloc.0
    IL_0146:  brtrue.s   IL_0161

    IL_0148:  nop
    IL_0149:  ldstr      "f8"
    IL_014e:  ldarg.0
    IL_014f:  ldfld      float64 MCCTest.VType6::f8
    IL_0154:  ldarga.s   expected
    IL_0156:  ldfld      float64 MCCTest.VType6::f8
    IL_015b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0160:  throw

    IL_0161:  ldarg.0
    IL_0162:  ldfld      float64 MCCTest.VType6::f9
    IL_0167:  ldarga.s   expected
    IL_0169:  ldfld      float64 MCCTest.VType6::f9
    IL_016e:  ceq
    IL_0170:  stloc.0
    IL_0171:  ldloc.0
    IL_0172:  brtrue.s   IL_018d

    IL_0174:  nop
    IL_0175:  ldstr      "f9"
    IL_017a:  ldarg.0
    IL_017b:  ldfld      float64 MCCTest.VType6::f9
    IL_0180:  ldarga.s   expected
    IL_0182:  ldfld      float64 MCCTest.VType6::f9
    IL_0187:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_018c:  throw

    IL_018d:  ldarg.0
    IL_018e:  ldfld      float64 MCCTest.VType6::f10
    IL_0193:  ldarga.s   expected
    IL_0195:  ldfld      float64 MCCTest.VType6::f10
    IL_019a:  ceq
    IL_019c:  stloc.0
    IL_019d:  ldloc.0
    IL_019e:  brtrue.s   IL_01b9

    IL_01a0:  nop
    IL_01a1:  ldstr      "f10"
    IL_01a6:  ldarg.0
    IL_01a7:  ldfld      float64 MCCTest.VType6::f10
    IL_01ac:  ldarga.s   expected
    IL_01ae:  ldfld      float64 MCCTest.VType6::f10
    IL_01b3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_01b8:  throw

    IL_01b9:  ldarg.0
    IL_01ba:  ldfld      float64 MCCTest.VType6::f11
    IL_01bf:  ldarga.s   expected
    IL_01c1:  ldfld      float64 MCCTest.VType6::f11
    IL_01c6:  ceq
    IL_01c8:  stloc.0
    IL_01c9:  ldloc.0
    IL_01ca:  brtrue.s   IL_01e5

    IL_01cc:  nop
    IL_01cd:  ldstr      "f11"
    IL_01d2:  ldarg.0
    IL_01d3:  ldfld      float64 MCCTest.VType6::f11
    IL_01d8:  ldarga.s   expected
    IL_01da:  ldfld      float64 MCCTest.VType6::f11
    IL_01df:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_01e4:  throw

    IL_01e5:  ldarg.0
    IL_01e6:  ldfld      float64 MCCTest.VType6::f12
    IL_01eb:  ldarga.s   expected
    IL_01ed:  ldfld      float64 MCCTest.VType6::f12
    IL_01f2:  ceq
    IL_01f4:  stloc.0
    IL_01f5:  ldloc.0
    IL_01f6:  brtrue.s   IL_0211

    IL_01f8:  nop
    IL_01f9:  ldstr      "f12"
    IL_01fe:  ldarg.0
    IL_01ff:  ldfld      float64 MCCTest.VType6::f12
    IL_0204:  ldarga.s   expected
    IL_0206:  ldfld      float64 MCCTest.VType6::f12
    IL_020b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0210:  throw

    IL_0211:  ldarg.0
    IL_0212:  ldfld      float64 MCCTest.VType6::f13
    IL_0217:  ldarga.s   expected
    IL_0219:  ldfld      float64 MCCTest.VType6::f13
    IL_021e:  ceq
    IL_0220:  stloc.0
    IL_0221:  ldloc.0
    IL_0222:  brtrue.s   IL_023d

    IL_0224:  nop
    IL_0225:  ldstr      "f13"
    IL_022a:  ldarg.0
    IL_022b:  ldfld      float64 MCCTest.VType6::f13
    IL_0230:  ldarga.s   expected
    IL_0232:  ldfld      float64 MCCTest.VType6::f13
    IL_0237:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_023c:  throw

    IL_023d:  ldarg.0
    IL_023e:  ldfld      float64 MCCTest.VType6::f14
    IL_0243:  ldarga.s   expected
    IL_0245:  ldfld      float64 MCCTest.VType6::f14
    IL_024a:  ceq
    IL_024c:  stloc.0
    IL_024d:  ldloc.0
    IL_024e:  brtrue.s   IL_0269

    IL_0250:  nop
    IL_0251:  ldstr      "f14"
    IL_0256:  ldarg.0
    IL_0257:  ldfld      float64 MCCTest.VType6::f14
    IL_025c:  ldarga.s   expected
    IL_025e:  ldfld      float64 MCCTest.VType6::f14
    IL_0263:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0268:  throw

    IL_0269:  ldarg.0
    IL_026a:  ldfld      float64 MCCTest.VType6::f15
    IL_026f:  ldarga.s   expected
    IL_0271:  ldfld      float64 MCCTest.VType6::f15
    IL_0276:  ceq
    IL_0278:  stloc.0
    IL_0279:  ldloc.0
    IL_027a:  brtrue.s   IL_0295

    IL_027c:  nop
    IL_027d:  ldstr      "f15"
    IL_0282:  ldarg.0
    IL_0283:  ldfld      float64 MCCTest.VType6::f15
    IL_0288:  ldarga.s   expected
    IL_028a:  ldfld      float64 MCCTest.VType6::f15
    IL_028f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0294:  throw

    IL_0295:  ldarg.0
    IL_0296:  ldfld      float64 MCCTest.VType6::f16
    IL_029b:  ldarga.s   expected
    IL_029d:  ldfld      float64 MCCTest.VType6::f16
    IL_02a2:  ceq
    IL_02a4:  stloc.0
    IL_02a5:  ldloc.0
    IL_02a6:  brtrue.s   IL_02c1

    IL_02a8:  nop
    IL_02a9:  ldstr      "f16"
    IL_02ae:  ldarg.0
    IL_02af:  ldfld      float64 MCCTest.VType6::f16
    IL_02b4:  ldarga.s   expected
    IL_02b6:  ldfld      float64 MCCTest.VType6::f16
    IL_02bb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_02c0:  throw

    IL_02c1:  ldarg.0
    IL_02c2:  ldfld      float64 MCCTest.VType6::f17
    IL_02c7:  ldarga.s   expected
    IL_02c9:  ldfld      float64 MCCTest.VType6::f17
    IL_02ce:  ceq
    IL_02d0:  stloc.0
    IL_02d1:  ldloc.0
    IL_02d2:  brtrue.s   IL_02ed

    IL_02d4:  nop
    IL_02d5:  ldstr      "f17"
    IL_02da:  ldarg.0
    IL_02db:  ldfld      float64 MCCTest.VType6::f17
    IL_02e0:  ldarga.s   expected
    IL_02e2:  ldfld      float64 MCCTest.VType6::f17
    IL_02e7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_02ec:  throw

    IL_02ed:  ldarg.0
    IL_02ee:  ldfld      float64 MCCTest.VType6::f18
    IL_02f3:  ldarga.s   expected
    IL_02f5:  ldfld      float64 MCCTest.VType6::f18
    IL_02fa:  ceq
    IL_02fc:  stloc.0
    IL_02fd:  ldloc.0
    IL_02fe:  brtrue.s   IL_0319

    IL_0300:  nop
    IL_0301:  ldstr      "f18"
    IL_0306:  ldarg.0
    IL_0307:  ldfld      float64 MCCTest.VType6::f18
    IL_030c:  ldarga.s   expected
    IL_030e:  ldfld      float64 MCCTest.VType6::f18
    IL_0313:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0318:  throw

    IL_0319:  ldarg.0
    IL_031a:  ldfld      float64 MCCTest.VType6::f19
    IL_031f:  ldarga.s   expected
    IL_0321:  ldfld      float64 MCCTest.VType6::f19
    IL_0326:  ceq
    IL_0328:  stloc.0
    IL_0329:  ldloc.0
    IL_032a:  brtrue.s   IL_0345

    IL_032c:  nop
    IL_032d:  ldstr      "f19"
    IL_0332:  ldarg.0
    IL_0333:  ldfld      float64 MCCTest.VType6::f19
    IL_0338:  ldarga.s   expected
    IL_033a:  ldfld      float64 MCCTest.VType6::f19
    IL_033f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0344:  throw

    IL_0345:  ldarg.0
    IL_0346:  ldfld      float64 MCCTest.VType6::f20
    IL_034b:  ldarga.s   expected
    IL_034d:  ldfld      float64 MCCTest.VType6::f20
    IL_0352:  ceq
    IL_0354:  stloc.0
    IL_0355:  ldloc.0
    IL_0356:  brtrue.s   IL_0371

    IL_0358:  nop
    IL_0359:  ldstr      "f20"
    IL_035e:  ldarg.0
    IL_035f:  ldfld      float64 MCCTest.VType6::f20
    IL_0364:  ldarga.s   expected
    IL_0366:  ldfld      float64 MCCTest.VType6::f20
    IL_036b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0370:  throw

    IL_0371:  ldarg.0
    IL_0372:  ldfld      float64 MCCTest.VType6::f21
    IL_0377:  ldarga.s   expected
    IL_0379:  ldfld      float64 MCCTest.VType6::f21
    IL_037e:  ceq
    IL_0380:  stloc.0
    IL_0381:  ldloc.0
    IL_0382:  brtrue.s   IL_039d

    IL_0384:  nop
    IL_0385:  ldstr      "f21"
    IL_038a:  ldarg.0
    IL_038b:  ldfld      float64 MCCTest.VType6::f21
    IL_0390:  ldarga.s   expected
    IL_0392:  ldfld      float64 MCCTest.VType6::f21
    IL_0397:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_039c:  throw

    IL_039d:  ldarg.0
    IL_039e:  ldfld      float64 MCCTest.VType6::f22
    IL_03a3:  ldarga.s   expected
    IL_03a5:  ldfld      float64 MCCTest.VType6::f22
    IL_03aa:  ceq
    IL_03ac:  stloc.0
    IL_03ad:  ldloc.0
    IL_03ae:  brtrue.s   IL_03c9

    IL_03b0:  nop
    IL_03b1:  ldstr      "f22"
    IL_03b6:  ldarg.0
    IL_03b7:  ldfld      float64 MCCTest.VType6::f22
    IL_03bc:  ldarga.s   expected
    IL_03be:  ldfld      float64 MCCTest.VType6::f22
    IL_03c3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_03c8:  throw

    IL_03c9:  ldarg.0
    IL_03ca:  ldfld      float64 MCCTest.VType6::f23
    IL_03cf:  ldarga.s   expected
    IL_03d1:  ldfld      float64 MCCTest.VType6::f23
    IL_03d6:  ceq
    IL_03d8:  stloc.0
    IL_03d9:  ldloc.0
    IL_03da:  brtrue.s   IL_03f5

    IL_03dc:  nop
    IL_03dd:  ldstr      "f23"
    IL_03e2:  ldarg.0
    IL_03e3:  ldfld      float64 MCCTest.VType6::f23
    IL_03e8:  ldarga.s   expected
    IL_03ea:  ldfld      float64 MCCTest.VType6::f23
    IL_03ef:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_03f4:  throw

    IL_03f5:  ldarg.0
    IL_03f6:  ldfld      float64 MCCTest.VType6::f24
    IL_03fb:  ldarga.s   expected
    IL_03fd:  ldfld      float64 MCCTest.VType6::f24
    IL_0402:  ceq
    IL_0404:  stloc.0
    IL_0405:  ldloc.0
    IL_0406:  brtrue.s   IL_0421

    IL_0408:  nop
    IL_0409:  ldstr      "f24"
    IL_040e:  ldarg.0
    IL_040f:  ldfld      float64 MCCTest.VType6::f24
    IL_0414:  ldarga.s   expected
    IL_0416:  ldfld      float64 MCCTest.VType6::f24
    IL_041b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0420:  throw

    IL_0421:  ldarg.0
    IL_0422:  ldfld      float64 MCCTest.VType6::f25
    IL_0427:  ldarga.s   expected
    IL_0429:  ldfld      float64 MCCTest.VType6::f25
    IL_042e:  ceq
    IL_0430:  stloc.0
    IL_0431:  ldloc.0
    IL_0432:  brtrue.s   IL_044d

    IL_0434:  nop
    IL_0435:  ldstr      "f25"
    IL_043a:  ldarg.0
    IL_043b:  ldfld      float64 MCCTest.VType6::f25
    IL_0440:  ldarga.s   expected
    IL_0442:  ldfld      float64 MCCTest.VType6::f25
    IL_0447:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_044c:  throw

    IL_044d:  ldarg.0
    IL_044e:  ldfld      float64 MCCTest.VType6::f26
    IL_0453:  ldarga.s   expected
    IL_0455:  ldfld      float64 MCCTest.VType6::f26
    IL_045a:  ceq
    IL_045c:  stloc.0
    IL_045d:  ldloc.0
    IL_045e:  brtrue.s   IL_0479

    IL_0460:  nop
    IL_0461:  ldstr      "f26"
    IL_0466:  ldarg.0
    IL_0467:  ldfld      float64 MCCTest.VType6::f26
    IL_046c:  ldarga.s   expected
    IL_046e:  ldfld      float64 MCCTest.VType6::f26
    IL_0473:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0478:  throw

    IL_0479:  ldarg.0
    IL_047a:  ldfld      float64 MCCTest.VType6::f27
    IL_047f:  ldarga.s   expected
    IL_0481:  ldfld      float64 MCCTest.VType6::f27
    IL_0486:  ceq
    IL_0488:  stloc.0
    IL_0489:  ldloc.0
    IL_048a:  brtrue.s   IL_04a5

    IL_048c:  nop
    IL_048d:  ldstr      "f27"
    IL_0492:  ldarg.0
    IL_0493:  ldfld      float64 MCCTest.VType6::f27
    IL_0498:  ldarga.s   expected
    IL_049a:  ldfld      float64 MCCTest.VType6::f27
    IL_049f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_04a4:  throw

    IL_04a5:  ldarg.0
    IL_04a6:  ldfld      float64 MCCTest.VType6::f28
    IL_04ab:  ldarga.s   expected
    IL_04ad:  ldfld      float64 MCCTest.VType6::f28
    IL_04b2:  ceq
    IL_04b4:  stloc.0
    IL_04b5:  ldloc.0
    IL_04b6:  brtrue.s   IL_04d1

    IL_04b8:  nop
    IL_04b9:  ldstr      "f28"
    IL_04be:  ldarg.0
    IL_04bf:  ldfld      float64 MCCTest.VType6::f28
    IL_04c4:  ldarga.s   expected
    IL_04c6:  ldfld      float64 MCCTest.VType6::f28
    IL_04cb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_04d0:  throw

    IL_04d1:  ldarg.0
    IL_04d2:  ldfld      float64 MCCTest.VType6::f29
    IL_04d7:  ldarga.s   expected
    IL_04d9:  ldfld      float64 MCCTest.VType6::f29
    IL_04de:  ceq
    IL_04e0:  stloc.0
    IL_04e1:  ldloc.0
    IL_04e2:  brtrue.s   IL_04fd

    IL_04e4:  nop
    IL_04e5:  ldstr      "f29"
    IL_04ea:  ldarg.0
    IL_04eb:  ldfld      float64 MCCTest.VType6::f29
    IL_04f0:  ldarga.s   expected
    IL_04f2:  ldfld      float64 MCCTest.VType6::f29
    IL_04f7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_04fc:  throw

    IL_04fd:  ret
  } // end of method VType6::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       866 (0x362)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "f1    = "
    IL_0015:  ldarg.0
    IL_0016:  ldfld      float64 MCCTest.VType6::f1
    IL_001b:  box        [mscorlib]System.Double
    IL_0020:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0025:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_002a:  nop
    IL_002b:  ldloc.1
    IL_002c:  ldloc.0
    IL_002d:  ldstr      "f2    = "
    IL_0032:  ldarg.0
    IL_0033:  ldfld      float64 MCCTest.VType6::f2
    IL_0038:  box        [mscorlib]System.Double
    IL_003d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0042:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0047:  nop
    IL_0048:  ldloc.1
    IL_0049:  ldloc.0
    IL_004a:  ldstr      "f3    = "
    IL_004f:  ldarg.0
    IL_0050:  ldfld      float64 MCCTest.VType6::f3
    IL_0055:  box        [mscorlib]System.Double
    IL_005a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_005f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0064:  nop
    IL_0065:  ldloc.1
    IL_0066:  ldloc.0
    IL_0067:  ldstr      "f4    = "
    IL_006c:  ldarg.0
    IL_006d:  ldfld      float64 MCCTest.VType6::f4
    IL_0072:  box        [mscorlib]System.Double
    IL_0077:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_007c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0081:  nop
    IL_0082:  ldloc.1
    IL_0083:  ldloc.0
    IL_0084:  ldstr      "f5    = "
    IL_0089:  ldarg.0
    IL_008a:  ldfld      float64 MCCTest.VType6::f5
    IL_008f:  box        [mscorlib]System.Double
    IL_0094:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0099:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_009e:  nop
    IL_009f:  ldloc.1
    IL_00a0:  ldloc.0
    IL_00a1:  ldstr      "f6    = "
    IL_00a6:  ldarg.0
    IL_00a7:  ldfld      float64 MCCTest.VType6::f6
    IL_00ac:  box        [mscorlib]System.Double
    IL_00b1:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00b6:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00bb:  nop
    IL_00bc:  ldloc.1
    IL_00bd:  ldloc.0
    IL_00be:  ldstr      "f7    = "
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      float64 MCCTest.VType6::f7
    IL_00c9:  box        [mscorlib]System.Double
    IL_00ce:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00d3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00d8:  nop
    IL_00d9:  ldloc.1
    IL_00da:  ldloc.0
    IL_00db:  ldstr      "f8    = "
    IL_00e0:  ldarg.0
    IL_00e1:  ldfld      float64 MCCTest.VType6::f8
    IL_00e6:  box        [mscorlib]System.Double
    IL_00eb:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00f0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00f5:  nop
    IL_00f6:  ldloc.1
    IL_00f7:  ldloc.0
    IL_00f8:  ldstr      "f9    = "
    IL_00fd:  ldarg.0
    IL_00fe:  ldfld      float64 MCCTest.VType6::f9
    IL_0103:  box        [mscorlib]System.Double
    IL_0108:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_010d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0112:  nop
    IL_0113:  ldloc.1
    IL_0114:  ldloc.0
    IL_0115:  ldstr      "f10    = "
    IL_011a:  ldarg.0
    IL_011b:  ldfld      float64 MCCTest.VType6::f10
    IL_0120:  box        [mscorlib]System.Double
    IL_0125:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_012a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_012f:  nop
    IL_0130:  ldloc.1
    IL_0131:  ldloc.0
    IL_0132:  ldstr      "f11    = "
    IL_0137:  ldarg.0
    IL_0138:  ldfld      float64 MCCTest.VType6::f11
    IL_013d:  box        [mscorlib]System.Double
    IL_0142:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0147:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_014c:  nop
    IL_014d:  ldloc.1
    IL_014e:  ldloc.0
    IL_014f:  ldstr      "f12    = "
    IL_0154:  ldarg.0
    IL_0155:  ldfld      float64 MCCTest.VType6::f12
    IL_015a:  box        [mscorlib]System.Double
    IL_015f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0164:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0169:  nop
    IL_016a:  ldloc.1
    IL_016b:  ldloc.0
    IL_016c:  ldstr      "f13    = "
    IL_0171:  ldarg.0
    IL_0172:  ldfld      float64 MCCTest.VType6::f13
    IL_0177:  box        [mscorlib]System.Double
    IL_017c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0181:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0186:  nop
    IL_0187:  ldloc.1
    IL_0188:  ldloc.0
    IL_0189:  ldstr      "f14    = "
    IL_018e:  ldarg.0
    IL_018f:  ldfld      float64 MCCTest.VType6::f14
    IL_0194:  box        [mscorlib]System.Double
    IL_0199:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_019e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01a3:  nop
    IL_01a4:  ldloc.1
    IL_01a5:  ldloc.0
    IL_01a6:  ldstr      "f15    = "
    IL_01ab:  ldarg.0
    IL_01ac:  ldfld      float64 MCCTest.VType6::f15
    IL_01b1:  box        [mscorlib]System.Double
    IL_01b6:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01bb:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01c0:  nop
    IL_01c1:  ldloc.1
    IL_01c2:  ldloc.0
    IL_01c3:  ldstr      "f16    = "
    IL_01c8:  ldarg.0
    IL_01c9:  ldfld      float64 MCCTest.VType6::f16
    IL_01ce:  box        [mscorlib]System.Double
    IL_01d3:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01d8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01dd:  nop
    IL_01de:  ldloc.1
    IL_01df:  ldloc.0
    IL_01e0:  ldstr      "f17    = "
    IL_01e5:  ldarg.0
    IL_01e6:  ldfld      float64 MCCTest.VType6::f17
    IL_01eb:  box        [mscorlib]System.Double
    IL_01f0:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01f5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01fa:  nop
    IL_01fb:  ldloc.1
    IL_01fc:  ldloc.0
    IL_01fd:  ldstr      "f18    = "
    IL_0202:  ldarg.0
    IL_0203:  ldfld      float64 MCCTest.VType6::f18
    IL_0208:  box        [mscorlib]System.Double
    IL_020d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0212:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0217:  nop
    IL_0218:  ldloc.1
    IL_0219:  ldloc.0
    IL_021a:  ldstr      "f19    = "
    IL_021f:  ldarg.0
    IL_0220:  ldfld      float64 MCCTest.VType6::f19
    IL_0225:  box        [mscorlib]System.Double
    IL_022a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_022f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0234:  nop
    IL_0235:  ldloc.1
    IL_0236:  ldloc.0
    IL_0237:  ldstr      "f20    = "
    IL_023c:  ldarg.0
    IL_023d:  ldfld      float64 MCCTest.VType6::f20
    IL_0242:  box        [mscorlib]System.Double
    IL_0247:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_024c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0251:  nop
    IL_0252:  ldloc.1
    IL_0253:  ldloc.0
    IL_0254:  ldstr      "f21    = "
    IL_0259:  ldarg.0
    IL_025a:  ldfld      float64 MCCTest.VType6::f21
    IL_025f:  box        [mscorlib]System.Double
    IL_0264:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0269:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_026e:  nop
    IL_026f:  ldloc.1
    IL_0270:  ldloc.0
    IL_0271:  ldstr      "f22    = "
    IL_0276:  ldarg.0
    IL_0277:  ldfld      float64 MCCTest.VType6::f22
    IL_027c:  box        [mscorlib]System.Double
    IL_0281:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0286:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_028b:  nop
    IL_028c:  ldloc.1
    IL_028d:  ldloc.0
    IL_028e:  ldstr      "f23    = "
    IL_0293:  ldarg.0
    IL_0294:  ldfld      float64 MCCTest.VType6::f23
    IL_0299:  box        [mscorlib]System.Double
    IL_029e:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_02a3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02a8:  nop
    IL_02a9:  ldloc.1
    IL_02aa:  ldloc.0
    IL_02ab:  ldstr      "f24    = "
    IL_02b0:  ldarg.0
    IL_02b1:  ldfld      float64 MCCTest.VType6::f24
    IL_02b6:  box        [mscorlib]System.Double
    IL_02bb:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_02c0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02c5:  nop
    IL_02c6:  ldloc.1
    IL_02c7:  ldloc.0
    IL_02c8:  ldstr      "f25    = "
    IL_02cd:  ldarg.0
    IL_02ce:  ldfld      float64 MCCTest.VType6::f25
    IL_02d3:  box        [mscorlib]System.Double
    IL_02d8:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_02dd:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02e2:  nop
    IL_02e3:  ldloc.1
    IL_02e4:  ldloc.0
    IL_02e5:  ldstr      "f26    = "
    IL_02ea:  ldarg.0
    IL_02eb:  ldfld      float64 MCCTest.VType6::f26
    IL_02f0:  box        [mscorlib]System.Double
    IL_02f5:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_02fa:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02ff:  nop
    IL_0300:  ldloc.1
    IL_0301:  ldloc.0
    IL_0302:  ldstr      "f27    = "
    IL_0307:  ldarg.0
    IL_0308:  ldfld      float64 MCCTest.VType6::f27
    IL_030d:  box        [mscorlib]System.Double
    IL_0312:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0317:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_031c:  nop
    IL_031d:  ldloc.1
    IL_031e:  ldloc.0
    IL_031f:  ldstr      "f28    = "
    IL_0324:  ldarg.0
    IL_0325:  ldfld      float64 MCCTest.VType6::f28
    IL_032a:  box        [mscorlib]System.Double
    IL_032f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0334:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0339:  nop
    IL_033a:  ldloc.1
    IL_033b:  ldloc.0
    IL_033c:  ldstr      "f29    = "
    IL_0341:  ldarg.0
    IL_0342:  ldfld      float64 MCCTest.VType6::f29
    IL_0347:  box        [mscorlib]System.Double
    IL_034c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0351:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0356:  nop
    IL_0357:  ldloc.1
    IL_0358:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_035d:  stloc.2
    IL_035e:  br.s       IL_0360

    IL_0360:  ldloc.2
    IL_0361:  ret
  } // end of method VType6::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VType6::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VType6::Dump

} // end of class MCCTest.VType6

.class public sequential ansi sealed beforefieldinit MCCTest.VType7
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VType7>
{
  .field public float64 f1
  .field public float64 f2
  .field public float64 f3
  .field public float64 f4
  .field public float64 f5
  .field public float64 f6
  .field public float64 f7
  .field public float64 f8
  .field public float64 f9
  .field public float64 f10
  .field public float64 f11
  .field public float64 f12
  .field public float64 f13
  .field public float64 f14
  .field public float64 f15
  .field public float64 f16
  .field public float64 f17
  .field public float64 f18
  .field public float64 f19
  .field public float64 f20
  .field public float64 f21
  .field public float64 f22
  .field public float64 f23
  .field public float64 f24
  .field public float64 f25
  .field public float64 f26
  .field public float64 f27
  .field public float64 f28
  .field public float64 f29
  .field public float64 f30
  .field public float64 f31
  .field public float64 f32
  .field public float64 f33
  .field public float64 f34
  .field public float64 f35
  .field public float64 f36
  .field public float64 f37
  .field public float64 f38
  .field public float64 f39
  .field public float64 f40
  .field public float64 f41
  .field public float64 f42
  .field public float64 f43
  .field public float64 f44
  .field public float64 f45
  .field public float64 f46
  .field public float64 f47
  .field public float64 f48
  .field public float64 f49
  .field public float64 f50
  .field public float64 f51
  .field public float64 f52
  .field public float64 f53
  .field public float64 f54
  .field public float64 f55
  .field public float64 f56
  .field public float64 f57
  .field public float64 f58
  .field public float64 f59
  .field public float64 f60
  .field public float64 f61
  .field public float64 f62
  .field public float64 f63
  .field public float64 f64
  .field public float64 f65
  .field public float64 f66
  .field public float64 f67
  .field public float64 f68
  .field public float64 f69
  .field public float64 f70
  .field public float64 f71
  .field public float64 f72
  .field public float64 f73
  .field public float64 f74
  .field public float64 f75
  .field public float64 f76
  .field public float64 f77
  .field public float64 f78
  .field public float64 f79
  .field public float64 f80
  .field public float64 f81
  .field public float64 f82
  .field public float64 f83
  .field public float64 f84
  .field public float64 f85
  .field public float64 f86
  .field public float64 f87
  .field public float64 f88
  .field public float64 f89
  .field public float64 f90
  .field public float64 f91
  .field public float64 f92
  .field public float64 f93
  .field public float64 f94
  .field public float64 f95
  .field public float64 f96
  .field public float64 f97
  .field public float64 f98
  .field public float64 f99
  .field public float64 f100
  .field public float64 f101
  .field public float64 f102
  .field public float64 f103
  .field public float64 f104
  .field public float64 f105
  .field public float64 f106
  .field public float64 f107
  .field public float64 f108
  .field public float64 f109
  .field public float64 f110
  .field public float64 f111
  .field public float64 f112
  .field public float64 f113
  .field public float64 f114
  .field public float64 f115
  .field public float64 f116
  .field public float64 f117
  .field public float64 f118
  .field public float64 f119
  .field public float64 f120
  .field public float64 f121
  .field public float64 f122
  .field public float64 f123
  .field public float64 f124
  .field public float64 f125
  .field public float64 f126
  .field public float64 f127
  .field public float64 f128
  .field public float64 f129
  .field public float64 f130
  .field public float64 f131
  .field public float64 f132
  .field public float64 f133
  .field public float64 f134
  .field public float64 f135
  .field public float64 f136
  .field public float64 f137
  .field public float64 f138
  .field public float64 f139
  .field public float64 f140
  .field public float64 f141
  .field public float64 f142
  .field public float64 f143
  .field public float64 f144
  .field public float64 f145
  .field public float64 f146
  .field public float64 f147
  .field public float64 f148
  .field public float64 f149
  .field public float64 f150
  .field public float64 f151
  .field public float64 f152
  .field public float64 f153
  .field public float64 f154
  .field public float64 f155
  .field public float64 f156
  .field public float64 f157
  .field public float64 f158
  .field public float64 f159
  .field public float64 f160
  .field public float64 f161
  .field public float64 f162
  .field public float64 f163
  .field public float64 f164
  .field public float64 f165
  .field public float64 f166
  .field public float64 f167
  .field public float64 f168
  .field public float64 f169
  .field public float64 f170
  .field public float64 f171
  .field public float64 f172
  .field public float64 f173
  .field public float64 f174
  .field public float64 f175
  .field public float64 f176
  .field public float64 f177
  .field public float64 f178
  .field public float64 f179
  .field public float64 f180
  .field public float64 f181
  .field public float64 f182
  .field public float64 f183
  .field public float64 f184
  .field public float64 f185
  .field public float64 f186
  .field public float64 f187
  .field public float64 f188
  .field public float64 f189
  .field public float64 f190
  .field public float64 f191
  .field public float64 f192
  .field public float64 f193
  .field public float64 f194
  .field public float64 f195
  .field public float64 f196
  .field public float64 f197
  .field public float64 f198
  .field public float64 f199
  .field public float64 f200
  .field public float64 f201
  .field public float64 f202
  .field public float64 f203
  .field public float64 f204
  .field public float64 f205
  .field public float64 f206
  .field public float64 f207
  .field public float64 f208
  .field public float64 f209
  .field public float64 f210
  .field public float64 f211
  .field public float64 f212
  .field public float64 f213
  .field public float64 f214
  .field public float64 f215
  .field public float64 f216
  .field public float64 f217
  .field public float64 f218
  .field public float64 f219
  .field public float64 f220
  .field public float64 f221
  .field public float64 f222
  .field public float64 f223
  .field public float64 f224
  .field public float64 f225
  .field public float64 f226
  .field public float64 f227
  .field public float64 f228
  .field public float64 f229
  .field public float64 f230
  .field public float64 f231
  .field public float64 f232
  .field public float64 f233
  .field public float64 f234
  .field public float64 f235
  .field public float64 f236
  .field public float64 f237
  .field public float64 f238
  .field public float64 f239
  .field public float64 f240
  .field public float64 f241
  .field public float64 f242
  .field public float64 f243
  .field public float64 f244
  .field public float64 f245
  .field public float64 f246
  .field public float64 f247
  .field public float64 f248
  .field public float64 f249
  .field public float64 f250
  .field public float64 f251
  .field public float64 f252
  .field public float64 f253
  .field public float64 f254
  .field public float64 f255
  .field public float64 f256
  .field public float64 f257
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       2058 (0x80a)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  conv.r8
    IL_0004:  stfld      float64 MCCTest.VType7::f1
    IL_0009:  ldarg.0
    IL_000a:  ldarg.1
    IL_000b:  conv.r8
    IL_000c:  stfld      float64 MCCTest.VType7::f2
    IL_0011:  ldarg.0
    IL_0012:  ldarg.1
    IL_0013:  conv.r8
    IL_0014:  stfld      float64 MCCTest.VType7::f3
    IL_0019:  ldarg.0
    IL_001a:  ldarg.1
    IL_001b:  conv.r8
    IL_001c:  stfld      float64 MCCTest.VType7::f4
    IL_0021:  ldarg.0
    IL_0022:  ldarg.1
    IL_0023:  conv.r8
    IL_0024:  stfld      float64 MCCTest.VType7::f5
    IL_0029:  ldarg.0
    IL_002a:  ldarg.1
    IL_002b:  conv.r8
    IL_002c:  stfld      float64 MCCTest.VType7::f6
    IL_0031:  ldarg.0
    IL_0032:  ldarg.1
    IL_0033:  conv.r8
    IL_0034:  stfld      float64 MCCTest.VType7::f7
    IL_0039:  ldarg.0
    IL_003a:  ldarg.1
    IL_003b:  conv.r8
    IL_003c:  stfld      float64 MCCTest.VType7::f8
    IL_0041:  ldarg.0
    IL_0042:  ldarg.1
    IL_0043:  conv.r8
    IL_0044:  stfld      float64 MCCTest.VType7::f9
    IL_0049:  ldarg.0
    IL_004a:  ldarg.1
    IL_004b:  conv.r8
    IL_004c:  stfld      float64 MCCTest.VType7::f10
    IL_0051:  ldarg.0
    IL_0052:  ldarg.1
    IL_0053:  conv.r8
    IL_0054:  stfld      float64 MCCTest.VType7::f11
    IL_0059:  ldarg.0
    IL_005a:  ldarg.1
    IL_005b:  conv.r8
    IL_005c:  stfld      float64 MCCTest.VType7::f12
    IL_0061:  ldarg.0
    IL_0062:  ldarg.1
    IL_0063:  conv.r8
    IL_0064:  stfld      float64 MCCTest.VType7::f13
    IL_0069:  ldarg.0
    IL_006a:  ldarg.1
    IL_006b:  conv.r8
    IL_006c:  stfld      float64 MCCTest.VType7::f14
    IL_0071:  ldarg.0
    IL_0072:  ldarg.1
    IL_0073:  conv.r8
    IL_0074:  stfld      float64 MCCTest.VType7::f15
    IL_0079:  ldarg.0
    IL_007a:  ldarg.1
    IL_007b:  conv.r8
    IL_007c:  stfld      float64 MCCTest.VType7::f16
    IL_0081:  ldarg.0
    IL_0082:  ldarg.1
    IL_0083:  conv.r8
    IL_0084:  stfld      float64 MCCTest.VType7::f17
    IL_0089:  ldarg.0
    IL_008a:  ldarg.1
    IL_008b:  conv.r8
    IL_008c:  stfld      float64 MCCTest.VType7::f18
    IL_0091:  ldarg.0
    IL_0092:  ldarg.1
    IL_0093:  conv.r8
    IL_0094:  stfld      float64 MCCTest.VType7::f19
    IL_0099:  ldarg.0
    IL_009a:  ldarg.1
    IL_009b:  conv.r8
    IL_009c:  stfld      float64 MCCTest.VType7::f20
    IL_00a1:  ldarg.0
    IL_00a2:  ldarg.1
    IL_00a3:  conv.r8
    IL_00a4:  stfld      float64 MCCTest.VType7::f21
    IL_00a9:  ldarg.0
    IL_00aa:  ldarg.1
    IL_00ab:  conv.r8
    IL_00ac:  stfld      float64 MCCTest.VType7::f22
    IL_00b1:  ldarg.0
    IL_00b2:  ldarg.1
    IL_00b3:  conv.r8
    IL_00b4:  stfld      float64 MCCTest.VType7::f23
    IL_00b9:  ldarg.0
    IL_00ba:  ldarg.1
    IL_00bb:  conv.r8
    IL_00bc:  stfld      float64 MCCTest.VType7::f24
    IL_00c1:  ldarg.0
    IL_00c2:  ldarg.1
    IL_00c3:  conv.r8
    IL_00c4:  stfld      float64 MCCTest.VType7::f25
    IL_00c9:  ldarg.0
    IL_00ca:  ldarg.1
    IL_00cb:  conv.r8
    IL_00cc:  stfld      float64 MCCTest.VType7::f26
    IL_00d1:  ldarg.0
    IL_00d2:  ldarg.1
    IL_00d3:  conv.r8
    IL_00d4:  stfld      float64 MCCTest.VType7::f27
    IL_00d9:  ldarg.0
    IL_00da:  ldarg.1
    IL_00db:  conv.r8
    IL_00dc:  stfld      float64 MCCTest.VType7::f28
    IL_00e1:  ldarg.0
    IL_00e2:  ldarg.1
    IL_00e3:  conv.r8
    IL_00e4:  stfld      float64 MCCTest.VType7::f29
    IL_00e9:  ldarg.0
    IL_00ea:  ldarg.1
    IL_00eb:  conv.r8
    IL_00ec:  stfld      float64 MCCTest.VType7::f30
    IL_00f1:  ldarg.0
    IL_00f2:  ldarg.1
    IL_00f3:  conv.r8
    IL_00f4:  stfld      float64 MCCTest.VType7::f31
    IL_00f9:  ldarg.0
    IL_00fa:  ldarg.1
    IL_00fb:  conv.r8
    IL_00fc:  stfld      float64 MCCTest.VType7::f32
    IL_0101:  ldarg.0
    IL_0102:  ldarg.1
    IL_0103:  conv.r8
    IL_0104:  stfld      float64 MCCTest.VType7::f33
    IL_0109:  ldarg.0
    IL_010a:  ldarg.1
    IL_010b:  conv.r8
    IL_010c:  stfld      float64 MCCTest.VType7::f34
    IL_0111:  ldarg.0
    IL_0112:  ldarg.1
    IL_0113:  conv.r8
    IL_0114:  stfld      float64 MCCTest.VType7::f35
    IL_0119:  ldarg.0
    IL_011a:  ldarg.1
    IL_011b:  conv.r8
    IL_011c:  stfld      float64 MCCTest.VType7::f36
    IL_0121:  ldarg.0
    IL_0122:  ldarg.1
    IL_0123:  conv.r8
    IL_0124:  stfld      float64 MCCTest.VType7::f37
    IL_0129:  ldarg.0
    IL_012a:  ldarg.1
    IL_012b:  conv.r8
    IL_012c:  stfld      float64 MCCTest.VType7::f38
    IL_0131:  ldarg.0
    IL_0132:  ldarg.1
    IL_0133:  conv.r8
    IL_0134:  stfld      float64 MCCTest.VType7::f39
    IL_0139:  ldarg.0
    IL_013a:  ldarg.1
    IL_013b:  conv.r8
    IL_013c:  stfld      float64 MCCTest.VType7::f40
    IL_0141:  ldarg.0
    IL_0142:  ldarg.1
    IL_0143:  conv.r8
    IL_0144:  stfld      float64 MCCTest.VType7::f41
    IL_0149:  ldarg.0
    IL_014a:  ldarg.1
    IL_014b:  conv.r8
    IL_014c:  stfld      float64 MCCTest.VType7::f42
    IL_0151:  ldarg.0
    IL_0152:  ldarg.1
    IL_0153:  conv.r8
    IL_0154:  stfld      float64 MCCTest.VType7::f43
    IL_0159:  ldarg.0
    IL_015a:  ldarg.1
    IL_015b:  conv.r8
    IL_015c:  stfld      float64 MCCTest.VType7::f44
    IL_0161:  ldarg.0
    IL_0162:  ldarg.1
    IL_0163:  conv.r8
    IL_0164:  stfld      float64 MCCTest.VType7::f45
    IL_0169:  ldarg.0
    IL_016a:  ldarg.1
    IL_016b:  conv.r8
    IL_016c:  stfld      float64 MCCTest.VType7::f46
    IL_0171:  ldarg.0
    IL_0172:  ldarg.1
    IL_0173:  conv.r8
    IL_0174:  stfld      float64 MCCTest.VType7::f47
    IL_0179:  ldarg.0
    IL_017a:  ldarg.1
    IL_017b:  conv.r8
    IL_017c:  stfld      float64 MCCTest.VType7::f48
    IL_0181:  ldarg.0
    IL_0182:  ldarg.1
    IL_0183:  conv.r8
    IL_0184:  stfld      float64 MCCTest.VType7::f49
    IL_0189:  ldarg.0
    IL_018a:  ldarg.1
    IL_018b:  conv.r8
    IL_018c:  stfld      float64 MCCTest.VType7::f50
    IL_0191:  ldarg.0
    IL_0192:  ldarg.1
    IL_0193:  conv.r8
    IL_0194:  stfld      float64 MCCTest.VType7::f51
    IL_0199:  ldarg.0
    IL_019a:  ldarg.1
    IL_019b:  conv.r8
    IL_019c:  stfld      float64 MCCTest.VType7::f52
    IL_01a1:  ldarg.0
    IL_01a2:  ldarg.1
    IL_01a3:  conv.r8
    IL_01a4:  stfld      float64 MCCTest.VType7::f53
    IL_01a9:  ldarg.0
    IL_01aa:  ldarg.1
    IL_01ab:  conv.r8
    IL_01ac:  stfld      float64 MCCTest.VType7::f54
    IL_01b1:  ldarg.0
    IL_01b2:  ldarg.1
    IL_01b3:  conv.r8
    IL_01b4:  stfld      float64 MCCTest.VType7::f55
    IL_01b9:  ldarg.0
    IL_01ba:  ldarg.1
    IL_01bb:  conv.r8
    IL_01bc:  stfld      float64 MCCTest.VType7::f56
    IL_01c1:  ldarg.0
    IL_01c2:  ldarg.1
    IL_01c3:  conv.r8
    IL_01c4:  stfld      float64 MCCTest.VType7::f57
    IL_01c9:  ldarg.0
    IL_01ca:  ldarg.1
    IL_01cb:  conv.r8
    IL_01cc:  stfld      float64 MCCTest.VType7::f58
    IL_01d1:  ldarg.0
    IL_01d2:  ldarg.1
    IL_01d3:  conv.r8
    IL_01d4:  stfld      float64 MCCTest.VType7::f59
    IL_01d9:  ldarg.0
    IL_01da:  ldarg.1
    IL_01db:  conv.r8
    IL_01dc:  stfld      float64 MCCTest.VType7::f60
    IL_01e1:  ldarg.0
    IL_01e2:  ldarg.1
    IL_01e3:  conv.r8
    IL_01e4:  stfld      float64 MCCTest.VType7::f61
    IL_01e9:  ldarg.0
    IL_01ea:  ldarg.1
    IL_01eb:  conv.r8
    IL_01ec:  stfld      float64 MCCTest.VType7::f62
    IL_01f1:  ldarg.0
    IL_01f2:  ldarg.1
    IL_01f3:  conv.r8
    IL_01f4:  stfld      float64 MCCTest.VType7::f63
    IL_01f9:  ldarg.0
    IL_01fa:  ldarg.1
    IL_01fb:  conv.r8
    IL_01fc:  stfld      float64 MCCTest.VType7::f64
    IL_0201:  ldarg.0
    IL_0202:  ldarg.1
    IL_0203:  conv.r8
    IL_0204:  stfld      float64 MCCTest.VType7::f65
    IL_0209:  ldarg.0
    IL_020a:  ldarg.1
    IL_020b:  conv.r8
    IL_020c:  stfld      float64 MCCTest.VType7::f66
    IL_0211:  ldarg.0
    IL_0212:  ldarg.1
    IL_0213:  conv.r8
    IL_0214:  stfld      float64 MCCTest.VType7::f67
    IL_0219:  ldarg.0
    IL_021a:  ldarg.1
    IL_021b:  conv.r8
    IL_021c:  stfld      float64 MCCTest.VType7::f68
    IL_0221:  ldarg.0
    IL_0222:  ldarg.1
    IL_0223:  conv.r8
    IL_0224:  stfld      float64 MCCTest.VType7::f69
    IL_0229:  ldarg.0
    IL_022a:  ldarg.1
    IL_022b:  conv.r8
    IL_022c:  stfld      float64 MCCTest.VType7::f70
    IL_0231:  ldarg.0
    IL_0232:  ldarg.1
    IL_0233:  conv.r8
    IL_0234:  stfld      float64 MCCTest.VType7::f71
    IL_0239:  ldarg.0
    IL_023a:  ldarg.1
    IL_023b:  conv.r8
    IL_023c:  stfld      float64 MCCTest.VType7::f72
    IL_0241:  ldarg.0
    IL_0242:  ldarg.1
    IL_0243:  conv.r8
    IL_0244:  stfld      float64 MCCTest.VType7::f73
    IL_0249:  ldarg.0
    IL_024a:  ldarg.1
    IL_024b:  conv.r8
    IL_024c:  stfld      float64 MCCTest.VType7::f74
    IL_0251:  ldarg.0
    IL_0252:  ldarg.1
    IL_0253:  conv.r8
    IL_0254:  stfld      float64 MCCTest.VType7::f75
    IL_0259:  ldarg.0
    IL_025a:  ldarg.1
    IL_025b:  conv.r8
    IL_025c:  stfld      float64 MCCTest.VType7::f76
    IL_0261:  ldarg.0
    IL_0262:  ldarg.1
    IL_0263:  conv.r8
    IL_0264:  stfld      float64 MCCTest.VType7::f77
    IL_0269:  ldarg.0
    IL_026a:  ldarg.1
    IL_026b:  conv.r8
    IL_026c:  stfld      float64 MCCTest.VType7::f78
    IL_0271:  ldarg.0
    IL_0272:  ldarg.1
    IL_0273:  conv.r8
    IL_0274:  stfld      float64 MCCTest.VType7::f79
    IL_0279:  ldarg.0
    IL_027a:  ldarg.1
    IL_027b:  conv.r8
    IL_027c:  stfld      float64 MCCTest.VType7::f80
    IL_0281:  ldarg.0
    IL_0282:  ldarg.1
    IL_0283:  conv.r8
    IL_0284:  stfld      float64 MCCTest.VType7::f81
    IL_0289:  ldarg.0
    IL_028a:  ldarg.1
    IL_028b:  conv.r8
    IL_028c:  stfld      float64 MCCTest.VType7::f82
    IL_0291:  ldarg.0
    IL_0292:  ldarg.1
    IL_0293:  conv.r8
    IL_0294:  stfld      float64 MCCTest.VType7::f83
    IL_0299:  ldarg.0
    IL_029a:  ldarg.1
    IL_029b:  conv.r8
    IL_029c:  stfld      float64 MCCTest.VType7::f84
    IL_02a1:  ldarg.0
    IL_02a2:  ldarg.1
    IL_02a3:  conv.r8
    IL_02a4:  stfld      float64 MCCTest.VType7::f85
    IL_02a9:  ldarg.0
    IL_02aa:  ldarg.1
    IL_02ab:  conv.r8
    IL_02ac:  stfld      float64 MCCTest.VType7::f86
    IL_02b1:  ldarg.0
    IL_02b2:  ldarg.1
    IL_02b3:  conv.r8
    IL_02b4:  stfld      float64 MCCTest.VType7::f87
    IL_02b9:  ldarg.0
    IL_02ba:  ldarg.1
    IL_02bb:  conv.r8
    IL_02bc:  stfld      float64 MCCTest.VType7::f88
    IL_02c1:  ldarg.0
    IL_02c2:  ldarg.1
    IL_02c3:  conv.r8
    IL_02c4:  stfld      float64 MCCTest.VType7::f89
    IL_02c9:  ldarg.0
    IL_02ca:  ldarg.1
    IL_02cb:  conv.r8
    IL_02cc:  stfld      float64 MCCTest.VType7::f90
    IL_02d1:  ldarg.0
    IL_02d2:  ldarg.1
    IL_02d3:  conv.r8
    IL_02d4:  stfld      float64 MCCTest.VType7::f91
    IL_02d9:  ldarg.0
    IL_02da:  ldarg.1
    IL_02db:  conv.r8
    IL_02dc:  stfld      float64 MCCTest.VType7::f92
    IL_02e1:  ldarg.0
    IL_02e2:  ldarg.1
    IL_02e3:  conv.r8
    IL_02e4:  stfld      float64 MCCTest.VType7::f93
    IL_02e9:  ldarg.0
    IL_02ea:  ldarg.1
    IL_02eb:  conv.r8
    IL_02ec:  stfld      float64 MCCTest.VType7::f94
    IL_02f1:  ldarg.0
    IL_02f2:  ldarg.1
    IL_02f3:  conv.r8
    IL_02f4:  stfld      float64 MCCTest.VType7::f95
    IL_02f9:  ldarg.0
    IL_02fa:  ldarg.1
    IL_02fb:  conv.r8
    IL_02fc:  stfld      float64 MCCTest.VType7::f96
    IL_0301:  ldarg.0
    IL_0302:  ldarg.1
    IL_0303:  conv.r8
    IL_0304:  stfld      float64 MCCTest.VType7::f97
    IL_0309:  ldarg.0
    IL_030a:  ldarg.1
    IL_030b:  conv.r8
    IL_030c:  stfld      float64 MCCTest.VType7::f98
    IL_0311:  ldarg.0
    IL_0312:  ldarg.1
    IL_0313:  conv.r8
    IL_0314:  stfld      float64 MCCTest.VType7::f99
    IL_0319:  ldarg.0
    IL_031a:  ldarg.1
    IL_031b:  conv.r8
    IL_031c:  stfld      float64 MCCTest.VType7::f100
    IL_0321:  ldarg.0
    IL_0322:  ldarg.1
    IL_0323:  conv.r8
    IL_0324:  stfld      float64 MCCTest.VType7::f101
    IL_0329:  ldarg.0
    IL_032a:  ldarg.1
    IL_032b:  conv.r8
    IL_032c:  stfld      float64 MCCTest.VType7::f102
    IL_0331:  ldarg.0
    IL_0332:  ldarg.1
    IL_0333:  conv.r8
    IL_0334:  stfld      float64 MCCTest.VType7::f103
    IL_0339:  ldarg.0
    IL_033a:  ldarg.1
    IL_033b:  conv.r8
    IL_033c:  stfld      float64 MCCTest.VType7::f104
    IL_0341:  ldarg.0
    IL_0342:  ldarg.1
    IL_0343:  conv.r8
    IL_0344:  stfld      float64 MCCTest.VType7::f105
    IL_0349:  ldarg.0
    IL_034a:  ldarg.1
    IL_034b:  conv.r8
    IL_034c:  stfld      float64 MCCTest.VType7::f106
    IL_0351:  ldarg.0
    IL_0352:  ldarg.1
    IL_0353:  conv.r8
    IL_0354:  stfld      float64 MCCTest.VType7::f107
    IL_0359:  ldarg.0
    IL_035a:  ldarg.1
    IL_035b:  conv.r8
    IL_035c:  stfld      float64 MCCTest.VType7::f108
    IL_0361:  ldarg.0
    IL_0362:  ldarg.1
    IL_0363:  conv.r8
    IL_0364:  stfld      float64 MCCTest.VType7::f109
    IL_0369:  ldarg.0
    IL_036a:  ldarg.1
    IL_036b:  conv.r8
    IL_036c:  stfld      float64 MCCTest.VType7::f110
    IL_0371:  ldarg.0
    IL_0372:  ldarg.1
    IL_0373:  conv.r8
    IL_0374:  stfld      float64 MCCTest.VType7::f111
    IL_0379:  ldarg.0
    IL_037a:  ldarg.1
    IL_037b:  conv.r8
    IL_037c:  stfld      float64 MCCTest.VType7::f112
    IL_0381:  ldarg.0
    IL_0382:  ldarg.1
    IL_0383:  conv.r8
    IL_0384:  stfld      float64 MCCTest.VType7::f113
    IL_0389:  ldarg.0
    IL_038a:  ldarg.1
    IL_038b:  conv.r8
    IL_038c:  stfld      float64 MCCTest.VType7::f114
    IL_0391:  ldarg.0
    IL_0392:  ldarg.1
    IL_0393:  conv.r8
    IL_0394:  stfld      float64 MCCTest.VType7::f115
    IL_0399:  ldarg.0
    IL_039a:  ldarg.1
    IL_039b:  conv.r8
    IL_039c:  stfld      float64 MCCTest.VType7::f116
    IL_03a1:  ldarg.0
    IL_03a2:  ldarg.1
    IL_03a3:  conv.r8
    IL_03a4:  stfld      float64 MCCTest.VType7::f117
    IL_03a9:  ldarg.0
    IL_03aa:  ldarg.1
    IL_03ab:  conv.r8
    IL_03ac:  stfld      float64 MCCTest.VType7::f118
    IL_03b1:  ldarg.0
    IL_03b2:  ldarg.1
    IL_03b3:  conv.r8
    IL_03b4:  stfld      float64 MCCTest.VType7::f119
    IL_03b9:  ldarg.0
    IL_03ba:  ldarg.1
    IL_03bb:  conv.r8
    IL_03bc:  stfld      float64 MCCTest.VType7::f120
    IL_03c1:  ldarg.0
    IL_03c2:  ldarg.1
    IL_03c3:  conv.r8
    IL_03c4:  stfld      float64 MCCTest.VType7::f121
    IL_03c9:  ldarg.0
    IL_03ca:  ldarg.1
    IL_03cb:  conv.r8
    IL_03cc:  stfld      float64 MCCTest.VType7::f122
    IL_03d1:  ldarg.0
    IL_03d2:  ldarg.1
    IL_03d3:  conv.r8
    IL_03d4:  stfld      float64 MCCTest.VType7::f123
    IL_03d9:  ldarg.0
    IL_03da:  ldarg.1
    IL_03db:  conv.r8
    IL_03dc:  stfld      float64 MCCTest.VType7::f124
    IL_03e1:  ldarg.0
    IL_03e2:  ldarg.1
    IL_03e3:  conv.r8
    IL_03e4:  stfld      float64 MCCTest.VType7::f125
    IL_03e9:  ldarg.0
    IL_03ea:  ldarg.1
    IL_03eb:  conv.r8
    IL_03ec:  stfld      float64 MCCTest.VType7::f126
    IL_03f1:  ldarg.0
    IL_03f2:  ldarg.1
    IL_03f3:  conv.r8
    IL_03f4:  stfld      float64 MCCTest.VType7::f127
    IL_03f9:  ldarg.0
    IL_03fa:  ldarg.1
    IL_03fb:  conv.r8
    IL_03fc:  stfld      float64 MCCTest.VType7::f128
    IL_0401:  ldarg.0
    IL_0402:  ldarg.1
    IL_0403:  conv.r8
    IL_0404:  stfld      float64 MCCTest.VType7::f129
    IL_0409:  ldarg.0
    IL_040a:  ldarg.1
    IL_040b:  conv.r8
    IL_040c:  stfld      float64 MCCTest.VType7::f130
    IL_0411:  ldarg.0
    IL_0412:  ldarg.1
    IL_0413:  conv.r8
    IL_0414:  stfld      float64 MCCTest.VType7::f131
    IL_0419:  ldarg.0
    IL_041a:  ldarg.1
    IL_041b:  conv.r8
    IL_041c:  stfld      float64 MCCTest.VType7::f132
    IL_0421:  ldarg.0
    IL_0422:  ldarg.1
    IL_0423:  conv.r8
    IL_0424:  stfld      float64 MCCTest.VType7::f133
    IL_0429:  ldarg.0
    IL_042a:  ldarg.1
    IL_042b:  conv.r8
    IL_042c:  stfld      float64 MCCTest.VType7::f134
    IL_0431:  ldarg.0
    IL_0432:  ldarg.1
    IL_0433:  conv.r8
    IL_0434:  stfld      float64 MCCTest.VType7::f135
    IL_0439:  ldarg.0
    IL_043a:  ldarg.1
    IL_043b:  conv.r8
    IL_043c:  stfld      float64 MCCTest.VType7::f136
    IL_0441:  ldarg.0
    IL_0442:  ldarg.1
    IL_0443:  conv.r8
    IL_0444:  stfld      float64 MCCTest.VType7::f137
    IL_0449:  ldarg.0
    IL_044a:  ldarg.1
    IL_044b:  conv.r8
    IL_044c:  stfld      float64 MCCTest.VType7::f138
    IL_0451:  ldarg.0
    IL_0452:  ldarg.1
    IL_0453:  conv.r8
    IL_0454:  stfld      float64 MCCTest.VType7::f139
    IL_0459:  ldarg.0
    IL_045a:  ldarg.1
    IL_045b:  conv.r8
    IL_045c:  stfld      float64 MCCTest.VType7::f140
    IL_0461:  ldarg.0
    IL_0462:  ldarg.1
    IL_0463:  conv.r8
    IL_0464:  stfld      float64 MCCTest.VType7::f141
    IL_0469:  ldarg.0
    IL_046a:  ldarg.1
    IL_046b:  conv.r8
    IL_046c:  stfld      float64 MCCTest.VType7::f142
    IL_0471:  ldarg.0
    IL_0472:  ldarg.1
    IL_0473:  conv.r8
    IL_0474:  stfld      float64 MCCTest.VType7::f143
    IL_0479:  ldarg.0
    IL_047a:  ldarg.1
    IL_047b:  conv.r8
    IL_047c:  stfld      float64 MCCTest.VType7::f144
    IL_0481:  ldarg.0
    IL_0482:  ldarg.1
    IL_0483:  conv.r8
    IL_0484:  stfld      float64 MCCTest.VType7::f145
    IL_0489:  ldarg.0
    IL_048a:  ldarg.1
    IL_048b:  conv.r8
    IL_048c:  stfld      float64 MCCTest.VType7::f146
    IL_0491:  ldarg.0
    IL_0492:  ldarg.1
    IL_0493:  conv.r8
    IL_0494:  stfld      float64 MCCTest.VType7::f147
    IL_0499:  ldarg.0
    IL_049a:  ldarg.1
    IL_049b:  conv.r8
    IL_049c:  stfld      float64 MCCTest.VType7::f148
    IL_04a1:  ldarg.0
    IL_04a2:  ldarg.1
    IL_04a3:  conv.r8
    IL_04a4:  stfld      float64 MCCTest.VType7::f149
    IL_04a9:  ldarg.0
    IL_04aa:  ldarg.1
    IL_04ab:  conv.r8
    IL_04ac:  stfld      float64 MCCTest.VType7::f150
    IL_04b1:  ldarg.0
    IL_04b2:  ldarg.1
    IL_04b3:  conv.r8
    IL_04b4:  stfld      float64 MCCTest.VType7::f151
    IL_04b9:  ldarg.0
    IL_04ba:  ldarg.1
    IL_04bb:  conv.r8
    IL_04bc:  stfld      float64 MCCTest.VType7::f152
    IL_04c1:  ldarg.0
    IL_04c2:  ldarg.1
    IL_04c3:  conv.r8
    IL_04c4:  stfld      float64 MCCTest.VType7::f153
    IL_04c9:  ldarg.0
    IL_04ca:  ldarg.1
    IL_04cb:  conv.r8
    IL_04cc:  stfld      float64 MCCTest.VType7::f154
    IL_04d1:  ldarg.0
    IL_04d2:  ldarg.1
    IL_04d3:  conv.r8
    IL_04d4:  stfld      float64 MCCTest.VType7::f155
    IL_04d9:  ldarg.0
    IL_04da:  ldarg.1
    IL_04db:  conv.r8
    IL_04dc:  stfld      float64 MCCTest.VType7::f156
    IL_04e1:  ldarg.0
    IL_04e2:  ldarg.1
    IL_04e3:  conv.r8
    IL_04e4:  stfld      float64 MCCTest.VType7::f157
    IL_04e9:  ldarg.0
    IL_04ea:  ldarg.1
    IL_04eb:  conv.r8
    IL_04ec:  stfld      float64 MCCTest.VType7::f158
    IL_04f1:  ldarg.0
    IL_04f2:  ldarg.1
    IL_04f3:  conv.r8
    IL_04f4:  stfld      float64 MCCTest.VType7::f159
    IL_04f9:  ldarg.0
    IL_04fa:  ldarg.1
    IL_04fb:  conv.r8
    IL_04fc:  stfld      float64 MCCTest.VType7::f160
    IL_0501:  ldarg.0
    IL_0502:  ldarg.1
    IL_0503:  conv.r8
    IL_0504:  stfld      float64 MCCTest.VType7::f161
    IL_0509:  ldarg.0
    IL_050a:  ldarg.1
    IL_050b:  conv.r8
    IL_050c:  stfld      float64 MCCTest.VType7::f162
    IL_0511:  ldarg.0
    IL_0512:  ldarg.1
    IL_0513:  conv.r8
    IL_0514:  stfld      float64 MCCTest.VType7::f163
    IL_0519:  ldarg.0
    IL_051a:  ldarg.1
    IL_051b:  conv.r8
    IL_051c:  stfld      float64 MCCTest.VType7::f164
    IL_0521:  ldarg.0
    IL_0522:  ldarg.1
    IL_0523:  conv.r8
    IL_0524:  stfld      float64 MCCTest.VType7::f165
    IL_0529:  ldarg.0
    IL_052a:  ldarg.1
    IL_052b:  conv.r8
    IL_052c:  stfld      float64 MCCTest.VType7::f166
    IL_0531:  ldarg.0
    IL_0532:  ldarg.1
    IL_0533:  conv.r8
    IL_0534:  stfld      float64 MCCTest.VType7::f167
    IL_0539:  ldarg.0
    IL_053a:  ldarg.1
    IL_053b:  conv.r8
    IL_053c:  stfld      float64 MCCTest.VType7::f168
    IL_0541:  ldarg.0
    IL_0542:  ldarg.1
    IL_0543:  conv.r8
    IL_0544:  stfld      float64 MCCTest.VType7::f169
    IL_0549:  ldarg.0
    IL_054a:  ldarg.1
    IL_054b:  conv.r8
    IL_054c:  stfld      float64 MCCTest.VType7::f170
    IL_0551:  ldarg.0
    IL_0552:  ldarg.1
    IL_0553:  conv.r8
    IL_0554:  stfld      float64 MCCTest.VType7::f171
    IL_0559:  ldarg.0
    IL_055a:  ldarg.1
    IL_055b:  conv.r8
    IL_055c:  stfld      float64 MCCTest.VType7::f172
    IL_0561:  ldarg.0
    IL_0562:  ldarg.1
    IL_0563:  conv.r8
    IL_0564:  stfld      float64 MCCTest.VType7::f173
    IL_0569:  ldarg.0
    IL_056a:  ldarg.1
    IL_056b:  conv.r8
    IL_056c:  stfld      float64 MCCTest.VType7::f174
    IL_0571:  ldarg.0
    IL_0572:  ldarg.1
    IL_0573:  conv.r8
    IL_0574:  stfld      float64 MCCTest.VType7::f175
    IL_0579:  ldarg.0
    IL_057a:  ldarg.1
    IL_057b:  conv.r8
    IL_057c:  stfld      float64 MCCTest.VType7::f176
    IL_0581:  ldarg.0
    IL_0582:  ldarg.1
    IL_0583:  conv.r8
    IL_0584:  stfld      float64 MCCTest.VType7::f177
    IL_0589:  ldarg.0
    IL_058a:  ldarg.1
    IL_058b:  conv.r8
    IL_058c:  stfld      float64 MCCTest.VType7::f178
    IL_0591:  ldarg.0
    IL_0592:  ldarg.1
    IL_0593:  conv.r8
    IL_0594:  stfld      float64 MCCTest.VType7::f179
    IL_0599:  ldarg.0
    IL_059a:  ldarg.1
    IL_059b:  conv.r8
    IL_059c:  stfld      float64 MCCTest.VType7::f180
    IL_05a1:  ldarg.0
    IL_05a2:  ldarg.1
    IL_05a3:  conv.r8
    IL_05a4:  stfld      float64 MCCTest.VType7::f181
    IL_05a9:  ldarg.0
    IL_05aa:  ldarg.1
    IL_05ab:  conv.r8
    IL_05ac:  stfld      float64 MCCTest.VType7::f182
    IL_05b1:  ldarg.0
    IL_05b2:  ldarg.1
    IL_05b3:  conv.r8
    IL_05b4:  stfld      float64 MCCTest.VType7::f183
    IL_05b9:  ldarg.0
    IL_05ba:  ldarg.1
    IL_05bb:  conv.r8
    IL_05bc:  stfld      float64 MCCTest.VType7::f184
    IL_05c1:  ldarg.0
    IL_05c2:  ldarg.1
    IL_05c3:  conv.r8
    IL_05c4:  stfld      float64 MCCTest.VType7::f185
    IL_05c9:  ldarg.0
    IL_05ca:  ldarg.1
    IL_05cb:  conv.r8
    IL_05cc:  stfld      float64 MCCTest.VType7::f186
    IL_05d1:  ldarg.0
    IL_05d2:  ldarg.1
    IL_05d3:  conv.r8
    IL_05d4:  stfld      float64 MCCTest.VType7::f187
    IL_05d9:  ldarg.0
    IL_05da:  ldarg.1
    IL_05db:  conv.r8
    IL_05dc:  stfld      float64 MCCTest.VType7::f188
    IL_05e1:  ldarg.0
    IL_05e2:  ldarg.1
    IL_05e3:  conv.r8
    IL_05e4:  stfld      float64 MCCTest.VType7::f189
    IL_05e9:  ldarg.0
    IL_05ea:  ldarg.1
    IL_05eb:  conv.r8
    IL_05ec:  stfld      float64 MCCTest.VType7::f190
    IL_05f1:  ldarg.0
    IL_05f2:  ldarg.1
    IL_05f3:  conv.r8
    IL_05f4:  stfld      float64 MCCTest.VType7::f191
    IL_05f9:  ldarg.0
    IL_05fa:  ldarg.1
    IL_05fb:  conv.r8
    IL_05fc:  stfld      float64 MCCTest.VType7::f192
    IL_0601:  ldarg.0
    IL_0602:  ldarg.1
    IL_0603:  conv.r8
    IL_0604:  stfld      float64 MCCTest.VType7::f193
    IL_0609:  ldarg.0
    IL_060a:  ldarg.1
    IL_060b:  conv.r8
    IL_060c:  stfld      float64 MCCTest.VType7::f194
    IL_0611:  ldarg.0
    IL_0612:  ldarg.1
    IL_0613:  conv.r8
    IL_0614:  stfld      float64 MCCTest.VType7::f195
    IL_0619:  ldarg.0
    IL_061a:  ldarg.1
    IL_061b:  conv.r8
    IL_061c:  stfld      float64 MCCTest.VType7::f196
    IL_0621:  ldarg.0
    IL_0622:  ldarg.1
    IL_0623:  conv.r8
    IL_0624:  stfld      float64 MCCTest.VType7::f197
    IL_0629:  ldarg.0
    IL_062a:  ldarg.1
    IL_062b:  conv.r8
    IL_062c:  stfld      float64 MCCTest.VType7::f198
    IL_0631:  ldarg.0
    IL_0632:  ldarg.1
    IL_0633:  conv.r8
    IL_0634:  stfld      float64 MCCTest.VType7::f199
    IL_0639:  ldarg.0
    IL_063a:  ldarg.1
    IL_063b:  conv.r8
    IL_063c:  stfld      float64 MCCTest.VType7::f200
    IL_0641:  ldarg.0
    IL_0642:  ldarg.1
    IL_0643:  conv.r8
    IL_0644:  stfld      float64 MCCTest.VType7::f201
    IL_0649:  ldarg.0
    IL_064a:  ldarg.1
    IL_064b:  conv.r8
    IL_064c:  stfld      float64 MCCTest.VType7::f202
    IL_0651:  ldarg.0
    IL_0652:  ldarg.1
    IL_0653:  conv.r8
    IL_0654:  stfld      float64 MCCTest.VType7::f203
    IL_0659:  ldarg.0
    IL_065a:  ldarg.1
    IL_065b:  conv.r8
    IL_065c:  stfld      float64 MCCTest.VType7::f204
    IL_0661:  ldarg.0
    IL_0662:  ldarg.1
    IL_0663:  conv.r8
    IL_0664:  stfld      float64 MCCTest.VType7::f205
    IL_0669:  ldarg.0
    IL_066a:  ldarg.1
    IL_066b:  conv.r8
    IL_066c:  stfld      float64 MCCTest.VType7::f206
    IL_0671:  ldarg.0
    IL_0672:  ldarg.1
    IL_0673:  conv.r8
    IL_0674:  stfld      float64 MCCTest.VType7::f207
    IL_0679:  ldarg.0
    IL_067a:  ldarg.1
    IL_067b:  conv.r8
    IL_067c:  stfld      float64 MCCTest.VType7::f208
    IL_0681:  ldarg.0
    IL_0682:  ldarg.1
    IL_0683:  conv.r8
    IL_0684:  stfld      float64 MCCTest.VType7::f209
    IL_0689:  ldarg.0
    IL_068a:  ldarg.1
    IL_068b:  conv.r8
    IL_068c:  stfld      float64 MCCTest.VType7::f210
    IL_0691:  ldarg.0
    IL_0692:  ldarg.1
    IL_0693:  conv.r8
    IL_0694:  stfld      float64 MCCTest.VType7::f211
    IL_0699:  ldarg.0
    IL_069a:  ldarg.1
    IL_069b:  conv.r8
    IL_069c:  stfld      float64 MCCTest.VType7::f212
    IL_06a1:  ldarg.0
    IL_06a2:  ldarg.1
    IL_06a3:  conv.r8
    IL_06a4:  stfld      float64 MCCTest.VType7::f213
    IL_06a9:  ldarg.0
    IL_06aa:  ldarg.1
    IL_06ab:  conv.r8
    IL_06ac:  stfld      float64 MCCTest.VType7::f214
    IL_06b1:  ldarg.0
    IL_06b2:  ldarg.1
    IL_06b3:  conv.r8
    IL_06b4:  stfld      float64 MCCTest.VType7::f215
    IL_06b9:  ldarg.0
    IL_06ba:  ldarg.1
    IL_06bb:  conv.r8
    IL_06bc:  stfld      float64 MCCTest.VType7::f216
    IL_06c1:  ldarg.0
    IL_06c2:  ldarg.1
    IL_06c3:  conv.r8
    IL_06c4:  stfld      float64 MCCTest.VType7::f217
    IL_06c9:  ldarg.0
    IL_06ca:  ldarg.1
    IL_06cb:  conv.r8
    IL_06cc:  stfld      float64 MCCTest.VType7::f218
    IL_06d1:  ldarg.0
    IL_06d2:  ldarg.1
    IL_06d3:  conv.r8
    IL_06d4:  stfld      float64 MCCTest.VType7::f219
    IL_06d9:  ldarg.0
    IL_06da:  ldarg.1
    IL_06db:  conv.r8
    IL_06dc:  stfld      float64 MCCTest.VType7::f220
    IL_06e1:  ldarg.0
    IL_06e2:  ldarg.1
    IL_06e3:  conv.r8
    IL_06e4:  stfld      float64 MCCTest.VType7::f221
    IL_06e9:  ldarg.0
    IL_06ea:  ldarg.1
    IL_06eb:  conv.r8
    IL_06ec:  stfld      float64 MCCTest.VType7::f222
    IL_06f1:  ldarg.0
    IL_06f2:  ldarg.1
    IL_06f3:  conv.r8
    IL_06f4:  stfld      float64 MCCTest.VType7::f223
    IL_06f9:  ldarg.0
    IL_06fa:  ldarg.1
    IL_06fb:  conv.r8
    IL_06fc:  stfld      float64 MCCTest.VType7::f224
    IL_0701:  ldarg.0
    IL_0702:  ldarg.1
    IL_0703:  conv.r8
    IL_0704:  stfld      float64 MCCTest.VType7::f225
    IL_0709:  ldarg.0
    IL_070a:  ldarg.1
    IL_070b:  conv.r8
    IL_070c:  stfld      float64 MCCTest.VType7::f226
    IL_0711:  ldarg.0
    IL_0712:  ldarg.1
    IL_0713:  conv.r8
    IL_0714:  stfld      float64 MCCTest.VType7::f227
    IL_0719:  ldarg.0
    IL_071a:  ldarg.1
    IL_071b:  conv.r8
    IL_071c:  stfld      float64 MCCTest.VType7::f228
    IL_0721:  ldarg.0
    IL_0722:  ldarg.1
    IL_0723:  conv.r8
    IL_0724:  stfld      float64 MCCTest.VType7::f229
    IL_0729:  ldarg.0
    IL_072a:  ldarg.1
    IL_072b:  conv.r8
    IL_072c:  stfld      float64 MCCTest.VType7::f230
    IL_0731:  ldarg.0
    IL_0732:  ldarg.1
    IL_0733:  conv.r8
    IL_0734:  stfld      float64 MCCTest.VType7::f231
    IL_0739:  ldarg.0
    IL_073a:  ldarg.1
    IL_073b:  conv.r8
    IL_073c:  stfld      float64 MCCTest.VType7::f232
    IL_0741:  ldarg.0
    IL_0742:  ldarg.1
    IL_0743:  conv.r8
    IL_0744:  stfld      float64 MCCTest.VType7::f233
    IL_0749:  ldarg.0
    IL_074a:  ldarg.1
    IL_074b:  conv.r8
    IL_074c:  stfld      float64 MCCTest.VType7::f234
    IL_0751:  ldarg.0
    IL_0752:  ldarg.1
    IL_0753:  conv.r8
    IL_0754:  stfld      float64 MCCTest.VType7::f235
    IL_0759:  ldarg.0
    IL_075a:  ldarg.1
    IL_075b:  conv.r8
    IL_075c:  stfld      float64 MCCTest.VType7::f236
    IL_0761:  ldarg.0
    IL_0762:  ldarg.1
    IL_0763:  conv.r8
    IL_0764:  stfld      float64 MCCTest.VType7::f237
    IL_0769:  ldarg.0
    IL_076a:  ldarg.1
    IL_076b:  conv.r8
    IL_076c:  stfld      float64 MCCTest.VType7::f238
    IL_0771:  ldarg.0
    IL_0772:  ldarg.1
    IL_0773:  conv.r8
    IL_0774:  stfld      float64 MCCTest.VType7::f239
    IL_0779:  ldarg.0
    IL_077a:  ldarg.1
    IL_077b:  conv.r8
    IL_077c:  stfld      float64 MCCTest.VType7::f240
    IL_0781:  ldarg.0
    IL_0782:  ldarg.1
    IL_0783:  conv.r8
    IL_0784:  stfld      float64 MCCTest.VType7::f241
    IL_0789:  ldarg.0
    IL_078a:  ldarg.1
    IL_078b:  conv.r8
    IL_078c:  stfld      float64 MCCTest.VType7::f242
    IL_0791:  ldarg.0
    IL_0792:  ldarg.1
    IL_0793:  conv.r8
    IL_0794:  stfld      float64 MCCTest.VType7::f243
    IL_0799:  ldarg.0
    IL_079a:  ldarg.1
    IL_079b:  conv.r8
    IL_079c:  stfld      float64 MCCTest.VType7::f244
    IL_07a1:  ldarg.0
    IL_07a2:  ldarg.1
    IL_07a3:  conv.r8
    IL_07a4:  stfld      float64 MCCTest.VType7::f245
    IL_07a9:  ldarg.0
    IL_07aa:  ldarg.1
    IL_07ab:  conv.r8
    IL_07ac:  stfld      float64 MCCTest.VType7::f246
    IL_07b1:  ldarg.0
    IL_07b2:  ldarg.1
    IL_07b3:  conv.r8
    IL_07b4:  stfld      float64 MCCTest.VType7::f247
    IL_07b9:  ldarg.0
    IL_07ba:  ldarg.1
    IL_07bb:  conv.r8
    IL_07bc:  stfld      float64 MCCTest.VType7::f248
    IL_07c1:  ldarg.0
    IL_07c2:  ldarg.1
    IL_07c3:  conv.r8
    IL_07c4:  stfld      float64 MCCTest.VType7::f249
    IL_07c9:  ldarg.0
    IL_07ca:  ldarg.1
    IL_07cb:  conv.r8
    IL_07cc:  stfld      float64 MCCTest.VType7::f250
    IL_07d1:  ldarg.0
    IL_07d2:  ldarg.1
    IL_07d3:  conv.r8
    IL_07d4:  stfld      float64 MCCTest.VType7::f251
    IL_07d9:  ldarg.0
    IL_07da:  ldarg.1
    IL_07db:  conv.r8
    IL_07dc:  stfld      float64 MCCTest.VType7::f252
    IL_07e1:  ldarg.0
    IL_07e2:  ldarg.1
    IL_07e3:  conv.r8
    IL_07e4:  stfld      float64 MCCTest.VType7::f253
    IL_07e9:  ldarg.0
    IL_07ea:  ldarg.1
    IL_07eb:  conv.r8
    IL_07ec:  stfld      float64 MCCTest.VType7::f254
    IL_07f1:  ldarg.0
    IL_07f2:  ldarg.1
    IL_07f3:  conv.r8
    IL_07f4:  stfld      float64 MCCTest.VType7::f255
    IL_07f9:  ldarg.0
    IL_07fa:  ldarg.1
    IL_07fb:  conv.r8
    IL_07fc:  stfld      float64 MCCTest.VType7::f256
    IL_0801:  ldarg.0
    IL_0802:  ldarg.1
    IL_0803:  conv.r8
    IL_0804:  stfld      float64 MCCTest.VType7::f257
    IL_0809:  ret
  } // end of method VType7::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VType7::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType7::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VType7::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType7::Zero

  .method public hidebysig instance void
          Add(valuetype MCCTest.VType7 val) cil managed
  {
    // Code size       5142 (0x1416)
    .maxstack  3
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      float64 MCCTest.VType7::f1
    IL_0008:  ldarga.s   val
    IL_000a:  ldfld      float64 MCCTest.VType7::f1
    IL_000f:  add
    IL_0010:  stfld      float64 MCCTest.VType7::f1
    IL_0015:  ldarg.0
    IL_0016:  dup
    IL_0017:  ldfld      float64 MCCTest.VType7::f2
    IL_001c:  ldarga.s   val
    IL_001e:  ldfld      float64 MCCTest.VType7::f2
    IL_0023:  add
    IL_0024:  stfld      float64 MCCTest.VType7::f2
    IL_0029:  ldarg.0
    IL_002a:  dup
    IL_002b:  ldfld      float64 MCCTest.VType7::f3
    IL_0030:  ldarga.s   val
    IL_0032:  ldfld      float64 MCCTest.VType7::f3
    IL_0037:  add
    IL_0038:  stfld      float64 MCCTest.VType7::f3
    IL_003d:  ldarg.0
    IL_003e:  dup
    IL_003f:  ldfld      float64 MCCTest.VType7::f4
    IL_0044:  ldarga.s   val
    IL_0046:  ldfld      float64 MCCTest.VType7::f4
    IL_004b:  add
    IL_004c:  stfld      float64 MCCTest.VType7::f4
    IL_0051:  ldarg.0
    IL_0052:  dup
    IL_0053:  ldfld      float64 MCCTest.VType7::f5
    IL_0058:  ldarga.s   val
    IL_005a:  ldfld      float64 MCCTest.VType7::f5
    IL_005f:  add
    IL_0060:  stfld      float64 MCCTest.VType7::f5
    IL_0065:  ldarg.0
    IL_0066:  dup
    IL_0067:  ldfld      float64 MCCTest.VType7::f6
    IL_006c:  ldarga.s   val
    IL_006e:  ldfld      float64 MCCTest.VType7::f6
    IL_0073:  add
    IL_0074:  stfld      float64 MCCTest.VType7::f6
    IL_0079:  ldarg.0
    IL_007a:  dup
    IL_007b:  ldfld      float64 MCCTest.VType7::f7
    IL_0080:  ldarga.s   val
    IL_0082:  ldfld      float64 MCCTest.VType7::f7
    IL_0087:  add
    IL_0088:  stfld      float64 MCCTest.VType7::f7
    IL_008d:  ldarg.0
    IL_008e:  dup
    IL_008f:  ldfld      float64 MCCTest.VType7::f8
    IL_0094:  ldarga.s   val
    IL_0096:  ldfld      float64 MCCTest.VType7::f8
    IL_009b:  add
    IL_009c:  stfld      float64 MCCTest.VType7::f8
    IL_00a1:  ldarg.0
    IL_00a2:  dup
    IL_00a3:  ldfld      float64 MCCTest.VType7::f9
    IL_00a8:  ldarga.s   val
    IL_00aa:  ldfld      float64 MCCTest.VType7::f9
    IL_00af:  add
    IL_00b0:  stfld      float64 MCCTest.VType7::f9
    IL_00b5:  ldarg.0
    IL_00b6:  dup
    IL_00b7:  ldfld      float64 MCCTest.VType7::f10
    IL_00bc:  ldarga.s   val
    IL_00be:  ldfld      float64 MCCTest.VType7::f10
    IL_00c3:  add
    IL_00c4:  stfld      float64 MCCTest.VType7::f10
    IL_00c9:  ldarg.0
    IL_00ca:  dup
    IL_00cb:  ldfld      float64 MCCTest.VType7::f11
    IL_00d0:  ldarga.s   val
    IL_00d2:  ldfld      float64 MCCTest.VType7::f11
    IL_00d7:  add
    IL_00d8:  stfld      float64 MCCTest.VType7::f11
    IL_00dd:  ldarg.0
    IL_00de:  dup
    IL_00df:  ldfld      float64 MCCTest.VType7::f12
    IL_00e4:  ldarga.s   val
    IL_00e6:  ldfld      float64 MCCTest.VType7::f12
    IL_00eb:  add
    IL_00ec:  stfld      float64 MCCTest.VType7::f12
    IL_00f1:  ldarg.0
    IL_00f2:  dup
    IL_00f3:  ldfld      float64 MCCTest.VType7::f13
    IL_00f8:  ldarga.s   val
    IL_00fa:  ldfld      float64 MCCTest.VType7::f13
    IL_00ff:  add
    IL_0100:  stfld      float64 MCCTest.VType7::f13
    IL_0105:  ldarg.0
    IL_0106:  dup
    IL_0107:  ldfld      float64 MCCTest.VType7::f14
    IL_010c:  ldarga.s   val
    IL_010e:  ldfld      float64 MCCTest.VType7::f14
    IL_0113:  add
    IL_0114:  stfld      float64 MCCTest.VType7::f14
    IL_0119:  ldarg.0
    IL_011a:  dup
    IL_011b:  ldfld      float64 MCCTest.VType7::f15
    IL_0120:  ldarga.s   val
    IL_0122:  ldfld      float64 MCCTest.VType7::f15
    IL_0127:  add
    IL_0128:  stfld      float64 MCCTest.VType7::f15
    IL_012d:  ldarg.0
    IL_012e:  dup
    IL_012f:  ldfld      float64 MCCTest.VType7::f16
    IL_0134:  ldarga.s   val
    IL_0136:  ldfld      float64 MCCTest.VType7::f16
    IL_013b:  add
    IL_013c:  stfld      float64 MCCTest.VType7::f16
    IL_0141:  ldarg.0
    IL_0142:  dup
    IL_0143:  ldfld      float64 MCCTest.VType7::f17
    IL_0148:  ldarga.s   val
    IL_014a:  ldfld      float64 MCCTest.VType7::f17
    IL_014f:  add
    IL_0150:  stfld      float64 MCCTest.VType7::f17
    IL_0155:  ldarg.0
    IL_0156:  dup
    IL_0157:  ldfld      float64 MCCTest.VType7::f18
    IL_015c:  ldarga.s   val
    IL_015e:  ldfld      float64 MCCTest.VType7::f18
    IL_0163:  add
    IL_0164:  stfld      float64 MCCTest.VType7::f18
    IL_0169:  ldarg.0
    IL_016a:  dup
    IL_016b:  ldfld      float64 MCCTest.VType7::f19
    IL_0170:  ldarga.s   val
    IL_0172:  ldfld      float64 MCCTest.VType7::f19
    IL_0177:  add
    IL_0178:  stfld      float64 MCCTest.VType7::f19
    IL_017d:  ldarg.0
    IL_017e:  dup
    IL_017f:  ldfld      float64 MCCTest.VType7::f20
    IL_0184:  ldarga.s   val
    IL_0186:  ldfld      float64 MCCTest.VType7::f20
    IL_018b:  add
    IL_018c:  stfld      float64 MCCTest.VType7::f20
    IL_0191:  ldarg.0
    IL_0192:  dup
    IL_0193:  ldfld      float64 MCCTest.VType7::f21
    IL_0198:  ldarga.s   val
    IL_019a:  ldfld      float64 MCCTest.VType7::f21
    IL_019f:  add
    IL_01a0:  stfld      float64 MCCTest.VType7::f21
    IL_01a5:  ldarg.0
    IL_01a6:  dup
    IL_01a7:  ldfld      float64 MCCTest.VType7::f22
    IL_01ac:  ldarga.s   val
    IL_01ae:  ldfld      float64 MCCTest.VType7::f22
    IL_01b3:  add
    IL_01b4:  stfld      float64 MCCTest.VType7::f22
    IL_01b9:  ldarg.0
    IL_01ba:  dup
    IL_01bb:  ldfld      float64 MCCTest.VType7::f23
    IL_01c0:  ldarga.s   val
    IL_01c2:  ldfld      float64 MCCTest.VType7::f23
    IL_01c7:  add
    IL_01c8:  stfld      float64 MCCTest.VType7::f23
    IL_01cd:  ldarg.0
    IL_01ce:  dup
    IL_01cf:  ldfld      float64 MCCTest.VType7::f24
    IL_01d4:  ldarga.s   val
    IL_01d6:  ldfld      float64 MCCTest.VType7::f24
    IL_01db:  add
    IL_01dc:  stfld      float64 MCCTest.VType7::f24
    IL_01e1:  ldarg.0
    IL_01e2:  dup
    IL_01e3:  ldfld      float64 MCCTest.VType7::f25
    IL_01e8:  ldarga.s   val
    IL_01ea:  ldfld      float64 MCCTest.VType7::f25
    IL_01ef:  add
    IL_01f0:  stfld      float64 MCCTest.VType7::f25
    IL_01f5:  ldarg.0
    IL_01f6:  dup
    IL_01f7:  ldfld      float64 MCCTest.VType7::f26
    IL_01fc:  ldarga.s   val
    IL_01fe:  ldfld      float64 MCCTest.VType7::f26
    IL_0203:  add
    IL_0204:  stfld      float64 MCCTest.VType7::f26
    IL_0209:  ldarg.0
    IL_020a:  dup
    IL_020b:  ldfld      float64 MCCTest.VType7::f27
    IL_0210:  ldarga.s   val
    IL_0212:  ldfld      float64 MCCTest.VType7::f27
    IL_0217:  add
    IL_0218:  stfld      float64 MCCTest.VType7::f27
    IL_021d:  ldarg.0
    IL_021e:  dup
    IL_021f:  ldfld      float64 MCCTest.VType7::f28
    IL_0224:  ldarga.s   val
    IL_0226:  ldfld      float64 MCCTest.VType7::f28
    IL_022b:  add
    IL_022c:  stfld      float64 MCCTest.VType7::f28
    IL_0231:  ldarg.0
    IL_0232:  dup
    IL_0233:  ldfld      float64 MCCTest.VType7::f29
    IL_0238:  ldarga.s   val
    IL_023a:  ldfld      float64 MCCTest.VType7::f29
    IL_023f:  add
    IL_0240:  stfld      float64 MCCTest.VType7::f29
    IL_0245:  ldarg.0
    IL_0246:  dup
    IL_0247:  ldfld      float64 MCCTest.VType7::f30
    IL_024c:  ldarga.s   val
    IL_024e:  ldfld      float64 MCCTest.VType7::f30
    IL_0253:  add
    IL_0254:  stfld      float64 MCCTest.VType7::f30
    IL_0259:  ldarg.0
    IL_025a:  dup
    IL_025b:  ldfld      float64 MCCTest.VType7::f31
    IL_0260:  ldarga.s   val
    IL_0262:  ldfld      float64 MCCTest.VType7::f31
    IL_0267:  add
    IL_0268:  stfld      float64 MCCTest.VType7::f31
    IL_026d:  ldarg.0
    IL_026e:  dup
    IL_026f:  ldfld      float64 MCCTest.VType7::f32
    IL_0274:  ldarga.s   val
    IL_0276:  ldfld      float64 MCCTest.VType7::f32
    IL_027b:  add
    IL_027c:  stfld      float64 MCCTest.VType7::f32
    IL_0281:  ldarg.0
    IL_0282:  dup
    IL_0283:  ldfld      float64 MCCTest.VType7::f33
    IL_0288:  ldarga.s   val
    IL_028a:  ldfld      float64 MCCTest.VType7::f33
    IL_028f:  add
    IL_0290:  stfld      float64 MCCTest.VType7::f33
    IL_0295:  ldarg.0
    IL_0296:  dup
    IL_0297:  ldfld      float64 MCCTest.VType7::f34
    IL_029c:  ldarga.s   val
    IL_029e:  ldfld      float64 MCCTest.VType7::f34
    IL_02a3:  add
    IL_02a4:  stfld      float64 MCCTest.VType7::f34
    IL_02a9:  ldarg.0
    IL_02aa:  dup
    IL_02ab:  ldfld      float64 MCCTest.VType7::f35
    IL_02b0:  ldarga.s   val
    IL_02b2:  ldfld      float64 MCCTest.VType7::f35
    IL_02b7:  add
    IL_02b8:  stfld      float64 MCCTest.VType7::f35
    IL_02bd:  ldarg.0
    IL_02be:  dup
    IL_02bf:  ldfld      float64 MCCTest.VType7::f36
    IL_02c4:  ldarga.s   val
    IL_02c6:  ldfld      float64 MCCTest.VType7::f36
    IL_02cb:  add
    IL_02cc:  stfld      float64 MCCTest.VType7::f36
    IL_02d1:  ldarg.0
    IL_02d2:  dup
    IL_02d3:  ldfld      float64 MCCTest.VType7::f37
    IL_02d8:  ldarga.s   val
    IL_02da:  ldfld      float64 MCCTest.VType7::f37
    IL_02df:  add
    IL_02e0:  stfld      float64 MCCTest.VType7::f37
    IL_02e5:  ldarg.0
    IL_02e6:  dup
    IL_02e7:  ldfld      float64 MCCTest.VType7::f38
    IL_02ec:  ldarga.s   val
    IL_02ee:  ldfld      float64 MCCTest.VType7::f38
    IL_02f3:  add
    IL_02f4:  stfld      float64 MCCTest.VType7::f38
    IL_02f9:  ldarg.0
    IL_02fa:  dup
    IL_02fb:  ldfld      float64 MCCTest.VType7::f39
    IL_0300:  ldarga.s   val
    IL_0302:  ldfld      float64 MCCTest.VType7::f39
    IL_0307:  add
    IL_0308:  stfld      float64 MCCTest.VType7::f39
    IL_030d:  ldarg.0
    IL_030e:  dup
    IL_030f:  ldfld      float64 MCCTest.VType7::f40
    IL_0314:  ldarga.s   val
    IL_0316:  ldfld      float64 MCCTest.VType7::f40
    IL_031b:  add
    IL_031c:  stfld      float64 MCCTest.VType7::f40
    IL_0321:  ldarg.0
    IL_0322:  dup
    IL_0323:  ldfld      float64 MCCTest.VType7::f41
    IL_0328:  ldarga.s   val
    IL_032a:  ldfld      float64 MCCTest.VType7::f41
    IL_032f:  add
    IL_0330:  stfld      float64 MCCTest.VType7::f41
    IL_0335:  ldarg.0
    IL_0336:  dup
    IL_0337:  ldfld      float64 MCCTest.VType7::f42
    IL_033c:  ldarga.s   val
    IL_033e:  ldfld      float64 MCCTest.VType7::f42
    IL_0343:  add
    IL_0344:  stfld      float64 MCCTest.VType7::f42
    IL_0349:  ldarg.0
    IL_034a:  dup
    IL_034b:  ldfld      float64 MCCTest.VType7::f43
    IL_0350:  ldarga.s   val
    IL_0352:  ldfld      float64 MCCTest.VType7::f43
    IL_0357:  add
    IL_0358:  stfld      float64 MCCTest.VType7::f43
    IL_035d:  ldarg.0
    IL_035e:  dup
    IL_035f:  ldfld      float64 MCCTest.VType7::f44
    IL_0364:  ldarga.s   val
    IL_0366:  ldfld      float64 MCCTest.VType7::f44
    IL_036b:  add
    IL_036c:  stfld      float64 MCCTest.VType7::f44
    IL_0371:  ldarg.0
    IL_0372:  dup
    IL_0373:  ldfld      float64 MCCTest.VType7::f45
    IL_0378:  ldarga.s   val
    IL_037a:  ldfld      float64 MCCTest.VType7::f45
    IL_037f:  add
    IL_0380:  stfld      float64 MCCTest.VType7::f45
    IL_0385:  ldarg.0
    IL_0386:  dup
    IL_0387:  ldfld      float64 MCCTest.VType7::f46
    IL_038c:  ldarga.s   val
    IL_038e:  ldfld      float64 MCCTest.VType7::f46
    IL_0393:  add
    IL_0394:  stfld      float64 MCCTest.VType7::f46
    IL_0399:  ldarg.0
    IL_039a:  dup
    IL_039b:  ldfld      float64 MCCTest.VType7::f47
    IL_03a0:  ldarga.s   val
    IL_03a2:  ldfld      float64 MCCTest.VType7::f47
    IL_03a7:  add
    IL_03a8:  stfld      float64 MCCTest.VType7::f47
    IL_03ad:  ldarg.0
    IL_03ae:  dup
    IL_03af:  ldfld      float64 MCCTest.VType7::f48
    IL_03b4:  ldarga.s   val
    IL_03b6:  ldfld      float64 MCCTest.VType7::f48
    IL_03bb:  add
    IL_03bc:  stfld      float64 MCCTest.VType7::f48
    IL_03c1:  ldarg.0
    IL_03c2:  dup
    IL_03c3:  ldfld      float64 MCCTest.VType7::f49
    IL_03c8:  ldarga.s   val
    IL_03ca:  ldfld      float64 MCCTest.VType7::f49
    IL_03cf:  add
    IL_03d0:  stfld      float64 MCCTest.VType7::f49
    IL_03d5:  ldarg.0
    IL_03d6:  dup
    IL_03d7:  ldfld      float64 MCCTest.VType7::f50
    IL_03dc:  ldarga.s   val
    IL_03de:  ldfld      float64 MCCTest.VType7::f50
    IL_03e3:  add
    IL_03e4:  stfld      float64 MCCTest.VType7::f50
    IL_03e9:  ldarg.0
    IL_03ea:  dup
    IL_03eb:  ldfld      float64 MCCTest.VType7::f51
    IL_03f0:  ldarga.s   val
    IL_03f2:  ldfld      float64 MCCTest.VType7::f51
    IL_03f7:  add
    IL_03f8:  stfld      float64 MCCTest.VType7::f51
    IL_03fd:  ldarg.0
    IL_03fe:  dup
    IL_03ff:  ldfld      float64 MCCTest.VType7::f52
    IL_0404:  ldarga.s   val
    IL_0406:  ldfld      float64 MCCTest.VType7::f52
    IL_040b:  add
    IL_040c:  stfld      float64 MCCTest.VType7::f52
    IL_0411:  ldarg.0
    IL_0412:  dup
    IL_0413:  ldfld      float64 MCCTest.VType7::f53
    IL_0418:  ldarga.s   val
    IL_041a:  ldfld      float64 MCCTest.VType7::f53
    IL_041f:  add
    IL_0420:  stfld      float64 MCCTest.VType7::f53
    IL_0425:  ldarg.0
    IL_0426:  dup
    IL_0427:  ldfld      float64 MCCTest.VType7::f54
    IL_042c:  ldarga.s   val
    IL_042e:  ldfld      float64 MCCTest.VType7::f54
    IL_0433:  add
    IL_0434:  stfld      float64 MCCTest.VType7::f54
    IL_0439:  ldarg.0
    IL_043a:  dup
    IL_043b:  ldfld      float64 MCCTest.VType7::f55
    IL_0440:  ldarga.s   val
    IL_0442:  ldfld      float64 MCCTest.VType7::f55
    IL_0447:  add
    IL_0448:  stfld      float64 MCCTest.VType7::f55
    IL_044d:  ldarg.0
    IL_044e:  dup
    IL_044f:  ldfld      float64 MCCTest.VType7::f56
    IL_0454:  ldarga.s   val
    IL_0456:  ldfld      float64 MCCTest.VType7::f56
    IL_045b:  add
    IL_045c:  stfld      float64 MCCTest.VType7::f56
    IL_0461:  ldarg.0
    IL_0462:  dup
    IL_0463:  ldfld      float64 MCCTest.VType7::f57
    IL_0468:  ldarga.s   val
    IL_046a:  ldfld      float64 MCCTest.VType7::f57
    IL_046f:  add
    IL_0470:  stfld      float64 MCCTest.VType7::f57
    IL_0475:  ldarg.0
    IL_0476:  dup
    IL_0477:  ldfld      float64 MCCTest.VType7::f58
    IL_047c:  ldarga.s   val
    IL_047e:  ldfld      float64 MCCTest.VType7::f58
    IL_0483:  add
    IL_0484:  stfld      float64 MCCTest.VType7::f58
    IL_0489:  ldarg.0
    IL_048a:  dup
    IL_048b:  ldfld      float64 MCCTest.VType7::f59
    IL_0490:  ldarga.s   val
    IL_0492:  ldfld      float64 MCCTest.VType7::f59
    IL_0497:  add
    IL_0498:  stfld      float64 MCCTest.VType7::f59
    IL_049d:  ldarg.0
    IL_049e:  dup
    IL_049f:  ldfld      float64 MCCTest.VType7::f60
    IL_04a4:  ldarga.s   val
    IL_04a6:  ldfld      float64 MCCTest.VType7::f60
    IL_04ab:  add
    IL_04ac:  stfld      float64 MCCTest.VType7::f60
    IL_04b1:  ldarg.0
    IL_04b2:  dup
    IL_04b3:  ldfld      float64 MCCTest.VType7::f61
    IL_04b8:  ldarga.s   val
    IL_04ba:  ldfld      float64 MCCTest.VType7::f61
    IL_04bf:  add
    IL_04c0:  stfld      float64 MCCTest.VType7::f61
    IL_04c5:  ldarg.0
    IL_04c6:  dup
    IL_04c7:  ldfld      float64 MCCTest.VType7::f62
    IL_04cc:  ldarga.s   val
    IL_04ce:  ldfld      float64 MCCTest.VType7::f62
    IL_04d3:  add
    IL_04d4:  stfld      float64 MCCTest.VType7::f62
    IL_04d9:  ldarg.0
    IL_04da:  dup
    IL_04db:  ldfld      float64 MCCTest.VType7::f63
    IL_04e0:  ldarga.s   val
    IL_04e2:  ldfld      float64 MCCTest.VType7::f63
    IL_04e7:  add
    IL_04e8:  stfld      float64 MCCTest.VType7::f63
    IL_04ed:  ldarg.0
    IL_04ee:  dup
    IL_04ef:  ldfld      float64 MCCTest.VType7::f64
    IL_04f4:  ldarga.s   val
    IL_04f6:  ldfld      float64 MCCTest.VType7::f64
    IL_04fb:  add
    IL_04fc:  stfld      float64 MCCTest.VType7::f64
    IL_0501:  ldarg.0
    IL_0502:  dup
    IL_0503:  ldfld      float64 MCCTest.VType7::f65
    IL_0508:  ldarga.s   val
    IL_050a:  ldfld      float64 MCCTest.VType7::f65
    IL_050f:  add
    IL_0510:  stfld      float64 MCCTest.VType7::f65
    IL_0515:  ldarg.0
    IL_0516:  dup
    IL_0517:  ldfld      float64 MCCTest.VType7::f66
    IL_051c:  ldarga.s   val
    IL_051e:  ldfld      float64 MCCTest.VType7::f66
    IL_0523:  add
    IL_0524:  stfld      float64 MCCTest.VType7::f66
    IL_0529:  ldarg.0
    IL_052a:  dup
    IL_052b:  ldfld      float64 MCCTest.VType7::f67
    IL_0530:  ldarga.s   val
    IL_0532:  ldfld      float64 MCCTest.VType7::f67
    IL_0537:  add
    IL_0538:  stfld      float64 MCCTest.VType7::f67
    IL_053d:  ldarg.0
    IL_053e:  dup
    IL_053f:  ldfld      float64 MCCTest.VType7::f68
    IL_0544:  ldarga.s   val
    IL_0546:  ldfld      float64 MCCTest.VType7::f68
    IL_054b:  add
    IL_054c:  stfld      float64 MCCTest.VType7::f68
    IL_0551:  ldarg.0
    IL_0552:  dup
    IL_0553:  ldfld      float64 MCCTest.VType7::f69
    IL_0558:  ldarga.s   val
    IL_055a:  ldfld      float64 MCCTest.VType7::f69
    IL_055f:  add
    IL_0560:  stfld      float64 MCCTest.VType7::f69
    IL_0565:  ldarg.0
    IL_0566:  dup
    IL_0567:  ldfld      float64 MCCTest.VType7::f70
    IL_056c:  ldarga.s   val
    IL_056e:  ldfld      float64 MCCTest.VType7::f70
    IL_0573:  add
    IL_0574:  stfld      float64 MCCTest.VType7::f70
    IL_0579:  ldarg.0
    IL_057a:  dup
    IL_057b:  ldfld      float64 MCCTest.VType7::f71
    IL_0580:  ldarga.s   val
    IL_0582:  ldfld      float64 MCCTest.VType7::f71
    IL_0587:  add
    IL_0588:  stfld      float64 MCCTest.VType7::f71
    IL_058d:  ldarg.0
    IL_058e:  dup
    IL_058f:  ldfld      float64 MCCTest.VType7::f72
    IL_0594:  ldarga.s   val
    IL_0596:  ldfld      float64 MCCTest.VType7::f72
    IL_059b:  add
    IL_059c:  stfld      float64 MCCTest.VType7::f72
    IL_05a1:  ldarg.0
    IL_05a2:  dup
    IL_05a3:  ldfld      float64 MCCTest.VType7::f73
    IL_05a8:  ldarga.s   val
    IL_05aa:  ldfld      float64 MCCTest.VType7::f73
    IL_05af:  add
    IL_05b0:  stfld      float64 MCCTest.VType7::f73
    IL_05b5:  ldarg.0
    IL_05b6:  dup
    IL_05b7:  ldfld      float64 MCCTest.VType7::f74
    IL_05bc:  ldarga.s   val
    IL_05be:  ldfld      float64 MCCTest.VType7::f74
    IL_05c3:  add
    IL_05c4:  stfld      float64 MCCTest.VType7::f74
    IL_05c9:  ldarg.0
    IL_05ca:  dup
    IL_05cb:  ldfld      float64 MCCTest.VType7::f75
    IL_05d0:  ldarga.s   val
    IL_05d2:  ldfld      float64 MCCTest.VType7::f75
    IL_05d7:  add
    IL_05d8:  stfld      float64 MCCTest.VType7::f75
    IL_05dd:  ldarg.0
    IL_05de:  dup
    IL_05df:  ldfld      float64 MCCTest.VType7::f76
    IL_05e4:  ldarga.s   val
    IL_05e6:  ldfld      float64 MCCTest.VType7::f76
    IL_05eb:  add
    IL_05ec:  stfld      float64 MCCTest.VType7::f76
    IL_05f1:  ldarg.0
    IL_05f2:  dup
    IL_05f3:  ldfld      float64 MCCTest.VType7::f77
    IL_05f8:  ldarga.s   val
    IL_05fa:  ldfld      float64 MCCTest.VType7::f77
    IL_05ff:  add
    IL_0600:  stfld      float64 MCCTest.VType7::f77
    IL_0605:  ldarg.0
    IL_0606:  dup
    IL_0607:  ldfld      float64 MCCTest.VType7::f78
    IL_060c:  ldarga.s   val
    IL_060e:  ldfld      float64 MCCTest.VType7::f78
    IL_0613:  add
    IL_0614:  stfld      float64 MCCTest.VType7::f78
    IL_0619:  ldarg.0
    IL_061a:  dup
    IL_061b:  ldfld      float64 MCCTest.VType7::f79
    IL_0620:  ldarga.s   val
    IL_0622:  ldfld      float64 MCCTest.VType7::f79
    IL_0627:  add
    IL_0628:  stfld      float64 MCCTest.VType7::f79
    IL_062d:  ldarg.0
    IL_062e:  dup
    IL_062f:  ldfld      float64 MCCTest.VType7::f80
    IL_0634:  ldarga.s   val
    IL_0636:  ldfld      float64 MCCTest.VType7::f80
    IL_063b:  add
    IL_063c:  stfld      float64 MCCTest.VType7::f80
    IL_0641:  ldarg.0
    IL_0642:  dup
    IL_0643:  ldfld      float64 MCCTest.VType7::f81
    IL_0648:  ldarga.s   val
    IL_064a:  ldfld      float64 MCCTest.VType7::f81
    IL_064f:  add
    IL_0650:  stfld      float64 MCCTest.VType7::f81
    IL_0655:  ldarg.0
    IL_0656:  dup
    IL_0657:  ldfld      float64 MCCTest.VType7::f82
    IL_065c:  ldarga.s   val
    IL_065e:  ldfld      float64 MCCTest.VType7::f82
    IL_0663:  add
    IL_0664:  stfld      float64 MCCTest.VType7::f82
    IL_0669:  ldarg.0
    IL_066a:  dup
    IL_066b:  ldfld      float64 MCCTest.VType7::f83
    IL_0670:  ldarga.s   val
    IL_0672:  ldfld      float64 MCCTest.VType7::f83
    IL_0677:  add
    IL_0678:  stfld      float64 MCCTest.VType7::f83
    IL_067d:  ldarg.0
    IL_067e:  dup
    IL_067f:  ldfld      float64 MCCTest.VType7::f84
    IL_0684:  ldarga.s   val
    IL_0686:  ldfld      float64 MCCTest.VType7::f84
    IL_068b:  add
    IL_068c:  stfld      float64 MCCTest.VType7::f84
    IL_0691:  ldarg.0
    IL_0692:  dup
    IL_0693:  ldfld      float64 MCCTest.VType7::f85
    IL_0698:  ldarga.s   val
    IL_069a:  ldfld      float64 MCCTest.VType7::f85
    IL_069f:  add
    IL_06a0:  stfld      float64 MCCTest.VType7::f85
    IL_06a5:  ldarg.0
    IL_06a6:  dup
    IL_06a7:  ldfld      float64 MCCTest.VType7::f86
    IL_06ac:  ldarga.s   val
    IL_06ae:  ldfld      float64 MCCTest.VType7::f86
    IL_06b3:  add
    IL_06b4:  stfld      float64 MCCTest.VType7::f86
    IL_06b9:  ldarg.0
    IL_06ba:  dup
    IL_06bb:  ldfld      float64 MCCTest.VType7::f87
    IL_06c0:  ldarga.s   val
    IL_06c2:  ldfld      float64 MCCTest.VType7::f87
    IL_06c7:  add
    IL_06c8:  stfld      float64 MCCTest.VType7::f87
    IL_06cd:  ldarg.0
    IL_06ce:  dup
    IL_06cf:  ldfld      float64 MCCTest.VType7::f88
    IL_06d4:  ldarga.s   val
    IL_06d6:  ldfld      float64 MCCTest.VType7::f88
    IL_06db:  add
    IL_06dc:  stfld      float64 MCCTest.VType7::f88
    IL_06e1:  ldarg.0
    IL_06e2:  dup
    IL_06e3:  ldfld      float64 MCCTest.VType7::f89
    IL_06e8:  ldarga.s   val
    IL_06ea:  ldfld      float64 MCCTest.VType7::f89
    IL_06ef:  add
    IL_06f0:  stfld      float64 MCCTest.VType7::f89
    IL_06f5:  ldarg.0
    IL_06f6:  dup
    IL_06f7:  ldfld      float64 MCCTest.VType7::f90
    IL_06fc:  ldarga.s   val
    IL_06fe:  ldfld      float64 MCCTest.VType7::f90
    IL_0703:  add
    IL_0704:  stfld      float64 MCCTest.VType7::f90
    IL_0709:  ldarg.0
    IL_070a:  dup
    IL_070b:  ldfld      float64 MCCTest.VType7::f91
    IL_0710:  ldarga.s   val
    IL_0712:  ldfld      float64 MCCTest.VType7::f91
    IL_0717:  add
    IL_0718:  stfld      float64 MCCTest.VType7::f91
    IL_071d:  ldarg.0
    IL_071e:  dup
    IL_071f:  ldfld      float64 MCCTest.VType7::f92
    IL_0724:  ldarga.s   val
    IL_0726:  ldfld      float64 MCCTest.VType7::f92
    IL_072b:  add
    IL_072c:  stfld      float64 MCCTest.VType7::f92
    IL_0731:  ldarg.0
    IL_0732:  dup
    IL_0733:  ldfld      float64 MCCTest.VType7::f93
    IL_0738:  ldarga.s   val
    IL_073a:  ldfld      float64 MCCTest.VType7::f93
    IL_073f:  add
    IL_0740:  stfld      float64 MCCTest.VType7::f93
    IL_0745:  ldarg.0
    IL_0746:  dup
    IL_0747:  ldfld      float64 MCCTest.VType7::f94
    IL_074c:  ldarga.s   val
    IL_074e:  ldfld      float64 MCCTest.VType7::f94
    IL_0753:  add
    IL_0754:  stfld      float64 MCCTest.VType7::f94
    IL_0759:  ldarg.0
    IL_075a:  dup
    IL_075b:  ldfld      float64 MCCTest.VType7::f95
    IL_0760:  ldarga.s   val
    IL_0762:  ldfld      float64 MCCTest.VType7::f95
    IL_0767:  add
    IL_0768:  stfld      float64 MCCTest.VType7::f95
    IL_076d:  ldarg.0
    IL_076e:  dup
    IL_076f:  ldfld      float64 MCCTest.VType7::f96
    IL_0774:  ldarga.s   val
    IL_0776:  ldfld      float64 MCCTest.VType7::f96
    IL_077b:  add
    IL_077c:  stfld      float64 MCCTest.VType7::f96
    IL_0781:  ldarg.0
    IL_0782:  dup
    IL_0783:  ldfld      float64 MCCTest.VType7::f97
    IL_0788:  ldarga.s   val
    IL_078a:  ldfld      float64 MCCTest.VType7::f97
    IL_078f:  add
    IL_0790:  stfld      float64 MCCTest.VType7::f97
    IL_0795:  ldarg.0
    IL_0796:  dup
    IL_0797:  ldfld      float64 MCCTest.VType7::f98
    IL_079c:  ldarga.s   val
    IL_079e:  ldfld      float64 MCCTest.VType7::f98
    IL_07a3:  add
    IL_07a4:  stfld      float64 MCCTest.VType7::f98
    IL_07a9:  ldarg.0
    IL_07aa:  dup
    IL_07ab:  ldfld      float64 MCCTest.VType7::f99
    IL_07b0:  ldarga.s   val
    IL_07b2:  ldfld      float64 MCCTest.VType7::f99
    IL_07b7:  add
    IL_07b8:  stfld      float64 MCCTest.VType7::f99
    IL_07bd:  ldarg.0
    IL_07be:  dup
    IL_07bf:  ldfld      float64 MCCTest.VType7::f100
    IL_07c4:  ldarga.s   val
    IL_07c6:  ldfld      float64 MCCTest.VType7::f100
    IL_07cb:  add
    IL_07cc:  stfld      float64 MCCTest.VType7::f100
    IL_07d1:  ldarg.0
    IL_07d2:  dup
    IL_07d3:  ldfld      float64 MCCTest.VType7::f101
    IL_07d8:  ldarga.s   val
    IL_07da:  ldfld      float64 MCCTest.VType7::f101
    IL_07df:  add
    IL_07e0:  stfld      float64 MCCTest.VType7::f101
    IL_07e5:  ldarg.0
    IL_07e6:  dup
    IL_07e7:  ldfld      float64 MCCTest.VType7::f102
    IL_07ec:  ldarga.s   val
    IL_07ee:  ldfld      float64 MCCTest.VType7::f102
    IL_07f3:  add
    IL_07f4:  stfld      float64 MCCTest.VType7::f102
    IL_07f9:  ldarg.0
    IL_07fa:  dup
    IL_07fb:  ldfld      float64 MCCTest.VType7::f103
    IL_0800:  ldarga.s   val
    IL_0802:  ldfld      float64 MCCTest.VType7::f103
    IL_0807:  add
    IL_0808:  stfld      float64 MCCTest.VType7::f103
    IL_080d:  ldarg.0
    IL_080e:  dup
    IL_080f:  ldfld      float64 MCCTest.VType7::f104
    IL_0814:  ldarga.s   val
    IL_0816:  ldfld      float64 MCCTest.VType7::f104
    IL_081b:  add
    IL_081c:  stfld      float64 MCCTest.VType7::f104
    IL_0821:  ldarg.0
    IL_0822:  dup
    IL_0823:  ldfld      float64 MCCTest.VType7::f105
    IL_0828:  ldarga.s   val
    IL_082a:  ldfld      float64 MCCTest.VType7::f105
    IL_082f:  add
    IL_0830:  stfld      float64 MCCTest.VType7::f105
    IL_0835:  ldarg.0
    IL_0836:  dup
    IL_0837:  ldfld      float64 MCCTest.VType7::f106
    IL_083c:  ldarga.s   val
    IL_083e:  ldfld      float64 MCCTest.VType7::f106
    IL_0843:  add
    IL_0844:  stfld      float64 MCCTest.VType7::f106
    IL_0849:  ldarg.0
    IL_084a:  dup
    IL_084b:  ldfld      float64 MCCTest.VType7::f107
    IL_0850:  ldarga.s   val
    IL_0852:  ldfld      float64 MCCTest.VType7::f107
    IL_0857:  add
    IL_0858:  stfld      float64 MCCTest.VType7::f107
    IL_085d:  ldarg.0
    IL_085e:  dup
    IL_085f:  ldfld      float64 MCCTest.VType7::f108
    IL_0864:  ldarga.s   val
    IL_0866:  ldfld      float64 MCCTest.VType7::f108
    IL_086b:  add
    IL_086c:  stfld      float64 MCCTest.VType7::f108
    IL_0871:  ldarg.0
    IL_0872:  dup
    IL_0873:  ldfld      float64 MCCTest.VType7::f109
    IL_0878:  ldarga.s   val
    IL_087a:  ldfld      float64 MCCTest.VType7::f109
    IL_087f:  add
    IL_0880:  stfld      float64 MCCTest.VType7::f109
    IL_0885:  ldarg.0
    IL_0886:  dup
    IL_0887:  ldfld      float64 MCCTest.VType7::f110
    IL_088c:  ldarga.s   val
    IL_088e:  ldfld      float64 MCCTest.VType7::f110
    IL_0893:  add
    IL_0894:  stfld      float64 MCCTest.VType7::f110
    IL_0899:  ldarg.0
    IL_089a:  dup
    IL_089b:  ldfld      float64 MCCTest.VType7::f111
    IL_08a0:  ldarga.s   val
    IL_08a2:  ldfld      float64 MCCTest.VType7::f111
    IL_08a7:  add
    IL_08a8:  stfld      float64 MCCTest.VType7::f111
    IL_08ad:  ldarg.0
    IL_08ae:  dup
    IL_08af:  ldfld      float64 MCCTest.VType7::f112
    IL_08b4:  ldarga.s   val
    IL_08b6:  ldfld      float64 MCCTest.VType7::f112
    IL_08bb:  add
    IL_08bc:  stfld      float64 MCCTest.VType7::f112
    IL_08c1:  ldarg.0
    IL_08c2:  dup
    IL_08c3:  ldfld      float64 MCCTest.VType7::f113
    IL_08c8:  ldarga.s   val
    IL_08ca:  ldfld      float64 MCCTest.VType7::f113
    IL_08cf:  add
    IL_08d0:  stfld      float64 MCCTest.VType7::f113
    IL_08d5:  ldarg.0
    IL_08d6:  dup
    IL_08d7:  ldfld      float64 MCCTest.VType7::f114
    IL_08dc:  ldarga.s   val
    IL_08de:  ldfld      float64 MCCTest.VType7::f114
    IL_08e3:  add
    IL_08e4:  stfld      float64 MCCTest.VType7::f114
    IL_08e9:  ldarg.0
    IL_08ea:  dup
    IL_08eb:  ldfld      float64 MCCTest.VType7::f115
    IL_08f0:  ldarga.s   val
    IL_08f2:  ldfld      float64 MCCTest.VType7::f115
    IL_08f7:  add
    IL_08f8:  stfld      float64 MCCTest.VType7::f115
    IL_08fd:  ldarg.0
    IL_08fe:  dup
    IL_08ff:  ldfld      float64 MCCTest.VType7::f116
    IL_0904:  ldarga.s   val
    IL_0906:  ldfld      float64 MCCTest.VType7::f116
    IL_090b:  add
    IL_090c:  stfld      float64 MCCTest.VType7::f116
    IL_0911:  ldarg.0
    IL_0912:  dup
    IL_0913:  ldfld      float64 MCCTest.VType7::f117
    IL_0918:  ldarga.s   val
    IL_091a:  ldfld      float64 MCCTest.VType7::f117
    IL_091f:  add
    IL_0920:  stfld      float64 MCCTest.VType7::f117
    IL_0925:  ldarg.0
    IL_0926:  dup
    IL_0927:  ldfld      float64 MCCTest.VType7::f118
    IL_092c:  ldarga.s   val
    IL_092e:  ldfld      float64 MCCTest.VType7::f118
    IL_0933:  add
    IL_0934:  stfld      float64 MCCTest.VType7::f118
    IL_0939:  ldarg.0
    IL_093a:  dup
    IL_093b:  ldfld      float64 MCCTest.VType7::f119
    IL_0940:  ldarga.s   val
    IL_0942:  ldfld      float64 MCCTest.VType7::f119
    IL_0947:  add
    IL_0948:  stfld      float64 MCCTest.VType7::f119
    IL_094d:  ldarg.0
    IL_094e:  dup
    IL_094f:  ldfld      float64 MCCTest.VType7::f120
    IL_0954:  ldarga.s   val
    IL_0956:  ldfld      float64 MCCTest.VType7::f120
    IL_095b:  add
    IL_095c:  stfld      float64 MCCTest.VType7::f120
    IL_0961:  ldarg.0
    IL_0962:  dup
    IL_0963:  ldfld      float64 MCCTest.VType7::f121
    IL_0968:  ldarga.s   val
    IL_096a:  ldfld      float64 MCCTest.VType7::f121
    IL_096f:  add
    IL_0970:  stfld      float64 MCCTest.VType7::f121
    IL_0975:  ldarg.0
    IL_0976:  dup
    IL_0977:  ldfld      float64 MCCTest.VType7::f122
    IL_097c:  ldarga.s   val
    IL_097e:  ldfld      float64 MCCTest.VType7::f122
    IL_0983:  add
    IL_0984:  stfld      float64 MCCTest.VType7::f122
    IL_0989:  ldarg.0
    IL_098a:  dup
    IL_098b:  ldfld      float64 MCCTest.VType7::f123
    IL_0990:  ldarga.s   val
    IL_0992:  ldfld      float64 MCCTest.VType7::f123
    IL_0997:  add
    IL_0998:  stfld      float64 MCCTest.VType7::f123
    IL_099d:  ldarg.0
    IL_099e:  dup
    IL_099f:  ldfld      float64 MCCTest.VType7::f124
    IL_09a4:  ldarga.s   val
    IL_09a6:  ldfld      float64 MCCTest.VType7::f124
    IL_09ab:  add
    IL_09ac:  stfld      float64 MCCTest.VType7::f124
    IL_09b1:  ldarg.0
    IL_09b2:  dup
    IL_09b3:  ldfld      float64 MCCTest.VType7::f125
    IL_09b8:  ldarga.s   val
    IL_09ba:  ldfld      float64 MCCTest.VType7::f125
    IL_09bf:  add
    IL_09c0:  stfld      float64 MCCTest.VType7::f125
    IL_09c5:  ldarg.0
    IL_09c6:  dup
    IL_09c7:  ldfld      float64 MCCTest.VType7::f126
    IL_09cc:  ldarga.s   val
    IL_09ce:  ldfld      float64 MCCTest.VType7::f126
    IL_09d3:  add
    IL_09d4:  stfld      float64 MCCTest.VType7::f126
    IL_09d9:  ldarg.0
    IL_09da:  dup
    IL_09db:  ldfld      float64 MCCTest.VType7::f127
    IL_09e0:  ldarga.s   val
    IL_09e2:  ldfld      float64 MCCTest.VType7::f127
    IL_09e7:  add
    IL_09e8:  stfld      float64 MCCTest.VType7::f127
    IL_09ed:  ldarg.0
    IL_09ee:  dup
    IL_09ef:  ldfld      float64 MCCTest.VType7::f128
    IL_09f4:  ldarga.s   val
    IL_09f6:  ldfld      float64 MCCTest.VType7::f128
    IL_09fb:  add
    IL_09fc:  stfld      float64 MCCTest.VType7::f128
    IL_0a01:  ldarg.0
    IL_0a02:  dup
    IL_0a03:  ldfld      float64 MCCTest.VType7::f129
    IL_0a08:  ldarga.s   val
    IL_0a0a:  ldfld      float64 MCCTest.VType7::f129
    IL_0a0f:  add
    IL_0a10:  stfld      float64 MCCTest.VType7::f129
    IL_0a15:  ldarg.0
    IL_0a16:  dup
    IL_0a17:  ldfld      float64 MCCTest.VType7::f130
    IL_0a1c:  ldarga.s   val
    IL_0a1e:  ldfld      float64 MCCTest.VType7::f130
    IL_0a23:  add
    IL_0a24:  stfld      float64 MCCTest.VType7::f130
    IL_0a29:  ldarg.0
    IL_0a2a:  dup
    IL_0a2b:  ldfld      float64 MCCTest.VType7::f131
    IL_0a30:  ldarga.s   val
    IL_0a32:  ldfld      float64 MCCTest.VType7::f131
    IL_0a37:  add
    IL_0a38:  stfld      float64 MCCTest.VType7::f131
    IL_0a3d:  ldarg.0
    IL_0a3e:  dup
    IL_0a3f:  ldfld      float64 MCCTest.VType7::f132
    IL_0a44:  ldarga.s   val
    IL_0a46:  ldfld      float64 MCCTest.VType7::f132
    IL_0a4b:  add
    IL_0a4c:  stfld      float64 MCCTest.VType7::f132
    IL_0a51:  ldarg.0
    IL_0a52:  dup
    IL_0a53:  ldfld      float64 MCCTest.VType7::f133
    IL_0a58:  ldarga.s   val
    IL_0a5a:  ldfld      float64 MCCTest.VType7::f133
    IL_0a5f:  add
    IL_0a60:  stfld      float64 MCCTest.VType7::f133
    IL_0a65:  ldarg.0
    IL_0a66:  dup
    IL_0a67:  ldfld      float64 MCCTest.VType7::f134
    IL_0a6c:  ldarga.s   val
    IL_0a6e:  ldfld      float64 MCCTest.VType7::f134
    IL_0a73:  add
    IL_0a74:  stfld      float64 MCCTest.VType7::f134
    IL_0a79:  ldarg.0
    IL_0a7a:  dup
    IL_0a7b:  ldfld      float64 MCCTest.VType7::f135
    IL_0a80:  ldarga.s   val
    IL_0a82:  ldfld      float64 MCCTest.VType7::f135
    IL_0a87:  add
    IL_0a88:  stfld      float64 MCCTest.VType7::f135
    IL_0a8d:  ldarg.0
    IL_0a8e:  dup
    IL_0a8f:  ldfld      float64 MCCTest.VType7::f136
    IL_0a94:  ldarga.s   val
    IL_0a96:  ldfld      float64 MCCTest.VType7::f136
    IL_0a9b:  add
    IL_0a9c:  stfld      float64 MCCTest.VType7::f136
    IL_0aa1:  ldarg.0
    IL_0aa2:  dup
    IL_0aa3:  ldfld      float64 MCCTest.VType7::f137
    IL_0aa8:  ldarga.s   val
    IL_0aaa:  ldfld      float64 MCCTest.VType7::f137
    IL_0aaf:  add
    IL_0ab0:  stfld      float64 MCCTest.VType7::f137
    IL_0ab5:  ldarg.0
    IL_0ab6:  dup
    IL_0ab7:  ldfld      float64 MCCTest.VType7::f138
    IL_0abc:  ldarga.s   val
    IL_0abe:  ldfld      float64 MCCTest.VType7::f138
    IL_0ac3:  add
    IL_0ac4:  stfld      float64 MCCTest.VType7::f138
    IL_0ac9:  ldarg.0
    IL_0aca:  dup
    IL_0acb:  ldfld      float64 MCCTest.VType7::f139
    IL_0ad0:  ldarga.s   val
    IL_0ad2:  ldfld      float64 MCCTest.VType7::f139
    IL_0ad7:  add
    IL_0ad8:  stfld      float64 MCCTest.VType7::f139
    IL_0add:  ldarg.0
    IL_0ade:  dup
    IL_0adf:  ldfld      float64 MCCTest.VType7::f140
    IL_0ae4:  ldarga.s   val
    IL_0ae6:  ldfld      float64 MCCTest.VType7::f140
    IL_0aeb:  add
    IL_0aec:  stfld      float64 MCCTest.VType7::f140
    IL_0af1:  ldarg.0
    IL_0af2:  dup
    IL_0af3:  ldfld      float64 MCCTest.VType7::f141
    IL_0af8:  ldarga.s   val
    IL_0afa:  ldfld      float64 MCCTest.VType7::f141
    IL_0aff:  add
    IL_0b00:  stfld      float64 MCCTest.VType7::f141
    IL_0b05:  ldarg.0
    IL_0b06:  dup
    IL_0b07:  ldfld      float64 MCCTest.VType7::f142
    IL_0b0c:  ldarga.s   val
    IL_0b0e:  ldfld      float64 MCCTest.VType7::f142
    IL_0b13:  add
    IL_0b14:  stfld      float64 MCCTest.VType7::f142
    IL_0b19:  ldarg.0
    IL_0b1a:  dup
    IL_0b1b:  ldfld      float64 MCCTest.VType7::f143
    IL_0b20:  ldarga.s   val
    IL_0b22:  ldfld      float64 MCCTest.VType7::f143
    IL_0b27:  add
    IL_0b28:  stfld      float64 MCCTest.VType7::f143
    IL_0b2d:  ldarg.0
    IL_0b2e:  dup
    IL_0b2f:  ldfld      float64 MCCTest.VType7::f144
    IL_0b34:  ldarga.s   val
    IL_0b36:  ldfld      float64 MCCTest.VType7::f144
    IL_0b3b:  add
    IL_0b3c:  stfld      float64 MCCTest.VType7::f144
    IL_0b41:  ldarg.0
    IL_0b42:  dup
    IL_0b43:  ldfld      float64 MCCTest.VType7::f145
    IL_0b48:  ldarga.s   val
    IL_0b4a:  ldfld      float64 MCCTest.VType7::f145
    IL_0b4f:  add
    IL_0b50:  stfld      float64 MCCTest.VType7::f145
    IL_0b55:  ldarg.0
    IL_0b56:  dup
    IL_0b57:  ldfld      float64 MCCTest.VType7::f146
    IL_0b5c:  ldarga.s   val
    IL_0b5e:  ldfld      float64 MCCTest.VType7::f146
    IL_0b63:  add
    IL_0b64:  stfld      float64 MCCTest.VType7::f146
    IL_0b69:  ldarg.0
    IL_0b6a:  dup
    IL_0b6b:  ldfld      float64 MCCTest.VType7::f147
    IL_0b70:  ldarga.s   val
    IL_0b72:  ldfld      float64 MCCTest.VType7::f147
    IL_0b77:  add
    IL_0b78:  stfld      float64 MCCTest.VType7::f147
    IL_0b7d:  ldarg.0
    IL_0b7e:  dup
    IL_0b7f:  ldfld      float64 MCCTest.VType7::f148
    IL_0b84:  ldarga.s   val
    IL_0b86:  ldfld      float64 MCCTest.VType7::f148
    IL_0b8b:  add
    IL_0b8c:  stfld      float64 MCCTest.VType7::f148
    IL_0b91:  ldarg.0
    IL_0b92:  dup
    IL_0b93:  ldfld      float64 MCCTest.VType7::f149
    IL_0b98:  ldarga.s   val
    IL_0b9a:  ldfld      float64 MCCTest.VType7::f149
    IL_0b9f:  add
    IL_0ba0:  stfld      float64 MCCTest.VType7::f149
    IL_0ba5:  ldarg.0
    IL_0ba6:  dup
    IL_0ba7:  ldfld      float64 MCCTest.VType7::f150
    IL_0bac:  ldarga.s   val
    IL_0bae:  ldfld      float64 MCCTest.VType7::f150
    IL_0bb3:  add
    IL_0bb4:  stfld      float64 MCCTest.VType7::f150
    IL_0bb9:  ldarg.0
    IL_0bba:  dup
    IL_0bbb:  ldfld      float64 MCCTest.VType7::f151
    IL_0bc0:  ldarga.s   val
    IL_0bc2:  ldfld      float64 MCCTest.VType7::f151
    IL_0bc7:  add
    IL_0bc8:  stfld      float64 MCCTest.VType7::f151
    IL_0bcd:  ldarg.0
    IL_0bce:  dup
    IL_0bcf:  ldfld      float64 MCCTest.VType7::f152
    IL_0bd4:  ldarga.s   val
    IL_0bd6:  ldfld      float64 MCCTest.VType7::f152
    IL_0bdb:  add
    IL_0bdc:  stfld      float64 MCCTest.VType7::f152
    IL_0be1:  ldarg.0
    IL_0be2:  dup
    IL_0be3:  ldfld      float64 MCCTest.VType7::f153
    IL_0be8:  ldarga.s   val
    IL_0bea:  ldfld      float64 MCCTest.VType7::f153
    IL_0bef:  add
    IL_0bf0:  stfld      float64 MCCTest.VType7::f153
    IL_0bf5:  ldarg.0
    IL_0bf6:  dup
    IL_0bf7:  ldfld      float64 MCCTest.VType7::f154
    IL_0bfc:  ldarga.s   val
    IL_0bfe:  ldfld      float64 MCCTest.VType7::f154
    IL_0c03:  add
    IL_0c04:  stfld      float64 MCCTest.VType7::f154
    IL_0c09:  ldarg.0
    IL_0c0a:  dup
    IL_0c0b:  ldfld      float64 MCCTest.VType7::f155
    IL_0c10:  ldarga.s   val
    IL_0c12:  ldfld      float64 MCCTest.VType7::f155
    IL_0c17:  add
    IL_0c18:  stfld      float64 MCCTest.VType7::f155
    IL_0c1d:  ldarg.0
    IL_0c1e:  dup
    IL_0c1f:  ldfld      float64 MCCTest.VType7::f156
    IL_0c24:  ldarga.s   val
    IL_0c26:  ldfld      float64 MCCTest.VType7::f156
    IL_0c2b:  add
    IL_0c2c:  stfld      float64 MCCTest.VType7::f156
    IL_0c31:  ldarg.0
    IL_0c32:  dup
    IL_0c33:  ldfld      float64 MCCTest.VType7::f157
    IL_0c38:  ldarga.s   val
    IL_0c3a:  ldfld      float64 MCCTest.VType7::f157
    IL_0c3f:  add
    IL_0c40:  stfld      float64 MCCTest.VType7::f157
    IL_0c45:  ldarg.0
    IL_0c46:  dup
    IL_0c47:  ldfld      float64 MCCTest.VType7::f158
    IL_0c4c:  ldarga.s   val
    IL_0c4e:  ldfld      float64 MCCTest.VType7::f158
    IL_0c53:  add
    IL_0c54:  stfld      float64 MCCTest.VType7::f158
    IL_0c59:  ldarg.0
    IL_0c5a:  dup
    IL_0c5b:  ldfld      float64 MCCTest.VType7::f159
    IL_0c60:  ldarga.s   val
    IL_0c62:  ldfld      float64 MCCTest.VType7::f159
    IL_0c67:  add
    IL_0c68:  stfld      float64 MCCTest.VType7::f159
    IL_0c6d:  ldarg.0
    IL_0c6e:  dup
    IL_0c6f:  ldfld      float64 MCCTest.VType7::f160
    IL_0c74:  ldarga.s   val
    IL_0c76:  ldfld      float64 MCCTest.VType7::f160
    IL_0c7b:  add
    IL_0c7c:  stfld      float64 MCCTest.VType7::f160
    IL_0c81:  ldarg.0
    IL_0c82:  dup
    IL_0c83:  ldfld      float64 MCCTest.VType7::f161
    IL_0c88:  ldarga.s   val
    IL_0c8a:  ldfld      float64 MCCTest.VType7::f161
    IL_0c8f:  add
    IL_0c90:  stfld      float64 MCCTest.VType7::f161
    IL_0c95:  ldarg.0
    IL_0c96:  dup
    IL_0c97:  ldfld      float64 MCCTest.VType7::f162
    IL_0c9c:  ldarga.s   val
    IL_0c9e:  ldfld      float64 MCCTest.VType7::f162
    IL_0ca3:  add
    IL_0ca4:  stfld      float64 MCCTest.VType7::f162
    IL_0ca9:  ldarg.0
    IL_0caa:  dup
    IL_0cab:  ldfld      float64 MCCTest.VType7::f163
    IL_0cb0:  ldarga.s   val
    IL_0cb2:  ldfld      float64 MCCTest.VType7::f163
    IL_0cb7:  add
    IL_0cb8:  stfld      float64 MCCTest.VType7::f163
    IL_0cbd:  ldarg.0
    IL_0cbe:  dup
    IL_0cbf:  ldfld      float64 MCCTest.VType7::f164
    IL_0cc4:  ldarga.s   val
    IL_0cc6:  ldfld      float64 MCCTest.VType7::f164
    IL_0ccb:  add
    IL_0ccc:  stfld      float64 MCCTest.VType7::f164
    IL_0cd1:  ldarg.0
    IL_0cd2:  dup
    IL_0cd3:  ldfld      float64 MCCTest.VType7::f165
    IL_0cd8:  ldarga.s   val
    IL_0cda:  ldfld      float64 MCCTest.VType7::f165
    IL_0cdf:  add
    IL_0ce0:  stfld      float64 MCCTest.VType7::f165
    IL_0ce5:  ldarg.0
    IL_0ce6:  dup
    IL_0ce7:  ldfld      float64 MCCTest.VType7::f166
    IL_0cec:  ldarga.s   val
    IL_0cee:  ldfld      float64 MCCTest.VType7::f166
    IL_0cf3:  add
    IL_0cf4:  stfld      float64 MCCTest.VType7::f166
    IL_0cf9:  ldarg.0
    IL_0cfa:  dup
    IL_0cfb:  ldfld      float64 MCCTest.VType7::f167
    IL_0d00:  ldarga.s   val
    IL_0d02:  ldfld      float64 MCCTest.VType7::f167
    IL_0d07:  add
    IL_0d08:  stfld      float64 MCCTest.VType7::f167
    IL_0d0d:  ldarg.0
    IL_0d0e:  dup
    IL_0d0f:  ldfld      float64 MCCTest.VType7::f168
    IL_0d14:  ldarga.s   val
    IL_0d16:  ldfld      float64 MCCTest.VType7::f168
    IL_0d1b:  add
    IL_0d1c:  stfld      float64 MCCTest.VType7::f168
    IL_0d21:  ldarg.0
    IL_0d22:  dup
    IL_0d23:  ldfld      float64 MCCTest.VType7::f169
    IL_0d28:  ldarga.s   val
    IL_0d2a:  ldfld      float64 MCCTest.VType7::f169
    IL_0d2f:  add
    IL_0d30:  stfld      float64 MCCTest.VType7::f169
    IL_0d35:  ldarg.0
    IL_0d36:  dup
    IL_0d37:  ldfld      float64 MCCTest.VType7::f170
    IL_0d3c:  ldarga.s   val
    IL_0d3e:  ldfld      float64 MCCTest.VType7::f170
    IL_0d43:  add
    IL_0d44:  stfld      float64 MCCTest.VType7::f170
    IL_0d49:  ldarg.0
    IL_0d4a:  dup
    IL_0d4b:  ldfld      float64 MCCTest.VType7::f171
    IL_0d50:  ldarga.s   val
    IL_0d52:  ldfld      float64 MCCTest.VType7::f171
    IL_0d57:  add
    IL_0d58:  stfld      float64 MCCTest.VType7::f171
    IL_0d5d:  ldarg.0
    IL_0d5e:  dup
    IL_0d5f:  ldfld      float64 MCCTest.VType7::f172
    IL_0d64:  ldarga.s   val
    IL_0d66:  ldfld      float64 MCCTest.VType7::f172
    IL_0d6b:  add
    IL_0d6c:  stfld      float64 MCCTest.VType7::f172
    IL_0d71:  ldarg.0
    IL_0d72:  dup
    IL_0d73:  ldfld      float64 MCCTest.VType7::f173
    IL_0d78:  ldarga.s   val
    IL_0d7a:  ldfld      float64 MCCTest.VType7::f173
    IL_0d7f:  add
    IL_0d80:  stfld      float64 MCCTest.VType7::f173
    IL_0d85:  ldarg.0
    IL_0d86:  dup
    IL_0d87:  ldfld      float64 MCCTest.VType7::f174
    IL_0d8c:  ldarga.s   val
    IL_0d8e:  ldfld      float64 MCCTest.VType7::f174
    IL_0d93:  add
    IL_0d94:  stfld      float64 MCCTest.VType7::f174
    IL_0d99:  ldarg.0
    IL_0d9a:  dup
    IL_0d9b:  ldfld      float64 MCCTest.VType7::f175
    IL_0da0:  ldarga.s   val
    IL_0da2:  ldfld      float64 MCCTest.VType7::f175
    IL_0da7:  add
    IL_0da8:  stfld      float64 MCCTest.VType7::f175
    IL_0dad:  ldarg.0
    IL_0dae:  dup
    IL_0daf:  ldfld      float64 MCCTest.VType7::f176
    IL_0db4:  ldarga.s   val
    IL_0db6:  ldfld      float64 MCCTest.VType7::f176
    IL_0dbb:  add
    IL_0dbc:  stfld      float64 MCCTest.VType7::f176
    IL_0dc1:  ldarg.0
    IL_0dc2:  dup
    IL_0dc3:  ldfld      float64 MCCTest.VType7::f177
    IL_0dc8:  ldarga.s   val
    IL_0dca:  ldfld      float64 MCCTest.VType7::f177
    IL_0dcf:  add
    IL_0dd0:  stfld      float64 MCCTest.VType7::f177
    IL_0dd5:  ldarg.0
    IL_0dd6:  dup
    IL_0dd7:  ldfld      float64 MCCTest.VType7::f178
    IL_0ddc:  ldarga.s   val
    IL_0dde:  ldfld      float64 MCCTest.VType7::f178
    IL_0de3:  add
    IL_0de4:  stfld      float64 MCCTest.VType7::f178
    IL_0de9:  ldarg.0
    IL_0dea:  dup
    IL_0deb:  ldfld      float64 MCCTest.VType7::f179
    IL_0df0:  ldarga.s   val
    IL_0df2:  ldfld      float64 MCCTest.VType7::f179
    IL_0df7:  add
    IL_0df8:  stfld      float64 MCCTest.VType7::f179
    IL_0dfd:  ldarg.0
    IL_0dfe:  dup
    IL_0dff:  ldfld      float64 MCCTest.VType7::f180
    IL_0e04:  ldarga.s   val
    IL_0e06:  ldfld      float64 MCCTest.VType7::f180
    IL_0e0b:  add
    IL_0e0c:  stfld      float64 MCCTest.VType7::f180
    IL_0e11:  ldarg.0
    IL_0e12:  dup
    IL_0e13:  ldfld      float64 MCCTest.VType7::f181
    IL_0e18:  ldarga.s   val
    IL_0e1a:  ldfld      float64 MCCTest.VType7::f181
    IL_0e1f:  add
    IL_0e20:  stfld      float64 MCCTest.VType7::f181
    IL_0e25:  ldarg.0
    IL_0e26:  dup
    IL_0e27:  ldfld      float64 MCCTest.VType7::f182
    IL_0e2c:  ldarga.s   val
    IL_0e2e:  ldfld      float64 MCCTest.VType7::f182
    IL_0e33:  add
    IL_0e34:  stfld      float64 MCCTest.VType7::f182
    IL_0e39:  ldarg.0
    IL_0e3a:  dup
    IL_0e3b:  ldfld      float64 MCCTest.VType7::f183
    IL_0e40:  ldarga.s   val
    IL_0e42:  ldfld      float64 MCCTest.VType7::f183
    IL_0e47:  add
    IL_0e48:  stfld      float64 MCCTest.VType7::f183
    IL_0e4d:  ldarg.0
    IL_0e4e:  dup
    IL_0e4f:  ldfld      float64 MCCTest.VType7::f184
    IL_0e54:  ldarga.s   val
    IL_0e56:  ldfld      float64 MCCTest.VType7::f184
    IL_0e5b:  add
    IL_0e5c:  stfld      float64 MCCTest.VType7::f184
    IL_0e61:  ldarg.0
    IL_0e62:  dup
    IL_0e63:  ldfld      float64 MCCTest.VType7::f185
    IL_0e68:  ldarga.s   val
    IL_0e6a:  ldfld      float64 MCCTest.VType7::f185
    IL_0e6f:  add
    IL_0e70:  stfld      float64 MCCTest.VType7::f185
    IL_0e75:  ldarg.0
    IL_0e76:  dup
    IL_0e77:  ldfld      float64 MCCTest.VType7::f186
    IL_0e7c:  ldarga.s   val
    IL_0e7e:  ldfld      float64 MCCTest.VType7::f186
    IL_0e83:  add
    IL_0e84:  stfld      float64 MCCTest.VType7::f186
    IL_0e89:  ldarg.0
    IL_0e8a:  dup
    IL_0e8b:  ldfld      float64 MCCTest.VType7::f187
    IL_0e90:  ldarga.s   val
    IL_0e92:  ldfld      float64 MCCTest.VType7::f187
    IL_0e97:  add
    IL_0e98:  stfld      float64 MCCTest.VType7::f187
    IL_0e9d:  ldarg.0
    IL_0e9e:  dup
    IL_0e9f:  ldfld      float64 MCCTest.VType7::f188
    IL_0ea4:  ldarga.s   val
    IL_0ea6:  ldfld      float64 MCCTest.VType7::f188
    IL_0eab:  add
    IL_0eac:  stfld      float64 MCCTest.VType7::f188
    IL_0eb1:  ldarg.0
    IL_0eb2:  dup
    IL_0eb3:  ldfld      float64 MCCTest.VType7::f189
    IL_0eb8:  ldarga.s   val
    IL_0eba:  ldfld      float64 MCCTest.VType7::f189
    IL_0ebf:  add
    IL_0ec0:  stfld      float64 MCCTest.VType7::f189
    IL_0ec5:  ldarg.0
    IL_0ec6:  dup
    IL_0ec7:  ldfld      float64 MCCTest.VType7::f190
    IL_0ecc:  ldarga.s   val
    IL_0ece:  ldfld      float64 MCCTest.VType7::f190
    IL_0ed3:  add
    IL_0ed4:  stfld      float64 MCCTest.VType7::f190
    IL_0ed9:  ldarg.0
    IL_0eda:  dup
    IL_0edb:  ldfld      float64 MCCTest.VType7::f191
    IL_0ee0:  ldarga.s   val
    IL_0ee2:  ldfld      float64 MCCTest.VType7::f191
    IL_0ee7:  add
    IL_0ee8:  stfld      float64 MCCTest.VType7::f191
    IL_0eed:  ldarg.0
    IL_0eee:  dup
    IL_0eef:  ldfld      float64 MCCTest.VType7::f192
    IL_0ef4:  ldarga.s   val
    IL_0ef6:  ldfld      float64 MCCTest.VType7::f192
    IL_0efb:  add
    IL_0efc:  stfld      float64 MCCTest.VType7::f192
    IL_0f01:  ldarg.0
    IL_0f02:  dup
    IL_0f03:  ldfld      float64 MCCTest.VType7::f193
    IL_0f08:  ldarga.s   val
    IL_0f0a:  ldfld      float64 MCCTest.VType7::f193
    IL_0f0f:  add
    IL_0f10:  stfld      float64 MCCTest.VType7::f193
    IL_0f15:  ldarg.0
    IL_0f16:  dup
    IL_0f17:  ldfld      float64 MCCTest.VType7::f194
    IL_0f1c:  ldarga.s   val
    IL_0f1e:  ldfld      float64 MCCTest.VType7::f194
    IL_0f23:  add
    IL_0f24:  stfld      float64 MCCTest.VType7::f194
    IL_0f29:  ldarg.0
    IL_0f2a:  dup
    IL_0f2b:  ldfld      float64 MCCTest.VType7::f195
    IL_0f30:  ldarga.s   val
    IL_0f32:  ldfld      float64 MCCTest.VType7::f195
    IL_0f37:  add
    IL_0f38:  stfld      float64 MCCTest.VType7::f195
    IL_0f3d:  ldarg.0
    IL_0f3e:  dup
    IL_0f3f:  ldfld      float64 MCCTest.VType7::f196
    IL_0f44:  ldarga.s   val
    IL_0f46:  ldfld      float64 MCCTest.VType7::f196
    IL_0f4b:  add
    IL_0f4c:  stfld      float64 MCCTest.VType7::f196
    IL_0f51:  ldarg.0
    IL_0f52:  dup
    IL_0f53:  ldfld      float64 MCCTest.VType7::f197
    IL_0f58:  ldarga.s   val
    IL_0f5a:  ldfld      float64 MCCTest.VType7::f197
    IL_0f5f:  add
    IL_0f60:  stfld      float64 MCCTest.VType7::f197
    IL_0f65:  ldarg.0
    IL_0f66:  dup
    IL_0f67:  ldfld      float64 MCCTest.VType7::f198
    IL_0f6c:  ldarga.s   val
    IL_0f6e:  ldfld      float64 MCCTest.VType7::f198
    IL_0f73:  add
    IL_0f74:  stfld      float64 MCCTest.VType7::f198
    IL_0f79:  ldarg.0
    IL_0f7a:  dup
    IL_0f7b:  ldfld      float64 MCCTest.VType7::f199
    IL_0f80:  ldarga.s   val
    IL_0f82:  ldfld      float64 MCCTest.VType7::f199
    IL_0f87:  add
    IL_0f88:  stfld      float64 MCCTest.VType7::f199
    IL_0f8d:  ldarg.0
    IL_0f8e:  dup
    IL_0f8f:  ldfld      float64 MCCTest.VType7::f200
    IL_0f94:  ldarga.s   val
    IL_0f96:  ldfld      float64 MCCTest.VType7::f200
    IL_0f9b:  add
    IL_0f9c:  stfld      float64 MCCTest.VType7::f200
    IL_0fa1:  ldarg.0
    IL_0fa2:  dup
    IL_0fa3:  ldfld      float64 MCCTest.VType7::f201
    IL_0fa8:  ldarga.s   val
    IL_0faa:  ldfld      float64 MCCTest.VType7::f201
    IL_0faf:  add
    IL_0fb0:  stfld      float64 MCCTest.VType7::f201
    IL_0fb5:  ldarg.0
    IL_0fb6:  dup
    IL_0fb7:  ldfld      float64 MCCTest.VType7::f202
    IL_0fbc:  ldarga.s   val
    IL_0fbe:  ldfld      float64 MCCTest.VType7::f202
    IL_0fc3:  add
    IL_0fc4:  stfld      float64 MCCTest.VType7::f202
    IL_0fc9:  ldarg.0
    IL_0fca:  dup
    IL_0fcb:  ldfld      float64 MCCTest.VType7::f203
    IL_0fd0:  ldarga.s   val
    IL_0fd2:  ldfld      float64 MCCTest.VType7::f203
    IL_0fd7:  add
    IL_0fd8:  stfld      float64 MCCTest.VType7::f203
    IL_0fdd:  ldarg.0
    IL_0fde:  dup
    IL_0fdf:  ldfld      float64 MCCTest.VType7::f204
    IL_0fe4:  ldarga.s   val
    IL_0fe6:  ldfld      float64 MCCTest.VType7::f204
    IL_0feb:  add
    IL_0fec:  stfld      float64 MCCTest.VType7::f204
    IL_0ff1:  ldarg.0
    IL_0ff2:  dup
    IL_0ff3:  ldfld      float64 MCCTest.VType7::f205
    IL_0ff8:  ldarga.s   val
    IL_0ffa:  ldfld      float64 MCCTest.VType7::f205
    IL_0fff:  add
    IL_1000:  stfld      float64 MCCTest.VType7::f205
    IL_1005:  ldarg.0
    IL_1006:  dup
    IL_1007:  ldfld      float64 MCCTest.VType7::f206
    IL_100c:  ldarga.s   val
    IL_100e:  ldfld      float64 MCCTest.VType7::f206
    IL_1013:  add
    IL_1014:  stfld      float64 MCCTest.VType7::f206
    IL_1019:  ldarg.0
    IL_101a:  dup
    IL_101b:  ldfld      float64 MCCTest.VType7::f207
    IL_1020:  ldarga.s   val
    IL_1022:  ldfld      float64 MCCTest.VType7::f207
    IL_1027:  add
    IL_1028:  stfld      float64 MCCTest.VType7::f207
    IL_102d:  ldarg.0
    IL_102e:  dup
    IL_102f:  ldfld      float64 MCCTest.VType7::f208
    IL_1034:  ldarga.s   val
    IL_1036:  ldfld      float64 MCCTest.VType7::f208
    IL_103b:  add
    IL_103c:  stfld      float64 MCCTest.VType7::f208
    IL_1041:  ldarg.0
    IL_1042:  dup
    IL_1043:  ldfld      float64 MCCTest.VType7::f209
    IL_1048:  ldarga.s   val
    IL_104a:  ldfld      float64 MCCTest.VType7::f209
    IL_104f:  add
    IL_1050:  stfld      float64 MCCTest.VType7::f209
    IL_1055:  ldarg.0
    IL_1056:  dup
    IL_1057:  ldfld      float64 MCCTest.VType7::f210
    IL_105c:  ldarga.s   val
    IL_105e:  ldfld      float64 MCCTest.VType7::f210
    IL_1063:  add
    IL_1064:  stfld      float64 MCCTest.VType7::f210
    IL_1069:  ldarg.0
    IL_106a:  dup
    IL_106b:  ldfld      float64 MCCTest.VType7::f211
    IL_1070:  ldarga.s   val
    IL_1072:  ldfld      float64 MCCTest.VType7::f211
    IL_1077:  add
    IL_1078:  stfld      float64 MCCTest.VType7::f211
    IL_107d:  ldarg.0
    IL_107e:  dup
    IL_107f:  ldfld      float64 MCCTest.VType7::f212
    IL_1084:  ldarga.s   val
    IL_1086:  ldfld      float64 MCCTest.VType7::f212
    IL_108b:  add
    IL_108c:  stfld      float64 MCCTest.VType7::f212
    IL_1091:  ldarg.0
    IL_1092:  dup
    IL_1093:  ldfld      float64 MCCTest.VType7::f213
    IL_1098:  ldarga.s   val
    IL_109a:  ldfld      float64 MCCTest.VType7::f213
    IL_109f:  add
    IL_10a0:  stfld      float64 MCCTest.VType7::f213
    IL_10a5:  ldarg.0
    IL_10a6:  dup
    IL_10a7:  ldfld      float64 MCCTest.VType7::f214
    IL_10ac:  ldarga.s   val
    IL_10ae:  ldfld      float64 MCCTest.VType7::f214
    IL_10b3:  add
    IL_10b4:  stfld      float64 MCCTest.VType7::f214
    IL_10b9:  ldarg.0
    IL_10ba:  dup
    IL_10bb:  ldfld      float64 MCCTest.VType7::f215
    IL_10c0:  ldarga.s   val
    IL_10c2:  ldfld      float64 MCCTest.VType7::f215
    IL_10c7:  add
    IL_10c8:  stfld      float64 MCCTest.VType7::f215
    IL_10cd:  ldarg.0
    IL_10ce:  dup
    IL_10cf:  ldfld      float64 MCCTest.VType7::f216
    IL_10d4:  ldarga.s   val
    IL_10d6:  ldfld      float64 MCCTest.VType7::f216
    IL_10db:  add
    IL_10dc:  stfld      float64 MCCTest.VType7::f216
    IL_10e1:  ldarg.0
    IL_10e2:  dup
    IL_10e3:  ldfld      float64 MCCTest.VType7::f217
    IL_10e8:  ldarga.s   val
    IL_10ea:  ldfld      float64 MCCTest.VType7::f217
    IL_10ef:  add
    IL_10f0:  stfld      float64 MCCTest.VType7::f217
    IL_10f5:  ldarg.0
    IL_10f6:  dup
    IL_10f7:  ldfld      float64 MCCTest.VType7::f218
    IL_10fc:  ldarga.s   val
    IL_10fe:  ldfld      float64 MCCTest.VType7::f218
    IL_1103:  add
    IL_1104:  stfld      float64 MCCTest.VType7::f218
    IL_1109:  ldarg.0
    IL_110a:  dup
    IL_110b:  ldfld      float64 MCCTest.VType7::f219
    IL_1110:  ldarga.s   val
    IL_1112:  ldfld      float64 MCCTest.VType7::f219
    IL_1117:  add
    IL_1118:  stfld      float64 MCCTest.VType7::f219
    IL_111d:  ldarg.0
    IL_111e:  dup
    IL_111f:  ldfld      float64 MCCTest.VType7::f220
    IL_1124:  ldarga.s   val
    IL_1126:  ldfld      float64 MCCTest.VType7::f220
    IL_112b:  add
    IL_112c:  stfld      float64 MCCTest.VType7::f220
    IL_1131:  ldarg.0
    IL_1132:  dup
    IL_1133:  ldfld      float64 MCCTest.VType7::f221
    IL_1138:  ldarga.s   val
    IL_113a:  ldfld      float64 MCCTest.VType7::f221
    IL_113f:  add
    IL_1140:  stfld      float64 MCCTest.VType7::f221
    IL_1145:  ldarg.0
    IL_1146:  dup
    IL_1147:  ldfld      float64 MCCTest.VType7::f222
    IL_114c:  ldarga.s   val
    IL_114e:  ldfld      float64 MCCTest.VType7::f222
    IL_1153:  add
    IL_1154:  stfld      float64 MCCTest.VType7::f222
    IL_1159:  ldarg.0
    IL_115a:  dup
    IL_115b:  ldfld      float64 MCCTest.VType7::f223
    IL_1160:  ldarga.s   val
    IL_1162:  ldfld      float64 MCCTest.VType7::f223
    IL_1167:  add
    IL_1168:  stfld      float64 MCCTest.VType7::f223
    IL_116d:  ldarg.0
    IL_116e:  dup
    IL_116f:  ldfld      float64 MCCTest.VType7::f224
    IL_1174:  ldarga.s   val
    IL_1176:  ldfld      float64 MCCTest.VType7::f224
    IL_117b:  add
    IL_117c:  stfld      float64 MCCTest.VType7::f224
    IL_1181:  ldarg.0
    IL_1182:  dup
    IL_1183:  ldfld      float64 MCCTest.VType7::f225
    IL_1188:  ldarga.s   val
    IL_118a:  ldfld      float64 MCCTest.VType7::f225
    IL_118f:  add
    IL_1190:  stfld      float64 MCCTest.VType7::f225
    IL_1195:  ldarg.0
    IL_1196:  dup
    IL_1197:  ldfld      float64 MCCTest.VType7::f226
    IL_119c:  ldarga.s   val
    IL_119e:  ldfld      float64 MCCTest.VType7::f226
    IL_11a3:  add
    IL_11a4:  stfld      float64 MCCTest.VType7::f226
    IL_11a9:  ldarg.0
    IL_11aa:  dup
    IL_11ab:  ldfld      float64 MCCTest.VType7::f227
    IL_11b0:  ldarga.s   val
    IL_11b2:  ldfld      float64 MCCTest.VType7::f227
    IL_11b7:  add
    IL_11b8:  stfld      float64 MCCTest.VType7::f227
    IL_11bd:  ldarg.0
    IL_11be:  dup
    IL_11bf:  ldfld      float64 MCCTest.VType7::f228
    IL_11c4:  ldarga.s   val
    IL_11c6:  ldfld      float64 MCCTest.VType7::f228
    IL_11cb:  add
    IL_11cc:  stfld      float64 MCCTest.VType7::f228
    IL_11d1:  ldarg.0
    IL_11d2:  dup
    IL_11d3:  ldfld      float64 MCCTest.VType7::f229
    IL_11d8:  ldarga.s   val
    IL_11da:  ldfld      float64 MCCTest.VType7::f229
    IL_11df:  add
    IL_11e0:  stfld      float64 MCCTest.VType7::f229
    IL_11e5:  ldarg.0
    IL_11e6:  dup
    IL_11e7:  ldfld      float64 MCCTest.VType7::f230
    IL_11ec:  ldarga.s   val
    IL_11ee:  ldfld      float64 MCCTest.VType7::f230
    IL_11f3:  add
    IL_11f4:  stfld      float64 MCCTest.VType7::f230
    IL_11f9:  ldarg.0
    IL_11fa:  dup
    IL_11fb:  ldfld      float64 MCCTest.VType7::f231
    IL_1200:  ldarga.s   val
    IL_1202:  ldfld      float64 MCCTest.VType7::f231
    IL_1207:  add
    IL_1208:  stfld      float64 MCCTest.VType7::f231
    IL_120d:  ldarg.0
    IL_120e:  dup
    IL_120f:  ldfld      float64 MCCTest.VType7::f232
    IL_1214:  ldarga.s   val
    IL_1216:  ldfld      float64 MCCTest.VType7::f232
    IL_121b:  add
    IL_121c:  stfld      float64 MCCTest.VType7::f232
    IL_1221:  ldarg.0
    IL_1222:  dup
    IL_1223:  ldfld      float64 MCCTest.VType7::f233
    IL_1228:  ldarga.s   val
    IL_122a:  ldfld      float64 MCCTest.VType7::f233
    IL_122f:  add
    IL_1230:  stfld      float64 MCCTest.VType7::f233
    IL_1235:  ldarg.0
    IL_1236:  dup
    IL_1237:  ldfld      float64 MCCTest.VType7::f234
    IL_123c:  ldarga.s   val
    IL_123e:  ldfld      float64 MCCTest.VType7::f234
    IL_1243:  add
    IL_1244:  stfld      float64 MCCTest.VType7::f234
    IL_1249:  ldarg.0
    IL_124a:  dup
    IL_124b:  ldfld      float64 MCCTest.VType7::f235
    IL_1250:  ldarga.s   val
    IL_1252:  ldfld      float64 MCCTest.VType7::f235
    IL_1257:  add
    IL_1258:  stfld      float64 MCCTest.VType7::f235
    IL_125d:  ldarg.0
    IL_125e:  dup
    IL_125f:  ldfld      float64 MCCTest.VType7::f236
    IL_1264:  ldarga.s   val
    IL_1266:  ldfld      float64 MCCTest.VType7::f236
    IL_126b:  add
    IL_126c:  stfld      float64 MCCTest.VType7::f236
    IL_1271:  ldarg.0
    IL_1272:  dup
    IL_1273:  ldfld      float64 MCCTest.VType7::f237
    IL_1278:  ldarga.s   val
    IL_127a:  ldfld      float64 MCCTest.VType7::f237
    IL_127f:  add
    IL_1280:  stfld      float64 MCCTest.VType7::f237
    IL_1285:  ldarg.0
    IL_1286:  dup
    IL_1287:  ldfld      float64 MCCTest.VType7::f238
    IL_128c:  ldarga.s   val
    IL_128e:  ldfld      float64 MCCTest.VType7::f238
    IL_1293:  add
    IL_1294:  stfld      float64 MCCTest.VType7::f238
    IL_1299:  ldarg.0
    IL_129a:  dup
    IL_129b:  ldfld      float64 MCCTest.VType7::f239
    IL_12a0:  ldarga.s   val
    IL_12a2:  ldfld      float64 MCCTest.VType7::f239
    IL_12a7:  add
    IL_12a8:  stfld      float64 MCCTest.VType7::f239
    IL_12ad:  ldarg.0
    IL_12ae:  dup
    IL_12af:  ldfld      float64 MCCTest.VType7::f240
    IL_12b4:  ldarga.s   val
    IL_12b6:  ldfld      float64 MCCTest.VType7::f240
    IL_12bb:  add
    IL_12bc:  stfld      float64 MCCTest.VType7::f240
    IL_12c1:  ldarg.0
    IL_12c2:  dup
    IL_12c3:  ldfld      float64 MCCTest.VType7::f241
    IL_12c8:  ldarga.s   val
    IL_12ca:  ldfld      float64 MCCTest.VType7::f241
    IL_12cf:  add
    IL_12d0:  stfld      float64 MCCTest.VType7::f241
    IL_12d5:  ldarg.0
    IL_12d6:  dup
    IL_12d7:  ldfld      float64 MCCTest.VType7::f242
    IL_12dc:  ldarga.s   val
    IL_12de:  ldfld      float64 MCCTest.VType7::f242
    IL_12e3:  add
    IL_12e4:  stfld      float64 MCCTest.VType7::f242
    IL_12e9:  ldarg.0
    IL_12ea:  dup
    IL_12eb:  ldfld      float64 MCCTest.VType7::f243
    IL_12f0:  ldarga.s   val
    IL_12f2:  ldfld      float64 MCCTest.VType7::f243
    IL_12f7:  add
    IL_12f8:  stfld      float64 MCCTest.VType7::f243
    IL_12fd:  ldarg.0
    IL_12fe:  dup
    IL_12ff:  ldfld      float64 MCCTest.VType7::f244
    IL_1304:  ldarga.s   val
    IL_1306:  ldfld      float64 MCCTest.VType7::f244
    IL_130b:  add
    IL_130c:  stfld      float64 MCCTest.VType7::f244
    IL_1311:  ldarg.0
    IL_1312:  dup
    IL_1313:  ldfld      float64 MCCTest.VType7::f245
    IL_1318:  ldarga.s   val
    IL_131a:  ldfld      float64 MCCTest.VType7::f245
    IL_131f:  add
    IL_1320:  stfld      float64 MCCTest.VType7::f245
    IL_1325:  ldarg.0
    IL_1326:  dup
    IL_1327:  ldfld      float64 MCCTest.VType7::f246
    IL_132c:  ldarga.s   val
    IL_132e:  ldfld      float64 MCCTest.VType7::f246
    IL_1333:  add
    IL_1334:  stfld      float64 MCCTest.VType7::f246
    IL_1339:  ldarg.0
    IL_133a:  dup
    IL_133b:  ldfld      float64 MCCTest.VType7::f247
    IL_1340:  ldarga.s   val
    IL_1342:  ldfld      float64 MCCTest.VType7::f247
    IL_1347:  add
    IL_1348:  stfld      float64 MCCTest.VType7::f247
    IL_134d:  ldarg.0
    IL_134e:  dup
    IL_134f:  ldfld      float64 MCCTest.VType7::f248
    IL_1354:  ldarga.s   val
    IL_1356:  ldfld      float64 MCCTest.VType7::f248
    IL_135b:  add
    IL_135c:  stfld      float64 MCCTest.VType7::f248
    IL_1361:  ldarg.0
    IL_1362:  dup
    IL_1363:  ldfld      float64 MCCTest.VType7::f249
    IL_1368:  ldarga.s   val
    IL_136a:  ldfld      float64 MCCTest.VType7::f249
    IL_136f:  add
    IL_1370:  stfld      float64 MCCTest.VType7::f249
    IL_1375:  ldarg.0
    IL_1376:  dup
    IL_1377:  ldfld      float64 MCCTest.VType7::f250
    IL_137c:  ldarga.s   val
    IL_137e:  ldfld      float64 MCCTest.VType7::f250
    IL_1383:  add
    IL_1384:  stfld      float64 MCCTest.VType7::f250
    IL_1389:  ldarg.0
    IL_138a:  dup
    IL_138b:  ldfld      float64 MCCTest.VType7::f251
    IL_1390:  ldarga.s   val
    IL_1392:  ldfld      float64 MCCTest.VType7::f251
    IL_1397:  add
    IL_1398:  stfld      float64 MCCTest.VType7::f251
    IL_139d:  ldarg.0
    IL_139e:  dup
    IL_139f:  ldfld      float64 MCCTest.VType7::f252
    IL_13a4:  ldarga.s   val
    IL_13a6:  ldfld      float64 MCCTest.VType7::f252
    IL_13ab:  add
    IL_13ac:  stfld      float64 MCCTest.VType7::f252
    IL_13b1:  ldarg.0
    IL_13b2:  dup
    IL_13b3:  ldfld      float64 MCCTest.VType7::f253
    IL_13b8:  ldarga.s   val
    IL_13ba:  ldfld      float64 MCCTest.VType7::f253
    IL_13bf:  add
    IL_13c0:  stfld      float64 MCCTest.VType7::f253
    IL_13c5:  ldarg.0
    IL_13c6:  dup
    IL_13c7:  ldfld      float64 MCCTest.VType7::f254
    IL_13cc:  ldarga.s   val
    IL_13ce:  ldfld      float64 MCCTest.VType7::f254
    IL_13d3:  add
    IL_13d4:  stfld      float64 MCCTest.VType7::f254
    IL_13d9:  ldarg.0
    IL_13da:  dup
    IL_13db:  ldfld      float64 MCCTest.VType7::f255
    IL_13e0:  ldarga.s   val
    IL_13e2:  ldfld      float64 MCCTest.VType7::f255
    IL_13e7:  add
    IL_13e8:  stfld      float64 MCCTest.VType7::f255
    IL_13ed:  ldarg.0
    IL_13ee:  dup
    IL_13ef:  ldfld      float64 MCCTest.VType7::f256
    IL_13f4:  ldarga.s   val
    IL_13f6:  ldfld      float64 MCCTest.VType7::f256
    IL_13fb:  add
    IL_13fc:  stfld      float64 MCCTest.VType7::f256
    IL_1401:  ldarg.0
    IL_1402:  dup
    IL_1403:  ldfld      float64 MCCTest.VType7::f257
    IL_1408:  ldarga.s   val
    IL_140a:  ldfld      float64 MCCTest.VType7::f257
    IL_140f:  add
    IL_1410:  stfld      float64 MCCTest.VType7::f257
    IL_1415:  ret
  } // end of method VType7::Add

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VType7 expected) cil managed
  {
    // Code size       11310 (0x2c2e)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      float64 MCCTest.VType7::f1
    IL_0007:  ldarga.s   expected
    IL_0009:  ldfld      float64 MCCTest.VType7::f1
    IL_000e:  ceq
    IL_0010:  stloc.0
    IL_0011:  ldloc.0
    IL_0012:  brtrue.s   IL_002d

    IL_0014:  nop
    IL_0015:  ldstr      "f1"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      float64 MCCTest.VType7::f1
    IL_0020:  ldarga.s   expected
    IL_0022:  ldfld      float64 MCCTest.VType7::f1
    IL_0027:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_002c:  throw

    IL_002d:  ldarg.0
    IL_002e:  ldfld      float64 MCCTest.VType7::f2
    IL_0033:  ldarga.s   expected
    IL_0035:  ldfld      float64 MCCTest.VType7::f2
    IL_003a:  ceq
    IL_003c:  stloc.0
    IL_003d:  ldloc.0
    IL_003e:  brtrue.s   IL_0059

    IL_0040:  nop
    IL_0041:  ldstr      "f2"
    IL_0046:  ldarg.0
    IL_0047:  ldfld      float64 MCCTest.VType7::f2
    IL_004c:  ldarga.s   expected
    IL_004e:  ldfld      float64 MCCTest.VType7::f2
    IL_0053:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0058:  throw

    IL_0059:  ldarg.0
    IL_005a:  ldfld      float64 MCCTest.VType7::f3
    IL_005f:  ldarga.s   expected
    IL_0061:  ldfld      float64 MCCTest.VType7::f3
    IL_0066:  ceq
    IL_0068:  stloc.0
    IL_0069:  ldloc.0
    IL_006a:  brtrue.s   IL_0085

    IL_006c:  nop
    IL_006d:  ldstr      "f3"
    IL_0072:  ldarg.0
    IL_0073:  ldfld      float64 MCCTest.VType7::f3
    IL_0078:  ldarga.s   expected
    IL_007a:  ldfld      float64 MCCTest.VType7::f3
    IL_007f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0084:  throw

    IL_0085:  ldarg.0
    IL_0086:  ldfld      float64 MCCTest.VType7::f4
    IL_008b:  ldarga.s   expected
    IL_008d:  ldfld      float64 MCCTest.VType7::f4
    IL_0092:  ceq
    IL_0094:  stloc.0
    IL_0095:  ldloc.0
    IL_0096:  brtrue.s   IL_00b1

    IL_0098:  nop
    IL_0099:  ldstr      "f4"
    IL_009e:  ldarg.0
    IL_009f:  ldfld      float64 MCCTest.VType7::f4
    IL_00a4:  ldarga.s   expected
    IL_00a6:  ldfld      float64 MCCTest.VType7::f4
    IL_00ab:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00b0:  throw

    IL_00b1:  ldarg.0
    IL_00b2:  ldfld      float64 MCCTest.VType7::f5
    IL_00b7:  ldarga.s   expected
    IL_00b9:  ldfld      float64 MCCTest.VType7::f5
    IL_00be:  ceq
    IL_00c0:  stloc.0
    IL_00c1:  ldloc.0
    IL_00c2:  brtrue.s   IL_00dd

    IL_00c4:  nop
    IL_00c5:  ldstr      "f5"
    IL_00ca:  ldarg.0
    IL_00cb:  ldfld      float64 MCCTest.VType7::f5
    IL_00d0:  ldarga.s   expected
    IL_00d2:  ldfld      float64 MCCTest.VType7::f5
    IL_00d7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00dc:  throw

    IL_00dd:  ldarg.0
    IL_00de:  ldfld      float64 MCCTest.VType7::f6
    IL_00e3:  ldarga.s   expected
    IL_00e5:  ldfld      float64 MCCTest.VType7::f6
    IL_00ea:  ceq
    IL_00ec:  stloc.0
    IL_00ed:  ldloc.0
    IL_00ee:  brtrue.s   IL_0109

    IL_00f0:  nop
    IL_00f1:  ldstr      "f6"
    IL_00f6:  ldarg.0
    IL_00f7:  ldfld      float64 MCCTest.VType7::f6
    IL_00fc:  ldarga.s   expected
    IL_00fe:  ldfld      float64 MCCTest.VType7::f6
    IL_0103:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0108:  throw

    IL_0109:  ldarg.0
    IL_010a:  ldfld      float64 MCCTest.VType7::f7
    IL_010f:  ldarga.s   expected
    IL_0111:  ldfld      float64 MCCTest.VType7::f7
    IL_0116:  ceq
    IL_0118:  stloc.0
    IL_0119:  ldloc.0
    IL_011a:  brtrue.s   IL_0135

    IL_011c:  nop
    IL_011d:  ldstr      "f7"
    IL_0122:  ldarg.0
    IL_0123:  ldfld      float64 MCCTest.VType7::f7
    IL_0128:  ldarga.s   expected
    IL_012a:  ldfld      float64 MCCTest.VType7::f7
    IL_012f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0134:  throw

    IL_0135:  ldarg.0
    IL_0136:  ldfld      float64 MCCTest.VType7::f8
    IL_013b:  ldarga.s   expected
    IL_013d:  ldfld      float64 MCCTest.VType7::f8
    IL_0142:  ceq
    IL_0144:  stloc.0
    IL_0145:  ldloc.0
    IL_0146:  brtrue.s   IL_0161

    IL_0148:  nop
    IL_0149:  ldstr      "f8"
    IL_014e:  ldarg.0
    IL_014f:  ldfld      float64 MCCTest.VType7::f8
    IL_0154:  ldarga.s   expected
    IL_0156:  ldfld      float64 MCCTest.VType7::f8
    IL_015b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0160:  throw

    IL_0161:  ldarg.0
    IL_0162:  ldfld      float64 MCCTest.VType7::f9
    IL_0167:  ldarga.s   expected
    IL_0169:  ldfld      float64 MCCTest.VType7::f9
    IL_016e:  ceq
    IL_0170:  stloc.0
    IL_0171:  ldloc.0
    IL_0172:  brtrue.s   IL_018d

    IL_0174:  nop
    IL_0175:  ldstr      "f9"
    IL_017a:  ldarg.0
    IL_017b:  ldfld      float64 MCCTest.VType7::f9
    IL_0180:  ldarga.s   expected
    IL_0182:  ldfld      float64 MCCTest.VType7::f9
    IL_0187:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_018c:  throw

    IL_018d:  ldarg.0
    IL_018e:  ldfld      float64 MCCTest.VType7::f10
    IL_0193:  ldarga.s   expected
    IL_0195:  ldfld      float64 MCCTest.VType7::f10
    IL_019a:  ceq
    IL_019c:  stloc.0
    IL_019d:  ldloc.0
    IL_019e:  brtrue.s   IL_01b9

    IL_01a0:  nop
    IL_01a1:  ldstr      "f10"
    IL_01a6:  ldarg.0
    IL_01a7:  ldfld      float64 MCCTest.VType7::f10
    IL_01ac:  ldarga.s   expected
    IL_01ae:  ldfld      float64 MCCTest.VType7::f10
    IL_01b3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_01b8:  throw

    IL_01b9:  ldarg.0
    IL_01ba:  ldfld      float64 MCCTest.VType7::f11
    IL_01bf:  ldarga.s   expected
    IL_01c1:  ldfld      float64 MCCTest.VType7::f11
    IL_01c6:  ceq
    IL_01c8:  stloc.0
    IL_01c9:  ldloc.0
    IL_01ca:  brtrue.s   IL_01e5

    IL_01cc:  nop
    IL_01cd:  ldstr      "f11"
    IL_01d2:  ldarg.0
    IL_01d3:  ldfld      float64 MCCTest.VType7::f11
    IL_01d8:  ldarga.s   expected
    IL_01da:  ldfld      float64 MCCTest.VType7::f11
    IL_01df:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_01e4:  throw

    IL_01e5:  ldarg.0
    IL_01e6:  ldfld      float64 MCCTest.VType7::f12
    IL_01eb:  ldarga.s   expected
    IL_01ed:  ldfld      float64 MCCTest.VType7::f12
    IL_01f2:  ceq
    IL_01f4:  stloc.0
    IL_01f5:  ldloc.0
    IL_01f6:  brtrue.s   IL_0211

    IL_01f8:  nop
    IL_01f9:  ldstr      "f12"
    IL_01fe:  ldarg.0
    IL_01ff:  ldfld      float64 MCCTest.VType7::f12
    IL_0204:  ldarga.s   expected
    IL_0206:  ldfld      float64 MCCTest.VType7::f12
    IL_020b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0210:  throw

    IL_0211:  ldarg.0
    IL_0212:  ldfld      float64 MCCTest.VType7::f13
    IL_0217:  ldarga.s   expected
    IL_0219:  ldfld      float64 MCCTest.VType7::f13
    IL_021e:  ceq
    IL_0220:  stloc.0
    IL_0221:  ldloc.0
    IL_0222:  brtrue.s   IL_023d

    IL_0224:  nop
    IL_0225:  ldstr      "f13"
    IL_022a:  ldarg.0
    IL_022b:  ldfld      float64 MCCTest.VType7::f13
    IL_0230:  ldarga.s   expected
    IL_0232:  ldfld      float64 MCCTest.VType7::f13
    IL_0237:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_023c:  throw

    IL_023d:  ldarg.0
    IL_023e:  ldfld      float64 MCCTest.VType7::f14
    IL_0243:  ldarga.s   expected
    IL_0245:  ldfld      float64 MCCTest.VType7::f14
    IL_024a:  ceq
    IL_024c:  stloc.0
    IL_024d:  ldloc.0
    IL_024e:  brtrue.s   IL_0269

    IL_0250:  nop
    IL_0251:  ldstr      "f14"
    IL_0256:  ldarg.0
    IL_0257:  ldfld      float64 MCCTest.VType7::f14
    IL_025c:  ldarga.s   expected
    IL_025e:  ldfld      float64 MCCTest.VType7::f14
    IL_0263:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0268:  throw

    IL_0269:  ldarg.0
    IL_026a:  ldfld      float64 MCCTest.VType7::f15
    IL_026f:  ldarga.s   expected
    IL_0271:  ldfld      float64 MCCTest.VType7::f15
    IL_0276:  ceq
    IL_0278:  stloc.0
    IL_0279:  ldloc.0
    IL_027a:  brtrue.s   IL_0295

    IL_027c:  nop
    IL_027d:  ldstr      "f15"
    IL_0282:  ldarg.0
    IL_0283:  ldfld      float64 MCCTest.VType7::f15
    IL_0288:  ldarga.s   expected
    IL_028a:  ldfld      float64 MCCTest.VType7::f15
    IL_028f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0294:  throw

    IL_0295:  ldarg.0
    IL_0296:  ldfld      float64 MCCTest.VType7::f16
    IL_029b:  ldarga.s   expected
    IL_029d:  ldfld      float64 MCCTest.VType7::f16
    IL_02a2:  ceq
    IL_02a4:  stloc.0
    IL_02a5:  ldloc.0
    IL_02a6:  brtrue.s   IL_02c1

    IL_02a8:  nop
    IL_02a9:  ldstr      "f16"
    IL_02ae:  ldarg.0
    IL_02af:  ldfld      float64 MCCTest.VType7::f16
    IL_02b4:  ldarga.s   expected
    IL_02b6:  ldfld      float64 MCCTest.VType7::f16
    IL_02bb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_02c0:  throw

    IL_02c1:  ldarg.0
    IL_02c2:  ldfld      float64 MCCTest.VType7::f17
    IL_02c7:  ldarga.s   expected
    IL_02c9:  ldfld      float64 MCCTest.VType7::f17
    IL_02ce:  ceq
    IL_02d0:  stloc.0
    IL_02d1:  ldloc.0
    IL_02d2:  brtrue.s   IL_02ed

    IL_02d4:  nop
    IL_02d5:  ldstr      "f17"
    IL_02da:  ldarg.0
    IL_02db:  ldfld      float64 MCCTest.VType7::f17
    IL_02e0:  ldarga.s   expected
    IL_02e2:  ldfld      float64 MCCTest.VType7::f17
    IL_02e7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_02ec:  throw

    IL_02ed:  ldarg.0
    IL_02ee:  ldfld      float64 MCCTest.VType7::f18
    IL_02f3:  ldarga.s   expected
    IL_02f5:  ldfld      float64 MCCTest.VType7::f18
    IL_02fa:  ceq
    IL_02fc:  stloc.0
    IL_02fd:  ldloc.0
    IL_02fe:  brtrue.s   IL_0319

    IL_0300:  nop
    IL_0301:  ldstr      "f18"
    IL_0306:  ldarg.0
    IL_0307:  ldfld      float64 MCCTest.VType7::f18
    IL_030c:  ldarga.s   expected
    IL_030e:  ldfld      float64 MCCTest.VType7::f18
    IL_0313:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0318:  throw

    IL_0319:  ldarg.0
    IL_031a:  ldfld      float64 MCCTest.VType7::f19
    IL_031f:  ldarga.s   expected
    IL_0321:  ldfld      float64 MCCTest.VType7::f19
    IL_0326:  ceq
    IL_0328:  stloc.0
    IL_0329:  ldloc.0
    IL_032a:  brtrue.s   IL_0345

    IL_032c:  nop
    IL_032d:  ldstr      "f19"
    IL_0332:  ldarg.0
    IL_0333:  ldfld      float64 MCCTest.VType7::f19
    IL_0338:  ldarga.s   expected
    IL_033a:  ldfld      float64 MCCTest.VType7::f19
    IL_033f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0344:  throw

    IL_0345:  ldarg.0
    IL_0346:  ldfld      float64 MCCTest.VType7::f20
    IL_034b:  ldarga.s   expected
    IL_034d:  ldfld      float64 MCCTest.VType7::f20
    IL_0352:  ceq
    IL_0354:  stloc.0
    IL_0355:  ldloc.0
    IL_0356:  brtrue.s   IL_0371

    IL_0358:  nop
    IL_0359:  ldstr      "f20"
    IL_035e:  ldarg.0
    IL_035f:  ldfld      float64 MCCTest.VType7::f20
    IL_0364:  ldarga.s   expected
    IL_0366:  ldfld      float64 MCCTest.VType7::f20
    IL_036b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0370:  throw

    IL_0371:  ldarg.0
    IL_0372:  ldfld      float64 MCCTest.VType7::f21
    IL_0377:  ldarga.s   expected
    IL_0379:  ldfld      float64 MCCTest.VType7::f21
    IL_037e:  ceq
    IL_0380:  stloc.0
    IL_0381:  ldloc.0
    IL_0382:  brtrue.s   IL_039d

    IL_0384:  nop
    IL_0385:  ldstr      "f21"
    IL_038a:  ldarg.0
    IL_038b:  ldfld      float64 MCCTest.VType7::f21
    IL_0390:  ldarga.s   expected
    IL_0392:  ldfld      float64 MCCTest.VType7::f21
    IL_0397:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_039c:  throw

    IL_039d:  ldarg.0
    IL_039e:  ldfld      float64 MCCTest.VType7::f22
    IL_03a3:  ldarga.s   expected
    IL_03a5:  ldfld      float64 MCCTest.VType7::f22
    IL_03aa:  ceq
    IL_03ac:  stloc.0
    IL_03ad:  ldloc.0
    IL_03ae:  brtrue.s   IL_03c9

    IL_03b0:  nop
    IL_03b1:  ldstr      "f22"
    IL_03b6:  ldarg.0
    IL_03b7:  ldfld      float64 MCCTest.VType7::f22
    IL_03bc:  ldarga.s   expected
    IL_03be:  ldfld      float64 MCCTest.VType7::f22
    IL_03c3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_03c8:  throw

    IL_03c9:  ldarg.0
    IL_03ca:  ldfld      float64 MCCTest.VType7::f23
    IL_03cf:  ldarga.s   expected
    IL_03d1:  ldfld      float64 MCCTest.VType7::f23
    IL_03d6:  ceq
    IL_03d8:  stloc.0
    IL_03d9:  ldloc.0
    IL_03da:  brtrue.s   IL_03f5

    IL_03dc:  nop
    IL_03dd:  ldstr      "f23"
    IL_03e2:  ldarg.0
    IL_03e3:  ldfld      float64 MCCTest.VType7::f23
    IL_03e8:  ldarga.s   expected
    IL_03ea:  ldfld      float64 MCCTest.VType7::f23
    IL_03ef:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_03f4:  throw

    IL_03f5:  ldarg.0
    IL_03f6:  ldfld      float64 MCCTest.VType7::f24
    IL_03fb:  ldarga.s   expected
    IL_03fd:  ldfld      float64 MCCTest.VType7::f24
    IL_0402:  ceq
    IL_0404:  stloc.0
    IL_0405:  ldloc.0
    IL_0406:  brtrue.s   IL_0421

    IL_0408:  nop
    IL_0409:  ldstr      "f24"
    IL_040e:  ldarg.0
    IL_040f:  ldfld      float64 MCCTest.VType7::f24
    IL_0414:  ldarga.s   expected
    IL_0416:  ldfld      float64 MCCTest.VType7::f24
    IL_041b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0420:  throw

    IL_0421:  ldarg.0
    IL_0422:  ldfld      float64 MCCTest.VType7::f25
    IL_0427:  ldarga.s   expected
    IL_0429:  ldfld      float64 MCCTest.VType7::f25
    IL_042e:  ceq
    IL_0430:  stloc.0
    IL_0431:  ldloc.0
    IL_0432:  brtrue.s   IL_044d

    IL_0434:  nop
    IL_0435:  ldstr      "f25"
    IL_043a:  ldarg.0
    IL_043b:  ldfld      float64 MCCTest.VType7::f25
    IL_0440:  ldarga.s   expected
    IL_0442:  ldfld      float64 MCCTest.VType7::f25
    IL_0447:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_044c:  throw

    IL_044d:  ldarg.0
    IL_044e:  ldfld      float64 MCCTest.VType7::f26
    IL_0453:  ldarga.s   expected
    IL_0455:  ldfld      float64 MCCTest.VType7::f26
    IL_045a:  ceq
    IL_045c:  stloc.0
    IL_045d:  ldloc.0
    IL_045e:  brtrue.s   IL_0479

    IL_0460:  nop
    IL_0461:  ldstr      "f26"
    IL_0466:  ldarg.0
    IL_0467:  ldfld      float64 MCCTest.VType7::f26
    IL_046c:  ldarga.s   expected
    IL_046e:  ldfld      float64 MCCTest.VType7::f26
    IL_0473:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0478:  throw

    IL_0479:  ldarg.0
    IL_047a:  ldfld      float64 MCCTest.VType7::f27
    IL_047f:  ldarga.s   expected
    IL_0481:  ldfld      float64 MCCTest.VType7::f27
    IL_0486:  ceq
    IL_0488:  stloc.0
    IL_0489:  ldloc.0
    IL_048a:  brtrue.s   IL_04a5

    IL_048c:  nop
    IL_048d:  ldstr      "f27"
    IL_0492:  ldarg.0
    IL_0493:  ldfld      float64 MCCTest.VType7::f27
    IL_0498:  ldarga.s   expected
    IL_049a:  ldfld      float64 MCCTest.VType7::f27
    IL_049f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_04a4:  throw

    IL_04a5:  ldarg.0
    IL_04a6:  ldfld      float64 MCCTest.VType7::f28
    IL_04ab:  ldarga.s   expected
    IL_04ad:  ldfld      float64 MCCTest.VType7::f28
    IL_04b2:  ceq
    IL_04b4:  stloc.0
    IL_04b5:  ldloc.0
    IL_04b6:  brtrue.s   IL_04d1

    IL_04b8:  nop
    IL_04b9:  ldstr      "f28"
    IL_04be:  ldarg.0
    IL_04bf:  ldfld      float64 MCCTest.VType7::f28
    IL_04c4:  ldarga.s   expected
    IL_04c6:  ldfld      float64 MCCTest.VType7::f28
    IL_04cb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_04d0:  throw

    IL_04d1:  ldarg.0
    IL_04d2:  ldfld      float64 MCCTest.VType7::f29
    IL_04d7:  ldarga.s   expected
    IL_04d9:  ldfld      float64 MCCTest.VType7::f29
    IL_04de:  ceq
    IL_04e0:  stloc.0
    IL_04e1:  ldloc.0
    IL_04e2:  brtrue.s   IL_04fd

    IL_04e4:  nop
    IL_04e5:  ldstr      "f29"
    IL_04ea:  ldarg.0
    IL_04eb:  ldfld      float64 MCCTest.VType7::f29
    IL_04f0:  ldarga.s   expected
    IL_04f2:  ldfld      float64 MCCTest.VType7::f29
    IL_04f7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_04fc:  throw

    IL_04fd:  ldarg.0
    IL_04fe:  ldfld      float64 MCCTest.VType7::f30
    IL_0503:  ldarga.s   expected
    IL_0505:  ldfld      float64 MCCTest.VType7::f30
    IL_050a:  ceq
    IL_050c:  stloc.0
    IL_050d:  ldloc.0
    IL_050e:  brtrue.s   IL_0529

    IL_0510:  nop
    IL_0511:  ldstr      "f30"
    IL_0516:  ldarg.0
    IL_0517:  ldfld      float64 MCCTest.VType7::f30
    IL_051c:  ldarga.s   expected
    IL_051e:  ldfld      float64 MCCTest.VType7::f30
    IL_0523:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0528:  throw

    IL_0529:  ldarg.0
    IL_052a:  ldfld      float64 MCCTest.VType7::f31
    IL_052f:  ldarga.s   expected
    IL_0531:  ldfld      float64 MCCTest.VType7::f31
    IL_0536:  ceq
    IL_0538:  stloc.0
    IL_0539:  ldloc.0
    IL_053a:  brtrue.s   IL_0555

    IL_053c:  nop
    IL_053d:  ldstr      "f31"
    IL_0542:  ldarg.0
    IL_0543:  ldfld      float64 MCCTest.VType7::f31
    IL_0548:  ldarga.s   expected
    IL_054a:  ldfld      float64 MCCTest.VType7::f31
    IL_054f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0554:  throw

    IL_0555:  ldarg.0
    IL_0556:  ldfld      float64 MCCTest.VType7::f32
    IL_055b:  ldarga.s   expected
    IL_055d:  ldfld      float64 MCCTest.VType7::f32
    IL_0562:  ceq
    IL_0564:  stloc.0
    IL_0565:  ldloc.0
    IL_0566:  brtrue.s   IL_0581

    IL_0568:  nop
    IL_0569:  ldstr      "f32"
    IL_056e:  ldarg.0
    IL_056f:  ldfld      float64 MCCTest.VType7::f32
    IL_0574:  ldarga.s   expected
    IL_0576:  ldfld      float64 MCCTest.VType7::f32
    IL_057b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0580:  throw

    IL_0581:  ldarg.0
    IL_0582:  ldfld      float64 MCCTest.VType7::f33
    IL_0587:  ldarga.s   expected
    IL_0589:  ldfld      float64 MCCTest.VType7::f33
    IL_058e:  ceq
    IL_0590:  stloc.0
    IL_0591:  ldloc.0
    IL_0592:  brtrue.s   IL_05ad

    IL_0594:  nop
    IL_0595:  ldstr      "f33"
    IL_059a:  ldarg.0
    IL_059b:  ldfld      float64 MCCTest.VType7::f33
    IL_05a0:  ldarga.s   expected
    IL_05a2:  ldfld      float64 MCCTest.VType7::f33
    IL_05a7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_05ac:  throw

    IL_05ad:  ldarg.0
    IL_05ae:  ldfld      float64 MCCTest.VType7::f34
    IL_05b3:  ldarga.s   expected
    IL_05b5:  ldfld      float64 MCCTest.VType7::f34
    IL_05ba:  ceq
    IL_05bc:  stloc.0
    IL_05bd:  ldloc.0
    IL_05be:  brtrue.s   IL_05d9

    IL_05c0:  nop
    IL_05c1:  ldstr      "f34"
    IL_05c6:  ldarg.0
    IL_05c7:  ldfld      float64 MCCTest.VType7::f34
    IL_05cc:  ldarga.s   expected
    IL_05ce:  ldfld      float64 MCCTest.VType7::f34
    IL_05d3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_05d8:  throw

    IL_05d9:  ldarg.0
    IL_05da:  ldfld      float64 MCCTest.VType7::f35
    IL_05df:  ldarga.s   expected
    IL_05e1:  ldfld      float64 MCCTest.VType7::f35
    IL_05e6:  ceq
    IL_05e8:  stloc.0
    IL_05e9:  ldloc.0
    IL_05ea:  brtrue.s   IL_0605

    IL_05ec:  nop
    IL_05ed:  ldstr      "f35"
    IL_05f2:  ldarg.0
    IL_05f3:  ldfld      float64 MCCTest.VType7::f35
    IL_05f8:  ldarga.s   expected
    IL_05fa:  ldfld      float64 MCCTest.VType7::f35
    IL_05ff:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0604:  throw

    IL_0605:  ldarg.0
    IL_0606:  ldfld      float64 MCCTest.VType7::f36
    IL_060b:  ldarga.s   expected
    IL_060d:  ldfld      float64 MCCTest.VType7::f36
    IL_0612:  ceq
    IL_0614:  stloc.0
    IL_0615:  ldloc.0
    IL_0616:  brtrue.s   IL_0631

    IL_0618:  nop
    IL_0619:  ldstr      "f36"
    IL_061e:  ldarg.0
    IL_061f:  ldfld      float64 MCCTest.VType7::f36
    IL_0624:  ldarga.s   expected
    IL_0626:  ldfld      float64 MCCTest.VType7::f36
    IL_062b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0630:  throw

    IL_0631:  ldarg.0
    IL_0632:  ldfld      float64 MCCTest.VType7::f37
    IL_0637:  ldarga.s   expected
    IL_0639:  ldfld      float64 MCCTest.VType7::f37
    IL_063e:  ceq
    IL_0640:  stloc.0
    IL_0641:  ldloc.0
    IL_0642:  brtrue.s   IL_065d

    IL_0644:  nop
    IL_0645:  ldstr      "f37"
    IL_064a:  ldarg.0
    IL_064b:  ldfld      float64 MCCTest.VType7::f37
    IL_0650:  ldarga.s   expected
    IL_0652:  ldfld      float64 MCCTest.VType7::f37
    IL_0657:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_065c:  throw

    IL_065d:  ldarg.0
    IL_065e:  ldfld      float64 MCCTest.VType7::f38
    IL_0663:  ldarga.s   expected
    IL_0665:  ldfld      float64 MCCTest.VType7::f38
    IL_066a:  ceq
    IL_066c:  stloc.0
    IL_066d:  ldloc.0
    IL_066e:  brtrue.s   IL_0689

    IL_0670:  nop
    IL_0671:  ldstr      "f38"
    IL_0676:  ldarg.0
    IL_0677:  ldfld      float64 MCCTest.VType7::f38
    IL_067c:  ldarga.s   expected
    IL_067e:  ldfld      float64 MCCTest.VType7::f38
    IL_0683:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0688:  throw

    IL_0689:  ldarg.0
    IL_068a:  ldfld      float64 MCCTest.VType7::f39
    IL_068f:  ldarga.s   expected
    IL_0691:  ldfld      float64 MCCTest.VType7::f39
    IL_0696:  ceq
    IL_0698:  stloc.0
    IL_0699:  ldloc.0
    IL_069a:  brtrue.s   IL_06b5

    IL_069c:  nop
    IL_069d:  ldstr      "f39"
    IL_06a2:  ldarg.0
    IL_06a3:  ldfld      float64 MCCTest.VType7::f39
    IL_06a8:  ldarga.s   expected
    IL_06aa:  ldfld      float64 MCCTest.VType7::f39
    IL_06af:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_06b4:  throw

    IL_06b5:  ldarg.0
    IL_06b6:  ldfld      float64 MCCTest.VType7::f40
    IL_06bb:  ldarga.s   expected
    IL_06bd:  ldfld      float64 MCCTest.VType7::f40
    IL_06c2:  ceq
    IL_06c4:  stloc.0
    IL_06c5:  ldloc.0
    IL_06c6:  brtrue.s   IL_06e1

    IL_06c8:  nop
    IL_06c9:  ldstr      "f40"
    IL_06ce:  ldarg.0
    IL_06cf:  ldfld      float64 MCCTest.VType7::f40
    IL_06d4:  ldarga.s   expected
    IL_06d6:  ldfld      float64 MCCTest.VType7::f40
    IL_06db:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_06e0:  throw

    IL_06e1:  ldarg.0
    IL_06e2:  ldfld      float64 MCCTest.VType7::f41
    IL_06e7:  ldarga.s   expected
    IL_06e9:  ldfld      float64 MCCTest.VType7::f41
    IL_06ee:  ceq
    IL_06f0:  stloc.0
    IL_06f1:  ldloc.0
    IL_06f2:  brtrue.s   IL_070d

    IL_06f4:  nop
    IL_06f5:  ldstr      "f41"
    IL_06fa:  ldarg.0
    IL_06fb:  ldfld      float64 MCCTest.VType7::f41
    IL_0700:  ldarga.s   expected
    IL_0702:  ldfld      float64 MCCTest.VType7::f41
    IL_0707:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_070c:  throw

    IL_070d:  ldarg.0
    IL_070e:  ldfld      float64 MCCTest.VType7::f42
    IL_0713:  ldarga.s   expected
    IL_0715:  ldfld      float64 MCCTest.VType7::f42
    IL_071a:  ceq
    IL_071c:  stloc.0
    IL_071d:  ldloc.0
    IL_071e:  brtrue.s   IL_0739

    IL_0720:  nop
    IL_0721:  ldstr      "f42"
    IL_0726:  ldarg.0
    IL_0727:  ldfld      float64 MCCTest.VType7::f42
    IL_072c:  ldarga.s   expected
    IL_072e:  ldfld      float64 MCCTest.VType7::f42
    IL_0733:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0738:  throw

    IL_0739:  ldarg.0
    IL_073a:  ldfld      float64 MCCTest.VType7::f43
    IL_073f:  ldarga.s   expected
    IL_0741:  ldfld      float64 MCCTest.VType7::f43
    IL_0746:  ceq
    IL_0748:  stloc.0
    IL_0749:  ldloc.0
    IL_074a:  brtrue.s   IL_0765

    IL_074c:  nop
    IL_074d:  ldstr      "f43"
    IL_0752:  ldarg.0
    IL_0753:  ldfld      float64 MCCTest.VType7::f43
    IL_0758:  ldarga.s   expected
    IL_075a:  ldfld      float64 MCCTest.VType7::f43
    IL_075f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0764:  throw

    IL_0765:  ldarg.0
    IL_0766:  ldfld      float64 MCCTest.VType7::f44
    IL_076b:  ldarga.s   expected
    IL_076d:  ldfld      float64 MCCTest.VType7::f44
    IL_0772:  ceq
    IL_0774:  stloc.0
    IL_0775:  ldloc.0
    IL_0776:  brtrue.s   IL_0791

    IL_0778:  nop
    IL_0779:  ldstr      "f44"
    IL_077e:  ldarg.0
    IL_077f:  ldfld      float64 MCCTest.VType7::f44
    IL_0784:  ldarga.s   expected
    IL_0786:  ldfld      float64 MCCTest.VType7::f44
    IL_078b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0790:  throw

    IL_0791:  ldarg.0
    IL_0792:  ldfld      float64 MCCTest.VType7::f45
    IL_0797:  ldarga.s   expected
    IL_0799:  ldfld      float64 MCCTest.VType7::f45
    IL_079e:  ceq
    IL_07a0:  stloc.0
    IL_07a1:  ldloc.0
    IL_07a2:  brtrue.s   IL_07bd

    IL_07a4:  nop
    IL_07a5:  ldstr      "f45"
    IL_07aa:  ldarg.0
    IL_07ab:  ldfld      float64 MCCTest.VType7::f45
    IL_07b0:  ldarga.s   expected
    IL_07b2:  ldfld      float64 MCCTest.VType7::f45
    IL_07b7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_07bc:  throw

    IL_07bd:  ldarg.0
    IL_07be:  ldfld      float64 MCCTest.VType7::f46
    IL_07c3:  ldarga.s   expected
    IL_07c5:  ldfld      float64 MCCTest.VType7::f46
    IL_07ca:  ceq
    IL_07cc:  stloc.0
    IL_07cd:  ldloc.0
    IL_07ce:  brtrue.s   IL_07e9

    IL_07d0:  nop
    IL_07d1:  ldstr      "f46"
    IL_07d6:  ldarg.0
    IL_07d7:  ldfld      float64 MCCTest.VType7::f46
    IL_07dc:  ldarga.s   expected
    IL_07de:  ldfld      float64 MCCTest.VType7::f46
    IL_07e3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_07e8:  throw

    IL_07e9:  ldarg.0
    IL_07ea:  ldfld      float64 MCCTest.VType7::f47
    IL_07ef:  ldarga.s   expected
    IL_07f1:  ldfld      float64 MCCTest.VType7::f47
    IL_07f6:  ceq
    IL_07f8:  stloc.0
    IL_07f9:  ldloc.0
    IL_07fa:  brtrue.s   IL_0815

    IL_07fc:  nop
    IL_07fd:  ldstr      "f47"
    IL_0802:  ldarg.0
    IL_0803:  ldfld      float64 MCCTest.VType7::f47
    IL_0808:  ldarga.s   expected
    IL_080a:  ldfld      float64 MCCTest.VType7::f47
    IL_080f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0814:  throw

    IL_0815:  ldarg.0
    IL_0816:  ldfld      float64 MCCTest.VType7::f48
    IL_081b:  ldarga.s   expected
    IL_081d:  ldfld      float64 MCCTest.VType7::f48
    IL_0822:  ceq
    IL_0824:  stloc.0
    IL_0825:  ldloc.0
    IL_0826:  brtrue.s   IL_0841

    IL_0828:  nop
    IL_0829:  ldstr      "f48"
    IL_082e:  ldarg.0
    IL_082f:  ldfld      float64 MCCTest.VType7::f48
    IL_0834:  ldarga.s   expected
    IL_0836:  ldfld      float64 MCCTest.VType7::f48
    IL_083b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0840:  throw

    IL_0841:  ldarg.0
    IL_0842:  ldfld      float64 MCCTest.VType7::f49
    IL_0847:  ldarga.s   expected
    IL_0849:  ldfld      float64 MCCTest.VType7::f49
    IL_084e:  ceq
    IL_0850:  stloc.0
    IL_0851:  ldloc.0
    IL_0852:  brtrue.s   IL_086d

    IL_0854:  nop
    IL_0855:  ldstr      "f49"
    IL_085a:  ldarg.0
    IL_085b:  ldfld      float64 MCCTest.VType7::f49
    IL_0860:  ldarga.s   expected
    IL_0862:  ldfld      float64 MCCTest.VType7::f49
    IL_0867:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_086c:  throw

    IL_086d:  ldarg.0
    IL_086e:  ldfld      float64 MCCTest.VType7::f50
    IL_0873:  ldarga.s   expected
    IL_0875:  ldfld      float64 MCCTest.VType7::f50
    IL_087a:  ceq
    IL_087c:  stloc.0
    IL_087d:  ldloc.0
    IL_087e:  brtrue.s   IL_0899

    IL_0880:  nop
    IL_0881:  ldstr      "f50"
    IL_0886:  ldarg.0
    IL_0887:  ldfld      float64 MCCTest.VType7::f50
    IL_088c:  ldarga.s   expected
    IL_088e:  ldfld      float64 MCCTest.VType7::f50
    IL_0893:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0898:  throw

    IL_0899:  ldarg.0
    IL_089a:  ldfld      float64 MCCTest.VType7::f51
    IL_089f:  ldarga.s   expected
    IL_08a1:  ldfld      float64 MCCTest.VType7::f51
    IL_08a6:  ceq
    IL_08a8:  stloc.0
    IL_08a9:  ldloc.0
    IL_08aa:  brtrue.s   IL_08c5

    IL_08ac:  nop
    IL_08ad:  ldstr      "f51"
    IL_08b2:  ldarg.0
    IL_08b3:  ldfld      float64 MCCTest.VType7::f51
    IL_08b8:  ldarga.s   expected
    IL_08ba:  ldfld      float64 MCCTest.VType7::f51
    IL_08bf:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_08c4:  throw

    IL_08c5:  ldarg.0
    IL_08c6:  ldfld      float64 MCCTest.VType7::f52
    IL_08cb:  ldarga.s   expected
    IL_08cd:  ldfld      float64 MCCTest.VType7::f52
    IL_08d2:  ceq
    IL_08d4:  stloc.0
    IL_08d5:  ldloc.0
    IL_08d6:  brtrue.s   IL_08f1

    IL_08d8:  nop
    IL_08d9:  ldstr      "f52"
    IL_08de:  ldarg.0
    IL_08df:  ldfld      float64 MCCTest.VType7::f52
    IL_08e4:  ldarga.s   expected
    IL_08e6:  ldfld      float64 MCCTest.VType7::f52
    IL_08eb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_08f0:  throw

    IL_08f1:  ldarg.0
    IL_08f2:  ldfld      float64 MCCTest.VType7::f53
    IL_08f7:  ldarga.s   expected
    IL_08f9:  ldfld      float64 MCCTest.VType7::f53
    IL_08fe:  ceq
    IL_0900:  stloc.0
    IL_0901:  ldloc.0
    IL_0902:  brtrue.s   IL_091d

    IL_0904:  nop
    IL_0905:  ldstr      "f53"
    IL_090a:  ldarg.0
    IL_090b:  ldfld      float64 MCCTest.VType7::f53
    IL_0910:  ldarga.s   expected
    IL_0912:  ldfld      float64 MCCTest.VType7::f53
    IL_0917:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_091c:  throw

    IL_091d:  ldarg.0
    IL_091e:  ldfld      float64 MCCTest.VType7::f54
    IL_0923:  ldarga.s   expected
    IL_0925:  ldfld      float64 MCCTest.VType7::f54
    IL_092a:  ceq
    IL_092c:  stloc.0
    IL_092d:  ldloc.0
    IL_092e:  brtrue.s   IL_0949

    IL_0930:  nop
    IL_0931:  ldstr      "f54"
    IL_0936:  ldarg.0
    IL_0937:  ldfld      float64 MCCTest.VType7::f54
    IL_093c:  ldarga.s   expected
    IL_093e:  ldfld      float64 MCCTest.VType7::f54
    IL_0943:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0948:  throw

    IL_0949:  ldarg.0
    IL_094a:  ldfld      float64 MCCTest.VType7::f55
    IL_094f:  ldarga.s   expected
    IL_0951:  ldfld      float64 MCCTest.VType7::f55
    IL_0956:  ceq
    IL_0958:  stloc.0
    IL_0959:  ldloc.0
    IL_095a:  brtrue.s   IL_0975

    IL_095c:  nop
    IL_095d:  ldstr      "f55"
    IL_0962:  ldarg.0
    IL_0963:  ldfld      float64 MCCTest.VType7::f55
    IL_0968:  ldarga.s   expected
    IL_096a:  ldfld      float64 MCCTest.VType7::f55
    IL_096f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0974:  throw

    IL_0975:  ldarg.0
    IL_0976:  ldfld      float64 MCCTest.VType7::f56
    IL_097b:  ldarga.s   expected
    IL_097d:  ldfld      float64 MCCTest.VType7::f56
    IL_0982:  ceq
    IL_0984:  stloc.0
    IL_0985:  ldloc.0
    IL_0986:  brtrue.s   IL_09a1

    IL_0988:  nop
    IL_0989:  ldstr      "f56"
    IL_098e:  ldarg.0
    IL_098f:  ldfld      float64 MCCTest.VType7::f56
    IL_0994:  ldarga.s   expected
    IL_0996:  ldfld      float64 MCCTest.VType7::f56
    IL_099b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_09a0:  throw

    IL_09a1:  ldarg.0
    IL_09a2:  ldfld      float64 MCCTest.VType7::f57
    IL_09a7:  ldarga.s   expected
    IL_09a9:  ldfld      float64 MCCTest.VType7::f57
    IL_09ae:  ceq
    IL_09b0:  stloc.0
    IL_09b1:  ldloc.0
    IL_09b2:  brtrue.s   IL_09cd

    IL_09b4:  nop
    IL_09b5:  ldstr      "f57"
    IL_09ba:  ldarg.0
    IL_09bb:  ldfld      float64 MCCTest.VType7::f57
    IL_09c0:  ldarga.s   expected
    IL_09c2:  ldfld      float64 MCCTest.VType7::f57
    IL_09c7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_09cc:  throw

    IL_09cd:  ldarg.0
    IL_09ce:  ldfld      float64 MCCTest.VType7::f58
    IL_09d3:  ldarga.s   expected
    IL_09d5:  ldfld      float64 MCCTest.VType7::f58
    IL_09da:  ceq
    IL_09dc:  stloc.0
    IL_09dd:  ldloc.0
    IL_09de:  brtrue.s   IL_09f9

    IL_09e0:  nop
    IL_09e1:  ldstr      "f58"
    IL_09e6:  ldarg.0
    IL_09e7:  ldfld      float64 MCCTest.VType7::f58
    IL_09ec:  ldarga.s   expected
    IL_09ee:  ldfld      float64 MCCTest.VType7::f58
    IL_09f3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_09f8:  throw

    IL_09f9:  ldarg.0
    IL_09fa:  ldfld      float64 MCCTest.VType7::f59
    IL_09ff:  ldarga.s   expected
    IL_0a01:  ldfld      float64 MCCTest.VType7::f59
    IL_0a06:  ceq
    IL_0a08:  stloc.0
    IL_0a09:  ldloc.0
    IL_0a0a:  brtrue.s   IL_0a25

    IL_0a0c:  nop
    IL_0a0d:  ldstr      "f59"
    IL_0a12:  ldarg.0
    IL_0a13:  ldfld      float64 MCCTest.VType7::f59
    IL_0a18:  ldarga.s   expected
    IL_0a1a:  ldfld      float64 MCCTest.VType7::f59
    IL_0a1f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0a24:  throw

    IL_0a25:  ldarg.0
    IL_0a26:  ldfld      float64 MCCTest.VType7::f60
    IL_0a2b:  ldarga.s   expected
    IL_0a2d:  ldfld      float64 MCCTest.VType7::f60
    IL_0a32:  ceq
    IL_0a34:  stloc.0
    IL_0a35:  ldloc.0
    IL_0a36:  brtrue.s   IL_0a51

    IL_0a38:  nop
    IL_0a39:  ldstr      "f60"
    IL_0a3e:  ldarg.0
    IL_0a3f:  ldfld      float64 MCCTest.VType7::f60
    IL_0a44:  ldarga.s   expected
    IL_0a46:  ldfld      float64 MCCTest.VType7::f60
    IL_0a4b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0a50:  throw

    IL_0a51:  ldarg.0
    IL_0a52:  ldfld      float64 MCCTest.VType7::f61
    IL_0a57:  ldarga.s   expected
    IL_0a59:  ldfld      float64 MCCTest.VType7::f61
    IL_0a5e:  ceq
    IL_0a60:  stloc.0
    IL_0a61:  ldloc.0
    IL_0a62:  brtrue.s   IL_0a7d

    IL_0a64:  nop
    IL_0a65:  ldstr      "f61"
    IL_0a6a:  ldarg.0
    IL_0a6b:  ldfld      float64 MCCTest.VType7::f61
    IL_0a70:  ldarga.s   expected
    IL_0a72:  ldfld      float64 MCCTest.VType7::f61
    IL_0a77:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0a7c:  throw

    IL_0a7d:  ldarg.0
    IL_0a7e:  ldfld      float64 MCCTest.VType7::f62
    IL_0a83:  ldarga.s   expected
    IL_0a85:  ldfld      float64 MCCTest.VType7::f62
    IL_0a8a:  ceq
    IL_0a8c:  stloc.0
    IL_0a8d:  ldloc.0
    IL_0a8e:  brtrue.s   IL_0aa9

    IL_0a90:  nop
    IL_0a91:  ldstr      "f62"
    IL_0a96:  ldarg.0
    IL_0a97:  ldfld      float64 MCCTest.VType7::f62
    IL_0a9c:  ldarga.s   expected
    IL_0a9e:  ldfld      float64 MCCTest.VType7::f62
    IL_0aa3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0aa8:  throw

    IL_0aa9:  ldarg.0
    IL_0aaa:  ldfld      float64 MCCTest.VType7::f63
    IL_0aaf:  ldarga.s   expected
    IL_0ab1:  ldfld      float64 MCCTest.VType7::f63
    IL_0ab6:  ceq
    IL_0ab8:  stloc.0
    IL_0ab9:  ldloc.0
    IL_0aba:  brtrue.s   IL_0ad5

    IL_0abc:  nop
    IL_0abd:  ldstr      "f63"
    IL_0ac2:  ldarg.0
    IL_0ac3:  ldfld      float64 MCCTest.VType7::f63
    IL_0ac8:  ldarga.s   expected
    IL_0aca:  ldfld      float64 MCCTest.VType7::f63
    IL_0acf:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0ad4:  throw

    IL_0ad5:  ldarg.0
    IL_0ad6:  ldfld      float64 MCCTest.VType7::f64
    IL_0adb:  ldarga.s   expected
    IL_0add:  ldfld      float64 MCCTest.VType7::f64
    IL_0ae2:  ceq
    IL_0ae4:  stloc.0
    IL_0ae5:  ldloc.0
    IL_0ae6:  brtrue.s   IL_0b01

    IL_0ae8:  nop
    IL_0ae9:  ldstr      "f64"
    IL_0aee:  ldarg.0
    IL_0aef:  ldfld      float64 MCCTest.VType7::f64
    IL_0af4:  ldarga.s   expected
    IL_0af6:  ldfld      float64 MCCTest.VType7::f64
    IL_0afb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0b00:  throw

    IL_0b01:  ldarg.0
    IL_0b02:  ldfld      float64 MCCTest.VType7::f65
    IL_0b07:  ldarga.s   expected
    IL_0b09:  ldfld      float64 MCCTest.VType7::f65
    IL_0b0e:  ceq
    IL_0b10:  stloc.0
    IL_0b11:  ldloc.0
    IL_0b12:  brtrue.s   IL_0b2d

    IL_0b14:  nop
    IL_0b15:  ldstr      "f65"
    IL_0b1a:  ldarg.0
    IL_0b1b:  ldfld      float64 MCCTest.VType7::f65
    IL_0b20:  ldarga.s   expected
    IL_0b22:  ldfld      float64 MCCTest.VType7::f65
    IL_0b27:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0b2c:  throw

    IL_0b2d:  ldarg.0
    IL_0b2e:  ldfld      float64 MCCTest.VType7::f66
    IL_0b33:  ldarga.s   expected
    IL_0b35:  ldfld      float64 MCCTest.VType7::f66
    IL_0b3a:  ceq
    IL_0b3c:  stloc.0
    IL_0b3d:  ldloc.0
    IL_0b3e:  brtrue.s   IL_0b59

    IL_0b40:  nop
    IL_0b41:  ldstr      "f66"
    IL_0b46:  ldarg.0
    IL_0b47:  ldfld      float64 MCCTest.VType7::f66
    IL_0b4c:  ldarga.s   expected
    IL_0b4e:  ldfld      float64 MCCTest.VType7::f66
    IL_0b53:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0b58:  throw

    IL_0b59:  ldarg.0
    IL_0b5a:  ldfld      float64 MCCTest.VType7::f67
    IL_0b5f:  ldarga.s   expected
    IL_0b61:  ldfld      float64 MCCTest.VType7::f67
    IL_0b66:  ceq
    IL_0b68:  stloc.0
    IL_0b69:  ldloc.0
    IL_0b6a:  brtrue.s   IL_0b85

    IL_0b6c:  nop
    IL_0b6d:  ldstr      "f67"
    IL_0b72:  ldarg.0
    IL_0b73:  ldfld      float64 MCCTest.VType7::f67
    IL_0b78:  ldarga.s   expected
    IL_0b7a:  ldfld      float64 MCCTest.VType7::f67
    IL_0b7f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0b84:  throw

    IL_0b85:  ldarg.0
    IL_0b86:  ldfld      float64 MCCTest.VType7::f68
    IL_0b8b:  ldarga.s   expected
    IL_0b8d:  ldfld      float64 MCCTest.VType7::f68
    IL_0b92:  ceq
    IL_0b94:  stloc.0
    IL_0b95:  ldloc.0
    IL_0b96:  brtrue.s   IL_0bb1

    IL_0b98:  nop
    IL_0b99:  ldstr      "f68"
    IL_0b9e:  ldarg.0
    IL_0b9f:  ldfld      float64 MCCTest.VType7::f68
    IL_0ba4:  ldarga.s   expected
    IL_0ba6:  ldfld      float64 MCCTest.VType7::f68
    IL_0bab:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0bb0:  throw

    IL_0bb1:  ldarg.0
    IL_0bb2:  ldfld      float64 MCCTest.VType7::f69
    IL_0bb7:  ldarga.s   expected
    IL_0bb9:  ldfld      float64 MCCTest.VType7::f69
    IL_0bbe:  ceq
    IL_0bc0:  stloc.0
    IL_0bc1:  ldloc.0
    IL_0bc2:  brtrue.s   IL_0bdd

    IL_0bc4:  nop
    IL_0bc5:  ldstr      "f69"
    IL_0bca:  ldarg.0
    IL_0bcb:  ldfld      float64 MCCTest.VType7::f69
    IL_0bd0:  ldarga.s   expected
    IL_0bd2:  ldfld      float64 MCCTest.VType7::f69
    IL_0bd7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0bdc:  throw

    IL_0bdd:  ldarg.0
    IL_0bde:  ldfld      float64 MCCTest.VType7::f70
    IL_0be3:  ldarga.s   expected
    IL_0be5:  ldfld      float64 MCCTest.VType7::f70
    IL_0bea:  ceq
    IL_0bec:  stloc.0
    IL_0bed:  ldloc.0
    IL_0bee:  brtrue.s   IL_0c09

    IL_0bf0:  nop
    IL_0bf1:  ldstr      "f70"
    IL_0bf6:  ldarg.0
    IL_0bf7:  ldfld      float64 MCCTest.VType7::f70
    IL_0bfc:  ldarga.s   expected
    IL_0bfe:  ldfld      float64 MCCTest.VType7::f70
    IL_0c03:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0c08:  throw

    IL_0c09:  ldarg.0
    IL_0c0a:  ldfld      float64 MCCTest.VType7::f71
    IL_0c0f:  ldarga.s   expected
    IL_0c11:  ldfld      float64 MCCTest.VType7::f71
    IL_0c16:  ceq
    IL_0c18:  stloc.0
    IL_0c19:  ldloc.0
    IL_0c1a:  brtrue.s   IL_0c35

    IL_0c1c:  nop
    IL_0c1d:  ldstr      "f71"
    IL_0c22:  ldarg.0
    IL_0c23:  ldfld      float64 MCCTest.VType7::f71
    IL_0c28:  ldarga.s   expected
    IL_0c2a:  ldfld      float64 MCCTest.VType7::f71
    IL_0c2f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0c34:  throw

    IL_0c35:  ldarg.0
    IL_0c36:  ldfld      float64 MCCTest.VType7::f72
    IL_0c3b:  ldarga.s   expected
    IL_0c3d:  ldfld      float64 MCCTest.VType7::f72
    IL_0c42:  ceq
    IL_0c44:  stloc.0
    IL_0c45:  ldloc.0
    IL_0c46:  brtrue.s   IL_0c61

    IL_0c48:  nop
    IL_0c49:  ldstr      "f72"
    IL_0c4e:  ldarg.0
    IL_0c4f:  ldfld      float64 MCCTest.VType7::f72
    IL_0c54:  ldarga.s   expected
    IL_0c56:  ldfld      float64 MCCTest.VType7::f72
    IL_0c5b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0c60:  throw

    IL_0c61:  ldarg.0
    IL_0c62:  ldfld      float64 MCCTest.VType7::f73
    IL_0c67:  ldarga.s   expected
    IL_0c69:  ldfld      float64 MCCTest.VType7::f73
    IL_0c6e:  ceq
    IL_0c70:  stloc.0
    IL_0c71:  ldloc.0
    IL_0c72:  brtrue.s   IL_0c8d

    IL_0c74:  nop
    IL_0c75:  ldstr      "f73"
    IL_0c7a:  ldarg.0
    IL_0c7b:  ldfld      float64 MCCTest.VType7::f73
    IL_0c80:  ldarga.s   expected
    IL_0c82:  ldfld      float64 MCCTest.VType7::f73
    IL_0c87:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0c8c:  throw

    IL_0c8d:  ldarg.0
    IL_0c8e:  ldfld      float64 MCCTest.VType7::f74
    IL_0c93:  ldarga.s   expected
    IL_0c95:  ldfld      float64 MCCTest.VType7::f74
    IL_0c9a:  ceq
    IL_0c9c:  stloc.0
    IL_0c9d:  ldloc.0
    IL_0c9e:  brtrue.s   IL_0cb9

    IL_0ca0:  nop
    IL_0ca1:  ldstr      "f74"
    IL_0ca6:  ldarg.0
    IL_0ca7:  ldfld      float64 MCCTest.VType7::f74
    IL_0cac:  ldarga.s   expected
    IL_0cae:  ldfld      float64 MCCTest.VType7::f74
    IL_0cb3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0cb8:  throw

    IL_0cb9:  ldarg.0
    IL_0cba:  ldfld      float64 MCCTest.VType7::f75
    IL_0cbf:  ldarga.s   expected
    IL_0cc1:  ldfld      float64 MCCTest.VType7::f75
    IL_0cc6:  ceq
    IL_0cc8:  stloc.0
    IL_0cc9:  ldloc.0
    IL_0cca:  brtrue.s   IL_0ce5

    IL_0ccc:  nop
    IL_0ccd:  ldstr      "f75"
    IL_0cd2:  ldarg.0
    IL_0cd3:  ldfld      float64 MCCTest.VType7::f75
    IL_0cd8:  ldarga.s   expected
    IL_0cda:  ldfld      float64 MCCTest.VType7::f75
    IL_0cdf:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0ce4:  throw

    IL_0ce5:  ldarg.0
    IL_0ce6:  ldfld      float64 MCCTest.VType7::f76
    IL_0ceb:  ldarga.s   expected
    IL_0ced:  ldfld      float64 MCCTest.VType7::f76
    IL_0cf2:  ceq
    IL_0cf4:  stloc.0
    IL_0cf5:  ldloc.0
    IL_0cf6:  brtrue.s   IL_0d11

    IL_0cf8:  nop
    IL_0cf9:  ldstr      "f76"
    IL_0cfe:  ldarg.0
    IL_0cff:  ldfld      float64 MCCTest.VType7::f76
    IL_0d04:  ldarga.s   expected
    IL_0d06:  ldfld      float64 MCCTest.VType7::f76
    IL_0d0b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0d10:  throw

    IL_0d11:  ldarg.0
    IL_0d12:  ldfld      float64 MCCTest.VType7::f77
    IL_0d17:  ldarga.s   expected
    IL_0d19:  ldfld      float64 MCCTest.VType7::f77
    IL_0d1e:  ceq
    IL_0d20:  stloc.0
    IL_0d21:  ldloc.0
    IL_0d22:  brtrue.s   IL_0d3d

    IL_0d24:  nop
    IL_0d25:  ldstr      "f77"
    IL_0d2a:  ldarg.0
    IL_0d2b:  ldfld      float64 MCCTest.VType7::f77
    IL_0d30:  ldarga.s   expected
    IL_0d32:  ldfld      float64 MCCTest.VType7::f77
    IL_0d37:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0d3c:  throw

    IL_0d3d:  ldarg.0
    IL_0d3e:  ldfld      float64 MCCTest.VType7::f78
    IL_0d43:  ldarga.s   expected
    IL_0d45:  ldfld      float64 MCCTest.VType7::f78
    IL_0d4a:  ceq
    IL_0d4c:  stloc.0
    IL_0d4d:  ldloc.0
    IL_0d4e:  brtrue.s   IL_0d69

    IL_0d50:  nop
    IL_0d51:  ldstr      "f78"
    IL_0d56:  ldarg.0
    IL_0d57:  ldfld      float64 MCCTest.VType7::f78
    IL_0d5c:  ldarga.s   expected
    IL_0d5e:  ldfld      float64 MCCTest.VType7::f78
    IL_0d63:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0d68:  throw

    IL_0d69:  ldarg.0
    IL_0d6a:  ldfld      float64 MCCTest.VType7::f79
    IL_0d6f:  ldarga.s   expected
    IL_0d71:  ldfld      float64 MCCTest.VType7::f79
    IL_0d76:  ceq
    IL_0d78:  stloc.0
    IL_0d79:  ldloc.0
    IL_0d7a:  brtrue.s   IL_0d95

    IL_0d7c:  nop
    IL_0d7d:  ldstr      "f79"
    IL_0d82:  ldarg.0
    IL_0d83:  ldfld      float64 MCCTest.VType7::f79
    IL_0d88:  ldarga.s   expected
    IL_0d8a:  ldfld      float64 MCCTest.VType7::f79
    IL_0d8f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0d94:  throw

    IL_0d95:  ldarg.0
    IL_0d96:  ldfld      float64 MCCTest.VType7::f80
    IL_0d9b:  ldarga.s   expected
    IL_0d9d:  ldfld      float64 MCCTest.VType7::f80
    IL_0da2:  ceq
    IL_0da4:  stloc.0
    IL_0da5:  ldloc.0
    IL_0da6:  brtrue.s   IL_0dc1

    IL_0da8:  nop
    IL_0da9:  ldstr      "f80"
    IL_0dae:  ldarg.0
    IL_0daf:  ldfld      float64 MCCTest.VType7::f80
    IL_0db4:  ldarga.s   expected
    IL_0db6:  ldfld      float64 MCCTest.VType7::f80
    IL_0dbb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0dc0:  throw

    IL_0dc1:  ldarg.0
    IL_0dc2:  ldfld      float64 MCCTest.VType7::f81
    IL_0dc7:  ldarga.s   expected
    IL_0dc9:  ldfld      float64 MCCTest.VType7::f81
    IL_0dce:  ceq
    IL_0dd0:  stloc.0
    IL_0dd1:  ldloc.0
    IL_0dd2:  brtrue.s   IL_0ded

    IL_0dd4:  nop
    IL_0dd5:  ldstr      "f81"
    IL_0dda:  ldarg.0
    IL_0ddb:  ldfld      float64 MCCTest.VType7::f81
    IL_0de0:  ldarga.s   expected
    IL_0de2:  ldfld      float64 MCCTest.VType7::f81
    IL_0de7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0dec:  throw

    IL_0ded:  ldarg.0
    IL_0dee:  ldfld      float64 MCCTest.VType7::f82
    IL_0df3:  ldarga.s   expected
    IL_0df5:  ldfld      float64 MCCTest.VType7::f82
    IL_0dfa:  ceq
    IL_0dfc:  stloc.0
    IL_0dfd:  ldloc.0
    IL_0dfe:  brtrue.s   IL_0e19

    IL_0e00:  nop
    IL_0e01:  ldstr      "f82"
    IL_0e06:  ldarg.0
    IL_0e07:  ldfld      float64 MCCTest.VType7::f82
    IL_0e0c:  ldarga.s   expected
    IL_0e0e:  ldfld      float64 MCCTest.VType7::f82
    IL_0e13:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0e18:  throw

    IL_0e19:  ldarg.0
    IL_0e1a:  ldfld      float64 MCCTest.VType7::f83
    IL_0e1f:  ldarga.s   expected
    IL_0e21:  ldfld      float64 MCCTest.VType7::f83
    IL_0e26:  ceq
    IL_0e28:  stloc.0
    IL_0e29:  ldloc.0
    IL_0e2a:  brtrue.s   IL_0e45

    IL_0e2c:  nop
    IL_0e2d:  ldstr      "f83"
    IL_0e32:  ldarg.0
    IL_0e33:  ldfld      float64 MCCTest.VType7::f83
    IL_0e38:  ldarga.s   expected
    IL_0e3a:  ldfld      float64 MCCTest.VType7::f83
    IL_0e3f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0e44:  throw

    IL_0e45:  ldarg.0
    IL_0e46:  ldfld      float64 MCCTest.VType7::f84
    IL_0e4b:  ldarga.s   expected
    IL_0e4d:  ldfld      float64 MCCTest.VType7::f84
    IL_0e52:  ceq
    IL_0e54:  stloc.0
    IL_0e55:  ldloc.0
    IL_0e56:  brtrue.s   IL_0e71

    IL_0e58:  nop
    IL_0e59:  ldstr      "f84"
    IL_0e5e:  ldarg.0
    IL_0e5f:  ldfld      float64 MCCTest.VType7::f84
    IL_0e64:  ldarga.s   expected
    IL_0e66:  ldfld      float64 MCCTest.VType7::f84
    IL_0e6b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0e70:  throw

    IL_0e71:  ldarg.0
    IL_0e72:  ldfld      float64 MCCTest.VType7::f85
    IL_0e77:  ldarga.s   expected
    IL_0e79:  ldfld      float64 MCCTest.VType7::f85
    IL_0e7e:  ceq
    IL_0e80:  stloc.0
    IL_0e81:  ldloc.0
    IL_0e82:  brtrue.s   IL_0e9d

    IL_0e84:  nop
    IL_0e85:  ldstr      "f85"
    IL_0e8a:  ldarg.0
    IL_0e8b:  ldfld      float64 MCCTest.VType7::f85
    IL_0e90:  ldarga.s   expected
    IL_0e92:  ldfld      float64 MCCTest.VType7::f85
    IL_0e97:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0e9c:  throw

    IL_0e9d:  ldarg.0
    IL_0e9e:  ldfld      float64 MCCTest.VType7::f86
    IL_0ea3:  ldarga.s   expected
    IL_0ea5:  ldfld      float64 MCCTest.VType7::f86
    IL_0eaa:  ceq
    IL_0eac:  stloc.0
    IL_0ead:  ldloc.0
    IL_0eae:  brtrue.s   IL_0ec9

    IL_0eb0:  nop
    IL_0eb1:  ldstr      "f86"
    IL_0eb6:  ldarg.0
    IL_0eb7:  ldfld      float64 MCCTest.VType7::f86
    IL_0ebc:  ldarga.s   expected
    IL_0ebe:  ldfld      float64 MCCTest.VType7::f86
    IL_0ec3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0ec8:  throw

    IL_0ec9:  ldarg.0
    IL_0eca:  ldfld      float64 MCCTest.VType7::f87
    IL_0ecf:  ldarga.s   expected
    IL_0ed1:  ldfld      float64 MCCTest.VType7::f87
    IL_0ed6:  ceq
    IL_0ed8:  stloc.0
    IL_0ed9:  ldloc.0
    IL_0eda:  brtrue.s   IL_0ef5

    IL_0edc:  nop
    IL_0edd:  ldstr      "f87"
    IL_0ee2:  ldarg.0
    IL_0ee3:  ldfld      float64 MCCTest.VType7::f87
    IL_0ee8:  ldarga.s   expected
    IL_0eea:  ldfld      float64 MCCTest.VType7::f87
    IL_0eef:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0ef4:  throw

    IL_0ef5:  ldarg.0
    IL_0ef6:  ldfld      float64 MCCTest.VType7::f88
    IL_0efb:  ldarga.s   expected
    IL_0efd:  ldfld      float64 MCCTest.VType7::f88
    IL_0f02:  ceq
    IL_0f04:  stloc.0
    IL_0f05:  ldloc.0
    IL_0f06:  brtrue.s   IL_0f21

    IL_0f08:  nop
    IL_0f09:  ldstr      "f88"
    IL_0f0e:  ldarg.0
    IL_0f0f:  ldfld      float64 MCCTest.VType7::f88
    IL_0f14:  ldarga.s   expected
    IL_0f16:  ldfld      float64 MCCTest.VType7::f88
    IL_0f1b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0f20:  throw

    IL_0f21:  ldarg.0
    IL_0f22:  ldfld      float64 MCCTest.VType7::f89
    IL_0f27:  ldarga.s   expected
    IL_0f29:  ldfld      float64 MCCTest.VType7::f89
    IL_0f2e:  ceq
    IL_0f30:  stloc.0
    IL_0f31:  ldloc.0
    IL_0f32:  brtrue.s   IL_0f4d

    IL_0f34:  nop
    IL_0f35:  ldstr      "f89"
    IL_0f3a:  ldarg.0
    IL_0f3b:  ldfld      float64 MCCTest.VType7::f89
    IL_0f40:  ldarga.s   expected
    IL_0f42:  ldfld      float64 MCCTest.VType7::f89
    IL_0f47:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0f4c:  throw

    IL_0f4d:  ldarg.0
    IL_0f4e:  ldfld      float64 MCCTest.VType7::f90
    IL_0f53:  ldarga.s   expected
    IL_0f55:  ldfld      float64 MCCTest.VType7::f90
    IL_0f5a:  ceq
    IL_0f5c:  stloc.0
    IL_0f5d:  ldloc.0
    IL_0f5e:  brtrue.s   IL_0f79

    IL_0f60:  nop
    IL_0f61:  ldstr      "f90"
    IL_0f66:  ldarg.0
    IL_0f67:  ldfld      float64 MCCTest.VType7::f90
    IL_0f6c:  ldarga.s   expected
    IL_0f6e:  ldfld      float64 MCCTest.VType7::f90
    IL_0f73:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0f78:  throw

    IL_0f79:  ldarg.0
    IL_0f7a:  ldfld      float64 MCCTest.VType7::f91
    IL_0f7f:  ldarga.s   expected
    IL_0f81:  ldfld      float64 MCCTest.VType7::f91
    IL_0f86:  ceq
    IL_0f88:  stloc.0
    IL_0f89:  ldloc.0
    IL_0f8a:  brtrue.s   IL_0fa5

    IL_0f8c:  nop
    IL_0f8d:  ldstr      "f91"
    IL_0f92:  ldarg.0
    IL_0f93:  ldfld      float64 MCCTest.VType7::f91
    IL_0f98:  ldarga.s   expected
    IL_0f9a:  ldfld      float64 MCCTest.VType7::f91
    IL_0f9f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0fa4:  throw

    IL_0fa5:  ldarg.0
    IL_0fa6:  ldfld      float64 MCCTest.VType7::f92
    IL_0fab:  ldarga.s   expected
    IL_0fad:  ldfld      float64 MCCTest.VType7::f92
    IL_0fb2:  ceq
    IL_0fb4:  stloc.0
    IL_0fb5:  ldloc.0
    IL_0fb6:  brtrue.s   IL_0fd1

    IL_0fb8:  nop
    IL_0fb9:  ldstr      "f92"
    IL_0fbe:  ldarg.0
    IL_0fbf:  ldfld      float64 MCCTest.VType7::f92
    IL_0fc4:  ldarga.s   expected
    IL_0fc6:  ldfld      float64 MCCTest.VType7::f92
    IL_0fcb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0fd0:  throw

    IL_0fd1:  ldarg.0
    IL_0fd2:  ldfld      float64 MCCTest.VType7::f93
    IL_0fd7:  ldarga.s   expected
    IL_0fd9:  ldfld      float64 MCCTest.VType7::f93
    IL_0fde:  ceq
    IL_0fe0:  stloc.0
    IL_0fe1:  ldloc.0
    IL_0fe2:  brtrue.s   IL_0ffd

    IL_0fe4:  nop
    IL_0fe5:  ldstr      "f93"
    IL_0fea:  ldarg.0
    IL_0feb:  ldfld      float64 MCCTest.VType7::f93
    IL_0ff0:  ldarga.s   expected
    IL_0ff2:  ldfld      float64 MCCTest.VType7::f93
    IL_0ff7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0ffc:  throw

    IL_0ffd:  ldarg.0
    IL_0ffe:  ldfld      float64 MCCTest.VType7::f94
    IL_1003:  ldarga.s   expected
    IL_1005:  ldfld      float64 MCCTest.VType7::f94
    IL_100a:  ceq
    IL_100c:  stloc.0
    IL_100d:  ldloc.0
    IL_100e:  brtrue.s   IL_1029

    IL_1010:  nop
    IL_1011:  ldstr      "f94"
    IL_1016:  ldarg.0
    IL_1017:  ldfld      float64 MCCTest.VType7::f94
    IL_101c:  ldarga.s   expected
    IL_101e:  ldfld      float64 MCCTest.VType7::f94
    IL_1023:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1028:  throw

    IL_1029:  ldarg.0
    IL_102a:  ldfld      float64 MCCTest.VType7::f95
    IL_102f:  ldarga.s   expected
    IL_1031:  ldfld      float64 MCCTest.VType7::f95
    IL_1036:  ceq
    IL_1038:  stloc.0
    IL_1039:  ldloc.0
    IL_103a:  brtrue.s   IL_1055

    IL_103c:  nop
    IL_103d:  ldstr      "f95"
    IL_1042:  ldarg.0
    IL_1043:  ldfld      float64 MCCTest.VType7::f95
    IL_1048:  ldarga.s   expected
    IL_104a:  ldfld      float64 MCCTest.VType7::f95
    IL_104f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1054:  throw

    IL_1055:  ldarg.0
    IL_1056:  ldfld      float64 MCCTest.VType7::f96
    IL_105b:  ldarga.s   expected
    IL_105d:  ldfld      float64 MCCTest.VType7::f96
    IL_1062:  ceq
    IL_1064:  stloc.0
    IL_1065:  ldloc.0
    IL_1066:  brtrue.s   IL_1081

    IL_1068:  nop
    IL_1069:  ldstr      "f96"
    IL_106e:  ldarg.0
    IL_106f:  ldfld      float64 MCCTest.VType7::f96
    IL_1074:  ldarga.s   expected
    IL_1076:  ldfld      float64 MCCTest.VType7::f96
    IL_107b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1080:  throw

    IL_1081:  ldarg.0
    IL_1082:  ldfld      float64 MCCTest.VType7::f97
    IL_1087:  ldarga.s   expected
    IL_1089:  ldfld      float64 MCCTest.VType7::f97
    IL_108e:  ceq
    IL_1090:  stloc.0
    IL_1091:  ldloc.0
    IL_1092:  brtrue.s   IL_10ad

    IL_1094:  nop
    IL_1095:  ldstr      "f97"
    IL_109a:  ldarg.0
    IL_109b:  ldfld      float64 MCCTest.VType7::f97
    IL_10a0:  ldarga.s   expected
    IL_10a2:  ldfld      float64 MCCTest.VType7::f97
    IL_10a7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_10ac:  throw

    IL_10ad:  ldarg.0
    IL_10ae:  ldfld      float64 MCCTest.VType7::f98
    IL_10b3:  ldarga.s   expected
    IL_10b5:  ldfld      float64 MCCTest.VType7::f98
    IL_10ba:  ceq
    IL_10bc:  stloc.0
    IL_10bd:  ldloc.0
    IL_10be:  brtrue.s   IL_10d9

    IL_10c0:  nop
    IL_10c1:  ldstr      "f98"
    IL_10c6:  ldarg.0
    IL_10c7:  ldfld      float64 MCCTest.VType7::f98
    IL_10cc:  ldarga.s   expected
    IL_10ce:  ldfld      float64 MCCTest.VType7::f98
    IL_10d3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_10d8:  throw

    IL_10d9:  ldarg.0
    IL_10da:  ldfld      float64 MCCTest.VType7::f99
    IL_10df:  ldarga.s   expected
    IL_10e1:  ldfld      float64 MCCTest.VType7::f99
    IL_10e6:  ceq
    IL_10e8:  stloc.0
    IL_10e9:  ldloc.0
    IL_10ea:  brtrue.s   IL_1105

    IL_10ec:  nop
    IL_10ed:  ldstr      "f99"
    IL_10f2:  ldarg.0
    IL_10f3:  ldfld      float64 MCCTest.VType7::f99
    IL_10f8:  ldarga.s   expected
    IL_10fa:  ldfld      float64 MCCTest.VType7::f99
    IL_10ff:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1104:  throw

    IL_1105:  ldarg.0
    IL_1106:  ldfld      float64 MCCTest.VType7::f100
    IL_110b:  ldarga.s   expected
    IL_110d:  ldfld      float64 MCCTest.VType7::f100
    IL_1112:  ceq
    IL_1114:  stloc.0
    IL_1115:  ldloc.0
    IL_1116:  brtrue.s   IL_1131

    IL_1118:  nop
    IL_1119:  ldstr      "f100"
    IL_111e:  ldarg.0
    IL_111f:  ldfld      float64 MCCTest.VType7::f100
    IL_1124:  ldarga.s   expected
    IL_1126:  ldfld      float64 MCCTest.VType7::f100
    IL_112b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1130:  throw

    IL_1131:  ldarg.0
    IL_1132:  ldfld      float64 MCCTest.VType7::f101
    IL_1137:  ldarga.s   expected
    IL_1139:  ldfld      float64 MCCTest.VType7::f101
    IL_113e:  ceq
    IL_1140:  stloc.0
    IL_1141:  ldloc.0
    IL_1142:  brtrue.s   IL_115d

    IL_1144:  nop
    IL_1145:  ldstr      "f101"
    IL_114a:  ldarg.0
    IL_114b:  ldfld      float64 MCCTest.VType7::f101
    IL_1150:  ldarga.s   expected
    IL_1152:  ldfld      float64 MCCTest.VType7::f101
    IL_1157:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_115c:  throw

    IL_115d:  ldarg.0
    IL_115e:  ldfld      float64 MCCTest.VType7::f102
    IL_1163:  ldarga.s   expected
    IL_1165:  ldfld      float64 MCCTest.VType7::f102
    IL_116a:  ceq
    IL_116c:  stloc.0
    IL_116d:  ldloc.0
    IL_116e:  brtrue.s   IL_1189

    IL_1170:  nop
    IL_1171:  ldstr      "f102"
    IL_1176:  ldarg.0
    IL_1177:  ldfld      float64 MCCTest.VType7::f102
    IL_117c:  ldarga.s   expected
    IL_117e:  ldfld      float64 MCCTest.VType7::f102
    IL_1183:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1188:  throw

    IL_1189:  ldarg.0
    IL_118a:  ldfld      float64 MCCTest.VType7::f103
    IL_118f:  ldarga.s   expected
    IL_1191:  ldfld      float64 MCCTest.VType7::f103
    IL_1196:  ceq
    IL_1198:  stloc.0
    IL_1199:  ldloc.0
    IL_119a:  brtrue.s   IL_11b5

    IL_119c:  nop
    IL_119d:  ldstr      "f103"
    IL_11a2:  ldarg.0
    IL_11a3:  ldfld      float64 MCCTest.VType7::f103
    IL_11a8:  ldarga.s   expected
    IL_11aa:  ldfld      float64 MCCTest.VType7::f103
    IL_11af:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_11b4:  throw

    IL_11b5:  ldarg.0
    IL_11b6:  ldfld      float64 MCCTest.VType7::f104
    IL_11bb:  ldarga.s   expected
    IL_11bd:  ldfld      float64 MCCTest.VType7::f104
    IL_11c2:  ceq
    IL_11c4:  stloc.0
    IL_11c5:  ldloc.0
    IL_11c6:  brtrue.s   IL_11e1

    IL_11c8:  nop
    IL_11c9:  ldstr      "f104"
    IL_11ce:  ldarg.0
    IL_11cf:  ldfld      float64 MCCTest.VType7::f104
    IL_11d4:  ldarga.s   expected
    IL_11d6:  ldfld      float64 MCCTest.VType7::f104
    IL_11db:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_11e0:  throw

    IL_11e1:  ldarg.0
    IL_11e2:  ldfld      float64 MCCTest.VType7::f105
    IL_11e7:  ldarga.s   expected
    IL_11e9:  ldfld      float64 MCCTest.VType7::f105
    IL_11ee:  ceq
    IL_11f0:  stloc.0
    IL_11f1:  ldloc.0
    IL_11f2:  brtrue.s   IL_120d

    IL_11f4:  nop
    IL_11f5:  ldstr      "f105"
    IL_11fa:  ldarg.0
    IL_11fb:  ldfld      float64 MCCTest.VType7::f105
    IL_1200:  ldarga.s   expected
    IL_1202:  ldfld      float64 MCCTest.VType7::f105
    IL_1207:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_120c:  throw

    IL_120d:  ldarg.0
    IL_120e:  ldfld      float64 MCCTest.VType7::f106
    IL_1213:  ldarga.s   expected
    IL_1215:  ldfld      float64 MCCTest.VType7::f106
    IL_121a:  ceq
    IL_121c:  stloc.0
    IL_121d:  ldloc.0
    IL_121e:  brtrue.s   IL_1239

    IL_1220:  nop
    IL_1221:  ldstr      "f106"
    IL_1226:  ldarg.0
    IL_1227:  ldfld      float64 MCCTest.VType7::f106
    IL_122c:  ldarga.s   expected
    IL_122e:  ldfld      float64 MCCTest.VType7::f106
    IL_1233:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1238:  throw

    IL_1239:  ldarg.0
    IL_123a:  ldfld      float64 MCCTest.VType7::f107
    IL_123f:  ldarga.s   expected
    IL_1241:  ldfld      float64 MCCTest.VType7::f107
    IL_1246:  ceq
    IL_1248:  stloc.0
    IL_1249:  ldloc.0
    IL_124a:  brtrue.s   IL_1265

    IL_124c:  nop
    IL_124d:  ldstr      "f107"
    IL_1252:  ldarg.0
    IL_1253:  ldfld      float64 MCCTest.VType7::f107
    IL_1258:  ldarga.s   expected
    IL_125a:  ldfld      float64 MCCTest.VType7::f107
    IL_125f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1264:  throw

    IL_1265:  ldarg.0
    IL_1266:  ldfld      float64 MCCTest.VType7::f108
    IL_126b:  ldarga.s   expected
    IL_126d:  ldfld      float64 MCCTest.VType7::f108
    IL_1272:  ceq
    IL_1274:  stloc.0
    IL_1275:  ldloc.0
    IL_1276:  brtrue.s   IL_1291

    IL_1278:  nop
    IL_1279:  ldstr      "f108"
    IL_127e:  ldarg.0
    IL_127f:  ldfld      float64 MCCTest.VType7::f108
    IL_1284:  ldarga.s   expected
    IL_1286:  ldfld      float64 MCCTest.VType7::f108
    IL_128b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1290:  throw

    IL_1291:  ldarg.0
    IL_1292:  ldfld      float64 MCCTest.VType7::f109
    IL_1297:  ldarga.s   expected
    IL_1299:  ldfld      float64 MCCTest.VType7::f109
    IL_129e:  ceq
    IL_12a0:  stloc.0
    IL_12a1:  ldloc.0
    IL_12a2:  brtrue.s   IL_12bd

    IL_12a4:  nop
    IL_12a5:  ldstr      "f109"
    IL_12aa:  ldarg.0
    IL_12ab:  ldfld      float64 MCCTest.VType7::f109
    IL_12b0:  ldarga.s   expected
    IL_12b2:  ldfld      float64 MCCTest.VType7::f109
    IL_12b7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_12bc:  throw

    IL_12bd:  ldarg.0
    IL_12be:  ldfld      float64 MCCTest.VType7::f110
    IL_12c3:  ldarga.s   expected
    IL_12c5:  ldfld      float64 MCCTest.VType7::f110
    IL_12ca:  ceq
    IL_12cc:  stloc.0
    IL_12cd:  ldloc.0
    IL_12ce:  brtrue.s   IL_12e9

    IL_12d0:  nop
    IL_12d1:  ldstr      "f110"
    IL_12d6:  ldarg.0
    IL_12d7:  ldfld      float64 MCCTest.VType7::f110
    IL_12dc:  ldarga.s   expected
    IL_12de:  ldfld      float64 MCCTest.VType7::f110
    IL_12e3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_12e8:  throw

    IL_12e9:  ldarg.0
    IL_12ea:  ldfld      float64 MCCTest.VType7::f111
    IL_12ef:  ldarga.s   expected
    IL_12f1:  ldfld      float64 MCCTest.VType7::f111
    IL_12f6:  ceq
    IL_12f8:  stloc.0
    IL_12f9:  ldloc.0
    IL_12fa:  brtrue.s   IL_1315

    IL_12fc:  nop
    IL_12fd:  ldstr      "f111"
    IL_1302:  ldarg.0
    IL_1303:  ldfld      float64 MCCTest.VType7::f111
    IL_1308:  ldarga.s   expected
    IL_130a:  ldfld      float64 MCCTest.VType7::f111
    IL_130f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1314:  throw

    IL_1315:  ldarg.0
    IL_1316:  ldfld      float64 MCCTest.VType7::f112
    IL_131b:  ldarga.s   expected
    IL_131d:  ldfld      float64 MCCTest.VType7::f112
    IL_1322:  ceq
    IL_1324:  stloc.0
    IL_1325:  ldloc.0
    IL_1326:  brtrue.s   IL_1341

    IL_1328:  nop
    IL_1329:  ldstr      "f112"
    IL_132e:  ldarg.0
    IL_132f:  ldfld      float64 MCCTest.VType7::f112
    IL_1334:  ldarga.s   expected
    IL_1336:  ldfld      float64 MCCTest.VType7::f112
    IL_133b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1340:  throw

    IL_1341:  ldarg.0
    IL_1342:  ldfld      float64 MCCTest.VType7::f113
    IL_1347:  ldarga.s   expected
    IL_1349:  ldfld      float64 MCCTest.VType7::f113
    IL_134e:  ceq
    IL_1350:  stloc.0
    IL_1351:  ldloc.0
    IL_1352:  brtrue.s   IL_136d

    IL_1354:  nop
    IL_1355:  ldstr      "f113"
    IL_135a:  ldarg.0
    IL_135b:  ldfld      float64 MCCTest.VType7::f113
    IL_1360:  ldarga.s   expected
    IL_1362:  ldfld      float64 MCCTest.VType7::f113
    IL_1367:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_136c:  throw

    IL_136d:  ldarg.0
    IL_136e:  ldfld      float64 MCCTest.VType7::f114
    IL_1373:  ldarga.s   expected
    IL_1375:  ldfld      float64 MCCTest.VType7::f114
    IL_137a:  ceq
    IL_137c:  stloc.0
    IL_137d:  ldloc.0
    IL_137e:  brtrue.s   IL_1399

    IL_1380:  nop
    IL_1381:  ldstr      "f114"
    IL_1386:  ldarg.0
    IL_1387:  ldfld      float64 MCCTest.VType7::f114
    IL_138c:  ldarga.s   expected
    IL_138e:  ldfld      float64 MCCTest.VType7::f114
    IL_1393:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1398:  throw

    IL_1399:  ldarg.0
    IL_139a:  ldfld      float64 MCCTest.VType7::f115
    IL_139f:  ldarga.s   expected
    IL_13a1:  ldfld      float64 MCCTest.VType7::f115
    IL_13a6:  ceq
    IL_13a8:  stloc.0
    IL_13a9:  ldloc.0
    IL_13aa:  brtrue.s   IL_13c5

    IL_13ac:  nop
    IL_13ad:  ldstr      "f115"
    IL_13b2:  ldarg.0
    IL_13b3:  ldfld      float64 MCCTest.VType7::f115
    IL_13b8:  ldarga.s   expected
    IL_13ba:  ldfld      float64 MCCTest.VType7::f115
    IL_13bf:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_13c4:  throw

    IL_13c5:  ldarg.0
    IL_13c6:  ldfld      float64 MCCTest.VType7::f116
    IL_13cb:  ldarga.s   expected
    IL_13cd:  ldfld      float64 MCCTest.VType7::f116
    IL_13d2:  ceq
    IL_13d4:  stloc.0
    IL_13d5:  ldloc.0
    IL_13d6:  brtrue.s   IL_13f1

    IL_13d8:  nop
    IL_13d9:  ldstr      "f116"
    IL_13de:  ldarg.0
    IL_13df:  ldfld      float64 MCCTest.VType7::f116
    IL_13e4:  ldarga.s   expected
    IL_13e6:  ldfld      float64 MCCTest.VType7::f116
    IL_13eb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_13f0:  throw

    IL_13f1:  ldarg.0
    IL_13f2:  ldfld      float64 MCCTest.VType7::f117
    IL_13f7:  ldarga.s   expected
    IL_13f9:  ldfld      float64 MCCTest.VType7::f117
    IL_13fe:  ceq
    IL_1400:  stloc.0
    IL_1401:  ldloc.0
    IL_1402:  brtrue.s   IL_141d

    IL_1404:  nop
    IL_1405:  ldstr      "f117"
    IL_140a:  ldarg.0
    IL_140b:  ldfld      float64 MCCTest.VType7::f117
    IL_1410:  ldarga.s   expected
    IL_1412:  ldfld      float64 MCCTest.VType7::f117
    IL_1417:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_141c:  throw

    IL_141d:  ldarg.0
    IL_141e:  ldfld      float64 MCCTest.VType7::f118
    IL_1423:  ldarga.s   expected
    IL_1425:  ldfld      float64 MCCTest.VType7::f118
    IL_142a:  ceq
    IL_142c:  stloc.0
    IL_142d:  ldloc.0
    IL_142e:  brtrue.s   IL_1449

    IL_1430:  nop
    IL_1431:  ldstr      "f118"
    IL_1436:  ldarg.0
    IL_1437:  ldfld      float64 MCCTest.VType7::f118
    IL_143c:  ldarga.s   expected
    IL_143e:  ldfld      float64 MCCTest.VType7::f118
    IL_1443:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1448:  throw

    IL_1449:  ldarg.0
    IL_144a:  ldfld      float64 MCCTest.VType7::f119
    IL_144f:  ldarga.s   expected
    IL_1451:  ldfld      float64 MCCTest.VType7::f119
    IL_1456:  ceq
    IL_1458:  stloc.0
    IL_1459:  ldloc.0
    IL_145a:  brtrue.s   IL_1475

    IL_145c:  nop
    IL_145d:  ldstr      "f119"
    IL_1462:  ldarg.0
    IL_1463:  ldfld      float64 MCCTest.VType7::f119
    IL_1468:  ldarga.s   expected
    IL_146a:  ldfld      float64 MCCTest.VType7::f119
    IL_146f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1474:  throw

    IL_1475:  ldarg.0
    IL_1476:  ldfld      float64 MCCTest.VType7::f120
    IL_147b:  ldarga.s   expected
    IL_147d:  ldfld      float64 MCCTest.VType7::f120
    IL_1482:  ceq
    IL_1484:  stloc.0
    IL_1485:  ldloc.0
    IL_1486:  brtrue.s   IL_14a1

    IL_1488:  nop
    IL_1489:  ldstr      "f120"
    IL_148e:  ldarg.0
    IL_148f:  ldfld      float64 MCCTest.VType7::f120
    IL_1494:  ldarga.s   expected
    IL_1496:  ldfld      float64 MCCTest.VType7::f120
    IL_149b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_14a0:  throw

    IL_14a1:  ldarg.0
    IL_14a2:  ldfld      float64 MCCTest.VType7::f121
    IL_14a7:  ldarga.s   expected
    IL_14a9:  ldfld      float64 MCCTest.VType7::f121
    IL_14ae:  ceq
    IL_14b0:  stloc.0
    IL_14b1:  ldloc.0
    IL_14b2:  brtrue.s   IL_14cd

    IL_14b4:  nop
    IL_14b5:  ldstr      "f121"
    IL_14ba:  ldarg.0
    IL_14bb:  ldfld      float64 MCCTest.VType7::f121
    IL_14c0:  ldarga.s   expected
    IL_14c2:  ldfld      float64 MCCTest.VType7::f121
    IL_14c7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_14cc:  throw

    IL_14cd:  ldarg.0
    IL_14ce:  ldfld      float64 MCCTest.VType7::f122
    IL_14d3:  ldarga.s   expected
    IL_14d5:  ldfld      float64 MCCTest.VType7::f122
    IL_14da:  ceq
    IL_14dc:  stloc.0
    IL_14dd:  ldloc.0
    IL_14de:  brtrue.s   IL_14f9

    IL_14e0:  nop
    IL_14e1:  ldstr      "f122"
    IL_14e6:  ldarg.0
    IL_14e7:  ldfld      float64 MCCTest.VType7::f122
    IL_14ec:  ldarga.s   expected
    IL_14ee:  ldfld      float64 MCCTest.VType7::f122
    IL_14f3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_14f8:  throw

    IL_14f9:  ldarg.0
    IL_14fa:  ldfld      float64 MCCTest.VType7::f123
    IL_14ff:  ldarga.s   expected
    IL_1501:  ldfld      float64 MCCTest.VType7::f123
    IL_1506:  ceq
    IL_1508:  stloc.0
    IL_1509:  ldloc.0
    IL_150a:  brtrue.s   IL_1525

    IL_150c:  nop
    IL_150d:  ldstr      "f123"
    IL_1512:  ldarg.0
    IL_1513:  ldfld      float64 MCCTest.VType7::f123
    IL_1518:  ldarga.s   expected
    IL_151a:  ldfld      float64 MCCTest.VType7::f123
    IL_151f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1524:  throw

    IL_1525:  ldarg.0
    IL_1526:  ldfld      float64 MCCTest.VType7::f124
    IL_152b:  ldarga.s   expected
    IL_152d:  ldfld      float64 MCCTest.VType7::f124
    IL_1532:  ceq
    IL_1534:  stloc.0
    IL_1535:  ldloc.0
    IL_1536:  brtrue.s   IL_1551

    IL_1538:  nop
    IL_1539:  ldstr      "f124"
    IL_153e:  ldarg.0
    IL_153f:  ldfld      float64 MCCTest.VType7::f124
    IL_1544:  ldarga.s   expected
    IL_1546:  ldfld      float64 MCCTest.VType7::f124
    IL_154b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1550:  throw

    IL_1551:  ldarg.0
    IL_1552:  ldfld      float64 MCCTest.VType7::f125
    IL_1557:  ldarga.s   expected
    IL_1559:  ldfld      float64 MCCTest.VType7::f125
    IL_155e:  ceq
    IL_1560:  stloc.0
    IL_1561:  ldloc.0
    IL_1562:  brtrue.s   IL_157d

    IL_1564:  nop
    IL_1565:  ldstr      "f125"
    IL_156a:  ldarg.0
    IL_156b:  ldfld      float64 MCCTest.VType7::f125
    IL_1570:  ldarga.s   expected
    IL_1572:  ldfld      float64 MCCTest.VType7::f125
    IL_1577:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_157c:  throw

    IL_157d:  ldarg.0
    IL_157e:  ldfld      float64 MCCTest.VType7::f126
    IL_1583:  ldarga.s   expected
    IL_1585:  ldfld      float64 MCCTest.VType7::f126
    IL_158a:  ceq
    IL_158c:  stloc.0
    IL_158d:  ldloc.0
    IL_158e:  brtrue.s   IL_15a9

    IL_1590:  nop
    IL_1591:  ldstr      "f126"
    IL_1596:  ldarg.0
    IL_1597:  ldfld      float64 MCCTest.VType7::f126
    IL_159c:  ldarga.s   expected
    IL_159e:  ldfld      float64 MCCTest.VType7::f126
    IL_15a3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_15a8:  throw

    IL_15a9:  ldarg.0
    IL_15aa:  ldfld      float64 MCCTest.VType7::f127
    IL_15af:  ldarga.s   expected
    IL_15b1:  ldfld      float64 MCCTest.VType7::f127
    IL_15b6:  ceq
    IL_15b8:  stloc.0
    IL_15b9:  ldloc.0
    IL_15ba:  brtrue.s   IL_15d5

    IL_15bc:  nop
    IL_15bd:  ldstr      "f127"
    IL_15c2:  ldarg.0
    IL_15c3:  ldfld      float64 MCCTest.VType7::f127
    IL_15c8:  ldarga.s   expected
    IL_15ca:  ldfld      float64 MCCTest.VType7::f127
    IL_15cf:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_15d4:  throw

    IL_15d5:  ldarg.0
    IL_15d6:  ldfld      float64 MCCTest.VType7::f128
    IL_15db:  ldarga.s   expected
    IL_15dd:  ldfld      float64 MCCTest.VType7::f128
    IL_15e2:  ceq
    IL_15e4:  stloc.0
    IL_15e5:  ldloc.0
    IL_15e6:  brtrue.s   IL_1601

    IL_15e8:  nop
    IL_15e9:  ldstr      "f128"
    IL_15ee:  ldarg.0
    IL_15ef:  ldfld      float64 MCCTest.VType7::f128
    IL_15f4:  ldarga.s   expected
    IL_15f6:  ldfld      float64 MCCTest.VType7::f128
    IL_15fb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1600:  throw

    IL_1601:  ldarg.0
    IL_1602:  ldfld      float64 MCCTest.VType7::f129
    IL_1607:  ldarga.s   expected
    IL_1609:  ldfld      float64 MCCTest.VType7::f129
    IL_160e:  ceq
    IL_1610:  stloc.0
    IL_1611:  ldloc.0
    IL_1612:  brtrue.s   IL_162d

    IL_1614:  nop
    IL_1615:  ldstr      "f129"
    IL_161a:  ldarg.0
    IL_161b:  ldfld      float64 MCCTest.VType7::f129
    IL_1620:  ldarga.s   expected
    IL_1622:  ldfld      float64 MCCTest.VType7::f129
    IL_1627:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_162c:  throw

    IL_162d:  ldarg.0
    IL_162e:  ldfld      float64 MCCTest.VType7::f130
    IL_1633:  ldarga.s   expected
    IL_1635:  ldfld      float64 MCCTest.VType7::f130
    IL_163a:  ceq
    IL_163c:  stloc.0
    IL_163d:  ldloc.0
    IL_163e:  brtrue.s   IL_1659

    IL_1640:  nop
    IL_1641:  ldstr      "f130"
    IL_1646:  ldarg.0
    IL_1647:  ldfld      float64 MCCTest.VType7::f130
    IL_164c:  ldarga.s   expected
    IL_164e:  ldfld      float64 MCCTest.VType7::f130
    IL_1653:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1658:  throw

    IL_1659:  ldarg.0
    IL_165a:  ldfld      float64 MCCTest.VType7::f131
    IL_165f:  ldarga.s   expected
    IL_1661:  ldfld      float64 MCCTest.VType7::f131
    IL_1666:  ceq
    IL_1668:  stloc.0
    IL_1669:  ldloc.0
    IL_166a:  brtrue.s   IL_1685

    IL_166c:  nop
    IL_166d:  ldstr      "f131"
    IL_1672:  ldarg.0
    IL_1673:  ldfld      float64 MCCTest.VType7::f131
    IL_1678:  ldarga.s   expected
    IL_167a:  ldfld      float64 MCCTest.VType7::f131
    IL_167f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1684:  throw

    IL_1685:  ldarg.0
    IL_1686:  ldfld      float64 MCCTest.VType7::f132
    IL_168b:  ldarga.s   expected
    IL_168d:  ldfld      float64 MCCTest.VType7::f132
    IL_1692:  ceq
    IL_1694:  stloc.0
    IL_1695:  ldloc.0
    IL_1696:  brtrue.s   IL_16b1

    IL_1698:  nop
    IL_1699:  ldstr      "f132"
    IL_169e:  ldarg.0
    IL_169f:  ldfld      float64 MCCTest.VType7::f132
    IL_16a4:  ldarga.s   expected
    IL_16a6:  ldfld      float64 MCCTest.VType7::f132
    IL_16ab:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_16b0:  throw

    IL_16b1:  ldarg.0
    IL_16b2:  ldfld      float64 MCCTest.VType7::f133
    IL_16b7:  ldarga.s   expected
    IL_16b9:  ldfld      float64 MCCTest.VType7::f133
    IL_16be:  ceq
    IL_16c0:  stloc.0
    IL_16c1:  ldloc.0
    IL_16c2:  brtrue.s   IL_16dd

    IL_16c4:  nop
    IL_16c5:  ldstr      "f133"
    IL_16ca:  ldarg.0
    IL_16cb:  ldfld      float64 MCCTest.VType7::f133
    IL_16d0:  ldarga.s   expected
    IL_16d2:  ldfld      float64 MCCTest.VType7::f133
    IL_16d7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_16dc:  throw

    IL_16dd:  ldarg.0
    IL_16de:  ldfld      float64 MCCTest.VType7::f134
    IL_16e3:  ldarga.s   expected
    IL_16e5:  ldfld      float64 MCCTest.VType7::f134
    IL_16ea:  ceq
    IL_16ec:  stloc.0
    IL_16ed:  ldloc.0
    IL_16ee:  brtrue.s   IL_1709

    IL_16f0:  nop
    IL_16f1:  ldstr      "f134"
    IL_16f6:  ldarg.0
    IL_16f7:  ldfld      float64 MCCTest.VType7::f134
    IL_16fc:  ldarga.s   expected
    IL_16fe:  ldfld      float64 MCCTest.VType7::f134
    IL_1703:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1708:  throw

    IL_1709:  ldarg.0
    IL_170a:  ldfld      float64 MCCTest.VType7::f135
    IL_170f:  ldarga.s   expected
    IL_1711:  ldfld      float64 MCCTest.VType7::f135
    IL_1716:  ceq
    IL_1718:  stloc.0
    IL_1719:  ldloc.0
    IL_171a:  brtrue.s   IL_1735

    IL_171c:  nop
    IL_171d:  ldstr      "f135"
    IL_1722:  ldarg.0
    IL_1723:  ldfld      float64 MCCTest.VType7::f135
    IL_1728:  ldarga.s   expected
    IL_172a:  ldfld      float64 MCCTest.VType7::f135
    IL_172f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1734:  throw

    IL_1735:  ldarg.0
    IL_1736:  ldfld      float64 MCCTest.VType7::f136
    IL_173b:  ldarga.s   expected
    IL_173d:  ldfld      float64 MCCTest.VType7::f136
    IL_1742:  ceq
    IL_1744:  stloc.0
    IL_1745:  ldloc.0
    IL_1746:  brtrue.s   IL_1761

    IL_1748:  nop
    IL_1749:  ldstr      "f136"
    IL_174e:  ldarg.0
    IL_174f:  ldfld      float64 MCCTest.VType7::f136
    IL_1754:  ldarga.s   expected
    IL_1756:  ldfld      float64 MCCTest.VType7::f136
    IL_175b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1760:  throw

    IL_1761:  ldarg.0
    IL_1762:  ldfld      float64 MCCTest.VType7::f137
    IL_1767:  ldarga.s   expected
    IL_1769:  ldfld      float64 MCCTest.VType7::f137
    IL_176e:  ceq
    IL_1770:  stloc.0
    IL_1771:  ldloc.0
    IL_1772:  brtrue.s   IL_178d

    IL_1774:  nop
    IL_1775:  ldstr      "f137"
    IL_177a:  ldarg.0
    IL_177b:  ldfld      float64 MCCTest.VType7::f137
    IL_1780:  ldarga.s   expected
    IL_1782:  ldfld      float64 MCCTest.VType7::f137
    IL_1787:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_178c:  throw

    IL_178d:  ldarg.0
    IL_178e:  ldfld      float64 MCCTest.VType7::f138
    IL_1793:  ldarga.s   expected
    IL_1795:  ldfld      float64 MCCTest.VType7::f138
    IL_179a:  ceq
    IL_179c:  stloc.0
    IL_179d:  ldloc.0
    IL_179e:  brtrue.s   IL_17b9

    IL_17a0:  nop
    IL_17a1:  ldstr      "f138"
    IL_17a6:  ldarg.0
    IL_17a7:  ldfld      float64 MCCTest.VType7::f138
    IL_17ac:  ldarga.s   expected
    IL_17ae:  ldfld      float64 MCCTest.VType7::f138
    IL_17b3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_17b8:  throw

    IL_17b9:  ldarg.0
    IL_17ba:  ldfld      float64 MCCTest.VType7::f139
    IL_17bf:  ldarga.s   expected
    IL_17c1:  ldfld      float64 MCCTest.VType7::f139
    IL_17c6:  ceq
    IL_17c8:  stloc.0
    IL_17c9:  ldloc.0
    IL_17ca:  brtrue.s   IL_17e5

    IL_17cc:  nop
    IL_17cd:  ldstr      "f139"
    IL_17d2:  ldarg.0
    IL_17d3:  ldfld      float64 MCCTest.VType7::f139
    IL_17d8:  ldarga.s   expected
    IL_17da:  ldfld      float64 MCCTest.VType7::f139
    IL_17df:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_17e4:  throw

    IL_17e5:  ldarg.0
    IL_17e6:  ldfld      float64 MCCTest.VType7::f140
    IL_17eb:  ldarga.s   expected
    IL_17ed:  ldfld      float64 MCCTest.VType7::f140
    IL_17f2:  ceq
    IL_17f4:  stloc.0
    IL_17f5:  ldloc.0
    IL_17f6:  brtrue.s   IL_1811

    IL_17f8:  nop
    IL_17f9:  ldstr      "f140"
    IL_17fe:  ldarg.0
    IL_17ff:  ldfld      float64 MCCTest.VType7::f140
    IL_1804:  ldarga.s   expected
    IL_1806:  ldfld      float64 MCCTest.VType7::f140
    IL_180b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1810:  throw

    IL_1811:  ldarg.0
    IL_1812:  ldfld      float64 MCCTest.VType7::f141
    IL_1817:  ldarga.s   expected
    IL_1819:  ldfld      float64 MCCTest.VType7::f141
    IL_181e:  ceq
    IL_1820:  stloc.0
    IL_1821:  ldloc.0
    IL_1822:  brtrue.s   IL_183d

    IL_1824:  nop
    IL_1825:  ldstr      "f141"
    IL_182a:  ldarg.0
    IL_182b:  ldfld      float64 MCCTest.VType7::f141
    IL_1830:  ldarga.s   expected
    IL_1832:  ldfld      float64 MCCTest.VType7::f141
    IL_1837:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_183c:  throw

    IL_183d:  ldarg.0
    IL_183e:  ldfld      float64 MCCTest.VType7::f142
    IL_1843:  ldarga.s   expected
    IL_1845:  ldfld      float64 MCCTest.VType7::f142
    IL_184a:  ceq
    IL_184c:  stloc.0
    IL_184d:  ldloc.0
    IL_184e:  brtrue.s   IL_1869

    IL_1850:  nop
    IL_1851:  ldstr      "f142"
    IL_1856:  ldarg.0
    IL_1857:  ldfld      float64 MCCTest.VType7::f142
    IL_185c:  ldarga.s   expected
    IL_185e:  ldfld      float64 MCCTest.VType7::f142
    IL_1863:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1868:  throw

    IL_1869:  ldarg.0
    IL_186a:  ldfld      float64 MCCTest.VType7::f143
    IL_186f:  ldarga.s   expected
    IL_1871:  ldfld      float64 MCCTest.VType7::f143
    IL_1876:  ceq
    IL_1878:  stloc.0
    IL_1879:  ldloc.0
    IL_187a:  brtrue.s   IL_1895

    IL_187c:  nop
    IL_187d:  ldstr      "f143"
    IL_1882:  ldarg.0
    IL_1883:  ldfld      float64 MCCTest.VType7::f143
    IL_1888:  ldarga.s   expected
    IL_188a:  ldfld      float64 MCCTest.VType7::f143
    IL_188f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1894:  throw

    IL_1895:  ldarg.0
    IL_1896:  ldfld      float64 MCCTest.VType7::f144
    IL_189b:  ldarga.s   expected
    IL_189d:  ldfld      float64 MCCTest.VType7::f144
    IL_18a2:  ceq
    IL_18a4:  stloc.0
    IL_18a5:  ldloc.0
    IL_18a6:  brtrue.s   IL_18c1

    IL_18a8:  nop
    IL_18a9:  ldstr      "f144"
    IL_18ae:  ldarg.0
    IL_18af:  ldfld      float64 MCCTest.VType7::f144
    IL_18b4:  ldarga.s   expected
    IL_18b6:  ldfld      float64 MCCTest.VType7::f144
    IL_18bb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_18c0:  throw

    IL_18c1:  ldarg.0
    IL_18c2:  ldfld      float64 MCCTest.VType7::f145
    IL_18c7:  ldarga.s   expected
    IL_18c9:  ldfld      float64 MCCTest.VType7::f145
    IL_18ce:  ceq
    IL_18d0:  stloc.0
    IL_18d1:  ldloc.0
    IL_18d2:  brtrue.s   IL_18ed

    IL_18d4:  nop
    IL_18d5:  ldstr      "f145"
    IL_18da:  ldarg.0
    IL_18db:  ldfld      float64 MCCTest.VType7::f145
    IL_18e0:  ldarga.s   expected
    IL_18e2:  ldfld      float64 MCCTest.VType7::f145
    IL_18e7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_18ec:  throw

    IL_18ed:  ldarg.0
    IL_18ee:  ldfld      float64 MCCTest.VType7::f146
    IL_18f3:  ldarga.s   expected
    IL_18f5:  ldfld      float64 MCCTest.VType7::f146
    IL_18fa:  ceq
    IL_18fc:  stloc.0
    IL_18fd:  ldloc.0
    IL_18fe:  brtrue.s   IL_1919

    IL_1900:  nop
    IL_1901:  ldstr      "f146"
    IL_1906:  ldarg.0
    IL_1907:  ldfld      float64 MCCTest.VType7::f146
    IL_190c:  ldarga.s   expected
    IL_190e:  ldfld      float64 MCCTest.VType7::f146
    IL_1913:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1918:  throw

    IL_1919:  ldarg.0
    IL_191a:  ldfld      float64 MCCTest.VType7::f147
    IL_191f:  ldarga.s   expected
    IL_1921:  ldfld      float64 MCCTest.VType7::f147
    IL_1926:  ceq
    IL_1928:  stloc.0
    IL_1929:  ldloc.0
    IL_192a:  brtrue.s   IL_1945

    IL_192c:  nop
    IL_192d:  ldstr      "f147"
    IL_1932:  ldarg.0
    IL_1933:  ldfld      float64 MCCTest.VType7::f147
    IL_1938:  ldarga.s   expected
    IL_193a:  ldfld      float64 MCCTest.VType7::f147
    IL_193f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1944:  throw

    IL_1945:  ldarg.0
    IL_1946:  ldfld      float64 MCCTest.VType7::f148
    IL_194b:  ldarga.s   expected
    IL_194d:  ldfld      float64 MCCTest.VType7::f148
    IL_1952:  ceq
    IL_1954:  stloc.0
    IL_1955:  ldloc.0
    IL_1956:  brtrue.s   IL_1971

    IL_1958:  nop
    IL_1959:  ldstr      "f148"
    IL_195e:  ldarg.0
    IL_195f:  ldfld      float64 MCCTest.VType7::f148
    IL_1964:  ldarga.s   expected
    IL_1966:  ldfld      float64 MCCTest.VType7::f148
    IL_196b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1970:  throw

    IL_1971:  ldarg.0
    IL_1972:  ldfld      float64 MCCTest.VType7::f149
    IL_1977:  ldarga.s   expected
    IL_1979:  ldfld      float64 MCCTest.VType7::f149
    IL_197e:  ceq
    IL_1980:  stloc.0
    IL_1981:  ldloc.0
    IL_1982:  brtrue.s   IL_199d

    IL_1984:  nop
    IL_1985:  ldstr      "f149"
    IL_198a:  ldarg.0
    IL_198b:  ldfld      float64 MCCTest.VType7::f149
    IL_1990:  ldarga.s   expected
    IL_1992:  ldfld      float64 MCCTest.VType7::f149
    IL_1997:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_199c:  throw

    IL_199d:  ldarg.0
    IL_199e:  ldfld      float64 MCCTest.VType7::f150
    IL_19a3:  ldarga.s   expected
    IL_19a5:  ldfld      float64 MCCTest.VType7::f150
    IL_19aa:  ceq
    IL_19ac:  stloc.0
    IL_19ad:  ldloc.0
    IL_19ae:  brtrue.s   IL_19c9

    IL_19b0:  nop
    IL_19b1:  ldstr      "f150"
    IL_19b6:  ldarg.0
    IL_19b7:  ldfld      float64 MCCTest.VType7::f150
    IL_19bc:  ldarga.s   expected
    IL_19be:  ldfld      float64 MCCTest.VType7::f150
    IL_19c3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_19c8:  throw

    IL_19c9:  ldarg.0
    IL_19ca:  ldfld      float64 MCCTest.VType7::f151
    IL_19cf:  ldarga.s   expected
    IL_19d1:  ldfld      float64 MCCTest.VType7::f151
    IL_19d6:  ceq
    IL_19d8:  stloc.0
    IL_19d9:  ldloc.0
    IL_19da:  brtrue.s   IL_19f5

    IL_19dc:  nop
    IL_19dd:  ldstr      "f151"
    IL_19e2:  ldarg.0
    IL_19e3:  ldfld      float64 MCCTest.VType7::f151
    IL_19e8:  ldarga.s   expected
    IL_19ea:  ldfld      float64 MCCTest.VType7::f151
    IL_19ef:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_19f4:  throw

    IL_19f5:  ldarg.0
    IL_19f6:  ldfld      float64 MCCTest.VType7::f152
    IL_19fb:  ldarga.s   expected
    IL_19fd:  ldfld      float64 MCCTest.VType7::f152
    IL_1a02:  ceq
    IL_1a04:  stloc.0
    IL_1a05:  ldloc.0
    IL_1a06:  brtrue.s   IL_1a21

    IL_1a08:  nop
    IL_1a09:  ldstr      "f152"
    IL_1a0e:  ldarg.0
    IL_1a0f:  ldfld      float64 MCCTest.VType7::f152
    IL_1a14:  ldarga.s   expected
    IL_1a16:  ldfld      float64 MCCTest.VType7::f152
    IL_1a1b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1a20:  throw

    IL_1a21:  ldarg.0
    IL_1a22:  ldfld      float64 MCCTest.VType7::f153
    IL_1a27:  ldarga.s   expected
    IL_1a29:  ldfld      float64 MCCTest.VType7::f153
    IL_1a2e:  ceq
    IL_1a30:  stloc.0
    IL_1a31:  ldloc.0
    IL_1a32:  brtrue.s   IL_1a4d

    IL_1a34:  nop
    IL_1a35:  ldstr      "f153"
    IL_1a3a:  ldarg.0
    IL_1a3b:  ldfld      float64 MCCTest.VType7::f153
    IL_1a40:  ldarga.s   expected
    IL_1a42:  ldfld      float64 MCCTest.VType7::f153
    IL_1a47:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1a4c:  throw

    IL_1a4d:  ldarg.0
    IL_1a4e:  ldfld      float64 MCCTest.VType7::f154
    IL_1a53:  ldarga.s   expected
    IL_1a55:  ldfld      float64 MCCTest.VType7::f154
    IL_1a5a:  ceq
    IL_1a5c:  stloc.0
    IL_1a5d:  ldloc.0
    IL_1a5e:  brtrue.s   IL_1a79

    IL_1a60:  nop
    IL_1a61:  ldstr      "f154"
    IL_1a66:  ldarg.0
    IL_1a67:  ldfld      float64 MCCTest.VType7::f154
    IL_1a6c:  ldarga.s   expected
    IL_1a6e:  ldfld      float64 MCCTest.VType7::f154
    IL_1a73:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1a78:  throw

    IL_1a79:  ldarg.0
    IL_1a7a:  ldfld      float64 MCCTest.VType7::f155
    IL_1a7f:  ldarga.s   expected
    IL_1a81:  ldfld      float64 MCCTest.VType7::f155
    IL_1a86:  ceq
    IL_1a88:  stloc.0
    IL_1a89:  ldloc.0
    IL_1a8a:  brtrue.s   IL_1aa5

    IL_1a8c:  nop
    IL_1a8d:  ldstr      "f155"
    IL_1a92:  ldarg.0
    IL_1a93:  ldfld      float64 MCCTest.VType7::f155
    IL_1a98:  ldarga.s   expected
    IL_1a9a:  ldfld      float64 MCCTest.VType7::f155
    IL_1a9f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1aa4:  throw

    IL_1aa5:  ldarg.0
    IL_1aa6:  ldfld      float64 MCCTest.VType7::f156
    IL_1aab:  ldarga.s   expected
    IL_1aad:  ldfld      float64 MCCTest.VType7::f156
    IL_1ab2:  ceq
    IL_1ab4:  stloc.0
    IL_1ab5:  ldloc.0
    IL_1ab6:  brtrue.s   IL_1ad1

    IL_1ab8:  nop
    IL_1ab9:  ldstr      "f156"
    IL_1abe:  ldarg.0
    IL_1abf:  ldfld      float64 MCCTest.VType7::f156
    IL_1ac4:  ldarga.s   expected
    IL_1ac6:  ldfld      float64 MCCTest.VType7::f156
    IL_1acb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1ad0:  throw

    IL_1ad1:  ldarg.0
    IL_1ad2:  ldfld      float64 MCCTest.VType7::f157
    IL_1ad7:  ldarga.s   expected
    IL_1ad9:  ldfld      float64 MCCTest.VType7::f157
    IL_1ade:  ceq
    IL_1ae0:  stloc.0
    IL_1ae1:  ldloc.0
    IL_1ae2:  brtrue.s   IL_1afd

    IL_1ae4:  nop
    IL_1ae5:  ldstr      "f157"
    IL_1aea:  ldarg.0
    IL_1aeb:  ldfld      float64 MCCTest.VType7::f157
    IL_1af0:  ldarga.s   expected
    IL_1af2:  ldfld      float64 MCCTest.VType7::f157
    IL_1af7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1afc:  throw

    IL_1afd:  ldarg.0
    IL_1afe:  ldfld      float64 MCCTest.VType7::f158
    IL_1b03:  ldarga.s   expected
    IL_1b05:  ldfld      float64 MCCTest.VType7::f158
    IL_1b0a:  ceq
    IL_1b0c:  stloc.0
    IL_1b0d:  ldloc.0
    IL_1b0e:  brtrue.s   IL_1b29

    IL_1b10:  nop
    IL_1b11:  ldstr      "f158"
    IL_1b16:  ldarg.0
    IL_1b17:  ldfld      float64 MCCTest.VType7::f158
    IL_1b1c:  ldarga.s   expected
    IL_1b1e:  ldfld      float64 MCCTest.VType7::f158
    IL_1b23:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1b28:  throw

    IL_1b29:  ldarg.0
    IL_1b2a:  ldfld      float64 MCCTest.VType7::f159
    IL_1b2f:  ldarga.s   expected
    IL_1b31:  ldfld      float64 MCCTest.VType7::f159
    IL_1b36:  ceq
    IL_1b38:  stloc.0
    IL_1b39:  ldloc.0
    IL_1b3a:  brtrue.s   IL_1b55

    IL_1b3c:  nop
    IL_1b3d:  ldstr      "f159"
    IL_1b42:  ldarg.0
    IL_1b43:  ldfld      float64 MCCTest.VType7::f159
    IL_1b48:  ldarga.s   expected
    IL_1b4a:  ldfld      float64 MCCTest.VType7::f159
    IL_1b4f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1b54:  throw

    IL_1b55:  ldarg.0
    IL_1b56:  ldfld      float64 MCCTest.VType7::f160
    IL_1b5b:  ldarga.s   expected
    IL_1b5d:  ldfld      float64 MCCTest.VType7::f160
    IL_1b62:  ceq
    IL_1b64:  stloc.0
    IL_1b65:  ldloc.0
    IL_1b66:  brtrue.s   IL_1b81

    IL_1b68:  nop
    IL_1b69:  ldstr      "f160"
    IL_1b6e:  ldarg.0
    IL_1b6f:  ldfld      float64 MCCTest.VType7::f160
    IL_1b74:  ldarga.s   expected
    IL_1b76:  ldfld      float64 MCCTest.VType7::f160
    IL_1b7b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1b80:  throw

    IL_1b81:  ldarg.0
    IL_1b82:  ldfld      float64 MCCTest.VType7::f161
    IL_1b87:  ldarga.s   expected
    IL_1b89:  ldfld      float64 MCCTest.VType7::f161
    IL_1b8e:  ceq
    IL_1b90:  stloc.0
    IL_1b91:  ldloc.0
    IL_1b92:  brtrue.s   IL_1bad

    IL_1b94:  nop
    IL_1b95:  ldstr      "f161"
    IL_1b9a:  ldarg.0
    IL_1b9b:  ldfld      float64 MCCTest.VType7::f161
    IL_1ba0:  ldarga.s   expected
    IL_1ba2:  ldfld      float64 MCCTest.VType7::f161
    IL_1ba7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1bac:  throw

    IL_1bad:  ldarg.0
    IL_1bae:  ldfld      float64 MCCTest.VType7::f162
    IL_1bb3:  ldarga.s   expected
    IL_1bb5:  ldfld      float64 MCCTest.VType7::f162
    IL_1bba:  ceq
    IL_1bbc:  stloc.0
    IL_1bbd:  ldloc.0
    IL_1bbe:  brtrue.s   IL_1bd9

    IL_1bc0:  nop
    IL_1bc1:  ldstr      "f162"
    IL_1bc6:  ldarg.0
    IL_1bc7:  ldfld      float64 MCCTest.VType7::f162
    IL_1bcc:  ldarga.s   expected
    IL_1bce:  ldfld      float64 MCCTest.VType7::f162
    IL_1bd3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1bd8:  throw

    IL_1bd9:  ldarg.0
    IL_1bda:  ldfld      float64 MCCTest.VType7::f163
    IL_1bdf:  ldarga.s   expected
    IL_1be1:  ldfld      float64 MCCTest.VType7::f163
    IL_1be6:  ceq
    IL_1be8:  stloc.0
    IL_1be9:  ldloc.0
    IL_1bea:  brtrue.s   IL_1c05

    IL_1bec:  nop
    IL_1bed:  ldstr      "f163"
    IL_1bf2:  ldarg.0
    IL_1bf3:  ldfld      float64 MCCTest.VType7::f163
    IL_1bf8:  ldarga.s   expected
    IL_1bfa:  ldfld      float64 MCCTest.VType7::f163
    IL_1bff:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1c04:  throw

    IL_1c05:  ldarg.0
    IL_1c06:  ldfld      float64 MCCTest.VType7::f164
    IL_1c0b:  ldarga.s   expected
    IL_1c0d:  ldfld      float64 MCCTest.VType7::f164
    IL_1c12:  ceq
    IL_1c14:  stloc.0
    IL_1c15:  ldloc.0
    IL_1c16:  brtrue.s   IL_1c31

    IL_1c18:  nop
    IL_1c19:  ldstr      "f164"
    IL_1c1e:  ldarg.0
    IL_1c1f:  ldfld      float64 MCCTest.VType7::f164
    IL_1c24:  ldarga.s   expected
    IL_1c26:  ldfld      float64 MCCTest.VType7::f164
    IL_1c2b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1c30:  throw

    IL_1c31:  ldarg.0
    IL_1c32:  ldfld      float64 MCCTest.VType7::f165
    IL_1c37:  ldarga.s   expected
    IL_1c39:  ldfld      float64 MCCTest.VType7::f165
    IL_1c3e:  ceq
    IL_1c40:  stloc.0
    IL_1c41:  ldloc.0
    IL_1c42:  brtrue.s   IL_1c5d

    IL_1c44:  nop
    IL_1c45:  ldstr      "f165"
    IL_1c4a:  ldarg.0
    IL_1c4b:  ldfld      float64 MCCTest.VType7::f165
    IL_1c50:  ldarga.s   expected
    IL_1c52:  ldfld      float64 MCCTest.VType7::f165
    IL_1c57:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1c5c:  throw

    IL_1c5d:  ldarg.0
    IL_1c5e:  ldfld      float64 MCCTest.VType7::f166
    IL_1c63:  ldarga.s   expected
    IL_1c65:  ldfld      float64 MCCTest.VType7::f166
    IL_1c6a:  ceq
    IL_1c6c:  stloc.0
    IL_1c6d:  ldloc.0
    IL_1c6e:  brtrue.s   IL_1c89

    IL_1c70:  nop
    IL_1c71:  ldstr      "f166"
    IL_1c76:  ldarg.0
    IL_1c77:  ldfld      float64 MCCTest.VType7::f166
    IL_1c7c:  ldarga.s   expected
    IL_1c7e:  ldfld      float64 MCCTest.VType7::f166
    IL_1c83:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1c88:  throw

    IL_1c89:  ldarg.0
    IL_1c8a:  ldfld      float64 MCCTest.VType7::f167
    IL_1c8f:  ldarga.s   expected
    IL_1c91:  ldfld      float64 MCCTest.VType7::f167
    IL_1c96:  ceq
    IL_1c98:  stloc.0
    IL_1c99:  ldloc.0
    IL_1c9a:  brtrue.s   IL_1cb5

    IL_1c9c:  nop
    IL_1c9d:  ldstr      "f167"
    IL_1ca2:  ldarg.0
    IL_1ca3:  ldfld      float64 MCCTest.VType7::f167
    IL_1ca8:  ldarga.s   expected
    IL_1caa:  ldfld      float64 MCCTest.VType7::f167
    IL_1caf:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1cb4:  throw

    IL_1cb5:  ldarg.0
    IL_1cb6:  ldfld      float64 MCCTest.VType7::f168
    IL_1cbb:  ldarga.s   expected
    IL_1cbd:  ldfld      float64 MCCTest.VType7::f168
    IL_1cc2:  ceq
    IL_1cc4:  stloc.0
    IL_1cc5:  ldloc.0
    IL_1cc6:  brtrue.s   IL_1ce1

    IL_1cc8:  nop
    IL_1cc9:  ldstr      "f168"
    IL_1cce:  ldarg.0
    IL_1ccf:  ldfld      float64 MCCTest.VType7::f168
    IL_1cd4:  ldarga.s   expected
    IL_1cd6:  ldfld      float64 MCCTest.VType7::f168
    IL_1cdb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1ce0:  throw

    IL_1ce1:  ldarg.0
    IL_1ce2:  ldfld      float64 MCCTest.VType7::f169
    IL_1ce7:  ldarga.s   expected
    IL_1ce9:  ldfld      float64 MCCTest.VType7::f169
    IL_1cee:  ceq
    IL_1cf0:  stloc.0
    IL_1cf1:  ldloc.0
    IL_1cf2:  brtrue.s   IL_1d0d

    IL_1cf4:  nop
    IL_1cf5:  ldstr      "f169"
    IL_1cfa:  ldarg.0
    IL_1cfb:  ldfld      float64 MCCTest.VType7::f169
    IL_1d00:  ldarga.s   expected
    IL_1d02:  ldfld      float64 MCCTest.VType7::f169
    IL_1d07:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1d0c:  throw

    IL_1d0d:  ldarg.0
    IL_1d0e:  ldfld      float64 MCCTest.VType7::f170
    IL_1d13:  ldarga.s   expected
    IL_1d15:  ldfld      float64 MCCTest.VType7::f170
    IL_1d1a:  ceq
    IL_1d1c:  stloc.0
    IL_1d1d:  ldloc.0
    IL_1d1e:  brtrue.s   IL_1d39

    IL_1d20:  nop
    IL_1d21:  ldstr      "f170"
    IL_1d26:  ldarg.0
    IL_1d27:  ldfld      float64 MCCTest.VType7::f170
    IL_1d2c:  ldarga.s   expected
    IL_1d2e:  ldfld      float64 MCCTest.VType7::f170
    IL_1d33:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1d38:  throw

    IL_1d39:  ldarg.0
    IL_1d3a:  ldfld      float64 MCCTest.VType7::f171
    IL_1d3f:  ldarga.s   expected
    IL_1d41:  ldfld      float64 MCCTest.VType7::f171
    IL_1d46:  ceq
    IL_1d48:  stloc.0
    IL_1d49:  ldloc.0
    IL_1d4a:  brtrue.s   IL_1d65

    IL_1d4c:  nop
    IL_1d4d:  ldstr      "f171"
    IL_1d52:  ldarg.0
    IL_1d53:  ldfld      float64 MCCTest.VType7::f171
    IL_1d58:  ldarga.s   expected
    IL_1d5a:  ldfld      float64 MCCTest.VType7::f171
    IL_1d5f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1d64:  throw

    IL_1d65:  ldarg.0
    IL_1d66:  ldfld      float64 MCCTest.VType7::f172
    IL_1d6b:  ldarga.s   expected
    IL_1d6d:  ldfld      float64 MCCTest.VType7::f172
    IL_1d72:  ceq
    IL_1d74:  stloc.0
    IL_1d75:  ldloc.0
    IL_1d76:  brtrue.s   IL_1d91

    IL_1d78:  nop
    IL_1d79:  ldstr      "f172"
    IL_1d7e:  ldarg.0
    IL_1d7f:  ldfld      float64 MCCTest.VType7::f172
    IL_1d84:  ldarga.s   expected
    IL_1d86:  ldfld      float64 MCCTest.VType7::f172
    IL_1d8b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1d90:  throw

    IL_1d91:  ldarg.0
    IL_1d92:  ldfld      float64 MCCTest.VType7::f173
    IL_1d97:  ldarga.s   expected
    IL_1d99:  ldfld      float64 MCCTest.VType7::f173
    IL_1d9e:  ceq
    IL_1da0:  stloc.0
    IL_1da1:  ldloc.0
    IL_1da2:  brtrue.s   IL_1dbd

    IL_1da4:  nop
    IL_1da5:  ldstr      "f173"
    IL_1daa:  ldarg.0
    IL_1dab:  ldfld      float64 MCCTest.VType7::f173
    IL_1db0:  ldarga.s   expected
    IL_1db2:  ldfld      float64 MCCTest.VType7::f173
    IL_1db7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1dbc:  throw

    IL_1dbd:  ldarg.0
    IL_1dbe:  ldfld      float64 MCCTest.VType7::f174
    IL_1dc3:  ldarga.s   expected
    IL_1dc5:  ldfld      float64 MCCTest.VType7::f174
    IL_1dca:  ceq
    IL_1dcc:  stloc.0
    IL_1dcd:  ldloc.0
    IL_1dce:  brtrue.s   IL_1de9

    IL_1dd0:  nop
    IL_1dd1:  ldstr      "f174"
    IL_1dd6:  ldarg.0
    IL_1dd7:  ldfld      float64 MCCTest.VType7::f174
    IL_1ddc:  ldarga.s   expected
    IL_1dde:  ldfld      float64 MCCTest.VType7::f174
    IL_1de3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1de8:  throw

    IL_1de9:  ldarg.0
    IL_1dea:  ldfld      float64 MCCTest.VType7::f175
    IL_1def:  ldarga.s   expected
    IL_1df1:  ldfld      float64 MCCTest.VType7::f175
    IL_1df6:  ceq
    IL_1df8:  stloc.0
    IL_1df9:  ldloc.0
    IL_1dfa:  brtrue.s   IL_1e15

    IL_1dfc:  nop
    IL_1dfd:  ldstr      "f175"
    IL_1e02:  ldarg.0
    IL_1e03:  ldfld      float64 MCCTest.VType7::f175
    IL_1e08:  ldarga.s   expected
    IL_1e0a:  ldfld      float64 MCCTest.VType7::f175
    IL_1e0f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1e14:  throw

    IL_1e15:  ldarg.0
    IL_1e16:  ldfld      float64 MCCTest.VType7::f176
    IL_1e1b:  ldarga.s   expected
    IL_1e1d:  ldfld      float64 MCCTest.VType7::f176
    IL_1e22:  ceq
    IL_1e24:  stloc.0
    IL_1e25:  ldloc.0
    IL_1e26:  brtrue.s   IL_1e41

    IL_1e28:  nop
    IL_1e29:  ldstr      "f176"
    IL_1e2e:  ldarg.0
    IL_1e2f:  ldfld      float64 MCCTest.VType7::f176
    IL_1e34:  ldarga.s   expected
    IL_1e36:  ldfld      float64 MCCTest.VType7::f176
    IL_1e3b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1e40:  throw

    IL_1e41:  ldarg.0
    IL_1e42:  ldfld      float64 MCCTest.VType7::f177
    IL_1e47:  ldarga.s   expected
    IL_1e49:  ldfld      float64 MCCTest.VType7::f177
    IL_1e4e:  ceq
    IL_1e50:  stloc.0
    IL_1e51:  ldloc.0
    IL_1e52:  brtrue.s   IL_1e6d

    IL_1e54:  nop
    IL_1e55:  ldstr      "f177"
    IL_1e5a:  ldarg.0
    IL_1e5b:  ldfld      float64 MCCTest.VType7::f177
    IL_1e60:  ldarga.s   expected
    IL_1e62:  ldfld      float64 MCCTest.VType7::f177
    IL_1e67:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1e6c:  throw

    IL_1e6d:  ldarg.0
    IL_1e6e:  ldfld      float64 MCCTest.VType7::f178
    IL_1e73:  ldarga.s   expected
    IL_1e75:  ldfld      float64 MCCTest.VType7::f178
    IL_1e7a:  ceq
    IL_1e7c:  stloc.0
    IL_1e7d:  ldloc.0
    IL_1e7e:  brtrue.s   IL_1e99

    IL_1e80:  nop
    IL_1e81:  ldstr      "f178"
    IL_1e86:  ldarg.0
    IL_1e87:  ldfld      float64 MCCTest.VType7::f178
    IL_1e8c:  ldarga.s   expected
    IL_1e8e:  ldfld      float64 MCCTest.VType7::f178
    IL_1e93:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1e98:  throw

    IL_1e99:  ldarg.0
    IL_1e9a:  ldfld      float64 MCCTest.VType7::f179
    IL_1e9f:  ldarga.s   expected
    IL_1ea1:  ldfld      float64 MCCTest.VType7::f179
    IL_1ea6:  ceq
    IL_1ea8:  stloc.0
    IL_1ea9:  ldloc.0
    IL_1eaa:  brtrue.s   IL_1ec5

    IL_1eac:  nop
    IL_1ead:  ldstr      "f179"
    IL_1eb2:  ldarg.0
    IL_1eb3:  ldfld      float64 MCCTest.VType7::f179
    IL_1eb8:  ldarga.s   expected
    IL_1eba:  ldfld      float64 MCCTest.VType7::f179
    IL_1ebf:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1ec4:  throw

    IL_1ec5:  ldarg.0
    IL_1ec6:  ldfld      float64 MCCTest.VType7::f180
    IL_1ecb:  ldarga.s   expected
    IL_1ecd:  ldfld      float64 MCCTest.VType7::f180
    IL_1ed2:  ceq
    IL_1ed4:  stloc.0
    IL_1ed5:  ldloc.0
    IL_1ed6:  brtrue.s   IL_1ef1

    IL_1ed8:  nop
    IL_1ed9:  ldstr      "f180"
    IL_1ede:  ldarg.0
    IL_1edf:  ldfld      float64 MCCTest.VType7::f180
    IL_1ee4:  ldarga.s   expected
    IL_1ee6:  ldfld      float64 MCCTest.VType7::f180
    IL_1eeb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1ef0:  throw

    IL_1ef1:  ldarg.0
    IL_1ef2:  ldfld      float64 MCCTest.VType7::f181
    IL_1ef7:  ldarga.s   expected
    IL_1ef9:  ldfld      float64 MCCTest.VType7::f181
    IL_1efe:  ceq
    IL_1f00:  stloc.0
    IL_1f01:  ldloc.0
    IL_1f02:  brtrue.s   IL_1f1d

    IL_1f04:  nop
    IL_1f05:  ldstr      "f181"
    IL_1f0a:  ldarg.0
    IL_1f0b:  ldfld      float64 MCCTest.VType7::f181
    IL_1f10:  ldarga.s   expected
    IL_1f12:  ldfld      float64 MCCTest.VType7::f181
    IL_1f17:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1f1c:  throw

    IL_1f1d:  ldarg.0
    IL_1f1e:  ldfld      float64 MCCTest.VType7::f182
    IL_1f23:  ldarga.s   expected
    IL_1f25:  ldfld      float64 MCCTest.VType7::f182
    IL_1f2a:  ceq
    IL_1f2c:  stloc.0
    IL_1f2d:  ldloc.0
    IL_1f2e:  brtrue.s   IL_1f49

    IL_1f30:  nop
    IL_1f31:  ldstr      "f182"
    IL_1f36:  ldarg.0
    IL_1f37:  ldfld      float64 MCCTest.VType7::f182
    IL_1f3c:  ldarga.s   expected
    IL_1f3e:  ldfld      float64 MCCTest.VType7::f182
    IL_1f43:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1f48:  throw

    IL_1f49:  ldarg.0
    IL_1f4a:  ldfld      float64 MCCTest.VType7::f183
    IL_1f4f:  ldarga.s   expected
    IL_1f51:  ldfld      float64 MCCTest.VType7::f183
    IL_1f56:  ceq
    IL_1f58:  stloc.0
    IL_1f59:  ldloc.0
    IL_1f5a:  brtrue.s   IL_1f75

    IL_1f5c:  nop
    IL_1f5d:  ldstr      "f183"
    IL_1f62:  ldarg.0
    IL_1f63:  ldfld      float64 MCCTest.VType7::f183
    IL_1f68:  ldarga.s   expected
    IL_1f6a:  ldfld      float64 MCCTest.VType7::f183
    IL_1f6f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1f74:  throw

    IL_1f75:  ldarg.0
    IL_1f76:  ldfld      float64 MCCTest.VType7::f184
    IL_1f7b:  ldarga.s   expected
    IL_1f7d:  ldfld      float64 MCCTest.VType7::f184
    IL_1f82:  ceq
    IL_1f84:  stloc.0
    IL_1f85:  ldloc.0
    IL_1f86:  brtrue.s   IL_1fa1

    IL_1f88:  nop
    IL_1f89:  ldstr      "f184"
    IL_1f8e:  ldarg.0
    IL_1f8f:  ldfld      float64 MCCTest.VType7::f184
    IL_1f94:  ldarga.s   expected
    IL_1f96:  ldfld      float64 MCCTest.VType7::f184
    IL_1f9b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1fa0:  throw

    IL_1fa1:  ldarg.0
    IL_1fa2:  ldfld      float64 MCCTest.VType7::f185
    IL_1fa7:  ldarga.s   expected
    IL_1fa9:  ldfld      float64 MCCTest.VType7::f185
    IL_1fae:  ceq
    IL_1fb0:  stloc.0
    IL_1fb1:  ldloc.0
    IL_1fb2:  brtrue.s   IL_1fcd

    IL_1fb4:  nop
    IL_1fb5:  ldstr      "f185"
    IL_1fba:  ldarg.0
    IL_1fbb:  ldfld      float64 MCCTest.VType7::f185
    IL_1fc0:  ldarga.s   expected
    IL_1fc2:  ldfld      float64 MCCTest.VType7::f185
    IL_1fc7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1fcc:  throw

    IL_1fcd:  ldarg.0
    IL_1fce:  ldfld      float64 MCCTest.VType7::f186
    IL_1fd3:  ldarga.s   expected
    IL_1fd5:  ldfld      float64 MCCTest.VType7::f186
    IL_1fda:  ceq
    IL_1fdc:  stloc.0
    IL_1fdd:  ldloc.0
    IL_1fde:  brtrue.s   IL_1ff9

    IL_1fe0:  nop
    IL_1fe1:  ldstr      "f186"
    IL_1fe6:  ldarg.0
    IL_1fe7:  ldfld      float64 MCCTest.VType7::f186
    IL_1fec:  ldarga.s   expected
    IL_1fee:  ldfld      float64 MCCTest.VType7::f186
    IL_1ff3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_1ff8:  throw

    IL_1ff9:  ldarg.0
    IL_1ffa:  ldfld      float64 MCCTest.VType7::f187
    IL_1fff:  ldarga.s   expected
    IL_2001:  ldfld      float64 MCCTest.VType7::f187
    IL_2006:  ceq
    IL_2008:  stloc.0
    IL_2009:  ldloc.0
    IL_200a:  brtrue.s   IL_2025

    IL_200c:  nop
    IL_200d:  ldstr      "f187"
    IL_2012:  ldarg.0
    IL_2013:  ldfld      float64 MCCTest.VType7::f187
    IL_2018:  ldarga.s   expected
    IL_201a:  ldfld      float64 MCCTest.VType7::f187
    IL_201f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2024:  throw

    IL_2025:  ldarg.0
    IL_2026:  ldfld      float64 MCCTest.VType7::f188
    IL_202b:  ldarga.s   expected
    IL_202d:  ldfld      float64 MCCTest.VType7::f188
    IL_2032:  ceq
    IL_2034:  stloc.0
    IL_2035:  ldloc.0
    IL_2036:  brtrue.s   IL_2051

    IL_2038:  nop
    IL_2039:  ldstr      "f188"
    IL_203e:  ldarg.0
    IL_203f:  ldfld      float64 MCCTest.VType7::f188
    IL_2044:  ldarga.s   expected
    IL_2046:  ldfld      float64 MCCTest.VType7::f188
    IL_204b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2050:  throw

    IL_2051:  ldarg.0
    IL_2052:  ldfld      float64 MCCTest.VType7::f189
    IL_2057:  ldarga.s   expected
    IL_2059:  ldfld      float64 MCCTest.VType7::f189
    IL_205e:  ceq
    IL_2060:  stloc.0
    IL_2061:  ldloc.0
    IL_2062:  brtrue.s   IL_207d

    IL_2064:  nop
    IL_2065:  ldstr      "f189"
    IL_206a:  ldarg.0
    IL_206b:  ldfld      float64 MCCTest.VType7::f189
    IL_2070:  ldarga.s   expected
    IL_2072:  ldfld      float64 MCCTest.VType7::f189
    IL_2077:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_207c:  throw

    IL_207d:  ldarg.0
    IL_207e:  ldfld      float64 MCCTest.VType7::f190
    IL_2083:  ldarga.s   expected
    IL_2085:  ldfld      float64 MCCTest.VType7::f190
    IL_208a:  ceq
    IL_208c:  stloc.0
    IL_208d:  ldloc.0
    IL_208e:  brtrue.s   IL_20a9

    IL_2090:  nop
    IL_2091:  ldstr      "f190"
    IL_2096:  ldarg.0
    IL_2097:  ldfld      float64 MCCTest.VType7::f190
    IL_209c:  ldarga.s   expected
    IL_209e:  ldfld      float64 MCCTest.VType7::f190
    IL_20a3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_20a8:  throw

    IL_20a9:  ldarg.0
    IL_20aa:  ldfld      float64 MCCTest.VType7::f191
    IL_20af:  ldarga.s   expected
    IL_20b1:  ldfld      float64 MCCTest.VType7::f191
    IL_20b6:  ceq
    IL_20b8:  stloc.0
    IL_20b9:  ldloc.0
    IL_20ba:  brtrue.s   IL_20d5

    IL_20bc:  nop
    IL_20bd:  ldstr      "f191"
    IL_20c2:  ldarg.0
    IL_20c3:  ldfld      float64 MCCTest.VType7::f191
    IL_20c8:  ldarga.s   expected
    IL_20ca:  ldfld      float64 MCCTest.VType7::f191
    IL_20cf:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_20d4:  throw

    IL_20d5:  ldarg.0
    IL_20d6:  ldfld      float64 MCCTest.VType7::f192
    IL_20db:  ldarga.s   expected
    IL_20dd:  ldfld      float64 MCCTest.VType7::f192
    IL_20e2:  ceq
    IL_20e4:  stloc.0
    IL_20e5:  ldloc.0
    IL_20e6:  brtrue.s   IL_2101

    IL_20e8:  nop
    IL_20e9:  ldstr      "f192"
    IL_20ee:  ldarg.0
    IL_20ef:  ldfld      float64 MCCTest.VType7::f192
    IL_20f4:  ldarga.s   expected
    IL_20f6:  ldfld      float64 MCCTest.VType7::f192
    IL_20fb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2100:  throw

    IL_2101:  ldarg.0
    IL_2102:  ldfld      float64 MCCTest.VType7::f193
    IL_2107:  ldarga.s   expected
    IL_2109:  ldfld      float64 MCCTest.VType7::f193
    IL_210e:  ceq
    IL_2110:  stloc.0
    IL_2111:  ldloc.0
    IL_2112:  brtrue.s   IL_212d

    IL_2114:  nop
    IL_2115:  ldstr      "f193"
    IL_211a:  ldarg.0
    IL_211b:  ldfld      float64 MCCTest.VType7::f193
    IL_2120:  ldarga.s   expected
    IL_2122:  ldfld      float64 MCCTest.VType7::f193
    IL_2127:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_212c:  throw

    IL_212d:  ldarg.0
    IL_212e:  ldfld      float64 MCCTest.VType7::f194
    IL_2133:  ldarga.s   expected
    IL_2135:  ldfld      float64 MCCTest.VType7::f194
    IL_213a:  ceq
    IL_213c:  stloc.0
    IL_213d:  ldloc.0
    IL_213e:  brtrue.s   IL_2159

    IL_2140:  nop
    IL_2141:  ldstr      "f194"
    IL_2146:  ldarg.0
    IL_2147:  ldfld      float64 MCCTest.VType7::f194
    IL_214c:  ldarga.s   expected
    IL_214e:  ldfld      float64 MCCTest.VType7::f194
    IL_2153:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2158:  throw

    IL_2159:  ldarg.0
    IL_215a:  ldfld      float64 MCCTest.VType7::f195
    IL_215f:  ldarga.s   expected
    IL_2161:  ldfld      float64 MCCTest.VType7::f195
    IL_2166:  ceq
    IL_2168:  stloc.0
    IL_2169:  ldloc.0
    IL_216a:  brtrue.s   IL_2185

    IL_216c:  nop
    IL_216d:  ldstr      "f195"
    IL_2172:  ldarg.0
    IL_2173:  ldfld      float64 MCCTest.VType7::f195
    IL_2178:  ldarga.s   expected
    IL_217a:  ldfld      float64 MCCTest.VType7::f195
    IL_217f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2184:  throw

    IL_2185:  ldarg.0
    IL_2186:  ldfld      float64 MCCTest.VType7::f196
    IL_218b:  ldarga.s   expected
    IL_218d:  ldfld      float64 MCCTest.VType7::f196
    IL_2192:  ceq
    IL_2194:  stloc.0
    IL_2195:  ldloc.0
    IL_2196:  brtrue.s   IL_21b1

    IL_2198:  nop
    IL_2199:  ldstr      "f196"
    IL_219e:  ldarg.0
    IL_219f:  ldfld      float64 MCCTest.VType7::f196
    IL_21a4:  ldarga.s   expected
    IL_21a6:  ldfld      float64 MCCTest.VType7::f196
    IL_21ab:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_21b0:  throw

    IL_21b1:  ldarg.0
    IL_21b2:  ldfld      float64 MCCTest.VType7::f197
    IL_21b7:  ldarga.s   expected
    IL_21b9:  ldfld      float64 MCCTest.VType7::f197
    IL_21be:  ceq
    IL_21c0:  stloc.0
    IL_21c1:  ldloc.0
    IL_21c2:  brtrue.s   IL_21dd

    IL_21c4:  nop
    IL_21c5:  ldstr      "f197"
    IL_21ca:  ldarg.0
    IL_21cb:  ldfld      float64 MCCTest.VType7::f197
    IL_21d0:  ldarga.s   expected
    IL_21d2:  ldfld      float64 MCCTest.VType7::f197
    IL_21d7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_21dc:  throw

    IL_21dd:  ldarg.0
    IL_21de:  ldfld      float64 MCCTest.VType7::f198
    IL_21e3:  ldarga.s   expected
    IL_21e5:  ldfld      float64 MCCTest.VType7::f198
    IL_21ea:  ceq
    IL_21ec:  stloc.0
    IL_21ed:  ldloc.0
    IL_21ee:  brtrue.s   IL_2209

    IL_21f0:  nop
    IL_21f1:  ldstr      "f198"
    IL_21f6:  ldarg.0
    IL_21f7:  ldfld      float64 MCCTest.VType7::f198
    IL_21fc:  ldarga.s   expected
    IL_21fe:  ldfld      float64 MCCTest.VType7::f198
    IL_2203:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2208:  throw

    IL_2209:  ldarg.0
    IL_220a:  ldfld      float64 MCCTest.VType7::f199
    IL_220f:  ldarga.s   expected
    IL_2211:  ldfld      float64 MCCTest.VType7::f199
    IL_2216:  ceq
    IL_2218:  stloc.0
    IL_2219:  ldloc.0
    IL_221a:  brtrue.s   IL_2235

    IL_221c:  nop
    IL_221d:  ldstr      "f199"
    IL_2222:  ldarg.0
    IL_2223:  ldfld      float64 MCCTest.VType7::f199
    IL_2228:  ldarga.s   expected
    IL_222a:  ldfld      float64 MCCTest.VType7::f199
    IL_222f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2234:  throw

    IL_2235:  ldarg.0
    IL_2236:  ldfld      float64 MCCTest.VType7::f200
    IL_223b:  ldarga.s   expected
    IL_223d:  ldfld      float64 MCCTest.VType7::f200
    IL_2242:  ceq
    IL_2244:  stloc.0
    IL_2245:  ldloc.0
    IL_2246:  brtrue.s   IL_2261

    IL_2248:  nop
    IL_2249:  ldstr      "f200"
    IL_224e:  ldarg.0
    IL_224f:  ldfld      float64 MCCTest.VType7::f200
    IL_2254:  ldarga.s   expected
    IL_2256:  ldfld      float64 MCCTest.VType7::f200
    IL_225b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2260:  throw

    IL_2261:  ldarg.0
    IL_2262:  ldfld      float64 MCCTest.VType7::f201
    IL_2267:  ldarga.s   expected
    IL_2269:  ldfld      float64 MCCTest.VType7::f201
    IL_226e:  ceq
    IL_2270:  stloc.0
    IL_2271:  ldloc.0
    IL_2272:  brtrue.s   IL_228d

    IL_2274:  nop
    IL_2275:  ldstr      "f201"
    IL_227a:  ldarg.0
    IL_227b:  ldfld      float64 MCCTest.VType7::f201
    IL_2280:  ldarga.s   expected
    IL_2282:  ldfld      float64 MCCTest.VType7::f201
    IL_2287:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_228c:  throw

    IL_228d:  ldarg.0
    IL_228e:  ldfld      float64 MCCTest.VType7::f202
    IL_2293:  ldarga.s   expected
    IL_2295:  ldfld      float64 MCCTest.VType7::f202
    IL_229a:  ceq
    IL_229c:  stloc.0
    IL_229d:  ldloc.0
    IL_229e:  brtrue.s   IL_22b9

    IL_22a0:  nop
    IL_22a1:  ldstr      "f202"
    IL_22a6:  ldarg.0
    IL_22a7:  ldfld      float64 MCCTest.VType7::f202
    IL_22ac:  ldarga.s   expected
    IL_22ae:  ldfld      float64 MCCTest.VType7::f202
    IL_22b3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_22b8:  throw

    IL_22b9:  ldarg.0
    IL_22ba:  ldfld      float64 MCCTest.VType7::f203
    IL_22bf:  ldarga.s   expected
    IL_22c1:  ldfld      float64 MCCTest.VType7::f203
    IL_22c6:  ceq
    IL_22c8:  stloc.0
    IL_22c9:  ldloc.0
    IL_22ca:  brtrue.s   IL_22e5

    IL_22cc:  nop
    IL_22cd:  ldstr      "f203"
    IL_22d2:  ldarg.0
    IL_22d3:  ldfld      float64 MCCTest.VType7::f203
    IL_22d8:  ldarga.s   expected
    IL_22da:  ldfld      float64 MCCTest.VType7::f203
    IL_22df:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_22e4:  throw

    IL_22e5:  ldarg.0
    IL_22e6:  ldfld      float64 MCCTest.VType7::f204
    IL_22eb:  ldarga.s   expected
    IL_22ed:  ldfld      float64 MCCTest.VType7::f204
    IL_22f2:  ceq
    IL_22f4:  stloc.0
    IL_22f5:  ldloc.0
    IL_22f6:  brtrue.s   IL_2311

    IL_22f8:  nop
    IL_22f9:  ldstr      "f204"
    IL_22fe:  ldarg.0
    IL_22ff:  ldfld      float64 MCCTest.VType7::f204
    IL_2304:  ldarga.s   expected
    IL_2306:  ldfld      float64 MCCTest.VType7::f204
    IL_230b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2310:  throw

    IL_2311:  ldarg.0
    IL_2312:  ldfld      float64 MCCTest.VType7::f205
    IL_2317:  ldarga.s   expected
    IL_2319:  ldfld      float64 MCCTest.VType7::f205
    IL_231e:  ceq
    IL_2320:  stloc.0
    IL_2321:  ldloc.0
    IL_2322:  brtrue.s   IL_233d

    IL_2324:  nop
    IL_2325:  ldstr      "f205"
    IL_232a:  ldarg.0
    IL_232b:  ldfld      float64 MCCTest.VType7::f205
    IL_2330:  ldarga.s   expected
    IL_2332:  ldfld      float64 MCCTest.VType7::f205
    IL_2337:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_233c:  throw

    IL_233d:  ldarg.0
    IL_233e:  ldfld      float64 MCCTest.VType7::f206
    IL_2343:  ldarga.s   expected
    IL_2345:  ldfld      float64 MCCTest.VType7::f206
    IL_234a:  ceq
    IL_234c:  stloc.0
    IL_234d:  ldloc.0
    IL_234e:  brtrue.s   IL_2369

    IL_2350:  nop
    IL_2351:  ldstr      "f206"
    IL_2356:  ldarg.0
    IL_2357:  ldfld      float64 MCCTest.VType7::f206
    IL_235c:  ldarga.s   expected
    IL_235e:  ldfld      float64 MCCTest.VType7::f206
    IL_2363:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2368:  throw

    IL_2369:  ldarg.0
    IL_236a:  ldfld      float64 MCCTest.VType7::f207
    IL_236f:  ldarga.s   expected
    IL_2371:  ldfld      float64 MCCTest.VType7::f207
    IL_2376:  ceq
    IL_2378:  stloc.0
    IL_2379:  ldloc.0
    IL_237a:  brtrue.s   IL_2395

    IL_237c:  nop
    IL_237d:  ldstr      "f207"
    IL_2382:  ldarg.0
    IL_2383:  ldfld      float64 MCCTest.VType7::f207
    IL_2388:  ldarga.s   expected
    IL_238a:  ldfld      float64 MCCTest.VType7::f207
    IL_238f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2394:  throw

    IL_2395:  ldarg.0
    IL_2396:  ldfld      float64 MCCTest.VType7::f208
    IL_239b:  ldarga.s   expected
    IL_239d:  ldfld      float64 MCCTest.VType7::f208
    IL_23a2:  ceq
    IL_23a4:  stloc.0
    IL_23a5:  ldloc.0
    IL_23a6:  brtrue.s   IL_23c1

    IL_23a8:  nop
    IL_23a9:  ldstr      "f208"
    IL_23ae:  ldarg.0
    IL_23af:  ldfld      float64 MCCTest.VType7::f208
    IL_23b4:  ldarga.s   expected
    IL_23b6:  ldfld      float64 MCCTest.VType7::f208
    IL_23bb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_23c0:  throw

    IL_23c1:  ldarg.0
    IL_23c2:  ldfld      float64 MCCTest.VType7::f209
    IL_23c7:  ldarga.s   expected
    IL_23c9:  ldfld      float64 MCCTest.VType7::f209
    IL_23ce:  ceq
    IL_23d0:  stloc.0
    IL_23d1:  ldloc.0
    IL_23d2:  brtrue.s   IL_23ed

    IL_23d4:  nop
    IL_23d5:  ldstr      "f209"
    IL_23da:  ldarg.0
    IL_23db:  ldfld      float64 MCCTest.VType7::f209
    IL_23e0:  ldarga.s   expected
    IL_23e2:  ldfld      float64 MCCTest.VType7::f209
    IL_23e7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_23ec:  throw

    IL_23ed:  ldarg.0
    IL_23ee:  ldfld      float64 MCCTest.VType7::f210
    IL_23f3:  ldarga.s   expected
    IL_23f5:  ldfld      float64 MCCTest.VType7::f210
    IL_23fa:  ceq
    IL_23fc:  stloc.0
    IL_23fd:  ldloc.0
    IL_23fe:  brtrue.s   IL_2419

    IL_2400:  nop
    IL_2401:  ldstr      "f210"
    IL_2406:  ldarg.0
    IL_2407:  ldfld      float64 MCCTest.VType7::f210
    IL_240c:  ldarga.s   expected
    IL_240e:  ldfld      float64 MCCTest.VType7::f210
    IL_2413:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2418:  throw

    IL_2419:  ldarg.0
    IL_241a:  ldfld      float64 MCCTest.VType7::f211
    IL_241f:  ldarga.s   expected
    IL_2421:  ldfld      float64 MCCTest.VType7::f211
    IL_2426:  ceq
    IL_2428:  stloc.0
    IL_2429:  ldloc.0
    IL_242a:  brtrue.s   IL_2445

    IL_242c:  nop
    IL_242d:  ldstr      "f211"
    IL_2432:  ldarg.0
    IL_2433:  ldfld      float64 MCCTest.VType7::f211
    IL_2438:  ldarga.s   expected
    IL_243a:  ldfld      float64 MCCTest.VType7::f211
    IL_243f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2444:  throw

    IL_2445:  ldarg.0
    IL_2446:  ldfld      float64 MCCTest.VType7::f212
    IL_244b:  ldarga.s   expected
    IL_244d:  ldfld      float64 MCCTest.VType7::f212
    IL_2452:  ceq
    IL_2454:  stloc.0
    IL_2455:  ldloc.0
    IL_2456:  brtrue.s   IL_2471

    IL_2458:  nop
    IL_2459:  ldstr      "f212"
    IL_245e:  ldarg.0
    IL_245f:  ldfld      float64 MCCTest.VType7::f212
    IL_2464:  ldarga.s   expected
    IL_2466:  ldfld      float64 MCCTest.VType7::f212
    IL_246b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2470:  throw

    IL_2471:  ldarg.0
    IL_2472:  ldfld      float64 MCCTest.VType7::f213
    IL_2477:  ldarga.s   expected
    IL_2479:  ldfld      float64 MCCTest.VType7::f213
    IL_247e:  ceq
    IL_2480:  stloc.0
    IL_2481:  ldloc.0
    IL_2482:  brtrue.s   IL_249d

    IL_2484:  nop
    IL_2485:  ldstr      "f213"
    IL_248a:  ldarg.0
    IL_248b:  ldfld      float64 MCCTest.VType7::f213
    IL_2490:  ldarga.s   expected
    IL_2492:  ldfld      float64 MCCTest.VType7::f213
    IL_2497:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_249c:  throw

    IL_249d:  ldarg.0
    IL_249e:  ldfld      float64 MCCTest.VType7::f214
    IL_24a3:  ldarga.s   expected
    IL_24a5:  ldfld      float64 MCCTest.VType7::f214
    IL_24aa:  ceq
    IL_24ac:  stloc.0
    IL_24ad:  ldloc.0
    IL_24ae:  brtrue.s   IL_24c9

    IL_24b0:  nop
    IL_24b1:  ldstr      "f214"
    IL_24b6:  ldarg.0
    IL_24b7:  ldfld      float64 MCCTest.VType7::f214
    IL_24bc:  ldarga.s   expected
    IL_24be:  ldfld      float64 MCCTest.VType7::f214
    IL_24c3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_24c8:  throw

    IL_24c9:  ldarg.0
    IL_24ca:  ldfld      float64 MCCTest.VType7::f215
    IL_24cf:  ldarga.s   expected
    IL_24d1:  ldfld      float64 MCCTest.VType7::f215
    IL_24d6:  ceq
    IL_24d8:  stloc.0
    IL_24d9:  ldloc.0
    IL_24da:  brtrue.s   IL_24f5

    IL_24dc:  nop
    IL_24dd:  ldstr      "f215"
    IL_24e2:  ldarg.0
    IL_24e3:  ldfld      float64 MCCTest.VType7::f215
    IL_24e8:  ldarga.s   expected
    IL_24ea:  ldfld      float64 MCCTest.VType7::f215
    IL_24ef:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_24f4:  throw

    IL_24f5:  ldarg.0
    IL_24f6:  ldfld      float64 MCCTest.VType7::f216
    IL_24fb:  ldarga.s   expected
    IL_24fd:  ldfld      float64 MCCTest.VType7::f216
    IL_2502:  ceq
    IL_2504:  stloc.0
    IL_2505:  ldloc.0
    IL_2506:  brtrue.s   IL_2521

    IL_2508:  nop
    IL_2509:  ldstr      "f216"
    IL_250e:  ldarg.0
    IL_250f:  ldfld      float64 MCCTest.VType7::f216
    IL_2514:  ldarga.s   expected
    IL_2516:  ldfld      float64 MCCTest.VType7::f216
    IL_251b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2520:  throw

    IL_2521:  ldarg.0
    IL_2522:  ldfld      float64 MCCTest.VType7::f217
    IL_2527:  ldarga.s   expected
    IL_2529:  ldfld      float64 MCCTest.VType7::f217
    IL_252e:  ceq
    IL_2530:  stloc.0
    IL_2531:  ldloc.0
    IL_2532:  brtrue.s   IL_254d

    IL_2534:  nop
    IL_2535:  ldstr      "f217"
    IL_253a:  ldarg.0
    IL_253b:  ldfld      float64 MCCTest.VType7::f217
    IL_2540:  ldarga.s   expected
    IL_2542:  ldfld      float64 MCCTest.VType7::f217
    IL_2547:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_254c:  throw

    IL_254d:  ldarg.0
    IL_254e:  ldfld      float64 MCCTest.VType7::f218
    IL_2553:  ldarga.s   expected
    IL_2555:  ldfld      float64 MCCTest.VType7::f218
    IL_255a:  ceq
    IL_255c:  stloc.0
    IL_255d:  ldloc.0
    IL_255e:  brtrue.s   IL_2579

    IL_2560:  nop
    IL_2561:  ldstr      "f218"
    IL_2566:  ldarg.0
    IL_2567:  ldfld      float64 MCCTest.VType7::f218
    IL_256c:  ldarga.s   expected
    IL_256e:  ldfld      float64 MCCTest.VType7::f218
    IL_2573:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2578:  throw

    IL_2579:  ldarg.0
    IL_257a:  ldfld      float64 MCCTest.VType7::f219
    IL_257f:  ldarga.s   expected
    IL_2581:  ldfld      float64 MCCTest.VType7::f219
    IL_2586:  ceq
    IL_2588:  stloc.0
    IL_2589:  ldloc.0
    IL_258a:  brtrue.s   IL_25a5

    IL_258c:  nop
    IL_258d:  ldstr      "f219"
    IL_2592:  ldarg.0
    IL_2593:  ldfld      float64 MCCTest.VType7::f219
    IL_2598:  ldarga.s   expected
    IL_259a:  ldfld      float64 MCCTest.VType7::f219
    IL_259f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_25a4:  throw

    IL_25a5:  ldarg.0
    IL_25a6:  ldfld      float64 MCCTest.VType7::f220
    IL_25ab:  ldarga.s   expected
    IL_25ad:  ldfld      float64 MCCTest.VType7::f220
    IL_25b2:  ceq
    IL_25b4:  stloc.0
    IL_25b5:  ldloc.0
    IL_25b6:  brtrue.s   IL_25d1

    IL_25b8:  nop
    IL_25b9:  ldstr      "f220"
    IL_25be:  ldarg.0
    IL_25bf:  ldfld      float64 MCCTest.VType7::f220
    IL_25c4:  ldarga.s   expected
    IL_25c6:  ldfld      float64 MCCTest.VType7::f220
    IL_25cb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_25d0:  throw

    IL_25d1:  ldarg.0
    IL_25d2:  ldfld      float64 MCCTest.VType7::f221
    IL_25d7:  ldarga.s   expected
    IL_25d9:  ldfld      float64 MCCTest.VType7::f221
    IL_25de:  ceq
    IL_25e0:  stloc.0
    IL_25e1:  ldloc.0
    IL_25e2:  brtrue.s   IL_25fd

    IL_25e4:  nop
    IL_25e5:  ldstr      "f221"
    IL_25ea:  ldarg.0
    IL_25eb:  ldfld      float64 MCCTest.VType7::f221
    IL_25f0:  ldarga.s   expected
    IL_25f2:  ldfld      float64 MCCTest.VType7::f221
    IL_25f7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_25fc:  throw

    IL_25fd:  ldarg.0
    IL_25fe:  ldfld      float64 MCCTest.VType7::f222
    IL_2603:  ldarga.s   expected
    IL_2605:  ldfld      float64 MCCTest.VType7::f222
    IL_260a:  ceq
    IL_260c:  stloc.0
    IL_260d:  ldloc.0
    IL_260e:  brtrue.s   IL_2629

    IL_2610:  nop
    IL_2611:  ldstr      "f222"
    IL_2616:  ldarg.0
    IL_2617:  ldfld      float64 MCCTest.VType7::f222
    IL_261c:  ldarga.s   expected
    IL_261e:  ldfld      float64 MCCTest.VType7::f222
    IL_2623:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2628:  throw

    IL_2629:  ldarg.0
    IL_262a:  ldfld      float64 MCCTest.VType7::f223
    IL_262f:  ldarga.s   expected
    IL_2631:  ldfld      float64 MCCTest.VType7::f223
    IL_2636:  ceq
    IL_2638:  stloc.0
    IL_2639:  ldloc.0
    IL_263a:  brtrue.s   IL_2655

    IL_263c:  nop
    IL_263d:  ldstr      "f223"
    IL_2642:  ldarg.0
    IL_2643:  ldfld      float64 MCCTest.VType7::f223
    IL_2648:  ldarga.s   expected
    IL_264a:  ldfld      float64 MCCTest.VType7::f223
    IL_264f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2654:  throw

    IL_2655:  ldarg.0
    IL_2656:  ldfld      float64 MCCTest.VType7::f224
    IL_265b:  ldarga.s   expected
    IL_265d:  ldfld      float64 MCCTest.VType7::f224
    IL_2662:  ceq
    IL_2664:  stloc.0
    IL_2665:  ldloc.0
    IL_2666:  brtrue.s   IL_2681

    IL_2668:  nop
    IL_2669:  ldstr      "f224"
    IL_266e:  ldarg.0
    IL_266f:  ldfld      float64 MCCTest.VType7::f224
    IL_2674:  ldarga.s   expected
    IL_2676:  ldfld      float64 MCCTest.VType7::f224
    IL_267b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2680:  throw

    IL_2681:  ldarg.0
    IL_2682:  ldfld      float64 MCCTest.VType7::f225
    IL_2687:  ldarga.s   expected
    IL_2689:  ldfld      float64 MCCTest.VType7::f225
    IL_268e:  ceq
    IL_2690:  stloc.0
    IL_2691:  ldloc.0
    IL_2692:  brtrue.s   IL_26ad

    IL_2694:  nop
    IL_2695:  ldstr      "f225"
    IL_269a:  ldarg.0
    IL_269b:  ldfld      float64 MCCTest.VType7::f225
    IL_26a0:  ldarga.s   expected
    IL_26a2:  ldfld      float64 MCCTest.VType7::f225
    IL_26a7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_26ac:  throw

    IL_26ad:  ldarg.0
    IL_26ae:  ldfld      float64 MCCTest.VType7::f226
    IL_26b3:  ldarga.s   expected
    IL_26b5:  ldfld      float64 MCCTest.VType7::f226
    IL_26ba:  ceq
    IL_26bc:  stloc.0
    IL_26bd:  ldloc.0
    IL_26be:  brtrue.s   IL_26d9

    IL_26c0:  nop
    IL_26c1:  ldstr      "f226"
    IL_26c6:  ldarg.0
    IL_26c7:  ldfld      float64 MCCTest.VType7::f226
    IL_26cc:  ldarga.s   expected
    IL_26ce:  ldfld      float64 MCCTest.VType7::f226
    IL_26d3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_26d8:  throw

    IL_26d9:  ldarg.0
    IL_26da:  ldfld      float64 MCCTest.VType7::f227
    IL_26df:  ldarga.s   expected
    IL_26e1:  ldfld      float64 MCCTest.VType7::f227
    IL_26e6:  ceq
    IL_26e8:  stloc.0
    IL_26e9:  ldloc.0
    IL_26ea:  brtrue.s   IL_2705

    IL_26ec:  nop
    IL_26ed:  ldstr      "f227"
    IL_26f2:  ldarg.0
    IL_26f3:  ldfld      float64 MCCTest.VType7::f227
    IL_26f8:  ldarga.s   expected
    IL_26fa:  ldfld      float64 MCCTest.VType7::f227
    IL_26ff:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2704:  throw

    IL_2705:  ldarg.0
    IL_2706:  ldfld      float64 MCCTest.VType7::f228
    IL_270b:  ldarga.s   expected
    IL_270d:  ldfld      float64 MCCTest.VType7::f228
    IL_2712:  ceq
    IL_2714:  stloc.0
    IL_2715:  ldloc.0
    IL_2716:  brtrue.s   IL_2731

    IL_2718:  nop
    IL_2719:  ldstr      "f228"
    IL_271e:  ldarg.0
    IL_271f:  ldfld      float64 MCCTest.VType7::f228
    IL_2724:  ldarga.s   expected
    IL_2726:  ldfld      float64 MCCTest.VType7::f228
    IL_272b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2730:  throw

    IL_2731:  ldarg.0
    IL_2732:  ldfld      float64 MCCTest.VType7::f229
    IL_2737:  ldarga.s   expected
    IL_2739:  ldfld      float64 MCCTest.VType7::f229
    IL_273e:  ceq
    IL_2740:  stloc.0
    IL_2741:  ldloc.0
    IL_2742:  brtrue.s   IL_275d

    IL_2744:  nop
    IL_2745:  ldstr      "f229"
    IL_274a:  ldarg.0
    IL_274b:  ldfld      float64 MCCTest.VType7::f229
    IL_2750:  ldarga.s   expected
    IL_2752:  ldfld      float64 MCCTest.VType7::f229
    IL_2757:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_275c:  throw

    IL_275d:  ldarg.0
    IL_275e:  ldfld      float64 MCCTest.VType7::f230
    IL_2763:  ldarga.s   expected
    IL_2765:  ldfld      float64 MCCTest.VType7::f230
    IL_276a:  ceq
    IL_276c:  stloc.0
    IL_276d:  ldloc.0
    IL_276e:  brtrue.s   IL_2789

    IL_2770:  nop
    IL_2771:  ldstr      "f230"
    IL_2776:  ldarg.0
    IL_2777:  ldfld      float64 MCCTest.VType7::f230
    IL_277c:  ldarga.s   expected
    IL_277e:  ldfld      float64 MCCTest.VType7::f230
    IL_2783:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2788:  throw

    IL_2789:  ldarg.0
    IL_278a:  ldfld      float64 MCCTest.VType7::f231
    IL_278f:  ldarga.s   expected
    IL_2791:  ldfld      float64 MCCTest.VType7::f231
    IL_2796:  ceq
    IL_2798:  stloc.0
    IL_2799:  ldloc.0
    IL_279a:  brtrue.s   IL_27b5

    IL_279c:  nop
    IL_279d:  ldstr      "f231"
    IL_27a2:  ldarg.0
    IL_27a3:  ldfld      float64 MCCTest.VType7::f231
    IL_27a8:  ldarga.s   expected
    IL_27aa:  ldfld      float64 MCCTest.VType7::f231
    IL_27af:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_27b4:  throw

    IL_27b5:  ldarg.0
    IL_27b6:  ldfld      float64 MCCTest.VType7::f232
    IL_27bb:  ldarga.s   expected
    IL_27bd:  ldfld      float64 MCCTest.VType7::f232
    IL_27c2:  ceq
    IL_27c4:  stloc.0
    IL_27c5:  ldloc.0
    IL_27c6:  brtrue.s   IL_27e1

    IL_27c8:  nop
    IL_27c9:  ldstr      "f232"
    IL_27ce:  ldarg.0
    IL_27cf:  ldfld      float64 MCCTest.VType7::f232
    IL_27d4:  ldarga.s   expected
    IL_27d6:  ldfld      float64 MCCTest.VType7::f232
    IL_27db:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_27e0:  throw

    IL_27e1:  ldarg.0
    IL_27e2:  ldfld      float64 MCCTest.VType7::f233
    IL_27e7:  ldarga.s   expected
    IL_27e9:  ldfld      float64 MCCTest.VType7::f233
    IL_27ee:  ceq
    IL_27f0:  stloc.0
    IL_27f1:  ldloc.0
    IL_27f2:  brtrue.s   IL_280d

    IL_27f4:  nop
    IL_27f5:  ldstr      "f233"
    IL_27fa:  ldarg.0
    IL_27fb:  ldfld      float64 MCCTest.VType7::f233
    IL_2800:  ldarga.s   expected
    IL_2802:  ldfld      float64 MCCTest.VType7::f233
    IL_2807:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_280c:  throw

    IL_280d:  ldarg.0
    IL_280e:  ldfld      float64 MCCTest.VType7::f234
    IL_2813:  ldarga.s   expected
    IL_2815:  ldfld      float64 MCCTest.VType7::f234
    IL_281a:  ceq
    IL_281c:  stloc.0
    IL_281d:  ldloc.0
    IL_281e:  brtrue.s   IL_2839

    IL_2820:  nop
    IL_2821:  ldstr      "f234"
    IL_2826:  ldarg.0
    IL_2827:  ldfld      float64 MCCTest.VType7::f234
    IL_282c:  ldarga.s   expected
    IL_282e:  ldfld      float64 MCCTest.VType7::f234
    IL_2833:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2838:  throw

    IL_2839:  ldarg.0
    IL_283a:  ldfld      float64 MCCTest.VType7::f235
    IL_283f:  ldarga.s   expected
    IL_2841:  ldfld      float64 MCCTest.VType7::f235
    IL_2846:  ceq
    IL_2848:  stloc.0
    IL_2849:  ldloc.0
    IL_284a:  brtrue.s   IL_2865

    IL_284c:  nop
    IL_284d:  ldstr      "f235"
    IL_2852:  ldarg.0
    IL_2853:  ldfld      float64 MCCTest.VType7::f235
    IL_2858:  ldarga.s   expected
    IL_285a:  ldfld      float64 MCCTest.VType7::f235
    IL_285f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2864:  throw

    IL_2865:  ldarg.0
    IL_2866:  ldfld      float64 MCCTest.VType7::f236
    IL_286b:  ldarga.s   expected
    IL_286d:  ldfld      float64 MCCTest.VType7::f236
    IL_2872:  ceq
    IL_2874:  stloc.0
    IL_2875:  ldloc.0
    IL_2876:  brtrue.s   IL_2891

    IL_2878:  nop
    IL_2879:  ldstr      "f236"
    IL_287e:  ldarg.0
    IL_287f:  ldfld      float64 MCCTest.VType7::f236
    IL_2884:  ldarga.s   expected
    IL_2886:  ldfld      float64 MCCTest.VType7::f236
    IL_288b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2890:  throw

    IL_2891:  ldarg.0
    IL_2892:  ldfld      float64 MCCTest.VType7::f237
    IL_2897:  ldarga.s   expected
    IL_2899:  ldfld      float64 MCCTest.VType7::f237
    IL_289e:  ceq
    IL_28a0:  stloc.0
    IL_28a1:  ldloc.0
    IL_28a2:  brtrue.s   IL_28bd

    IL_28a4:  nop
    IL_28a5:  ldstr      "f237"
    IL_28aa:  ldarg.0
    IL_28ab:  ldfld      float64 MCCTest.VType7::f237
    IL_28b0:  ldarga.s   expected
    IL_28b2:  ldfld      float64 MCCTest.VType7::f237
    IL_28b7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_28bc:  throw

    IL_28bd:  ldarg.0
    IL_28be:  ldfld      float64 MCCTest.VType7::f238
    IL_28c3:  ldarga.s   expected
    IL_28c5:  ldfld      float64 MCCTest.VType7::f238
    IL_28ca:  ceq
    IL_28cc:  stloc.0
    IL_28cd:  ldloc.0
    IL_28ce:  brtrue.s   IL_28e9

    IL_28d0:  nop
    IL_28d1:  ldstr      "f238"
    IL_28d6:  ldarg.0
    IL_28d7:  ldfld      float64 MCCTest.VType7::f238
    IL_28dc:  ldarga.s   expected
    IL_28de:  ldfld      float64 MCCTest.VType7::f238
    IL_28e3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_28e8:  throw

    IL_28e9:  ldarg.0
    IL_28ea:  ldfld      float64 MCCTest.VType7::f239
    IL_28ef:  ldarga.s   expected
    IL_28f1:  ldfld      float64 MCCTest.VType7::f239
    IL_28f6:  ceq
    IL_28f8:  stloc.0
    IL_28f9:  ldloc.0
    IL_28fa:  brtrue.s   IL_2915

    IL_28fc:  nop
    IL_28fd:  ldstr      "f239"
    IL_2902:  ldarg.0
    IL_2903:  ldfld      float64 MCCTest.VType7::f239
    IL_2908:  ldarga.s   expected
    IL_290a:  ldfld      float64 MCCTest.VType7::f239
    IL_290f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2914:  throw

    IL_2915:  ldarg.0
    IL_2916:  ldfld      float64 MCCTest.VType7::f240
    IL_291b:  ldarga.s   expected
    IL_291d:  ldfld      float64 MCCTest.VType7::f240
    IL_2922:  ceq
    IL_2924:  stloc.0
    IL_2925:  ldloc.0
    IL_2926:  brtrue.s   IL_2941

    IL_2928:  nop
    IL_2929:  ldstr      "f240"
    IL_292e:  ldarg.0
    IL_292f:  ldfld      float64 MCCTest.VType7::f240
    IL_2934:  ldarga.s   expected
    IL_2936:  ldfld      float64 MCCTest.VType7::f240
    IL_293b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2940:  throw

    IL_2941:  ldarg.0
    IL_2942:  ldfld      float64 MCCTest.VType7::f241
    IL_2947:  ldarga.s   expected
    IL_2949:  ldfld      float64 MCCTest.VType7::f241
    IL_294e:  ceq
    IL_2950:  stloc.0
    IL_2951:  ldloc.0
    IL_2952:  brtrue.s   IL_296d

    IL_2954:  nop
    IL_2955:  ldstr      "f241"
    IL_295a:  ldarg.0
    IL_295b:  ldfld      float64 MCCTest.VType7::f241
    IL_2960:  ldarga.s   expected
    IL_2962:  ldfld      float64 MCCTest.VType7::f241
    IL_2967:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_296c:  throw

    IL_296d:  ldarg.0
    IL_296e:  ldfld      float64 MCCTest.VType7::f242
    IL_2973:  ldarga.s   expected
    IL_2975:  ldfld      float64 MCCTest.VType7::f242
    IL_297a:  ceq
    IL_297c:  stloc.0
    IL_297d:  ldloc.0
    IL_297e:  brtrue.s   IL_2999

    IL_2980:  nop
    IL_2981:  ldstr      "f242"
    IL_2986:  ldarg.0
    IL_2987:  ldfld      float64 MCCTest.VType7::f242
    IL_298c:  ldarga.s   expected
    IL_298e:  ldfld      float64 MCCTest.VType7::f242
    IL_2993:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2998:  throw

    IL_2999:  ldarg.0
    IL_299a:  ldfld      float64 MCCTest.VType7::f243
    IL_299f:  ldarga.s   expected
    IL_29a1:  ldfld      float64 MCCTest.VType7::f243
    IL_29a6:  ceq
    IL_29a8:  stloc.0
    IL_29a9:  ldloc.0
    IL_29aa:  brtrue.s   IL_29c5

    IL_29ac:  nop
    IL_29ad:  ldstr      "f243"
    IL_29b2:  ldarg.0
    IL_29b3:  ldfld      float64 MCCTest.VType7::f243
    IL_29b8:  ldarga.s   expected
    IL_29ba:  ldfld      float64 MCCTest.VType7::f243
    IL_29bf:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_29c4:  throw

    IL_29c5:  ldarg.0
    IL_29c6:  ldfld      float64 MCCTest.VType7::f244
    IL_29cb:  ldarga.s   expected
    IL_29cd:  ldfld      float64 MCCTest.VType7::f244
    IL_29d2:  ceq
    IL_29d4:  stloc.0
    IL_29d5:  ldloc.0
    IL_29d6:  brtrue.s   IL_29f1

    IL_29d8:  nop
    IL_29d9:  ldstr      "f244"
    IL_29de:  ldarg.0
    IL_29df:  ldfld      float64 MCCTest.VType7::f244
    IL_29e4:  ldarga.s   expected
    IL_29e6:  ldfld      float64 MCCTest.VType7::f244
    IL_29eb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_29f0:  throw

    IL_29f1:  ldarg.0
    IL_29f2:  ldfld      float64 MCCTest.VType7::f245
    IL_29f7:  ldarga.s   expected
    IL_29f9:  ldfld      float64 MCCTest.VType7::f245
    IL_29fe:  ceq
    IL_2a00:  stloc.0
    IL_2a01:  ldloc.0
    IL_2a02:  brtrue.s   IL_2a1d

    IL_2a04:  nop
    IL_2a05:  ldstr      "f245"
    IL_2a0a:  ldarg.0
    IL_2a0b:  ldfld      float64 MCCTest.VType7::f245
    IL_2a10:  ldarga.s   expected
    IL_2a12:  ldfld      float64 MCCTest.VType7::f245
    IL_2a17:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2a1c:  throw

    IL_2a1d:  ldarg.0
    IL_2a1e:  ldfld      float64 MCCTest.VType7::f246
    IL_2a23:  ldarga.s   expected
    IL_2a25:  ldfld      float64 MCCTest.VType7::f246
    IL_2a2a:  ceq
    IL_2a2c:  stloc.0
    IL_2a2d:  ldloc.0
    IL_2a2e:  brtrue.s   IL_2a49

    IL_2a30:  nop
    IL_2a31:  ldstr      "f246"
    IL_2a36:  ldarg.0
    IL_2a37:  ldfld      float64 MCCTest.VType7::f246
    IL_2a3c:  ldarga.s   expected
    IL_2a3e:  ldfld      float64 MCCTest.VType7::f246
    IL_2a43:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2a48:  throw

    IL_2a49:  ldarg.0
    IL_2a4a:  ldfld      float64 MCCTest.VType7::f247
    IL_2a4f:  ldarga.s   expected
    IL_2a51:  ldfld      float64 MCCTest.VType7::f247
    IL_2a56:  ceq
    IL_2a58:  stloc.0
    IL_2a59:  ldloc.0
    IL_2a5a:  brtrue.s   IL_2a75

    IL_2a5c:  nop
    IL_2a5d:  ldstr      "f247"
    IL_2a62:  ldarg.0
    IL_2a63:  ldfld      float64 MCCTest.VType7::f247
    IL_2a68:  ldarga.s   expected
    IL_2a6a:  ldfld      float64 MCCTest.VType7::f247
    IL_2a6f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2a74:  throw

    IL_2a75:  ldarg.0
    IL_2a76:  ldfld      float64 MCCTest.VType7::f248
    IL_2a7b:  ldarga.s   expected
    IL_2a7d:  ldfld      float64 MCCTest.VType7::f248
    IL_2a82:  ceq
    IL_2a84:  stloc.0
    IL_2a85:  ldloc.0
    IL_2a86:  brtrue.s   IL_2aa1

    IL_2a88:  nop
    IL_2a89:  ldstr      "f248"
    IL_2a8e:  ldarg.0
    IL_2a8f:  ldfld      float64 MCCTest.VType7::f248
    IL_2a94:  ldarga.s   expected
    IL_2a96:  ldfld      float64 MCCTest.VType7::f248
    IL_2a9b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2aa0:  throw

    IL_2aa1:  ldarg.0
    IL_2aa2:  ldfld      float64 MCCTest.VType7::f249
    IL_2aa7:  ldarga.s   expected
    IL_2aa9:  ldfld      float64 MCCTest.VType7::f249
    IL_2aae:  ceq
    IL_2ab0:  stloc.0
    IL_2ab1:  ldloc.0
    IL_2ab2:  brtrue.s   IL_2acd

    IL_2ab4:  nop
    IL_2ab5:  ldstr      "f249"
    IL_2aba:  ldarg.0
    IL_2abb:  ldfld      float64 MCCTest.VType7::f249
    IL_2ac0:  ldarga.s   expected
    IL_2ac2:  ldfld      float64 MCCTest.VType7::f249
    IL_2ac7:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2acc:  throw

    IL_2acd:  ldarg.0
    IL_2ace:  ldfld      float64 MCCTest.VType7::f250
    IL_2ad3:  ldarga.s   expected
    IL_2ad5:  ldfld      float64 MCCTest.VType7::f250
    IL_2ada:  ceq
    IL_2adc:  stloc.0
    IL_2add:  ldloc.0
    IL_2ade:  brtrue.s   IL_2af9

    IL_2ae0:  nop
    IL_2ae1:  ldstr      "f250"
    IL_2ae6:  ldarg.0
    IL_2ae7:  ldfld      float64 MCCTest.VType7::f250
    IL_2aec:  ldarga.s   expected
    IL_2aee:  ldfld      float64 MCCTest.VType7::f250
    IL_2af3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2af8:  throw

    IL_2af9:  ldarg.0
    IL_2afa:  ldfld      float64 MCCTest.VType7::f251
    IL_2aff:  ldarga.s   expected
    IL_2b01:  ldfld      float64 MCCTest.VType7::f251
    IL_2b06:  ceq
    IL_2b08:  stloc.0
    IL_2b09:  ldloc.0
    IL_2b0a:  brtrue.s   IL_2b25

    IL_2b0c:  nop
    IL_2b0d:  ldstr      "f251"
    IL_2b12:  ldarg.0
    IL_2b13:  ldfld      float64 MCCTest.VType7::f251
    IL_2b18:  ldarga.s   expected
    IL_2b1a:  ldfld      float64 MCCTest.VType7::f251
    IL_2b1f:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2b24:  throw

    IL_2b25:  ldarg.0
    IL_2b26:  ldfld      float64 MCCTest.VType7::f252
    IL_2b2b:  ldarga.s   expected
    IL_2b2d:  ldfld      float64 MCCTest.VType7::f252
    IL_2b32:  ceq
    IL_2b34:  stloc.0
    IL_2b35:  ldloc.0
    IL_2b36:  brtrue.s   IL_2b51

    IL_2b38:  nop
    IL_2b39:  ldstr      "f252"
    IL_2b3e:  ldarg.0
    IL_2b3f:  ldfld      float64 MCCTest.VType7::f252
    IL_2b44:  ldarga.s   expected
    IL_2b46:  ldfld      float64 MCCTest.VType7::f252
    IL_2b4b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2b50:  throw

    IL_2b51:  ldarg.0
    IL_2b52:  ldfld      float64 MCCTest.VType7::f253
    IL_2b57:  ldarga.s   expected
    IL_2b59:  ldfld      float64 MCCTest.VType7::f253
    IL_2b5e:  ceq
    IL_2b60:  stloc.0
    IL_2b61:  ldloc.0
    IL_2b62:  brtrue.s   IL_2b7d

    IL_2b64:  nop
    IL_2b65:  ldstr      "f253"
    IL_2b6a:  ldarg.0
    IL_2b6b:  ldfld      float64 MCCTest.VType7::f253
    IL_2b70:  ldarga.s   expected
    IL_2b72:  ldfld      float64 MCCTest.VType7::f253
    IL_2b77:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2b7c:  throw

    IL_2b7d:  ldarg.0
    IL_2b7e:  ldfld      float64 MCCTest.VType7::f254
    IL_2b83:  ldarga.s   expected
    IL_2b85:  ldfld      float64 MCCTest.VType7::f254
    IL_2b8a:  ceq
    IL_2b8c:  stloc.0
    IL_2b8d:  ldloc.0
    IL_2b8e:  brtrue.s   IL_2ba9

    IL_2b90:  nop
    IL_2b91:  ldstr      "f254"
    IL_2b96:  ldarg.0
    IL_2b97:  ldfld      float64 MCCTest.VType7::f254
    IL_2b9c:  ldarga.s   expected
    IL_2b9e:  ldfld      float64 MCCTest.VType7::f254
    IL_2ba3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2ba8:  throw

    IL_2ba9:  ldarg.0
    IL_2baa:  ldfld      float64 MCCTest.VType7::f255
    IL_2baf:  ldarga.s   expected
    IL_2bb1:  ldfld      float64 MCCTest.VType7::f255
    IL_2bb6:  ceq
    IL_2bb8:  stloc.0
    IL_2bb9:  ldloc.0
    IL_2bba:  brtrue.s   IL_2bd5

    IL_2bbc:  nop
    IL_2bbd:  ldstr      "f255"
    IL_2bc2:  ldarg.0
    IL_2bc3:  ldfld      float64 MCCTest.VType7::f255
    IL_2bc8:  ldarga.s   expected
    IL_2bca:  ldfld      float64 MCCTest.VType7::f255
    IL_2bcf:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2bd4:  throw

    IL_2bd5:  ldarg.0
    IL_2bd6:  ldfld      float64 MCCTest.VType7::f256
    IL_2bdb:  ldarga.s   expected
    IL_2bdd:  ldfld      float64 MCCTest.VType7::f256
    IL_2be2:  ceq
    IL_2be4:  stloc.0
    IL_2be5:  ldloc.0
    IL_2be6:  brtrue.s   IL_2c01

    IL_2be8:  nop
    IL_2be9:  ldstr      "f256"
    IL_2bee:  ldarg.0
    IL_2bef:  ldfld      float64 MCCTest.VType7::f256
    IL_2bf4:  ldarga.s   expected
    IL_2bf6:  ldfld      float64 MCCTest.VType7::f256
    IL_2bfb:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2c00:  throw

    IL_2c01:  ldarg.0
    IL_2c02:  ldfld      float64 MCCTest.VType7::f257
    IL_2c07:  ldarga.s   expected
    IL_2c09:  ldfld      float64 MCCTest.VType7::f257
    IL_2c0e:  ceq
    IL_2c10:  stloc.0
    IL_2c11:  ldloc.0
    IL_2c12:  brtrue.s   IL_2c2d

    IL_2c14:  nop
    IL_2c15:  ldstr      "f257"
    IL_2c1a:  ldarg.0
    IL_2c1b:  ldfld      float64 MCCTest.VType7::f257
    IL_2c20:  ldarga.s   expected
    IL_2c22:  ldfld      float64 MCCTest.VType7::f257
    IL_2c27:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_2c2c:  throw

    IL_2c2d:  ret
  } // end of method VType7::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       7478 (0x1d36)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "f1    = "
    IL_0015:  ldarg.0
    IL_0016:  ldfld      float64 MCCTest.VType7::f1
    IL_001b:  box        [mscorlib]System.Double
    IL_0020:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0025:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_002a:  nop
    IL_002b:  ldloc.1
    IL_002c:  ldloc.0
    IL_002d:  ldstr      "f2    = "
    IL_0032:  ldarg.0
    IL_0033:  ldfld      float64 MCCTest.VType7::f2
    IL_0038:  box        [mscorlib]System.Double
    IL_003d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0042:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0047:  nop
    IL_0048:  ldloc.1
    IL_0049:  ldloc.0
    IL_004a:  ldstr      "f3    = "
    IL_004f:  ldarg.0
    IL_0050:  ldfld      float64 MCCTest.VType7::f3
    IL_0055:  box        [mscorlib]System.Double
    IL_005a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_005f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0064:  nop
    IL_0065:  ldloc.1
    IL_0066:  ldloc.0
    IL_0067:  ldstr      "f4    = "
    IL_006c:  ldarg.0
    IL_006d:  ldfld      float64 MCCTest.VType7::f4
    IL_0072:  box        [mscorlib]System.Double
    IL_0077:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_007c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0081:  nop
    IL_0082:  ldloc.1
    IL_0083:  ldloc.0
    IL_0084:  ldstr      "f5    = "
    IL_0089:  ldarg.0
    IL_008a:  ldfld      float64 MCCTest.VType7::f5
    IL_008f:  box        [mscorlib]System.Double
    IL_0094:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0099:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_009e:  nop
    IL_009f:  ldloc.1
    IL_00a0:  ldloc.0
    IL_00a1:  ldstr      "f6    = "
    IL_00a6:  ldarg.0
    IL_00a7:  ldfld      float64 MCCTest.VType7::f6
    IL_00ac:  box        [mscorlib]System.Double
    IL_00b1:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00b6:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00bb:  nop
    IL_00bc:  ldloc.1
    IL_00bd:  ldloc.0
    IL_00be:  ldstr      "f7    = "
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      float64 MCCTest.VType7::f7
    IL_00c9:  box        [mscorlib]System.Double
    IL_00ce:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00d3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00d8:  nop
    IL_00d9:  ldloc.1
    IL_00da:  ldloc.0
    IL_00db:  ldstr      "f8    = "
    IL_00e0:  ldarg.0
    IL_00e1:  ldfld      float64 MCCTest.VType7::f8
    IL_00e6:  box        [mscorlib]System.Double
    IL_00eb:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00f0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00f5:  nop
    IL_00f6:  ldloc.1
    IL_00f7:  ldloc.0
    IL_00f8:  ldstr      "f9    = "
    IL_00fd:  ldarg.0
    IL_00fe:  ldfld      float64 MCCTest.VType7::f9
    IL_0103:  box        [mscorlib]System.Double
    IL_0108:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_010d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0112:  nop
    IL_0113:  ldloc.1
    IL_0114:  ldloc.0
    IL_0115:  ldstr      "f10    = "
    IL_011a:  ldarg.0
    IL_011b:  ldfld      float64 MCCTest.VType7::f10
    IL_0120:  box        [mscorlib]System.Double
    IL_0125:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_012a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_012f:  nop
    IL_0130:  ldloc.1
    IL_0131:  ldloc.0
    IL_0132:  ldstr      "f11    = "
    IL_0137:  ldarg.0
    IL_0138:  ldfld      float64 MCCTest.VType7::f11
    IL_013d:  box        [mscorlib]System.Double
    IL_0142:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0147:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_014c:  nop
    IL_014d:  ldloc.1
    IL_014e:  ldloc.0
    IL_014f:  ldstr      "f12    = "
    IL_0154:  ldarg.0
    IL_0155:  ldfld      float64 MCCTest.VType7::f12
    IL_015a:  box        [mscorlib]System.Double
    IL_015f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0164:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0169:  nop
    IL_016a:  ldloc.1
    IL_016b:  ldloc.0
    IL_016c:  ldstr      "f13    = "
    IL_0171:  ldarg.0
    IL_0172:  ldfld      float64 MCCTest.VType7::f13
    IL_0177:  box        [mscorlib]System.Double
    IL_017c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0181:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0186:  nop
    IL_0187:  ldloc.1
    IL_0188:  ldloc.0
    IL_0189:  ldstr      "f14    = "
    IL_018e:  ldarg.0
    IL_018f:  ldfld      float64 MCCTest.VType7::f14
    IL_0194:  box        [mscorlib]System.Double
    IL_0199:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_019e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01a3:  nop
    IL_01a4:  ldloc.1
    IL_01a5:  ldloc.0
    IL_01a6:  ldstr      "f15    = "
    IL_01ab:  ldarg.0
    IL_01ac:  ldfld      float64 MCCTest.VType7::f15
    IL_01b1:  box        [mscorlib]System.Double
    IL_01b6:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01bb:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01c0:  nop
    IL_01c1:  ldloc.1
    IL_01c2:  ldloc.0
    IL_01c3:  ldstr      "f16    = "
    IL_01c8:  ldarg.0
    IL_01c9:  ldfld      float64 MCCTest.VType7::f16
    IL_01ce:  box        [mscorlib]System.Double
    IL_01d3:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01d8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01dd:  nop
    IL_01de:  ldloc.1
    IL_01df:  ldloc.0
    IL_01e0:  ldstr      "f17    = "
    IL_01e5:  ldarg.0
    IL_01e6:  ldfld      float64 MCCTest.VType7::f17
    IL_01eb:  box        [mscorlib]System.Double
    IL_01f0:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01f5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01fa:  nop
    IL_01fb:  ldloc.1
    IL_01fc:  ldloc.0
    IL_01fd:  ldstr      "f18    = "
    IL_0202:  ldarg.0
    IL_0203:  ldfld      float64 MCCTest.VType7::f18
    IL_0208:  box        [mscorlib]System.Double
    IL_020d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0212:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0217:  nop
    IL_0218:  ldloc.1
    IL_0219:  ldloc.0
    IL_021a:  ldstr      "f19    = "
    IL_021f:  ldarg.0
    IL_0220:  ldfld      float64 MCCTest.VType7::f19
    IL_0225:  box        [mscorlib]System.Double
    IL_022a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_022f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0234:  nop
    IL_0235:  ldloc.1
    IL_0236:  ldloc.0
    IL_0237:  ldstr      "f20    = "
    IL_023c:  ldarg.0
    IL_023d:  ldfld      float64 MCCTest.VType7::f20
    IL_0242:  box        [mscorlib]System.Double
    IL_0247:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_024c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0251:  nop
    IL_0252:  ldloc.1
    IL_0253:  ldloc.0
    IL_0254:  ldstr      "f21    = "
    IL_0259:  ldarg.0
    IL_025a:  ldfld      float64 MCCTest.VType7::f21
    IL_025f:  box        [mscorlib]System.Double
    IL_0264:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0269:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_026e:  nop
    IL_026f:  ldloc.1
    IL_0270:  ldloc.0
    IL_0271:  ldstr      "f22    = "
    IL_0276:  ldarg.0
    IL_0277:  ldfld      float64 MCCTest.VType7::f22
    IL_027c:  box        [mscorlib]System.Double
    IL_0281:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0286:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_028b:  nop
    IL_028c:  ldloc.1
    IL_028d:  ldloc.0
    IL_028e:  ldstr      "f23    = "
    IL_0293:  ldarg.0
    IL_0294:  ldfld      float64 MCCTest.VType7::f23
    IL_0299:  box        [mscorlib]System.Double
    IL_029e:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_02a3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02a8:  nop
    IL_02a9:  ldloc.1
    IL_02aa:  ldloc.0
    IL_02ab:  ldstr      "f24    = "
    IL_02b0:  ldarg.0
    IL_02b1:  ldfld      float64 MCCTest.VType7::f24
    IL_02b6:  box        [mscorlib]System.Double
    IL_02bb:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_02c0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02c5:  nop
    IL_02c6:  ldloc.1
    IL_02c7:  ldloc.0
    IL_02c8:  ldstr      "f25    = "
    IL_02cd:  ldarg.0
    IL_02ce:  ldfld      float64 MCCTest.VType7::f25
    IL_02d3:  box        [mscorlib]System.Double
    IL_02d8:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_02dd:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02e2:  nop
    IL_02e3:  ldloc.1
    IL_02e4:  ldloc.0
    IL_02e5:  ldstr      "f26    = "
    IL_02ea:  ldarg.0
    IL_02eb:  ldfld      float64 MCCTest.VType7::f26
    IL_02f0:  box        [mscorlib]System.Double
    IL_02f5:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_02fa:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02ff:  nop
    IL_0300:  ldloc.1
    IL_0301:  ldloc.0
    IL_0302:  ldstr      "f27    = "
    IL_0307:  ldarg.0
    IL_0308:  ldfld      float64 MCCTest.VType7::f27
    IL_030d:  box        [mscorlib]System.Double
    IL_0312:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0317:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_031c:  nop
    IL_031d:  ldloc.1
    IL_031e:  ldloc.0
    IL_031f:  ldstr      "f28    = "
    IL_0324:  ldarg.0
    IL_0325:  ldfld      float64 MCCTest.VType7::f28
    IL_032a:  box        [mscorlib]System.Double
    IL_032f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0334:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0339:  nop
    IL_033a:  ldloc.1
    IL_033b:  ldloc.0
    IL_033c:  ldstr      "f29    = "
    IL_0341:  ldarg.0
    IL_0342:  ldfld      float64 MCCTest.VType7::f29
    IL_0347:  box        [mscorlib]System.Double
    IL_034c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0351:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0356:  nop
    IL_0357:  ldloc.1
    IL_0358:  ldloc.0
    IL_0359:  ldstr      "f30    = "
    IL_035e:  ldarg.0
    IL_035f:  ldfld      float64 MCCTest.VType7::f30
    IL_0364:  box        [mscorlib]System.Double
    IL_0369:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_036e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0373:  nop
    IL_0374:  ldloc.1
    IL_0375:  ldloc.0
    IL_0376:  ldstr      "f31    = "
    IL_037b:  ldarg.0
    IL_037c:  ldfld      float64 MCCTest.VType7::f31
    IL_0381:  box        [mscorlib]System.Double
    IL_0386:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_038b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0390:  nop
    IL_0391:  ldloc.1
    IL_0392:  ldloc.0
    IL_0393:  ldstr      "f32    = "
    IL_0398:  ldarg.0
    IL_0399:  ldfld      float64 MCCTest.VType7::f32
    IL_039e:  box        [mscorlib]System.Double
    IL_03a3:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_03a8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_03ad:  nop
    IL_03ae:  ldloc.1
    IL_03af:  ldloc.0
    IL_03b0:  ldstr      "f33    = "
    IL_03b5:  ldarg.0
    IL_03b6:  ldfld      float64 MCCTest.VType7::f33
    IL_03bb:  box        [mscorlib]System.Double
    IL_03c0:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_03c5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_03ca:  nop
    IL_03cb:  ldloc.1
    IL_03cc:  ldloc.0
    IL_03cd:  ldstr      "f34    = "
    IL_03d2:  ldarg.0
    IL_03d3:  ldfld      float64 MCCTest.VType7::f34
    IL_03d8:  box        [mscorlib]System.Double
    IL_03dd:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_03e2:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_03e7:  nop
    IL_03e8:  ldloc.1
    IL_03e9:  ldloc.0
    IL_03ea:  ldstr      "f35    = "
    IL_03ef:  ldarg.0
    IL_03f0:  ldfld      float64 MCCTest.VType7::f35
    IL_03f5:  box        [mscorlib]System.Double
    IL_03fa:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_03ff:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0404:  nop
    IL_0405:  ldloc.1
    IL_0406:  ldloc.0
    IL_0407:  ldstr      "f36    = "
    IL_040c:  ldarg.0
    IL_040d:  ldfld      float64 MCCTest.VType7::f36
    IL_0412:  box        [mscorlib]System.Double
    IL_0417:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_041c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0421:  nop
    IL_0422:  ldloc.1
    IL_0423:  ldloc.0
    IL_0424:  ldstr      "f37    = "
    IL_0429:  ldarg.0
    IL_042a:  ldfld      float64 MCCTest.VType7::f37
    IL_042f:  box        [mscorlib]System.Double
    IL_0434:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0439:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_043e:  nop
    IL_043f:  ldloc.1
    IL_0440:  ldloc.0
    IL_0441:  ldstr      "f38    = "
    IL_0446:  ldarg.0
    IL_0447:  ldfld      float64 MCCTest.VType7::f38
    IL_044c:  box        [mscorlib]System.Double
    IL_0451:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0456:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_045b:  nop
    IL_045c:  ldloc.1
    IL_045d:  ldloc.0
    IL_045e:  ldstr      "f39    = "
    IL_0463:  ldarg.0
    IL_0464:  ldfld      float64 MCCTest.VType7::f39
    IL_0469:  box        [mscorlib]System.Double
    IL_046e:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0473:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0478:  nop
    IL_0479:  ldloc.1
    IL_047a:  ldloc.0
    IL_047b:  ldstr      "f40    = "
    IL_0480:  ldarg.0
    IL_0481:  ldfld      float64 MCCTest.VType7::f40
    IL_0486:  box        [mscorlib]System.Double
    IL_048b:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0490:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0495:  nop
    IL_0496:  ldloc.1
    IL_0497:  ldloc.0
    IL_0498:  ldstr      "f41    = "
    IL_049d:  ldarg.0
    IL_049e:  ldfld      float64 MCCTest.VType7::f41
    IL_04a3:  box        [mscorlib]System.Double
    IL_04a8:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_04ad:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_04b2:  nop
    IL_04b3:  ldloc.1
    IL_04b4:  ldloc.0
    IL_04b5:  ldstr      "f42    = "
    IL_04ba:  ldarg.0
    IL_04bb:  ldfld      float64 MCCTest.VType7::f42
    IL_04c0:  box        [mscorlib]System.Double
    IL_04c5:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_04ca:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_04cf:  nop
    IL_04d0:  ldloc.1
    IL_04d1:  ldloc.0
    IL_04d2:  ldstr      "f43    = "
    IL_04d7:  ldarg.0
    IL_04d8:  ldfld      float64 MCCTest.VType7::f43
    IL_04dd:  box        [mscorlib]System.Double
    IL_04e2:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_04e7:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_04ec:  nop
    IL_04ed:  ldloc.1
    IL_04ee:  ldloc.0
    IL_04ef:  ldstr      "f44    = "
    IL_04f4:  ldarg.0
    IL_04f5:  ldfld      float64 MCCTest.VType7::f44
    IL_04fa:  box        [mscorlib]System.Double
    IL_04ff:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0504:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0509:  nop
    IL_050a:  ldloc.1
    IL_050b:  ldloc.0
    IL_050c:  ldstr      "f45    = "
    IL_0511:  ldarg.0
    IL_0512:  ldfld      float64 MCCTest.VType7::f45
    IL_0517:  box        [mscorlib]System.Double
    IL_051c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0521:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0526:  nop
    IL_0527:  ldloc.1
    IL_0528:  ldloc.0
    IL_0529:  ldstr      "f46    = "
    IL_052e:  ldarg.0
    IL_052f:  ldfld      float64 MCCTest.VType7::f46
    IL_0534:  box        [mscorlib]System.Double
    IL_0539:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_053e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0543:  nop
    IL_0544:  ldloc.1
    IL_0545:  ldloc.0
    IL_0546:  ldstr      "f47    = "
    IL_054b:  ldarg.0
    IL_054c:  ldfld      float64 MCCTest.VType7::f47
    IL_0551:  box        [mscorlib]System.Double
    IL_0556:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_055b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0560:  nop
    IL_0561:  ldloc.1
    IL_0562:  ldloc.0
    IL_0563:  ldstr      "f48    = "
    IL_0568:  ldarg.0
    IL_0569:  ldfld      float64 MCCTest.VType7::f48
    IL_056e:  box        [mscorlib]System.Double
    IL_0573:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0578:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_057d:  nop
    IL_057e:  ldloc.1
    IL_057f:  ldloc.0
    IL_0580:  ldstr      "f49    = "
    IL_0585:  ldarg.0
    IL_0586:  ldfld      float64 MCCTest.VType7::f49
    IL_058b:  box        [mscorlib]System.Double
    IL_0590:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0595:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_059a:  nop
    IL_059b:  ldloc.1
    IL_059c:  ldloc.0
    IL_059d:  ldstr      "f50    = "
    IL_05a2:  ldarg.0
    IL_05a3:  ldfld      float64 MCCTest.VType7::f50
    IL_05a8:  box        [mscorlib]System.Double
    IL_05ad:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_05b2:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_05b7:  nop
    IL_05b8:  ldloc.1
    IL_05b9:  ldloc.0
    IL_05ba:  ldstr      "f51    = "
    IL_05bf:  ldarg.0
    IL_05c0:  ldfld      float64 MCCTest.VType7::f51
    IL_05c5:  box        [mscorlib]System.Double
    IL_05ca:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_05cf:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_05d4:  nop
    IL_05d5:  ldloc.1
    IL_05d6:  ldloc.0
    IL_05d7:  ldstr      "f52    = "
    IL_05dc:  ldarg.0
    IL_05dd:  ldfld      float64 MCCTest.VType7::f52
    IL_05e2:  box        [mscorlib]System.Double
    IL_05e7:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_05ec:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_05f1:  nop
    IL_05f2:  ldloc.1
    IL_05f3:  ldloc.0
    IL_05f4:  ldstr      "f53    = "
    IL_05f9:  ldarg.0
    IL_05fa:  ldfld      float64 MCCTest.VType7::f53
    IL_05ff:  box        [mscorlib]System.Double
    IL_0604:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0609:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_060e:  nop
    IL_060f:  ldloc.1
    IL_0610:  ldloc.0
    IL_0611:  ldstr      "f54    = "
    IL_0616:  ldarg.0
    IL_0617:  ldfld      float64 MCCTest.VType7::f54
    IL_061c:  box        [mscorlib]System.Double
    IL_0621:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0626:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_062b:  nop
    IL_062c:  ldloc.1
    IL_062d:  ldloc.0
    IL_062e:  ldstr      "f55    = "
    IL_0633:  ldarg.0
    IL_0634:  ldfld      float64 MCCTest.VType7::f55
    IL_0639:  box        [mscorlib]System.Double
    IL_063e:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0643:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0648:  nop
    IL_0649:  ldloc.1
    IL_064a:  ldloc.0
    IL_064b:  ldstr      "f56    = "
    IL_0650:  ldarg.0
    IL_0651:  ldfld      float64 MCCTest.VType7::f56
    IL_0656:  box        [mscorlib]System.Double
    IL_065b:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0660:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0665:  nop
    IL_0666:  ldloc.1
    IL_0667:  ldloc.0
    IL_0668:  ldstr      "f57    = "
    IL_066d:  ldarg.0
    IL_066e:  ldfld      float64 MCCTest.VType7::f57
    IL_0673:  box        [mscorlib]System.Double
    IL_0678:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_067d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0682:  nop
    IL_0683:  ldloc.1
    IL_0684:  ldloc.0
    IL_0685:  ldstr      "f58    = "
    IL_068a:  ldarg.0
    IL_068b:  ldfld      float64 MCCTest.VType7::f58
    IL_0690:  box        [mscorlib]System.Double
    IL_0695:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_069a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_069f:  nop
    IL_06a0:  ldloc.1
    IL_06a1:  ldloc.0
    IL_06a2:  ldstr      "f59    = "
    IL_06a7:  ldarg.0
    IL_06a8:  ldfld      float64 MCCTest.VType7::f59
    IL_06ad:  box        [mscorlib]System.Double
    IL_06b2:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_06b7:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_06bc:  nop
    IL_06bd:  ldloc.1
    IL_06be:  ldloc.0
    IL_06bf:  ldstr      "f60    = "
    IL_06c4:  ldarg.0
    IL_06c5:  ldfld      float64 MCCTest.VType7::f60
    IL_06ca:  box        [mscorlib]System.Double
    IL_06cf:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_06d4:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_06d9:  nop
    IL_06da:  ldloc.1
    IL_06db:  ldloc.0
    IL_06dc:  ldstr      "f61    = "
    IL_06e1:  ldarg.0
    IL_06e2:  ldfld      float64 MCCTest.VType7::f61
    IL_06e7:  box        [mscorlib]System.Double
    IL_06ec:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_06f1:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_06f6:  nop
    IL_06f7:  ldloc.1
    IL_06f8:  ldloc.0
    IL_06f9:  ldstr      "f62    = "
    IL_06fe:  ldarg.0
    IL_06ff:  ldfld      float64 MCCTest.VType7::f62
    IL_0704:  box        [mscorlib]System.Double
    IL_0709:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_070e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0713:  nop
    IL_0714:  ldloc.1
    IL_0715:  ldloc.0
    IL_0716:  ldstr      "f63    = "
    IL_071b:  ldarg.0
    IL_071c:  ldfld      float64 MCCTest.VType7::f63
    IL_0721:  box        [mscorlib]System.Double
    IL_0726:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_072b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0730:  nop
    IL_0731:  ldloc.1
    IL_0732:  ldloc.0
    IL_0733:  ldstr      "f64    = "
    IL_0738:  ldarg.0
    IL_0739:  ldfld      float64 MCCTest.VType7::f64
    IL_073e:  box        [mscorlib]System.Double
    IL_0743:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0748:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_074d:  nop
    IL_074e:  ldloc.1
    IL_074f:  ldloc.0
    IL_0750:  ldstr      "f65    = "
    IL_0755:  ldarg.0
    IL_0756:  ldfld      float64 MCCTest.VType7::f65
    IL_075b:  box        [mscorlib]System.Double
    IL_0760:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0765:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_076a:  nop
    IL_076b:  ldloc.1
    IL_076c:  ldloc.0
    IL_076d:  ldstr      "f66    = "
    IL_0772:  ldarg.0
    IL_0773:  ldfld      float64 MCCTest.VType7::f66
    IL_0778:  box        [mscorlib]System.Double
    IL_077d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0782:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0787:  nop
    IL_0788:  ldloc.1
    IL_0789:  ldloc.0
    IL_078a:  ldstr      "f67    = "
    IL_078f:  ldarg.0
    IL_0790:  ldfld      float64 MCCTest.VType7::f67
    IL_0795:  box        [mscorlib]System.Double
    IL_079a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_079f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_07a4:  nop
    IL_07a5:  ldloc.1
    IL_07a6:  ldloc.0
    IL_07a7:  ldstr      "f68    = "
    IL_07ac:  ldarg.0
    IL_07ad:  ldfld      float64 MCCTest.VType7::f68
    IL_07b2:  box        [mscorlib]System.Double
    IL_07b7:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_07bc:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_07c1:  nop
    IL_07c2:  ldloc.1
    IL_07c3:  ldloc.0
    IL_07c4:  ldstr      "f69    = "
    IL_07c9:  ldarg.0
    IL_07ca:  ldfld      float64 MCCTest.VType7::f69
    IL_07cf:  box        [mscorlib]System.Double
    IL_07d4:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_07d9:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_07de:  nop
    IL_07df:  ldloc.1
    IL_07e0:  ldloc.0
    IL_07e1:  ldstr      "f70    = "
    IL_07e6:  ldarg.0
    IL_07e7:  ldfld      float64 MCCTest.VType7::f70
    IL_07ec:  box        [mscorlib]System.Double
    IL_07f1:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_07f6:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_07fb:  nop
    IL_07fc:  ldloc.1
    IL_07fd:  ldloc.0
    IL_07fe:  ldstr      "f71    = "
    IL_0803:  ldarg.0
    IL_0804:  ldfld      float64 MCCTest.VType7::f71
    IL_0809:  box        [mscorlib]System.Double
    IL_080e:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0813:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0818:  nop
    IL_0819:  ldloc.1
    IL_081a:  ldloc.0
    IL_081b:  ldstr      "f72    = "
    IL_0820:  ldarg.0
    IL_0821:  ldfld      float64 MCCTest.VType7::f72
    IL_0826:  box        [mscorlib]System.Double
    IL_082b:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0830:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0835:  nop
    IL_0836:  ldloc.1
    IL_0837:  ldloc.0
    IL_0838:  ldstr      "f73    = "
    IL_083d:  ldarg.0
    IL_083e:  ldfld      float64 MCCTest.VType7::f73
    IL_0843:  box        [mscorlib]System.Double
    IL_0848:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_084d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0852:  nop
    IL_0853:  ldloc.1
    IL_0854:  ldloc.0
    IL_0855:  ldstr      "f74    = "
    IL_085a:  ldarg.0
    IL_085b:  ldfld      float64 MCCTest.VType7::f74
    IL_0860:  box        [mscorlib]System.Double
    IL_0865:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_086a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_086f:  nop
    IL_0870:  ldloc.1
    IL_0871:  ldloc.0
    IL_0872:  ldstr      "f75    = "
    IL_0877:  ldarg.0
    IL_0878:  ldfld      float64 MCCTest.VType7::f75
    IL_087d:  box        [mscorlib]System.Double
    IL_0882:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0887:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_088c:  nop
    IL_088d:  ldloc.1
    IL_088e:  ldloc.0
    IL_088f:  ldstr      "f76    = "
    IL_0894:  ldarg.0
    IL_0895:  ldfld      float64 MCCTest.VType7::f76
    IL_089a:  box        [mscorlib]System.Double
    IL_089f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_08a4:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_08a9:  nop
    IL_08aa:  ldloc.1
    IL_08ab:  ldloc.0
    IL_08ac:  ldstr      "f77    = "
    IL_08b1:  ldarg.0
    IL_08b2:  ldfld      float64 MCCTest.VType7::f77
    IL_08b7:  box        [mscorlib]System.Double
    IL_08bc:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_08c1:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_08c6:  nop
    IL_08c7:  ldloc.1
    IL_08c8:  ldloc.0
    IL_08c9:  ldstr      "f78    = "
    IL_08ce:  ldarg.0
    IL_08cf:  ldfld      float64 MCCTest.VType7::f78
    IL_08d4:  box        [mscorlib]System.Double
    IL_08d9:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_08de:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_08e3:  nop
    IL_08e4:  ldloc.1
    IL_08e5:  ldloc.0
    IL_08e6:  ldstr      "f79    = "
    IL_08eb:  ldarg.0
    IL_08ec:  ldfld      float64 MCCTest.VType7::f79
    IL_08f1:  box        [mscorlib]System.Double
    IL_08f6:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_08fb:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0900:  nop
    IL_0901:  ldloc.1
    IL_0902:  ldloc.0
    IL_0903:  ldstr      "f80    = "
    IL_0908:  ldarg.0
    IL_0909:  ldfld      float64 MCCTest.VType7::f80
    IL_090e:  box        [mscorlib]System.Double
    IL_0913:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0918:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_091d:  nop
    IL_091e:  ldloc.1
    IL_091f:  ldloc.0
    IL_0920:  ldstr      "f81    = "
    IL_0925:  ldarg.0
    IL_0926:  ldfld      float64 MCCTest.VType7::f81
    IL_092b:  box        [mscorlib]System.Double
    IL_0930:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0935:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_093a:  nop
    IL_093b:  ldloc.1
    IL_093c:  ldloc.0
    IL_093d:  ldstr      "f82    = "
    IL_0942:  ldarg.0
    IL_0943:  ldfld      float64 MCCTest.VType7::f82
    IL_0948:  box        [mscorlib]System.Double
    IL_094d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0952:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0957:  nop
    IL_0958:  ldloc.1
    IL_0959:  ldloc.0
    IL_095a:  ldstr      "f83    = "
    IL_095f:  ldarg.0
    IL_0960:  ldfld      float64 MCCTest.VType7::f83
    IL_0965:  box        [mscorlib]System.Double
    IL_096a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_096f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0974:  nop
    IL_0975:  ldloc.1
    IL_0976:  ldloc.0
    IL_0977:  ldstr      "f84    = "
    IL_097c:  ldarg.0
    IL_097d:  ldfld      float64 MCCTest.VType7::f84
    IL_0982:  box        [mscorlib]System.Double
    IL_0987:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_098c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0991:  nop
    IL_0992:  ldloc.1
    IL_0993:  ldloc.0
    IL_0994:  ldstr      "f85    = "
    IL_0999:  ldarg.0
    IL_099a:  ldfld      float64 MCCTest.VType7::f85
    IL_099f:  box        [mscorlib]System.Double
    IL_09a4:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_09a9:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_09ae:  nop
    IL_09af:  ldloc.1
    IL_09b0:  ldloc.0
    IL_09b1:  ldstr      "f86    = "
    IL_09b6:  ldarg.0
    IL_09b7:  ldfld      float64 MCCTest.VType7::f86
    IL_09bc:  box        [mscorlib]System.Double
    IL_09c1:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_09c6:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_09cb:  nop
    IL_09cc:  ldloc.1
    IL_09cd:  ldloc.0
    IL_09ce:  ldstr      "f87    = "
    IL_09d3:  ldarg.0
    IL_09d4:  ldfld      float64 MCCTest.VType7::f87
    IL_09d9:  box        [mscorlib]System.Double
    IL_09de:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_09e3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_09e8:  nop
    IL_09e9:  ldloc.1
    IL_09ea:  ldloc.0
    IL_09eb:  ldstr      "f88    = "
    IL_09f0:  ldarg.0
    IL_09f1:  ldfld      float64 MCCTest.VType7::f88
    IL_09f6:  box        [mscorlib]System.Double
    IL_09fb:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0a00:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0a05:  nop
    IL_0a06:  ldloc.1
    IL_0a07:  ldloc.0
    IL_0a08:  ldstr      "f89    = "
    IL_0a0d:  ldarg.0
    IL_0a0e:  ldfld      float64 MCCTest.VType7::f89
    IL_0a13:  box        [mscorlib]System.Double
    IL_0a18:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0a1d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0a22:  nop
    IL_0a23:  ldloc.1
    IL_0a24:  ldloc.0
    IL_0a25:  ldstr      "f90    = "
    IL_0a2a:  ldarg.0
    IL_0a2b:  ldfld      float64 MCCTest.VType7::f90
    IL_0a30:  box        [mscorlib]System.Double
    IL_0a35:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0a3a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0a3f:  nop
    IL_0a40:  ldloc.1
    IL_0a41:  ldloc.0
    IL_0a42:  ldstr      "f91    = "
    IL_0a47:  ldarg.0
    IL_0a48:  ldfld      float64 MCCTest.VType7::f91
    IL_0a4d:  box        [mscorlib]System.Double
    IL_0a52:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0a57:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0a5c:  nop
    IL_0a5d:  ldloc.1
    IL_0a5e:  ldloc.0
    IL_0a5f:  ldstr      "f92    = "
    IL_0a64:  ldarg.0
    IL_0a65:  ldfld      float64 MCCTest.VType7::f92
    IL_0a6a:  box        [mscorlib]System.Double
    IL_0a6f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0a74:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0a79:  nop
    IL_0a7a:  ldloc.1
    IL_0a7b:  ldloc.0
    IL_0a7c:  ldstr      "f93    = "
    IL_0a81:  ldarg.0
    IL_0a82:  ldfld      float64 MCCTest.VType7::f93
    IL_0a87:  box        [mscorlib]System.Double
    IL_0a8c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0a91:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0a96:  nop
    IL_0a97:  ldloc.1
    IL_0a98:  ldloc.0
    IL_0a99:  ldstr      "f94    = "
    IL_0a9e:  ldarg.0
    IL_0a9f:  ldfld      float64 MCCTest.VType7::f94
    IL_0aa4:  box        [mscorlib]System.Double
    IL_0aa9:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0aae:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0ab3:  nop
    IL_0ab4:  ldloc.1
    IL_0ab5:  ldloc.0
    IL_0ab6:  ldstr      "f95    = "
    IL_0abb:  ldarg.0
    IL_0abc:  ldfld      float64 MCCTest.VType7::f95
    IL_0ac1:  box        [mscorlib]System.Double
    IL_0ac6:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0acb:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0ad0:  nop
    IL_0ad1:  ldloc.1
    IL_0ad2:  ldloc.0
    IL_0ad3:  ldstr      "f96    = "
    IL_0ad8:  ldarg.0
    IL_0ad9:  ldfld      float64 MCCTest.VType7::f96
    IL_0ade:  box        [mscorlib]System.Double
    IL_0ae3:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0ae8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0aed:  nop
    IL_0aee:  ldloc.1
    IL_0aef:  ldloc.0
    IL_0af0:  ldstr      "f97    = "
    IL_0af5:  ldarg.0
    IL_0af6:  ldfld      float64 MCCTest.VType7::f97
    IL_0afb:  box        [mscorlib]System.Double
    IL_0b00:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0b05:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0b0a:  nop
    IL_0b0b:  ldloc.1
    IL_0b0c:  ldloc.0
    IL_0b0d:  ldstr      "f98    = "
    IL_0b12:  ldarg.0
    IL_0b13:  ldfld      float64 MCCTest.VType7::f98
    IL_0b18:  box        [mscorlib]System.Double
    IL_0b1d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0b22:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0b27:  nop
    IL_0b28:  ldloc.1
    IL_0b29:  ldloc.0
    IL_0b2a:  ldstr      "f99    = "
    IL_0b2f:  ldarg.0
    IL_0b30:  ldfld      float64 MCCTest.VType7::f99
    IL_0b35:  box        [mscorlib]System.Double
    IL_0b3a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0b3f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0b44:  nop
    IL_0b45:  ldloc.1
    IL_0b46:  ldloc.0
    IL_0b47:  ldstr      "f100    = "
    IL_0b4c:  ldarg.0
    IL_0b4d:  ldfld      float64 MCCTest.VType7::f100
    IL_0b52:  box        [mscorlib]System.Double
    IL_0b57:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0b5c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0b61:  nop
    IL_0b62:  ldloc.1
    IL_0b63:  ldloc.0
    IL_0b64:  ldstr      "f101    = "
    IL_0b69:  ldarg.0
    IL_0b6a:  ldfld      float64 MCCTest.VType7::f101
    IL_0b6f:  box        [mscorlib]System.Double
    IL_0b74:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0b79:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0b7e:  nop
    IL_0b7f:  ldloc.1
    IL_0b80:  ldloc.0
    IL_0b81:  ldstr      "f102    = "
    IL_0b86:  ldarg.0
    IL_0b87:  ldfld      float64 MCCTest.VType7::f102
    IL_0b8c:  box        [mscorlib]System.Double
    IL_0b91:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0b96:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0b9b:  nop
    IL_0b9c:  ldloc.1
    IL_0b9d:  ldloc.0
    IL_0b9e:  ldstr      "f103    = "
    IL_0ba3:  ldarg.0
    IL_0ba4:  ldfld      float64 MCCTest.VType7::f103
    IL_0ba9:  box        [mscorlib]System.Double
    IL_0bae:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0bb3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0bb8:  nop
    IL_0bb9:  ldloc.1
    IL_0bba:  ldloc.0
    IL_0bbb:  ldstr      "f104    = "
    IL_0bc0:  ldarg.0
    IL_0bc1:  ldfld      float64 MCCTest.VType7::f104
    IL_0bc6:  box        [mscorlib]System.Double
    IL_0bcb:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0bd0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0bd5:  nop
    IL_0bd6:  ldloc.1
    IL_0bd7:  ldloc.0
    IL_0bd8:  ldstr      "f105    = "
    IL_0bdd:  ldarg.0
    IL_0bde:  ldfld      float64 MCCTest.VType7::f105
    IL_0be3:  box        [mscorlib]System.Double
    IL_0be8:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0bed:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0bf2:  nop
    IL_0bf3:  ldloc.1
    IL_0bf4:  ldloc.0
    IL_0bf5:  ldstr      "f106    = "
    IL_0bfa:  ldarg.0
    IL_0bfb:  ldfld      float64 MCCTest.VType7::f106
    IL_0c00:  box        [mscorlib]System.Double
    IL_0c05:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0c0a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0c0f:  nop
    IL_0c10:  ldloc.1
    IL_0c11:  ldloc.0
    IL_0c12:  ldstr      "f107    = "
    IL_0c17:  ldarg.0
    IL_0c18:  ldfld      float64 MCCTest.VType7::f107
    IL_0c1d:  box        [mscorlib]System.Double
    IL_0c22:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0c27:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0c2c:  nop
    IL_0c2d:  ldloc.1
    IL_0c2e:  ldloc.0
    IL_0c2f:  ldstr      "f108    = "
    IL_0c34:  ldarg.0
    IL_0c35:  ldfld      float64 MCCTest.VType7::f108
    IL_0c3a:  box        [mscorlib]System.Double
    IL_0c3f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0c44:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0c49:  nop
    IL_0c4a:  ldloc.1
    IL_0c4b:  ldloc.0
    IL_0c4c:  ldstr      "f109    = "
    IL_0c51:  ldarg.0
    IL_0c52:  ldfld      float64 MCCTest.VType7::f109
    IL_0c57:  box        [mscorlib]System.Double
    IL_0c5c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0c61:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0c66:  nop
    IL_0c67:  ldloc.1
    IL_0c68:  ldloc.0
    IL_0c69:  ldstr      "f110    = "
    IL_0c6e:  ldarg.0
    IL_0c6f:  ldfld      float64 MCCTest.VType7::f110
    IL_0c74:  box        [mscorlib]System.Double
    IL_0c79:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0c7e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0c83:  nop
    IL_0c84:  ldloc.1
    IL_0c85:  ldloc.0
    IL_0c86:  ldstr      "f111    = "
    IL_0c8b:  ldarg.0
    IL_0c8c:  ldfld      float64 MCCTest.VType7::f111
    IL_0c91:  box        [mscorlib]System.Double
    IL_0c96:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0c9b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0ca0:  nop
    IL_0ca1:  ldloc.1
    IL_0ca2:  ldloc.0
    IL_0ca3:  ldstr      "f112    = "
    IL_0ca8:  ldarg.0
    IL_0ca9:  ldfld      float64 MCCTest.VType7::f112
    IL_0cae:  box        [mscorlib]System.Double
    IL_0cb3:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0cb8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0cbd:  nop
    IL_0cbe:  ldloc.1
    IL_0cbf:  ldloc.0
    IL_0cc0:  ldstr      "f113    = "
    IL_0cc5:  ldarg.0
    IL_0cc6:  ldfld      float64 MCCTest.VType7::f113
    IL_0ccb:  box        [mscorlib]System.Double
    IL_0cd0:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0cd5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0cda:  nop
    IL_0cdb:  ldloc.1
    IL_0cdc:  ldloc.0
    IL_0cdd:  ldstr      "f114    = "
    IL_0ce2:  ldarg.0
    IL_0ce3:  ldfld      float64 MCCTest.VType7::f114
    IL_0ce8:  box        [mscorlib]System.Double
    IL_0ced:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0cf2:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0cf7:  nop
    IL_0cf8:  ldloc.1
    IL_0cf9:  ldloc.0
    IL_0cfa:  ldstr      "f115    = "
    IL_0cff:  ldarg.0
    IL_0d00:  ldfld      float64 MCCTest.VType7::f115
    IL_0d05:  box        [mscorlib]System.Double
    IL_0d0a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0d0f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0d14:  nop
    IL_0d15:  ldloc.1
    IL_0d16:  ldloc.0
    IL_0d17:  ldstr      "f116    = "
    IL_0d1c:  ldarg.0
    IL_0d1d:  ldfld      float64 MCCTest.VType7::f116
    IL_0d22:  box        [mscorlib]System.Double
    IL_0d27:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0d2c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0d31:  nop
    IL_0d32:  ldloc.1
    IL_0d33:  ldloc.0
    IL_0d34:  ldstr      "f117    = "
    IL_0d39:  ldarg.0
    IL_0d3a:  ldfld      float64 MCCTest.VType7::f117
    IL_0d3f:  box        [mscorlib]System.Double
    IL_0d44:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0d49:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0d4e:  nop
    IL_0d4f:  ldloc.1
    IL_0d50:  ldloc.0
    IL_0d51:  ldstr      "f118    = "
    IL_0d56:  ldarg.0
    IL_0d57:  ldfld      float64 MCCTest.VType7::f118
    IL_0d5c:  box        [mscorlib]System.Double
    IL_0d61:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0d66:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0d6b:  nop
    IL_0d6c:  ldloc.1
    IL_0d6d:  ldloc.0
    IL_0d6e:  ldstr      "f119    = "
    IL_0d73:  ldarg.0
    IL_0d74:  ldfld      float64 MCCTest.VType7::f119
    IL_0d79:  box        [mscorlib]System.Double
    IL_0d7e:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0d83:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0d88:  nop
    IL_0d89:  ldloc.1
    IL_0d8a:  ldloc.0
    IL_0d8b:  ldstr      "f120    = "
    IL_0d90:  ldarg.0
    IL_0d91:  ldfld      float64 MCCTest.VType7::f120
    IL_0d96:  box        [mscorlib]System.Double
    IL_0d9b:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0da0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0da5:  nop
    IL_0da6:  ldloc.1
    IL_0da7:  ldloc.0
    IL_0da8:  ldstr      "f121    = "
    IL_0dad:  ldarg.0
    IL_0dae:  ldfld      float64 MCCTest.VType7::f121
    IL_0db3:  box        [mscorlib]System.Double
    IL_0db8:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0dbd:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0dc2:  nop
    IL_0dc3:  ldloc.1
    IL_0dc4:  ldloc.0
    IL_0dc5:  ldstr      "f122    = "
    IL_0dca:  ldarg.0
    IL_0dcb:  ldfld      float64 MCCTest.VType7::f122
    IL_0dd0:  box        [mscorlib]System.Double
    IL_0dd5:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0dda:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0ddf:  nop
    IL_0de0:  ldloc.1
    IL_0de1:  ldloc.0
    IL_0de2:  ldstr      "f123    = "
    IL_0de7:  ldarg.0
    IL_0de8:  ldfld      float64 MCCTest.VType7::f123
    IL_0ded:  box        [mscorlib]System.Double
    IL_0df2:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0df7:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0dfc:  nop
    IL_0dfd:  ldloc.1
    IL_0dfe:  ldloc.0
    IL_0dff:  ldstr      "f124    = "
    IL_0e04:  ldarg.0
    IL_0e05:  ldfld      float64 MCCTest.VType7::f124
    IL_0e0a:  box        [mscorlib]System.Double
    IL_0e0f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0e14:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0e19:  nop
    IL_0e1a:  ldloc.1
    IL_0e1b:  ldloc.0
    IL_0e1c:  ldstr      "f125    = "
    IL_0e21:  ldarg.0
    IL_0e22:  ldfld      float64 MCCTest.VType7::f125
    IL_0e27:  box        [mscorlib]System.Double
    IL_0e2c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0e31:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0e36:  nop
    IL_0e37:  ldloc.1
    IL_0e38:  ldloc.0
    IL_0e39:  ldstr      "f126    = "
    IL_0e3e:  ldarg.0
    IL_0e3f:  ldfld      float64 MCCTest.VType7::f126
    IL_0e44:  box        [mscorlib]System.Double
    IL_0e49:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0e4e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0e53:  nop
    IL_0e54:  ldloc.1
    IL_0e55:  ldloc.0
    IL_0e56:  ldstr      "f127    = "
    IL_0e5b:  ldarg.0
    IL_0e5c:  ldfld      float64 MCCTest.VType7::f127
    IL_0e61:  box        [mscorlib]System.Double
    IL_0e66:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0e6b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0e70:  nop
    IL_0e71:  ldloc.1
    IL_0e72:  ldloc.0
    IL_0e73:  ldstr      "f128    = "
    IL_0e78:  ldarg.0
    IL_0e79:  ldfld      float64 MCCTest.VType7::f128
    IL_0e7e:  box        [mscorlib]System.Double
    IL_0e83:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0e88:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0e8d:  nop
    IL_0e8e:  ldloc.1
    IL_0e8f:  ldloc.0
    IL_0e90:  ldstr      "f129    = "
    IL_0e95:  ldarg.0
    IL_0e96:  ldfld      float64 MCCTest.VType7::f129
    IL_0e9b:  box        [mscorlib]System.Double
    IL_0ea0:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0ea5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0eaa:  nop
    IL_0eab:  ldloc.1
    IL_0eac:  ldloc.0
    IL_0ead:  ldstr      "f130    = "
    IL_0eb2:  ldarg.0
    IL_0eb3:  ldfld      float64 MCCTest.VType7::f130
    IL_0eb8:  box        [mscorlib]System.Double
    IL_0ebd:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0ec2:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0ec7:  nop
    IL_0ec8:  ldloc.1
    IL_0ec9:  ldloc.0
    IL_0eca:  ldstr      "f131    = "
    IL_0ecf:  ldarg.0
    IL_0ed0:  ldfld      float64 MCCTest.VType7::f131
    IL_0ed5:  box        [mscorlib]System.Double
    IL_0eda:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0edf:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0ee4:  nop
    IL_0ee5:  ldloc.1
    IL_0ee6:  ldloc.0
    IL_0ee7:  ldstr      "f132    = "
    IL_0eec:  ldarg.0
    IL_0eed:  ldfld      float64 MCCTest.VType7::f132
    IL_0ef2:  box        [mscorlib]System.Double
    IL_0ef7:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0efc:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0f01:  nop
    IL_0f02:  ldloc.1
    IL_0f03:  ldloc.0
    IL_0f04:  ldstr      "f133    = "
    IL_0f09:  ldarg.0
    IL_0f0a:  ldfld      float64 MCCTest.VType7::f133
    IL_0f0f:  box        [mscorlib]System.Double
    IL_0f14:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0f19:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0f1e:  nop
    IL_0f1f:  ldloc.1
    IL_0f20:  ldloc.0
    IL_0f21:  ldstr      "f134    = "
    IL_0f26:  ldarg.0
    IL_0f27:  ldfld      float64 MCCTest.VType7::f134
    IL_0f2c:  box        [mscorlib]System.Double
    IL_0f31:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0f36:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0f3b:  nop
    IL_0f3c:  ldloc.1
    IL_0f3d:  ldloc.0
    IL_0f3e:  ldstr      "f135    = "
    IL_0f43:  ldarg.0
    IL_0f44:  ldfld      float64 MCCTest.VType7::f135
    IL_0f49:  box        [mscorlib]System.Double
    IL_0f4e:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0f53:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0f58:  nop
    IL_0f59:  ldloc.1
    IL_0f5a:  ldloc.0
    IL_0f5b:  ldstr      "f136    = "
    IL_0f60:  ldarg.0
    IL_0f61:  ldfld      float64 MCCTest.VType7::f136
    IL_0f66:  box        [mscorlib]System.Double
    IL_0f6b:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0f70:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0f75:  nop
    IL_0f76:  ldloc.1
    IL_0f77:  ldloc.0
    IL_0f78:  ldstr      "f137    = "
    IL_0f7d:  ldarg.0
    IL_0f7e:  ldfld      float64 MCCTest.VType7::f137
    IL_0f83:  box        [mscorlib]System.Double
    IL_0f88:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0f8d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0f92:  nop
    IL_0f93:  ldloc.1
    IL_0f94:  ldloc.0
    IL_0f95:  ldstr      "f138    = "
    IL_0f9a:  ldarg.0
    IL_0f9b:  ldfld      float64 MCCTest.VType7::f138
    IL_0fa0:  box        [mscorlib]System.Double
    IL_0fa5:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0faa:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0faf:  nop
    IL_0fb0:  ldloc.1
    IL_0fb1:  ldloc.0
    IL_0fb2:  ldstr      "f139    = "
    IL_0fb7:  ldarg.0
    IL_0fb8:  ldfld      float64 MCCTest.VType7::f139
    IL_0fbd:  box        [mscorlib]System.Double
    IL_0fc2:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0fc7:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0fcc:  nop
    IL_0fcd:  ldloc.1
    IL_0fce:  ldloc.0
    IL_0fcf:  ldstr      "f140    = "
    IL_0fd4:  ldarg.0
    IL_0fd5:  ldfld      float64 MCCTest.VType7::f140
    IL_0fda:  box        [mscorlib]System.Double
    IL_0fdf:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0fe4:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0fe9:  nop
    IL_0fea:  ldloc.1
    IL_0feb:  ldloc.0
    IL_0fec:  ldstr      "f141    = "
    IL_0ff1:  ldarg.0
    IL_0ff2:  ldfld      float64 MCCTest.VType7::f141
    IL_0ff7:  box        [mscorlib]System.Double
    IL_0ffc:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1001:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1006:  nop
    IL_1007:  ldloc.1
    IL_1008:  ldloc.0
    IL_1009:  ldstr      "f142    = "
    IL_100e:  ldarg.0
    IL_100f:  ldfld      float64 MCCTest.VType7::f142
    IL_1014:  box        [mscorlib]System.Double
    IL_1019:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_101e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1023:  nop
    IL_1024:  ldloc.1
    IL_1025:  ldloc.0
    IL_1026:  ldstr      "f143    = "
    IL_102b:  ldarg.0
    IL_102c:  ldfld      float64 MCCTest.VType7::f143
    IL_1031:  box        [mscorlib]System.Double
    IL_1036:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_103b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1040:  nop
    IL_1041:  ldloc.1
    IL_1042:  ldloc.0
    IL_1043:  ldstr      "f144    = "
    IL_1048:  ldarg.0
    IL_1049:  ldfld      float64 MCCTest.VType7::f144
    IL_104e:  box        [mscorlib]System.Double
    IL_1053:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1058:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_105d:  nop
    IL_105e:  ldloc.1
    IL_105f:  ldloc.0
    IL_1060:  ldstr      "f145    = "
    IL_1065:  ldarg.0
    IL_1066:  ldfld      float64 MCCTest.VType7::f145
    IL_106b:  box        [mscorlib]System.Double
    IL_1070:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1075:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_107a:  nop
    IL_107b:  ldloc.1
    IL_107c:  ldloc.0
    IL_107d:  ldstr      "f146    = "
    IL_1082:  ldarg.0
    IL_1083:  ldfld      float64 MCCTest.VType7::f146
    IL_1088:  box        [mscorlib]System.Double
    IL_108d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1092:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1097:  nop
    IL_1098:  ldloc.1
    IL_1099:  ldloc.0
    IL_109a:  ldstr      "f147    = "
    IL_109f:  ldarg.0
    IL_10a0:  ldfld      float64 MCCTest.VType7::f147
    IL_10a5:  box        [mscorlib]System.Double
    IL_10aa:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_10af:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_10b4:  nop
    IL_10b5:  ldloc.1
    IL_10b6:  ldloc.0
    IL_10b7:  ldstr      "f148    = "
    IL_10bc:  ldarg.0
    IL_10bd:  ldfld      float64 MCCTest.VType7::f148
    IL_10c2:  box        [mscorlib]System.Double
    IL_10c7:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_10cc:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_10d1:  nop
    IL_10d2:  ldloc.1
    IL_10d3:  ldloc.0
    IL_10d4:  ldstr      "f149    = "
    IL_10d9:  ldarg.0
    IL_10da:  ldfld      float64 MCCTest.VType7::f149
    IL_10df:  box        [mscorlib]System.Double
    IL_10e4:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_10e9:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_10ee:  nop
    IL_10ef:  ldloc.1
    IL_10f0:  ldloc.0
    IL_10f1:  ldstr      "f150    = "
    IL_10f6:  ldarg.0
    IL_10f7:  ldfld      float64 MCCTest.VType7::f150
    IL_10fc:  box        [mscorlib]System.Double
    IL_1101:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1106:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_110b:  nop
    IL_110c:  ldloc.1
    IL_110d:  ldloc.0
    IL_110e:  ldstr      "f151    = "
    IL_1113:  ldarg.0
    IL_1114:  ldfld      float64 MCCTest.VType7::f151
    IL_1119:  box        [mscorlib]System.Double
    IL_111e:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1123:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1128:  nop
    IL_1129:  ldloc.1
    IL_112a:  ldloc.0
    IL_112b:  ldstr      "f152    = "
    IL_1130:  ldarg.0
    IL_1131:  ldfld      float64 MCCTest.VType7::f152
    IL_1136:  box        [mscorlib]System.Double
    IL_113b:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1140:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1145:  nop
    IL_1146:  ldloc.1
    IL_1147:  ldloc.0
    IL_1148:  ldstr      "f153    = "
    IL_114d:  ldarg.0
    IL_114e:  ldfld      float64 MCCTest.VType7::f153
    IL_1153:  box        [mscorlib]System.Double
    IL_1158:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_115d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1162:  nop
    IL_1163:  ldloc.1
    IL_1164:  ldloc.0
    IL_1165:  ldstr      "f154    = "
    IL_116a:  ldarg.0
    IL_116b:  ldfld      float64 MCCTest.VType7::f154
    IL_1170:  box        [mscorlib]System.Double
    IL_1175:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_117a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_117f:  nop
    IL_1180:  ldloc.1
    IL_1181:  ldloc.0
    IL_1182:  ldstr      "f155    = "
    IL_1187:  ldarg.0
    IL_1188:  ldfld      float64 MCCTest.VType7::f155
    IL_118d:  box        [mscorlib]System.Double
    IL_1192:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1197:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_119c:  nop
    IL_119d:  ldloc.1
    IL_119e:  ldloc.0
    IL_119f:  ldstr      "f156    = "
    IL_11a4:  ldarg.0
    IL_11a5:  ldfld      float64 MCCTest.VType7::f156
    IL_11aa:  box        [mscorlib]System.Double
    IL_11af:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_11b4:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_11b9:  nop
    IL_11ba:  ldloc.1
    IL_11bb:  ldloc.0
    IL_11bc:  ldstr      "f157    = "
    IL_11c1:  ldarg.0
    IL_11c2:  ldfld      float64 MCCTest.VType7::f157
    IL_11c7:  box        [mscorlib]System.Double
    IL_11cc:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_11d1:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_11d6:  nop
    IL_11d7:  ldloc.1
    IL_11d8:  ldloc.0
    IL_11d9:  ldstr      "f158    = "
    IL_11de:  ldarg.0
    IL_11df:  ldfld      float64 MCCTest.VType7::f158
    IL_11e4:  box        [mscorlib]System.Double
    IL_11e9:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_11ee:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_11f3:  nop
    IL_11f4:  ldloc.1
    IL_11f5:  ldloc.0
    IL_11f6:  ldstr      "f159    = "
    IL_11fb:  ldarg.0
    IL_11fc:  ldfld      float64 MCCTest.VType7::f159
    IL_1201:  box        [mscorlib]System.Double
    IL_1206:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_120b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1210:  nop
    IL_1211:  ldloc.1
    IL_1212:  ldloc.0
    IL_1213:  ldstr      "f160    = "
    IL_1218:  ldarg.0
    IL_1219:  ldfld      float64 MCCTest.VType7::f160
    IL_121e:  box        [mscorlib]System.Double
    IL_1223:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1228:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_122d:  nop
    IL_122e:  ldloc.1
    IL_122f:  ldloc.0
    IL_1230:  ldstr      "f161    = "
    IL_1235:  ldarg.0
    IL_1236:  ldfld      float64 MCCTest.VType7::f161
    IL_123b:  box        [mscorlib]System.Double
    IL_1240:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1245:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_124a:  nop
    IL_124b:  ldloc.1
    IL_124c:  ldloc.0
    IL_124d:  ldstr      "f162    = "
    IL_1252:  ldarg.0
    IL_1253:  ldfld      float64 MCCTest.VType7::f162
    IL_1258:  box        [mscorlib]System.Double
    IL_125d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1262:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1267:  nop
    IL_1268:  ldloc.1
    IL_1269:  ldloc.0
    IL_126a:  ldstr      "f163    = "
    IL_126f:  ldarg.0
    IL_1270:  ldfld      float64 MCCTest.VType7::f163
    IL_1275:  box        [mscorlib]System.Double
    IL_127a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_127f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1284:  nop
    IL_1285:  ldloc.1
    IL_1286:  ldloc.0
    IL_1287:  ldstr      "f164    = "
    IL_128c:  ldarg.0
    IL_128d:  ldfld      float64 MCCTest.VType7::f164
    IL_1292:  box        [mscorlib]System.Double
    IL_1297:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_129c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_12a1:  nop
    IL_12a2:  ldloc.1
    IL_12a3:  ldloc.0
    IL_12a4:  ldstr      "f165    = "
    IL_12a9:  ldarg.0
    IL_12aa:  ldfld      float64 MCCTest.VType7::f165
    IL_12af:  box        [mscorlib]System.Double
    IL_12b4:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_12b9:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_12be:  nop
    IL_12bf:  ldloc.1
    IL_12c0:  ldloc.0
    IL_12c1:  ldstr      "f166    = "
    IL_12c6:  ldarg.0
    IL_12c7:  ldfld      float64 MCCTest.VType7::f166
    IL_12cc:  box        [mscorlib]System.Double
    IL_12d1:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_12d6:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_12db:  nop
    IL_12dc:  ldloc.1
    IL_12dd:  ldloc.0
    IL_12de:  ldstr      "f167    = "
    IL_12e3:  ldarg.0
    IL_12e4:  ldfld      float64 MCCTest.VType7::f167
    IL_12e9:  box        [mscorlib]System.Double
    IL_12ee:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_12f3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_12f8:  nop
    IL_12f9:  ldloc.1
    IL_12fa:  ldloc.0
    IL_12fb:  ldstr      "f168    = "
    IL_1300:  ldarg.0
    IL_1301:  ldfld      float64 MCCTest.VType7::f168
    IL_1306:  box        [mscorlib]System.Double
    IL_130b:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1310:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1315:  nop
    IL_1316:  ldloc.1
    IL_1317:  ldloc.0
    IL_1318:  ldstr      "f169    = "
    IL_131d:  ldarg.0
    IL_131e:  ldfld      float64 MCCTest.VType7::f169
    IL_1323:  box        [mscorlib]System.Double
    IL_1328:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_132d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1332:  nop
    IL_1333:  ldloc.1
    IL_1334:  ldloc.0
    IL_1335:  ldstr      "f170    = "
    IL_133a:  ldarg.0
    IL_133b:  ldfld      float64 MCCTest.VType7::f170
    IL_1340:  box        [mscorlib]System.Double
    IL_1345:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_134a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_134f:  nop
    IL_1350:  ldloc.1
    IL_1351:  ldloc.0
    IL_1352:  ldstr      "f171    = "
    IL_1357:  ldarg.0
    IL_1358:  ldfld      float64 MCCTest.VType7::f171
    IL_135d:  box        [mscorlib]System.Double
    IL_1362:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1367:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_136c:  nop
    IL_136d:  ldloc.1
    IL_136e:  ldloc.0
    IL_136f:  ldstr      "f172    = "
    IL_1374:  ldarg.0
    IL_1375:  ldfld      float64 MCCTest.VType7::f172
    IL_137a:  box        [mscorlib]System.Double
    IL_137f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1384:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1389:  nop
    IL_138a:  ldloc.1
    IL_138b:  ldloc.0
    IL_138c:  ldstr      "f173    = "
    IL_1391:  ldarg.0
    IL_1392:  ldfld      float64 MCCTest.VType7::f173
    IL_1397:  box        [mscorlib]System.Double
    IL_139c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_13a1:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_13a6:  nop
    IL_13a7:  ldloc.1
    IL_13a8:  ldloc.0
    IL_13a9:  ldstr      "f174    = "
    IL_13ae:  ldarg.0
    IL_13af:  ldfld      float64 MCCTest.VType7::f174
    IL_13b4:  box        [mscorlib]System.Double
    IL_13b9:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_13be:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_13c3:  nop
    IL_13c4:  ldloc.1
    IL_13c5:  ldloc.0
    IL_13c6:  ldstr      "f175    = "
    IL_13cb:  ldarg.0
    IL_13cc:  ldfld      float64 MCCTest.VType7::f175
    IL_13d1:  box        [mscorlib]System.Double
    IL_13d6:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_13db:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_13e0:  nop
    IL_13e1:  ldloc.1
    IL_13e2:  ldloc.0
    IL_13e3:  ldstr      "f176    = "
    IL_13e8:  ldarg.0
    IL_13e9:  ldfld      float64 MCCTest.VType7::f176
    IL_13ee:  box        [mscorlib]System.Double
    IL_13f3:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_13f8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_13fd:  nop
    IL_13fe:  ldloc.1
    IL_13ff:  ldloc.0
    IL_1400:  ldstr      "f177    = "
    IL_1405:  ldarg.0
    IL_1406:  ldfld      float64 MCCTest.VType7::f177
    IL_140b:  box        [mscorlib]System.Double
    IL_1410:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1415:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_141a:  nop
    IL_141b:  ldloc.1
    IL_141c:  ldloc.0
    IL_141d:  ldstr      "f178    = "
    IL_1422:  ldarg.0
    IL_1423:  ldfld      float64 MCCTest.VType7::f178
    IL_1428:  box        [mscorlib]System.Double
    IL_142d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1432:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1437:  nop
    IL_1438:  ldloc.1
    IL_1439:  ldloc.0
    IL_143a:  ldstr      "f179    = "
    IL_143f:  ldarg.0
    IL_1440:  ldfld      float64 MCCTest.VType7::f179
    IL_1445:  box        [mscorlib]System.Double
    IL_144a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_144f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1454:  nop
    IL_1455:  ldloc.1
    IL_1456:  ldloc.0
    IL_1457:  ldstr      "f180    = "
    IL_145c:  ldarg.0
    IL_145d:  ldfld      float64 MCCTest.VType7::f180
    IL_1462:  box        [mscorlib]System.Double
    IL_1467:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_146c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1471:  nop
    IL_1472:  ldloc.1
    IL_1473:  ldloc.0
    IL_1474:  ldstr      "f181    = "
    IL_1479:  ldarg.0
    IL_147a:  ldfld      float64 MCCTest.VType7::f181
    IL_147f:  box        [mscorlib]System.Double
    IL_1484:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1489:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_148e:  nop
    IL_148f:  ldloc.1
    IL_1490:  ldloc.0
    IL_1491:  ldstr      "f182    = "
    IL_1496:  ldarg.0
    IL_1497:  ldfld      float64 MCCTest.VType7::f182
    IL_149c:  box        [mscorlib]System.Double
    IL_14a1:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_14a6:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_14ab:  nop
    IL_14ac:  ldloc.1
    IL_14ad:  ldloc.0
    IL_14ae:  ldstr      "f183    = "
    IL_14b3:  ldarg.0
    IL_14b4:  ldfld      float64 MCCTest.VType7::f183
    IL_14b9:  box        [mscorlib]System.Double
    IL_14be:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_14c3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_14c8:  nop
    IL_14c9:  ldloc.1
    IL_14ca:  ldloc.0
    IL_14cb:  ldstr      "f184    = "
    IL_14d0:  ldarg.0
    IL_14d1:  ldfld      float64 MCCTest.VType7::f184
    IL_14d6:  box        [mscorlib]System.Double
    IL_14db:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_14e0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_14e5:  nop
    IL_14e6:  ldloc.1
    IL_14e7:  ldloc.0
    IL_14e8:  ldstr      "f185    = "
    IL_14ed:  ldarg.0
    IL_14ee:  ldfld      float64 MCCTest.VType7::f185
    IL_14f3:  box        [mscorlib]System.Double
    IL_14f8:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_14fd:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1502:  nop
    IL_1503:  ldloc.1
    IL_1504:  ldloc.0
    IL_1505:  ldstr      "f186    = "
    IL_150a:  ldarg.0
    IL_150b:  ldfld      float64 MCCTest.VType7::f186
    IL_1510:  box        [mscorlib]System.Double
    IL_1515:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_151a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_151f:  nop
    IL_1520:  ldloc.1
    IL_1521:  ldloc.0
    IL_1522:  ldstr      "f187    = "
    IL_1527:  ldarg.0
    IL_1528:  ldfld      float64 MCCTest.VType7::f187
    IL_152d:  box        [mscorlib]System.Double
    IL_1532:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1537:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_153c:  nop
    IL_153d:  ldloc.1
    IL_153e:  ldloc.0
    IL_153f:  ldstr      "f188    = "
    IL_1544:  ldarg.0
    IL_1545:  ldfld      float64 MCCTest.VType7::f188
    IL_154a:  box        [mscorlib]System.Double
    IL_154f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1554:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1559:  nop
    IL_155a:  ldloc.1
    IL_155b:  ldloc.0
    IL_155c:  ldstr      "f189    = "
    IL_1561:  ldarg.0
    IL_1562:  ldfld      float64 MCCTest.VType7::f189
    IL_1567:  box        [mscorlib]System.Double
    IL_156c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1571:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1576:  nop
    IL_1577:  ldloc.1
    IL_1578:  ldloc.0
    IL_1579:  ldstr      "f190    = "
    IL_157e:  ldarg.0
    IL_157f:  ldfld      float64 MCCTest.VType7::f190
    IL_1584:  box        [mscorlib]System.Double
    IL_1589:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_158e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1593:  nop
    IL_1594:  ldloc.1
    IL_1595:  ldloc.0
    IL_1596:  ldstr      "f191    = "
    IL_159b:  ldarg.0
    IL_159c:  ldfld      float64 MCCTest.VType7::f191
    IL_15a1:  box        [mscorlib]System.Double
    IL_15a6:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_15ab:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_15b0:  nop
    IL_15b1:  ldloc.1
    IL_15b2:  ldloc.0
    IL_15b3:  ldstr      "f192    = "
    IL_15b8:  ldarg.0
    IL_15b9:  ldfld      float64 MCCTest.VType7::f192
    IL_15be:  box        [mscorlib]System.Double
    IL_15c3:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_15c8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_15cd:  nop
    IL_15ce:  ldloc.1
    IL_15cf:  ldloc.0
    IL_15d0:  ldstr      "f193    = "
    IL_15d5:  ldarg.0
    IL_15d6:  ldfld      float64 MCCTest.VType7::f193
    IL_15db:  box        [mscorlib]System.Double
    IL_15e0:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_15e5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_15ea:  nop
    IL_15eb:  ldloc.1
    IL_15ec:  ldloc.0
    IL_15ed:  ldstr      "f194    = "
    IL_15f2:  ldarg.0
    IL_15f3:  ldfld      float64 MCCTest.VType7::f194
    IL_15f8:  box        [mscorlib]System.Double
    IL_15fd:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1602:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1607:  nop
    IL_1608:  ldloc.1
    IL_1609:  ldloc.0
    IL_160a:  ldstr      "f195    = "
    IL_160f:  ldarg.0
    IL_1610:  ldfld      float64 MCCTest.VType7::f195
    IL_1615:  box        [mscorlib]System.Double
    IL_161a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_161f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1624:  nop
    IL_1625:  ldloc.1
    IL_1626:  ldloc.0
    IL_1627:  ldstr      "f196    = "
    IL_162c:  ldarg.0
    IL_162d:  ldfld      float64 MCCTest.VType7::f196
    IL_1632:  box        [mscorlib]System.Double
    IL_1637:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_163c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1641:  nop
    IL_1642:  ldloc.1
    IL_1643:  ldloc.0
    IL_1644:  ldstr      "f197    = "
    IL_1649:  ldarg.0
    IL_164a:  ldfld      float64 MCCTest.VType7::f197
    IL_164f:  box        [mscorlib]System.Double
    IL_1654:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1659:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_165e:  nop
    IL_165f:  ldloc.1
    IL_1660:  ldloc.0
    IL_1661:  ldstr      "f198    = "
    IL_1666:  ldarg.0
    IL_1667:  ldfld      float64 MCCTest.VType7::f198
    IL_166c:  box        [mscorlib]System.Double
    IL_1671:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1676:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_167b:  nop
    IL_167c:  ldloc.1
    IL_167d:  ldloc.0
    IL_167e:  ldstr      "f199    = "
    IL_1683:  ldarg.0
    IL_1684:  ldfld      float64 MCCTest.VType7::f199
    IL_1689:  box        [mscorlib]System.Double
    IL_168e:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1693:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1698:  nop
    IL_1699:  ldloc.1
    IL_169a:  ldloc.0
    IL_169b:  ldstr      "f200    = "
    IL_16a0:  ldarg.0
    IL_16a1:  ldfld      float64 MCCTest.VType7::f200
    IL_16a6:  box        [mscorlib]System.Double
    IL_16ab:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_16b0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_16b5:  nop
    IL_16b6:  ldloc.1
    IL_16b7:  ldloc.0
    IL_16b8:  ldstr      "f201    = "
    IL_16bd:  ldarg.0
    IL_16be:  ldfld      float64 MCCTest.VType7::f201
    IL_16c3:  box        [mscorlib]System.Double
    IL_16c8:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_16cd:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_16d2:  nop
    IL_16d3:  ldloc.1
    IL_16d4:  ldloc.0
    IL_16d5:  ldstr      "f202    = "
    IL_16da:  ldarg.0
    IL_16db:  ldfld      float64 MCCTest.VType7::f202
    IL_16e0:  box        [mscorlib]System.Double
    IL_16e5:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_16ea:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_16ef:  nop
    IL_16f0:  ldloc.1
    IL_16f1:  ldloc.0
    IL_16f2:  ldstr      "f203    = "
    IL_16f7:  ldarg.0
    IL_16f8:  ldfld      float64 MCCTest.VType7::f203
    IL_16fd:  box        [mscorlib]System.Double
    IL_1702:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1707:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_170c:  nop
    IL_170d:  ldloc.1
    IL_170e:  ldloc.0
    IL_170f:  ldstr      "f204    = "
    IL_1714:  ldarg.0
    IL_1715:  ldfld      float64 MCCTest.VType7::f204
    IL_171a:  box        [mscorlib]System.Double
    IL_171f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1724:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1729:  nop
    IL_172a:  ldloc.1
    IL_172b:  ldloc.0
    IL_172c:  ldstr      "f205    = "
    IL_1731:  ldarg.0
    IL_1732:  ldfld      float64 MCCTest.VType7::f205
    IL_1737:  box        [mscorlib]System.Double
    IL_173c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1741:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1746:  nop
    IL_1747:  ldloc.1
    IL_1748:  ldloc.0
    IL_1749:  ldstr      "f206    = "
    IL_174e:  ldarg.0
    IL_174f:  ldfld      float64 MCCTest.VType7::f206
    IL_1754:  box        [mscorlib]System.Double
    IL_1759:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_175e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1763:  nop
    IL_1764:  ldloc.1
    IL_1765:  ldloc.0
    IL_1766:  ldstr      "f207    = "
    IL_176b:  ldarg.0
    IL_176c:  ldfld      float64 MCCTest.VType7::f207
    IL_1771:  box        [mscorlib]System.Double
    IL_1776:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_177b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1780:  nop
    IL_1781:  ldloc.1
    IL_1782:  ldloc.0
    IL_1783:  ldstr      "f208    = "
    IL_1788:  ldarg.0
    IL_1789:  ldfld      float64 MCCTest.VType7::f208
    IL_178e:  box        [mscorlib]System.Double
    IL_1793:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1798:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_179d:  nop
    IL_179e:  ldloc.1
    IL_179f:  ldloc.0
    IL_17a0:  ldstr      "f209    = "
    IL_17a5:  ldarg.0
    IL_17a6:  ldfld      float64 MCCTest.VType7::f209
    IL_17ab:  box        [mscorlib]System.Double
    IL_17b0:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_17b5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_17ba:  nop
    IL_17bb:  ldloc.1
    IL_17bc:  ldloc.0
    IL_17bd:  ldstr      "f210    = "
    IL_17c2:  ldarg.0
    IL_17c3:  ldfld      float64 MCCTest.VType7::f210
    IL_17c8:  box        [mscorlib]System.Double
    IL_17cd:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_17d2:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_17d7:  nop
    IL_17d8:  ldloc.1
    IL_17d9:  ldloc.0
    IL_17da:  ldstr      "f211    = "
    IL_17df:  ldarg.0
    IL_17e0:  ldfld      float64 MCCTest.VType7::f211
    IL_17e5:  box        [mscorlib]System.Double
    IL_17ea:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_17ef:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_17f4:  nop
    IL_17f5:  ldloc.1
    IL_17f6:  ldloc.0
    IL_17f7:  ldstr      "f212    = "
    IL_17fc:  ldarg.0
    IL_17fd:  ldfld      float64 MCCTest.VType7::f212
    IL_1802:  box        [mscorlib]System.Double
    IL_1807:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_180c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1811:  nop
    IL_1812:  ldloc.1
    IL_1813:  ldloc.0
    IL_1814:  ldstr      "f213    = "
    IL_1819:  ldarg.0
    IL_181a:  ldfld      float64 MCCTest.VType7::f213
    IL_181f:  box        [mscorlib]System.Double
    IL_1824:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1829:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_182e:  nop
    IL_182f:  ldloc.1
    IL_1830:  ldloc.0
    IL_1831:  ldstr      "f214    = "
    IL_1836:  ldarg.0
    IL_1837:  ldfld      float64 MCCTest.VType7::f214
    IL_183c:  box        [mscorlib]System.Double
    IL_1841:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1846:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_184b:  nop
    IL_184c:  ldloc.1
    IL_184d:  ldloc.0
    IL_184e:  ldstr      "f215    = "
    IL_1853:  ldarg.0
    IL_1854:  ldfld      float64 MCCTest.VType7::f215
    IL_1859:  box        [mscorlib]System.Double
    IL_185e:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1863:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1868:  nop
    IL_1869:  ldloc.1
    IL_186a:  ldloc.0
    IL_186b:  ldstr      "f216    = "
    IL_1870:  ldarg.0
    IL_1871:  ldfld      float64 MCCTest.VType7::f216
    IL_1876:  box        [mscorlib]System.Double
    IL_187b:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1880:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1885:  nop
    IL_1886:  ldloc.1
    IL_1887:  ldloc.0
    IL_1888:  ldstr      "f217    = "
    IL_188d:  ldarg.0
    IL_188e:  ldfld      float64 MCCTest.VType7::f217
    IL_1893:  box        [mscorlib]System.Double
    IL_1898:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_189d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_18a2:  nop
    IL_18a3:  ldloc.1
    IL_18a4:  ldloc.0
    IL_18a5:  ldstr      "f218    = "
    IL_18aa:  ldarg.0
    IL_18ab:  ldfld      float64 MCCTest.VType7::f218
    IL_18b0:  box        [mscorlib]System.Double
    IL_18b5:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_18ba:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_18bf:  nop
    IL_18c0:  ldloc.1
    IL_18c1:  ldloc.0
    IL_18c2:  ldstr      "f219    = "
    IL_18c7:  ldarg.0
    IL_18c8:  ldfld      float64 MCCTest.VType7::f219
    IL_18cd:  box        [mscorlib]System.Double
    IL_18d2:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_18d7:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_18dc:  nop
    IL_18dd:  ldloc.1
    IL_18de:  ldloc.0
    IL_18df:  ldstr      "f220    = "
    IL_18e4:  ldarg.0
    IL_18e5:  ldfld      float64 MCCTest.VType7::f220
    IL_18ea:  box        [mscorlib]System.Double
    IL_18ef:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_18f4:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_18f9:  nop
    IL_18fa:  ldloc.1
    IL_18fb:  ldloc.0
    IL_18fc:  ldstr      "f221    = "
    IL_1901:  ldarg.0
    IL_1902:  ldfld      float64 MCCTest.VType7::f221
    IL_1907:  box        [mscorlib]System.Double
    IL_190c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1911:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1916:  nop
    IL_1917:  ldloc.1
    IL_1918:  ldloc.0
    IL_1919:  ldstr      "f222    = "
    IL_191e:  ldarg.0
    IL_191f:  ldfld      float64 MCCTest.VType7::f222
    IL_1924:  box        [mscorlib]System.Double
    IL_1929:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_192e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1933:  nop
    IL_1934:  ldloc.1
    IL_1935:  ldloc.0
    IL_1936:  ldstr      "f223    = "
    IL_193b:  ldarg.0
    IL_193c:  ldfld      float64 MCCTest.VType7::f223
    IL_1941:  box        [mscorlib]System.Double
    IL_1946:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_194b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1950:  nop
    IL_1951:  ldloc.1
    IL_1952:  ldloc.0
    IL_1953:  ldstr      "f224    = "
    IL_1958:  ldarg.0
    IL_1959:  ldfld      float64 MCCTest.VType7::f224
    IL_195e:  box        [mscorlib]System.Double
    IL_1963:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1968:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_196d:  nop
    IL_196e:  ldloc.1
    IL_196f:  ldloc.0
    IL_1970:  ldstr      "f225    = "
    IL_1975:  ldarg.0
    IL_1976:  ldfld      float64 MCCTest.VType7::f225
    IL_197b:  box        [mscorlib]System.Double
    IL_1980:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1985:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_198a:  nop
    IL_198b:  ldloc.1
    IL_198c:  ldloc.0
    IL_198d:  ldstr      "f226    = "
    IL_1992:  ldarg.0
    IL_1993:  ldfld      float64 MCCTest.VType7::f226
    IL_1998:  box        [mscorlib]System.Double
    IL_199d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_19a2:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_19a7:  nop
    IL_19a8:  ldloc.1
    IL_19a9:  ldloc.0
    IL_19aa:  ldstr      "f227    = "
    IL_19af:  ldarg.0
    IL_19b0:  ldfld      float64 MCCTest.VType7::f227
    IL_19b5:  box        [mscorlib]System.Double
    IL_19ba:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_19bf:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_19c4:  nop
    IL_19c5:  ldloc.1
    IL_19c6:  ldloc.0
    IL_19c7:  ldstr      "f228    = "
    IL_19cc:  ldarg.0
    IL_19cd:  ldfld      float64 MCCTest.VType7::f228
    IL_19d2:  box        [mscorlib]System.Double
    IL_19d7:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_19dc:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_19e1:  nop
    IL_19e2:  ldloc.1
    IL_19e3:  ldloc.0
    IL_19e4:  ldstr      "f229    = "
    IL_19e9:  ldarg.0
    IL_19ea:  ldfld      float64 MCCTest.VType7::f229
    IL_19ef:  box        [mscorlib]System.Double
    IL_19f4:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_19f9:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_19fe:  nop
    IL_19ff:  ldloc.1
    IL_1a00:  ldloc.0
    IL_1a01:  ldstr      "f230    = "
    IL_1a06:  ldarg.0
    IL_1a07:  ldfld      float64 MCCTest.VType7::f230
    IL_1a0c:  box        [mscorlib]System.Double
    IL_1a11:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1a16:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1a1b:  nop
    IL_1a1c:  ldloc.1
    IL_1a1d:  ldloc.0
    IL_1a1e:  ldstr      "f231    = "
    IL_1a23:  ldarg.0
    IL_1a24:  ldfld      float64 MCCTest.VType7::f231
    IL_1a29:  box        [mscorlib]System.Double
    IL_1a2e:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1a33:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1a38:  nop
    IL_1a39:  ldloc.1
    IL_1a3a:  ldloc.0
    IL_1a3b:  ldstr      "f232    = "
    IL_1a40:  ldarg.0
    IL_1a41:  ldfld      float64 MCCTest.VType7::f232
    IL_1a46:  box        [mscorlib]System.Double
    IL_1a4b:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1a50:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1a55:  nop
    IL_1a56:  ldloc.1
    IL_1a57:  ldloc.0
    IL_1a58:  ldstr      "f233    = "
    IL_1a5d:  ldarg.0
    IL_1a5e:  ldfld      float64 MCCTest.VType7::f233
    IL_1a63:  box        [mscorlib]System.Double
    IL_1a68:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1a6d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1a72:  nop
    IL_1a73:  ldloc.1
    IL_1a74:  ldloc.0
    IL_1a75:  ldstr      "f234    = "
    IL_1a7a:  ldarg.0
    IL_1a7b:  ldfld      float64 MCCTest.VType7::f234
    IL_1a80:  box        [mscorlib]System.Double
    IL_1a85:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1a8a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1a8f:  nop
    IL_1a90:  ldloc.1
    IL_1a91:  ldloc.0
    IL_1a92:  ldstr      "f235    = "
    IL_1a97:  ldarg.0
    IL_1a98:  ldfld      float64 MCCTest.VType7::f235
    IL_1a9d:  box        [mscorlib]System.Double
    IL_1aa2:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1aa7:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1aac:  nop
    IL_1aad:  ldloc.1
    IL_1aae:  ldloc.0
    IL_1aaf:  ldstr      "f236    = "
    IL_1ab4:  ldarg.0
    IL_1ab5:  ldfld      float64 MCCTest.VType7::f236
    IL_1aba:  box        [mscorlib]System.Double
    IL_1abf:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1ac4:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1ac9:  nop
    IL_1aca:  ldloc.1
    IL_1acb:  ldloc.0
    IL_1acc:  ldstr      "f237    = "
    IL_1ad1:  ldarg.0
    IL_1ad2:  ldfld      float64 MCCTest.VType7::f237
    IL_1ad7:  box        [mscorlib]System.Double
    IL_1adc:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1ae1:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1ae6:  nop
    IL_1ae7:  ldloc.1
    IL_1ae8:  ldloc.0
    IL_1ae9:  ldstr      "f238    = "
    IL_1aee:  ldarg.0
    IL_1aef:  ldfld      float64 MCCTest.VType7::f238
    IL_1af4:  box        [mscorlib]System.Double
    IL_1af9:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1afe:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1b03:  nop
    IL_1b04:  ldloc.1
    IL_1b05:  ldloc.0
    IL_1b06:  ldstr      "f239    = "
    IL_1b0b:  ldarg.0
    IL_1b0c:  ldfld      float64 MCCTest.VType7::f239
    IL_1b11:  box        [mscorlib]System.Double
    IL_1b16:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1b1b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1b20:  nop
    IL_1b21:  ldloc.1
    IL_1b22:  ldloc.0
    IL_1b23:  ldstr      "f240    = "
    IL_1b28:  ldarg.0
    IL_1b29:  ldfld      float64 MCCTest.VType7::f240
    IL_1b2e:  box        [mscorlib]System.Double
    IL_1b33:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1b38:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1b3d:  nop
    IL_1b3e:  ldloc.1
    IL_1b3f:  ldloc.0
    IL_1b40:  ldstr      "f241    = "
    IL_1b45:  ldarg.0
    IL_1b46:  ldfld      float64 MCCTest.VType7::f241
    IL_1b4b:  box        [mscorlib]System.Double
    IL_1b50:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1b55:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1b5a:  nop
    IL_1b5b:  ldloc.1
    IL_1b5c:  ldloc.0
    IL_1b5d:  ldstr      "f242    = "
    IL_1b62:  ldarg.0
    IL_1b63:  ldfld      float64 MCCTest.VType7::f242
    IL_1b68:  box        [mscorlib]System.Double
    IL_1b6d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1b72:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1b77:  nop
    IL_1b78:  ldloc.1
    IL_1b79:  ldloc.0
    IL_1b7a:  ldstr      "f243    = "
    IL_1b7f:  ldarg.0
    IL_1b80:  ldfld      float64 MCCTest.VType7::f243
    IL_1b85:  box        [mscorlib]System.Double
    IL_1b8a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1b8f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1b94:  nop
    IL_1b95:  ldloc.1
    IL_1b96:  ldloc.0
    IL_1b97:  ldstr      "f244    = "
    IL_1b9c:  ldarg.0
    IL_1b9d:  ldfld      float64 MCCTest.VType7::f244
    IL_1ba2:  box        [mscorlib]System.Double
    IL_1ba7:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1bac:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1bb1:  nop
    IL_1bb2:  ldloc.1
    IL_1bb3:  ldloc.0
    IL_1bb4:  ldstr      "f245    = "
    IL_1bb9:  ldarg.0
    IL_1bba:  ldfld      float64 MCCTest.VType7::f245
    IL_1bbf:  box        [mscorlib]System.Double
    IL_1bc4:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1bc9:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1bce:  nop
    IL_1bcf:  ldloc.1
    IL_1bd0:  ldloc.0
    IL_1bd1:  ldstr      "f246    = "
    IL_1bd6:  ldarg.0
    IL_1bd7:  ldfld      float64 MCCTest.VType7::f246
    IL_1bdc:  box        [mscorlib]System.Double
    IL_1be1:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1be6:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1beb:  nop
    IL_1bec:  ldloc.1
    IL_1bed:  ldloc.0
    IL_1bee:  ldstr      "f247    = "
    IL_1bf3:  ldarg.0
    IL_1bf4:  ldfld      float64 MCCTest.VType7::f247
    IL_1bf9:  box        [mscorlib]System.Double
    IL_1bfe:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1c03:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1c08:  nop
    IL_1c09:  ldloc.1
    IL_1c0a:  ldloc.0
    IL_1c0b:  ldstr      "f248    = "
    IL_1c10:  ldarg.0
    IL_1c11:  ldfld      float64 MCCTest.VType7::f248
    IL_1c16:  box        [mscorlib]System.Double
    IL_1c1b:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1c20:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1c25:  nop
    IL_1c26:  ldloc.1
    IL_1c27:  ldloc.0
    IL_1c28:  ldstr      "f249    = "
    IL_1c2d:  ldarg.0
    IL_1c2e:  ldfld      float64 MCCTest.VType7::f249
    IL_1c33:  box        [mscorlib]System.Double
    IL_1c38:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1c3d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1c42:  nop
    IL_1c43:  ldloc.1
    IL_1c44:  ldloc.0
    IL_1c45:  ldstr      "f250    = "
    IL_1c4a:  ldarg.0
    IL_1c4b:  ldfld      float64 MCCTest.VType7::f250
    IL_1c50:  box        [mscorlib]System.Double
    IL_1c55:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1c5a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1c5f:  nop
    IL_1c60:  ldloc.1
    IL_1c61:  ldloc.0
    IL_1c62:  ldstr      "f251    = "
    IL_1c67:  ldarg.0
    IL_1c68:  ldfld      float64 MCCTest.VType7::f251
    IL_1c6d:  box        [mscorlib]System.Double
    IL_1c72:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1c77:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1c7c:  nop
    IL_1c7d:  ldloc.1
    IL_1c7e:  ldloc.0
    IL_1c7f:  ldstr      "f252    = "
    IL_1c84:  ldarg.0
    IL_1c85:  ldfld      float64 MCCTest.VType7::f252
    IL_1c8a:  box        [mscorlib]System.Double
    IL_1c8f:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1c94:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1c99:  nop
    IL_1c9a:  ldloc.1
    IL_1c9b:  ldloc.0
    IL_1c9c:  ldstr      "f253    = "
    IL_1ca1:  ldarg.0
    IL_1ca2:  ldfld      float64 MCCTest.VType7::f253
    IL_1ca7:  box        [mscorlib]System.Double
    IL_1cac:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1cb1:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1cb6:  nop
    IL_1cb7:  ldloc.1
    IL_1cb8:  ldloc.0
    IL_1cb9:  ldstr      "f254    = "
    IL_1cbe:  ldarg.0
    IL_1cbf:  ldfld      float64 MCCTest.VType7::f254
    IL_1cc4:  box        [mscorlib]System.Double
    IL_1cc9:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1cce:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1cd3:  nop
    IL_1cd4:  ldloc.1
    IL_1cd5:  ldloc.0
    IL_1cd6:  ldstr      "f255    = "
    IL_1cdb:  ldarg.0
    IL_1cdc:  ldfld      float64 MCCTest.VType7::f255
    IL_1ce1:  box        [mscorlib]System.Double
    IL_1ce6:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1ceb:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1cf0:  nop
    IL_1cf1:  ldloc.1
    IL_1cf2:  ldloc.0
    IL_1cf3:  ldstr      "f256    = "
    IL_1cf8:  ldarg.0
    IL_1cf9:  ldfld      float64 MCCTest.VType7::f256
    IL_1cfe:  box        [mscorlib]System.Double
    IL_1d03:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1d08:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1d0d:  nop
    IL_1d0e:  ldloc.1
    IL_1d0f:  ldloc.0
    IL_1d10:  ldstr      "f257    = "
    IL_1d15:  ldarg.0
    IL_1d16:  ldfld      float64 MCCTest.VType7::f257
    IL_1d1b:  box        [mscorlib]System.Double
    IL_1d20:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_1d25:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_1d2a:  nop
    IL_1d2b:  ldloc.1
    IL_1d2c:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_1d31:  stloc.2
    IL_1d32:  br.s       IL_1d34

    IL_1d34:  ldloc.2
    IL_1d35:  ret
  } // end of method VType7::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VType7::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VType7::Dump

} // end of class MCCTest.VType7

.class public sequential ansi sealed beforefieldinit MCCTest.VType8
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VType8>
{
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method VType8::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VType8::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType8::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VType8::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType8::Zero

  .method public hidebysig instance void
          Add(valuetype MCCTest.VType8 val) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method VType8::Add

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VType8 expected) cil managed
  {
    // Code size       22 (0x16)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.Type V_1,
             class MCCTest.ResultVerificationException V_2)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  stloc.0
    IL_0003:  ldnull
    IL_0004:  stloc.1
    .try
    {
      IL_0005:  nop
      IL_0006:  nop
      IL_0007:  leave.s    IL_0014

    }  // end .try
    catch MCCTest.ResultVerificationException
    {
      IL_0009:  stloc.2
      IL_000a:  nop
      IL_000b:  ldloc.0
      IL_000c:  ldloc.1
      IL_000d:  ldloc.2
      IL_000e:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                    class [mscorlib]System.Type,
                                                                                    class MCCTest.ResultVerificationException)
      IL_0013:  throw

    }  // end handler
    IL_0014:  nop
    IL_0015:  ret
  } // end of method VType8::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       25 (0x19)
    .maxstack  1
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0014:  stloc.2
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.2
    IL_0018:  ret
  } // end of method VType8::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VType8::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VType8::Dump

} // end of class MCCTest.VType8

.class public sequential ansi sealed beforefieldinit MCCTest.VType9
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VType9>
{
  .field public float32 f1
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  conv.r4
    IL_0004:  stfld      float32 MCCTest.VType9::f1
    IL_0009:  ret
  } // end of method VType9::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VType9::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType9::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VType9::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VType9::Zero

  .method public hidebysig instance void
          Add(valuetype MCCTest.VType9 val) cil managed
  {
    // Code size       22 (0x16)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      float32 MCCTest.VType9::f1
    IL_0008:  ldarga.s   val
    IL_000a:  ldfld      float32 MCCTest.VType9::f1
    IL_000f:  add
    IL_0010:  stfld      float32 MCCTest.VType9::f1
    IL_0015:  ret
  } // end of method VType9::Add

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VType9 expected) cil managed
  {
    // Code size       68 (0x44)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.Type V_1,
             class MCCTest.ResultVerificationException V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      float32 MCCTest.VType9::f1
    IL_0007:  ldarga.s   expected
    IL_0009:  ldfld      float32 MCCTest.VType9::f1
    IL_000e:  ceq
    IL_0010:  stloc.3
    IL_0011:  ldloc.3
    IL_0012:  brtrue.s   IL_002f

    IL_0014:  nop
    IL_0015:  ldstr      "f1"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      float32 MCCTest.VType9::f1
    IL_0020:  conv.r8
    IL_0021:  ldarga.s   expected
    IL_0023:  ldfld      float32 MCCTest.VType9::f1
    IL_0028:  conv.r8
    IL_0029:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_002e:  throw

    IL_002f:  ldnull
    IL_0030:  stloc.0
    IL_0031:  ldnull
    IL_0032:  stloc.1
    .try
    {
      IL_0033:  nop
      IL_0034:  nop
      IL_0035:  leave.s    IL_0042

    }  // end .try
    catch MCCTest.ResultVerificationException
    {
      IL_0037:  stloc.2
      IL_0038:  nop
      IL_0039:  ldloc.0
      IL_003a:  ldloc.1
      IL_003b:  ldloc.2
      IL_003c:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                    class [mscorlib]System.Type,
                                                                                    class MCCTest.ResultVerificationException)
      IL_0041:  throw

    }  // end handler
    IL_0042:  nop
    IL_0043:  ret
  } // end of method VType9::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       54 (0x36)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "f1    = "
    IL_0015:  ldarg.0
    IL_0016:  ldfld      float32 MCCTest.VType9::f1
    IL_001b:  box        [mscorlib]System.Single
    IL_0020:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0025:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_002a:  nop
    IL_002b:  ldloc.1
    IL_002c:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0031:  stloc.2
    IL_0032:  br.s       IL_0034

    IL_0034:  ldloc.2
    IL_0035:  ret
  } // end of method VType9::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VType9::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VType9::Dump

} // end of class MCCTest.VType9

.class public sequential ansi sealed beforefieldinit MCCTest.VTypeA
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VTypeA>
{
  .field public class MCCTest.RType4 f1
  .field public class MCCTest.RType4 f2
  .field public class MCCTest.RType4 f3
  .field public class MCCTest.RType4 f4
  .field public class MCCTest.RType4 f5
  .field public class MCCTest.RType4 f6
  .field public class MCCTest.RType4 f7
  .field public class MCCTest.RType4 f8
  .field public class MCCTest.RType4 f9
  .field public class MCCTest.RType4 f10
  .field public class MCCTest.RType4 f11
  .field public class MCCTest.RType4 f12
  .field public class MCCTest.RType4 f13
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       314 (0x13a)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0007:  stfld      class MCCTest.RType4 MCCTest.VTypeA::f1
    IL_000c:  ldarg.0
    IL_000d:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0012:  stfld      class MCCTest.RType4 MCCTest.VTypeA::f2
    IL_0017:  ldarg.0
    IL_0018:  newobj     instance void MCCTest.RType4::.ctor()
    IL_001d:  stfld      class MCCTest.RType4 MCCTest.VTypeA::f3
    IL_0022:  ldarg.0
    IL_0023:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0028:  stfld      class MCCTest.RType4 MCCTest.VTypeA::f4
    IL_002d:  ldarg.0
    IL_002e:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0033:  stfld      class MCCTest.RType4 MCCTest.VTypeA::f5
    IL_0038:  ldarg.0
    IL_0039:  newobj     instance void MCCTest.RType4::.ctor()
    IL_003e:  stfld      class MCCTest.RType4 MCCTest.VTypeA::f6
    IL_0043:  ldarg.0
    IL_0044:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0049:  stfld      class MCCTest.RType4 MCCTest.VTypeA::f7
    IL_004e:  ldarg.0
    IL_004f:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0054:  stfld      class MCCTest.RType4 MCCTest.VTypeA::f8
    IL_0059:  ldarg.0
    IL_005a:  newobj     instance void MCCTest.RType4::.ctor()
    IL_005f:  stfld      class MCCTest.RType4 MCCTest.VTypeA::f9
    IL_0064:  ldarg.0
    IL_0065:  newobj     instance void MCCTest.RType4::.ctor()
    IL_006a:  stfld      class MCCTest.RType4 MCCTest.VTypeA::f10
    IL_006f:  ldarg.0
    IL_0070:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0075:  stfld      class MCCTest.RType4 MCCTest.VTypeA::f11
    IL_007a:  ldarg.0
    IL_007b:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0080:  stfld      class MCCTest.RType4 MCCTest.VTypeA::f12
    IL_0085:  ldarg.0
    IL_0086:  newobj     instance void MCCTest.RType4::.ctor()
    IL_008b:  stfld      class MCCTest.RType4 MCCTest.VTypeA::f13
    IL_0090:  ldarg.0
    IL_0091:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f1
    IL_0096:  ldarg.1
    IL_0097:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_009c:  nop
    IL_009d:  ldarg.0
    IL_009e:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f2
    IL_00a3:  ldarg.1
    IL_00a4:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_00a9:  nop
    IL_00aa:  ldarg.0
    IL_00ab:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f3
    IL_00b0:  ldarg.1
    IL_00b1:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_00b6:  nop
    IL_00b7:  ldarg.0
    IL_00b8:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f4
    IL_00bd:  ldarg.1
    IL_00be:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_00c3:  nop
    IL_00c4:  ldarg.0
    IL_00c5:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f5
    IL_00ca:  ldarg.1
    IL_00cb:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_00d0:  nop
    IL_00d1:  ldarg.0
    IL_00d2:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f6
    IL_00d7:  ldarg.1
    IL_00d8:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_00dd:  nop
    IL_00de:  ldarg.0
    IL_00df:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f7
    IL_00e4:  ldarg.1
    IL_00e5:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_00ea:  nop
    IL_00eb:  ldarg.0
    IL_00ec:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f8
    IL_00f1:  ldarg.1
    IL_00f2:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_00f7:  nop
    IL_00f8:  ldarg.0
    IL_00f9:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f9
    IL_00fe:  ldarg.1
    IL_00ff:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_0104:  nop
    IL_0105:  ldarg.0
    IL_0106:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f10
    IL_010b:  ldarg.1
    IL_010c:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_0111:  nop
    IL_0112:  ldarg.0
    IL_0113:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f11
    IL_0118:  ldarg.1
    IL_0119:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_011e:  nop
    IL_011f:  ldarg.0
    IL_0120:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f12
    IL_0125:  ldarg.1
    IL_0126:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_012b:  nop
    IL_012c:  ldarg.0
    IL_012d:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f13
    IL_0132:  ldarg.1
    IL_0133:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_0138:  nop
    IL_0139:  ret
  } // end of method VTypeA::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VTypeA::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VTypeA::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VTypeA::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VTypeA::Zero

  .method public hidebysig instance void
          Add(valuetype MCCTest.VTypeA val) cil managed
  {
    // Code size       249 (0xf9)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f1
    IL_0007:  ldarga.s   val
    IL_0009:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f1
    IL_000e:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_0013:  nop
    IL_0014:  ldarg.0
    IL_0015:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f2
    IL_001a:  ldarga.s   val
    IL_001c:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f2
    IL_0021:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_0026:  nop
    IL_0027:  ldarg.0
    IL_0028:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f3
    IL_002d:  ldarga.s   val
    IL_002f:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f3
    IL_0034:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_0039:  nop
    IL_003a:  ldarg.0
    IL_003b:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f4
    IL_0040:  ldarga.s   val
    IL_0042:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f4
    IL_0047:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_004c:  nop
    IL_004d:  ldarg.0
    IL_004e:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f5
    IL_0053:  ldarga.s   val
    IL_0055:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f5
    IL_005a:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_005f:  nop
    IL_0060:  ldarg.0
    IL_0061:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f6
    IL_0066:  ldarga.s   val
    IL_0068:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f6
    IL_006d:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_0072:  nop
    IL_0073:  ldarg.0
    IL_0074:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f7
    IL_0079:  ldarga.s   val
    IL_007b:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f7
    IL_0080:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_0085:  nop
    IL_0086:  ldarg.0
    IL_0087:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f8
    IL_008c:  ldarga.s   val
    IL_008e:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f8
    IL_0093:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_0098:  nop
    IL_0099:  ldarg.0
    IL_009a:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f9
    IL_009f:  ldarga.s   val
    IL_00a1:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f9
    IL_00a6:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_00ab:  nop
    IL_00ac:  ldarg.0
    IL_00ad:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f10
    IL_00b2:  ldarga.s   val
    IL_00b4:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f10
    IL_00b9:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_00be:  nop
    IL_00bf:  ldarg.0
    IL_00c0:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f11
    IL_00c5:  ldarga.s   val
    IL_00c7:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f11
    IL_00cc:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_00d1:  nop
    IL_00d2:  ldarg.0
    IL_00d3:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f12
    IL_00d8:  ldarga.s   val
    IL_00da:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f12
    IL_00df:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_00e4:  nop
    IL_00e5:  ldarg.0
    IL_00e6:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f13
    IL_00eb:  ldarga.s   val
    IL_00ed:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f13
    IL_00f2:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_00f7:  nop
    IL_00f8:  ret
  } // end of method VTypeA::Add

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VTypeA expected) cil managed
  {
    // Code size       503 (0x1f7)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.Type V_1,
             class MCCTest.ResultVerificationException V_2)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  stloc.0
    IL_0003:  ldnull
    IL_0004:  stloc.1
    .try
    {
      IL_0005:  nop
      IL_0006:  ldstr      "f1"
      IL_000b:  stloc.0
      IL_000c:  ldarg.0
      IL_000d:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f1
      IL_0012:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0017:  stloc.1
      IL_0018:  ldarg.0
      IL_0019:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f1
      IL_001e:  ldarga.s   expected
      IL_0020:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f1
      IL_0025:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_002a:  nop
      IL_002b:  ldstr      "f2"
      IL_0030:  stloc.0
      IL_0031:  ldarg.0
      IL_0032:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f2
      IL_0037:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_003c:  stloc.1
      IL_003d:  ldarg.0
      IL_003e:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f2
      IL_0043:  ldarga.s   expected
      IL_0045:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f2
      IL_004a:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_004f:  nop
      IL_0050:  ldstr      "f3"
      IL_0055:  stloc.0
      IL_0056:  ldarg.0
      IL_0057:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f3
      IL_005c:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0061:  stloc.1
      IL_0062:  ldarg.0
      IL_0063:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f3
      IL_0068:  ldarga.s   expected
      IL_006a:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f3
      IL_006f:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_0074:  nop
      IL_0075:  ldstr      "f4"
      IL_007a:  stloc.0
      IL_007b:  ldarg.0
      IL_007c:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f4
      IL_0081:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0086:  stloc.1
      IL_0087:  ldarg.0
      IL_0088:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f4
      IL_008d:  ldarga.s   expected
      IL_008f:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f4
      IL_0094:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_0099:  nop
      IL_009a:  ldstr      "f5"
      IL_009f:  stloc.0
      IL_00a0:  ldarg.0
      IL_00a1:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f5
      IL_00a6:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_00ab:  stloc.1
      IL_00ac:  ldarg.0
      IL_00ad:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f5
      IL_00b2:  ldarga.s   expected
      IL_00b4:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f5
      IL_00b9:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_00be:  nop
      IL_00bf:  ldstr      "f6"
      IL_00c4:  stloc.0
      IL_00c5:  ldarg.0
      IL_00c6:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f6
      IL_00cb:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_00d0:  stloc.1
      IL_00d1:  ldarg.0
      IL_00d2:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f6
      IL_00d7:  ldarga.s   expected
      IL_00d9:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f6
      IL_00de:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_00e3:  nop
      IL_00e4:  ldstr      "f7"
      IL_00e9:  stloc.0
      IL_00ea:  ldarg.0
      IL_00eb:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f7
      IL_00f0:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_00f5:  stloc.1
      IL_00f6:  ldarg.0
      IL_00f7:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f7
      IL_00fc:  ldarga.s   expected
      IL_00fe:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f7
      IL_0103:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_0108:  nop
      IL_0109:  ldstr      "f8"
      IL_010e:  stloc.0
      IL_010f:  ldarg.0
      IL_0110:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f8
      IL_0115:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_011a:  stloc.1
      IL_011b:  ldarg.0
      IL_011c:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f8
      IL_0121:  ldarga.s   expected
      IL_0123:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f8
      IL_0128:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_012d:  nop
      IL_012e:  ldstr      "f9"
      IL_0133:  stloc.0
      IL_0134:  ldarg.0
      IL_0135:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f9
      IL_013a:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_013f:  stloc.1
      IL_0140:  ldarg.0
      IL_0141:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f9
      IL_0146:  ldarga.s   expected
      IL_0148:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f9
      IL_014d:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_0152:  nop
      IL_0153:  ldstr      "f10"
      IL_0158:  stloc.0
      IL_0159:  ldarg.0
      IL_015a:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f10
      IL_015f:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0164:  stloc.1
      IL_0165:  ldarg.0
      IL_0166:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f10
      IL_016b:  ldarga.s   expected
      IL_016d:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f10
      IL_0172:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_0177:  nop
      IL_0178:  ldstr      "f11"
      IL_017d:  stloc.0
      IL_017e:  ldarg.0
      IL_017f:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f11
      IL_0184:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0189:  stloc.1
      IL_018a:  ldarg.0
      IL_018b:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f11
      IL_0190:  ldarga.s   expected
      IL_0192:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f11
      IL_0197:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_019c:  nop
      IL_019d:  ldstr      "f12"
      IL_01a2:  stloc.0
      IL_01a3:  ldarg.0
      IL_01a4:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f12
      IL_01a9:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_01ae:  stloc.1
      IL_01af:  ldarg.0
      IL_01b0:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f12
      IL_01b5:  ldarga.s   expected
      IL_01b7:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f12
      IL_01bc:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_01c1:  nop
      IL_01c2:  ldstr      "f13"
      IL_01c7:  stloc.0
      IL_01c8:  ldarg.0
      IL_01c9:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f13
      IL_01ce:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_01d3:  stloc.1
      IL_01d4:  ldarg.0
      IL_01d5:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f13
      IL_01da:  ldarga.s   expected
      IL_01dc:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f13
      IL_01e1:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_01e6:  nop
      IL_01e7:  nop
      IL_01e8:  leave.s    IL_01f5

    }  // end .try
    catch MCCTest.ResultVerificationException
    {
      IL_01ea:  stloc.2
      IL_01eb:  nop
      IL_01ec:  ldloc.0
      IL_01ed:  ldloc.1
      IL_01ee:  ldloc.2
      IL_01ef:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                    class [mscorlib]System.Type,
                                                                                    class MCCTest.ResultVerificationException)
      IL_01f4:  throw

    }  // end handler
    IL_01f5:  nop
    IL_01f6:  ret
  } // end of method VTypeA::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       740 (0x2e4)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "[Field f1] [Type '{0}']"
    IL_0015:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_001a:  ldarg.0
    IL_001b:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f1
    IL_0020:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0025:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_002a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_002f:  nop
    IL_0030:  ldloc.1
    IL_0031:  ldarg.0
    IL_0032:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f1
    IL_0037:  ldarg.1
    IL_0038:  ldc.i4.1
    IL_0039:  add
    IL_003a:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_003f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0044:  nop
    IL_0045:  ldloc.1
    IL_0046:  ldloc.0
    IL_0047:  ldstr      "[Field f2] [Type '{0}']"
    IL_004c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0051:  ldarg.0
    IL_0052:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f2
    IL_0057:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_005c:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0061:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0066:  nop
    IL_0067:  ldloc.1
    IL_0068:  ldarg.0
    IL_0069:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f2
    IL_006e:  ldarg.1
    IL_006f:  ldc.i4.1
    IL_0070:  add
    IL_0071:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_0076:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_007b:  nop
    IL_007c:  ldloc.1
    IL_007d:  ldloc.0
    IL_007e:  ldstr      "[Field f3] [Type '{0}']"
    IL_0083:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0088:  ldarg.0
    IL_0089:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f3
    IL_008e:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0093:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0098:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_009d:  nop
    IL_009e:  ldloc.1
    IL_009f:  ldarg.0
    IL_00a0:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f3
    IL_00a5:  ldarg.1
    IL_00a6:  ldc.i4.1
    IL_00a7:  add
    IL_00a8:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_00ad:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00b2:  nop
    IL_00b3:  ldloc.1
    IL_00b4:  ldloc.0
    IL_00b5:  ldstr      "[Field f4] [Type '{0}']"
    IL_00ba:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_00bf:  ldarg.0
    IL_00c0:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f4
    IL_00c5:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_00ca:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00cf:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_00d4:  nop
    IL_00d5:  ldloc.1
    IL_00d6:  ldarg.0
    IL_00d7:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f4
    IL_00dc:  ldarg.1
    IL_00dd:  ldc.i4.1
    IL_00de:  add
    IL_00df:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_00e4:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00e9:  nop
    IL_00ea:  ldloc.1
    IL_00eb:  ldloc.0
    IL_00ec:  ldstr      "[Field f5] [Type '{0}']"
    IL_00f1:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_00f6:  ldarg.0
    IL_00f7:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f5
    IL_00fc:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0101:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0106:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_010b:  nop
    IL_010c:  ldloc.1
    IL_010d:  ldarg.0
    IL_010e:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f5
    IL_0113:  ldarg.1
    IL_0114:  ldc.i4.1
    IL_0115:  add
    IL_0116:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_011b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0120:  nop
    IL_0121:  ldloc.1
    IL_0122:  ldloc.0
    IL_0123:  ldstr      "[Field f6] [Type '{0}']"
    IL_0128:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_012d:  ldarg.0
    IL_012e:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f6
    IL_0133:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0138:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_013d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0142:  nop
    IL_0143:  ldloc.1
    IL_0144:  ldarg.0
    IL_0145:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f6
    IL_014a:  ldarg.1
    IL_014b:  ldc.i4.1
    IL_014c:  add
    IL_014d:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_0152:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0157:  nop
    IL_0158:  ldloc.1
    IL_0159:  ldloc.0
    IL_015a:  ldstr      "[Field f7] [Type '{0}']"
    IL_015f:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0164:  ldarg.0
    IL_0165:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f7
    IL_016a:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_016f:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0174:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0179:  nop
    IL_017a:  ldloc.1
    IL_017b:  ldarg.0
    IL_017c:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f7
    IL_0181:  ldarg.1
    IL_0182:  ldc.i4.1
    IL_0183:  add
    IL_0184:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_0189:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_018e:  nop
    IL_018f:  ldloc.1
    IL_0190:  ldloc.0
    IL_0191:  ldstr      "[Field f8] [Type '{0}']"
    IL_0196:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_019b:  ldarg.0
    IL_019c:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f8
    IL_01a1:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_01a6:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_01ab:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_01b0:  nop
    IL_01b1:  ldloc.1
    IL_01b2:  ldarg.0
    IL_01b3:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f8
    IL_01b8:  ldarg.1
    IL_01b9:  ldc.i4.1
    IL_01ba:  add
    IL_01bb:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_01c0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01c5:  nop
    IL_01c6:  ldloc.1
    IL_01c7:  ldloc.0
    IL_01c8:  ldstr      "[Field f9] [Type '{0}']"
    IL_01cd:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_01d2:  ldarg.0
    IL_01d3:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f9
    IL_01d8:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_01dd:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_01e2:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_01e7:  nop
    IL_01e8:  ldloc.1
    IL_01e9:  ldarg.0
    IL_01ea:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f9
    IL_01ef:  ldarg.1
    IL_01f0:  ldc.i4.1
    IL_01f1:  add
    IL_01f2:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_01f7:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01fc:  nop
    IL_01fd:  ldloc.1
    IL_01fe:  ldloc.0
    IL_01ff:  ldstr      "[Field f10] [Type '{0}']"
    IL_0204:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0209:  ldarg.0
    IL_020a:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f10
    IL_020f:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0214:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0219:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_021e:  nop
    IL_021f:  ldloc.1
    IL_0220:  ldarg.0
    IL_0221:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f10
    IL_0226:  ldarg.1
    IL_0227:  ldc.i4.1
    IL_0228:  add
    IL_0229:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_022e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0233:  nop
    IL_0234:  ldloc.1
    IL_0235:  ldloc.0
    IL_0236:  ldstr      "[Field f11] [Type '{0}']"
    IL_023b:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0240:  ldarg.0
    IL_0241:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f11
    IL_0246:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_024b:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0250:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0255:  nop
    IL_0256:  ldloc.1
    IL_0257:  ldarg.0
    IL_0258:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f11
    IL_025d:  ldarg.1
    IL_025e:  ldc.i4.1
    IL_025f:  add
    IL_0260:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_0265:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_026a:  nop
    IL_026b:  ldloc.1
    IL_026c:  ldloc.0
    IL_026d:  ldstr      "[Field f12] [Type '{0}']"
    IL_0272:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0277:  ldarg.0
    IL_0278:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f12
    IL_027d:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0282:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0287:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_028c:  nop
    IL_028d:  ldloc.1
    IL_028e:  ldarg.0
    IL_028f:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f12
    IL_0294:  ldarg.1
    IL_0295:  ldc.i4.1
    IL_0296:  add
    IL_0297:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_029c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02a1:  nop
    IL_02a2:  ldloc.1
    IL_02a3:  ldloc.0
    IL_02a4:  ldstr      "[Field f13] [Type '{0}']"
    IL_02a9:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_02ae:  ldarg.0
    IL_02af:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f13
    IL_02b4:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_02b9:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_02be:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_02c3:  nop
    IL_02c4:  ldloc.1
    IL_02c5:  ldarg.0
    IL_02c6:  ldfld      class MCCTest.RType4 MCCTest.VTypeA::f13
    IL_02cb:  ldarg.1
    IL_02cc:  ldc.i4.1
    IL_02cd:  add
    IL_02ce:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_02d3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02d8:  nop
    IL_02d9:  ldloc.1
    IL_02da:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_02df:  stloc.2
    IL_02e0:  br.s       IL_02e2

    IL_02e2:  ldloc.2
    IL_02e3:  ret
  } // end of method VTypeA::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VTypeA::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VTypeA::Dump

} // end of class MCCTest.VTypeA

.class public sequential ansi sealed beforefieldinit MCCTest.VTypeB
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VTypeB>
{
  .field public valuetype MCCTest.VType5 f1
  .field public valuetype MCCTest.VType5 f2
  .field public valuetype MCCTest.VType5 f3
  .field public valuetype MCCTest.VType5 f4
  .field public valuetype MCCTest.VType5 f5
  .field public valuetype MCCTest.VType5 f6
  .field public valuetype MCCTest.VType5 f7
  .field public valuetype MCCTest.VType5 f8
  .field public valuetype MCCTest.VType5 f9
  .field public valuetype MCCTest.VType5 f10
  .field public valuetype MCCTest.VType5 f11
  .field public valuetype MCCTest.VType5 f12
  .field public valuetype MCCTest.VType5 f13
  .field public valuetype MCCTest.VType5 f14
  .field public valuetype MCCTest.VType5 f15
  .field public valuetype MCCTest.VType5 f16
  .field public valuetype MCCTest.VType5 f17
  .field public valuetype MCCTest.VType5 f18
  .field public valuetype MCCTest.VType5 f19
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       249 (0xf9)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f1
    IL_0007:  ldarg.1
    IL_0008:  call       instance void MCCTest.VType5::Init(int32)
    IL_000d:  nop
    IL_000e:  ldarg.0
    IL_000f:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f2
    IL_0014:  ldarg.1
    IL_0015:  call       instance void MCCTest.VType5::Init(int32)
    IL_001a:  nop
    IL_001b:  ldarg.0
    IL_001c:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f3
    IL_0021:  ldarg.1
    IL_0022:  call       instance void MCCTest.VType5::Init(int32)
    IL_0027:  nop
    IL_0028:  ldarg.0
    IL_0029:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f4
    IL_002e:  ldarg.1
    IL_002f:  call       instance void MCCTest.VType5::Init(int32)
    IL_0034:  nop
    IL_0035:  ldarg.0
    IL_0036:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f5
    IL_003b:  ldarg.1
    IL_003c:  call       instance void MCCTest.VType5::Init(int32)
    IL_0041:  nop
    IL_0042:  ldarg.0
    IL_0043:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f6
    IL_0048:  ldarg.1
    IL_0049:  call       instance void MCCTest.VType5::Init(int32)
    IL_004e:  nop
    IL_004f:  ldarg.0
    IL_0050:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f7
    IL_0055:  ldarg.1
    IL_0056:  call       instance void MCCTest.VType5::Init(int32)
    IL_005b:  nop
    IL_005c:  ldarg.0
    IL_005d:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f8
    IL_0062:  ldarg.1
    IL_0063:  call       instance void MCCTest.VType5::Init(int32)
    IL_0068:  nop
    IL_0069:  ldarg.0
    IL_006a:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f9
    IL_006f:  ldarg.1
    IL_0070:  call       instance void MCCTest.VType5::Init(int32)
    IL_0075:  nop
    IL_0076:  ldarg.0
    IL_0077:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f10
    IL_007c:  ldarg.1
    IL_007d:  call       instance void MCCTest.VType5::Init(int32)
    IL_0082:  nop
    IL_0083:  ldarg.0
    IL_0084:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f11
    IL_0089:  ldarg.1
    IL_008a:  call       instance void MCCTest.VType5::Init(int32)
    IL_008f:  nop
    IL_0090:  ldarg.0
    IL_0091:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f12
    IL_0096:  ldarg.1
    IL_0097:  call       instance void MCCTest.VType5::Init(int32)
    IL_009c:  nop
    IL_009d:  ldarg.0
    IL_009e:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f13
    IL_00a3:  ldarg.1
    IL_00a4:  call       instance void MCCTest.VType5::Init(int32)
    IL_00a9:  nop
    IL_00aa:  ldarg.0
    IL_00ab:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f14
    IL_00b0:  ldarg.1
    IL_00b1:  call       instance void MCCTest.VType5::Init(int32)
    IL_00b6:  nop
    IL_00b7:  ldarg.0
    IL_00b8:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f15
    IL_00bd:  ldarg.1
    IL_00be:  call       instance void MCCTest.VType5::Init(int32)
    IL_00c3:  nop
    IL_00c4:  ldarg.0
    IL_00c5:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f16
    IL_00ca:  ldarg.1
    IL_00cb:  call       instance void MCCTest.VType5::Init(int32)
    IL_00d0:  nop
    IL_00d1:  ldarg.0
    IL_00d2:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f17
    IL_00d7:  ldarg.1
    IL_00d8:  call       instance void MCCTest.VType5::Init(int32)
    IL_00dd:  nop
    IL_00de:  ldarg.0
    IL_00df:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f18
    IL_00e4:  ldarg.1
    IL_00e5:  call       instance void MCCTest.VType5::Init(int32)
    IL_00ea:  nop
    IL_00eb:  ldarg.0
    IL_00ec:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f19
    IL_00f1:  ldarg.1
    IL_00f2:  call       instance void MCCTest.VType5::Init(int32)
    IL_00f7:  nop
    IL_00f8:  ret
  } // end of method VTypeB::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VTypeB::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VTypeB::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VTypeB::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VTypeB::Zero

  .method public hidebysig instance void
          Add(valuetype MCCTest.VTypeB val) cil managed
  {
    // Code size       363 (0x16b)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f1
    IL_0007:  ldarga.s   val
    IL_0009:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f1
    IL_000e:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_0013:  nop
    IL_0014:  ldarg.0
    IL_0015:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f2
    IL_001a:  ldarga.s   val
    IL_001c:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f2
    IL_0021:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_0026:  nop
    IL_0027:  ldarg.0
    IL_0028:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f3
    IL_002d:  ldarga.s   val
    IL_002f:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f3
    IL_0034:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_0039:  nop
    IL_003a:  ldarg.0
    IL_003b:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f4
    IL_0040:  ldarga.s   val
    IL_0042:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f4
    IL_0047:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_004c:  nop
    IL_004d:  ldarg.0
    IL_004e:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f5
    IL_0053:  ldarga.s   val
    IL_0055:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f5
    IL_005a:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_005f:  nop
    IL_0060:  ldarg.0
    IL_0061:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f6
    IL_0066:  ldarga.s   val
    IL_0068:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f6
    IL_006d:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_0072:  nop
    IL_0073:  ldarg.0
    IL_0074:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f7
    IL_0079:  ldarga.s   val
    IL_007b:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f7
    IL_0080:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_0085:  nop
    IL_0086:  ldarg.0
    IL_0087:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f8
    IL_008c:  ldarga.s   val
    IL_008e:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f8
    IL_0093:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_0098:  nop
    IL_0099:  ldarg.0
    IL_009a:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f9
    IL_009f:  ldarga.s   val
    IL_00a1:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f9
    IL_00a6:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_00ab:  nop
    IL_00ac:  ldarg.0
    IL_00ad:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f10
    IL_00b2:  ldarga.s   val
    IL_00b4:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f10
    IL_00b9:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_00be:  nop
    IL_00bf:  ldarg.0
    IL_00c0:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f11
    IL_00c5:  ldarga.s   val
    IL_00c7:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f11
    IL_00cc:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_00d1:  nop
    IL_00d2:  ldarg.0
    IL_00d3:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f12
    IL_00d8:  ldarga.s   val
    IL_00da:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f12
    IL_00df:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_00e4:  nop
    IL_00e5:  ldarg.0
    IL_00e6:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f13
    IL_00eb:  ldarga.s   val
    IL_00ed:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f13
    IL_00f2:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_00f7:  nop
    IL_00f8:  ldarg.0
    IL_00f9:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f14
    IL_00fe:  ldarga.s   val
    IL_0100:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f14
    IL_0105:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_010a:  nop
    IL_010b:  ldarg.0
    IL_010c:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f15
    IL_0111:  ldarga.s   val
    IL_0113:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f15
    IL_0118:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_011d:  nop
    IL_011e:  ldarg.0
    IL_011f:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f16
    IL_0124:  ldarga.s   val
    IL_0126:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f16
    IL_012b:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_0130:  nop
    IL_0131:  ldarg.0
    IL_0132:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f17
    IL_0137:  ldarga.s   val
    IL_0139:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f17
    IL_013e:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_0143:  nop
    IL_0144:  ldarg.0
    IL_0145:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f18
    IL_014a:  ldarga.s   val
    IL_014c:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f18
    IL_0151:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_0156:  nop
    IL_0157:  ldarg.0
    IL_0158:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f19
    IL_015d:  ldarga.s   val
    IL_015f:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f19
    IL_0164:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_0169:  nop
    IL_016a:  ret
  } // end of method VTypeB::Add

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VTypeB expected) cil managed
  {
    // Code size       820 (0x334)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.Type V_1,
             class MCCTest.ResultVerificationException V_2)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  stloc.0
    IL_0003:  ldnull
    IL_0004:  stloc.1
    .try
    {
      IL_0005:  nop
      IL_0006:  ldstr      "f1"
      IL_000b:  stloc.0
      IL_000c:  ldarg.0
      IL_000d:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f1
      IL_0012:  box        MCCTest.VType5
      IL_0017:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_001c:  stloc.1
      IL_001d:  ldarg.0
      IL_001e:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f1
      IL_0023:  ldarga.s   expected
      IL_0025:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f1
      IL_002a:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_002f:  nop
      IL_0030:  ldstr      "f2"
      IL_0035:  stloc.0
      IL_0036:  ldarg.0
      IL_0037:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f2
      IL_003c:  box        MCCTest.VType5
      IL_0041:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0046:  stloc.1
      IL_0047:  ldarg.0
      IL_0048:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f2
      IL_004d:  ldarga.s   expected
      IL_004f:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f2
      IL_0054:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_0059:  nop
      IL_005a:  ldstr      "f3"
      IL_005f:  stloc.0
      IL_0060:  ldarg.0
      IL_0061:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f3
      IL_0066:  box        MCCTest.VType5
      IL_006b:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0070:  stloc.1
      IL_0071:  ldarg.0
      IL_0072:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f3
      IL_0077:  ldarga.s   expected
      IL_0079:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f3
      IL_007e:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_0083:  nop
      IL_0084:  ldstr      "f4"
      IL_0089:  stloc.0
      IL_008a:  ldarg.0
      IL_008b:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f4
      IL_0090:  box        MCCTest.VType5
      IL_0095:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_009a:  stloc.1
      IL_009b:  ldarg.0
      IL_009c:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f4
      IL_00a1:  ldarga.s   expected
      IL_00a3:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f4
      IL_00a8:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_00ad:  nop
      IL_00ae:  ldstr      "f5"
      IL_00b3:  stloc.0
      IL_00b4:  ldarg.0
      IL_00b5:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f5
      IL_00ba:  box        MCCTest.VType5
      IL_00bf:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_00c4:  stloc.1
      IL_00c5:  ldarg.0
      IL_00c6:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f5
      IL_00cb:  ldarga.s   expected
      IL_00cd:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f5
      IL_00d2:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_00d7:  nop
      IL_00d8:  ldstr      "f6"
      IL_00dd:  stloc.0
      IL_00de:  ldarg.0
      IL_00df:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f6
      IL_00e4:  box        MCCTest.VType5
      IL_00e9:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_00ee:  stloc.1
      IL_00ef:  ldarg.0
      IL_00f0:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f6
      IL_00f5:  ldarga.s   expected
      IL_00f7:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f6
      IL_00fc:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_0101:  nop
      IL_0102:  ldstr      "f7"
      IL_0107:  stloc.0
      IL_0108:  ldarg.0
      IL_0109:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f7
      IL_010e:  box        MCCTest.VType5
      IL_0113:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0118:  stloc.1
      IL_0119:  ldarg.0
      IL_011a:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f7
      IL_011f:  ldarga.s   expected
      IL_0121:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f7
      IL_0126:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_012b:  nop
      IL_012c:  ldstr      "f8"
      IL_0131:  stloc.0
      IL_0132:  ldarg.0
      IL_0133:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f8
      IL_0138:  box        MCCTest.VType5
      IL_013d:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0142:  stloc.1
      IL_0143:  ldarg.0
      IL_0144:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f8
      IL_0149:  ldarga.s   expected
      IL_014b:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f8
      IL_0150:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_0155:  nop
      IL_0156:  ldstr      "f9"
      IL_015b:  stloc.0
      IL_015c:  ldarg.0
      IL_015d:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f9
      IL_0162:  box        MCCTest.VType5
      IL_0167:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_016c:  stloc.1
      IL_016d:  ldarg.0
      IL_016e:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f9
      IL_0173:  ldarga.s   expected
      IL_0175:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f9
      IL_017a:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_017f:  nop
      IL_0180:  ldstr      "f10"
      IL_0185:  stloc.0
      IL_0186:  ldarg.0
      IL_0187:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f10
      IL_018c:  box        MCCTest.VType5
      IL_0191:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0196:  stloc.1
      IL_0197:  ldarg.0
      IL_0198:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f10
      IL_019d:  ldarga.s   expected
      IL_019f:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f10
      IL_01a4:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_01a9:  nop
      IL_01aa:  ldstr      "f11"
      IL_01af:  stloc.0
      IL_01b0:  ldarg.0
      IL_01b1:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f11
      IL_01b6:  box        MCCTest.VType5
      IL_01bb:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_01c0:  stloc.1
      IL_01c1:  ldarg.0
      IL_01c2:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f11
      IL_01c7:  ldarga.s   expected
      IL_01c9:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f11
      IL_01ce:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_01d3:  nop
      IL_01d4:  ldstr      "f12"
      IL_01d9:  stloc.0
      IL_01da:  ldarg.0
      IL_01db:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f12
      IL_01e0:  box        MCCTest.VType5
      IL_01e5:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_01ea:  stloc.1
      IL_01eb:  ldarg.0
      IL_01ec:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f12
      IL_01f1:  ldarga.s   expected
      IL_01f3:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f12
      IL_01f8:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_01fd:  nop
      IL_01fe:  ldstr      "f13"
      IL_0203:  stloc.0
      IL_0204:  ldarg.0
      IL_0205:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f13
      IL_020a:  box        MCCTest.VType5
      IL_020f:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0214:  stloc.1
      IL_0215:  ldarg.0
      IL_0216:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f13
      IL_021b:  ldarga.s   expected
      IL_021d:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f13
      IL_0222:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_0227:  nop
      IL_0228:  ldstr      "f14"
      IL_022d:  stloc.0
      IL_022e:  ldarg.0
      IL_022f:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f14
      IL_0234:  box        MCCTest.VType5
      IL_0239:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_023e:  stloc.1
      IL_023f:  ldarg.0
      IL_0240:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f14
      IL_0245:  ldarga.s   expected
      IL_0247:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f14
      IL_024c:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_0251:  nop
      IL_0252:  ldstr      "f15"
      IL_0257:  stloc.0
      IL_0258:  ldarg.0
      IL_0259:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f15
      IL_025e:  box        MCCTest.VType5
      IL_0263:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0268:  stloc.1
      IL_0269:  ldarg.0
      IL_026a:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f15
      IL_026f:  ldarga.s   expected
      IL_0271:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f15
      IL_0276:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_027b:  nop
      IL_027c:  ldstr      "f16"
      IL_0281:  stloc.0
      IL_0282:  ldarg.0
      IL_0283:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f16
      IL_0288:  box        MCCTest.VType5
      IL_028d:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0292:  stloc.1
      IL_0293:  ldarg.0
      IL_0294:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f16
      IL_0299:  ldarga.s   expected
      IL_029b:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f16
      IL_02a0:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_02a5:  nop
      IL_02a6:  ldstr      "f17"
      IL_02ab:  stloc.0
      IL_02ac:  ldarg.0
      IL_02ad:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f17
      IL_02b2:  box        MCCTest.VType5
      IL_02b7:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_02bc:  stloc.1
      IL_02bd:  ldarg.0
      IL_02be:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f17
      IL_02c3:  ldarga.s   expected
      IL_02c5:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f17
      IL_02ca:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_02cf:  nop
      IL_02d0:  ldstr      "f18"
      IL_02d5:  stloc.0
      IL_02d6:  ldarg.0
      IL_02d7:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f18
      IL_02dc:  box        MCCTest.VType5
      IL_02e1:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_02e6:  stloc.1
      IL_02e7:  ldarg.0
      IL_02e8:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f18
      IL_02ed:  ldarga.s   expected
      IL_02ef:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f18
      IL_02f4:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_02f9:  nop
      IL_02fa:  ldstr      "f19"
      IL_02ff:  stloc.0
      IL_0300:  ldarg.0
      IL_0301:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f19
      IL_0306:  box        MCCTest.VType5
      IL_030b:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0310:  stloc.1
      IL_0311:  ldarg.0
      IL_0312:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f19
      IL_0317:  ldarga.s   expected
      IL_0319:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f19
      IL_031e:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_0323:  nop
      IL_0324:  nop
      IL_0325:  leave.s    IL_0332

    }  // end .try
    catch MCCTest.ResultVerificationException
    {
      IL_0327:  stloc.2
      IL_0328:  nop
      IL_0329:  ldloc.0
      IL_032a:  ldloc.1
      IL_032b:  ldloc.2
      IL_032c:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                    class [mscorlib]System.Type,
                                                                                    class MCCTest.ResultVerificationException)
      IL_0331:  throw

    }  // end handler
    IL_0332:  nop
    IL_0333:  ret
  } // end of method VTypeB::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       1165 (0x48d)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "[Field f1] [Type '{0}']"
    IL_0015:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_001a:  ldarg.0
    IL_001b:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f1
    IL_0020:  box        MCCTest.VType5
    IL_0025:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_002a:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_002f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0034:  nop
    IL_0035:  ldloc.1
    IL_0036:  ldarg.0
    IL_0037:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f1
    IL_003c:  ldarg.1
    IL_003d:  ldc.i4.1
    IL_003e:  add
    IL_003f:  call       instance string MCCTest.VType5::Dump(int32)
    IL_0044:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0049:  nop
    IL_004a:  ldloc.1
    IL_004b:  ldloc.0
    IL_004c:  ldstr      "[Field f2] [Type '{0}']"
    IL_0051:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0056:  ldarg.0
    IL_0057:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f2
    IL_005c:  box        MCCTest.VType5
    IL_0061:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0066:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_006b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0070:  nop
    IL_0071:  ldloc.1
    IL_0072:  ldarg.0
    IL_0073:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f2
    IL_0078:  ldarg.1
    IL_0079:  ldc.i4.1
    IL_007a:  add
    IL_007b:  call       instance string MCCTest.VType5::Dump(int32)
    IL_0080:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0085:  nop
    IL_0086:  ldloc.1
    IL_0087:  ldloc.0
    IL_0088:  ldstr      "[Field f3] [Type '{0}']"
    IL_008d:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0092:  ldarg.0
    IL_0093:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f3
    IL_0098:  box        MCCTest.VType5
    IL_009d:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_00a2:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00a7:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_00ac:  nop
    IL_00ad:  ldloc.1
    IL_00ae:  ldarg.0
    IL_00af:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f3
    IL_00b4:  ldarg.1
    IL_00b5:  ldc.i4.1
    IL_00b6:  add
    IL_00b7:  call       instance string MCCTest.VType5::Dump(int32)
    IL_00bc:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00c1:  nop
    IL_00c2:  ldloc.1
    IL_00c3:  ldloc.0
    IL_00c4:  ldstr      "[Field f4] [Type '{0}']"
    IL_00c9:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_00ce:  ldarg.0
    IL_00cf:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f4
    IL_00d4:  box        MCCTest.VType5
    IL_00d9:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_00de:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00e3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_00e8:  nop
    IL_00e9:  ldloc.1
    IL_00ea:  ldarg.0
    IL_00eb:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f4
    IL_00f0:  ldarg.1
    IL_00f1:  ldc.i4.1
    IL_00f2:  add
    IL_00f3:  call       instance string MCCTest.VType5::Dump(int32)
    IL_00f8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00fd:  nop
    IL_00fe:  ldloc.1
    IL_00ff:  ldloc.0
    IL_0100:  ldstr      "[Field f5] [Type '{0}']"
    IL_0105:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_010a:  ldarg.0
    IL_010b:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f5
    IL_0110:  box        MCCTest.VType5
    IL_0115:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_011a:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_011f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0124:  nop
    IL_0125:  ldloc.1
    IL_0126:  ldarg.0
    IL_0127:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f5
    IL_012c:  ldarg.1
    IL_012d:  ldc.i4.1
    IL_012e:  add
    IL_012f:  call       instance string MCCTest.VType5::Dump(int32)
    IL_0134:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0139:  nop
    IL_013a:  ldloc.1
    IL_013b:  ldloc.0
    IL_013c:  ldstr      "[Field f6] [Type '{0}']"
    IL_0141:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0146:  ldarg.0
    IL_0147:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f6
    IL_014c:  box        MCCTest.VType5
    IL_0151:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0156:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_015b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0160:  nop
    IL_0161:  ldloc.1
    IL_0162:  ldarg.0
    IL_0163:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f6
    IL_0168:  ldarg.1
    IL_0169:  ldc.i4.1
    IL_016a:  add
    IL_016b:  call       instance string MCCTest.VType5::Dump(int32)
    IL_0170:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0175:  nop
    IL_0176:  ldloc.1
    IL_0177:  ldloc.0
    IL_0178:  ldstr      "[Field f7] [Type '{0}']"
    IL_017d:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0182:  ldarg.0
    IL_0183:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f7
    IL_0188:  box        MCCTest.VType5
    IL_018d:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0192:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0197:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_019c:  nop
    IL_019d:  ldloc.1
    IL_019e:  ldarg.0
    IL_019f:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f7
    IL_01a4:  ldarg.1
    IL_01a5:  ldc.i4.1
    IL_01a6:  add
    IL_01a7:  call       instance string MCCTest.VType5::Dump(int32)
    IL_01ac:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01b1:  nop
    IL_01b2:  ldloc.1
    IL_01b3:  ldloc.0
    IL_01b4:  ldstr      "[Field f8] [Type '{0}']"
    IL_01b9:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_01be:  ldarg.0
    IL_01bf:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f8
    IL_01c4:  box        MCCTest.VType5
    IL_01c9:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_01ce:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_01d3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_01d8:  nop
    IL_01d9:  ldloc.1
    IL_01da:  ldarg.0
    IL_01db:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f8
    IL_01e0:  ldarg.1
    IL_01e1:  ldc.i4.1
    IL_01e2:  add
    IL_01e3:  call       instance string MCCTest.VType5::Dump(int32)
    IL_01e8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01ed:  nop
    IL_01ee:  ldloc.1
    IL_01ef:  ldloc.0
    IL_01f0:  ldstr      "[Field f9] [Type '{0}']"
    IL_01f5:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_01fa:  ldarg.0
    IL_01fb:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f9
    IL_0200:  box        MCCTest.VType5
    IL_0205:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_020a:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_020f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0214:  nop
    IL_0215:  ldloc.1
    IL_0216:  ldarg.0
    IL_0217:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f9
    IL_021c:  ldarg.1
    IL_021d:  ldc.i4.1
    IL_021e:  add
    IL_021f:  call       instance string MCCTest.VType5::Dump(int32)
    IL_0224:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0229:  nop
    IL_022a:  ldloc.1
    IL_022b:  ldloc.0
    IL_022c:  ldstr      "[Field f10] [Type '{0}']"
    IL_0231:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0236:  ldarg.0
    IL_0237:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f10
    IL_023c:  box        MCCTest.VType5
    IL_0241:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0246:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_024b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0250:  nop
    IL_0251:  ldloc.1
    IL_0252:  ldarg.0
    IL_0253:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f10
    IL_0258:  ldarg.1
    IL_0259:  ldc.i4.1
    IL_025a:  add
    IL_025b:  call       instance string MCCTest.VType5::Dump(int32)
    IL_0260:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0265:  nop
    IL_0266:  ldloc.1
    IL_0267:  ldloc.0
    IL_0268:  ldstr      "[Field f11] [Type '{0}']"
    IL_026d:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0272:  ldarg.0
    IL_0273:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f11
    IL_0278:  box        MCCTest.VType5
    IL_027d:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0282:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0287:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_028c:  nop
    IL_028d:  ldloc.1
    IL_028e:  ldarg.0
    IL_028f:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f11
    IL_0294:  ldarg.1
    IL_0295:  ldc.i4.1
    IL_0296:  add
    IL_0297:  call       instance string MCCTest.VType5::Dump(int32)
    IL_029c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02a1:  nop
    IL_02a2:  ldloc.1
    IL_02a3:  ldloc.0
    IL_02a4:  ldstr      "[Field f12] [Type '{0}']"
    IL_02a9:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_02ae:  ldarg.0
    IL_02af:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f12
    IL_02b4:  box        MCCTest.VType5
    IL_02b9:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_02be:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_02c3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_02c8:  nop
    IL_02c9:  ldloc.1
    IL_02ca:  ldarg.0
    IL_02cb:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f12
    IL_02d0:  ldarg.1
    IL_02d1:  ldc.i4.1
    IL_02d2:  add
    IL_02d3:  call       instance string MCCTest.VType5::Dump(int32)
    IL_02d8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02dd:  nop
    IL_02de:  ldloc.1
    IL_02df:  ldloc.0
    IL_02e0:  ldstr      "[Field f13] [Type '{0}']"
    IL_02e5:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_02ea:  ldarg.0
    IL_02eb:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f13
    IL_02f0:  box        MCCTest.VType5
    IL_02f5:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_02fa:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_02ff:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0304:  nop
    IL_0305:  ldloc.1
    IL_0306:  ldarg.0
    IL_0307:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f13
    IL_030c:  ldarg.1
    IL_030d:  ldc.i4.1
    IL_030e:  add
    IL_030f:  call       instance string MCCTest.VType5::Dump(int32)
    IL_0314:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0319:  nop
    IL_031a:  ldloc.1
    IL_031b:  ldloc.0
    IL_031c:  ldstr      "[Field f14] [Type '{0}']"
    IL_0321:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0326:  ldarg.0
    IL_0327:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f14
    IL_032c:  box        MCCTest.VType5
    IL_0331:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0336:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_033b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0340:  nop
    IL_0341:  ldloc.1
    IL_0342:  ldarg.0
    IL_0343:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f14
    IL_0348:  ldarg.1
    IL_0349:  ldc.i4.1
    IL_034a:  add
    IL_034b:  call       instance string MCCTest.VType5::Dump(int32)
    IL_0350:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0355:  nop
    IL_0356:  ldloc.1
    IL_0357:  ldloc.0
    IL_0358:  ldstr      "[Field f15] [Type '{0}']"
    IL_035d:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0362:  ldarg.0
    IL_0363:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f15
    IL_0368:  box        MCCTest.VType5
    IL_036d:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0372:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0377:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_037c:  nop
    IL_037d:  ldloc.1
    IL_037e:  ldarg.0
    IL_037f:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f15
    IL_0384:  ldarg.1
    IL_0385:  ldc.i4.1
    IL_0386:  add
    IL_0387:  call       instance string MCCTest.VType5::Dump(int32)
    IL_038c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0391:  nop
    IL_0392:  ldloc.1
    IL_0393:  ldloc.0
    IL_0394:  ldstr      "[Field f16] [Type '{0}']"
    IL_0399:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_039e:  ldarg.0
    IL_039f:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f16
    IL_03a4:  box        MCCTest.VType5
    IL_03a9:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_03ae:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_03b3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_03b8:  nop
    IL_03b9:  ldloc.1
    IL_03ba:  ldarg.0
    IL_03bb:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f16
    IL_03c0:  ldarg.1
    IL_03c1:  ldc.i4.1
    IL_03c2:  add
    IL_03c3:  call       instance string MCCTest.VType5::Dump(int32)
    IL_03c8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_03cd:  nop
    IL_03ce:  ldloc.1
    IL_03cf:  ldloc.0
    IL_03d0:  ldstr      "[Field f17] [Type '{0}']"
    IL_03d5:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_03da:  ldarg.0
    IL_03db:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f17
    IL_03e0:  box        MCCTest.VType5
    IL_03e5:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_03ea:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_03ef:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_03f4:  nop
    IL_03f5:  ldloc.1
    IL_03f6:  ldarg.0
    IL_03f7:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f17
    IL_03fc:  ldarg.1
    IL_03fd:  ldc.i4.1
    IL_03fe:  add
    IL_03ff:  call       instance string MCCTest.VType5::Dump(int32)
    IL_0404:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0409:  nop
    IL_040a:  ldloc.1
    IL_040b:  ldloc.0
    IL_040c:  ldstr      "[Field f18] [Type '{0}']"
    IL_0411:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0416:  ldarg.0
    IL_0417:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f18
    IL_041c:  box        MCCTest.VType5
    IL_0421:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0426:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_042b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0430:  nop
    IL_0431:  ldloc.1
    IL_0432:  ldarg.0
    IL_0433:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f18
    IL_0438:  ldarg.1
    IL_0439:  ldc.i4.1
    IL_043a:  add
    IL_043b:  call       instance string MCCTest.VType5::Dump(int32)
    IL_0440:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0445:  nop
    IL_0446:  ldloc.1
    IL_0447:  ldloc.0
    IL_0448:  ldstr      "[Field f19] [Type '{0}']"
    IL_044d:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0452:  ldarg.0
    IL_0453:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeB::f19
    IL_0458:  box        MCCTest.VType5
    IL_045d:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0462:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0467:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_046c:  nop
    IL_046d:  ldloc.1
    IL_046e:  ldarg.0
    IL_046f:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeB::f19
    IL_0474:  ldarg.1
    IL_0475:  ldc.i4.1
    IL_0476:  add
    IL_0477:  call       instance string MCCTest.VType5::Dump(int32)
    IL_047c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0481:  nop
    IL_0482:  ldloc.1
    IL_0483:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0488:  stloc.2
    IL_0489:  br.s       IL_048b

    IL_048b:  ldloc.2
    IL_048c:  ret
  } // end of method VTypeB::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VTypeB::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VTypeB::Dump

} // end of class MCCTest.VTypeB

.class public sequential ansi sealed beforefieldinit MCCTest.VTypeC
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VTypeC>
{
  .field public class MCCTest.RType4 f1
  .field public valuetype MCCTest.VType5 f2
  .field public valuetype MCCTest.VType6 f3
  .field public class MCCTest.RType4 f4
  .field public valuetype MCCTest.VType5 f5
  .field public valuetype MCCTest.VType6 f6
  .field public class MCCTest.RType4 f7
  .field public valuetype MCCTest.VType5 f8
  .field public valuetype MCCTest.VType6 f9
  .field public class MCCTest.RType4 f10
  .field public valuetype MCCTest.VType5 f11
  .field public valuetype MCCTest.VType6 f12
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       202 (0xca)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0007:  stfld      class MCCTest.RType4 MCCTest.VTypeC::f1
    IL_000c:  ldarg.0
    IL_000d:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0012:  stfld      class MCCTest.RType4 MCCTest.VTypeC::f4
    IL_0017:  ldarg.0
    IL_0018:  newobj     instance void MCCTest.RType4::.ctor()
    IL_001d:  stfld      class MCCTest.RType4 MCCTest.VTypeC::f7
    IL_0022:  ldarg.0
    IL_0023:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0028:  stfld      class MCCTest.RType4 MCCTest.VTypeC::f10
    IL_002d:  ldarg.0
    IL_002e:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f1
    IL_0033:  ldarg.1
    IL_0034:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_0039:  nop
    IL_003a:  ldarg.0
    IL_003b:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f2
    IL_0040:  ldarg.1
    IL_0041:  call       instance void MCCTest.VType5::Init(int32)
    IL_0046:  nop
    IL_0047:  ldarg.0
    IL_0048:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f3
    IL_004d:  ldarg.1
    IL_004e:  call       instance void MCCTest.VType6::Init(int32)
    IL_0053:  nop
    IL_0054:  ldarg.0
    IL_0055:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f4
    IL_005a:  ldarg.1
    IL_005b:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_0060:  nop
    IL_0061:  ldarg.0
    IL_0062:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f5
    IL_0067:  ldarg.1
    IL_0068:  call       instance void MCCTest.VType5::Init(int32)
    IL_006d:  nop
    IL_006e:  ldarg.0
    IL_006f:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f6
    IL_0074:  ldarg.1
    IL_0075:  call       instance void MCCTest.VType6::Init(int32)
    IL_007a:  nop
    IL_007b:  ldarg.0
    IL_007c:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f7
    IL_0081:  ldarg.1
    IL_0082:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_0087:  nop
    IL_0088:  ldarg.0
    IL_0089:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f8
    IL_008e:  ldarg.1
    IL_008f:  call       instance void MCCTest.VType5::Init(int32)
    IL_0094:  nop
    IL_0095:  ldarg.0
    IL_0096:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f9
    IL_009b:  ldarg.1
    IL_009c:  call       instance void MCCTest.VType6::Init(int32)
    IL_00a1:  nop
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f10
    IL_00a8:  ldarg.1
    IL_00a9:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_00ae:  nop
    IL_00af:  ldarg.0
    IL_00b0:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f11
    IL_00b5:  ldarg.1
    IL_00b6:  call       instance void MCCTest.VType5::Init(int32)
    IL_00bb:  nop
    IL_00bc:  ldarg.0
    IL_00bd:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f12
    IL_00c2:  ldarg.1
    IL_00c3:  call       instance void MCCTest.VType6::Init(int32)
    IL_00c8:  nop
    IL_00c9:  ret
  } // end of method VTypeC::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VTypeC::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VTypeC::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VTypeC::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VTypeC::Zero

  .method public hidebysig instance void
          Add(valuetype MCCTest.VTypeC val) cil managed
  {
    // Code size       230 (0xe6)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f1
    IL_0007:  ldarga.s   val
    IL_0009:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f1
    IL_000e:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_0013:  nop
    IL_0014:  ldarg.0
    IL_0015:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f2
    IL_001a:  ldarga.s   val
    IL_001c:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f2
    IL_0021:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_0026:  nop
    IL_0027:  ldarg.0
    IL_0028:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f3
    IL_002d:  ldarga.s   val
    IL_002f:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f3
    IL_0034:  call       instance void MCCTest.VType6::Add(valuetype MCCTest.VType6)
    IL_0039:  nop
    IL_003a:  ldarg.0
    IL_003b:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f4
    IL_0040:  ldarga.s   val
    IL_0042:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f4
    IL_0047:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_004c:  nop
    IL_004d:  ldarg.0
    IL_004e:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f5
    IL_0053:  ldarga.s   val
    IL_0055:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f5
    IL_005a:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_005f:  nop
    IL_0060:  ldarg.0
    IL_0061:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f6
    IL_0066:  ldarga.s   val
    IL_0068:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f6
    IL_006d:  call       instance void MCCTest.VType6::Add(valuetype MCCTest.VType6)
    IL_0072:  nop
    IL_0073:  ldarg.0
    IL_0074:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f7
    IL_0079:  ldarga.s   val
    IL_007b:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f7
    IL_0080:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_0085:  nop
    IL_0086:  ldarg.0
    IL_0087:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f8
    IL_008c:  ldarga.s   val
    IL_008e:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f8
    IL_0093:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_0098:  nop
    IL_0099:  ldarg.0
    IL_009a:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f9
    IL_009f:  ldarga.s   val
    IL_00a1:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f9
    IL_00a6:  call       instance void MCCTest.VType6::Add(valuetype MCCTest.VType6)
    IL_00ab:  nop
    IL_00ac:  ldarg.0
    IL_00ad:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f10
    IL_00b2:  ldarga.s   val
    IL_00b4:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f10
    IL_00b9:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_00be:  nop
    IL_00bf:  ldarg.0
    IL_00c0:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f11
    IL_00c5:  ldarga.s   val
    IL_00c7:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f11
    IL_00cc:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_00d1:  nop
    IL_00d2:  ldarg.0
    IL_00d3:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f12
    IL_00d8:  ldarga.s   val
    IL_00da:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f12
    IL_00df:  call       instance void MCCTest.VType6::Add(valuetype MCCTest.VType6)
    IL_00e4:  nop
    IL_00e5:  ret
  } // end of method VTypeC::Add

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VTypeC expected) cil managed
  {
    // Code size       506 (0x1fa)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.Type V_1,
             class MCCTest.ResultVerificationException V_2)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  stloc.0
    IL_0003:  ldnull
    IL_0004:  stloc.1
    .try
    {
      IL_0005:  nop
      IL_0006:  ldstr      "f1"
      IL_000b:  stloc.0
      IL_000c:  ldarg.0
      IL_000d:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f1
      IL_0012:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0017:  stloc.1
      IL_0018:  ldarg.0
      IL_0019:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f1
      IL_001e:  ldarga.s   expected
      IL_0020:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f1
      IL_0025:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_002a:  nop
      IL_002b:  ldstr      "f2"
      IL_0030:  stloc.0
      IL_0031:  ldarg.0
      IL_0032:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f2
      IL_0037:  box        MCCTest.VType5
      IL_003c:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0041:  stloc.1
      IL_0042:  ldarg.0
      IL_0043:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f2
      IL_0048:  ldarga.s   expected
      IL_004a:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f2
      IL_004f:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_0054:  nop
      IL_0055:  ldstr      "f3"
      IL_005a:  stloc.0
      IL_005b:  ldarg.0
      IL_005c:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f3
      IL_0061:  box        MCCTest.VType6
      IL_0066:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_006b:  stloc.1
      IL_006c:  ldarg.0
      IL_006d:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f3
      IL_0072:  ldarga.s   expected
      IL_0074:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f3
      IL_0079:  call       instance void MCCTest.VType6::Check(valuetype MCCTest.VType6)
      IL_007e:  nop
      IL_007f:  ldstr      "f4"
      IL_0084:  stloc.0
      IL_0085:  ldarg.0
      IL_0086:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f4
      IL_008b:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0090:  stloc.1
      IL_0091:  ldarg.0
      IL_0092:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f4
      IL_0097:  ldarga.s   expected
      IL_0099:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f4
      IL_009e:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_00a3:  nop
      IL_00a4:  ldstr      "f5"
      IL_00a9:  stloc.0
      IL_00aa:  ldarg.0
      IL_00ab:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f5
      IL_00b0:  box        MCCTest.VType5
      IL_00b5:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_00ba:  stloc.1
      IL_00bb:  ldarg.0
      IL_00bc:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f5
      IL_00c1:  ldarga.s   expected
      IL_00c3:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f5
      IL_00c8:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_00cd:  nop
      IL_00ce:  ldstr      "f6"
      IL_00d3:  stloc.0
      IL_00d4:  ldarg.0
      IL_00d5:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f6
      IL_00da:  box        MCCTest.VType6
      IL_00df:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_00e4:  stloc.1
      IL_00e5:  ldarg.0
      IL_00e6:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f6
      IL_00eb:  ldarga.s   expected
      IL_00ed:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f6
      IL_00f2:  call       instance void MCCTest.VType6::Check(valuetype MCCTest.VType6)
      IL_00f7:  nop
      IL_00f8:  ldstr      "f7"
      IL_00fd:  stloc.0
      IL_00fe:  ldarg.0
      IL_00ff:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f7
      IL_0104:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0109:  stloc.1
      IL_010a:  ldarg.0
      IL_010b:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f7
      IL_0110:  ldarga.s   expected
      IL_0112:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f7
      IL_0117:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_011c:  nop
      IL_011d:  ldstr      "f8"
      IL_0122:  stloc.0
      IL_0123:  ldarg.0
      IL_0124:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f8
      IL_0129:  box        MCCTest.VType5
      IL_012e:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0133:  stloc.1
      IL_0134:  ldarg.0
      IL_0135:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f8
      IL_013a:  ldarga.s   expected
      IL_013c:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f8
      IL_0141:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_0146:  nop
      IL_0147:  ldstr      "f9"
      IL_014c:  stloc.0
      IL_014d:  ldarg.0
      IL_014e:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f9
      IL_0153:  box        MCCTest.VType6
      IL_0158:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_015d:  stloc.1
      IL_015e:  ldarg.0
      IL_015f:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f9
      IL_0164:  ldarga.s   expected
      IL_0166:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f9
      IL_016b:  call       instance void MCCTest.VType6::Check(valuetype MCCTest.VType6)
      IL_0170:  nop
      IL_0171:  ldstr      "f10"
      IL_0176:  stloc.0
      IL_0177:  ldarg.0
      IL_0178:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f10
      IL_017d:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0182:  stloc.1
      IL_0183:  ldarg.0
      IL_0184:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f10
      IL_0189:  ldarga.s   expected
      IL_018b:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f10
      IL_0190:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_0195:  nop
      IL_0196:  ldstr      "f11"
      IL_019b:  stloc.0
      IL_019c:  ldarg.0
      IL_019d:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f11
      IL_01a2:  box        MCCTest.VType5
      IL_01a7:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_01ac:  stloc.1
      IL_01ad:  ldarg.0
      IL_01ae:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f11
      IL_01b3:  ldarga.s   expected
      IL_01b5:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f11
      IL_01ba:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_01bf:  nop
      IL_01c0:  ldstr      "f12"
      IL_01c5:  stloc.0
      IL_01c6:  ldarg.0
      IL_01c7:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f12
      IL_01cc:  box        MCCTest.VType6
      IL_01d1:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_01d6:  stloc.1
      IL_01d7:  ldarg.0
      IL_01d8:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f12
      IL_01dd:  ldarga.s   expected
      IL_01df:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f12
      IL_01e4:  call       instance void MCCTest.VType6::Check(valuetype MCCTest.VType6)
      IL_01e9:  nop
      IL_01ea:  nop
      IL_01eb:  leave.s    IL_01f8

    }  // end .try
    catch MCCTest.ResultVerificationException
    {
      IL_01ed:  stloc.2
      IL_01ee:  nop
      IL_01ef:  ldloc.0
      IL_01f0:  ldloc.1
      IL_01f1:  ldloc.2
      IL_01f2:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                    class [mscorlib]System.Type,
                                                                                    class MCCTest.ResultVerificationException)
      IL_01f7:  throw

    }  // end handler
    IL_01f8:  nop
    IL_01f9:  ret
  } // end of method VTypeC::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       725 (0x2d5)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "[Field f1] [Type '{0}']"
    IL_0015:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_001a:  ldarg.0
    IL_001b:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f1
    IL_0020:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0025:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_002a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_002f:  nop
    IL_0030:  ldloc.1
    IL_0031:  ldarg.0
    IL_0032:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f1
    IL_0037:  ldarg.1
    IL_0038:  ldc.i4.1
    IL_0039:  add
    IL_003a:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_003f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0044:  nop
    IL_0045:  ldloc.1
    IL_0046:  ldloc.0
    IL_0047:  ldstr      "[Field f2] [Type '{0}']"
    IL_004c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0051:  ldarg.0
    IL_0052:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f2
    IL_0057:  box        MCCTest.VType5
    IL_005c:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0061:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0066:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_006b:  nop
    IL_006c:  ldloc.1
    IL_006d:  ldarg.0
    IL_006e:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f2
    IL_0073:  ldarg.1
    IL_0074:  ldc.i4.1
    IL_0075:  add
    IL_0076:  call       instance string MCCTest.VType5::Dump(int32)
    IL_007b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0080:  nop
    IL_0081:  ldloc.1
    IL_0082:  ldloc.0
    IL_0083:  ldstr      "[Field f3] [Type '{0}']"
    IL_0088:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008d:  ldarg.0
    IL_008e:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f3
    IL_0093:  box        MCCTest.VType6
    IL_0098:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_009d:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00a2:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_00a7:  nop
    IL_00a8:  ldloc.1
    IL_00a9:  ldarg.0
    IL_00aa:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f3
    IL_00af:  ldarg.1
    IL_00b0:  ldc.i4.1
    IL_00b1:  add
    IL_00b2:  call       instance string MCCTest.VType6::Dump(int32)
    IL_00b7:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00bc:  nop
    IL_00bd:  ldloc.1
    IL_00be:  ldloc.0
    IL_00bf:  ldstr      "[Field f4] [Type '{0}']"
    IL_00c4:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_00c9:  ldarg.0
    IL_00ca:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f4
    IL_00cf:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_00d4:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00d9:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_00de:  nop
    IL_00df:  ldloc.1
    IL_00e0:  ldarg.0
    IL_00e1:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f4
    IL_00e6:  ldarg.1
    IL_00e7:  ldc.i4.1
    IL_00e8:  add
    IL_00e9:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_00ee:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00f3:  nop
    IL_00f4:  ldloc.1
    IL_00f5:  ldloc.0
    IL_00f6:  ldstr      "[Field f5] [Type '{0}']"
    IL_00fb:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0100:  ldarg.0
    IL_0101:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f5
    IL_0106:  box        MCCTest.VType5
    IL_010b:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0110:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0115:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_011a:  nop
    IL_011b:  ldloc.1
    IL_011c:  ldarg.0
    IL_011d:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f5
    IL_0122:  ldarg.1
    IL_0123:  ldc.i4.1
    IL_0124:  add
    IL_0125:  call       instance string MCCTest.VType5::Dump(int32)
    IL_012a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_012f:  nop
    IL_0130:  ldloc.1
    IL_0131:  ldloc.0
    IL_0132:  ldstr      "[Field f6] [Type '{0}']"
    IL_0137:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_013c:  ldarg.0
    IL_013d:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f6
    IL_0142:  box        MCCTest.VType6
    IL_0147:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_014c:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0151:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0156:  nop
    IL_0157:  ldloc.1
    IL_0158:  ldarg.0
    IL_0159:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f6
    IL_015e:  ldarg.1
    IL_015f:  ldc.i4.1
    IL_0160:  add
    IL_0161:  call       instance string MCCTest.VType6::Dump(int32)
    IL_0166:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_016b:  nop
    IL_016c:  ldloc.1
    IL_016d:  ldloc.0
    IL_016e:  ldstr      "[Field f7] [Type '{0}']"
    IL_0173:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0178:  ldarg.0
    IL_0179:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f7
    IL_017e:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0183:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0188:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_018d:  nop
    IL_018e:  ldloc.1
    IL_018f:  ldarg.0
    IL_0190:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f7
    IL_0195:  ldarg.1
    IL_0196:  ldc.i4.1
    IL_0197:  add
    IL_0198:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_019d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01a2:  nop
    IL_01a3:  ldloc.1
    IL_01a4:  ldloc.0
    IL_01a5:  ldstr      "[Field f8] [Type '{0}']"
    IL_01aa:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_01af:  ldarg.0
    IL_01b0:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f8
    IL_01b5:  box        MCCTest.VType5
    IL_01ba:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_01bf:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_01c4:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_01c9:  nop
    IL_01ca:  ldloc.1
    IL_01cb:  ldarg.0
    IL_01cc:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f8
    IL_01d1:  ldarg.1
    IL_01d2:  ldc.i4.1
    IL_01d3:  add
    IL_01d4:  call       instance string MCCTest.VType5::Dump(int32)
    IL_01d9:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01de:  nop
    IL_01df:  ldloc.1
    IL_01e0:  ldloc.0
    IL_01e1:  ldstr      "[Field f9] [Type '{0}']"
    IL_01e6:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_01eb:  ldarg.0
    IL_01ec:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f9
    IL_01f1:  box        MCCTest.VType6
    IL_01f6:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_01fb:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0200:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0205:  nop
    IL_0206:  ldloc.1
    IL_0207:  ldarg.0
    IL_0208:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f9
    IL_020d:  ldarg.1
    IL_020e:  ldc.i4.1
    IL_020f:  add
    IL_0210:  call       instance string MCCTest.VType6::Dump(int32)
    IL_0215:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_021a:  nop
    IL_021b:  ldloc.1
    IL_021c:  ldloc.0
    IL_021d:  ldstr      "[Field f10] [Type '{0}']"
    IL_0222:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0227:  ldarg.0
    IL_0228:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f10
    IL_022d:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0232:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0237:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_023c:  nop
    IL_023d:  ldloc.1
    IL_023e:  ldarg.0
    IL_023f:  ldfld      class MCCTest.RType4 MCCTest.VTypeC::f10
    IL_0244:  ldarg.1
    IL_0245:  ldc.i4.1
    IL_0246:  add
    IL_0247:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_024c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0251:  nop
    IL_0252:  ldloc.1
    IL_0253:  ldloc.0
    IL_0254:  ldstr      "[Field f11] [Type '{0}']"
    IL_0259:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_025e:  ldarg.0
    IL_025f:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeC::f11
    IL_0264:  box        MCCTest.VType5
    IL_0269:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_026e:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0273:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0278:  nop
    IL_0279:  ldloc.1
    IL_027a:  ldarg.0
    IL_027b:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeC::f11
    IL_0280:  ldarg.1
    IL_0281:  ldc.i4.1
    IL_0282:  add
    IL_0283:  call       instance string MCCTest.VType5::Dump(int32)
    IL_0288:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_028d:  nop
    IL_028e:  ldloc.1
    IL_028f:  ldloc.0
    IL_0290:  ldstr      "[Field f12] [Type '{0}']"
    IL_0295:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_029a:  ldarg.0
    IL_029b:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeC::f12
    IL_02a0:  box        MCCTest.VType6
    IL_02a5:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_02aa:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_02af:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_02b4:  nop
    IL_02b5:  ldloc.1
    IL_02b6:  ldarg.0
    IL_02b7:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeC::f12
    IL_02bc:  ldarg.1
    IL_02bd:  ldc.i4.1
    IL_02be:  add
    IL_02bf:  call       instance string MCCTest.VType6::Dump(int32)
    IL_02c4:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02c9:  nop
    IL_02ca:  ldloc.1
    IL_02cb:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_02d0:  stloc.2
    IL_02d1:  br.s       IL_02d3

    IL_02d3:  ldloc.2
    IL_02d4:  ret
  } // end of method VTypeC::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VTypeC::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VTypeC::Dump

} // end of class MCCTest.VTypeC

.class public sequential ansi sealed beforefieldinit MCCTest.VTypeD
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VTypeD>
{
  .field public int32 f1
  .field public valuetype MCCTest.VType3 f2
  .field public float64 f3
  .field public class MCCTest.RType4 f4
  .field public valuetype MCCTest.VType7 f5
  .field public uint64 f6
  .field public float32 f7
  .field public class MCCTest.RType4 f8
  .field public valuetype MCCTest.VType6 f9
  .field public float64 f10
  .field public int16 f11
  .field public class MCCTest.RType4 f12
  .field public valuetype MCCTest.VType5 f13
  .field public valuetype MCCTest.VType3 f14
  .field public class MCCTest.RType4 f15
  .field public valuetype MCCTest.VType7 f16
  .field public uint32 f17
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       230 (0xe6)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0007:  stfld      class MCCTest.RType4 MCCTest.VTypeD::f4
    IL_000c:  ldarg.0
    IL_000d:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0012:  stfld      class MCCTest.RType4 MCCTest.VTypeD::f8
    IL_0017:  ldarg.0
    IL_0018:  newobj     instance void MCCTest.RType4::.ctor()
    IL_001d:  stfld      class MCCTest.RType4 MCCTest.VTypeD::f12
    IL_0022:  ldarg.0
    IL_0023:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0028:  stfld      class MCCTest.RType4 MCCTest.VTypeD::f15
    IL_002d:  ldarg.0
    IL_002e:  ldarg.1
    IL_002f:  stfld      int32 MCCTest.VTypeD::f1
    IL_0034:  ldarg.0
    IL_0035:  ldflda     valuetype MCCTest.VType3 MCCTest.VTypeD::f2
    IL_003a:  ldarg.1
    IL_003b:  call       instance void MCCTest.VType3::Init(int32)
    IL_0040:  nop
    IL_0041:  ldarg.0
    IL_0042:  ldarg.1
    IL_0043:  conv.r8
    IL_0044:  stfld      float64 MCCTest.VTypeD::f3
    IL_0049:  ldarg.0
    IL_004a:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f4
    IL_004f:  ldarg.1
    IL_0050:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_0055:  nop
    IL_0056:  ldarg.0
    IL_0057:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeD::f5
    IL_005c:  ldarg.1
    IL_005d:  call       instance void MCCTest.VType7::Init(int32)
    IL_0062:  nop
    IL_0063:  ldarg.0
    IL_0064:  ldarg.1
    IL_0065:  conv.i8
    IL_0066:  stfld      uint64 MCCTest.VTypeD::f6
    IL_006b:  ldarg.0
    IL_006c:  ldarg.1
    IL_006d:  conv.r4
    IL_006e:  stfld      float32 MCCTest.VTypeD::f7
    IL_0073:  ldarg.0
    IL_0074:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f8
    IL_0079:  ldarg.1
    IL_007a:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_007f:  nop
    IL_0080:  ldarg.0
    IL_0081:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeD::f9
    IL_0086:  ldarg.1
    IL_0087:  call       instance void MCCTest.VType6::Init(int32)
    IL_008c:  nop
    IL_008d:  ldarg.0
    IL_008e:  ldarg.1
    IL_008f:  conv.r8
    IL_0090:  stfld      float64 MCCTest.VTypeD::f10
    IL_0095:  ldarg.0
    IL_0096:  ldarg.1
    IL_0097:  conv.i2
    IL_0098:  stfld      int16 MCCTest.VTypeD::f11
    IL_009d:  ldarg.0
    IL_009e:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f12
    IL_00a3:  ldarg.1
    IL_00a4:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_00a9:  nop
    IL_00aa:  ldarg.0
    IL_00ab:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeD::f13
    IL_00b0:  ldarg.1
    IL_00b1:  call       instance void MCCTest.VType5::Init(int32)
    IL_00b6:  nop
    IL_00b7:  ldarg.0
    IL_00b8:  ldflda     valuetype MCCTest.VType3 MCCTest.VTypeD::f14
    IL_00bd:  ldarg.1
    IL_00be:  call       instance void MCCTest.VType3::Init(int32)
    IL_00c3:  nop
    IL_00c4:  ldarg.0
    IL_00c5:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f15
    IL_00ca:  ldarg.1
    IL_00cb:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_00d0:  nop
    IL_00d1:  ldarg.0
    IL_00d2:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeD::f16
    IL_00d7:  ldarg.1
    IL_00d8:  call       instance void MCCTest.VType7::Init(int32)
    IL_00dd:  nop
    IL_00de:  ldarg.0
    IL_00df:  ldarg.1
    IL_00e0:  stfld      uint32 MCCTest.VTypeD::f17
    IL_00e5:  ret
  } // end of method VTypeD::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VTypeD::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VTypeD::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VTypeD::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VTypeD::Zero

  .method public hidebysig instance void
          Add(valuetype MCCTest.VTypeD val) cil managed
  {
    // Code size       333 (0x14d)
    .maxstack  3
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 MCCTest.VTypeD::f1
    IL_0008:  ldarga.s   val
    IL_000a:  ldfld      int32 MCCTest.VTypeD::f1
    IL_000f:  add
    IL_0010:  stfld      int32 MCCTest.VTypeD::f1
    IL_0015:  ldarg.0
    IL_0016:  ldflda     valuetype MCCTest.VType3 MCCTest.VTypeD::f2
    IL_001b:  ldarga.s   val
    IL_001d:  ldfld      valuetype MCCTest.VType3 MCCTest.VTypeD::f2
    IL_0022:  call       instance void MCCTest.VType3::Add(valuetype MCCTest.VType3)
    IL_0027:  nop
    IL_0028:  ldarg.0
    IL_0029:  dup
    IL_002a:  ldfld      float64 MCCTest.VTypeD::f3
    IL_002f:  ldarga.s   val
    IL_0031:  ldfld      float64 MCCTest.VTypeD::f3
    IL_0036:  add
    IL_0037:  stfld      float64 MCCTest.VTypeD::f3
    IL_003c:  ldarg.0
    IL_003d:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f4
    IL_0042:  ldarga.s   val
    IL_0044:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f4
    IL_0049:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_004e:  nop
    IL_004f:  ldarg.0
    IL_0050:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeD::f5
    IL_0055:  ldarga.s   val
    IL_0057:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeD::f5
    IL_005c:  call       instance void MCCTest.VType7::Add(valuetype MCCTest.VType7)
    IL_0061:  nop
    IL_0062:  ldarg.0
    IL_0063:  dup
    IL_0064:  ldfld      uint64 MCCTest.VTypeD::f6
    IL_0069:  ldarga.s   val
    IL_006b:  ldfld      uint64 MCCTest.VTypeD::f6
    IL_0070:  add
    IL_0071:  stfld      uint64 MCCTest.VTypeD::f6
    IL_0076:  ldarg.0
    IL_0077:  dup
    IL_0078:  ldfld      float32 MCCTest.VTypeD::f7
    IL_007d:  ldarga.s   val
    IL_007f:  ldfld      float32 MCCTest.VTypeD::f7
    IL_0084:  add
    IL_0085:  stfld      float32 MCCTest.VTypeD::f7
    IL_008a:  ldarg.0
    IL_008b:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f8
    IL_0090:  ldarga.s   val
    IL_0092:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f8
    IL_0097:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_009c:  nop
    IL_009d:  ldarg.0
    IL_009e:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeD::f9
    IL_00a3:  ldarga.s   val
    IL_00a5:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeD::f9
    IL_00aa:  call       instance void MCCTest.VType6::Add(valuetype MCCTest.VType6)
    IL_00af:  nop
    IL_00b0:  ldarg.0
    IL_00b1:  dup
    IL_00b2:  ldfld      float64 MCCTest.VTypeD::f10
    IL_00b7:  ldarga.s   val
    IL_00b9:  ldfld      float64 MCCTest.VTypeD::f10
    IL_00be:  add
    IL_00bf:  stfld      float64 MCCTest.VTypeD::f10
    IL_00c4:  ldarg.0
    IL_00c5:  dup
    IL_00c6:  ldfld      int16 MCCTest.VTypeD::f11
    IL_00cb:  ldarga.s   val
    IL_00cd:  ldfld      int16 MCCTest.VTypeD::f11
    IL_00d2:  add
    IL_00d3:  conv.i2
    IL_00d4:  stfld      int16 MCCTest.VTypeD::f11
    IL_00d9:  ldarg.0
    IL_00da:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f12
    IL_00df:  ldarga.s   val
    IL_00e1:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f12
    IL_00e6:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_00eb:  nop
    IL_00ec:  ldarg.0
    IL_00ed:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeD::f13
    IL_00f2:  ldarga.s   val
    IL_00f4:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeD::f13
    IL_00f9:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_00fe:  nop
    IL_00ff:  ldarg.0
    IL_0100:  ldflda     valuetype MCCTest.VType3 MCCTest.VTypeD::f14
    IL_0105:  ldarga.s   val
    IL_0107:  ldfld      valuetype MCCTest.VType3 MCCTest.VTypeD::f14
    IL_010c:  call       instance void MCCTest.VType3::Add(valuetype MCCTest.VType3)
    IL_0111:  nop
    IL_0112:  ldarg.0
    IL_0113:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f15
    IL_0118:  ldarga.s   val
    IL_011a:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f15
    IL_011f:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_0124:  nop
    IL_0125:  ldarg.0
    IL_0126:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeD::f16
    IL_012b:  ldarga.s   val
    IL_012d:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeD::f16
    IL_0132:  call       instance void MCCTest.VType7::Add(valuetype MCCTest.VType7)
    IL_0137:  nop
    IL_0138:  ldarg.0
    IL_0139:  dup
    IL_013a:  ldfld      uint32 MCCTest.VTypeD::f17
    IL_013f:  ldarga.s   val
    IL_0141:  ldfld      uint32 MCCTest.VTypeD::f17
    IL_0146:  add
    IL_0147:  stfld      uint32 MCCTest.VTypeD::f17
    IL_014c:  ret
  } // end of method VTypeD::Add

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VTypeD expected) cil managed
  {
    // Code size       742 (0x2e6)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.Type V_1,
             class MCCTest.ResultVerificationException V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      int32 MCCTest.VTypeD::f1
    IL_0007:  ldarga.s   expected
    IL_0009:  ldfld      int32 MCCTest.VTypeD::f1
    IL_000e:  ceq
    IL_0010:  stloc.3
    IL_0011:  ldloc.3
    IL_0012:  brtrue.s   IL_002f

    IL_0014:  nop
    IL_0015:  ldstr      "f1"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      int32 MCCTest.VTypeD::f1
    IL_0020:  conv.i8
    IL_0021:  ldarga.s   expected
    IL_0023:  ldfld      int32 MCCTest.VTypeD::f1
    IL_0028:  conv.i8
    IL_0029:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_002e:  throw

    IL_002f:  ldarg.0
    IL_0030:  ldfld      float64 MCCTest.VTypeD::f3
    IL_0035:  ldarga.s   expected
    IL_0037:  ldfld      float64 MCCTest.VTypeD::f3
    IL_003c:  ceq
    IL_003e:  stloc.3
    IL_003f:  ldloc.3
    IL_0040:  brtrue.s   IL_005b

    IL_0042:  nop
    IL_0043:  ldstr      "f3"
    IL_0048:  ldarg.0
    IL_0049:  ldfld      float64 MCCTest.VTypeD::f3
    IL_004e:  ldarga.s   expected
    IL_0050:  ldfld      float64 MCCTest.VTypeD::f3
    IL_0055:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_005a:  throw

    IL_005b:  ldarg.0
    IL_005c:  ldfld      uint64 MCCTest.VTypeD::f6
    IL_0061:  ldarga.s   expected
    IL_0063:  ldfld      uint64 MCCTest.VTypeD::f6
    IL_0068:  ceq
    IL_006a:  stloc.3
    IL_006b:  ldloc.3
    IL_006c:  brtrue.s   IL_008b

    IL_006e:  nop
    IL_006f:  ldstr      "f6"
    IL_0074:  ldarg.0
    IL_0075:  ldfld      uint64 MCCTest.VTypeD::f6
    IL_007a:  conv.r.un
    IL_007b:  conv.r8
    IL_007c:  ldarga.s   expected
    IL_007e:  ldfld      uint64 MCCTest.VTypeD::f6
    IL_0083:  conv.r.un
    IL_0084:  conv.r8
    IL_0085:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_008a:  throw

    IL_008b:  ldarg.0
    IL_008c:  ldfld      float32 MCCTest.VTypeD::f7
    IL_0091:  ldarga.s   expected
    IL_0093:  ldfld      float32 MCCTest.VTypeD::f7
    IL_0098:  ceq
    IL_009a:  stloc.3
    IL_009b:  ldloc.3
    IL_009c:  brtrue.s   IL_00b9

    IL_009e:  nop
    IL_009f:  ldstr      "f7"
    IL_00a4:  ldarg.0
    IL_00a5:  ldfld      float32 MCCTest.VTypeD::f7
    IL_00aa:  conv.r8
    IL_00ab:  ldarga.s   expected
    IL_00ad:  ldfld      float32 MCCTest.VTypeD::f7
    IL_00b2:  conv.r8
    IL_00b3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00b8:  throw

    IL_00b9:  ldarg.0
    IL_00ba:  ldfld      float64 MCCTest.VTypeD::f10
    IL_00bf:  ldarga.s   expected
    IL_00c1:  ldfld      float64 MCCTest.VTypeD::f10
    IL_00c6:  ceq
    IL_00c8:  stloc.3
    IL_00c9:  ldloc.3
    IL_00ca:  brtrue.s   IL_00e5

    IL_00cc:  nop
    IL_00cd:  ldstr      "f10"
    IL_00d2:  ldarg.0
    IL_00d3:  ldfld      float64 MCCTest.VTypeD::f10
    IL_00d8:  ldarga.s   expected
    IL_00da:  ldfld      float64 MCCTest.VTypeD::f10
    IL_00df:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00e4:  throw

    IL_00e5:  ldarg.0
    IL_00e6:  ldfld      int16 MCCTest.VTypeD::f11
    IL_00eb:  ldarga.s   expected
    IL_00ed:  ldfld      int16 MCCTest.VTypeD::f11
    IL_00f2:  ceq
    IL_00f4:  stloc.3
    IL_00f5:  ldloc.3
    IL_00f6:  brtrue.s   IL_0113

    IL_00f8:  nop
    IL_00f9:  ldstr      "f11"
    IL_00fe:  ldarg.0
    IL_00ff:  ldfld      int16 MCCTest.VTypeD::f11
    IL_0104:  conv.i8
    IL_0105:  ldarga.s   expected
    IL_0107:  ldfld      int16 MCCTest.VTypeD::f11
    IL_010c:  conv.i8
    IL_010d:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_0112:  throw

    IL_0113:  ldarg.0
    IL_0114:  ldfld      uint32 MCCTest.VTypeD::f17
    IL_0119:  ldarga.s   expected
    IL_011b:  ldfld      uint32 MCCTest.VTypeD::f17
    IL_0120:  ceq
    IL_0122:  stloc.3
    IL_0123:  ldloc.3
    IL_0124:  brtrue.s   IL_0141

    IL_0126:  nop
    IL_0127:  ldstr      "f17"
    IL_012c:  ldarg.0
    IL_012d:  ldfld      uint32 MCCTest.VTypeD::f17
    IL_0132:  conv.u8
    IL_0133:  ldarga.s   expected
    IL_0135:  ldfld      uint32 MCCTest.VTypeD::f17
    IL_013a:  conv.u8
    IL_013b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_0140:  throw

    IL_0141:  ldnull
    IL_0142:  stloc.0
    IL_0143:  ldnull
    IL_0144:  stloc.1
    .try
    {
      IL_0145:  nop
      IL_0146:  ldstr      "f2"
      IL_014b:  stloc.0
      IL_014c:  ldarg.0
      IL_014d:  ldfld      valuetype MCCTest.VType3 MCCTest.VTypeD::f2
      IL_0152:  box        MCCTest.VType3
      IL_0157:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_015c:  stloc.1
      IL_015d:  ldarg.0
      IL_015e:  ldflda     valuetype MCCTest.VType3 MCCTest.VTypeD::f2
      IL_0163:  ldarga.s   expected
      IL_0165:  ldfld      valuetype MCCTest.VType3 MCCTest.VTypeD::f2
      IL_016a:  call       instance void MCCTest.VType3::Check(valuetype MCCTest.VType3)
      IL_016f:  nop
      IL_0170:  ldstr      "f4"
      IL_0175:  stloc.0
      IL_0176:  ldarg.0
      IL_0177:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f4
      IL_017c:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0181:  stloc.1
      IL_0182:  ldarg.0
      IL_0183:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f4
      IL_0188:  ldarga.s   expected
      IL_018a:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f4
      IL_018f:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_0194:  nop
      IL_0195:  ldstr      "f5"
      IL_019a:  stloc.0
      IL_019b:  ldarg.0
      IL_019c:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeD::f5
      IL_01a1:  box        MCCTest.VType7
      IL_01a6:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_01ab:  stloc.1
      IL_01ac:  ldarg.0
      IL_01ad:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeD::f5
      IL_01b2:  ldarga.s   expected
      IL_01b4:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeD::f5
      IL_01b9:  call       instance void MCCTest.VType7::Check(valuetype MCCTest.VType7)
      IL_01be:  nop
      IL_01bf:  ldstr      "f8"
      IL_01c4:  stloc.0
      IL_01c5:  ldarg.0
      IL_01c6:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f8
      IL_01cb:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_01d0:  stloc.1
      IL_01d1:  ldarg.0
      IL_01d2:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f8
      IL_01d7:  ldarga.s   expected
      IL_01d9:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f8
      IL_01de:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_01e3:  nop
      IL_01e4:  ldstr      "f9"
      IL_01e9:  stloc.0
      IL_01ea:  ldarg.0
      IL_01eb:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeD::f9
      IL_01f0:  box        MCCTest.VType6
      IL_01f5:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_01fa:  stloc.1
      IL_01fb:  ldarg.0
      IL_01fc:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeD::f9
      IL_0201:  ldarga.s   expected
      IL_0203:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeD::f9
      IL_0208:  call       instance void MCCTest.VType6::Check(valuetype MCCTest.VType6)
      IL_020d:  nop
      IL_020e:  ldstr      "f12"
      IL_0213:  stloc.0
      IL_0214:  ldarg.0
      IL_0215:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f12
      IL_021a:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_021f:  stloc.1
      IL_0220:  ldarg.0
      IL_0221:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f12
      IL_0226:  ldarga.s   expected
      IL_0228:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f12
      IL_022d:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_0232:  nop
      IL_0233:  ldstr      "f13"
      IL_0238:  stloc.0
      IL_0239:  ldarg.0
      IL_023a:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeD::f13
      IL_023f:  box        MCCTest.VType5
      IL_0244:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0249:  stloc.1
      IL_024a:  ldarg.0
      IL_024b:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeD::f13
      IL_0250:  ldarga.s   expected
      IL_0252:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeD::f13
      IL_0257:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_025c:  nop
      IL_025d:  ldstr      "f14"
      IL_0262:  stloc.0
      IL_0263:  ldarg.0
      IL_0264:  ldfld      valuetype MCCTest.VType3 MCCTest.VTypeD::f14
      IL_0269:  box        MCCTest.VType3
      IL_026e:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0273:  stloc.1
      IL_0274:  ldarg.0
      IL_0275:  ldflda     valuetype MCCTest.VType3 MCCTest.VTypeD::f14
      IL_027a:  ldarga.s   expected
      IL_027c:  ldfld      valuetype MCCTest.VType3 MCCTest.VTypeD::f14
      IL_0281:  call       instance void MCCTest.VType3::Check(valuetype MCCTest.VType3)
      IL_0286:  nop
      IL_0287:  ldstr      "f15"
      IL_028c:  stloc.0
      IL_028d:  ldarg.0
      IL_028e:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f15
      IL_0293:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0298:  stloc.1
      IL_0299:  ldarg.0
      IL_029a:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f15
      IL_029f:  ldarga.s   expected
      IL_02a1:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f15
      IL_02a6:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_02ab:  nop
      IL_02ac:  ldstr      "f16"
      IL_02b1:  stloc.0
      IL_02b2:  ldarg.0
      IL_02b3:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeD::f16
      IL_02b8:  box        MCCTest.VType7
      IL_02bd:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_02c2:  stloc.1
      IL_02c3:  ldarg.0
      IL_02c4:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeD::f16
      IL_02c9:  ldarga.s   expected
      IL_02cb:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeD::f16
      IL_02d0:  call       instance void MCCTest.VType7::Check(valuetype MCCTest.VType7)
      IL_02d5:  nop
      IL_02d6:  nop
      IL_02d7:  leave.s    IL_02e4

    }  // end .try
    catch MCCTest.ResultVerificationException
    {
      IL_02d9:  stloc.2
      IL_02da:  nop
      IL_02db:  ldloc.0
      IL_02dc:  ldloc.1
      IL_02dd:  ldloc.2
      IL_02de:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                    class [mscorlib]System.Type,
                                                                                    class MCCTest.ResultVerificationException)
      IL_02e3:  throw

    }  // end handler
    IL_02e4:  nop
    IL_02e5:  ret
  } // end of method VTypeD::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       808 (0x328)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "f1    = "
    IL_0015:  ldarg.0
    IL_0016:  ldfld      int32 MCCTest.VTypeD::f1
    IL_001b:  box        [mscorlib]System.Int32
    IL_0020:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0025:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_002a:  nop
    IL_002b:  ldloc.1
    IL_002c:  ldloc.0
    IL_002d:  ldstr      "[Field f2] [Type '{0}']"
    IL_0032:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0037:  ldarg.0
    IL_0038:  ldfld      valuetype MCCTest.VType3 MCCTest.VTypeD::f2
    IL_003d:  box        MCCTest.VType3
    IL_0042:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0047:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_004c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0051:  nop
    IL_0052:  ldloc.1
    IL_0053:  ldarg.0
    IL_0054:  ldflda     valuetype MCCTest.VType3 MCCTest.VTypeD::f2
    IL_0059:  ldarg.1
    IL_005a:  ldc.i4.1
    IL_005b:  add
    IL_005c:  call       instance string MCCTest.VType3::Dump(int32)
    IL_0061:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0066:  nop
    IL_0067:  ldloc.1
    IL_0068:  ldloc.0
    IL_0069:  ldstr      "f3    = "
    IL_006e:  ldarg.0
    IL_006f:  ldfld      float64 MCCTest.VTypeD::f3
    IL_0074:  box        [mscorlib]System.Double
    IL_0079:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_007e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0083:  nop
    IL_0084:  ldloc.1
    IL_0085:  ldloc.0
    IL_0086:  ldstr      "[Field f4] [Type '{0}']"
    IL_008b:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0090:  ldarg.0
    IL_0091:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f4
    IL_0096:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_009b:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00a0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_00a5:  nop
    IL_00a6:  ldloc.1
    IL_00a7:  ldarg.0
    IL_00a8:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f4
    IL_00ad:  ldarg.1
    IL_00ae:  ldc.i4.1
    IL_00af:  add
    IL_00b0:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_00b5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00ba:  nop
    IL_00bb:  ldloc.1
    IL_00bc:  ldloc.0
    IL_00bd:  ldstr      "[Field f5] [Type '{0}']"
    IL_00c2:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_00c7:  ldarg.0
    IL_00c8:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeD::f5
    IL_00cd:  box        MCCTest.VType7
    IL_00d2:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_00d7:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00dc:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_00e1:  nop
    IL_00e2:  ldloc.1
    IL_00e3:  ldarg.0
    IL_00e4:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeD::f5
    IL_00e9:  ldarg.1
    IL_00ea:  ldc.i4.1
    IL_00eb:  add
    IL_00ec:  call       instance string MCCTest.VType7::Dump(int32)
    IL_00f1:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00f6:  nop
    IL_00f7:  ldloc.1
    IL_00f8:  ldloc.0
    IL_00f9:  ldstr      "f6    = "
    IL_00fe:  ldarg.0
    IL_00ff:  ldfld      uint64 MCCTest.VTypeD::f6
    IL_0104:  box        [mscorlib]System.UInt64
    IL_0109:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_010e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0113:  nop
    IL_0114:  ldloc.1
    IL_0115:  ldloc.0
    IL_0116:  ldstr      "f7    = "
    IL_011b:  ldarg.0
    IL_011c:  ldfld      float32 MCCTest.VTypeD::f7
    IL_0121:  box        [mscorlib]System.Single
    IL_0126:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_012b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0130:  nop
    IL_0131:  ldloc.1
    IL_0132:  ldloc.0
    IL_0133:  ldstr      "[Field f8] [Type '{0}']"
    IL_0138:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_013d:  ldarg.0
    IL_013e:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f8
    IL_0143:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0148:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_014d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0152:  nop
    IL_0153:  ldloc.1
    IL_0154:  ldarg.0
    IL_0155:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f8
    IL_015a:  ldarg.1
    IL_015b:  ldc.i4.1
    IL_015c:  add
    IL_015d:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_0162:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0167:  nop
    IL_0168:  ldloc.1
    IL_0169:  ldloc.0
    IL_016a:  ldstr      "[Field f9] [Type '{0}']"
    IL_016f:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0174:  ldarg.0
    IL_0175:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeD::f9
    IL_017a:  box        MCCTest.VType6
    IL_017f:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0184:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0189:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_018e:  nop
    IL_018f:  ldloc.1
    IL_0190:  ldarg.0
    IL_0191:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeD::f9
    IL_0196:  ldarg.1
    IL_0197:  ldc.i4.1
    IL_0198:  add
    IL_0199:  call       instance string MCCTest.VType6::Dump(int32)
    IL_019e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01a3:  nop
    IL_01a4:  ldloc.1
    IL_01a5:  ldloc.0
    IL_01a6:  ldstr      "f10    = "
    IL_01ab:  ldarg.0
    IL_01ac:  ldfld      float64 MCCTest.VTypeD::f10
    IL_01b1:  box        [mscorlib]System.Double
    IL_01b6:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01bb:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01c0:  nop
    IL_01c1:  ldloc.1
    IL_01c2:  ldloc.0
    IL_01c3:  ldstr      "f11    = "
    IL_01c8:  ldarg.0
    IL_01c9:  ldfld      int16 MCCTest.VTypeD::f11
    IL_01ce:  box        [mscorlib]System.Int16
    IL_01d3:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01d8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01dd:  nop
    IL_01de:  ldloc.1
    IL_01df:  ldloc.0
    IL_01e0:  ldstr      "[Field f12] [Type '{0}']"
    IL_01e5:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_01ea:  ldarg.0
    IL_01eb:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f12
    IL_01f0:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_01f5:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_01fa:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_01ff:  nop
    IL_0200:  ldloc.1
    IL_0201:  ldarg.0
    IL_0202:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f12
    IL_0207:  ldarg.1
    IL_0208:  ldc.i4.1
    IL_0209:  add
    IL_020a:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_020f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0214:  nop
    IL_0215:  ldloc.1
    IL_0216:  ldloc.0
    IL_0217:  ldstr      "[Field f13] [Type '{0}']"
    IL_021c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0221:  ldarg.0
    IL_0222:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeD::f13
    IL_0227:  box        MCCTest.VType5
    IL_022c:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0231:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0236:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_023b:  nop
    IL_023c:  ldloc.1
    IL_023d:  ldarg.0
    IL_023e:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeD::f13
    IL_0243:  ldarg.1
    IL_0244:  ldc.i4.1
    IL_0245:  add
    IL_0246:  call       instance string MCCTest.VType5::Dump(int32)
    IL_024b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0250:  nop
    IL_0251:  ldloc.1
    IL_0252:  ldloc.0
    IL_0253:  ldstr      "[Field f14] [Type '{0}']"
    IL_0258:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_025d:  ldarg.0
    IL_025e:  ldfld      valuetype MCCTest.VType3 MCCTest.VTypeD::f14
    IL_0263:  box        MCCTest.VType3
    IL_0268:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_026d:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0272:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0277:  nop
    IL_0278:  ldloc.1
    IL_0279:  ldarg.0
    IL_027a:  ldflda     valuetype MCCTest.VType3 MCCTest.VTypeD::f14
    IL_027f:  ldarg.1
    IL_0280:  ldc.i4.1
    IL_0281:  add
    IL_0282:  call       instance string MCCTest.VType3::Dump(int32)
    IL_0287:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_028c:  nop
    IL_028d:  ldloc.1
    IL_028e:  ldloc.0
    IL_028f:  ldstr      "[Field f15] [Type '{0}']"
    IL_0294:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0299:  ldarg.0
    IL_029a:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f15
    IL_029f:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_02a4:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_02a9:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_02ae:  nop
    IL_02af:  ldloc.1
    IL_02b0:  ldarg.0
    IL_02b1:  ldfld      class MCCTest.RType4 MCCTest.VTypeD::f15
    IL_02b6:  ldarg.1
    IL_02b7:  ldc.i4.1
    IL_02b8:  add
    IL_02b9:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_02be:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02c3:  nop
    IL_02c4:  ldloc.1
    IL_02c5:  ldloc.0
    IL_02c6:  ldstr      "[Field f16] [Type '{0}']"
    IL_02cb:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_02d0:  ldarg.0
    IL_02d1:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeD::f16
    IL_02d6:  box        MCCTest.VType7
    IL_02db:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_02e0:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_02e5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_02ea:  nop
    IL_02eb:  ldloc.1
    IL_02ec:  ldarg.0
    IL_02ed:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeD::f16
    IL_02f2:  ldarg.1
    IL_02f3:  ldc.i4.1
    IL_02f4:  add
    IL_02f5:  call       instance string MCCTest.VType7::Dump(int32)
    IL_02fa:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02ff:  nop
    IL_0300:  ldloc.1
    IL_0301:  ldloc.0
    IL_0302:  ldstr      "f17    = "
    IL_0307:  ldarg.0
    IL_0308:  ldfld      uint32 MCCTest.VTypeD::f17
    IL_030d:  box        [mscorlib]System.UInt32
    IL_0312:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0317:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_031c:  nop
    IL_031d:  ldloc.1
    IL_031e:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0323:  stloc.2
    IL_0324:  br.s       IL_0326

    IL_0326:  ldloc.2
    IL_0327:  ret
  } // end of method VTypeD::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VTypeD::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VTypeD::Dump

} // end of class MCCTest.VTypeD

.class public sequential ansi sealed beforefieldinit MCCTest.VTypeE
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VTypeE>
{
  .field public valuetype MCCTest.VType9 f1
  .field public class MCCTest.RType4 f2
  .field public valuetype MCCTest.VType8 f3
  .field public int16 f4
  .field public valuetype MCCTest.VType8 f5
  .field public valuetype MCCTest.VType8 f6
  .field public float64 f7
  .field public valuetype MCCTest.VTypeA f8
  .field public int16 f9
  .field public valuetype MCCTest.VTypeD f10
  .field public valuetype MCCTest.VType8 f11
  .field public valuetype MCCTest.VTypeC f12
  .field public valuetype MCCTest.VTypeA f13
  .field public float32 f14
  .field public int32 f15
  .field public valuetype MCCTest.VTypeC f16
  .field public valuetype MCCTest.VType7 f17
  .field public float64 f18
  .field public uint64 f19
  .field public valuetype MCCTest.VType8 f20
  .field public valuetype MCCTest.VType9 f21
  .field public valuetype MCCTest.VTypeC f22
  .field public valuetype MCCTest.VType8 f23
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       276 (0x114)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0007:  stfld      class MCCTest.RType4 MCCTest.VTypeE::f2
    IL_000c:  ldarg.0
    IL_000d:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeE::f1
    IL_0012:  ldarg.1
    IL_0013:  call       instance void MCCTest.VType9::Init(int32)
    IL_0018:  nop
    IL_0019:  ldarg.0
    IL_001a:  ldfld      class MCCTest.RType4 MCCTest.VTypeE::f2
    IL_001f:  ldarg.1
    IL_0020:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_0025:  nop
    IL_0026:  ldarg.0
    IL_0027:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f3
    IL_002c:  ldarg.1
    IL_002d:  call       instance void MCCTest.VType8::Init(int32)
    IL_0032:  nop
    IL_0033:  ldarg.0
    IL_0034:  ldarg.1
    IL_0035:  conv.i2
    IL_0036:  stfld      int16 MCCTest.VTypeE::f4
    IL_003b:  ldarg.0
    IL_003c:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f5
    IL_0041:  ldarg.1
    IL_0042:  call       instance void MCCTest.VType8::Init(int32)
    IL_0047:  nop
    IL_0048:  ldarg.0
    IL_0049:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f6
    IL_004e:  ldarg.1
    IL_004f:  call       instance void MCCTest.VType8::Init(int32)
    IL_0054:  nop
    IL_0055:  ldarg.0
    IL_0056:  ldarg.1
    IL_0057:  conv.r8
    IL_0058:  stfld      float64 MCCTest.VTypeE::f7
    IL_005d:  ldarg.0
    IL_005e:  ldflda     valuetype MCCTest.VTypeA MCCTest.VTypeE::f8
    IL_0063:  ldarg.1
    IL_0064:  call       instance void MCCTest.VTypeA::Init(int32)
    IL_0069:  nop
    IL_006a:  ldarg.0
    IL_006b:  ldarg.1
    IL_006c:  conv.i2
    IL_006d:  stfld      int16 MCCTest.VTypeE::f9
    IL_0072:  ldarg.0
    IL_0073:  ldflda     valuetype MCCTest.VTypeD MCCTest.VTypeE::f10
    IL_0078:  ldarg.1
    IL_0079:  call       instance void MCCTest.VTypeD::Init(int32)
    IL_007e:  nop
    IL_007f:  ldarg.0
    IL_0080:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f11
    IL_0085:  ldarg.1
    IL_0086:  call       instance void MCCTest.VType8::Init(int32)
    IL_008b:  nop
    IL_008c:  ldarg.0
    IL_008d:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeE::f12
    IL_0092:  ldarg.1
    IL_0093:  call       instance void MCCTest.VTypeC::Init(int32)
    IL_0098:  nop
    IL_0099:  ldarg.0
    IL_009a:  ldflda     valuetype MCCTest.VTypeA MCCTest.VTypeE::f13
    IL_009f:  ldarg.1
    IL_00a0:  call       instance void MCCTest.VTypeA::Init(int32)
    IL_00a5:  nop
    IL_00a6:  ldarg.0
    IL_00a7:  ldarg.1
    IL_00a8:  conv.r4
    IL_00a9:  stfld      float32 MCCTest.VTypeE::f14
    IL_00ae:  ldarg.0
    IL_00af:  ldarg.1
    IL_00b0:  stfld      int32 MCCTest.VTypeE::f15
    IL_00b5:  ldarg.0
    IL_00b6:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeE::f16
    IL_00bb:  ldarg.1
    IL_00bc:  call       instance void MCCTest.VTypeC::Init(int32)
    IL_00c1:  nop
    IL_00c2:  ldarg.0
    IL_00c3:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeE::f17
    IL_00c8:  ldarg.1
    IL_00c9:  call       instance void MCCTest.VType7::Init(int32)
    IL_00ce:  nop
    IL_00cf:  ldarg.0
    IL_00d0:  ldarg.1
    IL_00d1:  conv.r8
    IL_00d2:  stfld      float64 MCCTest.VTypeE::f18
    IL_00d7:  ldarg.0
    IL_00d8:  ldarg.1
    IL_00d9:  conv.i8
    IL_00da:  stfld      uint64 MCCTest.VTypeE::f19
    IL_00df:  ldarg.0
    IL_00e0:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f20
    IL_00e5:  ldarg.1
    IL_00e6:  call       instance void MCCTest.VType8::Init(int32)
    IL_00eb:  nop
    IL_00ec:  ldarg.0
    IL_00ed:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeE::f21
    IL_00f2:  ldarg.1
    IL_00f3:  call       instance void MCCTest.VType9::Init(int32)
    IL_00f8:  nop
    IL_00f9:  ldarg.0
    IL_00fa:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeE::f22
    IL_00ff:  ldarg.1
    IL_0100:  call       instance void MCCTest.VTypeC::Init(int32)
    IL_0105:  nop
    IL_0106:  ldarg.0
    IL_0107:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f23
    IL_010c:  ldarg.1
    IL_010d:  call       instance void MCCTest.VType8::Init(int32)
    IL_0112:  nop
    IL_0113:  ret
  } // end of method VTypeE::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VTypeE::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VTypeE::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VTypeE::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VTypeE::Zero

  .method public hidebysig instance void
          Add(valuetype MCCTest.VTypeE val) cil managed
  {
    // Code size       448 (0x1c0)
    .maxstack  3
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeE::f1
    IL_0007:  ldarga.s   val
    IL_0009:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeE::f1
    IL_000e:  call       instance void MCCTest.VType9::Add(valuetype MCCTest.VType9)
    IL_0013:  nop
    IL_0014:  ldarg.0
    IL_0015:  ldfld      class MCCTest.RType4 MCCTest.VTypeE::f2
    IL_001a:  ldarga.s   val
    IL_001c:  ldfld      class MCCTest.RType4 MCCTest.VTypeE::f2
    IL_0021:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_0026:  nop
    IL_0027:  ldarg.0
    IL_0028:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f3
    IL_002d:  ldarga.s   val
    IL_002f:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f3
    IL_0034:  call       instance void MCCTest.VType8::Add(valuetype MCCTest.VType8)
    IL_0039:  nop
    IL_003a:  ldarg.0
    IL_003b:  dup
    IL_003c:  ldfld      int16 MCCTest.VTypeE::f4
    IL_0041:  ldarga.s   val
    IL_0043:  ldfld      int16 MCCTest.VTypeE::f4
    IL_0048:  add
    IL_0049:  conv.i2
    IL_004a:  stfld      int16 MCCTest.VTypeE::f4
    IL_004f:  ldarg.0
    IL_0050:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f5
    IL_0055:  ldarga.s   val
    IL_0057:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f5
    IL_005c:  call       instance void MCCTest.VType8::Add(valuetype MCCTest.VType8)
    IL_0061:  nop
    IL_0062:  ldarg.0
    IL_0063:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f6
    IL_0068:  ldarga.s   val
    IL_006a:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f6
    IL_006f:  call       instance void MCCTest.VType8::Add(valuetype MCCTest.VType8)
    IL_0074:  nop
    IL_0075:  ldarg.0
    IL_0076:  dup
    IL_0077:  ldfld      float64 MCCTest.VTypeE::f7
    IL_007c:  ldarga.s   val
    IL_007e:  ldfld      float64 MCCTest.VTypeE::f7
    IL_0083:  add
    IL_0084:  stfld      float64 MCCTest.VTypeE::f7
    IL_0089:  ldarg.0
    IL_008a:  ldflda     valuetype MCCTest.VTypeA MCCTest.VTypeE::f8
    IL_008f:  ldarga.s   val
    IL_0091:  ldfld      valuetype MCCTest.VTypeA MCCTest.VTypeE::f8
    IL_0096:  call       instance void MCCTest.VTypeA::Add(valuetype MCCTest.VTypeA)
    IL_009b:  nop
    IL_009c:  ldarg.0
    IL_009d:  dup
    IL_009e:  ldfld      int16 MCCTest.VTypeE::f9
    IL_00a3:  ldarga.s   val
    IL_00a5:  ldfld      int16 MCCTest.VTypeE::f9
    IL_00aa:  add
    IL_00ab:  conv.i2
    IL_00ac:  stfld      int16 MCCTest.VTypeE::f9
    IL_00b1:  ldarg.0
    IL_00b2:  ldflda     valuetype MCCTest.VTypeD MCCTest.VTypeE::f10
    IL_00b7:  ldarga.s   val
    IL_00b9:  ldfld      valuetype MCCTest.VTypeD MCCTest.VTypeE::f10
    IL_00be:  call       instance void MCCTest.VTypeD::Add(valuetype MCCTest.VTypeD)
    IL_00c3:  nop
    IL_00c4:  ldarg.0
    IL_00c5:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f11
    IL_00ca:  ldarga.s   val
    IL_00cc:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f11
    IL_00d1:  call       instance void MCCTest.VType8::Add(valuetype MCCTest.VType8)
    IL_00d6:  nop
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeE::f12
    IL_00dd:  ldarga.s   val
    IL_00df:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeE::f12
    IL_00e4:  call       instance void MCCTest.VTypeC::Add(valuetype MCCTest.VTypeC)
    IL_00e9:  nop
    IL_00ea:  ldarg.0
    IL_00eb:  ldflda     valuetype MCCTest.VTypeA MCCTest.VTypeE::f13
    IL_00f0:  ldarga.s   val
    IL_00f2:  ldfld      valuetype MCCTest.VTypeA MCCTest.VTypeE::f13
    IL_00f7:  call       instance void MCCTest.VTypeA::Add(valuetype MCCTest.VTypeA)
    IL_00fc:  nop
    IL_00fd:  ldarg.0
    IL_00fe:  dup
    IL_00ff:  ldfld      float32 MCCTest.VTypeE::f14
    IL_0104:  ldarga.s   val
    IL_0106:  ldfld      float32 MCCTest.VTypeE::f14
    IL_010b:  add
    IL_010c:  stfld      float32 MCCTest.VTypeE::f14
    IL_0111:  ldarg.0
    IL_0112:  dup
    IL_0113:  ldfld      int32 MCCTest.VTypeE::f15
    IL_0118:  ldarga.s   val
    IL_011a:  ldfld      int32 MCCTest.VTypeE::f15
    IL_011f:  add
    IL_0120:  stfld      int32 MCCTest.VTypeE::f15
    IL_0125:  ldarg.0
    IL_0126:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeE::f16
    IL_012b:  ldarga.s   val
    IL_012d:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeE::f16
    IL_0132:  call       instance void MCCTest.VTypeC::Add(valuetype MCCTest.VTypeC)
    IL_0137:  nop
    IL_0138:  ldarg.0
    IL_0139:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeE::f17
    IL_013e:  ldarga.s   val
    IL_0140:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeE::f17
    IL_0145:  call       instance void MCCTest.VType7::Add(valuetype MCCTest.VType7)
    IL_014a:  nop
    IL_014b:  ldarg.0
    IL_014c:  dup
    IL_014d:  ldfld      float64 MCCTest.VTypeE::f18
    IL_0152:  ldarga.s   val
    IL_0154:  ldfld      float64 MCCTest.VTypeE::f18
    IL_0159:  add
    IL_015a:  stfld      float64 MCCTest.VTypeE::f18
    IL_015f:  ldarg.0
    IL_0160:  dup
    IL_0161:  ldfld      uint64 MCCTest.VTypeE::f19
    IL_0166:  ldarga.s   val
    IL_0168:  ldfld      uint64 MCCTest.VTypeE::f19
    IL_016d:  add
    IL_016e:  stfld      uint64 MCCTest.VTypeE::f19
    IL_0173:  ldarg.0
    IL_0174:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f20
    IL_0179:  ldarga.s   val
    IL_017b:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f20
    IL_0180:  call       instance void MCCTest.VType8::Add(valuetype MCCTest.VType8)
    IL_0185:  nop
    IL_0186:  ldarg.0
    IL_0187:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeE::f21
    IL_018c:  ldarga.s   val
    IL_018e:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeE::f21
    IL_0193:  call       instance void MCCTest.VType9::Add(valuetype MCCTest.VType9)
    IL_0198:  nop
    IL_0199:  ldarg.0
    IL_019a:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeE::f22
    IL_019f:  ldarga.s   val
    IL_01a1:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeE::f22
    IL_01a6:  call       instance void MCCTest.VTypeC::Add(valuetype MCCTest.VTypeC)
    IL_01ab:  nop
    IL_01ac:  ldarg.0
    IL_01ad:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f23
    IL_01b2:  ldarga.s   val
    IL_01b4:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f23
    IL_01b9:  call       instance void MCCTest.VType8::Add(valuetype MCCTest.VType8)
    IL_01be:  nop
    IL_01bf:  ret
  } // end of method VTypeE::Add

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VTypeE expected) cil managed
  {
    // Code size       1009 (0x3f1)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.Type V_1,
             class MCCTest.ResultVerificationException V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      int16 MCCTest.VTypeE::f4
    IL_0007:  ldarga.s   expected
    IL_0009:  ldfld      int16 MCCTest.VTypeE::f4
    IL_000e:  ceq
    IL_0010:  stloc.3
    IL_0011:  ldloc.3
    IL_0012:  brtrue.s   IL_002f

    IL_0014:  nop
    IL_0015:  ldstr      "f4"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      int16 MCCTest.VTypeE::f4
    IL_0020:  conv.i8
    IL_0021:  ldarga.s   expected
    IL_0023:  ldfld      int16 MCCTest.VTypeE::f4
    IL_0028:  conv.i8
    IL_0029:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_002e:  throw

    IL_002f:  ldarg.0
    IL_0030:  ldfld      float64 MCCTest.VTypeE::f7
    IL_0035:  ldarga.s   expected
    IL_0037:  ldfld      float64 MCCTest.VTypeE::f7
    IL_003c:  ceq
    IL_003e:  stloc.3
    IL_003f:  ldloc.3
    IL_0040:  brtrue.s   IL_005b

    IL_0042:  nop
    IL_0043:  ldstr      "f7"
    IL_0048:  ldarg.0
    IL_0049:  ldfld      float64 MCCTest.VTypeE::f7
    IL_004e:  ldarga.s   expected
    IL_0050:  ldfld      float64 MCCTest.VTypeE::f7
    IL_0055:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_005a:  throw

    IL_005b:  ldarg.0
    IL_005c:  ldfld      int16 MCCTest.VTypeE::f9
    IL_0061:  ldarga.s   expected
    IL_0063:  ldfld      int16 MCCTest.VTypeE::f9
    IL_0068:  ceq
    IL_006a:  stloc.3
    IL_006b:  ldloc.3
    IL_006c:  brtrue.s   IL_0089

    IL_006e:  nop
    IL_006f:  ldstr      "f9"
    IL_0074:  ldarg.0
    IL_0075:  ldfld      int16 MCCTest.VTypeE::f9
    IL_007a:  conv.i8
    IL_007b:  ldarga.s   expected
    IL_007d:  ldfld      int16 MCCTest.VTypeE::f9
    IL_0082:  conv.i8
    IL_0083:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_0088:  throw

    IL_0089:  ldarg.0
    IL_008a:  ldfld      float32 MCCTest.VTypeE::f14
    IL_008f:  ldarga.s   expected
    IL_0091:  ldfld      float32 MCCTest.VTypeE::f14
    IL_0096:  ceq
    IL_0098:  stloc.3
    IL_0099:  ldloc.3
    IL_009a:  brtrue.s   IL_00b7

    IL_009c:  nop
    IL_009d:  ldstr      "f14"
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      float32 MCCTest.VTypeE::f14
    IL_00a8:  conv.r8
    IL_00a9:  ldarga.s   expected
    IL_00ab:  ldfld      float32 MCCTest.VTypeE::f14
    IL_00b0:  conv.r8
    IL_00b1:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00b6:  throw

    IL_00b7:  ldarg.0
    IL_00b8:  ldfld      int32 MCCTest.VTypeE::f15
    IL_00bd:  ldarga.s   expected
    IL_00bf:  ldfld      int32 MCCTest.VTypeE::f15
    IL_00c4:  ceq
    IL_00c6:  stloc.3
    IL_00c7:  ldloc.3
    IL_00c8:  brtrue.s   IL_00e5

    IL_00ca:  nop
    IL_00cb:  ldstr      "f15"
    IL_00d0:  ldarg.0
    IL_00d1:  ldfld      int32 MCCTest.VTypeE::f15
    IL_00d6:  conv.i8
    IL_00d7:  ldarga.s   expected
    IL_00d9:  ldfld      int32 MCCTest.VTypeE::f15
    IL_00de:  conv.i8
    IL_00df:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_00e4:  throw

    IL_00e5:  ldarg.0
    IL_00e6:  ldfld      float64 MCCTest.VTypeE::f18
    IL_00eb:  ldarga.s   expected
    IL_00ed:  ldfld      float64 MCCTest.VTypeE::f18
    IL_00f2:  ceq
    IL_00f4:  stloc.3
    IL_00f5:  ldloc.3
    IL_00f6:  brtrue.s   IL_0111

    IL_00f8:  nop
    IL_00f9:  ldstr      "f18"
    IL_00fe:  ldarg.0
    IL_00ff:  ldfld      float64 MCCTest.VTypeE::f18
    IL_0104:  ldarga.s   expected
    IL_0106:  ldfld      float64 MCCTest.VTypeE::f18
    IL_010b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0110:  throw

    IL_0111:  ldarg.0
    IL_0112:  ldfld      uint64 MCCTest.VTypeE::f19
    IL_0117:  ldarga.s   expected
    IL_0119:  ldfld      uint64 MCCTest.VTypeE::f19
    IL_011e:  ceq
    IL_0120:  stloc.3
    IL_0121:  ldloc.3
    IL_0122:  brtrue.s   IL_0141

    IL_0124:  nop
    IL_0125:  ldstr      "f19"
    IL_012a:  ldarg.0
    IL_012b:  ldfld      uint64 MCCTest.VTypeE::f19
    IL_0130:  conv.r.un
    IL_0131:  conv.r8
    IL_0132:  ldarga.s   expected
    IL_0134:  ldfld      uint64 MCCTest.VTypeE::f19
    IL_0139:  conv.r.un
    IL_013a:  conv.r8
    IL_013b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0140:  throw

    IL_0141:  ldnull
    IL_0142:  stloc.0
    IL_0143:  ldnull
    IL_0144:  stloc.1
    .try
    {
      IL_0145:  nop
      IL_0146:  ldstr      "f1"
      IL_014b:  stloc.0
      IL_014c:  ldarg.0
      IL_014d:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeE::f1
      IL_0152:  box        MCCTest.VType9
      IL_0157:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_015c:  stloc.1
      IL_015d:  ldarg.0
      IL_015e:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeE::f1
      IL_0163:  ldarga.s   expected
      IL_0165:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeE::f1
      IL_016a:  call       instance void MCCTest.VType9::Check(valuetype MCCTest.VType9)
      IL_016f:  nop
      IL_0170:  ldstr      "f2"
      IL_0175:  stloc.0
      IL_0176:  ldarg.0
      IL_0177:  ldfld      class MCCTest.RType4 MCCTest.VTypeE::f2
      IL_017c:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0181:  stloc.1
      IL_0182:  ldarg.0
      IL_0183:  ldfld      class MCCTest.RType4 MCCTest.VTypeE::f2
      IL_0188:  ldarga.s   expected
      IL_018a:  ldfld      class MCCTest.RType4 MCCTest.VTypeE::f2
      IL_018f:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_0194:  nop
      IL_0195:  ldstr      "f3"
      IL_019a:  stloc.0
      IL_019b:  ldarg.0
      IL_019c:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f3
      IL_01a1:  box        MCCTest.VType8
      IL_01a6:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_01ab:  stloc.1
      IL_01ac:  ldarg.0
      IL_01ad:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f3
      IL_01b2:  ldarga.s   expected
      IL_01b4:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f3
      IL_01b9:  call       instance void MCCTest.VType8::Check(valuetype MCCTest.VType8)
      IL_01be:  nop
      IL_01bf:  ldstr      "f5"
      IL_01c4:  stloc.0
      IL_01c5:  ldarg.0
      IL_01c6:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f5
      IL_01cb:  box        MCCTest.VType8
      IL_01d0:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_01d5:  stloc.1
      IL_01d6:  ldarg.0
      IL_01d7:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f5
      IL_01dc:  ldarga.s   expected
      IL_01de:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f5
      IL_01e3:  call       instance void MCCTest.VType8::Check(valuetype MCCTest.VType8)
      IL_01e8:  nop
      IL_01e9:  ldstr      "f6"
      IL_01ee:  stloc.0
      IL_01ef:  ldarg.0
      IL_01f0:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f6
      IL_01f5:  box        MCCTest.VType8
      IL_01fa:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_01ff:  stloc.1
      IL_0200:  ldarg.0
      IL_0201:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f6
      IL_0206:  ldarga.s   expected
      IL_0208:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f6
      IL_020d:  call       instance void MCCTest.VType8::Check(valuetype MCCTest.VType8)
      IL_0212:  nop
      IL_0213:  ldstr      "f8"
      IL_0218:  stloc.0
      IL_0219:  ldarg.0
      IL_021a:  ldfld      valuetype MCCTest.VTypeA MCCTest.VTypeE::f8
      IL_021f:  box        MCCTest.VTypeA
      IL_0224:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0229:  stloc.1
      IL_022a:  ldarg.0
      IL_022b:  ldflda     valuetype MCCTest.VTypeA MCCTest.VTypeE::f8
      IL_0230:  ldarga.s   expected
      IL_0232:  ldfld      valuetype MCCTest.VTypeA MCCTest.VTypeE::f8
      IL_0237:  call       instance void MCCTest.VTypeA::Check(valuetype MCCTest.VTypeA)
      IL_023c:  nop
      IL_023d:  ldstr      "f10"
      IL_0242:  stloc.0
      IL_0243:  ldarg.0
      IL_0244:  ldfld      valuetype MCCTest.VTypeD MCCTest.VTypeE::f10
      IL_0249:  box        MCCTest.VTypeD
      IL_024e:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0253:  stloc.1
      IL_0254:  ldarg.0
      IL_0255:  ldflda     valuetype MCCTest.VTypeD MCCTest.VTypeE::f10
      IL_025a:  ldarga.s   expected
      IL_025c:  ldfld      valuetype MCCTest.VTypeD MCCTest.VTypeE::f10
      IL_0261:  call       instance void MCCTest.VTypeD::Check(valuetype MCCTest.VTypeD)
      IL_0266:  nop
      IL_0267:  ldstr      "f11"
      IL_026c:  stloc.0
      IL_026d:  ldarg.0
      IL_026e:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f11
      IL_0273:  box        MCCTest.VType8
      IL_0278:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_027d:  stloc.1
      IL_027e:  ldarg.0
      IL_027f:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f11
      IL_0284:  ldarga.s   expected
      IL_0286:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f11
      IL_028b:  call       instance void MCCTest.VType8::Check(valuetype MCCTest.VType8)
      IL_0290:  nop
      IL_0291:  ldstr      "f12"
      IL_0296:  stloc.0
      IL_0297:  ldarg.0
      IL_0298:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeE::f12
      IL_029d:  box        MCCTest.VTypeC
      IL_02a2:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_02a7:  stloc.1
      IL_02a8:  ldarg.0
      IL_02a9:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeE::f12
      IL_02ae:  ldarga.s   expected
      IL_02b0:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeE::f12
      IL_02b5:  call       instance void MCCTest.VTypeC::Check(valuetype MCCTest.VTypeC)
      IL_02ba:  nop
      IL_02bb:  ldstr      "f13"
      IL_02c0:  stloc.0
      IL_02c1:  ldarg.0
      IL_02c2:  ldfld      valuetype MCCTest.VTypeA MCCTest.VTypeE::f13
      IL_02c7:  box        MCCTest.VTypeA
      IL_02cc:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_02d1:  stloc.1
      IL_02d2:  ldarg.0
      IL_02d3:  ldflda     valuetype MCCTest.VTypeA MCCTest.VTypeE::f13
      IL_02d8:  ldarga.s   expected
      IL_02da:  ldfld      valuetype MCCTest.VTypeA MCCTest.VTypeE::f13
      IL_02df:  call       instance void MCCTest.VTypeA::Check(valuetype MCCTest.VTypeA)
      IL_02e4:  nop
      IL_02e5:  ldstr      "f16"
      IL_02ea:  stloc.0
      IL_02eb:  ldarg.0
      IL_02ec:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeE::f16
      IL_02f1:  box        MCCTest.VTypeC
      IL_02f6:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_02fb:  stloc.1
      IL_02fc:  ldarg.0
      IL_02fd:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeE::f16
      IL_0302:  ldarga.s   expected
      IL_0304:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeE::f16
      IL_0309:  call       instance void MCCTest.VTypeC::Check(valuetype MCCTest.VTypeC)
      IL_030e:  nop
      IL_030f:  ldstr      "f17"
      IL_0314:  stloc.0
      IL_0315:  ldarg.0
      IL_0316:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeE::f17
      IL_031b:  box        MCCTest.VType7
      IL_0320:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0325:  stloc.1
      IL_0326:  ldarg.0
      IL_0327:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeE::f17
      IL_032c:  ldarga.s   expected
      IL_032e:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeE::f17
      IL_0333:  call       instance void MCCTest.VType7::Check(valuetype MCCTest.VType7)
      IL_0338:  nop
      IL_0339:  ldstr      "f20"
      IL_033e:  stloc.0
      IL_033f:  ldarg.0
      IL_0340:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f20
      IL_0345:  box        MCCTest.VType8
      IL_034a:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_034f:  stloc.1
      IL_0350:  ldarg.0
      IL_0351:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f20
      IL_0356:  ldarga.s   expected
      IL_0358:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f20
      IL_035d:  call       instance void MCCTest.VType8::Check(valuetype MCCTest.VType8)
      IL_0362:  nop
      IL_0363:  ldstr      "f21"
      IL_0368:  stloc.0
      IL_0369:  ldarg.0
      IL_036a:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeE::f21
      IL_036f:  box        MCCTest.VType9
      IL_0374:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0379:  stloc.1
      IL_037a:  ldarg.0
      IL_037b:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeE::f21
      IL_0380:  ldarga.s   expected
      IL_0382:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeE::f21
      IL_0387:  call       instance void MCCTest.VType9::Check(valuetype MCCTest.VType9)
      IL_038c:  nop
      IL_038d:  ldstr      "f22"
      IL_0392:  stloc.0
      IL_0393:  ldarg.0
      IL_0394:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeE::f22
      IL_0399:  box        MCCTest.VTypeC
      IL_039e:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_03a3:  stloc.1
      IL_03a4:  ldarg.0
      IL_03a5:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeE::f22
      IL_03aa:  ldarga.s   expected
      IL_03ac:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeE::f22
      IL_03b1:  call       instance void MCCTest.VTypeC::Check(valuetype MCCTest.VTypeC)
      IL_03b6:  nop
      IL_03b7:  ldstr      "f23"
      IL_03bc:  stloc.0
      IL_03bd:  ldarg.0
      IL_03be:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f23
      IL_03c3:  box        MCCTest.VType8
      IL_03c8:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_03cd:  stloc.1
      IL_03ce:  ldarg.0
      IL_03cf:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f23
      IL_03d4:  ldarga.s   expected
      IL_03d6:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f23
      IL_03db:  call       instance void MCCTest.VType8::Check(valuetype MCCTest.VType8)
      IL_03e0:  nop
      IL_03e1:  nop
      IL_03e2:  leave.s    IL_03ef

    }  // end .try
    catch MCCTest.ResultVerificationException
    {
      IL_03e4:  stloc.2
      IL_03e5:  nop
      IL_03e6:  ldloc.0
      IL_03e7:  ldloc.1
      IL_03e8:  ldloc.2
      IL_03e9:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                    class [mscorlib]System.Type,
                                                                                    class MCCTest.ResultVerificationException)
      IL_03ee:  throw

    }  // end handler
    IL_03ef:  nop
    IL_03f0:  ret
  } // end of method VTypeE::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       1183 (0x49f)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "[Field f1] [Type '{0}']"
    IL_0015:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_001a:  ldarg.0
    IL_001b:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeE::f1
    IL_0020:  box        MCCTest.VType9
    IL_0025:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_002a:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_002f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0034:  nop
    IL_0035:  ldloc.1
    IL_0036:  ldarg.0
    IL_0037:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeE::f1
    IL_003c:  ldarg.1
    IL_003d:  ldc.i4.1
    IL_003e:  add
    IL_003f:  call       instance string MCCTest.VType9::Dump(int32)
    IL_0044:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0049:  nop
    IL_004a:  ldloc.1
    IL_004b:  ldloc.0
    IL_004c:  ldstr      "[Field f2] [Type '{0}']"
    IL_0051:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0056:  ldarg.0
    IL_0057:  ldfld      class MCCTest.RType4 MCCTest.VTypeE::f2
    IL_005c:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0061:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0066:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_006b:  nop
    IL_006c:  ldloc.1
    IL_006d:  ldarg.0
    IL_006e:  ldfld      class MCCTest.RType4 MCCTest.VTypeE::f2
    IL_0073:  ldarg.1
    IL_0074:  ldc.i4.1
    IL_0075:  add
    IL_0076:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_007b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0080:  nop
    IL_0081:  ldloc.1
    IL_0082:  ldloc.0
    IL_0083:  ldstr      "[Field f3] [Type '{0}']"
    IL_0088:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008d:  ldarg.0
    IL_008e:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f3
    IL_0093:  box        MCCTest.VType8
    IL_0098:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_009d:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00a2:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_00a7:  nop
    IL_00a8:  ldloc.1
    IL_00a9:  ldarg.0
    IL_00aa:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f3
    IL_00af:  ldarg.1
    IL_00b0:  ldc.i4.1
    IL_00b1:  add
    IL_00b2:  call       instance string MCCTest.VType8::Dump(int32)
    IL_00b7:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00bc:  nop
    IL_00bd:  ldloc.1
    IL_00be:  ldloc.0
    IL_00bf:  ldstr      "f4    = "
    IL_00c4:  ldarg.0
    IL_00c5:  ldfld      int16 MCCTest.VTypeE::f4
    IL_00ca:  box        [mscorlib]System.Int16
    IL_00cf:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00d4:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00d9:  nop
    IL_00da:  ldloc.1
    IL_00db:  ldloc.0
    IL_00dc:  ldstr      "[Field f5] [Type '{0}']"
    IL_00e1:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_00e6:  ldarg.0
    IL_00e7:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f5
    IL_00ec:  box        MCCTest.VType8
    IL_00f1:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_00f6:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00fb:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0100:  nop
    IL_0101:  ldloc.1
    IL_0102:  ldarg.0
    IL_0103:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f5
    IL_0108:  ldarg.1
    IL_0109:  ldc.i4.1
    IL_010a:  add
    IL_010b:  call       instance string MCCTest.VType8::Dump(int32)
    IL_0110:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0115:  nop
    IL_0116:  ldloc.1
    IL_0117:  ldloc.0
    IL_0118:  ldstr      "[Field f6] [Type '{0}']"
    IL_011d:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0122:  ldarg.0
    IL_0123:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f6
    IL_0128:  box        MCCTest.VType8
    IL_012d:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0132:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0137:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_013c:  nop
    IL_013d:  ldloc.1
    IL_013e:  ldarg.0
    IL_013f:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f6
    IL_0144:  ldarg.1
    IL_0145:  ldc.i4.1
    IL_0146:  add
    IL_0147:  call       instance string MCCTest.VType8::Dump(int32)
    IL_014c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0151:  nop
    IL_0152:  ldloc.1
    IL_0153:  ldloc.0
    IL_0154:  ldstr      "f7    = "
    IL_0159:  ldarg.0
    IL_015a:  ldfld      float64 MCCTest.VTypeE::f7
    IL_015f:  box        [mscorlib]System.Double
    IL_0164:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0169:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_016e:  nop
    IL_016f:  ldloc.1
    IL_0170:  ldloc.0
    IL_0171:  ldstr      "[Field f8] [Type '{0}']"
    IL_0176:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_017b:  ldarg.0
    IL_017c:  ldfld      valuetype MCCTest.VTypeA MCCTest.VTypeE::f8
    IL_0181:  box        MCCTest.VTypeA
    IL_0186:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_018b:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0190:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0195:  nop
    IL_0196:  ldloc.1
    IL_0197:  ldarg.0
    IL_0198:  ldflda     valuetype MCCTest.VTypeA MCCTest.VTypeE::f8
    IL_019d:  ldarg.1
    IL_019e:  ldc.i4.1
    IL_019f:  add
    IL_01a0:  call       instance string MCCTest.VTypeA::Dump(int32)
    IL_01a5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01aa:  nop
    IL_01ab:  ldloc.1
    IL_01ac:  ldloc.0
    IL_01ad:  ldstr      "f9    = "
    IL_01b2:  ldarg.0
    IL_01b3:  ldfld      int16 MCCTest.VTypeE::f9
    IL_01b8:  box        [mscorlib]System.Int16
    IL_01bd:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_01c2:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01c7:  nop
    IL_01c8:  ldloc.1
    IL_01c9:  ldloc.0
    IL_01ca:  ldstr      "[Field f10] [Type '{0}']"
    IL_01cf:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_01d4:  ldarg.0
    IL_01d5:  ldfld      valuetype MCCTest.VTypeD MCCTest.VTypeE::f10
    IL_01da:  box        MCCTest.VTypeD
    IL_01df:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_01e4:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_01e9:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_01ee:  nop
    IL_01ef:  ldloc.1
    IL_01f0:  ldarg.0
    IL_01f1:  ldflda     valuetype MCCTest.VTypeD MCCTest.VTypeE::f10
    IL_01f6:  ldarg.1
    IL_01f7:  ldc.i4.1
    IL_01f8:  add
    IL_01f9:  call       instance string MCCTest.VTypeD::Dump(int32)
    IL_01fe:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0203:  nop
    IL_0204:  ldloc.1
    IL_0205:  ldloc.0
    IL_0206:  ldstr      "[Field f11] [Type '{0}']"
    IL_020b:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0210:  ldarg.0
    IL_0211:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f11
    IL_0216:  box        MCCTest.VType8
    IL_021b:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0220:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0225:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_022a:  nop
    IL_022b:  ldloc.1
    IL_022c:  ldarg.0
    IL_022d:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f11
    IL_0232:  ldarg.1
    IL_0233:  ldc.i4.1
    IL_0234:  add
    IL_0235:  call       instance string MCCTest.VType8::Dump(int32)
    IL_023a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_023f:  nop
    IL_0240:  ldloc.1
    IL_0241:  ldloc.0
    IL_0242:  ldstr      "[Field f12] [Type '{0}']"
    IL_0247:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_024c:  ldarg.0
    IL_024d:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeE::f12
    IL_0252:  box        MCCTest.VTypeC
    IL_0257:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_025c:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0261:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0266:  nop
    IL_0267:  ldloc.1
    IL_0268:  ldarg.0
    IL_0269:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeE::f12
    IL_026e:  ldarg.1
    IL_026f:  ldc.i4.1
    IL_0270:  add
    IL_0271:  call       instance string MCCTest.VTypeC::Dump(int32)
    IL_0276:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_027b:  nop
    IL_027c:  ldloc.1
    IL_027d:  ldloc.0
    IL_027e:  ldstr      "[Field f13] [Type '{0}']"
    IL_0283:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0288:  ldarg.0
    IL_0289:  ldfld      valuetype MCCTest.VTypeA MCCTest.VTypeE::f13
    IL_028e:  box        MCCTest.VTypeA
    IL_0293:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0298:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_029d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_02a2:  nop
    IL_02a3:  ldloc.1
    IL_02a4:  ldarg.0
    IL_02a5:  ldflda     valuetype MCCTest.VTypeA MCCTest.VTypeE::f13
    IL_02aa:  ldarg.1
    IL_02ab:  ldc.i4.1
    IL_02ac:  add
    IL_02ad:  call       instance string MCCTest.VTypeA::Dump(int32)
    IL_02b2:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02b7:  nop
    IL_02b8:  ldloc.1
    IL_02b9:  ldloc.0
    IL_02ba:  ldstr      "f14    = "
    IL_02bf:  ldarg.0
    IL_02c0:  ldfld      float32 MCCTest.VTypeE::f14
    IL_02c5:  box        [mscorlib]System.Single
    IL_02ca:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_02cf:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02d4:  nop
    IL_02d5:  ldloc.1
    IL_02d6:  ldloc.0
    IL_02d7:  ldstr      "f15    = "
    IL_02dc:  ldarg.0
    IL_02dd:  ldfld      int32 MCCTest.VTypeE::f15
    IL_02e2:  box        [mscorlib]System.Int32
    IL_02e7:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_02ec:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02f1:  nop
    IL_02f2:  ldloc.1
    IL_02f3:  ldloc.0
    IL_02f4:  ldstr      "[Field f16] [Type '{0}']"
    IL_02f9:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_02fe:  ldarg.0
    IL_02ff:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeE::f16
    IL_0304:  box        MCCTest.VTypeC
    IL_0309:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_030e:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0313:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0318:  nop
    IL_0319:  ldloc.1
    IL_031a:  ldarg.0
    IL_031b:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeE::f16
    IL_0320:  ldarg.1
    IL_0321:  ldc.i4.1
    IL_0322:  add
    IL_0323:  call       instance string MCCTest.VTypeC::Dump(int32)
    IL_0328:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_032d:  nop
    IL_032e:  ldloc.1
    IL_032f:  ldloc.0
    IL_0330:  ldstr      "[Field f17] [Type '{0}']"
    IL_0335:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_033a:  ldarg.0
    IL_033b:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeE::f17
    IL_0340:  box        MCCTest.VType7
    IL_0345:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_034a:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_034f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0354:  nop
    IL_0355:  ldloc.1
    IL_0356:  ldarg.0
    IL_0357:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeE::f17
    IL_035c:  ldarg.1
    IL_035d:  ldc.i4.1
    IL_035e:  add
    IL_035f:  call       instance string MCCTest.VType7::Dump(int32)
    IL_0364:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0369:  nop
    IL_036a:  ldloc.1
    IL_036b:  ldloc.0
    IL_036c:  ldstr      "f18    = "
    IL_0371:  ldarg.0
    IL_0372:  ldfld      float64 MCCTest.VTypeE::f18
    IL_0377:  box        [mscorlib]System.Double
    IL_037c:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0381:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0386:  nop
    IL_0387:  ldloc.1
    IL_0388:  ldloc.0
    IL_0389:  ldstr      "f19    = "
    IL_038e:  ldarg.0
    IL_038f:  ldfld      uint64 MCCTest.VTypeE::f19
    IL_0394:  box        [mscorlib]System.UInt64
    IL_0399:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_039e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_03a3:  nop
    IL_03a4:  ldloc.1
    IL_03a5:  ldloc.0
    IL_03a6:  ldstr      "[Field f20] [Type '{0}']"
    IL_03ab:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_03b0:  ldarg.0
    IL_03b1:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f20
    IL_03b6:  box        MCCTest.VType8
    IL_03bb:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_03c0:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_03c5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_03ca:  nop
    IL_03cb:  ldloc.1
    IL_03cc:  ldarg.0
    IL_03cd:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f20
    IL_03d2:  ldarg.1
    IL_03d3:  ldc.i4.1
    IL_03d4:  add
    IL_03d5:  call       instance string MCCTest.VType8::Dump(int32)
    IL_03da:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_03df:  nop
    IL_03e0:  ldloc.1
    IL_03e1:  ldloc.0
    IL_03e2:  ldstr      "[Field f21] [Type '{0}']"
    IL_03e7:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_03ec:  ldarg.0
    IL_03ed:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeE::f21
    IL_03f2:  box        MCCTest.VType9
    IL_03f7:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_03fc:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0401:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0406:  nop
    IL_0407:  ldloc.1
    IL_0408:  ldarg.0
    IL_0409:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeE::f21
    IL_040e:  ldarg.1
    IL_040f:  ldc.i4.1
    IL_0410:  add
    IL_0411:  call       instance string MCCTest.VType9::Dump(int32)
    IL_0416:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_041b:  nop
    IL_041c:  ldloc.1
    IL_041d:  ldloc.0
    IL_041e:  ldstr      "[Field f22] [Type '{0}']"
    IL_0423:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0428:  ldarg.0
    IL_0429:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeE::f22
    IL_042e:  box        MCCTest.VTypeC
    IL_0433:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0438:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_043d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0442:  nop
    IL_0443:  ldloc.1
    IL_0444:  ldarg.0
    IL_0445:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeE::f22
    IL_044a:  ldarg.1
    IL_044b:  ldc.i4.1
    IL_044c:  add
    IL_044d:  call       instance string MCCTest.VTypeC::Dump(int32)
    IL_0452:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0457:  nop
    IL_0458:  ldloc.1
    IL_0459:  ldloc.0
    IL_045a:  ldstr      "[Field f23] [Type '{0}']"
    IL_045f:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0464:  ldarg.0
    IL_0465:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeE::f23
    IL_046a:  box        MCCTest.VType8
    IL_046f:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0474:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0479:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_047e:  nop
    IL_047f:  ldloc.1
    IL_0480:  ldarg.0
    IL_0481:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeE::f23
    IL_0486:  ldarg.1
    IL_0487:  ldc.i4.1
    IL_0488:  add
    IL_0489:  call       instance string MCCTest.VType8::Dump(int32)
    IL_048e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0493:  nop
    IL_0494:  ldloc.1
    IL_0495:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_049a:  stloc.2
    IL_049b:  br.s       IL_049d

    IL_049d:  ldloc.2
    IL_049e:  ret
  } // end of method VTypeE::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VTypeE::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VTypeE::Dump

} // end of class MCCTest.VTypeE

.class public sequential ansi sealed beforefieldinit MCCTest.VTypeF
       extends [mscorlib]System.ValueType
       implements class MCCTest.CType`1<valuetype MCCTest.VTypeF>
{
  .field public char f1
  .field public float64 f2
  .field public float32 f3
  .field public valuetype MCCTest.VType9 f4
  .field public class MCCTest.RType4 f5
  .field public char f6
  .field public float32 f7
  .field public valuetype MCCTest.VTypeE f8
  .field public valuetype MCCTest.VType8 f9
  .field public valuetype MCCTest.VTypeE f10
  .field public valuetype MCCTest.VType3 f11
  .field public int64 f12
  .field public uint16 f13
  .field public valuetype MCCTest.VType5 f14
  .field public valuetype MCCTest.VType9 f15
  .field public class MCCTest.RType4 f16
  .field public class MCCTest.RType4 f17
  .field public int32 f18
  .field public valuetype MCCTest.VType8 f19
  .field public class MCCTest.RType4 f20
  .field public valuetype MCCTest.VTypeA f21
  .field public valuetype MCCTest.VTypeD f22
  .field public valuetype MCCTest.VType9 f23
  .field public valuetype MCCTest.VTypeE f24
  .field public class MCCTest.RType4 f25
  .field public float32 f26
  .field public uint8 f27
  .field public float64 f28
  .field public valuetype MCCTest.VTypeC f29
  .field public valuetype MCCTest.VType6 f30
  .field public float64 f31
  .field public valuetype MCCTest.VType8 f32
  .field public int8 f33
  .field public valuetype MCCTest.VTypeD f34
  .field public valuetype MCCTest.VTypeE f35
  .field public valuetype MCCTest.VTypeE f36
  .field public valuetype MCCTest.VType7 f37
  .field public valuetype MCCTest.VType9 f38
  .field public valuetype MCCTest.VType8 f39
  .field public float32 f40
  .field public int32 f41
  .field public char f42
  .field public valuetype MCCTest.VTypeB f43
  .field public valuetype MCCTest.VType8 f44
  .method public hidebysig newslot virtual final
          instance void  Init(int32 count) cil managed
  {
    // Code size       547 (0x223)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0007:  stfld      class MCCTest.RType4 MCCTest.VTypeF::f5
    IL_000c:  ldarg.0
    IL_000d:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0012:  stfld      class MCCTest.RType4 MCCTest.VTypeF::f16
    IL_0017:  ldarg.0
    IL_0018:  newobj     instance void MCCTest.RType4::.ctor()
    IL_001d:  stfld      class MCCTest.RType4 MCCTest.VTypeF::f17
    IL_0022:  ldarg.0
    IL_0023:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0028:  stfld      class MCCTest.RType4 MCCTest.VTypeF::f20
    IL_002d:  ldarg.0
    IL_002e:  newobj     instance void MCCTest.RType4::.ctor()
    IL_0033:  stfld      class MCCTest.RType4 MCCTest.VTypeF::f25
    IL_0038:  ldarg.0
    IL_0039:  ldarg.1
    IL_003a:  conv.u2
    IL_003b:  stfld      char MCCTest.VTypeF::f1
    IL_0040:  ldarg.0
    IL_0041:  ldarg.1
    IL_0042:  conv.r8
    IL_0043:  stfld      float64 MCCTest.VTypeF::f2
    IL_0048:  ldarg.0
    IL_0049:  ldarg.1
    IL_004a:  conv.r4
    IL_004b:  stfld      float32 MCCTest.VTypeF::f3
    IL_0050:  ldarg.0
    IL_0051:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f4
    IL_0056:  ldarg.1
    IL_0057:  call       instance void MCCTest.VType9::Init(int32)
    IL_005c:  nop
    IL_005d:  ldarg.0
    IL_005e:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f5
    IL_0063:  ldarg.1
    IL_0064:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_0069:  nop
    IL_006a:  ldarg.0
    IL_006b:  ldarg.1
    IL_006c:  conv.u2
    IL_006d:  stfld      char MCCTest.VTypeF::f6
    IL_0072:  ldarg.0
    IL_0073:  ldarg.1
    IL_0074:  conv.r4
    IL_0075:  stfld      float32 MCCTest.VTypeF::f7
    IL_007a:  ldarg.0
    IL_007b:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f8
    IL_0080:  ldarg.1
    IL_0081:  call       instance void MCCTest.VTypeE::Init(int32)
    IL_0086:  nop
    IL_0087:  ldarg.0
    IL_0088:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f9
    IL_008d:  ldarg.1
    IL_008e:  call       instance void MCCTest.VType8::Init(int32)
    IL_0093:  nop
    IL_0094:  ldarg.0
    IL_0095:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f10
    IL_009a:  ldarg.1
    IL_009b:  call       instance void MCCTest.VTypeE::Init(int32)
    IL_00a0:  nop
    IL_00a1:  ldarg.0
    IL_00a2:  ldflda     valuetype MCCTest.VType3 MCCTest.VTypeF::f11
    IL_00a7:  ldarg.1
    IL_00a8:  call       instance void MCCTest.VType3::Init(int32)
    IL_00ad:  nop
    IL_00ae:  ldarg.0
    IL_00af:  ldarg.1
    IL_00b0:  conv.i8
    IL_00b1:  stfld      int64 MCCTest.VTypeF::f12
    IL_00b6:  ldarg.0
    IL_00b7:  ldarg.1
    IL_00b8:  conv.u2
    IL_00b9:  stfld      uint16 MCCTest.VTypeF::f13
    IL_00be:  ldarg.0
    IL_00bf:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeF::f14
    IL_00c4:  ldarg.1
    IL_00c5:  call       instance void MCCTest.VType5::Init(int32)
    IL_00ca:  nop
    IL_00cb:  ldarg.0
    IL_00cc:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f15
    IL_00d1:  ldarg.1
    IL_00d2:  call       instance void MCCTest.VType9::Init(int32)
    IL_00d7:  nop
    IL_00d8:  ldarg.0
    IL_00d9:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f16
    IL_00de:  ldarg.1
    IL_00df:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_00e4:  nop
    IL_00e5:  ldarg.0
    IL_00e6:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f17
    IL_00eb:  ldarg.1
    IL_00ec:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_00f1:  nop
    IL_00f2:  ldarg.0
    IL_00f3:  ldarg.1
    IL_00f4:  stfld      int32 MCCTest.VTypeF::f18
    IL_00f9:  ldarg.0
    IL_00fa:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f19
    IL_00ff:  ldarg.1
    IL_0100:  call       instance void MCCTest.VType8::Init(int32)
    IL_0105:  nop
    IL_0106:  ldarg.0
    IL_0107:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f20
    IL_010c:  ldarg.1
    IL_010d:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_0112:  nop
    IL_0113:  ldarg.0
    IL_0114:  ldflda     valuetype MCCTest.VTypeA MCCTest.VTypeF::f21
    IL_0119:  ldarg.1
    IL_011a:  call       instance void MCCTest.VTypeA::Init(int32)
    IL_011f:  nop
    IL_0120:  ldarg.0
    IL_0121:  ldflda     valuetype MCCTest.VTypeD MCCTest.VTypeF::f22
    IL_0126:  ldarg.1
    IL_0127:  call       instance void MCCTest.VTypeD::Init(int32)
    IL_012c:  nop
    IL_012d:  ldarg.0
    IL_012e:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f23
    IL_0133:  ldarg.1
    IL_0134:  call       instance void MCCTest.VType9::Init(int32)
    IL_0139:  nop
    IL_013a:  ldarg.0
    IL_013b:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f24
    IL_0140:  ldarg.1
    IL_0141:  call       instance void MCCTest.VTypeE::Init(int32)
    IL_0146:  nop
    IL_0147:  ldarg.0
    IL_0148:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f25
    IL_014d:  ldarg.1
    IL_014e:  callvirt   instance void MCCTest.RType4::Init(int32)
    IL_0153:  nop
    IL_0154:  ldarg.0
    IL_0155:  ldarg.1
    IL_0156:  conv.r4
    IL_0157:  stfld      float32 MCCTest.VTypeF::f26
    IL_015c:  ldarg.0
    IL_015d:  ldarg.1
    IL_015e:  conv.u1
    IL_015f:  stfld      uint8 MCCTest.VTypeF::f27
    IL_0164:  ldarg.0
    IL_0165:  ldarg.1
    IL_0166:  conv.r8
    IL_0167:  stfld      float64 MCCTest.VTypeF::f28
    IL_016c:  ldarg.0
    IL_016d:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeF::f29
    IL_0172:  ldarg.1
    IL_0173:  call       instance void MCCTest.VTypeC::Init(int32)
    IL_0178:  nop
    IL_0179:  ldarg.0
    IL_017a:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeF::f30
    IL_017f:  ldarg.1
    IL_0180:  call       instance void MCCTest.VType6::Init(int32)
    IL_0185:  nop
    IL_0186:  ldarg.0
    IL_0187:  ldarg.1
    IL_0188:  conv.r8
    IL_0189:  stfld      float64 MCCTest.VTypeF::f31
    IL_018e:  ldarg.0
    IL_018f:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f32
    IL_0194:  ldarg.1
    IL_0195:  call       instance void MCCTest.VType8::Init(int32)
    IL_019a:  nop
    IL_019b:  ldarg.0
    IL_019c:  ldarg.1
    IL_019d:  conv.i1
    IL_019e:  stfld      int8 MCCTest.VTypeF::f33
    IL_01a3:  ldarg.0
    IL_01a4:  ldflda     valuetype MCCTest.VTypeD MCCTest.VTypeF::f34
    IL_01a9:  ldarg.1
    IL_01aa:  call       instance void MCCTest.VTypeD::Init(int32)
    IL_01af:  nop
    IL_01b0:  ldarg.0
    IL_01b1:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f35
    IL_01b6:  ldarg.1
    IL_01b7:  call       instance void MCCTest.VTypeE::Init(int32)
    IL_01bc:  nop
    IL_01bd:  ldarg.0
    IL_01be:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f36
    IL_01c3:  ldarg.1
    IL_01c4:  call       instance void MCCTest.VTypeE::Init(int32)
    IL_01c9:  nop
    IL_01ca:  ldarg.0
    IL_01cb:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeF::f37
    IL_01d0:  ldarg.1
    IL_01d1:  call       instance void MCCTest.VType7::Init(int32)
    IL_01d6:  nop
    IL_01d7:  ldarg.0
    IL_01d8:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f38
    IL_01dd:  ldarg.1
    IL_01de:  call       instance void MCCTest.VType9::Init(int32)
    IL_01e3:  nop
    IL_01e4:  ldarg.0
    IL_01e5:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f39
    IL_01ea:  ldarg.1
    IL_01eb:  call       instance void MCCTest.VType8::Init(int32)
    IL_01f0:  nop
    IL_01f1:  ldarg.0
    IL_01f2:  ldarg.1
    IL_01f3:  conv.r4
    IL_01f4:  stfld      float32 MCCTest.VTypeF::f40
    IL_01f9:  ldarg.0
    IL_01fa:  ldarg.1
    IL_01fb:  stfld      int32 MCCTest.VTypeF::f41
    IL_0200:  ldarg.0
    IL_0201:  ldarg.1
    IL_0202:  conv.u2
    IL_0203:  stfld      char MCCTest.VTypeF::f42
    IL_0208:  ldarg.0
    IL_0209:  ldflda     valuetype MCCTest.VTypeB MCCTest.VTypeF::f43
    IL_020e:  ldarg.1
    IL_020f:  call       instance void MCCTest.VTypeB::Init(int32)
    IL_0214:  nop
    IL_0215:  ldarg.0
    IL_0216:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f44
    IL_021b:  ldarg.1
    IL_021c:  call       instance void MCCTest.VType8::Init(int32)
    IL_0221:  nop
    IL_0222:  ret
  } // end of method VTypeF::Init

  .method public hidebysig newslot virtual final
          instance void  Init() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance void MCCTest.VTypeF::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VTypeF::Init

  .method public hidebysig newslot virtual final
          instance void  Zero() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance void MCCTest.VTypeF::Init(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method VTypeF::Zero

  .method public hidebysig instance void
          Add(valuetype MCCTest.VTypeF val) cil managed
  {
    // Code size       860 (0x35c)
    .maxstack  3
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      char MCCTest.VTypeF::f1
    IL_0008:  ldarga.s   val
    IL_000a:  ldfld      char MCCTest.VTypeF::f1
    IL_000f:  add
    IL_0010:  conv.u2
    IL_0011:  stfld      char MCCTest.VTypeF::f1
    IL_0016:  ldarg.0
    IL_0017:  dup
    IL_0018:  ldfld      float64 MCCTest.VTypeF::f2
    IL_001d:  ldarga.s   val
    IL_001f:  ldfld      float64 MCCTest.VTypeF::f2
    IL_0024:  add
    IL_0025:  stfld      float64 MCCTest.VTypeF::f2
    IL_002a:  ldarg.0
    IL_002b:  dup
    IL_002c:  ldfld      float32 MCCTest.VTypeF::f3
    IL_0031:  ldarga.s   val
    IL_0033:  ldfld      float32 MCCTest.VTypeF::f3
    IL_0038:  add
    IL_0039:  stfld      float32 MCCTest.VTypeF::f3
    IL_003e:  ldarg.0
    IL_003f:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f4
    IL_0044:  ldarga.s   val
    IL_0046:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f4
    IL_004b:  call       instance void MCCTest.VType9::Add(valuetype MCCTest.VType9)
    IL_0050:  nop
    IL_0051:  ldarg.0
    IL_0052:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f5
    IL_0057:  ldarga.s   val
    IL_0059:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f5
    IL_005e:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_0063:  nop
    IL_0064:  ldarg.0
    IL_0065:  dup
    IL_0066:  ldfld      char MCCTest.VTypeF::f6
    IL_006b:  ldarga.s   val
    IL_006d:  ldfld      char MCCTest.VTypeF::f6
    IL_0072:  add
    IL_0073:  conv.u2
    IL_0074:  stfld      char MCCTest.VTypeF::f6
    IL_0079:  ldarg.0
    IL_007a:  dup
    IL_007b:  ldfld      float32 MCCTest.VTypeF::f7
    IL_0080:  ldarga.s   val
    IL_0082:  ldfld      float32 MCCTest.VTypeF::f7
    IL_0087:  add
    IL_0088:  stfld      float32 MCCTest.VTypeF::f7
    IL_008d:  ldarg.0
    IL_008e:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f8
    IL_0093:  ldarga.s   val
    IL_0095:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f8
    IL_009a:  call       instance void MCCTest.VTypeE::Add(valuetype MCCTest.VTypeE)
    IL_009f:  nop
    IL_00a0:  ldarg.0
    IL_00a1:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f9
    IL_00a6:  ldarga.s   val
    IL_00a8:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f9
    IL_00ad:  call       instance void MCCTest.VType8::Add(valuetype MCCTest.VType8)
    IL_00b2:  nop
    IL_00b3:  ldarg.0
    IL_00b4:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f10
    IL_00b9:  ldarga.s   val
    IL_00bb:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f10
    IL_00c0:  call       instance void MCCTest.VTypeE::Add(valuetype MCCTest.VTypeE)
    IL_00c5:  nop
    IL_00c6:  ldarg.0
    IL_00c7:  ldflda     valuetype MCCTest.VType3 MCCTest.VTypeF::f11
    IL_00cc:  ldarga.s   val
    IL_00ce:  ldfld      valuetype MCCTest.VType3 MCCTest.VTypeF::f11
    IL_00d3:  call       instance void MCCTest.VType3::Add(valuetype MCCTest.VType3)
    IL_00d8:  nop
    IL_00d9:  ldarg.0
    IL_00da:  dup
    IL_00db:  ldfld      int64 MCCTest.VTypeF::f12
    IL_00e0:  ldarga.s   val
    IL_00e2:  ldfld      int64 MCCTest.VTypeF::f12
    IL_00e7:  add
    IL_00e8:  stfld      int64 MCCTest.VTypeF::f12
    IL_00ed:  ldarg.0
    IL_00ee:  dup
    IL_00ef:  ldfld      uint16 MCCTest.VTypeF::f13
    IL_00f4:  ldarga.s   val
    IL_00f6:  ldfld      uint16 MCCTest.VTypeF::f13
    IL_00fb:  add
    IL_00fc:  conv.u2
    IL_00fd:  stfld      uint16 MCCTest.VTypeF::f13
    IL_0102:  ldarg.0
    IL_0103:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeF::f14
    IL_0108:  ldarga.s   val
    IL_010a:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeF::f14
    IL_010f:  call       instance void MCCTest.VType5::Add(valuetype MCCTest.VType5)
    IL_0114:  nop
    IL_0115:  ldarg.0
    IL_0116:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f15
    IL_011b:  ldarga.s   val
    IL_011d:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f15
    IL_0122:  call       instance void MCCTest.VType9::Add(valuetype MCCTest.VType9)
    IL_0127:  nop
    IL_0128:  ldarg.0
    IL_0129:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f16
    IL_012e:  ldarga.s   val
    IL_0130:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f16
    IL_0135:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_013a:  nop
    IL_013b:  ldarg.0
    IL_013c:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f17
    IL_0141:  ldarga.s   val
    IL_0143:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f17
    IL_0148:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_014d:  nop
    IL_014e:  ldarg.0
    IL_014f:  dup
    IL_0150:  ldfld      int32 MCCTest.VTypeF::f18
    IL_0155:  ldarga.s   val
    IL_0157:  ldfld      int32 MCCTest.VTypeF::f18
    IL_015c:  add
    IL_015d:  stfld      int32 MCCTest.VTypeF::f18
    IL_0162:  ldarg.0
    IL_0163:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f19
    IL_0168:  ldarga.s   val
    IL_016a:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f19
    IL_016f:  call       instance void MCCTest.VType8::Add(valuetype MCCTest.VType8)
    IL_0174:  nop
    IL_0175:  ldarg.0
    IL_0176:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f20
    IL_017b:  ldarga.s   val
    IL_017d:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f20
    IL_0182:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_0187:  nop
    IL_0188:  ldarg.0
    IL_0189:  ldflda     valuetype MCCTest.VTypeA MCCTest.VTypeF::f21
    IL_018e:  ldarga.s   val
    IL_0190:  ldfld      valuetype MCCTest.VTypeA MCCTest.VTypeF::f21
    IL_0195:  call       instance void MCCTest.VTypeA::Add(valuetype MCCTest.VTypeA)
    IL_019a:  nop
    IL_019b:  ldarg.0
    IL_019c:  ldflda     valuetype MCCTest.VTypeD MCCTest.VTypeF::f22
    IL_01a1:  ldarga.s   val
    IL_01a3:  ldfld      valuetype MCCTest.VTypeD MCCTest.VTypeF::f22
    IL_01a8:  call       instance void MCCTest.VTypeD::Add(valuetype MCCTest.VTypeD)
    IL_01ad:  nop
    IL_01ae:  ldarg.0
    IL_01af:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f23
    IL_01b4:  ldarga.s   val
    IL_01b6:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f23
    IL_01bb:  call       instance void MCCTest.VType9::Add(valuetype MCCTest.VType9)
    IL_01c0:  nop
    IL_01c1:  ldarg.0
    IL_01c2:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f24
    IL_01c7:  ldarga.s   val
    IL_01c9:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f24
    IL_01ce:  call       instance void MCCTest.VTypeE::Add(valuetype MCCTest.VTypeE)
    IL_01d3:  nop
    IL_01d4:  ldarg.0
    IL_01d5:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f25
    IL_01da:  ldarga.s   val
    IL_01dc:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f25
    IL_01e1:  callvirt   instance void MCCTest.RType4::Add(class MCCTest.RType4)
    IL_01e6:  nop
    IL_01e7:  ldarg.0
    IL_01e8:  dup
    IL_01e9:  ldfld      float32 MCCTest.VTypeF::f26
    IL_01ee:  ldarga.s   val
    IL_01f0:  ldfld      float32 MCCTest.VTypeF::f26
    IL_01f5:  add
    IL_01f6:  stfld      float32 MCCTest.VTypeF::f26
    IL_01fb:  ldarg.0
    IL_01fc:  dup
    IL_01fd:  ldfld      uint8 MCCTest.VTypeF::f27
    IL_0202:  ldarga.s   val
    IL_0204:  ldfld      uint8 MCCTest.VTypeF::f27
    IL_0209:  add
    IL_020a:  conv.u1
    IL_020b:  stfld      uint8 MCCTest.VTypeF::f27
    IL_0210:  ldarg.0
    IL_0211:  dup
    IL_0212:  ldfld      float64 MCCTest.VTypeF::f28
    IL_0217:  ldarga.s   val
    IL_0219:  ldfld      float64 MCCTest.VTypeF::f28
    IL_021e:  add
    IL_021f:  stfld      float64 MCCTest.VTypeF::f28
    IL_0224:  ldarg.0
    IL_0225:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeF::f29
    IL_022a:  ldarga.s   val
    IL_022c:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeF::f29
    IL_0231:  call       instance void MCCTest.VTypeC::Add(valuetype MCCTest.VTypeC)
    IL_0236:  nop
    IL_0237:  ldarg.0
    IL_0238:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeF::f30
    IL_023d:  ldarga.s   val
    IL_023f:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeF::f30
    IL_0244:  call       instance void MCCTest.VType6::Add(valuetype MCCTest.VType6)
    IL_0249:  nop
    IL_024a:  ldarg.0
    IL_024b:  dup
    IL_024c:  ldfld      float64 MCCTest.VTypeF::f31
    IL_0251:  ldarga.s   val
    IL_0253:  ldfld      float64 MCCTest.VTypeF::f31
    IL_0258:  add
    IL_0259:  stfld      float64 MCCTest.VTypeF::f31
    IL_025e:  ldarg.0
    IL_025f:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f32
    IL_0264:  ldarga.s   val
    IL_0266:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f32
    IL_026b:  call       instance void MCCTest.VType8::Add(valuetype MCCTest.VType8)
    IL_0270:  nop
    IL_0271:  ldarg.0
    IL_0272:  dup
    IL_0273:  ldfld      int8 MCCTest.VTypeF::f33
    IL_0278:  ldarga.s   val
    IL_027a:  ldfld      int8 MCCTest.VTypeF::f33
    IL_027f:  add
    IL_0280:  conv.i1
    IL_0281:  stfld      int8 MCCTest.VTypeF::f33
    IL_0286:  ldarg.0
    IL_0287:  ldflda     valuetype MCCTest.VTypeD MCCTest.VTypeF::f34
    IL_028c:  ldarga.s   val
    IL_028e:  ldfld      valuetype MCCTest.VTypeD MCCTest.VTypeF::f34
    IL_0293:  call       instance void MCCTest.VTypeD::Add(valuetype MCCTest.VTypeD)
    IL_0298:  nop
    IL_0299:  ldarg.0
    IL_029a:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f35
    IL_029f:  ldarga.s   val
    IL_02a1:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f35
    IL_02a6:  call       instance void MCCTest.VTypeE::Add(valuetype MCCTest.VTypeE)
    IL_02ab:  nop
    IL_02ac:  ldarg.0
    IL_02ad:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f36
    IL_02b2:  ldarga.s   val
    IL_02b4:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f36
    IL_02b9:  call       instance void MCCTest.VTypeE::Add(valuetype MCCTest.VTypeE)
    IL_02be:  nop
    IL_02bf:  ldarg.0
    IL_02c0:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeF::f37
    IL_02c5:  ldarga.s   val
    IL_02c7:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeF::f37
    IL_02cc:  call       instance void MCCTest.VType7::Add(valuetype MCCTest.VType7)
    IL_02d1:  nop
    IL_02d2:  ldarg.0
    IL_02d3:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f38
    IL_02d8:  ldarga.s   val
    IL_02da:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f38
    IL_02df:  call       instance void MCCTest.VType9::Add(valuetype MCCTest.VType9)
    IL_02e4:  nop
    IL_02e5:  ldarg.0
    IL_02e6:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f39
    IL_02eb:  ldarga.s   val
    IL_02ed:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f39
    IL_02f2:  call       instance void MCCTest.VType8::Add(valuetype MCCTest.VType8)
    IL_02f7:  nop
    IL_02f8:  ldarg.0
    IL_02f9:  dup
    IL_02fa:  ldfld      float32 MCCTest.VTypeF::f40
    IL_02ff:  ldarga.s   val
    IL_0301:  ldfld      float32 MCCTest.VTypeF::f40
    IL_0306:  add
    IL_0307:  stfld      float32 MCCTest.VTypeF::f40
    IL_030c:  ldarg.0
    IL_030d:  dup
    IL_030e:  ldfld      int32 MCCTest.VTypeF::f41
    IL_0313:  ldarga.s   val
    IL_0315:  ldfld      int32 MCCTest.VTypeF::f41
    IL_031a:  add
    IL_031b:  stfld      int32 MCCTest.VTypeF::f41
    IL_0320:  ldarg.0
    IL_0321:  dup
    IL_0322:  ldfld      char MCCTest.VTypeF::f42
    IL_0327:  ldarga.s   val
    IL_0329:  ldfld      char MCCTest.VTypeF::f42
    IL_032e:  add
    IL_032f:  conv.u2
    IL_0330:  stfld      char MCCTest.VTypeF::f42
    IL_0335:  ldarg.0
    IL_0336:  ldflda     valuetype MCCTest.VTypeB MCCTest.VTypeF::f43
    IL_033b:  ldarga.s   val
    IL_033d:  ldfld      valuetype MCCTest.VTypeB MCCTest.VTypeF::f43
    IL_0342:  call       instance void MCCTest.VTypeB::Add(valuetype MCCTest.VTypeB)
    IL_0347:  nop
    IL_0348:  ldarg.0
    IL_0349:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f44
    IL_034e:  ldarga.s   val
    IL_0350:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f44
    IL_0355:  call       instance void MCCTest.VType8::Add(valuetype MCCTest.VType8)
    IL_035a:  nop
    IL_035b:  ret
  } // end of method VTypeF::Add

  .method public hidebysig newslot virtual final
          instance void  Check(valuetype MCCTest.VTypeF expected) cil managed
  {
    // Code size       1901 (0x76d)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.Type V_1,
             class MCCTest.ResultVerificationException V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      char MCCTest.VTypeF::f1
    IL_0007:  ldarga.s   expected
    IL_0009:  ldfld      char MCCTest.VTypeF::f1
    IL_000e:  ceq
    IL_0010:  stloc.3
    IL_0011:  ldloc.3
    IL_0012:  brtrue.s   IL_002f

    IL_0014:  nop
    IL_0015:  ldstr      "f1"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      char MCCTest.VTypeF::f1
    IL_0020:  conv.u8
    IL_0021:  ldarga.s   expected
    IL_0023:  ldfld      char MCCTest.VTypeF::f1
    IL_0028:  conv.u8
    IL_0029:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_002e:  throw

    IL_002f:  ldarg.0
    IL_0030:  ldfld      float64 MCCTest.VTypeF::f2
    IL_0035:  ldarga.s   expected
    IL_0037:  ldfld      float64 MCCTest.VTypeF::f2
    IL_003c:  ceq
    IL_003e:  stloc.3
    IL_003f:  ldloc.3
    IL_0040:  brtrue.s   IL_005b

    IL_0042:  nop
    IL_0043:  ldstr      "f2"
    IL_0048:  ldarg.0
    IL_0049:  ldfld      float64 MCCTest.VTypeF::f2
    IL_004e:  ldarga.s   expected
    IL_0050:  ldfld      float64 MCCTest.VTypeF::f2
    IL_0055:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_005a:  throw

    IL_005b:  ldarg.0
    IL_005c:  ldfld      float32 MCCTest.VTypeF::f3
    IL_0061:  ldarga.s   expected
    IL_0063:  ldfld      float32 MCCTest.VTypeF::f3
    IL_0068:  ceq
    IL_006a:  stloc.3
    IL_006b:  ldloc.3
    IL_006c:  brtrue.s   IL_0089

    IL_006e:  nop
    IL_006f:  ldstr      "f3"
    IL_0074:  ldarg.0
    IL_0075:  ldfld      float32 MCCTest.VTypeF::f3
    IL_007a:  conv.r8
    IL_007b:  ldarga.s   expected
    IL_007d:  ldfld      float32 MCCTest.VTypeF::f3
    IL_0082:  conv.r8
    IL_0083:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0088:  throw

    IL_0089:  ldarg.0
    IL_008a:  ldfld      char MCCTest.VTypeF::f6
    IL_008f:  ldarga.s   expected
    IL_0091:  ldfld      char MCCTest.VTypeF::f6
    IL_0096:  ceq
    IL_0098:  stloc.3
    IL_0099:  ldloc.3
    IL_009a:  brtrue.s   IL_00b7

    IL_009c:  nop
    IL_009d:  ldstr      "f6"
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      char MCCTest.VTypeF::f6
    IL_00a8:  conv.u8
    IL_00a9:  ldarga.s   expected
    IL_00ab:  ldfld      char MCCTest.VTypeF::f6
    IL_00b0:  conv.u8
    IL_00b1:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_00b6:  throw

    IL_00b7:  ldarg.0
    IL_00b8:  ldfld      float32 MCCTest.VTypeF::f7
    IL_00bd:  ldarga.s   expected
    IL_00bf:  ldfld      float32 MCCTest.VTypeF::f7
    IL_00c4:  ceq
    IL_00c6:  stloc.3
    IL_00c7:  ldloc.3
    IL_00c8:  brtrue.s   IL_00e5

    IL_00ca:  nop
    IL_00cb:  ldstr      "f7"
    IL_00d0:  ldarg.0
    IL_00d1:  ldfld      float32 MCCTest.VTypeF::f7
    IL_00d6:  conv.r8
    IL_00d7:  ldarga.s   expected
    IL_00d9:  ldfld      float32 MCCTest.VTypeF::f7
    IL_00de:  conv.r8
    IL_00df:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_00e4:  throw

    IL_00e5:  ldarg.0
    IL_00e6:  ldfld      int64 MCCTest.VTypeF::f12
    IL_00eb:  ldarga.s   expected
    IL_00ed:  ldfld      int64 MCCTest.VTypeF::f12
    IL_00f2:  ceq
    IL_00f4:  stloc.3
    IL_00f5:  ldloc.3
    IL_00f6:  brtrue.s   IL_0111

    IL_00f8:  nop
    IL_00f9:  ldstr      "f12"
    IL_00fe:  ldarg.0
    IL_00ff:  ldfld      int64 MCCTest.VTypeF::f12
    IL_0104:  ldarga.s   expected
    IL_0106:  ldfld      int64 MCCTest.VTypeF::f12
    IL_010b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_0110:  throw

    IL_0111:  ldarg.0
    IL_0112:  ldfld      uint16 MCCTest.VTypeF::f13
    IL_0117:  ldarga.s   expected
    IL_0119:  ldfld      uint16 MCCTest.VTypeF::f13
    IL_011e:  ceq
    IL_0120:  stloc.3
    IL_0121:  ldloc.3
    IL_0122:  brtrue.s   IL_013f

    IL_0124:  nop
    IL_0125:  ldstr      "f13"
    IL_012a:  ldarg.0
    IL_012b:  ldfld      uint16 MCCTest.VTypeF::f13
    IL_0130:  conv.u8
    IL_0131:  ldarga.s   expected
    IL_0133:  ldfld      uint16 MCCTest.VTypeF::f13
    IL_0138:  conv.u8
    IL_0139:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_013e:  throw

    IL_013f:  ldarg.0
    IL_0140:  ldfld      int32 MCCTest.VTypeF::f18
    IL_0145:  ldarga.s   expected
    IL_0147:  ldfld      int32 MCCTest.VTypeF::f18
    IL_014c:  ceq
    IL_014e:  stloc.3
    IL_014f:  ldloc.3
    IL_0150:  brtrue.s   IL_016d

    IL_0152:  nop
    IL_0153:  ldstr      "f18"
    IL_0158:  ldarg.0
    IL_0159:  ldfld      int32 MCCTest.VTypeF::f18
    IL_015e:  conv.i8
    IL_015f:  ldarga.s   expected
    IL_0161:  ldfld      int32 MCCTest.VTypeF::f18
    IL_0166:  conv.i8
    IL_0167:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_016c:  throw

    IL_016d:  ldarg.0
    IL_016e:  ldfld      float32 MCCTest.VTypeF::f26
    IL_0173:  ldarga.s   expected
    IL_0175:  ldfld      float32 MCCTest.VTypeF::f26
    IL_017a:  ceq
    IL_017c:  stloc.3
    IL_017d:  ldloc.3
    IL_017e:  brtrue.s   IL_019b

    IL_0180:  nop
    IL_0181:  ldstr      "f26"
    IL_0186:  ldarg.0
    IL_0187:  ldfld      float32 MCCTest.VTypeF::f26
    IL_018c:  conv.r8
    IL_018d:  ldarga.s   expected
    IL_018f:  ldfld      float32 MCCTest.VTypeF::f26
    IL_0194:  conv.r8
    IL_0195:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_019a:  throw

    IL_019b:  ldarg.0
    IL_019c:  ldfld      uint8 MCCTest.VTypeF::f27
    IL_01a1:  ldarga.s   expected
    IL_01a3:  ldfld      uint8 MCCTest.VTypeF::f27
    IL_01a8:  ceq
    IL_01aa:  stloc.3
    IL_01ab:  ldloc.3
    IL_01ac:  brtrue.s   IL_01c9

    IL_01ae:  nop
    IL_01af:  ldstr      "f27"
    IL_01b4:  ldarg.0
    IL_01b5:  ldfld      uint8 MCCTest.VTypeF::f27
    IL_01ba:  conv.u8
    IL_01bb:  ldarga.s   expected
    IL_01bd:  ldfld      uint8 MCCTest.VTypeF::f27
    IL_01c2:  conv.u8
    IL_01c3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_01c8:  throw

    IL_01c9:  ldarg.0
    IL_01ca:  ldfld      float64 MCCTest.VTypeF::f28
    IL_01cf:  ldarga.s   expected
    IL_01d1:  ldfld      float64 MCCTest.VTypeF::f28
    IL_01d6:  ceq
    IL_01d8:  stloc.3
    IL_01d9:  ldloc.3
    IL_01da:  brtrue.s   IL_01f5

    IL_01dc:  nop
    IL_01dd:  ldstr      "f28"
    IL_01e2:  ldarg.0
    IL_01e3:  ldfld      float64 MCCTest.VTypeF::f28
    IL_01e8:  ldarga.s   expected
    IL_01ea:  ldfld      float64 MCCTest.VTypeF::f28
    IL_01ef:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_01f4:  throw

    IL_01f5:  ldarg.0
    IL_01f6:  ldfld      float64 MCCTest.VTypeF::f31
    IL_01fb:  ldarga.s   expected
    IL_01fd:  ldfld      float64 MCCTest.VTypeF::f31
    IL_0202:  ceq
    IL_0204:  stloc.3
    IL_0205:  ldloc.3
    IL_0206:  brtrue.s   IL_0221

    IL_0208:  nop
    IL_0209:  ldstr      "f31"
    IL_020e:  ldarg.0
    IL_020f:  ldfld      float64 MCCTest.VTypeF::f31
    IL_0214:  ldarga.s   expected
    IL_0216:  ldfld      float64 MCCTest.VTypeF::f31
    IL_021b:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_0220:  throw

    IL_0221:  ldarg.0
    IL_0222:  ldfld      int8 MCCTest.VTypeF::f33
    IL_0227:  ldarga.s   expected
    IL_0229:  ldfld      int8 MCCTest.VTypeF::f33
    IL_022e:  ceq
    IL_0230:  stloc.3
    IL_0231:  ldloc.3
    IL_0232:  brtrue.s   IL_024f

    IL_0234:  nop
    IL_0235:  ldstr      "f33"
    IL_023a:  ldarg.0
    IL_023b:  ldfld      int8 MCCTest.VTypeF::f33
    IL_0240:  conv.i8
    IL_0241:  ldarga.s   expected
    IL_0243:  ldfld      int8 MCCTest.VTypeF::f33
    IL_0248:  conv.i8
    IL_0249:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_024e:  throw

    IL_024f:  ldarg.0
    IL_0250:  ldfld      float32 MCCTest.VTypeF::f40
    IL_0255:  ldarga.s   expected
    IL_0257:  ldfld      float32 MCCTest.VTypeF::f40
    IL_025c:  ceq
    IL_025e:  stloc.3
    IL_025f:  ldloc.3
    IL_0260:  brtrue.s   IL_027d

    IL_0262:  nop
    IL_0263:  ldstr      "f40"
    IL_0268:  ldarg.0
    IL_0269:  ldfld      float32 MCCTest.VTypeF::f40
    IL_026e:  conv.r8
    IL_026f:  ldarga.s   expected
    IL_0271:  ldfld      float32 MCCTest.VTypeF::f40
    IL_0276:  conv.r8
    IL_0277:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  float64,
                                                                                  float64)
    IL_027c:  throw

    IL_027d:  ldarg.0
    IL_027e:  ldfld      int32 MCCTest.VTypeF::f41
    IL_0283:  ldarga.s   expected
    IL_0285:  ldfld      int32 MCCTest.VTypeF::f41
    IL_028a:  ceq
    IL_028c:  stloc.3
    IL_028d:  ldloc.3
    IL_028e:  brtrue.s   IL_02ab

    IL_0290:  nop
    IL_0291:  ldstr      "f41"
    IL_0296:  ldarg.0
    IL_0297:  ldfld      int32 MCCTest.VTypeF::f41
    IL_029c:  conv.i8
    IL_029d:  ldarga.s   expected
    IL_029f:  ldfld      int32 MCCTest.VTypeF::f41
    IL_02a4:  conv.i8
    IL_02a5:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_02aa:  throw

    IL_02ab:  ldarg.0
    IL_02ac:  ldfld      char MCCTest.VTypeF::f42
    IL_02b1:  ldarga.s   expected
    IL_02b3:  ldfld      char MCCTest.VTypeF::f42
    IL_02b8:  ceq
    IL_02ba:  stloc.3
    IL_02bb:  ldloc.3
    IL_02bc:  brtrue.s   IL_02d9

    IL_02be:  nop
    IL_02bf:  ldstr      "f42"
    IL_02c4:  ldarg.0
    IL_02c5:  ldfld      char MCCTest.VTypeF::f42
    IL_02ca:  conv.u8
    IL_02cb:  ldarga.s   expected
    IL_02cd:  ldfld      char MCCTest.VTypeF::f42
    IL_02d2:  conv.u8
    IL_02d3:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                  int64,
                                                                                  int64)
    IL_02d8:  throw

    IL_02d9:  ldnull
    IL_02da:  stloc.0
    IL_02db:  ldnull
    IL_02dc:  stloc.1
    .try
    {
      IL_02dd:  nop
      IL_02de:  ldstr      "f4"
      IL_02e3:  stloc.0
      IL_02e4:  ldarg.0
      IL_02e5:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f4
      IL_02ea:  box        MCCTest.VType9
      IL_02ef:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_02f4:  stloc.1
      IL_02f5:  ldarg.0
      IL_02f6:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f4
      IL_02fb:  ldarga.s   expected
      IL_02fd:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f4
      IL_0302:  call       instance void MCCTest.VType9::Check(valuetype MCCTest.VType9)
      IL_0307:  nop
      IL_0308:  ldstr      "f5"
      IL_030d:  stloc.0
      IL_030e:  ldarg.0
      IL_030f:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f5
      IL_0314:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0319:  stloc.1
      IL_031a:  ldarg.0
      IL_031b:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f5
      IL_0320:  ldarga.s   expected
      IL_0322:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f5
      IL_0327:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_032c:  nop
      IL_032d:  ldstr      "f8"
      IL_0332:  stloc.0
      IL_0333:  ldarg.0
      IL_0334:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f8
      IL_0339:  box        MCCTest.VTypeE
      IL_033e:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0343:  stloc.1
      IL_0344:  ldarg.0
      IL_0345:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f8
      IL_034a:  ldarga.s   expected
      IL_034c:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f8
      IL_0351:  call       instance void MCCTest.VTypeE::Check(valuetype MCCTest.VTypeE)
      IL_0356:  nop
      IL_0357:  ldstr      "f9"
      IL_035c:  stloc.0
      IL_035d:  ldarg.0
      IL_035e:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f9
      IL_0363:  box        MCCTest.VType8
      IL_0368:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_036d:  stloc.1
      IL_036e:  ldarg.0
      IL_036f:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f9
      IL_0374:  ldarga.s   expected
      IL_0376:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f9
      IL_037b:  call       instance void MCCTest.VType8::Check(valuetype MCCTest.VType8)
      IL_0380:  nop
      IL_0381:  ldstr      "f10"
      IL_0386:  stloc.0
      IL_0387:  ldarg.0
      IL_0388:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f10
      IL_038d:  box        MCCTest.VTypeE
      IL_0392:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0397:  stloc.1
      IL_0398:  ldarg.0
      IL_0399:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f10
      IL_039e:  ldarga.s   expected
      IL_03a0:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f10
      IL_03a5:  call       instance void MCCTest.VTypeE::Check(valuetype MCCTest.VTypeE)
      IL_03aa:  nop
      IL_03ab:  ldstr      "f11"
      IL_03b0:  stloc.0
      IL_03b1:  ldarg.0
      IL_03b2:  ldfld      valuetype MCCTest.VType3 MCCTest.VTypeF::f11
      IL_03b7:  box        MCCTest.VType3
      IL_03bc:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_03c1:  stloc.1
      IL_03c2:  ldarg.0
      IL_03c3:  ldflda     valuetype MCCTest.VType3 MCCTest.VTypeF::f11
      IL_03c8:  ldarga.s   expected
      IL_03ca:  ldfld      valuetype MCCTest.VType3 MCCTest.VTypeF::f11
      IL_03cf:  call       instance void MCCTest.VType3::Check(valuetype MCCTest.VType3)
      IL_03d4:  nop
      IL_03d5:  ldstr      "f14"
      IL_03da:  stloc.0
      IL_03db:  ldarg.0
      IL_03dc:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeF::f14
      IL_03e1:  box        MCCTest.VType5
      IL_03e6:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_03eb:  stloc.1
      IL_03ec:  ldarg.0
      IL_03ed:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeF::f14
      IL_03f2:  ldarga.s   expected
      IL_03f4:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeF::f14
      IL_03f9:  call       instance void MCCTest.VType5::Check(valuetype MCCTest.VType5)
      IL_03fe:  nop
      IL_03ff:  ldstr      "f15"
      IL_0404:  stloc.0
      IL_0405:  ldarg.0
      IL_0406:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f15
      IL_040b:  box        MCCTest.VType9
      IL_0410:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0415:  stloc.1
      IL_0416:  ldarg.0
      IL_0417:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f15
      IL_041c:  ldarga.s   expected
      IL_041e:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f15
      IL_0423:  call       instance void MCCTest.VType9::Check(valuetype MCCTest.VType9)
      IL_0428:  nop
      IL_0429:  ldstr      "f16"
      IL_042e:  stloc.0
      IL_042f:  ldarg.0
      IL_0430:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f16
      IL_0435:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_043a:  stloc.1
      IL_043b:  ldarg.0
      IL_043c:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f16
      IL_0441:  ldarga.s   expected
      IL_0443:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f16
      IL_0448:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_044d:  nop
      IL_044e:  ldstr      "f17"
      IL_0453:  stloc.0
      IL_0454:  ldarg.0
      IL_0455:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f17
      IL_045a:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_045f:  stloc.1
      IL_0460:  ldarg.0
      IL_0461:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f17
      IL_0466:  ldarga.s   expected
      IL_0468:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f17
      IL_046d:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_0472:  nop
      IL_0473:  ldstr      "f19"
      IL_0478:  stloc.0
      IL_0479:  ldarg.0
      IL_047a:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f19
      IL_047f:  box        MCCTest.VType8
      IL_0484:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0489:  stloc.1
      IL_048a:  ldarg.0
      IL_048b:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f19
      IL_0490:  ldarga.s   expected
      IL_0492:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f19
      IL_0497:  call       instance void MCCTest.VType8::Check(valuetype MCCTest.VType8)
      IL_049c:  nop
      IL_049d:  ldstr      "f20"
      IL_04a2:  stloc.0
      IL_04a3:  ldarg.0
      IL_04a4:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f20
      IL_04a9:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_04ae:  stloc.1
      IL_04af:  ldarg.0
      IL_04b0:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f20
      IL_04b5:  ldarga.s   expected
      IL_04b7:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f20
      IL_04bc:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_04c1:  nop
      IL_04c2:  ldstr      "f21"
      IL_04c7:  stloc.0
      IL_04c8:  ldarg.0
      IL_04c9:  ldfld      valuetype MCCTest.VTypeA MCCTest.VTypeF::f21
      IL_04ce:  box        MCCTest.VTypeA
      IL_04d3:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_04d8:  stloc.1
      IL_04d9:  ldarg.0
      IL_04da:  ldflda     valuetype MCCTest.VTypeA MCCTest.VTypeF::f21
      IL_04df:  ldarga.s   expected
      IL_04e1:  ldfld      valuetype MCCTest.VTypeA MCCTest.VTypeF::f21
      IL_04e6:  call       instance void MCCTest.VTypeA::Check(valuetype MCCTest.VTypeA)
      IL_04eb:  nop
      IL_04ec:  ldstr      "f22"
      IL_04f1:  stloc.0
      IL_04f2:  ldarg.0
      IL_04f3:  ldfld      valuetype MCCTest.VTypeD MCCTest.VTypeF::f22
      IL_04f8:  box        MCCTest.VTypeD
      IL_04fd:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0502:  stloc.1
      IL_0503:  ldarg.0
      IL_0504:  ldflda     valuetype MCCTest.VTypeD MCCTest.VTypeF::f22
      IL_0509:  ldarga.s   expected
      IL_050b:  ldfld      valuetype MCCTest.VTypeD MCCTest.VTypeF::f22
      IL_0510:  call       instance void MCCTest.VTypeD::Check(valuetype MCCTest.VTypeD)
      IL_0515:  nop
      IL_0516:  ldstr      "f23"
      IL_051b:  stloc.0
      IL_051c:  ldarg.0
      IL_051d:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f23
      IL_0522:  box        MCCTest.VType9
      IL_0527:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_052c:  stloc.1
      IL_052d:  ldarg.0
      IL_052e:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f23
      IL_0533:  ldarga.s   expected
      IL_0535:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f23
      IL_053a:  call       instance void MCCTest.VType9::Check(valuetype MCCTest.VType9)
      IL_053f:  nop
      IL_0540:  ldstr      "f24"
      IL_0545:  stloc.0
      IL_0546:  ldarg.0
      IL_0547:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f24
      IL_054c:  box        MCCTest.VTypeE
      IL_0551:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0556:  stloc.1
      IL_0557:  ldarg.0
      IL_0558:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f24
      IL_055d:  ldarga.s   expected
      IL_055f:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f24
      IL_0564:  call       instance void MCCTest.VTypeE::Check(valuetype MCCTest.VTypeE)
      IL_0569:  nop
      IL_056a:  ldstr      "f25"
      IL_056f:  stloc.0
      IL_0570:  ldarg.0
      IL_0571:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f25
      IL_0576:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_057b:  stloc.1
      IL_057c:  ldarg.0
      IL_057d:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f25
      IL_0582:  ldarga.s   expected
      IL_0584:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f25
      IL_0589:  callvirt   instance void MCCTest.RType4::Check(class MCCTest.RType4)
      IL_058e:  nop
      IL_058f:  ldstr      "f29"
      IL_0594:  stloc.0
      IL_0595:  ldarg.0
      IL_0596:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeF::f29
      IL_059b:  box        MCCTest.VTypeC
      IL_05a0:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_05a5:  stloc.1
      IL_05a6:  ldarg.0
      IL_05a7:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeF::f29
      IL_05ac:  ldarga.s   expected
      IL_05ae:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeF::f29
      IL_05b3:  call       instance void MCCTest.VTypeC::Check(valuetype MCCTest.VTypeC)
      IL_05b8:  nop
      IL_05b9:  ldstr      "f30"
      IL_05be:  stloc.0
      IL_05bf:  ldarg.0
      IL_05c0:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeF::f30
      IL_05c5:  box        MCCTest.VType6
      IL_05ca:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_05cf:  stloc.1
      IL_05d0:  ldarg.0
      IL_05d1:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeF::f30
      IL_05d6:  ldarga.s   expected
      IL_05d8:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeF::f30
      IL_05dd:  call       instance void MCCTest.VType6::Check(valuetype MCCTest.VType6)
      IL_05e2:  nop
      IL_05e3:  ldstr      "f32"
      IL_05e8:  stloc.0
      IL_05e9:  ldarg.0
      IL_05ea:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f32
      IL_05ef:  box        MCCTest.VType8
      IL_05f4:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_05f9:  stloc.1
      IL_05fa:  ldarg.0
      IL_05fb:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f32
      IL_0600:  ldarga.s   expected
      IL_0602:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f32
      IL_0607:  call       instance void MCCTest.VType8::Check(valuetype MCCTest.VType8)
      IL_060c:  nop
      IL_060d:  ldstr      "f34"
      IL_0612:  stloc.0
      IL_0613:  ldarg.0
      IL_0614:  ldfld      valuetype MCCTest.VTypeD MCCTest.VTypeF::f34
      IL_0619:  box        MCCTest.VTypeD
      IL_061e:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0623:  stloc.1
      IL_0624:  ldarg.0
      IL_0625:  ldflda     valuetype MCCTest.VTypeD MCCTest.VTypeF::f34
      IL_062a:  ldarga.s   expected
      IL_062c:  ldfld      valuetype MCCTest.VTypeD MCCTest.VTypeF::f34
      IL_0631:  call       instance void MCCTest.VTypeD::Check(valuetype MCCTest.VTypeD)
      IL_0636:  nop
      IL_0637:  ldstr      "f35"
      IL_063c:  stloc.0
      IL_063d:  ldarg.0
      IL_063e:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f35
      IL_0643:  box        MCCTest.VTypeE
      IL_0648:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_064d:  stloc.1
      IL_064e:  ldarg.0
      IL_064f:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f35
      IL_0654:  ldarga.s   expected
      IL_0656:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f35
      IL_065b:  call       instance void MCCTest.VTypeE::Check(valuetype MCCTest.VTypeE)
      IL_0660:  nop
      IL_0661:  ldstr      "f36"
      IL_0666:  stloc.0
      IL_0667:  ldarg.0
      IL_0668:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f36
      IL_066d:  box        MCCTest.VTypeE
      IL_0672:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0677:  stloc.1
      IL_0678:  ldarg.0
      IL_0679:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f36
      IL_067e:  ldarga.s   expected
      IL_0680:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f36
      IL_0685:  call       instance void MCCTest.VTypeE::Check(valuetype MCCTest.VTypeE)
      IL_068a:  nop
      IL_068b:  ldstr      "f37"
      IL_0690:  stloc.0
      IL_0691:  ldarg.0
      IL_0692:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeF::f37
      IL_0697:  box        MCCTest.VType7
      IL_069c:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_06a1:  stloc.1
      IL_06a2:  ldarg.0
      IL_06a3:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeF::f37
      IL_06a8:  ldarga.s   expected
      IL_06aa:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeF::f37
      IL_06af:  call       instance void MCCTest.VType7::Check(valuetype MCCTest.VType7)
      IL_06b4:  nop
      IL_06b5:  ldstr      "f38"
      IL_06ba:  stloc.0
      IL_06bb:  ldarg.0
      IL_06bc:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f38
      IL_06c1:  box        MCCTest.VType9
      IL_06c6:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_06cb:  stloc.1
      IL_06cc:  ldarg.0
      IL_06cd:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f38
      IL_06d2:  ldarga.s   expected
      IL_06d4:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f38
      IL_06d9:  call       instance void MCCTest.VType9::Check(valuetype MCCTest.VType9)
      IL_06de:  nop
      IL_06df:  ldstr      "f39"
      IL_06e4:  stloc.0
      IL_06e5:  ldarg.0
      IL_06e6:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f39
      IL_06eb:  box        MCCTest.VType8
      IL_06f0:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_06f5:  stloc.1
      IL_06f6:  ldarg.0
      IL_06f7:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f39
      IL_06fc:  ldarga.s   expected
      IL_06fe:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f39
      IL_0703:  call       instance void MCCTest.VType8::Check(valuetype MCCTest.VType8)
      IL_0708:  nop
      IL_0709:  ldstr      "f43"
      IL_070e:  stloc.0
      IL_070f:  ldarg.0
      IL_0710:  ldfld      valuetype MCCTest.VTypeB MCCTest.VTypeF::f43
      IL_0715:  box        MCCTest.VTypeB
      IL_071a:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_071f:  stloc.1
      IL_0720:  ldarg.0
      IL_0721:  ldflda     valuetype MCCTest.VTypeB MCCTest.VTypeF::f43
      IL_0726:  ldarga.s   expected
      IL_0728:  ldfld      valuetype MCCTest.VTypeB MCCTest.VTypeF::f43
      IL_072d:  call       instance void MCCTest.VTypeB::Check(valuetype MCCTest.VTypeB)
      IL_0732:  nop
      IL_0733:  ldstr      "f44"
      IL_0738:  stloc.0
      IL_0739:  ldarg.0
      IL_073a:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f44
      IL_073f:  box        MCCTest.VType8
      IL_0744:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0749:  stloc.1
      IL_074a:  ldarg.0
      IL_074b:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f44
      IL_0750:  ldarga.s   expected
      IL_0752:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f44
      IL_0757:  call       instance void MCCTest.VType8::Check(valuetype MCCTest.VType8)
      IL_075c:  nop
      IL_075d:  nop
      IL_075e:  leave.s    IL_076b

    }  // end .try
    catch MCCTest.ResultVerificationException
    {
      IL_0760:  stloc.2
      IL_0761:  nop
      IL_0762:  ldloc.0
      IL_0763:  ldloc.1
      IL_0764:  ldloc.2
      IL_0765:  newobj     instance void MCCTest.ResultVerificationException::.ctor(string,
                                                                                    class [mscorlib]System.Type,
                                                                                    class MCCTest.ResultVerificationException)
      IL_076a:  throw

    }  // end handler
    IL_076b:  nop
    IL_076c:  ret
  } // end of method VTypeF::Check

  .method public hidebysig instance string
          Dump(int32 level) cil managed
  {
    // Code size       2144 (0x860)
    .maxstack  4
    .locals init (string V_0,
             class [mscorlib]System.IO.StringWriter V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string MCCTest.FormatUtils::GetPadding(int32)
    IL_0007:  stloc.0
    IL_0008:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldstr      "f1    = "
    IL_0015:  ldarg.0
    IL_0016:  ldfld      char MCCTest.VTypeF::f1
    IL_001b:  box        [mscorlib]System.Char
    IL_0020:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0025:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_002a:  nop
    IL_002b:  ldloc.1
    IL_002c:  ldloc.0
    IL_002d:  ldstr      "f2    = "
    IL_0032:  ldarg.0
    IL_0033:  ldfld      float64 MCCTest.VTypeF::f2
    IL_0038:  box        [mscorlib]System.Double
    IL_003d:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0042:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0047:  nop
    IL_0048:  ldloc.1
    IL_0049:  ldloc.0
    IL_004a:  ldstr      "f3    = "
    IL_004f:  ldarg.0
    IL_0050:  ldfld      float32 MCCTest.VTypeF::f3
    IL_0055:  box        [mscorlib]System.Single
    IL_005a:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_005f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0064:  nop
    IL_0065:  ldloc.1
    IL_0066:  ldloc.0
    IL_0067:  ldstr      "[Field f4] [Type '{0}']"
    IL_006c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0071:  ldarg.0
    IL_0072:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f4
    IL_0077:  box        MCCTest.VType9
    IL_007c:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0081:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0086:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_008b:  nop
    IL_008c:  ldloc.1
    IL_008d:  ldarg.0
    IL_008e:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f4
    IL_0093:  ldarg.1
    IL_0094:  ldc.i4.1
    IL_0095:  add
    IL_0096:  call       instance string MCCTest.VType9::Dump(int32)
    IL_009b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00a0:  nop
    IL_00a1:  ldloc.1
    IL_00a2:  ldloc.0
    IL_00a3:  ldstr      "[Field f5] [Type '{0}']"
    IL_00a8:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_00ad:  ldarg.0
    IL_00ae:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f5
    IL_00b3:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_00b8:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00bd:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_00c2:  nop
    IL_00c3:  ldloc.1
    IL_00c4:  ldarg.0
    IL_00c5:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f5
    IL_00ca:  ldarg.1
    IL_00cb:  ldc.i4.1
    IL_00cc:  add
    IL_00cd:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_00d2:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00d7:  nop
    IL_00d8:  ldloc.1
    IL_00d9:  ldloc.0
    IL_00da:  ldstr      "f6    = "
    IL_00df:  ldarg.0
    IL_00e0:  ldfld      char MCCTest.VTypeF::f6
    IL_00e5:  box        [mscorlib]System.Char
    IL_00ea:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_00ef:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00f4:  nop
    IL_00f5:  ldloc.1
    IL_00f6:  ldloc.0
    IL_00f7:  ldstr      "f7    = "
    IL_00fc:  ldarg.0
    IL_00fd:  ldfld      float32 MCCTest.VTypeF::f7
    IL_0102:  box        [mscorlib]System.Single
    IL_0107:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_010c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0111:  nop
    IL_0112:  ldloc.1
    IL_0113:  ldloc.0
    IL_0114:  ldstr      "[Field f8] [Type '{0}']"
    IL_0119:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_011e:  ldarg.0
    IL_011f:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f8
    IL_0124:  box        MCCTest.VTypeE
    IL_0129:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_012e:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0133:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0138:  nop
    IL_0139:  ldloc.1
    IL_013a:  ldarg.0
    IL_013b:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f8
    IL_0140:  ldarg.1
    IL_0141:  ldc.i4.1
    IL_0142:  add
    IL_0143:  call       instance string MCCTest.VTypeE::Dump(int32)
    IL_0148:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_014d:  nop
    IL_014e:  ldloc.1
    IL_014f:  ldloc.0
    IL_0150:  ldstr      "[Field f9] [Type '{0}']"
    IL_0155:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_015a:  ldarg.0
    IL_015b:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f9
    IL_0160:  box        MCCTest.VType8
    IL_0165:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_016a:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_016f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0174:  nop
    IL_0175:  ldloc.1
    IL_0176:  ldarg.0
    IL_0177:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f9
    IL_017c:  ldarg.1
    IL_017d:  ldc.i4.1
    IL_017e:  add
    IL_017f:  call       instance string MCCTest.VType8::Dump(int32)
    IL_0184:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0189:  nop
    IL_018a:  ldloc.1
    IL_018b:  ldloc.0
    IL_018c:  ldstr      "[Field f10] [Type '{0}']"
    IL_0191:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0196:  ldarg.0
    IL_0197:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f10
    IL_019c:  box        MCCTest.VTypeE
    IL_01a1:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_01a6:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_01ab:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_01b0:  nop
    IL_01b1:  ldloc.1
    IL_01b2:  ldarg.0
    IL_01b3:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f10
    IL_01b8:  ldarg.1
    IL_01b9:  ldc.i4.1
    IL_01ba:  add
    IL_01bb:  call       instance string MCCTest.VTypeE::Dump(int32)
    IL_01c0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_01c5:  nop
    IL_01c6:  ldloc.1
    IL_01c7:  ldloc.0
    IL_01c8:  ldstr      "[Field f11] [Type '{0}']"
    IL_01cd:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_01d2:  ldarg.0
    IL_01d3:  ldfld      valuetype MCCTest.VType3 MCCTest.VTypeF::f11
    IL_01d8:  box        MCCTest.VType3
    IL_01dd:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_01e2:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_01e7:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_01ec:  nop
    IL_01ed:  ldloc.1
    IL_01ee:  ldarg.0
    IL_01ef:  ldflda     valuetype MCCTest.VType3 MCCTest.VTypeF::f11
    IL_01f4:  ldarg.1
    IL_01f5:  ldc.i4.1
    IL_01f6:  add
    IL_01f7:  call       instance string MCCTest.VType3::Dump(int32)
    IL_01fc:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0201:  nop
    IL_0202:  ldloc.1
    IL_0203:  ldloc.0
    IL_0204:  ldstr      "f12    = "
    IL_0209:  ldarg.0
    IL_020a:  ldfld      int64 MCCTest.VTypeF::f12
    IL_020f:  box        [mscorlib]System.Int64
    IL_0214:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0219:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_021e:  nop
    IL_021f:  ldloc.1
    IL_0220:  ldloc.0
    IL_0221:  ldstr      "f13    = "
    IL_0226:  ldarg.0
    IL_0227:  ldfld      uint16 MCCTest.VTypeF::f13
    IL_022c:  box        [mscorlib]System.UInt16
    IL_0231:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0236:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_023b:  nop
    IL_023c:  ldloc.1
    IL_023d:  ldloc.0
    IL_023e:  ldstr      "[Field f14] [Type '{0}']"
    IL_0243:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0248:  ldarg.0
    IL_0249:  ldfld      valuetype MCCTest.VType5 MCCTest.VTypeF::f14
    IL_024e:  box        MCCTest.VType5
    IL_0253:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0258:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_025d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0262:  nop
    IL_0263:  ldloc.1
    IL_0264:  ldarg.0
    IL_0265:  ldflda     valuetype MCCTest.VType5 MCCTest.VTypeF::f14
    IL_026a:  ldarg.1
    IL_026b:  ldc.i4.1
    IL_026c:  add
    IL_026d:  call       instance string MCCTest.VType5::Dump(int32)
    IL_0272:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0277:  nop
    IL_0278:  ldloc.1
    IL_0279:  ldloc.0
    IL_027a:  ldstr      "[Field f15] [Type '{0}']"
    IL_027f:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0284:  ldarg.0
    IL_0285:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f15
    IL_028a:  box        MCCTest.VType9
    IL_028f:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0294:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0299:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_029e:  nop
    IL_029f:  ldloc.1
    IL_02a0:  ldarg.0
    IL_02a1:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f15
    IL_02a6:  ldarg.1
    IL_02a7:  ldc.i4.1
    IL_02a8:  add
    IL_02a9:  call       instance string MCCTest.VType9::Dump(int32)
    IL_02ae:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02b3:  nop
    IL_02b4:  ldloc.1
    IL_02b5:  ldloc.0
    IL_02b6:  ldstr      "[Field f16] [Type '{0}']"
    IL_02bb:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_02c0:  ldarg.0
    IL_02c1:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f16
    IL_02c6:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_02cb:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_02d0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_02d5:  nop
    IL_02d6:  ldloc.1
    IL_02d7:  ldarg.0
    IL_02d8:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f16
    IL_02dd:  ldarg.1
    IL_02de:  ldc.i4.1
    IL_02df:  add
    IL_02e0:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_02e5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_02ea:  nop
    IL_02eb:  ldloc.1
    IL_02ec:  ldloc.0
    IL_02ed:  ldstr      "[Field f17] [Type '{0}']"
    IL_02f2:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_02f7:  ldarg.0
    IL_02f8:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f17
    IL_02fd:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0302:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0307:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_030c:  nop
    IL_030d:  ldloc.1
    IL_030e:  ldarg.0
    IL_030f:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f17
    IL_0314:  ldarg.1
    IL_0315:  ldc.i4.1
    IL_0316:  add
    IL_0317:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_031c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0321:  nop
    IL_0322:  ldloc.1
    IL_0323:  ldloc.0
    IL_0324:  ldstr      "f18    = "
    IL_0329:  ldarg.0
    IL_032a:  ldfld      int32 MCCTest.VTypeF::f18
    IL_032f:  box        [mscorlib]System.Int32
    IL_0334:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0339:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_033e:  nop
    IL_033f:  ldloc.1
    IL_0340:  ldloc.0
    IL_0341:  ldstr      "[Field f19] [Type '{0}']"
    IL_0346:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_034b:  ldarg.0
    IL_034c:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f19
    IL_0351:  box        MCCTest.VType8
    IL_0356:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_035b:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0360:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0365:  nop
    IL_0366:  ldloc.1
    IL_0367:  ldarg.0
    IL_0368:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f19
    IL_036d:  ldarg.1
    IL_036e:  ldc.i4.1
    IL_036f:  add
    IL_0370:  call       instance string MCCTest.VType8::Dump(int32)
    IL_0375:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_037a:  nop
    IL_037b:  ldloc.1
    IL_037c:  ldloc.0
    IL_037d:  ldstr      "[Field f20] [Type '{0}']"
    IL_0382:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0387:  ldarg.0
    IL_0388:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f20
    IL_038d:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0392:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0397:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_039c:  nop
    IL_039d:  ldloc.1
    IL_039e:  ldarg.0
    IL_039f:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f20
    IL_03a4:  ldarg.1
    IL_03a5:  ldc.i4.1
    IL_03a6:  add
    IL_03a7:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_03ac:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_03b1:  nop
    IL_03b2:  ldloc.1
    IL_03b3:  ldloc.0
    IL_03b4:  ldstr      "[Field f21] [Type '{0}']"
    IL_03b9:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_03be:  ldarg.0
    IL_03bf:  ldfld      valuetype MCCTest.VTypeA MCCTest.VTypeF::f21
    IL_03c4:  box        MCCTest.VTypeA
    IL_03c9:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_03ce:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_03d3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_03d8:  nop
    IL_03d9:  ldloc.1
    IL_03da:  ldarg.0
    IL_03db:  ldflda     valuetype MCCTest.VTypeA MCCTest.VTypeF::f21
    IL_03e0:  ldarg.1
    IL_03e1:  ldc.i4.1
    IL_03e2:  add
    IL_03e3:  call       instance string MCCTest.VTypeA::Dump(int32)
    IL_03e8:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_03ed:  nop
    IL_03ee:  ldloc.1
    IL_03ef:  ldloc.0
    IL_03f0:  ldstr      "[Field f22] [Type '{0}']"
    IL_03f5:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_03fa:  ldarg.0
    IL_03fb:  ldfld      valuetype MCCTest.VTypeD MCCTest.VTypeF::f22
    IL_0400:  box        MCCTest.VTypeD
    IL_0405:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_040a:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_040f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0414:  nop
    IL_0415:  ldloc.1
    IL_0416:  ldarg.0
    IL_0417:  ldflda     valuetype MCCTest.VTypeD MCCTest.VTypeF::f22
    IL_041c:  ldarg.1
    IL_041d:  ldc.i4.1
    IL_041e:  add
    IL_041f:  call       instance string MCCTest.VTypeD::Dump(int32)
    IL_0424:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0429:  nop
    IL_042a:  ldloc.1
    IL_042b:  ldloc.0
    IL_042c:  ldstr      "[Field f23] [Type '{0}']"
    IL_0431:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0436:  ldarg.0
    IL_0437:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f23
    IL_043c:  box        MCCTest.VType9
    IL_0441:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0446:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_044b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0450:  nop
    IL_0451:  ldloc.1
    IL_0452:  ldarg.0
    IL_0453:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f23
    IL_0458:  ldarg.1
    IL_0459:  ldc.i4.1
    IL_045a:  add
    IL_045b:  call       instance string MCCTest.VType9::Dump(int32)
    IL_0460:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0465:  nop
    IL_0466:  ldloc.1
    IL_0467:  ldloc.0
    IL_0468:  ldstr      "[Field f24] [Type '{0}']"
    IL_046d:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0472:  ldarg.0
    IL_0473:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f24
    IL_0478:  box        MCCTest.VTypeE
    IL_047d:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0482:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0487:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_048c:  nop
    IL_048d:  ldloc.1
    IL_048e:  ldarg.0
    IL_048f:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f24
    IL_0494:  ldarg.1
    IL_0495:  ldc.i4.1
    IL_0496:  add
    IL_0497:  call       instance string MCCTest.VTypeE::Dump(int32)
    IL_049c:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_04a1:  nop
    IL_04a2:  ldloc.1
    IL_04a3:  ldloc.0
    IL_04a4:  ldstr      "[Field f25] [Type '{0}']"
    IL_04a9:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_04ae:  ldarg.0
    IL_04af:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f25
    IL_04b4:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_04b9:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_04be:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_04c3:  nop
    IL_04c4:  ldloc.1
    IL_04c5:  ldarg.0
    IL_04c6:  ldfld      class MCCTest.RType4 MCCTest.VTypeF::f25
    IL_04cb:  ldarg.1
    IL_04cc:  ldc.i4.1
    IL_04cd:  add
    IL_04ce:  callvirt   instance string MCCTest.RType4::Dump(int32)
    IL_04d3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_04d8:  nop
    IL_04d9:  ldloc.1
    IL_04da:  ldloc.0
    IL_04db:  ldstr      "f26    = "
    IL_04e0:  ldarg.0
    IL_04e1:  ldfld      float32 MCCTest.VTypeF::f26
    IL_04e6:  box        [mscorlib]System.Single
    IL_04eb:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_04f0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_04f5:  nop
    IL_04f6:  ldloc.1
    IL_04f7:  ldloc.0
    IL_04f8:  ldstr      "f27    = "
    IL_04fd:  ldarg.0
    IL_04fe:  ldfld      uint8 MCCTest.VTypeF::f27
    IL_0503:  box        [mscorlib]System.Byte
    IL_0508:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_050d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0512:  nop
    IL_0513:  ldloc.1
    IL_0514:  ldloc.0
    IL_0515:  ldstr      "f28    = "
    IL_051a:  ldarg.0
    IL_051b:  ldfld      float64 MCCTest.VTypeF::f28
    IL_0520:  box        [mscorlib]System.Double
    IL_0525:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_052a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_052f:  nop
    IL_0530:  ldloc.1
    IL_0531:  ldloc.0
    IL_0532:  ldstr      "[Field f29] [Type '{0}']"
    IL_0537:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_053c:  ldarg.0
    IL_053d:  ldfld      valuetype MCCTest.VTypeC MCCTest.VTypeF::f29
    IL_0542:  box        MCCTest.VTypeC
    IL_0547:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_054c:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0551:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0556:  nop
    IL_0557:  ldloc.1
    IL_0558:  ldarg.0
    IL_0559:  ldflda     valuetype MCCTest.VTypeC MCCTest.VTypeF::f29
    IL_055e:  ldarg.1
    IL_055f:  ldc.i4.1
    IL_0560:  add
    IL_0561:  call       instance string MCCTest.VTypeC::Dump(int32)
    IL_0566:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_056b:  nop
    IL_056c:  ldloc.1
    IL_056d:  ldloc.0
    IL_056e:  ldstr      "[Field f30] [Type '{0}']"
    IL_0573:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0578:  ldarg.0
    IL_0579:  ldfld      valuetype MCCTest.VType6 MCCTest.VTypeF::f30
    IL_057e:  box        MCCTest.VType6
    IL_0583:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0588:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_058d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0592:  nop
    IL_0593:  ldloc.1
    IL_0594:  ldarg.0
    IL_0595:  ldflda     valuetype MCCTest.VType6 MCCTest.VTypeF::f30
    IL_059a:  ldarg.1
    IL_059b:  ldc.i4.1
    IL_059c:  add
    IL_059d:  call       instance string MCCTest.VType6::Dump(int32)
    IL_05a2:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_05a7:  nop
    IL_05a8:  ldloc.1
    IL_05a9:  ldloc.0
    IL_05aa:  ldstr      "f31    = "
    IL_05af:  ldarg.0
    IL_05b0:  ldfld      float64 MCCTest.VTypeF::f31
    IL_05b5:  box        [mscorlib]System.Double
    IL_05ba:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_05bf:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_05c4:  nop
    IL_05c5:  ldloc.1
    IL_05c6:  ldloc.0
    IL_05c7:  ldstr      "[Field f32] [Type '{0}']"
    IL_05cc:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_05d1:  ldarg.0
    IL_05d2:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f32
    IL_05d7:  box        MCCTest.VType8
    IL_05dc:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_05e1:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_05e6:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_05eb:  nop
    IL_05ec:  ldloc.1
    IL_05ed:  ldarg.0
    IL_05ee:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f32
    IL_05f3:  ldarg.1
    IL_05f4:  ldc.i4.1
    IL_05f5:  add
    IL_05f6:  call       instance string MCCTest.VType8::Dump(int32)
    IL_05fb:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0600:  nop
    IL_0601:  ldloc.1
    IL_0602:  ldloc.0
    IL_0603:  ldstr      "f33    = "
    IL_0608:  ldarg.0
    IL_0609:  ldfld      int8 MCCTest.VTypeF::f33
    IL_060e:  box        [mscorlib]System.SByte
    IL_0613:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0618:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_061d:  nop
    IL_061e:  ldloc.1
    IL_061f:  ldloc.0
    IL_0620:  ldstr      "[Field f34] [Type '{0}']"
    IL_0625:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_062a:  ldarg.0
    IL_062b:  ldfld      valuetype MCCTest.VTypeD MCCTest.VTypeF::f34
    IL_0630:  box        MCCTest.VTypeD
    IL_0635:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_063a:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_063f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0644:  nop
    IL_0645:  ldloc.1
    IL_0646:  ldarg.0
    IL_0647:  ldflda     valuetype MCCTest.VTypeD MCCTest.VTypeF::f34
    IL_064c:  ldarg.1
    IL_064d:  ldc.i4.1
    IL_064e:  add
    IL_064f:  call       instance string MCCTest.VTypeD::Dump(int32)
    IL_0654:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0659:  nop
    IL_065a:  ldloc.1
    IL_065b:  ldloc.0
    IL_065c:  ldstr      "[Field f35] [Type '{0}']"
    IL_0661:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0666:  ldarg.0
    IL_0667:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f35
    IL_066c:  box        MCCTest.VTypeE
    IL_0671:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0676:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_067b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0680:  nop
    IL_0681:  ldloc.1
    IL_0682:  ldarg.0
    IL_0683:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f35
    IL_0688:  ldarg.1
    IL_0689:  ldc.i4.1
    IL_068a:  add
    IL_068b:  call       instance string MCCTest.VTypeE::Dump(int32)
    IL_0690:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0695:  nop
    IL_0696:  ldloc.1
    IL_0697:  ldloc.0
    IL_0698:  ldstr      "[Field f36] [Type '{0}']"
    IL_069d:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_06a2:  ldarg.0
    IL_06a3:  ldfld      valuetype MCCTest.VTypeE MCCTest.VTypeF::f36
    IL_06a8:  box        MCCTest.VTypeE
    IL_06ad:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_06b2:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_06b7:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_06bc:  nop
    IL_06bd:  ldloc.1
    IL_06be:  ldarg.0
    IL_06bf:  ldflda     valuetype MCCTest.VTypeE MCCTest.VTypeF::f36
    IL_06c4:  ldarg.1
    IL_06c5:  ldc.i4.1
    IL_06c6:  add
    IL_06c7:  call       instance string MCCTest.VTypeE::Dump(int32)
    IL_06cc:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_06d1:  nop
    IL_06d2:  ldloc.1
    IL_06d3:  ldloc.0
    IL_06d4:  ldstr      "[Field f37] [Type '{0}']"
    IL_06d9:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_06de:  ldarg.0
    IL_06df:  ldfld      valuetype MCCTest.VType7 MCCTest.VTypeF::f37
    IL_06e4:  box        MCCTest.VType7
    IL_06e9:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_06ee:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_06f3:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_06f8:  nop
    IL_06f9:  ldloc.1
    IL_06fa:  ldarg.0
    IL_06fb:  ldflda     valuetype MCCTest.VType7 MCCTest.VTypeF::f37
    IL_0700:  ldarg.1
    IL_0701:  ldc.i4.1
    IL_0702:  add
    IL_0703:  call       instance string MCCTest.VType7::Dump(int32)
    IL_0708:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_070d:  nop
    IL_070e:  ldloc.1
    IL_070f:  ldloc.0
    IL_0710:  ldstr      "[Field f38] [Type '{0}']"
    IL_0715:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_071a:  ldarg.0
    IL_071b:  ldfld      valuetype MCCTest.VType9 MCCTest.VTypeF::f38
    IL_0720:  box        MCCTest.VType9
    IL_0725:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_072a:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_072f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0734:  nop
    IL_0735:  ldloc.1
    IL_0736:  ldarg.0
    IL_0737:  ldflda     valuetype MCCTest.VType9 MCCTest.VTypeF::f38
    IL_073c:  ldarg.1
    IL_073d:  ldc.i4.1
    IL_073e:  add
    IL_073f:  call       instance string MCCTest.VType9::Dump(int32)
    IL_0744:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0749:  nop
    IL_074a:  ldloc.1
    IL_074b:  ldloc.0
    IL_074c:  ldstr      "[Field f39] [Type '{0}']"
    IL_0751:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0756:  ldarg.0
    IL_0757:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f39
    IL_075c:  box        MCCTest.VType8
    IL_0761:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0766:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_076b:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0770:  nop
    IL_0771:  ldloc.1
    IL_0772:  ldarg.0
    IL_0773:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f39
    IL_0778:  ldarg.1
    IL_0779:  ldc.i4.1
    IL_077a:  add
    IL_077b:  call       instance string MCCTest.VType8::Dump(int32)
    IL_0780:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0785:  nop
    IL_0786:  ldloc.1
    IL_0787:  ldloc.0
    IL_0788:  ldstr      "f40    = "
    IL_078d:  ldarg.0
    IL_078e:  ldfld      float32 MCCTest.VTypeF::f40
    IL_0793:  box        [mscorlib]System.Single
    IL_0798:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_079d:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_07a2:  nop
    IL_07a3:  ldloc.1
    IL_07a4:  ldloc.0
    IL_07a5:  ldstr      "f41    = "
    IL_07aa:  ldarg.0
    IL_07ab:  ldfld      int32 MCCTest.VTypeF::f41
    IL_07b0:  box        [mscorlib]System.Int32
    IL_07b5:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_07ba:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_07bf:  nop
    IL_07c0:  ldloc.1
    IL_07c1:  ldloc.0
    IL_07c2:  ldstr      "f42    = "
    IL_07c7:  ldarg.0
    IL_07c8:  ldfld      char MCCTest.VTypeF::f42
    IL_07cd:  box        [mscorlib]System.Char
    IL_07d2:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_07d7:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_07dc:  nop
    IL_07dd:  ldloc.1
    IL_07de:  ldloc.0
    IL_07df:  ldstr      "[Field f43] [Type '{0}']"
    IL_07e4:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_07e9:  ldarg.0
    IL_07ea:  ldfld      valuetype MCCTest.VTypeB MCCTest.VTypeF::f43
    IL_07ef:  box        MCCTest.VTypeB
    IL_07f4:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_07f9:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_07fe:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_0803:  nop
    IL_0804:  ldloc.1
    IL_0805:  ldarg.0
    IL_0806:  ldflda     valuetype MCCTest.VTypeB MCCTest.VTypeF::f43
    IL_080b:  ldarg.1
    IL_080c:  ldc.i4.1
    IL_080d:  add
    IL_080e:  call       instance string MCCTest.VTypeB::Dump(int32)
    IL_0813:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0818:  nop
    IL_0819:  ldloc.1
    IL_081a:  ldloc.0
    IL_081b:  ldstr      "[Field f44] [Type '{0}']"
    IL_0820:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0825:  ldarg.0
    IL_0826:  ldfld      valuetype MCCTest.VType8 MCCTest.VTypeF::f44
    IL_082b:  box        MCCTest.VType8
    IL_0830:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0835:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_083a:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string,
                                                                                 object)
    IL_083f:  nop
    IL_0840:  ldloc.1
    IL_0841:  ldarg.0
    IL_0842:  ldflda     valuetype MCCTest.VType8 MCCTest.VTypeF::f44
    IL_0847:  ldarg.1
    IL_0848:  ldc.i4.1
    IL_0849:  add
    IL_084a:  call       instance string MCCTest.VType8::Dump(int32)
    IL_084f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0854:  nop
    IL_0855:  ldloc.1
    IL_0856:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_085b:  stloc.2
    IL_085c:  br.s       IL_085e

    IL_085e:  ldloc.2
    IL_085f:  ret
  } // end of method VTypeF::Dump

  .method public hidebysig instance string
          Dump() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  call       instance string MCCTest.VTypeF::Dump(int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method VTypeF::Dump

} // end of class MCCTest.VTypeF

.class public auto ansi beforefieldinit MCCTest.Common
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VType0 actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VType0>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VType1 actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VType1>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VType2 actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VType2>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VType3 actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VType3>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(class MCCTest.RType4 actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<class MCCTest.RType4>::CheckResult(!0,
                                                                                          int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VType5 actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VType5>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VType6 actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VType6>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VType7 actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VType7>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VType8 actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VType8>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VType9 actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VType9>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VTypeA actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VTypeA>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VTypeB actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VTypeB>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VTypeC actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VTypeC>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VTypeD actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VTypeD>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VTypeE actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VTypeE>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig static int32  CheckResult(valuetype MCCTest.VTypeF actual,
                                                     int32 count) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  call       int32 class MCCTest.Common2`1<valuetype MCCTest.VTypeF>::CheckResult(!0,
                                                                                              int32)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Common::CheckResult

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Common::.ctor

} // end of class MCCTest.Common

.class public auto ansi beforefieldinit MCCTest.Common2`1<.ctor (class MCCTest.CType`1<!T>) T>
       extends [mscorlib]System.Object
{
  .field private static literal int32 SUCC_RET_CODE = int32(0x00000064)
  .field private static literal int32 FAIL_RET_CODE = int32(0x00000001)
  .method public hidebysig static int32  CheckResult(!T actual,
                                                     int32 count) cil managed
  {
    // Code size       121 (0x79)
    .maxstack  2
    .locals init (int32 V_0,
             !T V_1,
             class MCCTest.ResultVerificationException V_2,
             int32 V_3,
             !T V_4)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    .try
    {
      IL_0003:  nop
      IL_0004:  ldloca.s   V_4
      IL_0006:  initobj    !T
      IL_000c:  ldloc.s    V_4
      IL_000e:  box        !T
      IL_0013:  brfalse.s  IL_0021

      IL_0015:  ldloca.s   V_4
      IL_0017:  initobj    !T
      IL_001d:  ldloc.s    V_4
      IL_001f:  br.s       IL_0026

      IL_0021:  call       !!0 [mscorlib]System.Activator::CreateInstance<!T>()
      IL_0026:  stloc.1
      IL_0027:  ldloca.s   V_1
      IL_0029:  ldarg.1
      IL_002a:  constrained. !T
      IL_0030:  callvirt   instance void class MCCTest.CType`1<!T>::Init(int32)
      IL_0035:  nop
      IL_0036:  ldarga.s   actual
      IL_0038:  ldloc.1
      IL_0039:  constrained. !T
      IL_003f:  callvirt   instance void class MCCTest.CType`1<!T>::Check(!0)
      IL_0044:  nop
      IL_0045:  ldstr      "PASSED"
      IL_004a:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_004f:  nop
      IL_0050:  ldc.i4.s   100
      IL_0052:  stloc.0
      IL_0053:  nop
      IL_0054:  leave.s    IL_0072

    }  // end .try
    catch MCCTest.ResultVerificationException
    {
      IL_0056:  stloc.2
      IL_0057:  nop
      IL_0058:  ldloc.2
      IL_0059:  callvirt   instance string MCCTest.ResultVerificationException::GetMessage()
      IL_005e:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0063:  nop
      IL_0064:  ldstr      "FAILED"
      IL_0069:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_006e:  nop
      IL_006f:  nop
      IL_0070:  leave.s    IL_0072

    }  // end handler
    IL_0072:  nop
    IL_0073:  ldloc.0
    IL_0074:  stloc.3
    IL_0075:  br.s       IL_0077

    IL_0077:  ldloc.3
    IL_0078:  ret
  } // end of method Common2`1::CheckResult

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Common2`1::.ctor

} // end of class MCCTest.Common2`1


// =============================================================

.custom ([mscorlib]System.Runtime.CompilerServices.AssemblyAttributesGoHere) instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                                                                                63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
// *********** DISASSEMBLY COMPLETE ***********************
