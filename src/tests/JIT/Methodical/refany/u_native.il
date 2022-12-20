// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'u_native'// as "native"
{
}
.assembly extern xunit.core {}
// MVID: {C18F1061-8239-420B-AC69-D4CB0BB38F2B}
.namespace JitTest_u_native_refany_il
{
  .class public auto ansi Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       92 (0x5c)
      .maxstack  2
      .locals (native unsigned int V_0,
               typedref V_1,
               native unsigned int V_2)
      IL_0000:  ldc.i4     0x1492
      IL_0005:  stloc.0
      IL_0006:  ldloca.s   V_0
      IL_0008:  mkrefany   native unsigned int
      IL_000d:  stloc.1
      IL_000e:  ldloc.1
      IL_000f:  refanytype
      IL_0011:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0016:  ldtoken    native unsigned int
      IL_001b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0020:  beq.s      IL_0030

      IL_0022:  ldstr      "Type does not match. Test failed"
      IL_0027:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_002c:  ldc.i4.1
      IL_002d:  stloc.2
      IL_002e:  br.s       IL_005a

      IL_0030:  ldloc.1
      IL_0031:  refanyval  native unsigned int
      IL_0036:  ldind.i
      IL_0037:  ldc.i4     0x1492
      IL_003c:  beq.s      IL_004c

      IL_003e:  ldstr      "Value does not match. Test failed"
      IL_0043:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0048:  ldc.i4.2
      IL_0049:  stloc.2
      IL_004a:  br.s       IL_005a

      IL_004c:  ldstr      "Test passed"
      IL_0051:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0056:  ldc.i4 100
      IL_0057:  stloc.2
      IL_0058:  br.s       IL_005a

      IL_005a:  ldloc.2
      IL_005b:  ret
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest_u_native_refany_il

//*********** DISASSEMBLY COMPLETE ***********************
