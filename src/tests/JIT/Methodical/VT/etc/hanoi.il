// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.file alignment 512

.assembly extern mscorlib
{
}
.assembly hanoi // as "hanoi"
{
}
.assembly extern xunit.core {}
.namespace JitTest_hanoi_etc_il
{
  .class value private auto ansi sealed CI
         extends [mscorlib]System.ValueType
  {
    .field public int32 index
  } // end of class CI

  .class public auto ansi Test
         extends [mscorlib]System.Object
  {
    .field private static int32[][] cols
    .field private static int32[] heights
    .method private hidebysig static void
            test() il managed
    {
      .maxstack  3
      .locals (int32 V_0,
               int32 V_1)
      IL_0000:  ldc.i4.0
      IL_0001:  stloc.0
      IL_0002:  br.s       IL_0036

      IL_0004:  ldc.i4.1
      IL_0005:  stloc.1
      IL_0006:  br.s       IL_0028

      IL_0008:  ldsfld     int32[][] JitTest_hanoi_etc_il.Test::cols
      IL_000d:  ldloc.0
      IL_000e:  ldelem.ref
      IL_000f:  ldloc.1
      IL_0010:  ldc.i4.1
      IL_0011:  sub
      IL_0012:  ldelem.i4
      IL_0013:  ldsfld     int32[][] JitTest_hanoi_etc_il.Test::cols
      IL_0018:  ldloc.0
      IL_0019:  ldelem.ref
      IL_001a:  ldloc.1
      IL_001b:  ldelem.i4
      IL_001c:  bgt.s      IL_0024

      IL_001e:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0023:  throw

      IL_0024:  ldloc.1
      IL_0025:  ldc.i4.1
      IL_0026:  add
      IL_0027:  stloc.1
      IL_0028:  ldloc.1
      IL_0029:  ldsfld     int32[] JitTest_hanoi_etc_il.Test::heights
      IL_002e:  ldloc.0
      IL_002f:  ldelem.i4
      IL_0030:  blt.s      IL_0008

      IL_0032:  ldloc.0
      IL_0033:  ldc.i4.1
      IL_0034:  add
      IL_0035:  stloc.0
      IL_0036:  ldloc.0
      IL_0037:  ldc.i4.3
      IL_0038:  blt.s      IL_0004

      IL_003a:  ret
    } // end of method Test::test

    .method private hidebysig static void 
            move1(value class JitTest_hanoi_etc_il.CI from,
                  value class JitTest_hanoi_etc_il.CI 'to') il managed
    {
      .maxstack  7
      .locals (int32[] V_0,
               int32 V_1,
               int32 V_2)
      IL_0000:  ldsfld     int32[][] JitTest_hanoi_etc_il.Test::cols
      IL_0005:  ldarga.s   'to'
      IL_0007:  ldfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_000c:  ldelem.ref
      IL_000d:  ldsfld     int32[] JitTest_hanoi_etc_il.Test::heights
      IL_0012:  dup
      IL_0013:  stloc.0
      IL_0014:  ldarga.s   'to'
      IL_0016:  ldfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_001b:  dup
      IL_001c:  stloc.1
      IL_001d:  ldloc.0
      IL_001e:  ldloc.1
      IL_001f:  ldelem.i4
      IL_0020:  dup
      IL_0021:  stloc.2
      IL_0022:  ldc.i4.1
      IL_0023:  add
      IL_0024:  stelem.i4
      IL_0025:  ldloc.2
      IL_0026:  ldsfld     int32[][] JitTest_hanoi_etc_il.Test::cols
      IL_002b:  ldarga.s   from
      IL_002d:  ldfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_0032:  ldelem.ref
      IL_0033:  ldsfld     int32[] JitTest_hanoi_etc_il.Test::heights
      IL_0038:  dup
      IL_0039:  stloc.0
      IL_003a:  ldarga.s   from
      IL_003c:  ldfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_0041:  dup
      IL_0042:  stloc.1
      IL_0043:  ldloc.0
      IL_0044:  ldloc.1
      IL_0045:  ldelem.i4
      IL_0046:  ldc.i4.1
      IL_0047:  sub
      IL_0048:  dup
      IL_0049:  stloc.2
      IL_004a:  stelem.i4
      IL_004b:  ldloc.2
      IL_004c:  ldelem.i4
      IL_004d:  stelem.i4
      IL_004e:  call       void JitTest_hanoi_etc_il.Test::test()
      IL_0053:  ret
    } // end of method Test::move1

    .method private hidebysig static void 
            move(value class JitTest_hanoi_etc_il.CI from,
                 value class JitTest_hanoi_etc_il.CI 'to',
                 int32 num) il managed
    {
      .maxstack  4
      .locals (value class JitTest_hanoi_etc_il.CI V_0,
               value class JitTest_hanoi_etc_il.CI V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    JitTest_hanoi_etc_il.CI
      IL_0008:  ldloca.s   V_1
      IL_000a:  initobj    JitTest_hanoi_etc_il.CI
      IL_0010:  ldarg.2
      IL_0011:  ldc.i4.1
      IL_0012:  bne.un.s   IL_001d

      IL_0014:  ldarg.0
      IL_0015:  ldarg.1
      IL_0016:  call       void JitTest_hanoi_etc_il.Test::move1(value class JitTest_hanoi_etc_il.CI,
                                                    value class JitTest_hanoi_etc_il.CI)
      IL_001b:  br.s       IL_0084

      IL_001d:  ldloca.s   V_0
      IL_001f:  ldarga.s   from
      IL_0021:  ldfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_0026:  stfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_002b:  ldloca.s   V_1
      IL_002d:  ldc.i4.3
      IL_002e:  ldarga.s   from
      IL_0030:  ldfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_0035:  sub
      IL_0036:  ldarga.s   'to'
      IL_0038:  ldfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_003d:  sub
      IL_003e:  stfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_0043:  ldloc.0
      IL_0044:  ldloc.1
      IL_0045:  ldarg.2
      IL_0046:  ldc.i4.1
      IL_0047:  sub
      IL_0048:  call       void JitTest_hanoi_etc_il.Test::move(value class JitTest_hanoi_etc_il.CI,
                                                   value class JitTest_hanoi_etc_il.CI,
                                                   int32)
      IL_004d:  ldarg.0
      IL_004e:  ldarg.1
      IL_004f:  call       void JitTest_hanoi_etc_il.Test::move1(value class JitTest_hanoi_etc_il.CI,
                                                    value class JitTest_hanoi_etc_il.CI)
      IL_0054:  ldloca.s   V_0
      IL_0056:  ldc.i4.3
      IL_0057:  ldarga.s   from
      IL_0059:  ldfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_005e:  sub
      IL_005f:  ldarga.s   'to'
      IL_0061:  ldfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_0066:  sub
      IL_0067:  stfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_006c:  ldloca.s   V_1
      IL_006e:  ldarga.s   'to'
      IL_0070:  ldfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_0075:  stfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_007a:  ldloc.0
      IL_007b:  ldloc.1
      IL_007c:  ldarg.2
      IL_007d:  ldc.i4.1
      IL_007e:  sub
      IL_007f:  call       void JitTest_hanoi_etc_il.Test::move(value class JitTest_hanoi_etc_il.CI,
                                                   value class JitTest_hanoi_etc_il.CI,
                                                   int32)
      IL_0084:  ret
    } // end of method Test::move

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  4
      .zeroinit
      .locals (int32 V_0,
               int32 V_1,
               value class JitTest_hanoi_etc_il.CI V_2,
               value class JitTest_hanoi_etc_il.CI V_3,
               int32 V_4,
               int32[] V_5)
     
      IL_0010:  ldc.i4.4
      IL_0011:  stloc.0
      IL_0012:  ldc.i4.3
      IL_0013:  newarr     int32[]
      IL_0018:  stsfld     int32[][] JitTest_hanoi_etc_il.Test::cols
      IL_001d:  ldsfld     int32[][] JitTest_hanoi_etc_il.Test::cols
      IL_0022:  ldc.i4.0
      IL_0023:  ldloc.0
      IL_0024:  newarr     [mscorlib]System.Int32
      IL_0029:  stelem.ref
      IL_002a:  ldsfld     int32[][] JitTest_hanoi_etc_il.Test::cols
      IL_002f:  ldc.i4.1
      IL_0030:  ldloc.0
      IL_0031:  newarr     [mscorlib]System.Int32
      IL_0036:  stelem.ref
      IL_0037:  ldsfld     int32[][] JitTest_hanoi_etc_il.Test::cols
      IL_003c:  ldc.i4.2
      IL_003d:  ldloc.0
      IL_003e:  newarr     [mscorlib]System.Int32
      IL_0043:  stelem.ref
      IL_0044:  ldc.i4.3
      IL_0045:  newarr     [mscorlib]System.Int32
      IL_004a:  stloc.s    V_5
      IL_004c:  ldloc.s    V_5
      IL_004e:  ldc.i4.0
      IL_004f:  ldloc.0
      IL_0050:  stelem.i4
      IL_0051:  ldloc.s    V_5
      IL_0053:  stsfld     int32[] JitTest_hanoi_etc_il.Test::heights
      IL_0058:  ldc.i4.0
      IL_0059:  stloc.1
      IL_005a:  br.s       IL_006c

      IL_005c:  ldsfld     int32[][] JitTest_hanoi_etc_il.Test::cols
      IL_0061:  ldc.i4.0
      IL_0062:  ldelem.ref
      IL_0063:  ldloc.1
      IL_0064:  ldloc.0
      IL_0065:  ldloc.1
      IL_0066:  sub
      IL_0067:  stelem.i4
      IL_0068:  ldloc.1
      IL_0069:  ldc.i4.1
      IL_006a:  add
      IL_006b:  stloc.1
      IL_006c:  ldloc.1
      IL_006d:  ldloc.0
      IL_006e:  blt.s      IL_005c

      IL_0070:  call       void JitTest_hanoi_etc_il.Test::test()
      IL_0075:  ldloca.s   V_2
      IL_0077:  ldc.i4.0
      IL_0078:  stfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_007d:  ldloca.s   V_3
      IL_007f:  ldc.i4.1
      IL_0080:  stfld      int32 JitTest_hanoi_etc_il.CI::index
      IL_0085:  ldloc.2
      IL_0086:  ldloc.3
      IL_0087:  ldloc.0
      IL_0088:  call       void JitTest_hanoi_etc_il.Test::move(value class JitTest_hanoi_etc_il.CI,
                                                   value class JitTest_hanoi_etc_il.CI,
                                                   int32)
      ldc.i4 100
      ret
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest_hanoi_etc_il

