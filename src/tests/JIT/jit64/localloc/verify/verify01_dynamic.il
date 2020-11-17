// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Test Invalid program (evaluation stack is not empty before localloc call

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib{}
.assembly eh01
{
}

.class private auto ansi beforefieldinit LocallocTest
       extends [mscorlib]System.Object
{
  .field public static int32 stackAllocSize
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    .maxstack  1
    .locals init(int32 V_0)
    ldc.i4	0x1000
    stsfld	int32 LocallocTest::stackAllocSize
    .try
    {
      IL_0000:  call       void LocallocTest::func()
      IL_0005:  leave.s    IL_0027

    }  
    catch [mscorlib]System.Security.VerificationException 
    {
      IL_0007:  pop
      IL_0008:  ldstr      "Passed"
      IL_000d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0012:  ldc.i4.s   100
      IL_0014:  stloc.0
      IL_0015:  leave.s    IL_0035

    }  
    catch [mscorlib]System.InvalidProgramException 
    {
      IL_0017:  pop
      IL_0018:  ldstr      "Passed"
      IL_009d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0022:  ldc.i4.s   100
      IL_0024:  stloc.0
      IL_0025:  leave.s    IL_0035

    }  
    IL_0027:  ldstr      "Failed"
    IL_002c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0031:  ldc.i4.1
    IL_0032:  stloc.0
    IL_0033:  br.s       IL_0035

    IL_0035:  ldloc.0
    IL_0036:  ret
  } 

  .method private hidebysig static void  func() cil managed
  {
    .maxstack  2
    .locals init(int32* V_0,
             int32* V_1)
    IL_0000:  ldc.i4.4
    IL_0001:  ldsfld     int32 LocallocTest::stackAllocSize
    IL_0006:  mul
    IL_0007:  localloc
    IL_0009:  stloc.1
    IL_000a:  ldc.i4.4
    IL_000b:  ldsfld     int32 LocallocTest::stackAllocSize
    IL_0010:  mul
    IL_0011:  localloc
    IL_0013:  stloc.0
    IL_0014:  ret
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
