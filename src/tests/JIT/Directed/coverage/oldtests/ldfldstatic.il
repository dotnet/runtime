// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern legacy library mscorlib {}
.assembly ASSEMBLY_NAME {}
.class public auto ansi beforefieldinit ldfldstatic
       extends [mscorlib]System.Object
{
  .field public static char Achar
  .field public static bool Abool
  .field public static float32 Ar4
  .field public static float64 Ar8
  .field public static int8 Ai1
  .field public static int16 Ai2
  .field public static int32 Ai4
  .field public static int64 Ai8
  .field public static unsigned int8 Au1
  .field public static unsigned int16 Au2
  .field public static unsigned int32 Au4
  .field public static unsigned int64 Au8
  .field public static string Astring
  
  .method public hidebysig static int32 Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals (bool V_0,
             class ldfldstatic V_1,
             int32 V_2)
    ldstr      "Testing ldfld on static fields"
    call       void [System.Console]System.Console::WriteLine(string)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  newobj     instance void ldfldstatic::.ctor()
    IL_0007:  stloc.1
    IL_0008:  ldloc.1
    		 ldfld     char ldfldstatic::Achar
    IL_000d:  ldc.i4.s   65
    IL_000f:  beq.s      IL_001d

    IL_0011:  ldc.i4.0
    IL_0012:  stloc.0
    IL_0013:  ldstr      "Achar != 'A'"
    IL_0018:  call       void [System.Console]System.Console::WriteLine(string)
    IL_001d:  ldloc.1
    		 ldfld     bool ldfldstatic::Abool
    IL_0022:  brtrue.s   IL_0030

    IL_0024:  ldc.i4.0
    IL_0025:  stloc.0
    IL_0026:  ldstr      "Abool != true"
    IL_002b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0030:  ldloc.1
    		 ldfld     float32 ldfldstatic::Ar4
    IL_0035:  ldc.r4     2.1234567
    IL_003a:  beq.s      IL_0048

    IL_003c:  ldc.i4.0
    IL_003d:  stloc.0
    IL_003e:  ldstr      "Ar4 != 2.123456789F"
    IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0048:  ldloc.1
    		 ldfld     float64 ldfldstatic::Ar8
    IL_004d:  ldc.r8     -3.1234567890123457
    IL_0056:  beq.s      IL_0064

    IL_0058:  ldc.i4.0
    IL_0059:  stloc.0
    IL_005a:  ldstr      "Ar8 != -3.123456789012345678"
    IL_005f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0064:  ldloc.1
    		 ldfld     int8 ldfldstatic::Ai1
    IL_0069:  conv.i4
    IL_006a:  ldc.i4.m1
    IL_006b:  beq.s      IL_0079

    IL_006d:  ldc.i4.0
    IL_006e:  stloc.0
    IL_006f:  ldstr      "Ai1 != -1"
    IL_0074:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0079:  ldloc.1
    		 ldfld     int16 ldfldstatic::Ai2
    IL_007e:  ldc.i4     0xffffff01
    IL_0083:  beq.s      IL_0091

    IL_0085:  ldc.i4.0
    IL_0086:  stloc.0
    IL_0087:  ldstr      "Ai2 != -255"
    IL_008c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0091:  ldloc.1
    		 ldfld     int32 ldfldstatic::Ai4
    IL_0096:  ldc.i4     0xffed2979
    IL_009b:  beq.s      IL_00a9

    IL_009d:  ldc.i4.0
    IL_009e:  stloc.0
    IL_009f:  ldstr      "Ai4 != -1234567"
    IL_00a4:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a9:  ldloc.1
    		 ldfld     int64 ldfldstatic::Ai8
    IL_00ae:  ldc.i4     0xb669fd2e
    IL_00b3:  conv.i8
    IL_00b4:  beq.s      IL_00c2

    IL_00b6:  ldc.i4.0
    IL_00b7:  stloc.0
    IL_00b8:  ldstr      "Ai8 != -1234567890"
    IL_00bd:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c2:  ldloc.1
    		 ldfld     unsigned int8 ldfldstatic::Au1
    IL_00c7:  ldc.i4.1
    IL_00c8:  beq.s      IL_00d6

    IL_00ca:  ldc.i4.0
    IL_00cb:  stloc.0
    IL_00cc:  ldstr      "Au1 != 1"
    IL_00d1:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00d6:  ldloc.1
    		 ldfld     unsigned int16 ldfldstatic::Au2
    IL_00db:  conv.i4
    IL_00dc:  ldc.i4     0x80
    IL_00e1:  beq.s      IL_00ef

    IL_00e3:  ldc.i4.0
    IL_00e4:  stloc.0
    IL_00e5:  ldstr      "Au2 != 128"
    IL_00ea:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00ef:  ldloc.1
    		 ldfld     int16 ldfldstatic::Ai2
    IL_00f4:  ldc.i4     0xffffff01
    IL_00f9:  beq.s      IL_0107

    IL_00fb:  ldc.i4.0
    IL_00fc:  stloc.0
    IL_00fd:  ldstr      "Ai2 != -255"
    IL_0102:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0107:  ldloc.1
    		 ldfld     unsigned int32 ldfldstatic::Au4
    IL_010c:  ldc.i4     0x12d687
    IL_0111:  beq.s      IL_011f

    IL_0113:  ldc.i4.0
    IL_0114:  stloc.0
    IL_0115:  ldstr      "Au4 != 1234567"
    IL_011a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_011f:  ldloc.1
    		 ldfld     unsigned int64 ldfldstatic::Au8
    IL_0124:  ldc.i4     0x499602d2
    IL_0129:  conv.i8
    IL_012a:  beq.s      IL_0138

    IL_012c:  ldc.i4.0
    IL_012d:  stloc.0
    IL_012e:  ldstr      "Au8 != 1234567890"
    IL_0133:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0138:  ldloc.1
    		 ldfld     string ldfldstatic::Astring
    IL_013d:  ldstr      "testing ldfld on static"
    IL_0142:  call       bool [mscorlib]System.String::Equals(string,
                                                              string)
    IL_0147:  brtrue.s   IL_0162

    IL_0149:  ldc.i4.0
    IL_014a:  stloc.0
    IL_014b:  ldloc.1
    		 ldfld     string ldfldstatic::Astring
    IL_0150:  ldstr      "testing ldfld on static"
    IL_0155:  call       bool [mscorlib]System.String::Equals(string,
                                                              string)
    IL_015a:  ldc.i4.0
    IL_015b:  ceq
    IL_015d:  call       void [System.Console]System.Console::WriteLine(bool)
    IL_0162:  ldloc.0
    IL_0163:  brtrue.s   IL_0173

    IL_0165:  ldstr      "FAILED"
    IL_016a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_016f:  ldc.i4.1
    IL_0170:  stloc.2
    IL_0171:  br.s       IL_0182

    IL_0173:  ldstr      "PASSED"
    IL_0178:  call       void [System.Console]System.Console::WriteLine(string)
    IL_017d:  ldc.i4.s   100
    IL_017f:  stloc.2
    IL_0180:  br.s       IL_0182

    IL_0182:  ldloc.2
    IL_0183:  ret
  }

  .method private hidebysig specialname rtspecialname static 
          void .cctor()
  {
    .maxstack  1
    IL_0000:  ldc.i4.s   65
    IL_0002:  stsfld     char ldfldstatic::Achar
    IL_0007:  ldc.i4.1
    IL_0008:  stsfld     bool ldfldstatic::Abool
    IL_000d:  ldc.r4     2.1234567
    IL_0012:  stsfld     float32 ldfldstatic::Ar4
    IL_0017:  ldc.r8     -3.1234567890123457
    IL_0020:  stsfld     float64 ldfldstatic::Ar8
    IL_0025:  ldc.i4.m1
    IL_0026:  stsfld     int8 ldfldstatic::Ai1
    IL_002b:  ldc.i4     0xffffff01
    IL_0030:  stsfld     int16 ldfldstatic::Ai2
    IL_0035:  ldc.i4     0xffed2979
    IL_003a:  stsfld     int32 ldfldstatic::Ai4
    IL_003f:  ldc.i4     0xb669fd2e
    IL_0044:  conv.i8
    IL_0045:  stsfld     int64 ldfldstatic::Ai8
    IL_004a:  ldc.i4.1
    IL_004b:  stsfld     unsigned int8 ldfldstatic::Au1
    IL_0050:  ldc.i4     0x80
    IL_0055:  stsfld     unsigned int16 ldfldstatic::Au2
    IL_005a:  ldc.i4     0x12d687
    IL_005f:  stsfld     unsigned int32 ldfldstatic::Au4
    IL_0064:  ldc.i4     0x499602d2
    IL_0069:  conv.i8
    IL_006a:  stsfld     unsigned int64 ldfldstatic::Au8
    IL_006f:  ldstr      "testing ldfld on static"
    IL_0074:  stsfld     string ldfldstatic::Astring
    IL_0079:  ret
  }

.method public hidebysig specialname rtspecialname 
        instance void .ctor()
{
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
  IL_0006:  ret
}

}
