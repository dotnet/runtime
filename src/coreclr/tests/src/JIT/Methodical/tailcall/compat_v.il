// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly compat_v { }
.namespace JitTest
{
  .class public sequential ansi sealed beforefieldinit BaseStruct
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 1024
    .method public hidebysig  static 
            value class JitTest.BaseStruct 
            caller(bool which) il managed
    {
      // Code size       22 (0x16)
      .maxstack  1
      .locals (value class JitTest.BaseStruct V_0,
               value class JitTest.BaseStruct V_1)
      IL_0000:  ldarg.0
      IL_0001:  brfalse.s  IL_000e

      IL_0003:  ldloca.s   V_1
      IL_0005:  initobj    JitTest.BaseStruct
      IL_000b:  ldloc.1
      IL_000c:  br.s       IL_0015

      IL_000e:  tail.
      IL_0010:  call       value class JitTest.BaseStruct JitTest.BaseStruct::callee()
      IL_0015:  ret
    } // end of method BaseStruct::caller

    .method public hidebysig  static 
            value class JitTest.BaseStruct
            callee() il managed
    {
      // Code size       14 (0xe)
      .maxstack  1
      .locals (value class JitTest.BaseStruct V_0,
               value class JitTest.BaseStruct V_1)
      IL_0000:  ldloca.s   V_1
      IL_0002:  initobj    JitTest.BaseStruct
      IL_0008:  ldloc.1
      IL_0009:  stloc.0
      IL_000a:  br.s       IL_000c

      IL_000c:  ldloc.0
      IL_000d:  ret
    } // end of method BaseStruct::callee

    .method public hidebysig  static 
            int32 Main() il managed
    {
      .entrypoint
      // Code size       55 (0x37)
      .maxstack  1
      .locals (int32 V_0)
      IL_0000:  ldc.i4.0
      IL_0001:  call       value class JitTest.BaseStruct JitTest.BaseStruct::caller(bool)
      //IL_0006:  stloc.1
      //IL_0007:  ldloc.s   V_1
      IL_0009:  box        JitTest.BaseStruct
      IL_000e:  call       void [System.Console]System.Console::WriteLine(class System.Object)
      IL_0013:  ldc.i4.1
      IL_0014:  call       value class JitTest.BaseStruct JitTest.BaseStruct::caller(bool)
      //IL_0019:  stloc.1
      //IL_001a:  ldloc.s   V_1
      IL_001c:  box        JitTest.BaseStruct
      IL_0021:  call       void [System.Console]System.Console::WriteLine(class System.Object)
      IL_0026:  ldstr      "passed"
      IL_002b:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0030:  ldc.i4.s   100
      			ret
    } // end of method BaseStruct::Main

  } // end of class BaseStruct

} // end of namespace JitTest

//*********** DISASSEMBLY COMPLETE ***********************
