// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly Negative001 {}
.assembly extern xunit.core {}
.class public auto ansi beforefieldinit GenBase<([mscorlib]System.Object) T>
       extends [mscorlib]System.Object
{
  .field private !0 t
  .method private hidebysig instance !0  Dummy(!0 t) cil managed
  {
    .maxstack  2
    .locals init (!0 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      !0 class GenBase<!0>::t
    IL_0007:  ldarg.1
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 

.class public explicit ansi beforefieldinit Gen<([mscorlib]System.Object) T>
       extends class GenBase<!0>
{
  .method private hidebysig instance !0  Dummy(!0 t) cil managed
  {
    .maxstack  1
    .locals init (!0 V_0)
    IL_0000:  ldarg.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class GenBase<!0>::.ctor()
    IL_0006:  ret
  } 

} 

.class public auto ansi beforefieldinit GenTest<([mscorlib]System.Object) T>
       extends [mscorlib]System.Object
{
  .method private hidebysig instance class Gen<!0> 
          InternalTest() cil managed
  {
    .maxstack  1
    .locals init (class Gen<!0> V_0)
    IL_0000:  newobj     instance void class Gen<!0>::.ctor()
    IL_0005:  stloc.0
    IL_0006:  br.s       IL_0008

    IL_0008:  ldloc.0
    IL_0009:  ret
  } 

  .method private hidebysig instance void 
          IndirectTest() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance class Gen<!0> class GenTest<!0>::InternalTest()
    IL_0006:  pop
    IL_0007:  ret
  } 

  .method public hidebysig instance bool 
          Test_Negative001() cil managed
  {
    .maxstack  2
    .locals init (class [mscorlib]System.Exception V_0,
             bool V_1)
    .try
    {
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class GenTest<!0>::IndirectTest()
      IL_0006:  ldstr      "Test did not throw expected TypeLoadException"
      IL_000b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0010:  ldc.i4.0
      IL_0011:  stloc.1
      IL_0012:  leave.s    IL_002e

    }  
    catch [mscorlib]System.TypeLoadException 
    {
      IL_0014:  pop
      IL_0015:  ldc.i4.1
      IL_0016:  stloc.1
      IL_0017:  leave.s    IL_002e

    }  
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldstr      "Test caught unexpected Exception "
      IL_001f:  ldloc.0
      IL_0020:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0025:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002a:  ldc.i4.0
      IL_002b:  stloc.1
      IL_002c:  leave.s    IL_002e

    }  
    IL_002e:  ldloc.1
    IL_002f:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 

.class public auto ansi beforefieldinit Test_Negative001
       extends [mscorlib]System.Object
{
  .field public static int32 counter
  .field public static bool result
  .method public hidebysig static void  Eval(bool exp) cil managed
  {
    .maxstack  8
    IL_0000:  ldsfld     int32 Test_Negative001::counter
    IL_0005:  ldc.i4.1
    IL_0006:  add
    IL_0007:  stsfld     int32 Test_Negative001::counter
    IL_000c:  ldarg.0
    IL_000d:  brtrue.s   IL_002e

    IL_000f:  ldarg.0
    IL_0010:  stsfld     bool Test_Negative001::result
    IL_0015:  ldstr      "Test Failed at location: "
    IL_001a:  ldsfld     int32 Test_Negative001::counter
    IL_001f:  box        [mscorlib]System.Int32
    IL_0024:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0029:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002e:  ret
  } 

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  newobj     instance void class GenTest<int32>::.ctor()
    IL_0005:  call       instance bool class GenTest<int32>::Test_Negative001()
    IL_000a:  call       void Test_Negative001::Eval(bool)
    IL_000f:  newobj     instance void class GenTest<float64>::.ctor()
    IL_0014:  call       instance bool class GenTest<float64>::Test_Negative001()
    IL_0019:  call       void Test_Negative001::Eval(bool)
    IL_001e:  newobj     instance void class GenTest<valuetype [mscorlib]System.Guid>::.ctor()
    IL_0023:  call       instance bool class GenTest<valuetype [mscorlib]System.Guid>::Test_Negative001()
    IL_0028:  call       void Test_Negative001::Eval(bool)
    IL_002d:  newobj     instance void class GenTest<string>::.ctor()
    IL_0032:  call       instance bool class GenTest<string>::Test_Negative001()
    IL_0037:  call       void Test_Negative001::Eval(bool)
    IL_003c:  newobj     instance void class GenTest<object>::.ctor()
    IL_0041:  call       instance bool class GenTest<object>::Test_Negative001()
    IL_0046:  call       void Test_Negative001::Eval(bool)
    IL_004b:  ldsfld     bool Test_Negative001::result
    IL_0050:  brfalse.s  IL_0061

    IL_0052:  ldstr      "Test Passed"
    IL_0057:  call       void [System.Console]System.Console::WriteLine(string)
    IL_005c:  ldc.i4.s   100
    IL_005e:  stloc.0
    IL_005f:  br.s       IL_006f

    IL_0061:  ldstr      "Test Failed"
    IL_0066:  call       void [System.Console]System.Console::WriteLine(string)
    IL_006b:  ldc.i4.1
    IL_006c:  stloc.0
    IL_006d:  br.s       IL_006f

    IL_006f:  ldloc.0
    IL_0070:  ret
  } 

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     int32 Test_Negative001::counter
    IL_0006:  ldc.i4.1
    IL_0007:  stsfld     bool Test_Negative001::result
    IL_000c:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 
}
