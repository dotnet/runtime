// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'tailjump' { }
.assembly extern xunit.core {}
.namespace BoxTest_tailjump_il
{
  .class public auto ansi Test extends [mscorlib]System.Object
  {
    .method family hidebysig instance class System.Object 
            Fibonacci(class System.Object num,
                      class System.Object flag) il managed
    {
      // Code size       48 (0x30)
      .maxstack  8
      IL_0000:  ldarg.2
      IL_0001:  unbox      [mscorlib]System.Boolean
      IL_0006:  ldind.i1
      IL_0007:  brfalse.s  IL_0012

      IL_0009:  ldarg.0
      IL_000a:  ldarg.1
      IL_000b:  ldarg.2
      IL_000c:  tail. call       instance class System.Object BoxTest_tailjump_il.Test::Fibonacci2(class System.Object,
                                                                                 class System.Object)
      IL_0011:  ret

      IL_0012:  ldarg.1
      IL_0013:  unbox      [mscorlib]System.Int32
      IL_0018:  ldind.i4
      IL_0019:  ldc.i4.2
      IL_001a:  rem
      IL_001b:  ldc.i4.0
      IL_001c:  bne.un.s   IL_0027

				jmp        instance class System.Object BoxTest_tailjump_il.Test::Fibonacci2(class System.Object,
                                                                                 class System.Object)
                                                                                 
      IL_0027: 
      			jmp       instance class System.Object BoxTest_tailjump_il.Test::Fibonacci2(class System.Object,
                                                                                 class System.Object)
    } // end of method 'Test::Fibonacci'

    .method family hidebysig instance class System.Object 
            Fibonacci2(class System.Object num,
                       class System.Object flag) il managed
    {
      // Code size       129 (0x81)
      .maxstack  4
      .locals ([0] int32 N,
               [1] int32 V_1,
               [2] bool V_2)
      IL_0000:  ldarg.1
      IL_0001:  unbox      [mscorlib]System.Int32
      IL_0006:  ldind.i4
      IL_0007:  ldc.i4.1
      IL_0008:  bgt.s      IL_0014

      IL_000a:  ldarg.1
      IL_000b:  unbox      [mscorlib]System.Int32
      IL_0010:  ldind.i4
      IL_0011:  stloc.0
      IL_0012:  br.s       IL_005a

      IL_0014:  ldarg.0
      IL_0015:  ldarg.1
      IL_0016:  unbox      [mscorlib]System.Int32
      IL_001b:  ldind.i4
      IL_001c:  ldc.i4.2
      IL_001d:  sub
      IL_001e:  stloc.1
      IL_001f:  ldloc.s   V_1
      IL_0021:  box[mscorlib]System.Int32
      IL_0026:  ldc.i4.0
      IL_0027:  stloc.2
      IL_0028:  ldloc.s   V_2
      IL_002a:  box[mscorlib]System.Boolean
      IL_002f:  call       instance class System.Object BoxTest_tailjump_il.Test::Fibonacci(class System.Object,
                                                                                class System.Object)
      IL_0034:  unbox      [mscorlib]System.Int32
      IL_0039:  ldind.i4
      IL_003a:  ldarg.0
      IL_003b:  ldarg.1
      IL_003c:  unbox      [mscorlib]System.Int32
      IL_0041:  ldind.i4
      IL_0042:  ldc.i4.1
      IL_0043:  sub
      IL_0044:  stloc.1
      IL_0045:  ldloc.s   V_1
      IL_0047:  box[mscorlib]System.Int32
      IL_004c:  ldarg.2
      IL_004d:  call       instance class System.Object BoxTest_tailjump_il.Test::Fibonacci(class System.Object,
                                                                                class System.Object)
      IL_0052:  unbox      [mscorlib]System.Int32
      IL_0057:  ldind.i4
      IL_0058:  add
      IL_0059:  stloc.0
      IL_005a:  ldarg.2
      IL_005b:  unbox      [mscorlib]System.Boolean
      IL_0060:  ldind.i1
      IL_0061:  brfalse.s  IL_0079

      IL_0063:  ldloca.s   N
      IL_0065:  call       instance class System.String [mscorlib]System.Int32::ToString()
      IL_006a:  ldstr      " "
      IL_006f:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0074:  call       void [System.Console]System.Console::Write(class System.String)
      IL_0079:  ldloc.s   N
      IL_007b:  box[mscorlib]System.Int32
      IL_0080:  ret
    } // end of method 'Test::Fibonacci2'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       47 (0x2f)
      .maxstack  3
      .locals ([0] int32 V_0,
               [1] bool V_1)
      IL_0000:  newobj     instance void BoxTest_tailjump_il.Test::.ctor()
      IL_0005:  ldc.i4.s   20
      IL_0007:  stloc.0
      IL_0008:  ldloc.s   V_0
      IL_000a:  box[mscorlib]System.Int32
      IL_000f:  ldc.i4.1
      IL_0010:  stloc.1
      IL_0011:  ldloc.s   V_1
      IL_0013:  box[mscorlib]System.Boolean
      IL_0018:  call       instance class System.Object BoxTest_tailjump_il.Test::Fibonacci(class System.Object,
                                                                                class System.Object)
      IL_001d:  pop
      IL_001e:  call       void [System.Console]System.Console::WriteLine()
      IL_0023:  ldstr      "*** PASSED ***"
      IL_0028:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_002d:  ldc.i4    0x64
      IL_002e:  ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'BoxTest_tailjump_il'

//*********** DISASSEMBLY COMPLETE ***********************
