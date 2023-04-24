.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console
{
}

.assembly GitHub_23147
{
}
.assembly extern xunit.core {}

.class private sequential ansi sealed beforefieldinit Test_GitHub_23147.HFA_f2
       extends [System.Runtime]System.ValueType
{
  .field public float32 f1
  .field public float32 f2
} // end of class Test.HFA_f2

.class private sequential ansi sealed beforefieldinit Test_GitHub_23147.HFA_f3
       extends [System.Runtime]System.ValueType
{
  .field public float32 f1
  .field public float32 f2
  .field public float32 f3
} // end of class Test.HFA_f3

.class private sequential ansi sealed beforefieldinit Test_GitHub_23147.HFA_f4
       extends [System.Runtime]System.ValueType
{
  .field public float32 f1
  .field public float32 f2
  .field public float32 f3
  .field public float32 f4
} // end of class Test.HFA_f4

.class private sequential ansi sealed beforefieldinit Test_GitHub_23147.HFA_d2
       extends [System.Runtime]System.ValueType
{
  .field public float64 d1
  .field public float64 d2
} // end of class Test.HFA_d2

.class private sequential ansi sealed beforefieldinit Test_GitHub_23147.HFA_d3
       extends [System.Runtime]System.ValueType
{
  .field public float64 d1
  .field public float64 d2
  .field public float64 d3
} // end of class Test.HFA_d3

.class private sequential ansi sealed beforefieldinit Test_GitHub_23147.HFA_d4
       extends [System.Runtime]System.ValueType
{
  .field public float64 d1
  .field public float64 d2
  .field public float64 d3
  .field public float64 d4
} // end of class Test.HFA_d4

