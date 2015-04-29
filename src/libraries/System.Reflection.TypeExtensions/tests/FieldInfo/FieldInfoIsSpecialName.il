.assembly extern mscorlib
{
}
.assembly extern TestLibrary
{
}
.assembly FieldInfoIsSpecialName
{
}

.module FieldInfoIsSpecialName.exe

.class private auto ansi beforefieldinit FieldInfoIsSpecialName
       extends [mscorlib]System.Object
{
  .field private valuetype [mscorlib]System.Reflection.BindingFlags AllFlags

  .field private static int32 Field1
  .field public specialname static string Field2
  .field static family int32 Field3
  .field static assembly string Field4
  .field static famorassem int32 Field5
  .field private int32 Field6
  .field public specialname string Field7
  .field family int32 Field8
  .field assembly string Field9
  .field famorassem int32 Field10
  .field public string Field11

  .method private hidebysig static int32 
          Main() cil managed
  {
    .entrypoint
    .maxstack  2
    .locals init (class FieldInfoIsSpecialName V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  newobj     instance void FieldInfoIsSpecialName::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldstr      "FieldInfo.IsSpecialName"
    IL_000c:  call       void [TestLibrary]TestLibrary.TestFramework::BeginTestCase(string)
    IL_0011:  nop
    IL_0012:  ldloc.0
    IL_0013:  callvirt   instance bool FieldInfoIsSpecialName::RunTests()
    IL_0018:  ldc.i4.0
    IL_0019:  ceq
    IL_001b:  stloc.2
    IL_001c:  ldloc.2
    IL_001d:  brtrue.s   IL_0036

    IL_001f:  nop
    IL_0020:  call       bool [TestLibrary]TestLibrary.TestFramework::EndTestCase()
    IL_0025:  pop
    IL_0026:  ldstr      "PASS"
    IL_002b:  call       void [TestLibrary]TestLibrary.TestFramework::LogInformation(string)
    IL_0030:  nop
    IL_0031:  ldc.i4.s   100
    IL_0033:  stloc.1
    IL_0034:  br.s       IL_004c

    IL_0036:  nop
    IL_0037:  call       bool [TestLibrary]TestLibrary.TestFramework::EndTestCase()
    IL_003c:  pop
    IL_003d:  ldstr      "FAIL"
    IL_0042:  call       void [TestLibrary]TestLibrary.TestFramework::LogInformation(string)
    IL_0047:  nop
    IL_0048:  ldc.i4.0
    IL_0049:  stloc.1
    IL_004a:  br.s       IL_004c

    IL_004c:  ldloc.1
    IL_004d:  ret
  } // end of method FieldInfoIsSpecialName::Main

  .method public hidebysig instance bool 
          RunTests() cil managed
  {
    .maxstack  1
    .locals init (bool V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  ldstr      "[Positive]"
    IL_0008:  call       void [TestLibrary]TestLibrary.TestFramework::LogInformation(string)
    IL_000d:  nop
    IL_000e:  ldarg.0
    IL_000f:  call       instance bool FieldInfoIsSpecialName::PosTest1()
    IL_0014:  brfalse.s  IL_0019

    IL_0016:  ldloc.0
    IL_0017:  br.s       IL_001a

    IL_0019:  ldc.i4.0
    IL_001a:  stloc.0
    IL_001b:  ldarg.0
    IL_001c:  call       instance bool FieldInfoIsSpecialName::PosTest2()
    IL_0021:  brfalse.s  IL_0026

    IL_0023:  ldloc.0
    IL_0024:  br.s       IL_0027

    IL_0026:  ldc.i4.0
    IL_0027:  stloc.0
    IL_0028:  ldarg.0
    IL_0029:  call       instance bool FieldInfoIsSpecialName::PosTest3()
    IL_002e:  brfalse.s  IL_0033

    IL_0030:  ldloc.0
    IL_0031:  br.s       IL_0034

    IL_0033:  ldc.i4.0
    IL_0034:  stloc.0
    IL_0035:  ldarg.0
    IL_0036:  call       instance bool FieldInfoIsSpecialName::PosTest4()
    IL_003b:  brfalse.s  IL_0040

    IL_003d:  ldloc.0
    IL_003e:  br.s       IL_0041

    IL_0040:  ldc.i4.0
    IL_0041:  stloc.0
    IL_0042:  ldarg.0
    IL_0043:  call       instance bool FieldInfoIsSpecialName::PosTest5()
    IL_0048:  brfalse.s  IL_004d

    IL_004a:  ldloc.0
    IL_004b:  br.s       IL_004e

    IL_004d:  ldc.i4.0
    IL_004e:  stloc.0
    IL_004f:  ldarg.0
    IL_0050:  call       instance bool FieldInfoIsSpecialName::PosTest6()
    IL_0055:  brfalse.s  IL_005a

    IL_0057:  ldloc.0
    IL_0058:  br.s       IL_005b

    IL_005a:  ldc.i4.0
    IL_005b:  stloc.0
    IL_005c:  ldarg.0
    IL_005d:  call       instance bool FieldInfoIsSpecialName::PosTest7()
    IL_0062:  brfalse.s  IL_0067

    IL_0064:  ldloc.0
    IL_0065:  br.s       IL_0068

    IL_0067:  ldc.i4.0
    IL_0068:  stloc.0
    IL_0069:  ldarg.0
    IL_006a:  call       instance bool FieldInfoIsSpecialName::PosTest8()
    IL_006f:  brfalse.s  IL_0074

    IL_0071:  ldloc.0
    IL_0072:  br.s       IL_0075

    IL_0074:  ldc.i4.0
    IL_0075:  stloc.0
    IL_0076:  ldarg.0
    IL_0077:  call       instance bool FieldInfoIsSpecialName::PosTest9()
    IL_007c:  brfalse.s  IL_0081

    IL_007e:  ldloc.0
    IL_007f:  br.s       IL_0082

    IL_0081:  ldc.i4.0
    IL_0082:  stloc.0
    IL_0083:  ldarg.0
    IL_0084:  call       instance bool FieldInfoIsSpecialName::PosTest10()
    IL_0089:  brfalse.s  IL_008e

    IL_008b:  ldloc.0
    IL_008c:  br.s       IL_008f

    IL_008e:  ldc.i4.0
    IL_008f:  stloc.0
    IL_0090:  ldarg.0
    IL_0091:  call       instance bool FieldInfoIsSpecialName::PosTest11()
    IL_0096:  brfalse.s  IL_009b

    IL_0098:  ldloc.0
    IL_0099:  br.s       IL_009c

    IL_009b:  ldc.i4.0
    IL_009c:  stloc.0
    IL_009d:  ldloc.0
    IL_009e:  stloc.1
    IL_009f:  br.s       IL_00a1

    IL_00a1:  ldloc.1
    IL_00a2:  ret
  } // end of method FieldInfoIsSpecialName::RunTests

  .method public hidebysig instance bool 
          PosTest1() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldtoken    FieldInfoIsSpecialName
    IL_0007:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000c:  ldstr      "Field1"
    IL_0011:  ldarg.0
    IL_0012:  ldfld      valuetype [mscorlib]System.Reflection.BindingFlags FieldInfoIsSpecialName::AllFlags
    IL_0017:  callvirt   instance class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Type::GetField(string,
                                                                                                              valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_001c:  ldc.i4.0
    IL_001d:  ldstr      "00A"
    IL_0022:  call       instance bool FieldInfoIsSpecialName::PosTest(class [mscorlib]System.Reflection.FieldInfo,
                                                                       bool,
                                                                       string)
    IL_0027:  stloc.0
    IL_0028:  br.s       IL_002a

    IL_002a:  ldloc.0
    IL_002b:  ret
  } // end of method FieldInfoIsSpecialName::PosTest1

  .method public hidebysig instance bool 
          PosTest2() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldtoken    FieldInfoIsSpecialName
    IL_0007:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000c:  ldstr      "Field2"
    IL_0011:  ldarg.0
    IL_0012:  ldfld      valuetype [mscorlib]System.Reflection.BindingFlags FieldInfoIsSpecialName::AllFlags
    IL_0017:  callvirt   instance class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Type::GetField(string,
                                                                                                              valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_001c:  ldc.i4.1
    IL_001d:  ldstr      "00B"
    IL_0022:  call       instance bool FieldInfoIsSpecialName::PosTest(class [mscorlib]System.Reflection.FieldInfo,
                                                                       bool,
                                                                       string)
    IL_0027:  stloc.0
    IL_0028:  br.s       IL_002a

    IL_002a:  ldloc.0
    IL_002b:  ret
  } // end of method FieldInfoIsSpecialName::PosTest2

  .method public hidebysig instance bool 
          PosTest3() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldtoken    FieldInfoIsSpecialName
    IL_0007:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000c:  ldstr      "Field3"
    IL_0011:  ldarg.0
    IL_0012:  ldfld      valuetype [mscorlib]System.Reflection.BindingFlags FieldInfoIsSpecialName::AllFlags
    IL_0017:  callvirt   instance class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Type::GetField(string,
                                                                                                              valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_001c:  ldc.i4.0
    IL_001d:  ldstr      "00C"
    IL_0022:  call       instance bool FieldInfoIsSpecialName::PosTest(class [mscorlib]System.Reflection.FieldInfo,
                                                                       bool,
                                                                       string)
    IL_0027:  stloc.0
    IL_0028:  br.s       IL_002a

    IL_002a:  ldloc.0
    IL_002b:  ret
  } // end of method FieldInfoIsSpecialName::PosTest3

  .method public hidebysig instance bool 
          PosTest4() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldtoken    FieldInfoIsSpecialName
    IL_0007:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000c:  ldstr      "Field4"
    IL_0011:  ldarg.0
    IL_0012:  ldfld      valuetype [mscorlib]System.Reflection.BindingFlags FieldInfoIsSpecialName::AllFlags
    IL_0017:  callvirt   instance class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Type::GetField(string,
                                                                                                              valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_001c:  ldc.i4.0
    IL_001d:  ldstr      "00D"
    IL_0022:  call       instance bool FieldInfoIsSpecialName::PosTest(class [mscorlib]System.Reflection.FieldInfo,
                                                                       bool,
                                                                       string)
    IL_0027:  stloc.0
    IL_0028:  br.s       IL_002a

    IL_002a:  ldloc.0
    IL_002b:  ret
  } // end of method FieldInfoIsSpecialName::PosTest4

  .method public hidebysig instance bool 
          PosTest5() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldtoken    FieldInfoIsSpecialName
    IL_0007:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000c:  ldstr      "Field5"
    IL_0011:  ldarg.0
    IL_0012:  ldfld      valuetype [mscorlib]System.Reflection.BindingFlags FieldInfoIsSpecialName::AllFlags
    IL_0017:  callvirt   instance class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Type::GetField(string,
                                                                                                              valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_001c:  ldc.i4.0
    IL_001d:  ldstr      "00E"
    IL_0022:  call       instance bool FieldInfoIsSpecialName::PosTest(class [mscorlib]System.Reflection.FieldInfo,
                                                                       bool,
                                                                       string)
    IL_0027:  stloc.0
    IL_0028:  br.s       IL_002a

    IL_002a:  ldloc.0
    IL_002b:  ret
  } // end of method FieldInfoIsSpecialName::PosTest5

  .method public hidebysig instance bool 
          PosTest6() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldtoken    FieldInfoIsSpecialName
    IL_0007:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000c:  ldstr      "Field6"
    IL_0011:  ldarg.0
    IL_0012:  ldfld      valuetype [mscorlib]System.Reflection.BindingFlags FieldInfoIsSpecialName::AllFlags
    IL_0017:  callvirt   instance class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Type::GetField(string,
                                                                                                              valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_001c:  ldc.i4.0
    IL_001d:  ldstr      "00F"
    IL_0022:  call       instance bool FieldInfoIsSpecialName::PosTest(class [mscorlib]System.Reflection.FieldInfo,
                                                                       bool,
                                                                       string)
    IL_0027:  stloc.0
    IL_0028:  br.s       IL_002a

    IL_002a:  ldloc.0
    IL_002b:  ret
  } // end of method FieldInfoIsSpecialName::PosTest6

  .method public hidebysig instance bool 
          PosTest7() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldtoken    FieldInfoIsSpecialName
    IL_0007:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000c:  ldstr      "Field7"
    IL_0011:  ldarg.0
    IL_0012:  ldfld      valuetype [mscorlib]System.Reflection.BindingFlags FieldInfoIsSpecialName::AllFlags
    IL_0017:  callvirt   instance class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Type::GetField(string,
                                                                                                              valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_001c:  ldc.i4.1
    IL_001d:  ldstr      "00G"
    IL_0022:  call       instance bool FieldInfoIsSpecialName::PosTest(class [mscorlib]System.Reflection.FieldInfo,
                                                                       bool,
                                                                       string)
    IL_0027:  stloc.0
    IL_0028:  br.s       IL_002a

    IL_002a:  ldloc.0
    IL_002b:  ret
  } // end of method FieldInfoIsSpecialName::PosTest7

  .method public hidebysig instance bool 
          PosTest8() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldtoken    FieldInfoIsSpecialName
    IL_0007:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000c:  ldstr      "Field8"
    IL_0011:  ldarg.0
    IL_0012:  ldfld      valuetype [mscorlib]System.Reflection.BindingFlags FieldInfoIsSpecialName::AllFlags
    IL_0017:  callvirt   instance class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Type::GetField(string,
                                                                                                              valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_001c:  ldc.i4.0
    IL_001d:  ldstr      "00H"
    IL_0022:  call       instance bool FieldInfoIsSpecialName::PosTest(class [mscorlib]System.Reflection.FieldInfo,
                                                                       bool,
                                                                       string)
    IL_0027:  stloc.0
    IL_0028:  br.s       IL_002a

    IL_002a:  ldloc.0
    IL_002b:  ret
  } // end of method FieldInfoIsSpecialName::PosTest8

  .method public hidebysig instance bool 
          PosTest9() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldtoken    FieldInfoIsSpecialName
    IL_0007:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000c:  ldstr      "Field9"
    IL_0011:  ldarg.0
    IL_0012:  ldfld      valuetype [mscorlib]System.Reflection.BindingFlags FieldInfoIsSpecialName::AllFlags
    IL_0017:  callvirt   instance class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Type::GetField(string,
                                                                                                              valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_001c:  ldc.i4.0
    IL_001d:  ldstr      "00I"
    IL_0022:  call       instance bool FieldInfoIsSpecialName::PosTest(class [mscorlib]System.Reflection.FieldInfo,
                                                                       bool,
                                                                       string)
    IL_0027:  stloc.0
    IL_0028:  br.s       IL_002a

    IL_002a:  ldloc.0
    IL_002b:  ret
  } // end of method FieldInfoIsSpecialName::PosTest9

  .method public hidebysig instance bool 
          PosTest10() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldtoken    FieldInfoIsSpecialName
    IL_0007:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000c:  ldstr      "Field10"
    IL_0011:  ldarg.0
    IL_0012:  ldfld      valuetype [mscorlib]System.Reflection.BindingFlags FieldInfoIsSpecialName::AllFlags
    IL_0017:  callvirt   instance class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Type::GetField(string,
                                                                                                              valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_001c:  ldc.i4.0
    IL_001d:  ldstr      "00J"
    IL_0022:  call       instance bool FieldInfoIsSpecialName::PosTest(class [mscorlib]System.Reflection.FieldInfo,
                                                                       bool,
                                                                       string)
    IL_0027:  stloc.0
    IL_0028:  br.s       IL_002a

    IL_002a:  ldloc.0
    IL_002b:  ret
  } // end of method FieldInfoIsSpecialName::PosTest10

  .method public hidebysig instance bool 
          PosTest11() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldtoken    FieldInfoIsSpecialName
    IL_0007:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000c:  ldstr      "Field11"
    IL_0011:  ldarg.0
    IL_0012:  ldfld      valuetype [mscorlib]System.Reflection.BindingFlags FieldInfoIsSpecialName::AllFlags
    IL_0017:  callvirt   instance class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Type::GetField(string,
                                                                                                              valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_001c:  ldc.i4.0
    IL_001d:  ldstr      "00K"
    IL_0022:  call       instance bool FieldInfoIsSpecialName::PosTest(class [mscorlib]System.Reflection.FieldInfo,
                                                                       bool,
                                                                       string)
    IL_0027:  stloc.0
    IL_0028:  br.s       IL_002a

    IL_002a:  ldloc.0
    IL_002b:  ret
  } // end of method FieldInfoIsSpecialName::PosTest11

  .method private hidebysig instance bool 
          PosTest(class [mscorlib]System.Reflection.FieldInfo fi,
                  bool expected,
                  string id) cil managed
  {
    // Code size       192 (0xc0)
    .maxstack  4
    .locals init (bool V_0,
             bool V_1,
             class [mscorlib]System.Exception V_2,
             bool V_3,
             bool V_4,
             object[] V_5)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  ldstr      "Test "
    IL_0008:  ldarg.3
    IL_0009:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000e:  call       void [TestLibrary]TestLibrary.TestFramework::BeginScenario(string)
    IL_0013:  nop
    .try
    {
      IL_0014:  nop
      IL_0015:  ldarg.1
      IL_0016:  callvirt   instance bool [mscorlib]System.Reflection.FieldInfo::get_IsSpecialName()
      IL_001b:  stloc.1
      IL_001c:  ldloc.1
      IL_001d:  ldarg.2
      IL_001e:  ceq
      IL_0020:  stloc.s    V_4
      IL_0022:  ldloc.s    V_4
      IL_0024:  brtrue.s   IL_0079

      IL_0026:  nop
      IL_0027:  ldstr      "001"
      IL_002c:  ldc.i4.6
      IL_002d:  newarr     [mscorlib]System.Object
      IL_0032:  stloc.s    V_5
      IL_0034:  ldloc.s    V_5
      IL_0036:  ldc.i4.0
      IL_0037:  ldstr      "Error in "
      IL_003c:  stelem.ref
      IL_003d:  ldloc.s    V_5
      IL_003f:  ldc.i4.1
      IL_0040:  ldarg.3
      IL_0041:  stelem.ref
      IL_0042:  ldloc.s    V_5
      IL_0044:  ldc.i4.2
      IL_0045:  ldstr      ": Value not as expected. Expected: "
      IL_004a:  stelem.ref
      IL_004b:  ldloc.s    V_5
      IL_004d:  ldc.i4.3
      IL_004e:  ldarga.s   expected
      IL_0050:  call       instance string [mscorlib]System.Boolean::ToString()
      IL_0055:  stelem.ref
      IL_0056:  ldloc.s    V_5
      IL_0058:  ldc.i4.4
      IL_0059:  ldstr      ", Actual: "
      IL_005e:  stelem.ref
      IL_005f:  ldloc.s    V_5
      IL_0061:  ldc.i4.5
      IL_0062:  ldloc.1
      IL_0063:  box        [mscorlib]System.Boolean
      IL_0068:  stelem.ref
      IL_0069:  ldloc.s    V_5
      IL_006b:  call       string [mscorlib]System.String::Concat(object[])
      IL_0070:  call       void [TestLibrary]TestLibrary.TestFramework::LogError(string,
                                                                                 string)
      IL_0075:  nop
      IL_0076:  ldc.i4.0
      IL_0077:  stloc.0
      IL_0078:  nop
      IL_0079:  nop
      IL_007a:  leave.s    IL_00b9

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_007c:  stloc.2
      IL_007d:  nop
      IL_007e:  ldstr      "002"
      IL_0083:  ldc.i4.4
      IL_0084:  newarr     [mscorlib]System.Object
      IL_0089:  stloc.s    V_5
      IL_008b:  ldloc.s    V_5
      IL_008d:  ldc.i4.0
      IL_008e:  ldstr      "Unexpected exception in "
      IL_0093:  stelem.ref
      IL_0094:  ldloc.s    V_5
      IL_0096:  ldc.i4.1
      IL_0097:  ldarg.3
      IL_0098:  stelem.ref
      IL_0099:  ldloc.s    V_5
      IL_009b:  ldc.i4.2
      IL_009c:  ldstr      ": "
      IL_00a1:  stelem.ref
      IL_00a2:  ldloc.s    V_5
      IL_00a4:  ldc.i4.3
      IL_00a5:  ldloc.2
      IL_00a6:  stelem.ref
      IL_00a7:  ldloc.s    V_5
      IL_00a9:  call       string [mscorlib]System.String::Concat(object[])
      IL_00ae:  call       void [TestLibrary]TestLibrary.TestFramework::LogError(string,
                                                                                 string)
      IL_00b3:  nop
      IL_00b4:  ldc.i4.0
      IL_00b5:  stloc.0
      IL_00b6:  nop
      IL_00b7:  leave.s    IL_00b9

    }  // end handler
    IL_00b9:  nop
    IL_00ba:  ldloc.0
    IL_00bb:  stloc.3
    IL_00bc:  br.s       IL_00be

    IL_00be:  ldloc.3
    IL_00bf:  ret
  } // end of method FieldInfoIsSpecialName::PosTest

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.s   60
    IL_0003:  stfld      valuetype [mscorlib]System.Reflection.BindingFlags FieldInfoIsSpecialName::AllFlags
    IL_0008:  ldarg.0
    IL_0009:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000e:  nop
    IL_000f:  ret
  } // end of method FieldInfoIsSpecialName::.ctor

} // end of class FieldInfoIsSpecialName

