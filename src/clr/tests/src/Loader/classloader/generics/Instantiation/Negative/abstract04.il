// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
.assembly extern mscorlib{}
.assembly abstract04{}

// Gen<T> extends non-generic abstract class "Base".
// Base declares abstract virtual method Dummy()
// But Gen<T> does not override/implement Dummy()
// Thus, we should throw a TypeLoadException.

.class public abstract beforefieldinit Base extends [mscorlib]System.Object
{
  .method public newslot abstract virtual instance void  Dummy(int32 t){}

  .method family hidebysig specialname rtspecialname instance void .ctor()
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}

.class public beforefieldinit Gen<([mscorlib]System.Object) T> extends Base
{
  .field private !0 t
  .field private int32 _int
 
  .method public specialname rtspecialname instance void .ctor()
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Base::.ctor()
    IL_0006:  ret
  }
}

.class public beforefieldinit GenTest<([mscorlib]System.Object) T> extends [mscorlib]System.Object
{
  .method private instance class Gen<!0> InternalTest()
  {
    .maxstack  1
    .locals init (class Gen<!0> V_0)
    IL_0000:  newobj     instance void class Gen<!0>::.ctor()
    IL_0005:  stloc.0
    IL_0006:  br.s       IL_0008

    IL_0008:  ldloc.0
    IL_0009:  ret
  }

  .method private hidebysig instance void IndirectTest()
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance class Gen<!0> class GenTest<!0>::InternalTest()
    IL_0006:  pop
    IL_0007:  ret
  } // end of method GenTest::IndirectTest

  .method public instance bool Test()
  {
    .maxstack  2
    .locals init (class [mscorlib]System.Exception V_0, bool V_1)
    .try
    {
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class GenTest<!0>::IndirectTest()
      IL_0006:  ldstr      "Test did not throw expected TypeLoadException"
      IL_000b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0010:  ldc.i4.0
      IL_0011:  stloc.1
      IL_0012:  leave.s    IL_002e

    }  // end .try
    catch [mscorlib]System.TypeLoadException 
    {
      IL_0014:  pop
      IL_0015:  ldc.i4.1
      IL_0016:  stloc.1
      IL_0017:  leave.s    IL_002e

    }  // end handler
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

    }  // end handler
    IL_002e:  ldloc.1
    IL_002f:  ret
  } // end of method GenTest::Test

  .method public specialname rtspecialname instance void  .ctor()
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method GenTest::.ctor

} // end of class GenTest

.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .field public static int32 counter
  .field public static bool result
  .method public hidebysig static void  Eval(bool exp) cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  2
    IL_0000:  ldsfld     int32 Test::counter
    IL_0005:  ldc.i4.1
    IL_0006:  add
    IL_0007:  stsfld     int32 Test::counter
    IL_000c:  ldarg.0
    IL_000d:  brtrue.s   IL_002e

    IL_000f:  ldarg.0
    IL_0010:  stsfld     bool Test::result
    IL_0015:  ldstr      "Test Failed at location: "
    IL_001a:  ldsfld     int32 Test::counter
    IL_001f:  box        [mscorlib]System.Int32
    IL_0024:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0029:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002e:  ret
  } // end of method Test::Eval

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       113 (0x71)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  newobj     instance void class GenTest<int32>::.ctor()
    IL_0005:  call       instance bool class GenTest<int32>::Test()
    IL_000a:  call       void Test::Eval(bool)
    IL_000f:  newobj     instance void class GenTest<float64>::.ctor()
    IL_0014:  call       instance bool class GenTest<float64>::Test()
    IL_0019:  call       void Test::Eval(bool)
    IL_001e:  newobj     instance void class GenTest<valuetype [mscorlib]System.Guid>::.ctor()
    IL_0023:  call       instance bool class GenTest<valuetype [mscorlib]System.Guid>::Test()
    IL_0028:  call       void Test::Eval(bool)
    IL_002d:  newobj     instance void class GenTest<object>::.ctor()
    IL_0032:  call       instance bool class GenTest<object>::Test()
    IL_0037:  call       void Test::Eval(bool)
    IL_003c:  newobj     instance void class GenTest<string>::.ctor()
    IL_0041:  call       instance bool class GenTest<string>::Test()
    IL_0046:  call       void Test::Eval(bool)
    IL_004b:  ldsfld     bool Test::result
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
  } // end of method Test::Main

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  1
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     int32 Test::counter
    IL_0006:  ldc.i4.1
    IL_0007:  stsfld     bool Test::result
    IL_000c:  ret
  } // end of method Test::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test
