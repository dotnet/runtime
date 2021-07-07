// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { auto }
.assembly extern System.Runtime { auto }
.assembly LongMulOn32BitTest { }

.class auto LongMulOn32BitTest extends [System.Runtime]System.Object
{
  .method private hidebysig static void PrintErrorWithResult(string message, int64 result) cil managed
  {
    call class [System.Runtime]System.IO.TextWriter [System.Console]System.Console::get_Error()
    ldarg message
    ldarg result
    box [System.Runtime]System.Int64
    callvirt instance void [System.Runtime]System.IO.TextWriter::WriteLine(string, object)
    ret
  }

  .method private hidebysig static void PrintError(string message) cil managed
  {
    call class [System.Runtime]System.IO.TextWriter [System.Console]System.Console::get_Error()
    ldarg message
    callvirt instance void [System.Runtime]System.IO.TextWriter::WriteLine(string)
    ret
  }

  .method private hidebysig static int32 Main() cil managed
  {
    .entrypoint
    .locals ( int64 result )

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_1(int32)
    stloc result
    ldloc result
    ldc.i8 4611686018427387904
    ceq
    brtrue NEXT1
    ldstr "'LongMulOn32BitTest::LongMul_1' returned: '{0}'. Expected: '4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT1:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_1(int32)
    stloc result
    ldloc result
    ldc.i8 2147483648
    ceq
    brtrue NEXT2
    ldstr "'LongMulOn32BitTest::LongMul_1' returned: '{0}'. Expected: '2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT2:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_1(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT3
    ldstr "'LongMulOn32BitTest::LongMul_1' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT3:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_1(int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT4
    ldstr "'LongMulOn32BitTest::LongMul_1' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT4:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_2(int32)
    stloc result
    ldloc result
    ldc.i8 4611686018427387904
    ceq
    brtrue NEXT5
    ldstr "'LongMulOn32BitTest::LongMul_2' returned: '{0}'. Expected: '4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT5:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_2(int32)
    stloc result
    ldloc result
    ldc.i8 2147483648
    ceq
    brtrue NEXT6
    ldstr "'LongMulOn32BitTest::LongMul_2' returned: '{0}'. Expected: '2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT6:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_2(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT7
    ldstr "'LongMulOn32BitTest::LongMul_2' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT7:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_2(int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT8
    ldstr "'LongMulOn32BitTest::LongMul_2' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT8:

    .try
    {
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_3(int32)
      ldstr "'LongMulOn32BitTest::LongMul_3' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT9
    }
  NEXT9:

    .try
    {
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_3(int32)
      ldstr "'LongMulOn32BitTest::LongMul_3' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT10
    }
  NEXT10:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_3(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT11
    ldstr "'LongMulOn32BitTest::LongMul_3' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT11:

    .try
    {
      ldc.i4 2147483647
      call int64 LongMulOn32BitTest::LongMul_3(int32)
      ldstr "'LongMulOn32BitTest::LongMul_3' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT12
    }
  NEXT12:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_4(int32)
    stloc result
    ldloc result
    ldc.i8 2147483648
    ceq
    brtrue NEXT13
    ldstr "'LongMulOn32BitTest::LongMul_4' returned: '{0}'. Expected: '2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT13:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_4(int32)
    stloc result
    ldloc result
    ldc.i8 1
    ceq
    brtrue NEXT14
    ldstr "'LongMulOn32BitTest::LongMul_4' returned: '{0}'. Expected: '1'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT14:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_4(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT15
    ldstr "'LongMulOn32BitTest::LongMul_4' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT15:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_4(int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT16
    ldstr "'LongMulOn32BitTest::LongMul_4' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT16:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_5(int32)
    stloc result
    ldloc result
    ldc.i8 2147483648
    ceq
    brtrue NEXT17
    ldstr "'LongMulOn32BitTest::LongMul_5' returned: '{0}'. Expected: '2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT17:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_5(int32)
    stloc result
    ldloc result
    ldc.i8 1
    ceq
    brtrue NEXT18
    ldstr "'LongMulOn32BitTest::LongMul_5' returned: '{0}'. Expected: '1'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT18:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_5(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT19
    ldstr "'LongMulOn32BitTest::LongMul_5' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT19:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_5(int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT20
    ldstr "'LongMulOn32BitTest::LongMul_5' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT20:

    .try
    {
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_6(int32)
      ldstr "'LongMulOn32BitTest::LongMul_6' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT21
    }
  NEXT21:

    .try
    {
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_6(int32)
      ldstr "'LongMulOn32BitTest::LongMul_6' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT22
    }
  NEXT22:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_6(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT23
    ldstr "'LongMulOn32BitTest::LongMul_6' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT23:

    .try
    {
      ldc.i4 2147483647
      call int64 LongMulOn32BitTest::LongMul_6(int32)
      ldstr "'LongMulOn32BitTest::LongMul_6' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT24
    }
  NEXT24:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_7(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT25
    ldstr "'LongMulOn32BitTest::LongMul_7' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT25:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_7(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT26
    ldstr "'LongMulOn32BitTest::LongMul_7' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT26:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_7(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT27
    ldstr "'LongMulOn32BitTest::LongMul_7' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT27:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_7(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT28
    ldstr "'LongMulOn32BitTest::LongMul_7' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT28:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_8(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT29
    ldstr "'LongMulOn32BitTest::LongMul_8' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT29:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_8(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT30
    ldstr "'LongMulOn32BitTest::LongMul_8' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT30:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_8(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT31
    ldstr "'LongMulOn32BitTest::LongMul_8' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT31:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_8(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT32
    ldstr "'LongMulOn32BitTest::LongMul_8' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT32:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_9(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT33
    ldstr "'LongMulOn32BitTest::LongMul_9' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT33:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_9(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT34
    ldstr "'LongMulOn32BitTest::LongMul_9' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT34:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_9(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT35
    ldstr "'LongMulOn32BitTest::LongMul_9' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT35:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_9(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT36
    ldstr "'LongMulOn32BitTest::LongMul_9' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT36:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_10(int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT37
    ldstr "'LongMulOn32BitTest::LongMul_10' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT37:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_10(int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT38
    ldstr "'LongMulOn32BitTest::LongMul_10' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT38:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_10(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT39
    ldstr "'LongMulOn32BitTest::LongMul_10' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT39:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_10(int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT40
    ldstr "'LongMulOn32BitTest::LongMul_10' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT40:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_11(int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT41
    ldstr "'LongMulOn32BitTest::LongMul_11' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT41:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_11(int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT42
    ldstr "'LongMulOn32BitTest::LongMul_11' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT42:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_11(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT43
    ldstr "'LongMulOn32BitTest::LongMul_11' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT43:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_11(int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT44
    ldstr "'LongMulOn32BitTest::LongMul_11' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT44:

    .try
    {
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_12(int32)
      ldstr "'LongMulOn32BitTest::LongMul_12' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT45
    }
  NEXT45:

    .try
    {
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_12(int32)
      ldstr "'LongMulOn32BitTest::LongMul_12' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT46
    }
  NEXT46:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_12(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT47
    ldstr "'LongMulOn32BitTest::LongMul_12' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT47:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_12(int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT48
    ldstr "'LongMulOn32BitTest::LongMul_12' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT48:

    ldc.i4 -2147483648
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686018427387904
    ceq
    brtrue NEXT49
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT49:

    ldc.i4 -2147483648
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 2147483648
    ceq
    brtrue NEXT50
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT50:

    ldc.i4 -2147483648
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT51
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT51:

    ldc.i4 -2147483648
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT52
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT52:

    ldc.i4 -1
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 2147483648
    ceq
    brtrue NEXT53
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT53:

    ldc.i4 -1
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 1
    ceq
    brtrue NEXT54
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '1'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT54:

    ldc.i4 -1
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT55
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT55:

    ldc.i4 -1
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT56
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT56:

    ldc.i4 0
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT57
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT57:

    ldc.i4 0
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT58
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT58:

    ldc.i4 0
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT59
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT59:

    ldc.i4 0
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT60
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT60:

    ldc.i4 2147483647
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT61
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT61:

    ldc.i4 2147483647
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT62
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT62:

    ldc.i4 2147483647
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT63
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT63:

    ldc.i4 2147483647
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_13(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT64
    ldstr "'LongMulOn32BitTest::LongMul_13' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT64:

    ldc.i4 -2147483648
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686018427387904
    ceq
    brtrue NEXT65
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT65:

    ldc.i4 -2147483648
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 2147483648
    ceq
    brtrue NEXT66
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT66:

    ldc.i4 -2147483648
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT67
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT67:

    ldc.i4 -2147483648
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT68
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT68:

    ldc.i4 -1
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 2147483648
    ceq
    brtrue NEXT69
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT69:

    ldc.i4 -1
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 1
    ceq
    brtrue NEXT70
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '1'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT70:

    ldc.i4 -1
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT71
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT71:

    ldc.i4 -1
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT72
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT72:

    ldc.i4 0
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT73
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT73:

    ldc.i4 0
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT74
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT74:

    ldc.i4 0
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT75
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT75:

    ldc.i4 0
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT76
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT76:

    ldc.i4 2147483647
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT77
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT77:

    ldc.i4 2147483647
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT78
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT78:

    ldc.i4 2147483647
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT79
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT79:

    ldc.i4 2147483647
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_14(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT80
    ldstr "'LongMulOn32BitTest::LongMul_14' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT80:

    .try
    {
      ldc.i4 -2147483648
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_15' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT81
    }
  NEXT81:

    .try
    {
      ldc.i4 -2147483648
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_15' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT82
    }
  NEXT82:

    ldc.i4 -2147483648
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT83
    ldstr "'LongMulOn32BitTest::LongMul_15' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT83:

    .try
    {
      ldc.i4 -2147483648
      ldc.i4 2147483647
      call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_15' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT84
    }
  NEXT84:

    .try
    {
      ldc.i4 -1
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_15' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT85
    }
  NEXT85:

    .try
    {
      ldc.i4 -1
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_15' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT86
    }
  NEXT86:

    ldc.i4 -1
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT87
    ldstr "'LongMulOn32BitTest::LongMul_15' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT87:

    .try
    {
      ldc.i4 -1
      ldc.i4 2147483647
      call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_15' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT88
    }
  NEXT88:

    ldc.i4 0
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT89
    ldstr "'LongMulOn32BitTest::LongMul_15' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT89:

    ldc.i4 0
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT90
    ldstr "'LongMulOn32BitTest::LongMul_15' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT90:

    ldc.i4 0
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT91
    ldstr "'LongMulOn32BitTest::LongMul_15' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT91:

    ldc.i4 0
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT92
    ldstr "'LongMulOn32BitTest::LongMul_15' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT92:

    .try
    {
      ldc.i4 2147483647
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_15' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT93
    }
  NEXT93:

    .try
    {
      ldc.i4 2147483647
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_15' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT94
    }
  NEXT94:

    ldc.i4 2147483647
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT95
    ldstr "'LongMulOn32BitTest::LongMul_15' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT95:

    ldc.i4 2147483647
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_15(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT96
    ldstr "'LongMulOn32BitTest::LongMul_15' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT96:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_16(int32)
    stloc result
    ldloc result
    ldc.i8 -4611686018427387904
    ceq
    brtrue NEXT97
    ldstr "'LongMulOn32BitTest::LongMul_16' returned: '{0}'. Expected: '-4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT97:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_16(int32)
    stloc result
    ldloc result
    ldc.i8 -2147483648
    ceq
    brtrue NEXT98
    ldstr "'LongMulOn32BitTest::LongMul_16' returned: '{0}'. Expected: '-2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT98:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_16(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT99
    ldstr "'LongMulOn32BitTest::LongMul_16' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT99:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_16(int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT100
    ldstr "'LongMulOn32BitTest::LongMul_16' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT100:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_17(int32)
    stloc result
    ldloc result
    ldc.i8 -4611686018427387904
    ceq
    brtrue NEXT101
    ldstr "'LongMulOn32BitTest::LongMul_17' returned: '{0}'. Expected: '-4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT101:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_17(int32)
    stloc result
    ldloc result
    ldc.i8 -2147483648
    ceq
    brtrue NEXT102
    ldstr "'LongMulOn32BitTest::LongMul_17' returned: '{0}'. Expected: '-2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT102:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_17(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT103
    ldstr "'LongMulOn32BitTest::LongMul_17' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT103:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_17(int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT104
    ldstr "'LongMulOn32BitTest::LongMul_17' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT104:

    .try
    {
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_18(int32)
      ldstr "'LongMulOn32BitTest::LongMul_18' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT105
    }
  NEXT105:

    .try
    {
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_18(int32)
      ldstr "'LongMulOn32BitTest::LongMul_18' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT106
    }
  NEXT106:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_18(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT107
    ldstr "'LongMulOn32BitTest::LongMul_18' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT107:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_18(int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT108
    ldstr "'LongMulOn32BitTest::LongMul_18' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT108:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_19(int32)
    stloc result
    ldloc result
    ldc.i8 -9223372034707292160
    ceq
    brtrue NEXT109
    ldstr "'LongMulOn32BitTest::LongMul_19' returned: '{0}'. Expected: '-9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT109:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_19(int32)
    stloc result
    ldloc result
    ldc.i8 -4294967295
    ceq
    brtrue NEXT110
    ldstr "'LongMulOn32BitTest::LongMul_19' returned: '{0}'. Expected: '-4294967295'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT110:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_19(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT111
    ldstr "'LongMulOn32BitTest::LongMul_19' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT111:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_19(int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT112
    ldstr "'LongMulOn32BitTest::LongMul_19' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT112:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_20(int32)
    stloc result
    ldloc result
    ldc.i8 -9223372034707292160
    ceq
    brtrue NEXT113
    ldstr "'LongMulOn32BitTest::LongMul_20' returned: '{0}'. Expected: '-9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT113:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_20(int32)
    stloc result
    ldloc result
    ldc.i8 -4294967295
    ceq
    brtrue NEXT114
    ldstr "'LongMulOn32BitTest::LongMul_20' returned: '{0}'. Expected: '-4294967295'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT114:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_20(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT115
    ldstr "'LongMulOn32BitTest::LongMul_20' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT115:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_20(int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT116
    ldstr "'LongMulOn32BitTest::LongMul_20' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT116:

    .try
    {
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_21(int32)
      ldstr "'LongMulOn32BitTest::LongMul_21' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT117
    }
  NEXT117:

    .try
    {
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_21(int32)
      ldstr "'LongMulOn32BitTest::LongMul_21' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT118
    }
  NEXT118:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_21(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT119
    ldstr "'LongMulOn32BitTest::LongMul_21' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT119:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_21(int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT120
    ldstr "'LongMulOn32BitTest::LongMul_21' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT120:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_22(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT121
    ldstr "'LongMulOn32BitTest::LongMul_22' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT121:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_22(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT122
    ldstr "'LongMulOn32BitTest::LongMul_22' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT122:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_22(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT123
    ldstr "'LongMulOn32BitTest::LongMul_22' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT123:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_22(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT124
    ldstr "'LongMulOn32BitTest::LongMul_22' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT124:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_23(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT125
    ldstr "'LongMulOn32BitTest::LongMul_23' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT125:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_23(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT126
    ldstr "'LongMulOn32BitTest::LongMul_23' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT126:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_23(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT127
    ldstr "'LongMulOn32BitTest::LongMul_23' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT127:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_23(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT128
    ldstr "'LongMulOn32BitTest::LongMul_23' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT128:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_24(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT129
    ldstr "'LongMulOn32BitTest::LongMul_24' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT129:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_24(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT130
    ldstr "'LongMulOn32BitTest::LongMul_24' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT130:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_24(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT131
    ldstr "'LongMulOn32BitTest::LongMul_24' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT131:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_24(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT132
    ldstr "'LongMulOn32BitTest::LongMul_24' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT132:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_25(int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT133
    ldstr "'LongMulOn32BitTest::LongMul_25' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT133:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_25(int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT134
    ldstr "'LongMulOn32BitTest::LongMul_25' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT134:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_25(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT135
    ldstr "'LongMulOn32BitTest::LongMul_25' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT135:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_25(int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT136
    ldstr "'LongMulOn32BitTest::LongMul_25' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT136:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_26(int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT137
    ldstr "'LongMulOn32BitTest::LongMul_26' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT137:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_26(int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT138
    ldstr "'LongMulOn32BitTest::LongMul_26' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT138:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_26(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT139
    ldstr "'LongMulOn32BitTest::LongMul_26' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT139:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_26(int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT140
    ldstr "'LongMulOn32BitTest::LongMul_26' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT140:

    .try
    {
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_27(int32)
      ldstr "'LongMulOn32BitTest::LongMul_27' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT141
    }
  NEXT141:

    .try
    {
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_27(int32)
      ldstr "'LongMulOn32BitTest::LongMul_27' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT142
    }
  NEXT142:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_27(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT143
    ldstr "'LongMulOn32BitTest::LongMul_27' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT143:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_27(int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT144
    ldstr "'LongMulOn32BitTest::LongMul_27' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT144:

    ldc.i4 -2147483648
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4611686018427387904
    ceq
    brtrue NEXT145
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '-4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT145:

    ldc.i4 -2147483648
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -9223372034707292160
    ceq
    brtrue NEXT146
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '-9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT146:

    ldc.i4 -2147483648
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT147
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT147:

    ldc.i4 -2147483648
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT148
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT148:

    ldc.i4 -1
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -2147483648
    ceq
    brtrue NEXT149
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '-2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT149:

    ldc.i4 -1
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4294967295
    ceq
    brtrue NEXT150
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '-4294967295'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT150:

    ldc.i4 -1
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT151
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT151:

    ldc.i4 -1
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT152
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT152:

    ldc.i4 0
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT153
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT153:

    ldc.i4 0
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT154
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT154:

    ldc.i4 0
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT155
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT155:

    ldc.i4 0
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT156
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT156:

    ldc.i4 2147483647
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT157
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT157:

    ldc.i4 2147483647
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT158
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT158:

    ldc.i4 2147483647
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT159
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT159:

    ldc.i4 2147483647
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_28(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT160
    ldstr "'LongMulOn32BitTest::LongMul_28' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT160:

    ldc.i4 -2147483648
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4611686018427387904
    ceq
    brtrue NEXT161
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '-4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT161:

    ldc.i4 -2147483648
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -9223372034707292160
    ceq
    brtrue NEXT162
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '-9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT162:

    ldc.i4 -2147483648
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT163
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT163:

    ldc.i4 -2147483648
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT164
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT164:

    ldc.i4 -1
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -2147483648
    ceq
    brtrue NEXT165
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '-2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT165:

    ldc.i4 -1
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4294967295
    ceq
    brtrue NEXT166
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '-4294967295'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT166:

    ldc.i4 -1
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT167
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT167:

    ldc.i4 -1
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT168
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT168:

    ldc.i4 0
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT169
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT169:

    ldc.i4 0
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT170
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT170:

    ldc.i4 0
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT171
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT171:

    ldc.i4 0
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT172
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT172:

    ldc.i4 2147483647
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT173
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT173:

    ldc.i4 2147483647
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT174
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT174:

    ldc.i4 2147483647
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT175
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT175:

    ldc.i4 2147483647
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_29(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT176
    ldstr "'LongMulOn32BitTest::LongMul_29' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT176:

    .try
    {
      ldc.i4 -2147483648
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_30' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT177
    }
  NEXT177:

    .try
    {
      ldc.i4 -2147483648
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_30' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT178
    }
  NEXT178:

    ldc.i4 -2147483648
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT179
    ldstr "'LongMulOn32BitTest::LongMul_30' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT179:

    .try
    {
      ldc.i4 -2147483648
      ldc.i4 2147483647
      call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_30' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT180
    }
  NEXT180:

    .try
    {
      ldc.i4 -1
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_30' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT181
    }
  NEXT181:

    .try
    {
      ldc.i4 -1
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_30' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT182
    }
  NEXT182:

    ldc.i4 -1
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT183
    ldstr "'LongMulOn32BitTest::LongMul_30' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT183:

    .try
    {
      ldc.i4 -1
      ldc.i4 2147483647
      call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_30' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT184
    }
  NEXT184:

    ldc.i4 0
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT185
    ldstr "'LongMulOn32BitTest::LongMul_30' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT185:

    ldc.i4 0
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT186
    ldstr "'LongMulOn32BitTest::LongMul_30' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT186:

    ldc.i4 0
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT187
    ldstr "'LongMulOn32BitTest::LongMul_30' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT187:

    ldc.i4 0
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT188
    ldstr "'LongMulOn32BitTest::LongMul_30' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT188:

    ldc.i4 2147483647
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT189
    ldstr "'LongMulOn32BitTest::LongMul_30' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT189:

    ldc.i4 2147483647
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT190
    ldstr "'LongMulOn32BitTest::LongMul_30' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT190:

    ldc.i4 2147483647
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT191
    ldstr "'LongMulOn32BitTest::LongMul_30' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT191:

    ldc.i4 2147483647
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_30(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT192
    ldstr "'LongMulOn32BitTest::LongMul_30' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT192:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_31(int32)
    stloc result
    ldloc result
    ldc.i8 -4611686018427387904
    ceq
    brtrue NEXT193
    ldstr "'LongMulOn32BitTest::LongMul_31' returned: '{0}'. Expected: '-4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT193:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_31(int32)
    stloc result
    ldloc result
    ldc.i8 -9223372034707292160
    ceq
    brtrue NEXT194
    ldstr "'LongMulOn32BitTest::LongMul_31' returned: '{0}'. Expected: '-9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT194:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_31(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT195
    ldstr "'LongMulOn32BitTest::LongMul_31' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT195:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_31(int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT196
    ldstr "'LongMulOn32BitTest::LongMul_31' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT196:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_32(int32)
    stloc result
    ldloc result
    ldc.i8 -4611686018427387904
    ceq
    brtrue NEXT197
    ldstr "'LongMulOn32BitTest::LongMul_32' returned: '{0}'. Expected: '-4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT197:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_32(int32)
    stloc result
    ldloc result
    ldc.i8 -9223372034707292160
    ceq
    brtrue NEXT198
    ldstr "'LongMulOn32BitTest::LongMul_32' returned: '{0}'. Expected: '-9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT198:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_32(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT199
    ldstr "'LongMulOn32BitTest::LongMul_32' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT199:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_32(int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT200
    ldstr "'LongMulOn32BitTest::LongMul_32' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT200:

    .try
    {
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_33(int32)
      ldstr "'LongMulOn32BitTest::LongMul_33' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT201
    }
  NEXT201:

    .try
    {
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_33(int32)
      ldstr "'LongMulOn32BitTest::LongMul_33' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT202
    }
  NEXT202:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_33(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT203
    ldstr "'LongMulOn32BitTest::LongMul_33' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT203:

    .try
    {
      ldc.i4 2147483647
      call int64 LongMulOn32BitTest::LongMul_33(int32)
      ldstr "'LongMulOn32BitTest::LongMul_33' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT204
    }
  NEXT204:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_34(int32)
    stloc result
    ldloc result
    ldc.i8 -2147483648
    ceq
    brtrue NEXT205
    ldstr "'LongMulOn32BitTest::LongMul_34' returned: '{0}'. Expected: '-2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT205:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_34(int32)
    stloc result
    ldloc result
    ldc.i8 -4294967295
    ceq
    brtrue NEXT206
    ldstr "'LongMulOn32BitTest::LongMul_34' returned: '{0}'. Expected: '-4294967295'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT206:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_34(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT207
    ldstr "'LongMulOn32BitTest::LongMul_34' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT207:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_34(int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT208
    ldstr "'LongMulOn32BitTest::LongMul_34' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT208:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_35(int32)
    stloc result
    ldloc result
    ldc.i8 -2147483648
    ceq
    brtrue NEXT209
    ldstr "'LongMulOn32BitTest::LongMul_35' returned: '{0}'. Expected: '-2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT209:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_35(int32)
    stloc result
    ldloc result
    ldc.i8 -4294967295
    ceq
    brtrue NEXT210
    ldstr "'LongMulOn32BitTest::LongMul_35' returned: '{0}'. Expected: '-4294967295'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT210:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_35(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT211
    ldstr "'LongMulOn32BitTest::LongMul_35' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT211:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_35(int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT212
    ldstr "'LongMulOn32BitTest::LongMul_35' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT212:

    .try
    {
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_36(int32)
      ldstr "'LongMulOn32BitTest::LongMul_36' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT213
    }
  NEXT213:

    .try
    {
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_36(int32)
      ldstr "'LongMulOn32BitTest::LongMul_36' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT214
    }
  NEXT214:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_36(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT215
    ldstr "'LongMulOn32BitTest::LongMul_36' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT215:

    .try
    {
      ldc.i4 2147483647
      call int64 LongMulOn32BitTest::LongMul_36(int32)
      ldstr "'LongMulOn32BitTest::LongMul_36' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT216
    }
  NEXT216:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_37(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT217
    ldstr "'LongMulOn32BitTest::LongMul_37' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT217:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_37(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT218
    ldstr "'LongMulOn32BitTest::LongMul_37' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT218:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_37(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT219
    ldstr "'LongMulOn32BitTest::LongMul_37' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT219:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_37(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT220
    ldstr "'LongMulOn32BitTest::LongMul_37' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT220:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_38(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT221
    ldstr "'LongMulOn32BitTest::LongMul_38' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT221:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_38(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT222
    ldstr "'LongMulOn32BitTest::LongMul_38' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT222:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_38(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT223
    ldstr "'LongMulOn32BitTest::LongMul_38' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT223:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_38(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT224
    ldstr "'LongMulOn32BitTest::LongMul_38' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT224:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_39(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT225
    ldstr "'LongMulOn32BitTest::LongMul_39' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT225:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_39(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT226
    ldstr "'LongMulOn32BitTest::LongMul_39' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT226:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_39(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT227
    ldstr "'LongMulOn32BitTest::LongMul_39' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT227:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_39(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT228
    ldstr "'LongMulOn32BitTest::LongMul_39' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT228:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_40(int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT229
    ldstr "'LongMulOn32BitTest::LongMul_40' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT229:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_40(int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT230
    ldstr "'LongMulOn32BitTest::LongMul_40' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT230:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_40(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT231
    ldstr "'LongMulOn32BitTest::LongMul_40' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT231:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_40(int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT232
    ldstr "'LongMulOn32BitTest::LongMul_40' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT232:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_41(int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT233
    ldstr "'LongMulOn32BitTest::LongMul_41' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT233:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_41(int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT234
    ldstr "'LongMulOn32BitTest::LongMul_41' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT234:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_41(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT235
    ldstr "'LongMulOn32BitTest::LongMul_41' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT235:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_41(int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT236
    ldstr "'LongMulOn32BitTest::LongMul_41' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT236:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_42(int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT237
    ldstr "'LongMulOn32BitTest::LongMul_42' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT237:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_42(int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT238
    ldstr "'LongMulOn32BitTest::LongMul_42' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT238:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_42(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT239
    ldstr "'LongMulOn32BitTest::LongMul_42' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT239:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_42(int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT240
    ldstr "'LongMulOn32BitTest::LongMul_42' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT240:

    ldc.i4 -2147483648
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4611686018427387904
    ceq
    brtrue NEXT241
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '-4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT241:

    ldc.i4 -2147483648
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -2147483648
    ceq
    brtrue NEXT242
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '-2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT242:

    ldc.i4 -2147483648
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT243
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT243:

    ldc.i4 -2147483648
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT244
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT244:

    ldc.i4 -1
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -9223372034707292160
    ceq
    brtrue NEXT245
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '-9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT245:

    ldc.i4 -1
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4294967295
    ceq
    brtrue NEXT246
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '-4294967295'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT246:

    ldc.i4 -1
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT247
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT247:

    ldc.i4 -1
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT248
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT248:

    ldc.i4 0
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT249
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT249:

    ldc.i4 0
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT250
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT250:

    ldc.i4 0
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT251
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT251:

    ldc.i4 0
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT252
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT252:

    ldc.i4 2147483647
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT253
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT253:

    ldc.i4 2147483647
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT254
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT254:

    ldc.i4 2147483647
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT255
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT255:

    ldc.i4 2147483647
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_43(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT256
    ldstr "'LongMulOn32BitTest::LongMul_43' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT256:

    ldc.i4 -2147483648
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4611686018427387904
    ceq
    brtrue NEXT257
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '-4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT257:

    ldc.i4 -2147483648
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -2147483648
    ceq
    brtrue NEXT258
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '-2147483648'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT258:

    ldc.i4 -2147483648
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT259
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT259:

    ldc.i4 -2147483648
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT260
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT260:

    ldc.i4 -1
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -9223372034707292160
    ceq
    brtrue NEXT261
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '-9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT261:

    ldc.i4 -1
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4294967295
    ceq
    brtrue NEXT262
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '-4294967295'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT262:

    ldc.i4 -1
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT263
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT263:

    ldc.i4 -1
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT264
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT264:

    ldc.i4 0
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT265
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT265:

    ldc.i4 0
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT266
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT266:

    ldc.i4 0
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT267
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT267:

    ldc.i4 0
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT268
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT268:

    ldc.i4 2147483647
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -4611686016279904256
    ceq
    brtrue NEXT269
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '-4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT269:

    ldc.i4 2147483647
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -2147483647
    ceq
    brtrue NEXT270
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '-2147483647'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT270:

    ldc.i4 2147483647
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT271
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT271:

    ldc.i4 2147483647
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_44(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT272
    ldstr "'LongMulOn32BitTest::LongMul_44' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT272:

    .try
    {
      ldc.i4 -2147483648
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_45' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT273
    }
  NEXT273:

    .try
    {
      ldc.i4 -2147483648
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_45' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT274
    }
  NEXT274:

    ldc.i4 -2147483648
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT275
    ldstr "'LongMulOn32BitTest::LongMul_45' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT275:

    ldc.i4 -2147483648
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT276
    ldstr "'LongMulOn32BitTest::LongMul_45' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT276:

    .try
    {
      ldc.i4 -1
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_45' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT277
    }
  NEXT277:

    .try
    {
      ldc.i4 -1
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_45' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT278
    }
  NEXT278:

    ldc.i4 -1
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT279
    ldstr "'LongMulOn32BitTest::LongMul_45' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT279:

    ldc.i4 -1
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT280
    ldstr "'LongMulOn32BitTest::LongMul_45' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT280:

    ldc.i4 0
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT281
    ldstr "'LongMulOn32BitTest::LongMul_45' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT281:

    ldc.i4 0
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT282
    ldstr "'LongMulOn32BitTest::LongMul_45' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT282:

    ldc.i4 0
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT283
    ldstr "'LongMulOn32BitTest::LongMul_45' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT283:

    ldc.i4 0
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT284
    ldstr "'LongMulOn32BitTest::LongMul_45' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT284:

    .try
    {
      ldc.i4 2147483647
      ldc.i4 -2147483648
      call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_45' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT285
    }
  NEXT285:

    .try
    {
      ldc.i4 2147483647
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_45' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT286
    }
  NEXT286:

    ldc.i4 2147483647
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT287
    ldstr "'LongMulOn32BitTest::LongMul_45' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT287:

    ldc.i4 2147483647
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_45(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT288
    ldstr "'LongMulOn32BitTest::LongMul_45' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT288:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_46(int32)
    stloc result
    ldloc result
    ldc.i8 4611686018427387904
    ceq
    brtrue NEXT289
    ldstr "'LongMulOn32BitTest::LongMul_46' returned: '{0}'. Expected: '4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT289:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_46(int32)
    stloc result
    ldloc result
    ldc.i8 9223372034707292160
    ceq
    brtrue NEXT290
    ldstr "'LongMulOn32BitTest::LongMul_46' returned: '{0}'. Expected: '9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT290:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_46(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT291
    ldstr "'LongMulOn32BitTest::LongMul_46' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT291:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_46(int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT292
    ldstr "'LongMulOn32BitTest::LongMul_46' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT292:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_47(int32)
    stloc result
    ldloc result
    ldc.i8 4611686018427387904
    ceq
    brtrue NEXT293
    ldstr "'LongMulOn32BitTest::LongMul_47' returned: '{0}'. Expected: '4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT293:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_47(int32)
    stloc result
    ldloc result
    ldc.i8 9223372034707292160
    ceq
    brtrue NEXT294
    ldstr "'LongMulOn32BitTest::LongMul_47' returned: '{0}'. Expected: '9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT294:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_47(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT295
    ldstr "'LongMulOn32BitTest::LongMul_47' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT295:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_47(int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT296
    ldstr "'LongMulOn32BitTest::LongMul_47' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT296:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_48(int32)
    stloc result
    ldloc result
    ldc.i8 4611686018427387904
    ceq
    brtrue NEXT297
    ldstr "'LongMulOn32BitTest::LongMul_48' returned: '{0}'. Expected: '4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT297:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_48(int32)
    stloc result
    ldloc result
    ldc.i8 9223372034707292160
    ceq
    brtrue NEXT298
    ldstr "'LongMulOn32BitTest::LongMul_48' returned: '{0}'. Expected: '9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT298:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_48(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT299
    ldstr "'LongMulOn32BitTest::LongMul_48' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT299:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_48(int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT300
    ldstr "'LongMulOn32BitTest::LongMul_48' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT300:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_49(int32)
    stloc result
    ldloc result
    ldc.i8 9223372034707292160
    ceq
    brtrue NEXT301
    ldstr "'LongMulOn32BitTest::LongMul_49' returned: '{0}'. Expected: '9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT301:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_49(int32)
    stloc result
    ldloc result
    ldc.i8 -8589934591
    ceq
    brtrue NEXT302
    ldstr "'LongMulOn32BitTest::LongMul_49' returned: '{0}'. Expected: '-8589934591'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT302:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_49(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT303
    ldstr "'LongMulOn32BitTest::LongMul_49' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT303:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_49(int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT304
    ldstr "'LongMulOn32BitTest::LongMul_49' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT304:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_50(int32)
    stloc result
    ldloc result
    ldc.i8 9223372034707292160
    ceq
    brtrue NEXT305
    ldstr "'LongMulOn32BitTest::LongMul_50' returned: '{0}'. Expected: '9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT305:

    .try
    {
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_50(int32)
      ldstr "'LongMulOn32BitTest::LongMul_50' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT306
    }
  NEXT306:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_50(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT307
    ldstr "'LongMulOn32BitTest::LongMul_50' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT307:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_50(int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT308
    ldstr "'LongMulOn32BitTest::LongMul_50' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT308:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_51(int32)
    stloc result
    ldloc result
    ldc.i8 9223372034707292160
    ceq
    brtrue NEXT309
    ldstr "'LongMulOn32BitTest::LongMul_51' returned: '{0}'. Expected: '9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT309:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_51(int32)
    stloc result
    ldloc result
    ldc.i8 -8589934591
    ceq
    brtrue NEXT310
    ldstr "'LongMulOn32BitTest::LongMul_51' returned: '{0}'. Expected: '-8589934591'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT310:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_51(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT311
    ldstr "'LongMulOn32BitTest::LongMul_51' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT311:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_51(int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT312
    ldstr "'LongMulOn32BitTest::LongMul_51' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT312:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_52(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT313
    ldstr "'LongMulOn32BitTest::LongMul_52' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT313:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_52(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT314
    ldstr "'LongMulOn32BitTest::LongMul_52' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT314:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_52(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT315
    ldstr "'LongMulOn32BitTest::LongMul_52' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT315:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_52(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT316
    ldstr "'LongMulOn32BitTest::LongMul_52' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT316:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_53(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT317
    ldstr "'LongMulOn32BitTest::LongMul_53' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT317:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_53(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT318
    ldstr "'LongMulOn32BitTest::LongMul_53' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT318:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_53(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT319
    ldstr "'LongMulOn32BitTest::LongMul_53' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT319:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_53(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT320
    ldstr "'LongMulOn32BitTest::LongMul_53' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT320:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_54(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT321
    ldstr "'LongMulOn32BitTest::LongMul_54' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT321:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_54(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT322
    ldstr "'LongMulOn32BitTest::LongMul_54' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT322:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_54(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT323
    ldstr "'LongMulOn32BitTest::LongMul_54' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT323:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_54(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT324
    ldstr "'LongMulOn32BitTest::LongMul_54' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT324:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_55(int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT325
    ldstr "'LongMulOn32BitTest::LongMul_55' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT325:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_55(int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT326
    ldstr "'LongMulOn32BitTest::LongMul_55' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT326:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_55(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT327
    ldstr "'LongMulOn32BitTest::LongMul_55' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT327:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_55(int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT328
    ldstr "'LongMulOn32BitTest::LongMul_55' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT328:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_56(int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT329
    ldstr "'LongMulOn32BitTest::LongMul_56' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT329:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_56(int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT330
    ldstr "'LongMulOn32BitTest::LongMul_56' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT330:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_56(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT331
    ldstr "'LongMulOn32BitTest::LongMul_56' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT331:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_56(int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT332
    ldstr "'LongMulOn32BitTest::LongMul_56' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT332:

    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_57(int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT333
    ldstr "'LongMulOn32BitTest::LongMul_57' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT333:

    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_57(int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT334
    ldstr "'LongMulOn32BitTest::LongMul_57' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT334:

    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_57(int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT335
    ldstr "'LongMulOn32BitTest::LongMul_57' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT335:

    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_57(int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT336
    ldstr "'LongMulOn32BitTest::LongMul_57' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT336:

    ldc.i4 -2147483648
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686018427387904
    ceq
    brtrue NEXT337
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT337:

    ldc.i4 -2147483648
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372034707292160
    ceq
    brtrue NEXT338
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT338:

    ldc.i4 -2147483648
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT339
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT339:

    ldc.i4 -2147483648
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT340
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT340:

    ldc.i4 -1
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372034707292160
    ceq
    brtrue NEXT341
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT341:

    ldc.i4 -1
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -8589934591
    ceq
    brtrue NEXT342
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '-8589934591'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT342:

    ldc.i4 -1
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT343
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT343:

    ldc.i4 -1
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT344
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT344:

    ldc.i4 0
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT345
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT345:

    ldc.i4 0
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT346
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT346:

    ldc.i4 0
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT347
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT347:

    ldc.i4 0
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT348
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT348:

    ldc.i4 2147483647
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT349
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT349:

    ldc.i4 2147483647
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT350
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT350:

    ldc.i4 2147483647
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT351
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT351:

    ldc.i4 2147483647
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_58(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT352
    ldstr "'LongMulOn32BitTest::LongMul_58' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT352:

    ldc.i4 -2147483648
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686018427387904
    ceq
    brtrue NEXT353
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT353:

    ldc.i4 -2147483648
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372034707292160
    ceq
    brtrue NEXT354
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT354:

    ldc.i4 -2147483648
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT355
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT355:

    ldc.i4 -2147483648
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT356
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT356:

    ldc.i4 -1
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372034707292160
    ceq
    brtrue NEXT357
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT357:

    .try
    {
      ldc.i4 -1
      ldc.i4 -1
      call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
      ldstr "'LongMulOn32BitTest::LongMul_59' failed to throw OverflowException"
      call void LongMulOn32BitTest::PrintError(string)
      leave FAIL
    }
    catch [System.Runtime]System.OverflowException
    {
      leave NEXT358
    }
  NEXT358:

    ldc.i4 -1
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT359
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT359:

    ldc.i4 -1
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT360
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT360:

    ldc.i4 0
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT361
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT361:

    ldc.i4 0
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT362
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT362:

    ldc.i4 0
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT363
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT363:

    ldc.i4 0
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT364
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT364:

    ldc.i4 2147483647
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT365
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT365:

    ldc.i4 2147483647
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT366
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT366:

    ldc.i4 2147483647
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT367
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT367:

    ldc.i4 2147483647
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_59(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT368
    ldstr "'LongMulOn32BitTest::LongMul_59' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT368:

    ldc.i4 -2147483648
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686018427387904
    ceq
    brtrue NEXT369
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '4611686018427387904'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT369:

    ldc.i4 -2147483648
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372034707292160
    ceq
    brtrue NEXT370
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT370:

    ldc.i4 -2147483648
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT371
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT371:

    ldc.i4 -2147483648
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT372
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT372:

    ldc.i4 -1
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372034707292160
    ceq
    brtrue NEXT373
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '9223372034707292160'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT373:

    ldc.i4 -1
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 -8589934591
    ceq
    brtrue NEXT374
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '-8589934591'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT374:

    ldc.i4 -1
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT375
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT375:

    ldc.i4 -1
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT376
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT376:

    ldc.i4 0
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT377
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT377:

    ldc.i4 0
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT378
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT378:

    ldc.i4 0
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT379
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT379:

    ldc.i4 0
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT380
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT380:

    ldc.i4 2147483647
    ldc.i4 -2147483648
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686016279904256
    ceq
    brtrue NEXT381
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '4611686016279904256'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT381:

    ldc.i4 2147483647
    ldc.i4 -1
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 9223372030412324865
    ceq
    brtrue NEXT382
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '9223372030412324865'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT382:

    ldc.i4 2147483647
    ldc.i4 0
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 0
    ceq
    brtrue NEXT383
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '0'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT383:

    ldc.i4 2147483647
    ldc.i4 2147483647
    call int64 LongMulOn32BitTest::LongMul_60(int32, int32)
    stloc result
    ldloc result
    ldc.i8 4611686014132420609
    ceq
    brtrue NEXT384
    ldstr "'LongMulOn32BitTest::LongMul_60' returned: '{0}'. Expected: '4611686014132420609'"
    ldloc result
    call void LongMulOn32BitTest::PrintErrorWithResult(string, int64)
    br FAIL
  NEXT384:

    ldstr "SUCCESS"
    call void [System.Console]System.Console::WriteLine(string)
    ldc.i4 100
    ret
FAIL:
    ldstr "FAILED"
    call void LongMulOn32BitTest::PrintError(string)
    ldc.i4 1
    ret
  }

  .method private hidebysig static int64 LongMul_1(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 -2147483648
    conv.i8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_2(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 -2147483648
    conv.i8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_3(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 -2147483648
    conv.i8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_4(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 -1
    conv.i8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_5(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 -1
    conv.i8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_6(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 -1
    conv.i8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_7(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 0
    conv.i8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_8(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 0
    conv.i8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_9(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 0
    conv.i8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_10(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 2147483647
    conv.i8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_11(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 2147483647
    conv.i8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_12(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 2147483647
    conv.i8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_13(int32 left, int32 right) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldarg right
    conv.i8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_14(int32 left, int32 right) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldarg right
    conv.i8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_15(int32 left, int32 right) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldarg right
    conv.i8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_16(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 -2147483648
    conv.u8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_17(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 -2147483648
    conv.u8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_18(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 -2147483648
    conv.u8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_19(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 -1
    conv.u8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_20(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 -1
    conv.u8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_21(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 -1
    conv.u8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_22(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 0
    conv.u8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_23(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 0
    conv.u8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_24(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 0
    conv.u8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_25(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 2147483647
    conv.u8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_26(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 2147483647
    conv.u8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_27(int32 left) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldc.i4 2147483647
    conv.u8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_28(int32 left, int32 right) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldarg right
    conv.u8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_29(int32 left, int32 right) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldarg right
    conv.u8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_30(int32 left, int32 right) cil managed noinlining
  {
    ldarg left
    conv.i8
    ldarg right
    conv.u8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_31(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 -2147483648
    conv.i8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_32(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 -2147483648
    conv.i8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_33(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 -2147483648
    conv.i8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_34(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 -1
    conv.i8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_35(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 -1
    conv.i8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_36(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 -1
    conv.i8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_37(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 0
    conv.i8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_38(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 0
    conv.i8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_39(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 0
    conv.i8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_40(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 2147483647
    conv.i8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_41(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 2147483647
    conv.i8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_42(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 2147483647
    conv.i8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_43(int32 left, int32 right) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldarg right
    conv.i8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_44(int32 left, int32 right) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldarg right
    conv.i8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_45(int32 left, int32 right) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldarg right
    conv.i8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_46(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 -2147483648
    conv.u8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_47(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 -2147483648
    conv.u8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_48(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 -2147483648
    conv.u8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_49(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 -1
    conv.u8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_50(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 -1
    conv.u8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_51(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 -1
    conv.u8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_52(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 0
    conv.u8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_53(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 0
    conv.u8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_54(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 0
    conv.u8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_55(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 2147483647
    conv.u8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_56(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 2147483647
    conv.u8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_57(int32 left) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldc.i4 2147483647
    conv.u8
    mul.ovf.un
    ret
  }

  .method private hidebysig static int64 LongMul_58(int32 left, int32 right) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldarg right
    conv.u8
    mul
    ret
  }

  .method private hidebysig static int64 LongMul_59(int32 left, int32 right) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldarg right
    conv.u8
    mul.ovf
    ret
  }

  .method private hidebysig static int64 LongMul_60(int32 left, int32 right) cil managed noinlining
  {
    ldarg left
    conv.u8
    ldarg right
    conv.u8
    mul.ovf.un
    ret
  }
}
