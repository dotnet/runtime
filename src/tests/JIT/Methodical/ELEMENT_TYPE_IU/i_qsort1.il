// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME// as "qsort"
{
}
.assembly extern xunit.core {}
// MVID: {D2333E3D-2CA6-4703-8D46-E41A46BDF801}
.namespace QSortTest_i_qsort1
{
  .class public auto ansi Sorter
         extends [mscorlib]System.Object
  {
    .method private hidebysig static void Sort(native int[] A,
                 native int L, native int H) il managed
    {
      // Code size       178 (0xb2)
      .maxstack  5
      .locals (native int[] V_0,
               native int V_1,
               native int V_2,
               native int V_3,
               native int V_4)
      IL_0000:  ldarg.1
      IL_0001:  ldarg.2
      IL_0002:  bge        IL_00b1

      IL_0007:  ldc.i4.s   64
      IL_0009:  newarr     [mscorlib]System.IntPtr
      IL_000e:  stloc.0
      IL_000f:  ldc.i4.0
      IL_0010:  stloc.1
      IL_0011:  br         IL_00ac

      IL_0016:  ldarg.1
      IL_0017:  ldc.i4.1
      IL_0018:  sub
      IL_0019:  stloc.2
      IL_001a:  ldarg.2
      IL_001b:  ldc.i4.1
      IL_001c:  add
      IL_001d:  stloc.3
      IL_001e:  ldarg.0
      IL_001f:  ldarg.1
      IL_0020:  ldarg.2
      IL_0021:  add
      IL_0022:  ldc.i4.2
      IL_0023:  div
      IL_0024:  ldelem.i
      IL_0025:  stloc.s    V_4
      IL_0027:  br.s       IL_0067

      IL_0029:  br.s       IL_002b

      IL_002b:  ldarg.0
      IL_002c:  ldloc.3
      IL_002d:  ldc.i4.1
      IL_002e:  sub
      IL_002f:  dup
      IL_0030:  stloc.3
      IL_0031:  ldelem.i
      IL_0032:  ldloc.s    V_4
      IL_0034:  bgt.s      IL_002b

      IL_0036:  br.s       IL_0038

      IL_0038:  ldarg.0
      IL_0039:  ldloc.2
      IL_003a:  ldc.i4.1
      IL_003b:  add
      IL_003c:  dup
      IL_003d:  stloc.2
      IL_003e:  ldelem.i
      IL_003f:  ldloc.s    V_4
      IL_0041:  blt.s      IL_0038

      IL_0043:  ldloc.2
      IL_0044:  ldloc.3
      IL_0045:  blt.s      IL_0049

      IL_0047:  br.s       IL_0069

      IL_0049:  ldarg.0
      IL_004a:  ldloc.2
      IL_004b:  ldarg.0
      IL_004c:  ldloc.2
      IL_004d:  ldelem.i
      IL_004e:  ldarg.0
      IL_004f:  ldloc.3
      IL_0050:  ldelem.i
      IL_0051:  xor
      IL_0052:  stelem.i
      IL_0053:  ldarg.0
      IL_0054:  ldloc.3
      IL_0055:  ldarg.0
      IL_0056:  ldloc.2
      IL_0057:  ldelem.i
      IL_0058:  ldarg.0
      IL_0059:  ldloc.3
      IL_005a:  ldelem.i
      IL_005b:  xor
      IL_005c:  stelem.i
      IL_005d:  ldarg.0
      IL_005e:  ldloc.2
      IL_005f:  ldarg.0
      IL_0060:  ldloc.2
      IL_0061:  ldelem.i
      IL_0062:  ldarg.0
      IL_0063:  ldloc.3
      IL_0064:  ldelem.i
      IL_0065:  xor
      IL_0066:  stelem.i
      IL_0067:  br.s       IL_0029

      IL_0069:  ldarg.1
      IL_006a:  ldloc.3
      IL_006b:  bge.s      IL_007d

      IL_006d:  ldloc.0
      IL_006e:  ldloc.1
      IL_006f:  dup
      IL_0070:  ldc.i4.1
      IL_0071:  add
      IL_0072:  stloc.1
      IL_0073:  ldarg.1
      IL_0074:  stelem.i
      IL_0075:  ldloc.0
      IL_0076:  ldloc.1
      IL_0077:  dup
      IL_0078:  ldc.i4.1
      IL_0079:  add
      IL_007a:  stloc.1
      IL_007b:  ldloc.3
      IL_007c:  stelem.i
      IL_007d:  ldloc.3
      IL_007e:  ldc.i4.1
      IL_007f:  add
      IL_0080:  ldarg.2
      IL_0081:  bge.s      IL_0095

      IL_0083:  ldloc.0
      IL_0084:  ldloc.1
      IL_0085:  dup
      IL_0086:  ldc.i4.1
      IL_0087:  add
      IL_0088:  stloc.1
      IL_0089:  ldloc.3
      IL_008a:  ldc.i4.1
      IL_008b:  add
      IL_008c:  stelem.i
      IL_008d:  ldloc.0
      IL_008e:  ldloc.1
      IL_008f:  dup
      IL_0090:  ldc.i4.1
      IL_0091:  add
      IL_0092:  stloc.1
      IL_0093:  ldarg.2
      IL_0094:  stelem.i
      IL_0095:  ldloc.1
      IL_0096:  brtrue.s   IL_009a

      IL_0098:  br.s       IL_00b1

      IL_009a:  ldloc.0
      IL_009b:  ldloc.1
      IL_009c:  ldc.i4.1
      IL_009d:  sub
      IL_009e:  dup
      IL_009f:  stloc.1
      IL_00a0:  ldelem.i
      IL_00a1:  starg.s    H
      IL_00a3:  ldloc.0
      IL_00a4:  ldloc.1
      IL_00a5:  ldc.i4.1
      IL_00a6:  sub
      IL_00a7:  dup
      IL_00a8:  stloc.1
      IL_00a9:  ldelem.i
      IL_00aa:  starg.s    L
      IL_00ac:  br         IL_0016

      IL_00b1:  ret
    } // end of method 'Sorter::Sort'

    .method public hidebysig static int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       134 (0x86)
      .maxstack  4
      .locals (native int V_0,
               native int[] V_1,
               native int V_2,
               native int V_3,
               native int V_4,
               native int V_5)
      IL_0000:  ldc.i4     0x1f40
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldc.i4.1
      IL_0008:  add
      IL_0009:  newarr     [mscorlib]System.IntPtr
      IL_000e:  stloc.1
      IL_000f:  ldloc.0
      IL_0010:  ldc.i4     0x7cf
      IL_0015:  mul
      IL_0016:  stloc.2
      IL_0017:  ldc.i4.1
      IL_0018:  stloc.3
      IL_0019:  br.s       IL_0031

      IL_001b:  ldloc.1
      IL_001c:  ldloc.3
      IL_001d:  ldloc.2
      IL_001e:  ldc.i4.s   19
      IL_0020:  add.ovf
      IL_0021:  ldc.i4.s   17
      IL_0023:  mul.ovf
      IL_0024:  ldc.i4     0x7cf
      IL_0029:  rem
      IL_002a:  dup
      IL_002b:  stloc.2
      			ldc.i4 600
      			sub
      IL_002c:  stelem.i
      IL_002d:  ldloc.3
      IL_002e:  ldc.i4.1
      IL_002f:  add
      IL_0030:  stloc.3
      IL_0031:  ldloc.3
      IL_0032:  ldloc.0
      IL_0033:  ble.s      IL_001b

      IL_0035:  ldloc.1
      IL_0036:  ldc.i4.1
      IL_0037:  ldloc.0
      IL_0038:  call       void QSortTest_i_qsort1.Sorter::Sort(native int[],
                                                       native int,
                                                       native int)
      IL_003d:  ldc.i4.2
      IL_003e:  stloc.s    V_4
      IL_0040:  br.s       IL_006f

      IL_0042:  ldloc.1
      IL_0043:  ldloc.s    V_4
      IL_0045:  ldc.i4.1
      IL_0046:  sub
      IL_0047:  ldelem.i
      IL_0048:  ldloc.1
      IL_0049:  ldloc.s    V_4
      IL_004b:  ldelem.i
      IL_004c:  ble.s      IL_0069

      IL_004e:  ldstr      "Test failed at index "
      IL_0053:  ldloca.s   V_4
      IL_0055:  call       instance class [mscorlib]System.String [mscorlib]System.IntPtr::ToString()
      IL_005a:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String,
                                                                                 class [mscorlib]System.String)
      IL_005f:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0064:  ldc.i4.1
      IL_0065:  stloc.s    V_5
      IL_0067:  br.s       IL_0083

      IL_0069:  ldloc.s    V_4
      IL_006b:  ldc.i4.1
      IL_006c:  add
      IL_006d:  stloc.s    V_4
      IL_006f:  ldloc.s    V_4
      IL_0071:  ldloc.0
      IL_0072:  ble.s      IL_0042

      IL_0074:  ldstr      "qsort test passed"
      IL_0079:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_007e:  ldc.i4    0x64
      IL_007f:  stloc.s    V_5
      IL_0081:  br.s       IL_0083

      IL_0083:  ldloc.s    V_5
      IL_0085:  ret
    } // end of method 'Sorter::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Sorter::.ctor'

  } // end of class 'Sorter'

} // end of namespace 'QSortTest_i_qsort1'

//*********** DISASSEMBLY COMPLETE ***********************