.class public auto ansi beforefieldinit Test_GitHub_23147.X
       extends [System.Runtime]System.Object
{
  .field public static float32 fx1
  .field public static float32 fx2
  .field public static float32 fx3
  .field public static float32 fx4
  .field public static float64 dx1
  .field public static float64 dx2
  .field public static float64 dx3
  .field public static float64 dx4
  .method public hidebysig static void  clear_argument_regs(int32 i1,
                                                            int32 i2,
                                                            int32 i3,
                                                            int32 i4,
                                                            int32 i5,
                                                            int32 i6,
                                                            int32 i7,
                                                            int32 i8,
                                                            int32 i9,
                                                            float64 f1,
                                                            float64 f2,
                                                            float64 f3,
                                                            float64 f4,
                                                            float64 f5,
                                                            float64 f6,
                                                            float64 f7,
                                                            float64 f8,
                                                            float64 f9) cil managed noinlining
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method X::clear_argument_regs

  .method public hidebysig static int32  test_f2(valuetype Test_GitHub_23147.HFA_f2 a) cil managed noinlining
  {
    // Code size       31 (0x1f)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 Test_GitHub_23147.HFA_f2::f1
    IL_0006:  ldsfld     float32 Test_GitHub_23147.X::fx1
    IL_000b:  bne.un.s   IL_001d

    IL_000d:  ldarg.0
    IL_000e:  ldfld      float32 Test_GitHub_23147.HFA_f2::f2
    IL_0013:  ldsfld     float32 Test_GitHub_23147.X::fx2
    IL_0018:  bne.un.s   IL_001d

    IL_001a:  ldc.i4.s   100
    IL_001c:  ret

    IL_001d:  ldc.i4.1
    IL_001e:  ret
  } // end of method X::test_f2

  .method public hidebysig static int32  test_f3(valuetype Test_GitHub_23147.HFA_f3 a) cil managed noinlining
  {
    // Code size       44 (0x2c)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 Test_GitHub_23147.HFA_f3::f1
    IL_0006:  ldsfld     float32 Test_GitHub_23147.X::fx1
    IL_000b:  bne.un.s   IL_002a

    IL_000d:  ldarg.0
    IL_000e:  ldfld      float32 Test_GitHub_23147.HFA_f3::f2
    IL_0013:  ldsfld     float32 Test_GitHub_23147.X::fx2
    IL_0018:  bne.un.s   IL_002a

    IL_001a:  ldarg.0
    IL_001b:  ldfld      float32 Test_GitHub_23147.HFA_f3::f3
    IL_0020:  ldsfld     float32 Test_GitHub_23147.X::fx3
    IL_0025:  bne.un.s   IL_002a

    IL_0027:  ldc.i4.s   100
    IL_0029:  ret

    IL_002a:  ldc.i4.1
    IL_002b:  ret
  } // end of method X::test_f3

  .method public hidebysig static int32  test_f4(valuetype Test_GitHub_23147.HFA_f4 a) cil managed noinlining
  {
    // Code size       57 (0x39)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 Test_GitHub_23147.HFA_f4::f1
    IL_0006:  ldsfld     float32 Test_GitHub_23147.X::fx1
    IL_000b:  bne.un.s   IL_0037

    IL_000d:  ldarg.0
    IL_000e:  ldfld      float32 Test_GitHub_23147.HFA_f4::f2
    IL_0013:  ldsfld     float32 Test_GitHub_23147.X::fx2
    IL_0018:  bne.un.s   IL_0037

    IL_001a:  ldarg.0
    IL_001b:  ldfld      float32 Test_GitHub_23147.HFA_f4::f3
    IL_0020:  ldsfld     float32 Test_GitHub_23147.X::fx3
    IL_0025:  bne.un.s   IL_0037

    IL_0027:  ldarg.0
    IL_0028:  ldfld      float32 Test_GitHub_23147.HFA_f4::f4
    IL_002d:  ldsfld     float32 Test_GitHub_23147.X::fx4
    IL_0032:  bne.un.s   IL_0037

    IL_0034:  ldc.i4.s   100
    IL_0036:  ret

    IL_0037:  ldc.i4.1
    IL_0038:  ret
  } // end of method X::test_f4

  .method public hidebysig static int32  test_d2(valuetype Test_GitHub_23147.HFA_d2 a) cil managed noinlining
  {
    // Code size       31 (0x1f)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float64 Test_GitHub_23147.HFA_d2::d1
    IL_0006:  ldsfld     float64 Test_GitHub_23147.X::dx1
    IL_000b:  bne.un.s   IL_001d

    IL_000d:  ldarg.0
    IL_000e:  ldfld      float64 Test_GitHub_23147.HFA_d2::d2
    IL_0013:  ldsfld     float64 Test_GitHub_23147.X::dx2
    IL_0018:  bne.un.s   IL_001d

    IL_001a:  ldc.i4.s   100
    IL_001c:  ret

    IL_001d:  ldc.i4.1
    IL_001e:  ret
  } // end of method X::test_d2

  .method public hidebysig static int32  test_d3(valuetype Test_GitHub_23147.HFA_d3 a) cil managed noinlining
  {
    // Code size       44 (0x2c)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float64 Test_GitHub_23147.HFA_d3::d1
    IL_0006:  ldsfld     float64 Test_GitHub_23147.X::dx1
    IL_000b:  bne.un.s   IL_002a

    IL_000d:  ldarg.0
    IL_000e:  ldfld      float64 Test_GitHub_23147.HFA_d3::d2
    IL_0013:  ldsfld     float64 Test_GitHub_23147.X::dx2
    IL_0018:  bne.un.s   IL_002a

    IL_001a:  ldarg.0
    IL_001b:  ldfld      float64 Test_GitHub_23147.HFA_d3::d3
    IL_0020:  ldsfld     float64 Test_GitHub_23147.X::dx3
    IL_0025:  bne.un.s   IL_002a

    IL_0027:  ldc.i4.s   100
    IL_0029:  ret

    IL_002a:  ldc.i4.1
    IL_002b:  ret
  } // end of method X::test_d3

  .method public hidebysig static int32  test_d4(valuetype Test_GitHub_23147.HFA_d4 a) cil managed noinlining
  {
    // Code size       57 (0x39)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float64 Test_GitHub_23147.HFA_d4::d1
    IL_0006:  ldsfld     float64 Test_GitHub_23147.X::dx1
    IL_000b:  bne.un.s   IL_0037

    IL_000d:  ldarg.0
    IL_000e:  ldfld      float64 Test_GitHub_23147.HFA_d4::d2
    IL_0013:  ldsfld     float64 Test_GitHub_23147.X::dx2
    IL_0018:  bne.un.s   IL_0037

    IL_001a:  ldarg.0
    IL_001b:  ldfld      float64 Test_GitHub_23147.HFA_d4::d3
    IL_0020:  ldsfld     float64 Test_GitHub_23147.X::dx3
    IL_0025:  bne.un.s   IL_0037

    IL_0027:  ldarg.0
    IL_0028:  ldfld      float64 Test_GitHub_23147.HFA_d4::d4
    IL_002d:  ldsfld     float64 Test_GitHub_23147.X::dx4
    IL_0032:  bne.un.s   IL_0037

    IL_0034:  ldc.i4.s   100
    IL_0036:  ret

    IL_0037:  ldc.i4.1
    IL_0038:  ret
  } // end of method X::test_d4

  .method public hidebysig static int32  jmp_test_f2(valuetype Test_GitHub_23147.HFA_f2 a) cil managed noinlining
  {
    // Code size       103 (0x67)
    .maxstack  18
    IL_0000:  ldc.i4.1
    IL_0001:  ldc.i4.2
    IL_0002:  ldc.i4.3
    IL_0003:  ldc.i4.4
    IL_0004:  ldc.i4.5
    IL_0005:  ldc.i4.6
    IL_0006:  ldc.i4.7
    IL_0007:  ldc.i4.8
    IL_0008:  ldc.i4.s   9
    IL_000a:  ldc.r8     1.
    IL_0013:  ldc.r8     2.
    IL_001c:  ldc.r8     3.
    IL_0025:  ldc.r8     4.
    IL_002e:  ldc.r8     5.
    IL_0037:  ldc.r8     6.
    IL_0040:  ldc.r8     7.
    IL_0049:  ldc.r8     8.
    IL_0052:  ldc.r8     9.
    IL_005b:  call       void Test_GitHub_23147.X::clear_argument_regs(int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64)
    jmp       int32 Test_GitHub_23147.X::test_f2(valuetype Test_GitHub_23147.HFA_f2)
  } // end of method X::jmp_test_f2

  .method public hidebysig static int32  jmp_test_f3(valuetype Test_GitHub_23147.HFA_f3 a) cil managed noinlining
  {
    // Code size       103 (0x67)
    .maxstack  18
    IL_0000:  ldc.i4.1
    IL_0001:  ldc.i4.2
    IL_0002:  ldc.i4.3
    IL_0003:  ldc.i4.4
    IL_0004:  ldc.i4.5
    IL_0005:  ldc.i4.6
    IL_0006:  ldc.i4.7
    IL_0007:  ldc.i4.8
    IL_0008:  ldc.i4.s   9
    IL_000a:  ldc.r8     1.
    IL_0013:  ldc.r8     2.
    IL_001c:  ldc.r8     3.
    IL_0025:  ldc.r8     4.
    IL_002e:  ldc.r8     5.
    IL_0037:  ldc.r8     6.
    IL_0040:  ldc.r8     7.
    IL_0049:  ldc.r8     8.
    IL_0052:  ldc.r8     9.
    IL_005b:  call       void Test_GitHub_23147.X::clear_argument_regs(int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64)
    jmp       int32 Test_GitHub_23147.X::test_f3(valuetype Test_GitHub_23147.HFA_f3)
  } // end of method X::jmp_test_f3

  .method public hidebysig static int32  jmp_test_f4(valuetype Test_GitHub_23147.HFA_f4 a) cil managed noinlining
  {
    // Code size       103 (0x67)
    .maxstack  18
    IL_0000:  ldc.i4.1
    IL_0001:  ldc.i4.2
    IL_0002:  ldc.i4.3
    IL_0003:  ldc.i4.4
    IL_0004:  ldc.i4.5
    IL_0005:  ldc.i4.6
    IL_0006:  ldc.i4.7
    IL_0007:  ldc.i4.8
    IL_0008:  ldc.i4.s   9
    IL_000a:  ldc.r8     1.
    IL_0013:  ldc.r8     2.
    IL_001c:  ldc.r8     3.
    IL_0025:  ldc.r8     4.
    IL_002e:  ldc.r8     5.
    IL_0037:  ldc.r8     6.
    IL_0040:  ldc.r8     7.
    IL_0049:  ldc.r8     8.
    IL_0052:  ldc.r8     9.
    IL_005b:  call       void Test_GitHub_23147.X::clear_argument_regs(int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64)
    jmp       int32 Test_GitHub_23147.X::test_f4(valuetype Test_GitHub_23147.HFA_f4)
  } // end of method X::jmp_test_f4

  .method public hidebysig static int32  jmp_test_d2(valuetype Test_GitHub_23147.HFA_d2 a) cil managed noinlining
  {
    // Code size       103 (0x67)
    .maxstack  18
    IL_0000:  ldc.i4.1
    IL_0001:  ldc.i4.2
    IL_0002:  ldc.i4.3
    IL_0003:  ldc.i4.4
    IL_0004:  ldc.i4.5
    IL_0005:  ldc.i4.6
    IL_0006:  ldc.i4.7
    IL_0007:  ldc.i4.8
    IL_0008:  ldc.i4.s   9
    IL_000a:  ldc.r8     1.
    IL_0013:  ldc.r8     2.
    IL_001c:  ldc.r8     3.
    IL_0025:  ldc.r8     4.
    IL_002e:  ldc.r8     5.
    IL_0037:  ldc.r8     6.
    IL_0040:  ldc.r8     7.
    IL_0049:  ldc.r8     8.
    IL_0052:  ldc.r8     9.
    IL_005b:  call       void Test_GitHub_23147.X::clear_argument_regs(int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64)
    jmp       int32 Test_GitHub_23147.X::test_d2(valuetype Test_GitHub_23147.HFA_d2)
  } // end of method X::jmp_test_d2

  .method public hidebysig static int32  jmp_test_d3(valuetype Test_GitHub_23147.HFA_d3 a) cil managed noinlining
  {
    // Code size       103 (0x67)
    .maxstack  18
    IL_0000:  ldc.i4.1
    IL_0001:  ldc.i4.2
    IL_0002:  ldc.i4.3
    IL_0003:  ldc.i4.4
    IL_0004:  ldc.i4.5
    IL_0005:  ldc.i4.6
    IL_0006:  ldc.i4.7
    IL_0007:  ldc.i4.8
    IL_0008:  ldc.i4.s   9
    IL_000a:  ldc.r8     1.
    IL_0013:  ldc.r8     2.
    IL_001c:  ldc.r8     3.
    IL_0025:  ldc.r8     4.
    IL_002e:  ldc.r8     5.
    IL_0037:  ldc.r8     6.
    IL_0040:  ldc.r8     7.
    IL_0049:  ldc.r8     8.
    IL_0052:  ldc.r8     9.
    IL_005b:  call       void Test_GitHub_23147.X::clear_argument_regs(int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64)
    jmp int32 Test_GitHub_23147.X::test_d3(valuetype Test_GitHub_23147.HFA_d3)
  } // end of method X::jmp_test_d3

  .method public hidebysig static int32  jmp_test_d4(valuetype Test_GitHub_23147.HFA_d4 a) cil managed noinlining
  {
    // Code size       103 (0x67)
    .maxstack  18
    IL_0000:  ldc.i4.1
    IL_0001:  ldc.i4.2
    IL_0002:  ldc.i4.3
    IL_0003:  ldc.i4.4
    IL_0004:  ldc.i4.5
    IL_0005:  ldc.i4.6
    IL_0006:  ldc.i4.7
    IL_0007:  ldc.i4.8
    IL_0008:  ldc.i4.s   9
    IL_000a:  ldc.r8     1.
    IL_0013:  ldc.r8     2.
    IL_001c:  ldc.r8     3.
    IL_0025:  ldc.r8     4.
    IL_002e:  ldc.r8     5.
    IL_0037:  ldc.r8     6.
    IL_0040:  ldc.r8     7.
    IL_0049:  ldc.r8     8.
    IL_0052:  ldc.r8     9.
    IL_005b:  call       void Test_GitHub_23147.X::clear_argument_regs(int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          int32,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64,
                                                          float64)
    jmp int32 Test_GitHub_23147.X::test_d4(valuetype Test_GitHub_23147.HFA_d4)
  } // end of method X::jmp_test_d4

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       579 (0x243)
    .maxstack  2
    .locals init (valuetype Test_GitHub_23147.HFA_f2 V_0,
             valuetype Test_GitHub_23147.HFA_f3 V_1,
             valuetype Test_GitHub_23147.HFA_f4 V_2,
             valuetype Test_GitHub_23147.HFA_d2 V_3,
             valuetype Test_GitHub_23147.HFA_d3 V_4,
             valuetype Test_GitHub_23147.HFA_d4 V_5,
             int32 V_6)
    IL_0000:  ldc.r4     1.1
    IL_0005:  stsfld     float32 Test_GitHub_23147.X::fx1
    IL_000a:  ldc.r4     2.2
    IL_000f:  stsfld     float32 Test_GitHub_23147.X::fx2
    IL_0014:  ldc.r4     3.3
    IL_0019:  stsfld     float32 Test_GitHub_23147.X::fx3
    IL_001e:  ldc.r4     4.4000001
    IL_0023:  stsfld     float32 Test_GitHub_23147.X::fx4
    IL_0028:  ldc.r8     1.
    IL_0031:  stsfld     float64 Test_GitHub_23147.X::dx1
    IL_0036:  ldc.r8     2.
    IL_003f:  stsfld     float64 Test_GitHub_23147.X::dx2
    IL_0044:  ldc.r8     3.
    IL_004d:  stsfld     float64 Test_GitHub_23147.X::dx3
    IL_0052:  ldc.r8     4.
    IL_005b:  stsfld     float64 Test_GitHub_23147.X::dx4
    IL_0060:  ldloca.s   V_0
    IL_0062:  initobj    Test_GitHub_23147.HFA_f2
    IL_0068:  ldloca.s   V_0
    IL_006a:  ldsfld     float32 Test_GitHub_23147.X::fx1
    IL_006f:  stfld      float32 Test_GitHub_23147.HFA_f2::f1
    IL_0074:  ldloca.s   V_0
    IL_0076:  ldsfld     float32 Test_GitHub_23147.X::fx2
    IL_007b:  stfld      float32 Test_GitHub_23147.HFA_f2::f2
    IL_0080:  ldloca.s   V_1
    IL_0082:  initobj    Test_GitHub_23147.HFA_f3
    IL_0088:  ldloca.s   V_1
    IL_008a:  ldsfld     float32 Test_GitHub_23147.X::fx1
    IL_008f:  stfld      float32 Test_GitHub_23147.HFA_f3::f1
    IL_0094:  ldloca.s   V_1
    IL_0096:  ldsfld     float32 Test_GitHub_23147.X::fx2
    IL_009b:  stfld      float32 Test_GitHub_23147.HFA_f3::f2
    IL_00a0:  ldloca.s   V_1
    IL_00a2:  ldsfld     float32 Test_GitHub_23147.X::fx3
    IL_00a7:  stfld      float32 Test_GitHub_23147.HFA_f3::f3
    IL_00ac:  ldloca.s   V_2
    IL_00ae:  initobj    Test_GitHub_23147.HFA_f4
    IL_00b4:  ldloca.s   V_2
    IL_00b6:  ldsfld     float32 Test_GitHub_23147.X::fx1
    IL_00bb:  stfld      float32 Test_GitHub_23147.HFA_f4::f1
    IL_00c0:  ldloca.s   V_2
    IL_00c2:  ldsfld     float32 Test_GitHub_23147.X::fx2
    IL_00c7:  stfld      float32 Test_GitHub_23147.HFA_f4::f2
    IL_00cc:  ldloca.s   V_2
    IL_00ce:  ldsfld     float32 Test_GitHub_23147.X::fx3
    IL_00d3:  stfld      float32 Test_GitHub_23147.HFA_f4::f3
    IL_00d8:  ldloca.s   V_2
    IL_00da:  ldsfld     float32 Test_GitHub_23147.X::fx4
    IL_00df:  stfld      float32 Test_GitHub_23147.HFA_f4::f4
    IL_00e4:  ldloca.s   V_3
    IL_00e6:  initobj    Test_GitHub_23147.HFA_d2
    IL_00ec:  ldloca.s   V_3
    IL_00ee:  ldsfld     float64 Test_GitHub_23147.X::dx1
    IL_00f3:  stfld      float64 Test_GitHub_23147.HFA_d2::d1
    IL_00f8:  ldloca.s   V_3
    IL_00fa:  ldsfld     float64 Test_GitHub_23147.X::dx2
    IL_00ff:  stfld      float64 Test_GitHub_23147.HFA_d2::d2
    IL_0104:  ldloca.s   V_4
    IL_0106:  initobj    Test_GitHub_23147.HFA_d3
    IL_010c:  ldloca.s   V_4
    IL_010e:  ldsfld     float64 Test_GitHub_23147.X::dx1
    IL_0113:  stfld      float64 Test_GitHub_23147.HFA_d3::d1
    IL_0118:  ldloca.s   V_4
    IL_011a:  ldsfld     float64 Test_GitHub_23147.X::dx2
    IL_011f:  stfld      float64 Test_GitHub_23147.HFA_d3::d2
    IL_0124:  ldloca.s   V_4
    IL_0126:  ldsfld     float64 Test_GitHub_23147.X::dx3
    IL_012b:  stfld      float64 Test_GitHub_23147.HFA_d3::d3
    IL_0130:  ldloca.s   V_5
    IL_0132:  initobj    Test_GitHub_23147.HFA_d4
    IL_0138:  ldloca.s   V_5
    IL_013a:  ldsfld     float64 Test_GitHub_23147.X::dx1
    IL_013f:  stfld      float64 Test_GitHub_23147.HFA_d4::d1
    IL_0144:  ldloca.s   V_5
    IL_0146:  ldsfld     float64 Test_GitHub_23147.X::dx2
    IL_014b:  stfld      float64 Test_GitHub_23147.HFA_d4::d2
    IL_0150:  ldloca.s   V_5
    IL_0152:  ldsfld     float64 Test_GitHub_23147.X::dx3
    IL_0157:  stfld      float64 Test_GitHub_23147.HFA_d4::d3
    IL_015c:  ldloca.s   V_5
    IL_015e:  ldsfld     float64 Test_GitHub_23147.X::dx4
    IL_0163:  stfld      float64 Test_GitHub_23147.HFA_d4::d4
    IL_0168:  ldc.i4.s   100
    IL_016a:  stloc.s    V_6
    IL_016c:  ldloc.0
    IL_016d:  call       int32 Test_GitHub_23147.X::jmp_test_f2(valuetype Test_GitHub_23147.HFA_f2)
    IL_0172:  ldc.i4.s   100
    IL_0174:  bne.un.s   IL_0182

    IL_0176:  ldstr      "jmp_test_f2 PASS"
    IL_017b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0180:  br.s       IL_018f

    IL_0182:  ldstr      "jmp_test_f2 FAIL"
    IL_0187:  call       void [System.Console]System.Console::WriteLine(string)
    IL_018c:  ldc.i4.1
    IL_018d:  stloc.s    V_6
    IL_018f:  ldloc.1
    IL_0190:  call       int32 Test_GitHub_23147.X::jmp_test_f3(valuetype Test_GitHub_23147.HFA_f3)
    IL_0195:  ldc.i4.s   100
    IL_0197:  bne.un.s   IL_01a5

    IL_0199:  ldstr      "jmp_test_f3 PASS"
    IL_019e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01a3:  br.s       IL_01b2

    IL_01a5:  ldstr      "jmp_test_f3 FAIL"
    IL_01aa:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01af:  ldc.i4.1
    IL_01b0:  stloc.s    V_6
    IL_01b2:  ldloc.2
    IL_01b3:  call       int32 Test_GitHub_23147.X::jmp_test_f4(valuetype Test_GitHub_23147.HFA_f4)
    IL_01b8:  ldc.i4.s   100
    IL_01ba:  bne.un.s   IL_01c8

    IL_01bc:  ldstr      "jmp_test_f4 PASS"
    IL_01c1:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01c6:  br.s       IL_01d5

    IL_01c8:  ldstr      "jmp_test_f4 FAIL"
    IL_01cd:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01d2:  ldc.i4.1
    IL_01d3:  stloc.s    V_6
    IL_01d5:  ldloc.3
    IL_01d6:  call       int32 Test_GitHub_23147.X::jmp_test_d2(valuetype Test_GitHub_23147.HFA_d2)
    IL_01db:  ldc.i4.s   100
    IL_01dd:  bne.un.s   IL_01eb

    IL_01df:  ldstr      "jmp_test_d2 PASS"
    IL_01e4:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01e9:  br.s       IL_01f8

    IL_01eb:  ldstr      "jmp_test_d2 FAIL"
    IL_01f0:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01f5:  ldc.i4.1
    IL_01f6:  stloc.s    V_6
    IL_01f8:  ldloc.s    V_4
    IL_01fa:  call       int32 Test_GitHub_23147.X::jmp_test_d3(valuetype Test_GitHub_23147.HFA_d3)
    IL_01ff:  ldc.i4.s   100
    IL_0201:  bne.un.s   IL_020f

    IL_0203:  ldstr      "jmp_test_d3 PASS"
    IL_0208:  call       void [System.Console]System.Console::WriteLine(string)
    IL_020d:  br.s       IL_021c

    IL_020f:  ldstr      "jmp_test_d3 FAIL"
    IL_0214:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0219:  ldc.i4.1
    IL_021a:  stloc.s    V_6
    IL_021c:  ldloc.s    V_5
    IL_021e:  call       int32 Test_GitHub_23147.X::jmp_test_d4(valuetype Test_GitHub_23147.HFA_d4)
    IL_0223:  ldc.i4.s   100
    IL_0225:  bne.un.s   IL_0233

    IL_0227:  ldstr      "jmp_test_d4 PASS"
    IL_022c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0231:  br.s       IL_0240

    IL_0233:  ldstr      "jmp_test_d4 FAIL"
    IL_0238:  call       void [System.Console]System.Console::WriteLine(string)
    IL_023d:  ldc.i4.1
    IL_023e:  stloc.s    V_6
    IL_0240:  ldloc.s    V_6
    IL_0242:  ret
  } // end of method X::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method X::.ctor

} // end of class Test.X
