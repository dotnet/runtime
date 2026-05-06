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
.assembly ASSEMBLY_NAME
{
}
.assembly extern xunit.core {}


.file alignment 512

.namespace JitTest_intrinsic_nonf_NaN_il
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static void 
            Fail(string func,
                 float64 arg,
                 float64 exp,
                 float64 res) cil managed
    {
      .maxstack  3
      .locals (string[] V_0)
      IL_0000:  ldc.i4.7
      IL_0001:  newarr     [mscorlib]System.String
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  ldc.i4.0
      IL_0009:  ldarg.0
      IL_000a:  stelem.ref
      IL_000b:  ldloc.0
      IL_000c:  ldc.i4.1
      IL_000d:  ldstr      "("
      IL_0012:  stelem.ref
      IL_0013:  ldloc.0
      IL_0014:  ldc.i4.2
      IL_0015:  ldarga.s   arg
      IL_0017:  call       instance string [mscorlib]System.Double::ToString()
      IL_001c:  stelem.ref
      IL_001d:  ldloc.0
      IL_001e:  ldc.i4.3
      IL_001f:  ldstr      ") failed: expected "
      IL_0024:  stelem.ref
      IL_0025:  ldloc.0
      IL_0026:  ldc.i4.4
      IL_0027:  ldarga.s   exp
      IL_0029:  call       instance string [mscorlib]System.Double::ToString()
      IL_002e:  stelem.ref
      IL_002f:  ldloc.0
      IL_0030:  ldc.i4.5
      IL_0031:  ldstr      ", got "
      IL_0036:  stelem.ref
      IL_0037:  ldloc.0
      IL_0038:  ldc.i4.6
      IL_0039:  ldarga.s   res
      IL_003b:  call       instance string [mscorlib]System.Double::ToString()
      IL_0040:  stelem.ref
      IL_0041:  ldloc.0
      IL_0042:  call       string [mscorlib]System.String::Concat(string[])
      IL_0047:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
      IL_004c:  throw
    } // end of method Test::Fail

    .method private hidebysig static void 
            Fail2(string func,
                  float64 arg1,
                  float64 arg2,
                  float64 exp,
                  float64 res) cil managed
    {
      .maxstack  3
      .locals (string[] V_0)
      IL_0000:  ldc.i4.s   9
      IL_0002:  newarr     [mscorlib]System.String
      IL_0007:  stloc.0
      IL_0008:  ldloc.0
      IL_0009:  ldc.i4.0
      IL_000a:  ldarg.0
      IL_000b:  stelem.ref
      IL_000c:  ldloc.0
      IL_000d:  ldc.i4.1
      IL_000e:  ldstr      "("
      IL_0013:  stelem.ref
      IL_0014:  ldloc.0
      IL_0015:  ldc.i4.2
      IL_0016:  ldarga.s   arg1
      IL_0018:  call       instance string [mscorlib]System.Double::ToString()
      IL_001d:  stelem.ref
      IL_001e:  ldloc.0
      IL_001f:  ldc.i4.3
      IL_0020:  ldstr      ", "
      IL_0025:  stelem.ref
      IL_0026:  ldloc.0
      IL_0027:  ldc.i4.4
      IL_0028:  ldarga.s   arg2
      IL_002a:  call       instance string [mscorlib]System.Double::ToString()
      IL_002f:  stelem.ref
      IL_0030:  ldloc.0
      IL_0031:  ldc.i4.5
      IL_0032:  ldstr      ") failed: expected "
      IL_0037:  stelem.ref
      IL_0038:  ldloc.0
      IL_0039:  ldc.i4.6
      IL_003a:  ldarga.s   exp
      IL_003c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0041:  stelem.ref
      IL_0042:  ldloc.0
      IL_0043:  ldc.i4.7
      IL_0044:  ldstr      ", got "
      IL_0049:  stelem.ref
      IL_004a:  ldloc.0
      IL_004b:  ldc.i4.8
      IL_004c:  ldarga.s   res
      IL_004e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0053:  stelem.ref
      IL_0054:  ldloc.0
      IL_0055:  call       string [mscorlib]System.String::Concat(string[])
      IL_005a:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
      IL_005f:  throw
    } // end of method Test::Fail2

    .method private hidebysig static void 
            TestAbs(float64 arg,
                    float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Abs("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Abs("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Abs"
      IL_00c0:  ldarg.0
      IL_00c1:  ldarg.1
      IL_00c2:  ldloc.0
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestAbs

    .method private hidebysig static void 
            TestAcos(float64 arg,
                     float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Acos(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Acos("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Acos("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Acos"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestAcos

    .method private hidebysig static void 
            TestAsin(float64 arg,
                     float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Asin(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Asin("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Asin("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Asin"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestAsin

    .method private hidebysig static void 
            TestAtan(float64 arg,
                     float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Atan(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Atan("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Atan("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Atan"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestAtan

    .method private hidebysig static void 
            TestCeiling(float64 arg,
                        float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Ceiling(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Ceiling("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Ceiling("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Ceiling"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestCeiling

    .method private hidebysig static void 
            TestCos(float64 arg,
                    float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Cos(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Cos("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Cos("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Cos"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestCos

    .method private hidebysig static void 
            TestCosh(float64 arg,
                     float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Cosh(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Cosh("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Cosh("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Cosh"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestCosh

    .method private hidebysig static void 
            TestExp(float64 arg,
                    float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Exp(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Exp("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Exp("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Exp"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestExp

    .method private hidebysig static void 
            TestFloor(float64 arg,
                      float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Floor(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Floor("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Floor("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Floor"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestFloor

    .method private hidebysig static void 
            TestLog(float64 arg,
                    float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Log(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Log("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Log("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Log"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestLog

    .method private hidebysig static void 
            TestLog10(float64 arg,
                      float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Log10(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Log10("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Log10("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Log10"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestLog10

    .method private hidebysig static void 
            TestRound(float64 arg,
                      float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Round(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Round("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Round("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Round"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestRound

    .method private hidebysig static void 
            TestSign(float64 arg,
                     float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       int32 [mscorlib]System.Math::Sign(float64)
      IL_0006:  conv.r8
      IL_0007:  stloc.0
      IL_0008:  ldarg.1
      IL_0009:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000e:  brfalse.s  IL_0018

      IL_0010:  ldloc.0
      IL_0011:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0016:  brtrue.s   IL_0038

      IL_0018:  ldarg.1
      IL_0019:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001e:  brfalse.s  IL_0028

      IL_0020:  ldloc.0
      IL_0021:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0026:  brtrue.s   IL_0038

      IL_0028:  ldarg.1
      IL_0029:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002e:  brfalse.s  IL_0078

      IL_0030:  ldloc.0
      IL_0031:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0036:  brfalse.s  IL_0078

      IL_0038:  ldc.i4.5
      IL_0039:  newarr     [mscorlib]System.String
      IL_003e:  stloc.1
      IL_003f:  ldloc.1
      IL_0040:  ldc.i4.0
      IL_0041:  ldstr      "Sign("
      IL_0046:  stelem.ref
      IL_0047:  ldloc.1
      IL_0048:  ldc.i4.1
      IL_0049:  ldarga.s   arg
      IL_004b:  call       instance string [mscorlib]System.Double::ToString()
      IL_0050:  stelem.ref
      IL_0051:  ldloc.1
      IL_0052:  ldc.i4.2
      IL_0053:  ldstr      ") == "
      IL_0058:  stelem.ref
      IL_0059:  ldloc.1
      IL_005a:  ldc.i4.3
      IL_005b:  ldloca.s   V_0
      IL_005d:  call       instance string [mscorlib]System.Double::ToString()
      IL_0062:  stelem.ref
      IL_0063:  ldloc.1
      IL_0064:  ldc.i4.4
      IL_0065:  ldstr      "  OK"
      IL_006a:  stelem.ref
      IL_006b:  ldloc.1
      IL_006c:  call       string [mscorlib]System.String::Concat(string[])
      IL_0071:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0076:  br.s       IL_00c9

      IL_0078:  ldarg.1
      IL_0079:  ldloc.0
      IL_007a:  bne.un.s   IL_00bc

      IL_007c:  ldc.i4.5
      IL_007d:  newarr     [mscorlib]System.String
      IL_0082:  stloc.1
      IL_0083:  ldloc.1
      IL_0084:  ldc.i4.0
      IL_0085:  ldstr      "Sign("
      IL_008a:  stelem.ref
      IL_008b:  ldloc.1
      IL_008c:  ldc.i4.1
      IL_008d:  ldarga.s   arg
      IL_008f:  call       instance string [mscorlib]System.Double::ToString()
      IL_0094:  stelem.ref
      IL_0095:  ldloc.1
      IL_0096:  ldc.i4.2
      IL_0097:  ldstr      ") == "
      IL_009c:  stelem.ref
      IL_009d:  ldloc.1
      IL_009e:  ldc.i4.3
      IL_009f:  ldloca.s   V_0
      IL_00a1:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a6:  stelem.ref
      IL_00a7:  ldloc.1
      IL_00a8:  ldc.i4.4
      IL_00a9:  ldstr      "  OK"
      IL_00ae:  stelem.ref
      IL_00af:  ldloc.1
      IL_00b0:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b5:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00ba:  br.s       IL_00c9

      IL_00bc:  ldstr      "Sign"
      IL_00c1:  ldloc.0
      IL_00c2:  ldarg.0
      IL_00c3:  ldarg.1
      IL_00c4:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c9:  ret
    } // end of method Test::TestSign

    .method private hidebysig static void 
            TestSin(float64 arg,
                    float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Sin(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Sin("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Sin("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Sin"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestSin

    .method private hidebysig static void 
            TestSinh(float64 arg,
                     float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Sinh(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Sinh("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Sinh("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Sinh"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestSinh

    .method private hidebysig static void 
            TestSqrt(float64 arg,
                     float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Sqrt(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Sqrt("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Sqrt("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Sqrt"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestSqrt

    .method private hidebysig static void 
            TestTan(float64 arg,
                    float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Tan(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Tan("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Tan("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Tan"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestTan

    .method private hidebysig static void 
            TestTanh(float64 arg,
                     float64 exp) cil managed
    {
      .maxstack  4
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  call       float64 [mscorlib]System.Math::Tanh(float64)
      IL_0006:  stloc.0
      IL_0007:  ldarg.1
      IL_0008:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000d:  brfalse.s  IL_0017

      IL_000f:  ldloc.0
      IL_0010:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0015:  brtrue.s   IL_0037

      IL_0017:  ldarg.1
      IL_0018:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001d:  brfalse.s  IL_0027

      IL_001f:  ldloc.0
      IL_0020:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldarg.1
      IL_0028:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002d:  brfalse.s  IL_0077

      IL_002f:  ldloc.0
      IL_0030:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0035:  brfalse.s  IL_0077

      IL_0037:  ldc.i4.5
      IL_0038:  newarr     [mscorlib]System.String
      IL_003d:  stloc.1
      IL_003e:  ldloc.1
      IL_003f:  ldc.i4.0
      IL_0040:  ldstr      "Tanh("
      IL_0045:  stelem.ref
      IL_0046:  ldloc.1
      IL_0047:  ldc.i4.1
      IL_0048:  ldarga.s   arg
      IL_004a:  call       instance string [mscorlib]System.Double::ToString()
      IL_004f:  stelem.ref
      IL_0050:  ldloc.1
      IL_0051:  ldc.i4.2
      IL_0052:  ldstr      ") == "
      IL_0057:  stelem.ref
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.3
      IL_005a:  ldloca.s   V_0
      IL_005c:  call       instance string [mscorlib]System.Double::ToString()
      IL_0061:  stelem.ref
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.4
      IL_0064:  ldstr      "  OK"
      IL_0069:  stelem.ref
      IL_006a:  ldloc.1
      IL_006b:  call       string [mscorlib]System.String::Concat(string[])
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  br.s       IL_00c8

      IL_0077:  ldarg.1
      IL_0078:  ldloc.0
      IL_0079:  bne.un.s   IL_00bb

      IL_007b:  ldc.i4.5
      IL_007c:  newarr     [mscorlib]System.String
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  ldstr      "Tanh("
      IL_0089:  stelem.ref
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.1
      IL_008c:  ldarga.s   arg
      IL_008e:  call       instance string [mscorlib]System.Double::ToString()
      IL_0093:  stelem.ref
      IL_0094:  ldloc.1
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ") == "
      IL_009b:  stelem.ref
      IL_009c:  ldloc.1
      IL_009d:  ldc.i4.3
      IL_009e:  ldloca.s   V_0
      IL_00a0:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a5:  stelem.ref
      IL_00a6:  ldloc.1
      IL_00a7:  ldc.i4.4
      IL_00a8:  ldstr      "  OK"
      IL_00ad:  stelem.ref
      IL_00ae:  ldloc.1
      IL_00af:  call       string [mscorlib]System.String::Concat(string[])
      IL_00b4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b9:  br.s       IL_00c8

      IL_00bb:  ldstr      "Tanh"
      IL_00c0:  ldloc.0
      IL_00c1:  ldarg.0
      IL_00c2:  ldarg.1
      IL_00c3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail(string,
                                                   float64,
                                                   float64,
                                                   float64)
      IL_00c8:  ret
    } // end of method Test::TestTanh

    .method private hidebysig static void 
            TestLog2(float64 arg1,
                     float64 arg2,
                     float64 exp) cil managed
    {
      .maxstack  5
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  call       float64 [mscorlib]System.Math::Log(float64,
                                                              float64)
      IL_0007:  stloc.0
      IL_0008:  ldarg.2
      IL_0009:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000e:  brfalse.s  IL_0018

      IL_0010:  ldloc.0
      IL_0011:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0016:  brtrue.s   IL_0038

      IL_0018:  ldarg.2
      IL_0019:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001e:  brfalse.s  IL_0028

      IL_0020:  ldloc.0
      IL_0021:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0026:  brtrue.s   IL_0038

      IL_0028:  ldarg.2
      IL_0029:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002e:  brfalse.s  IL_008a

      IL_0030:  ldloc.0
      IL_0031:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0036:  brfalse.s  IL_008a

      IL_0038:  ldc.i4.7
      IL_0039:  newarr     [mscorlib]System.String
      IL_003e:  stloc.1
      IL_003f:  ldloc.1
      IL_0040:  ldc.i4.0
      IL_0041:  ldstr      "Log2("
      IL_0046:  stelem.ref
      IL_0047:  ldloc.1
      IL_0048:  ldc.i4.1
      IL_0049:  ldarga.s   arg1
      IL_004b:  call       instance string [mscorlib]System.Double::ToString()
      IL_0050:  stelem.ref
      IL_0051:  ldloc.1
      IL_0052:  ldc.i4.2
      IL_0053:  ldstr      ", "
      IL_0058:  stelem.ref
      IL_0059:  ldloc.1
      IL_005a:  ldc.i4.3
      IL_005b:  ldarga.s   arg2
      IL_005d:  call       instance string [mscorlib]System.Double::ToString()
      IL_0062:  stelem.ref
      IL_0063:  ldloc.1
      IL_0064:  ldc.i4.4
      IL_0065:  ldstr      ") == "
      IL_006a:  stelem.ref
      IL_006b:  ldloc.1
      IL_006c:  ldc.i4.5
      IL_006d:  ldloca.s   V_0
      IL_006f:  call       instance string [mscorlib]System.Double::ToString()
      IL_0074:  stelem.ref
      IL_0075:  ldloc.1
      IL_0076:  ldc.i4.6
      IL_0077:  ldstr      "  OK"
      IL_007c:  stelem.ref
      IL_007d:  ldloc.1
      IL_007e:  call       string [mscorlib]System.String::Concat(string[])
      IL_0083:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0088:  br.s       IL_00ee

      IL_008a:  ldarg.2
      IL_008b:  ldloc.0
      IL_008c:  bne.un.s   IL_00e0

      IL_008e:  ldc.i4.7
      IL_008f:  newarr     [mscorlib]System.String
      IL_0094:  stloc.1
      IL_0095:  ldloc.1
      IL_0096:  ldc.i4.0
      IL_0097:  ldstr      "Log2("
      IL_009c:  stelem.ref
      IL_009d:  ldloc.1
      IL_009e:  ldc.i4.1
      IL_009f:  ldarga.s   arg1
      IL_00a1:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a6:  stelem.ref
      IL_00a7:  ldloc.1
      IL_00a8:  ldc.i4.2
      IL_00a9:  ldstr      ", "
      IL_00ae:  stelem.ref
      IL_00af:  ldloc.1
      IL_00b0:  ldc.i4.3
      IL_00b1:  ldarga.s   arg2
      IL_00b3:  call       instance string [mscorlib]System.Double::ToString()
      IL_00b8:  stelem.ref
      IL_00b9:  ldloc.1
      IL_00ba:  ldc.i4.4
      IL_00bb:  ldstr      ") == "
      IL_00c0:  stelem.ref
      IL_00c1:  ldloc.1
      IL_00c2:  ldc.i4.5
      IL_00c3:  ldloca.s   V_0
      IL_00c5:  call       instance string [mscorlib]System.Double::ToString()
      IL_00ca:  stelem.ref
      IL_00cb:  ldloc.1
      IL_00cc:  ldc.i4.6
      IL_00cd:  ldstr      "  OK"
      IL_00d2:  stelem.ref
      IL_00d3:  ldloc.1
      IL_00d4:  call       string [mscorlib]System.String::Concat(string[])
      IL_00d9:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00de:  br.s       IL_00ee

      IL_00e0:  ldstr      "Log2"
      IL_00e5:  ldarg.0
      IL_00e6:  ldarg.1
      IL_00e7:  ldarg.2
      IL_00e8:  ldloc.0
      IL_00e9:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail2(string,
                                                    float64,
                                                    float64,
                                                    float64,
                                                    float64)
      IL_00ee:  ret
    } // end of method Test::TestLog2

    .method private hidebysig static void 
            TestPow(float64 arg1,
                    float64 arg2,
                    float64 exp) cil managed
    {
      .maxstack  5
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  call       float64 [mscorlib]System.Math::Pow(float64,
                                                              float64)
      IL_0007:  stloc.0
      IL_0008:  ldarg.2
      IL_0009:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000e:  brfalse.s  IL_0018

      IL_0010:  ldloc.0
      IL_0011:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0016:  brtrue.s   IL_0038

      IL_0018:  ldarg.2
      IL_0019:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001e:  brfalse.s  IL_0028

      IL_0020:  ldloc.0
      IL_0021:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0026:  brtrue.s   IL_0038

      IL_0028:  ldarg.2
      IL_0029:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002e:  brfalse.s  IL_008a

      IL_0030:  ldloc.0
      IL_0031:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0036:  brfalse.s  IL_008a

      IL_0038:  ldc.i4.7
      IL_0039:  newarr     [mscorlib]System.String
      IL_003e:  stloc.1
      IL_003f:  ldloc.1
      IL_0040:  ldc.i4.0
      IL_0041:  ldstr      "Pow("
      IL_0046:  stelem.ref
      IL_0047:  ldloc.1
      IL_0048:  ldc.i4.1
      IL_0049:  ldarga.s   arg1
      IL_004b:  call       instance string [mscorlib]System.Double::ToString()
      IL_0050:  stelem.ref
      IL_0051:  ldloc.1
      IL_0052:  ldc.i4.2
      IL_0053:  ldstr      ", "
      IL_0058:  stelem.ref
      IL_0059:  ldloc.1
      IL_005a:  ldc.i4.3
      IL_005b:  ldarga.s   arg2
      IL_005d:  call       instance string [mscorlib]System.Double::ToString()
      IL_0062:  stelem.ref
      IL_0063:  ldloc.1
      IL_0064:  ldc.i4.4
      IL_0065:  ldstr      ") == "
      IL_006a:  stelem.ref
      IL_006b:  ldloc.1
      IL_006c:  ldc.i4.5
      IL_006d:  ldloca.s   V_0
      IL_006f:  call       instance string [mscorlib]System.Double::ToString()
      IL_0074:  stelem.ref
      IL_0075:  ldloc.1
      IL_0076:  ldc.i4.6
      IL_0077:  ldstr      "  OK"
      IL_007c:  stelem.ref
      IL_007d:  ldloc.1
      IL_007e:  call       string [mscorlib]System.String::Concat(string[])
      IL_0083:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0088:  br.s       IL_00ee

      IL_008a:  ldarg.2
      IL_008b:  ldloc.0
      IL_008c:  bne.un.s   IL_00e0

      IL_008e:  ldc.i4.7
      IL_008f:  newarr     [mscorlib]System.String
      IL_0094:  stloc.1
      IL_0095:  ldloc.1
      IL_0096:  ldc.i4.0
      IL_0097:  ldstr      "Pow("
      IL_009c:  stelem.ref
      IL_009d:  ldloc.1
      IL_009e:  ldc.i4.1
      IL_009f:  ldarga.s   arg1
      IL_00a1:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a6:  stelem.ref
      IL_00a7:  ldloc.1
      IL_00a8:  ldc.i4.2
      IL_00a9:  ldstr      ", "
      IL_00ae:  stelem.ref
      IL_00af:  ldloc.1
      IL_00b0:  ldc.i4.3
      IL_00b1:  ldarga.s   arg2
      IL_00b3:  call       instance string [mscorlib]System.Double::ToString()
      IL_00b8:  stelem.ref
      IL_00b9:  ldloc.1
      IL_00ba:  ldc.i4.4
      IL_00bb:  ldstr      ") == "
      IL_00c0:  stelem.ref
      IL_00c1:  ldloc.1
      IL_00c2:  ldc.i4.5
      IL_00c3:  ldloca.s   V_0
      IL_00c5:  call       instance string [mscorlib]System.Double::ToString()
      IL_00ca:  stelem.ref
      IL_00cb:  ldloc.1
      IL_00cc:  ldc.i4.6
      IL_00cd:  ldstr      "  OK"
      IL_00d2:  stelem.ref
      IL_00d3:  ldloc.1
      IL_00d4:  call       string [mscorlib]System.String::Concat(string[])
      IL_00d9:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00de:  br.s       IL_00ee

      IL_00e0:  ldstr      "Pow"
      IL_00e5:  ldarg.0
      IL_00e6:  ldarg.1
      IL_00e7:  ldarg.2
      IL_00e8:  ldloc.0
      IL_00e9:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail2(string,
                                                    float64,
                                                    float64,
                                                    float64,
                                                    float64)
      IL_00ee:  ret
    } // end of method Test::TestPow

    .method private hidebysig static void 
            TestAtan2(float64 arg1,
                      float64 arg2,
                      float64 exp) cil managed
    {
      .maxstack  5
      .locals (float64 V_0,
               string[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  call       float64 [mscorlib]System.Math::Atan2(float64,
                                                                float64)
      IL_0007:  stloc.0
      IL_0008:  ldarg.2
      IL_0009:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_000e:  brfalse.s  IL_0018

      IL_0010:  ldloc.0
      IL_0011:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0016:  brtrue.s   IL_0038

      IL_0018:  ldarg.2
      IL_0019:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_001e:  brfalse.s  IL_0028

      IL_0020:  ldloc.0
      IL_0021:  call       bool [mscorlib]System.Double::IsNegativeInfinity(float64)
      IL_0026:  brtrue.s   IL_0038

      IL_0028:  ldarg.2
      IL_0029:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_002e:  brfalse.s  IL_008a

      IL_0030:  ldloc.0
      IL_0031:  call       bool [mscorlib]System.Double::IsPositiveInfinity(float64)
      IL_0036:  brfalse.s  IL_008a

      IL_0038:  ldc.i4.7
      IL_0039:  newarr     [mscorlib]System.String
      IL_003e:  stloc.1
      IL_003f:  ldloc.1
      IL_0040:  ldc.i4.0
      IL_0041:  ldstr      "Atan2("
      IL_0046:  stelem.ref
      IL_0047:  ldloc.1
      IL_0048:  ldc.i4.1
      IL_0049:  ldarga.s   arg1
      IL_004b:  call       instance string [mscorlib]System.Double::ToString()
      IL_0050:  stelem.ref
      IL_0051:  ldloc.1
      IL_0052:  ldc.i4.2
      IL_0053:  ldstr      ", "
      IL_0058:  stelem.ref
      IL_0059:  ldloc.1
      IL_005a:  ldc.i4.3
      IL_005b:  ldarga.s   arg2
      IL_005d:  call       instance string [mscorlib]System.Double::ToString()
      IL_0062:  stelem.ref
      IL_0063:  ldloc.1
      IL_0064:  ldc.i4.4
      IL_0065:  ldstr      ") == "
      IL_006a:  stelem.ref
      IL_006b:  ldloc.1
      IL_006c:  ldc.i4.5
      IL_006d:  ldloca.s   V_0
      IL_006f:  call       instance string [mscorlib]System.Double::ToString()
      IL_0074:  stelem.ref
      IL_0075:  ldloc.1
      IL_0076:  ldc.i4.6
      IL_0077:  ldstr      "  OK"
      IL_007c:  stelem.ref
      IL_007d:  ldloc.1
      IL_007e:  call       string [mscorlib]System.String::Concat(string[])
      IL_0083:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0088:  br.s       IL_00ee

      IL_008a:  ldarg.2
      IL_008b:  ldloc.0
      IL_008c:  bne.un.s   IL_00e0

      IL_008e:  ldc.i4.7
      IL_008f:  newarr     [mscorlib]System.String
      IL_0094:  stloc.1
      IL_0095:  ldloc.1
      IL_0096:  ldc.i4.0
      IL_0097:  ldstr      "Atan2("
      IL_009c:  stelem.ref
      IL_009d:  ldloc.1
      IL_009e:  ldc.i4.1
      IL_009f:  ldarga.s   arg1
      IL_00a1:  call       instance string [mscorlib]System.Double::ToString()
      IL_00a6:  stelem.ref
      IL_00a7:  ldloc.1
      IL_00a8:  ldc.i4.2
      IL_00a9:  ldstr      ", "
      IL_00ae:  stelem.ref
      IL_00af:  ldloc.1
      IL_00b0:  ldc.i4.3
      IL_00b1:  ldarga.s   arg2
      IL_00b3:  call       instance string [mscorlib]System.Double::ToString()
      IL_00b8:  stelem.ref
      IL_00b9:  ldloc.1
      IL_00ba:  ldc.i4.4
      IL_00bb:  ldstr      ") == "
      IL_00c0:  stelem.ref
      IL_00c1:  ldloc.1
      IL_00c2:  ldc.i4.5
      IL_00c3:  ldloca.s   V_0
      IL_00c5:  call       instance string [mscorlib]System.Double::ToString()
      IL_00ca:  stelem.ref
      IL_00cb:  ldloc.1
      IL_00cc:  ldc.i4.6
      IL_00cd:  ldstr      "  OK"
      IL_00d2:  stelem.ref
      IL_00d3:  ldloc.1
      IL_00d4:  call       string [mscorlib]System.String::Concat(string[])
      IL_00d9:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00de:  br.s       IL_00ee

      IL_00e0:  ldstr      "Atan2"
      IL_00e5:  ldarg.0
      IL_00e6:  ldarg.1
      IL_00e7:  ldarg.2
      IL_00e8:  ldloc.0
      IL_00e9:  call       void JitTest_intrinsic_nonf_NaN_il.Test::Fail2(string,
                                                    float64,
                                                    float64,
                                                    float64,
                                                    float64)
      IL_00ee:  ret
    } // end of method Test::TestAtan2

    .method public hidebysig static int32 
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  3
      .locals (class [mscorlib]System.Exception V_0,
               int32 V_1)
      .try
      {
        IL_0000:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0009:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0012:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAbs(float64,
                                                        float64)
        IL_0017:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0020:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0029:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAbs(float64,
                                                        float64)
        IL_002e:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0037:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0040:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAbs(float64,
                                                        float64)
        IL_0045:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_004e:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0057:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAcos(float64,
                                                         float64)
        IL_005c:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0065:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_006e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAcos(float64,
                                                         float64)
        IL_0073:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_007c:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0085:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAcos(float64,
                                                         float64)
        IL_008a:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0093:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_009c:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAsin(float64,
                                                         float64)
        IL_00a1:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_00aa:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_00b3:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAsin(float64,
                                                         float64)
        IL_00b8:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_00c1:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_00ca:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAsin(float64,
                                                         float64)
        IL_00cf:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_00d8:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_00e1:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAtan(float64,
                                                         float64)
        IL_00e6:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_00ef:  ldc.r8     -1.5707963267948966
        IL_00f8:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAtan(float64,
                                                         float64)
        IL_00fd:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0106:  ldc.r8     1.5707963267948966
        IL_010f:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAtan(float64,
                                                         float64)
        IL_0114:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_011d:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0126:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestCeiling(float64,
                                                            float64)
        IL_012b:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0134:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_013d:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestCeiling(float64,
                                                            float64)
        IL_0142:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_014b:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0154:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestCeiling(float64,
                                                            float64)
        IL_0159:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0162:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_016b:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestCos(float64,
                                                        float64)
        IL_0170:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0179:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0182:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestCos(float64,
                                                        float64)
        IL_0187:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0190:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0199:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestCos(float64,
                                                        float64)
        IL_019e:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_01a7:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_01b0:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestCosh(float64,
                                                         float64)
        IL_01b5:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_01be:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_01c7:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestCosh(float64,
                                                         float64)
        IL_01cc:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_01d5:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_01de:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestCosh(float64,
                                                         float64)
        IL_01e3:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_01ec:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_01f5:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestExp(float64,
                                                        float64)
        IL_01fa:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0203:  ldc.r8     0.
        IL_020c:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestExp(float64,
                                                        float64)
        IL_0211:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_021a:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0223:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestExp(float64,
                                                        float64)
        IL_0228:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0231:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_023a:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestFloor(float64,
                                                          float64)
        IL_023f:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0248:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0251:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestFloor(float64,
                                                          float64)
        IL_0256:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_025f:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0268:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestFloor(float64,
                                                          float64)
        IL_026d:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0276:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_027f:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog(float64,
                                                        float64)
        IL_0284:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_028d:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0296:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog(float64,
                                                        float64)
        IL_029b:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_02a4:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_02ad:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog(float64,
                                                        float64)
        IL_02b2:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_02bb:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_02c4:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog10(float64,
                                                          float64)
        IL_02c9:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_02d2:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_02db:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog10(float64,
                                                          float64)
        IL_02e0:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_02e9:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_02f2:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog10(float64,
                                                          float64)
        IL_02f7:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0300:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0309:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestRound(float64,
                                                          float64)
        IL_030e:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0317:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0320:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestRound(float64,
                                                          float64)
        IL_0325:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_032e:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0337:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestRound(float64,
                                                          float64)
        IL_033c:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0345:  ldc.r8     -1.
        IL_034e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestSign(float64,
                                                         float64)
        IL_0353:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_035c:  ldc.r8     1.
        IL_0365:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestSign(float64,
                                                         float64)
        IL_036a:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0373:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_037c:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestSin(float64,
                                                        float64)
        IL_0381:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_038a:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0393:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestSin(float64,
                                                        float64)
        IL_0398:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_03a1:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_03aa:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestSin(float64,
                                                        float64)
        IL_03af:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_03b8:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_03c1:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestSinh(float64,
                                                         float64)
        IL_03c6:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_03cf:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_03d8:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestSinh(float64,
                                                         float64)
        IL_03dd:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_03e6:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_03ef:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestSinh(float64,
                                                         float64)
        IL_03f4:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_03fd:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0406:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestSqrt(float64,
                                                         float64)
        IL_040b:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0414:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_041d:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestSqrt(float64,
                                                         float64)
        IL_0422:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_042b:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0434:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestSqrt(float64,
                                                         float64)
        IL_0439:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0442:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_044b:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestTan(float64,
                                                        float64)
        IL_0450:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0459:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0462:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestTan(float64,
                                                        float64)
        IL_0467:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0470:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0479:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestTan(float64,
                                                        float64)
        IL_047e:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0487:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0490:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestTanh(float64,
                                                         float64)
        IL_0495:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_049e:  ldc.r8     -1.
        IL_04a7:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestTanh(float64,
                                                         float64)
        IL_04ac:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_04b5:  ldc.r8     1.
        IL_04be:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestTanh(float64,
                                                         float64)
        IL_04c3:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_04cc:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_04d5:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_04de:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog2(float64,
                                                         float64,
                                                         float64)
        IL_04e3:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_04ec:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_04f5:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_04fe:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog2(float64,
                                                         float64,
                                                         float64)
        IL_0503:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_050c:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0515:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_051e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog2(float64,
                                                         float64,
                                                         float64)
        IL_0523:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_052c:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0535:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_053e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog2(float64,
                                                         float64,
                                                         float64)
        IL_0543:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_054c:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0555:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_055e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog2(float64,
                                                         float64,
                                                         float64)
        IL_0563:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_056c:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0575:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_057e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog2(float64,
                                                         float64,
                                                         float64)
        IL_0583:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_058c:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0595:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_059e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog2(float64,
                                                         float64,
                                                         float64)
        IL_05a3:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_05ac:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_05b5:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_05be:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog2(float64,
                                                         float64,
                                                         float64)
        IL_05c3:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_05cc:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_05d5:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_05de:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestLog2(float64,
                                                         float64,
                                                         float64)
        IL_05e3:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_05ec:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_05f5:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_05fe:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestPow(float64,
                                                        float64,
                                                        float64)
        IL_0603:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_060c:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0615:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_061e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestPow(float64,
                                                        float64,
                                                        float64)
        IL_0623:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_062c:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0635:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_063e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestPow(float64,
                                                        float64,
                                                        float64)
        IL_0643:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_064c:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0655:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_065e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestPow(float64,
                                                        float64,
                                                        float64)
        IL_0663:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_066c:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0675:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_067e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestPow(float64,
                                                        float64,
                                                        float64)
        IL_0683:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_068c:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0695:  ldc.r8     0.
        IL_069e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestPow(float64,
                                                        float64,
                                                        float64)
        IL_06a3:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_06ac:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_06b5:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_06be:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestPow(float64,
                                                        float64,
                                                        float64)
        IL_06c3:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_06cc:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_06d5:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_06de:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestPow(float64,
                                                        float64,
                                                        float64)
        IL_06e3:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_06ec:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_06f5:  ldc.r8     0.
        IL_06fe:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestPow(float64,
                                                        float64,
                                                        float64)
        IL_0703:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_070c:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0715:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_071e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAtan2(float64,
                                                          float64,
                                                          float64)
        IL_0723:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_072c:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0735:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_073e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAtan2(float64,
                                                          float64,
                                                          float64)
        IL_0743:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_074c:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0755:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_075e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAtan2(float64,
                                                          float64,
                                                          float64)
        IL_0763:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_076c:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_0775:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_077e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAtan2(float64,
                                                          float64,
                                                          float64)
        IL_0783:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_078c:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_0795:  ldc.r8     0.78539816339744828
        IL_079e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAtan2(float64,
                                                          float64,
                                                          float64)
        IL_07a3:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_07ac:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_07b5:  ldc.r8     2.3561944901923448
        IL_07be:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAtan2(float64,
                                                          float64,
                                                          float64)
        IL_07c3:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_07cc:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_07d5:  ldc.r8     (FF FF FF FF FF FF FF 7F)
        IL_07de:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAtan2(float64,
                                                          float64,
                                                          float64)
        IL_07e3:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_07ec:  ldc.r8     (00 00 00 00 00 00 F0 7F)
        IL_07f5:  ldc.r8     -0.78539816339744828
        IL_07fe:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAtan2(float64,
                                                          float64,
                                                          float64)
        IL_0803:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_080c:  ldc.r8     (00 00 00 00 00 00 F0 FF)
        IL_0815:  ldc.r8     -2.3561944901923448
        IL_081e:  call       void JitTest_intrinsic_nonf_NaN_il.Test::TestAtan2(float64,
                                                          float64,
                                                          float64)
        IL_0823:  leave.s    IL_0840

      }  // end .try
      catch [mscorlib]System.Exception 
      {
        IL_0825:  stloc.0
        IL_0826:  ldloc.0
        IL_0827:  callvirt   instance string [mscorlib]System.Exception::ToString()
        IL_082c:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0831:  ldstr      "=== FAILED ==="
        IL_0836:  call       void [System.Console]System.Console::WriteLine(string)
        IL_083b:  ldc.i4.s   101
        IL_083d:  stloc.1
        IL_083e:  leave.s    IL_084f

      }  // end handler
      IL_0840:  ldstr      "=== PASSED ==="
      IL_0845:  call       void [System.Console]System.Console::WriteLine(string)
      IL_084a:  ldc.i4.s   100
      IL_084c:  stloc.1
      IL_084d:  br.s       IL_084f

      IL_084f:  ldloc.1
      IL_0850:  ret
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest_intrinsic_nonf_NaN_il

