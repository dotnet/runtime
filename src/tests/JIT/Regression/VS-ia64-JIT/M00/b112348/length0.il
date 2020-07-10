// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib {}

.assembly test
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module test.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY


.namespace JitTest
{
  .class private auto ansi Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static int32 
            try_index(int32[0...] arr,
                      int32 idx,
                      int32 expected) cil managed
    {
      .maxstack  5
      .locals (int32 V_0)
      IL_0000:  ldstr      "Trying index "
      IL_0005:  call       void [System.Console]System.Console::Write(string)
      IL_000a:  ldarga.s   idx
      IL_000c:  call       instance string [mscorlib]System.Int32::ToString()
      IL_0011:  call       void [System.Console]System.Console::Write(string)
      .try
      {
        IL_0016:  ldarg.0
        IL_0017:  ldarg.1
        IL_0018:  ldarg.1
        IL_0019:  stelem.i4
        IL_001a:  ldarg.0
        IL_001b:  ldarg.1
        IL_001c:  ldelem.i4
        IL_001d:  ldarg.1
        IL_001e:  ceq
        IL_0020:  brtrue     IL_0035

        IL_0025:  ldstr      "READ-WRITE FAILED"
        IL_002a:  call       void [System.Console]System.Console::WriteLine(string)
        IL_002f:  newobj     instance void [mscorlib]System.Exception::.ctor()
        IL_0034:  throw

        IL_0035:  ldstr      ": OK"
        IL_003a:  call       void [System.Console]System.Console::WriteLine(string)
        IL_003f:  ldc.i4.1
        IL_0040:  stloc.0
        IL_0041:  leave.s    IL_0052

      }  // end .try
      catch [mscorlib]System.Object 
      {
        IL_0043:  pop
        IL_0044:  ldstr      ": exception!"
        IL_0049:  call       void [System.Console]System.Console::WriteLine(string)
        IL_004e:  ldc.i4.0
        IL_004f:  stloc.0
        IL_0050:  leave.s    IL_0052

      }  // end handler
      IL_0052:  ldloc.0
      IL_0053:  ldarg.2
      IL_0054:  ceq
      IL_0056:  ret
    } // end of method Test::try_index

    .method private hidebysig static int32 
            Main() cil managed
    {
      .entrypoint
      .maxstack  4
      .locals (int32[0...] V_0,
               int32 V_1,
               int32 V_2)
      IL_0000:  ldstr      "Creating zero-length array (canonical form ctor)"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  ldc.i4.0
      IL_000b:  newobj     instance void int32[0...]::.ctor(int32)
      IL_0010:  stloc.0
      IL_0011:  ldstr      "Reported array size is "
      IL_0016:  call       void [System.Console]System.Console::Write(string)
      IL_001b:  ldloc.0
      IL_001c:  ldlen
      IL_001d:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_0022:  ldstr      "[mscorlib]System.Array::get_Rank returned "
      IL_0027:  call       void [System.Console]System.Console::Write(string)
      IL_002c:  ldloc.0
      IL_002d:  callvirt   instance int32 [mscorlib]System.Array::get_Rank()
      IL_0032:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_0037:  ldstr      "[mscorlib]System.Array::GetLowerBound returned "
      IL_003c:  call       void [System.Console]System.Console::Write(string)
      IL_0041:  ldloc.0
      IL_0042:  ldc.i4.0
      IL_0043:  callvirt   instance int32 [mscorlib]System.Array::GetLowerBound(int32)
      IL_0048:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_004d:  ldstr      "[mscorlib]System.Array::GetUpperBound returned "
      IL_0052:  call       void [System.Console]System.Console::Write(string)
      IL_0057:  ldloc.0
      IL_0058:  ldc.i4.0
      IL_0059:  callvirt   instance int32 [mscorlib]System.Array::GetUpperBound(int32)
      IL_005e:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_0063:  ldloc.0
      IL_0064:  ldc.i4     0xffffd8f0
      IL_0069:  ldc.i4.0
      IL_006a:  call       int32 JitTest.Test::try_index(int32[0...],
                                                         int32,
                                                         int32)
      IL_006f:  brfalse    IL_0168

      IL_0074:  ldloc.0
      IL_0075:  ldc.i4.m1
      IL_0076:  ldc.i4.0
      IL_0077:  call       int32 JitTest.Test::try_index(int32[0...],
                                                         int32,
                                                         int32)
      IL_007c:  brfalse    IL_0168

      IL_0081:  ldloc.0
      IL_0082:  ldc.i4.0
      IL_0083:  ldc.i4.0
      IL_0084:  call       int32 JitTest.Test::try_index(int32[0...],
                                                         int32,
                                                         int32)
      IL_0089:  brfalse    IL_0168

      IL_008e:  ldloc.0
      IL_008f:  ldc.i4.1
      IL_0090:  ldc.i4.0
      IL_0091:  call       int32 JitTest.Test::try_index(int32[0...],
                                                         int32,
                                                         int32)
      IL_0096:  brfalse    IL_0168

      IL_009b:  ldloc.0
      IL_009c:  ldc.i4     0x3e8
      IL_00a1:  ldc.i4.0
      IL_00a2:  call       int32 JitTest.Test::try_index(int32[0...],
                                                         int32,
                                                         int32)
      IL_00a7:  brfalse    IL_0168

      IL_00ac:  ldstr      "\nCreating zero-length array (newarr instruction)"
      IL_00b1:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b6:  ldc.i4.0
      IL_00b7:  newarr     int32
      IL_00bc:  stloc.0
      IL_00bd:  ldstr      "Reported array size is "
      IL_00c2:  call       void [System.Console]System.Console::Write(string)
      IL_00c7:  ldloc.0
      IL_00c8:  ldlen
      IL_00c9:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_00ce:  ldstr      "[mscorlib]System.Array::get_Rank returned "
      IL_00d3:  call       void [System.Console]System.Console::Write(string)
      IL_00d8:  ldloc.0
      IL_00d9:  callvirt   instance int32 [mscorlib]System.Array::get_Rank()
      IL_00de:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_00e3:  ldstr      "[mscorlib]System.Array::GetLowerBound returned "
      IL_00e8:  call       void [System.Console]System.Console::Write(string)
      IL_00ed:  ldloc.0
      IL_00ee:  ldc.i4.0
      IL_00ef:  callvirt   instance int32 [mscorlib]System.Array::GetLowerBound(int32)
      IL_00f4:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_00f9:  ldstr      "[mscorlib]System.Array::GetUpperBound returned "
      IL_00fe:  call       void [System.Console]System.Console::Write(string)
      IL_0103:  ldloc.0
      IL_0104:  ldc.i4.0
      IL_0105:  callvirt   instance int32 [mscorlib]System.Array::GetUpperBound(int32)
      IL_010a:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_010f:  ldloc.0
      IL_0110:  ldc.i4     0xffffd8f0
      IL_0115:  ldc.i4.0
      IL_0116:  call       int32 JitTest.Test::try_index(int32[0...],
                                                         int32,
                                                         int32)
      IL_011b:  brfalse    IL_0168

      IL_0120:  ldloc.0
      IL_0121:  ldc.i4.m1
      IL_0122:  ldc.i4.0
      IL_0123:  call       int32 JitTest.Test::try_index(int32[0...],
                                                         int32,
                                                         int32)
      IL_0128:  brfalse    IL_0168

      IL_012d:  ldloc.0
      IL_012e:  ldc.i4.0
      IL_012f:  ldc.i4.0
      IL_0130:  call       int32 JitTest.Test::try_index(int32[0...],
                                                         int32,
                                                         int32)
      IL_0135:  brfalse    IL_0168

      IL_013a:  ldloc.0
      IL_013b:  ldc.i4.1
      IL_013c:  ldc.i4.0
      IL_013d:  call       int32 JitTest.Test::try_index(int32[0...],
                                                         int32,
                                                         int32)
      IL_0142:  brfalse    IL_0168

      IL_0147:  ldloc.0
      IL_0148:  ldc.i4     0x3e8
      IL_014d:  ldc.i4.0
      IL_014e:  call       int32 JitTest.Test::try_index(int32[0...],
                                                         int32,
                                                         int32)
      IL_0153:  brfalse    IL_0168

      IL_0158:  ldc.i4 100
      IL_0159:  ldstr      "**** PASSED ****"
      IL_015e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0163:  ret

      IL_0168:  ldc.i4.1
      IL_0169:  ldstr      "**** FAILED ****"
      IL_016e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0173:  ret
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

} // end of namespace JitTest



