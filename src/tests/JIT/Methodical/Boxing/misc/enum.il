// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'enum' { }
.assembly extern xunit.core {}
.namespace BoxTest_enum_il
{
  .class value auto ansi serializable sealed ToPrintOrNotToPrint extends [mscorlib]System.Enum
  {
    .field public specialname rtspecialname int32 value__
    .field public static literal value class BoxTest_enum_il.ToPrintOrNotToPrint Print = int32(0x00000000)
    .field public static literal value class BoxTest_enum_il.ToPrintOrNotToPrint DoNotPrint = int32(0x00000001)
  } // end of class 'ToPrintOrNotToPrint'

  .class public auto ansi Test extends [mscorlib]System.Object
  {
    .method family hidebysig instance class System.Object 
            Fibonacci(class System.Object num,
                      class System.Object flag) il managed
    {
      // Code size       49 (0x31)
      .maxstack  8
      IL_0000:  ldarg.2
      IL_0001:  unbox      BoxTest_enum_il.ToPrintOrNotToPrint
      IL_0006:  ldind.i4
      IL_0007:  ldc.i4.1
      IL_0008:  bne.un.s   IL_0013

      IL_000a:  ldarg.0
      IL_000b:  ldarg.1
      IL_000c:  ldarg.2
      IL_000d:  call       instance class System.Object BoxTest_enum_il.Test::Fibonacci2(class System.Object,
                                                                                 class System.Object)
      IL_0012:  ret

      IL_0013:  ldarg.1
      IL_0014:  unbox      [mscorlib]System.Int32
      IL_0019:  ldind.i4
      IL_001a:  ldc.i4.2
      IL_001b:  rem
      IL_001c:  ldc.i4.0
      IL_001d:  bne.un.s   IL_0028

      IL_001f:  ldarg.0
      IL_0020:  ldarg.1
      IL_0021:  ldarg.2
      IL_0022:  call       instance class System.Object BoxTest_enum_il.Test::Fibonacci2(class System.Object,
                                                                                 class System.Object)
      IL_0027:  ret

      IL_0028:  ldarg.0
      IL_0029:  ldarg.1
      IL_002a:  ldarg.2
      IL_002b:  call       instance class System.Object BoxTest_enum_il.Test::Fibonacci2(class System.Object,
                                                                                 class System.Object)
      IL_0030:  ret
    } // end of method 'Test::Fibonacci'

    .method family hidebysig instance class System.Object 
            Fibonacci2(class System.Object num,
                       class System.Object flag) il managed
    {
      // Code size       130 (0x82)
      .maxstack  4
      .locals ([0] int32 N,
               [1] int32 V_1,
               [2] value class BoxTest_enum_il.ToPrintOrNotToPrint V_2)
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
      IL_0021:  box [mscorlib]System.Int32
      IL_0026:  ldc.i4.1
      IL_0027:  stloc.2
      IL_0028:  ldloc.s   V_2
      IL_002a:  box BoxTest_enum_il.ToPrintOrNotToPrint
      IL_002f:  call       instance class System.Object BoxTest_enum_il.Test::Fibonacci(class System.Object,
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
      IL_0047:  box [mscorlib]System.Int32
      IL_004c:  ldarg.2
      IL_004d:  call       instance class System.Object BoxTest_enum_il.Test::Fibonacci(class System.Object,
                                                                                class System.Object)
      IL_0052:  unbox      [mscorlib]System.Int32
      IL_0057:  ldind.i4
      IL_0058:  add
      IL_0059:  stloc.0
      IL_005a:  ldarg.2
      IL_005b:  unbox      BoxTest_enum_il.ToPrintOrNotToPrint
      IL_0060:  ldind.i4
      IL_0061:  ldc.i4.0
      IL_0062:  bne.un.s   IL_007a

      IL_0064:  ldloca.s   N
      IL_0066:  call       instance class System.String [mscorlib]System.Int32::ToString()
      IL_006b:  ldstr      " "
      IL_0070:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0075:  call       void [System.Console]System.Console::Write(class System.String)
      IL_007a:  ldloc.s   N
      IL_007c:  box [mscorlib]System.Int32
      IL_0081:  ret
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
               [1] value class BoxTest_enum_il.ToPrintOrNotToPrint V_1)
      IL_0000:  newobj     instance void BoxTest_enum_il.Test::.ctor()
      IL_0005:  ldc.i4.s   20
      IL_0007:  stloc.0
      IL_0008:  ldloc.s   V_0
      IL_000a:  box [mscorlib]System.Int32
      IL_000f:  ldc.i4.0
      IL_0010:  stloc.1
      IL_0011:  ldloc.s   V_1
      IL_0013:  box BoxTest_enum_il.ToPrintOrNotToPrint
      IL_0018:  call       instance class System.Object BoxTest_enum_il.Test::Fibonacci(class System.Object,
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

} // end of namespace 'BoxTest_enum_il'

//*********** DISASSEMBLY COMPLETE ***********************
