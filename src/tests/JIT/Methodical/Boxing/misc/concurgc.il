// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern System.Threading.Thread { auto }
.assembly extern mscorlib { auto }

.assembly ASSEMBLY_NAME// as "concur"
{
}
.assembly extern xunit.core {}
.namespace Test_concurgc_il
{
  .class public auto ansi App extends [mscorlib]System.Object
  {
    .field private static bool signal

    .method private hidebysig instance void
            Func() il managed
    {
      .maxstack  8
      IL_0002:  call       void [mscorlib]System.GC::Collect()
                ldc.i4 100
                call       void [System.Threading.Thread]System.Threading.Thread::Sleep(int32)
                volatile.
                ldsfld     bool Test_concurgc_il.App::signal
                brfalse.s  IL_0002
                ret
    } // end of method 'App::Func'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  3
      .locals ([0] class Test_concurgc_il.App app,
               [1] class [System.Threading.Thread]System.Threading.ThreadStart ts,
               [2] class [System.Threading.Thread]System.Threading.Thread th,
               [3] class System.Object obj,
               [4] class System.Object I,
               [5] float64 V_5,
               [6] int32 V_6)
      IL_0000:  newobj     instance void Test_concurgc_il.App::.ctor()
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldftn      instance void Test_concurgc_il.App::Func()
      IL_000d:  newobj     instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(class System.Object,native int)
      IL_0012:  stloc.1
      IL_0013:  ldloc.1
      IL_0014:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
      IL_0019:  stloc.2
                ldc.i4.0
                volatile.
                stsfld     bool  Test_concurgc_il.App::signal
      IL_001a:  ldloc.2
      IL_001b:  call       instance void [System.Threading.Thread]System.Threading.Thread::Start()
      IL_0020:  ldc.r8     11.
      IL_0029:  stloc.s    V_5
      IL_002b:  ldloc.s   V_5
      IL_002d:  box [mscorlib]System.Double
      IL_0032:  stloc.3
      IL_0033:  ldc.i4.0
      IL_0034:  stloc.s    V_6
      IL_0036:  ldloc.s   V_6
      IL_0038:  box [mscorlib]System.Int32
      IL_003d:  stloc.s    I
      IL_003f:  br       IL_00b2

      IL_0041:  ldloc.3
      IL_0042:  unbox      [mscorlib]System.Double
      IL_0047:  ldind.r8
      IL_0048:  stloc.s    V_5
      IL_004a:  ldloca.s   V_5
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
                ldobj        [mscorlib]System.Double
                box        [mscorlib]System.Double
                unbox      [mscorlib]System.Double
      IL_0092:  ldind.r8
      IL_0093:  stloc.s    V_5
      IL_0095:  ldloc.s   V_5
      IL_0097:  box [mscorlib]System.Double
      IL_009c:  stloc.3
      IL_009d:  ldloc.s    I
      IL_009f:  unbox      [mscorlib]System.Int32
      IL_00a4:  ldind.i4
      IL_00a5:  ldc.i4.1
      IL_00a6:  add
      IL_00a7:  stloc.s    V_6
      IL_00a9:  ldloc.s   V_6
      IL_00ab:  box [mscorlib]System.Int32
      IL_00b0:  stloc.s    I
      IL_00b2:  ldloc.s    I
      IL_00b4:  unbox      [mscorlib]System.Int32
      IL_00b9:  ldind.i4
      IL_00ba:  ldc.i4     50
      IL_00bf:  blt      IL_0041

      IL_00c1:  ldc.i4.1
                volatile.
      IL_00c2:  stsfld     bool  Test_concurgc_il.App::signal
      IL_00c7:  ldstr      "**** PASSED ****"
      IL_00cc:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00d1:  ldc.i4    0x64
      IL_00d2:  ret
    } // end of method 'App::Main'

    .method public hidebysig specialname rtspecialname
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'App::.ctor'

  } // end of class 'App'

} // end of namespace 'Test_concurgc_il'

