// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}

.assembly GitHub_12761
{
}

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .class sequential ansi sealed nested public beforefieldinit IntWrapper
         extends [mscorlib]System.ValueType
  {
    .field public int32 i
  } // end of class IntWrapper

  // Sum1 and Sum2 are have tail-recursive calls that the jit turns into loops.
  // Both methods access locals that are not initialized by msil instructions; instead,
  // the methods have .locals init directives. The jit has to initialize the locals on each iteration
  // of the loop. The first test has a scalar local, the second test has a struct local.

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    .maxstack  8
              ldc.i4.0
              ldc.i4.s   10
              call       int32 Test::Sum1(int32,
                                          int32)
              ldc.i4.s   55
              beq      TEST2

              ldc.i4.m1
              ret

    TEST2:    ldc.i4.0
              ldc.i4.s   10
              call       int32 Test::Sum2(int32,
                                          int32)
              ldc.i4.s   55
              beq.s      SUCCESS

              ldc.i4.m1
              ret

    SUCCESS:  ldc.i4.s   100
              ret
  } // end of method Test::Main

  .method public hidebysig static int32  Sum1(int32 cur,
                                              int32 n) cil managed
  {
    .maxstack  3
    .locals init (int32 V_0)

              ldloc.0
              brfalse.s  L1

              ldc.i4     0x7d0
              ret

    L1:       ldarg.1
              brtrue.s   L2

              ldarg.0
              ret

    L2:       ldloc.0
              call       int32 Test::AddTwo(int32)
              stloc.0
              ldarg.0
              ldarg.1
              add
              ldarg.1
              ldc.i4.1
              sub
              call       int32 Test::Sum1(int32,
                                          int32)
              ret
  } // end of method Test::Sum1

  .method public hidebysig static int32  Sum2(int32 cur,
                                              int32 n) cil managed
  {
    .maxstack  3
    .locals init (valuetype Test/IntWrapper V_0)

              ldloc.0
              call       bool Test::CheckZero(valuetype Test/IntWrapper)
              brtrue.s   L1

              ldc.i4     0x7d0
              ret

    L1:       ldarg.1
              brtrue.s   L2

              ldarg.0
              ret

    L2:  ldloc.0
              call       valuetype Test/IntWrapper Test::AddTwo(valuetype Test/IntWrapper)
              stloc.0
              ldarg.0
              ldarg.1
              add
              ldarg.1
              ldc.i4.1
              sub
              call       int32 Test::Sum2(int32,
                                          int32)
              ret
  } // end of method Test::Sum2

  .method public hidebysig static int32  AddTwo(int32 i) cil managed noinlining
  {
    .maxstack  8
              ldarg.0
              ldc.i4.2
              add
              ret
  } // end of method Test::AddTwo

  .method public hidebysig static valuetype Test/IntWrapper 
          AddTwo(valuetype Test/IntWrapper wrapper) cil managed noinlining
  {
    .maxstack  8
              ldarga.s   wrapper
              ldflda     int32 Test/IntWrapper::i
              dup
              ldind.i4
              ldc.i4.2
              add
              stind.i4
              ldarg.0
              ret
  } // end of method Test::AddTwo

  .method public hidebysig static bool  CheckZero(valuetype Test/IntWrapper wrapper) cil managed noinlining
  {
    .maxstack  8
              ldarg.0
              ldfld      int32 Test/IntWrapper::i
              ldc.i4.0
              ceq
              ret
  } // end of method Test::CheckZero

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
              ldarg.0
              call       instance void [mscorlib]System.Object::.ctor()
              ret
  } // end of method Test::.ctor

} // end of class Test

// =============================================================
