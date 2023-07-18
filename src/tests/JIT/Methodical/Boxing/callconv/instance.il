// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
}
.assembly ASSEMBLY_NAME
{
}
.assembly extern xunit.core {}
.namespace BoxTest_instance_il
{
  .class abstract auto ansi BaseTest extends [mscorlib]System.Object
  {
    .method family hidebysig newslot virtual abstract 
            instance class System.Object 
            Fibonacci2(class System.Object num,
                       class System.Object flag) il managed{ }
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'BaseTest::.ctor'

  } // end of class 'BaseTest'

  .class public auto ansi Test extends BoxTest_instance_il.BaseTest
  {
    .field private class System.Object m_num
    .method family hidebysig instance class System.Object 
            Fibonacci(class System.Object num,
                      class System.Object flag) il managed
    {
      .maxstack  8
      IL_0000:  ldarg.1
      IL_0001:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0006:  ldtoken    [mscorlib]System.Single
      IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0010:  bne.un.s   IL_0024

      IL_0012:  ldarg.2
      IL_0013:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0018:  ldtoken    [mscorlib]System.Boolean
      IL_001d:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0022:  beq.s      IL_002a

      IL_0024:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0029:  throw

      IL_002a:  ldarg.0
      IL_002b:  ldarg.1
      IL_002c:  ldarg.2
      IL_002d:  callvirt   instance class System.Object BoxTest_instance_il.BaseTest::Fibonacci2(class System.Object,
                                                                                     class System.Object)
      IL_0032:  ret
    } // end of method 'Test::Fibonacci'

    .method family hidebysig virtual instance class System.Object 
            Fibonacci2(class System.Object num,
                       class System.Object flag) il managed
    {
      .maxstack  4
      .locals ([0] class System.Object N,
               [1] float32 V_1,
               [2] bool V_2)
      IL_0000:  ldarg.1
      IL_0001:  unbox      [mscorlib]System.Single
      IL_0006:  ldind.r4
      IL_0007:  conv.r8
      IL_0008:  ldc.r8     1.1000000000000001
      IL_0011:  bge.un.s   IL_0017

      IL_0013:  ldarg.1
      IL_0014:  stloc.0
      IL_0015:  br.s       IL_006d

      IL_0017:  ldarg.0
      IL_0018:  ldarg.1
      IL_0019:  unbox      [mscorlib]System.Single
      IL_001e:  ldind.r4
      IL_001f:  ldc.r4     2.
      IL_0024:  sub
      IL_0025:  stloc.1
      IL_0026:  ldloc.s   V_1
      IL_0028:  box        [mscorlib]System.Single
      IL_002d:  ldc.i4.0
      IL_002e:  stloc.2
      IL_002f:  ldloc.s   V_2
      IL_0031:  box        [mscorlib]System.Boolean
      IL_0036:  call       instance class System.Object BoxTest_instance_il.Test::Fibonacci(class System.Object,
                                                                                class System.Object)
      IL_003b:  unbox      [mscorlib]System.Single
      IL_0040:  ldind.r4
      IL_0041:  ldarg.0
      IL_0042:  ldarg.1
      IL_0043:  unbox      [mscorlib]System.Single
      IL_0048:  ldind.r4
      IL_0049:  ldc.r4     1.
      IL_004e:  sub
      IL_004f:  stloc.1
      IL_0050:  ldloc.s   V_1
      IL_0052:  box        [mscorlib]System.Single
      IL_0057:  ldarg.2
      IL_0058:  call       instance class System.Object BoxTest_instance_il.Test::Fibonacci(class System.Object,
                                                                                class System.Object)
      IL_005d:  unbox      [mscorlib]System.Single
      IL_0062:  ldind.r4
      IL_0063:  add
      IL_0064:  stloc.1
      IL_0065:  ldloc.s   V_1
      IL_0067:  box        [mscorlib]System.Single
      IL_006c:  stloc.0
      IL_006d:  ldarg.2
      IL_006e:  unbox      [mscorlib]System.Boolean
      IL_0073:  ldind.i1
      IL_0074:  brfalse.s  IL_008b

      IL_0076:  ldloc.0
      IL_0077:  callvirt   instance class System.String [mscorlib]System.Object::ToString()
      IL_007c:  ldstr      " "
      IL_0081:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0086:  call       void [System.Console]System.Console::Write(class System.String)
      IL_008b:  ldloc.0
      IL_008c:  ret
    } // end of method 'Test::Fibonacci2'

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(class System.Object num) il managed
    {
      .maxstack  2
      .locals ([0] float32 V_0)
      IL_0000:  ldarg.0
      IL_0001:  call       instance void BoxTest_instance_il.BaseTest::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  unbox      [mscorlib]System.Double
      IL_000d:  ldind.r8
      IL_000e:  conv.r4
      IL_000f:  stloc.0
      IL_0010:  ldloc.s   V_0
      IL_0012:  box        [mscorlib]System.Single
      IL_0017:  stfld      class System.Object BoxTest_instance_il.Test::m_num
      IL_001c:  ret
    } // end of method 'Test::.ctor'

    .method public hidebysig instance class System.Object
            Print() il managed
    {
      .maxstack  3
      .locals ([0] bool V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldarg.0
      IL_0002:  ldfld      class System.Object BoxTest_instance_il.Test::m_num
      IL_0007:  ldc.i4.1
      IL_0008:  stloc.0
      IL_0009:  ldloc.s   V_0
      IL_000b:  box        [mscorlib]System.Boolean
      IL_0010:  call       instance class System.Object BoxTest_instance_il.Test::Fibonacci(class System.Object,
                                                                                class System.Object)
      IL_0015:  pop
      IL_0016:  call       void [System.Console]System.Console::WriteLine()
      IL_001b:  ldarg.0
      IL_001c:  ldfld      class System.Object BoxTest_instance_il.Test::m_num
      IL_0021:  ret
    } // end of method 'Test::Print'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  2
      .locals ([0] class BoxTest_instance_il.Test test,
               [1] float64 V_1)
      IL_0000:  ldc.r8     20.
      IL_0009:  stloc.1
      IL_000a:  ldloc.s   V_1
      IL_000c:  box        [mscorlib]System.Double
      IL_0011:  newobj     instance void BoxTest_instance_il.Test::.ctor(class System.Object)
      IL_0016:  stloc.0
      IL_0017:  ldloc.0
      IL_0018:  call       instance class System.Object BoxTest_instance_il.Test::Print()
      IL_001d:  pop
      IL_001e:  ldstr      "*** PASSED ***"
      IL_0023:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0028:  ldc.i4    0x64
      IL_0029:  ret
    } // end of method 'Test::Main'

  } // end of class 'Test'

} // end of namespace 'BoxTest_instance_il'

