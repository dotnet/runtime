// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern System.Runtime { }
.assembly extern mscorlib { }

.assembly extern A { }
.assembly extern B { }
.assembly extern C { }
.assembly extern Dictionary { }
.assembly extern GenRetType { }
.assembly extern GenDerive1 { }
.assembly extern GenDerive2 { }
.assembly extern GenDerive3 { }
.assembly extern NonGenericDerived1 { }
.assembly extern NonGenericDerived2 { }
.assembly extern NonGenericDerived3 { }
.assembly extern NonGenericDerived4 { }
.assembly extern GenToNonGen1 { }
.assembly extern GenToNonGen2 { }
.assembly extern GenToNonGen3 { }
.assembly extern NonGenThroughGen1 { }
.assembly extern NonGenThroughGen2 { }
.assembly extern NonGenThroughGen3 { }
.assembly extern NonGenThroughGen4 { }

.assembly extern GenBaseType { }
.assembly extern GenTestType { }

.assembly UnitTestMultiModule { }

.class public auto ansi beforefieldinit CMain extends [mscorlib]System.Object
{
  .method public static bool RunTest1() noinlining 
  {
    .locals init (string res, bool flag)
    
    ldstr "EXPECTED: GenRetType<!U,!V> TestType.NewFunc1()"
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class [GenTestType]GenTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance object class [GenBaseType]GenBaseType<int32,object>::MyFunc(string&)
    pop
    
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(object)
    
	ldloc.0
	ldstr "GenRetType<!U,!V> TestType.NewFunc1()"
	call bool [System.Runtime]System.String::op_Equality(string, string)
    ret
  }

  .method public static bool RunTest2() noinlining 
  {
    .locals init (string res, bool flag)
    
    ldstr "EXPECTED: C TestType.NewFunc2()"
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class [GenTestType]GenTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class [B]B class [GenBaseType]GenBaseType<int32,object>::MyFunc(string&)
    pop
    
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(object)
    
	ldloc.0
	ldstr "C TestType.NewFunc2()"
	call bool [System.Runtime]System.String::op_Equality(string, string)
    ret
  }
  
  .method public static bool RunTest3() noinlining 
  {
    .locals init (string res, bool flag)
    
    ldstr "EXPECTED: GenDerive3<!U,!V> TestType.NewGenFunc2()"
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class [GenTestType]GenTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class [GenRetType]GenRetType<!0, class [Dictionary]Dictionary<string,!1>> class [GenBaseType]GenBaseType<int32,object>::MyGenFunc(string&)
    pop
    
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(object)
    
	ldloc.0
	ldstr "GenDerive3<!U,!V> TestType.NewGenFunc2()"
	call bool [System.Runtime]System.String::op_Equality(string, string)
    ret
  }

  .method public static bool RunTest4() noinlining 
  {
    .locals init (string res, bool flag)
    
    ldstr "EXPECTED: NonGenericDerived4 TestType.NewGenFunc1()"
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class [GenTestType]GenTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class [GenRetType]GenRetType<int32,object> class [GenBaseType]GenBaseType<int32,object>::MyGenFunc(string&)
    pop
    
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(object)
    
	ldloc.0
	ldstr "NonGenericDerived4 TestType.NewGenFunc1()"
	call bool [System.Runtime]System.String::op_Equality(string, string)
    ret
  }

  .method public static bool RunTest5() noinlining 
  {
    .locals init (string res, bool flag)
    
    ldstr "EXPECTED: GenToNonGen3<!V,object> TestType.GenToNonGenOverride()"
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class [GenTestType]GenTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class [B]B class [GenBaseType]GenBaseType<int32,object>::GenToNonGen(string&)
    pop
    
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(object)
    
	ldloc.0
	ldstr "GenToNonGen3<!V,object> TestType.GenToNonGenOverride()"
	call bool [System.Runtime]System.String::op_Equality(string, string)
    ret
  }

  .method public static bool RunTest6() noinlining 
  {
    .locals init (string res, bool flag)
    
    ldstr "EXPECTED: NonGenThroughGen4 TestType.NonGenThroughGenFunc()"
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class [GenTestType]GenTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class [B]B class [GenBaseType]GenBaseType<int32,object>::NonGenThroughGenFunc(string& res)
    pop
    
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(object)
    
	ldloc.0
	ldstr "NonGenThroughGen4 TestType.NonGenThroughGenFunc()"
	call bool [System.Runtime]System.String::op_Equality(string, string)
    ret
  }

  .method public hidebysig static int32  Main( string[] args) cil managed
  {
    .entrypoint
    .maxstack  2
    .locals init ( bool result )
    
    ldc.i4.1
    stloc.0
    
    T1:
    call bool CMain::RunTest1()
    brtrue.s T2
    ldc.i4.0
    stloc.0
    
    T2:
    call bool CMain::RunTest2()
    brtrue.s T3
    ldc.i4.0
    stloc.0
    
    T3:
    call bool CMain::RunTest3()
    brtrue.s T4
    ldc.i4.0
    stloc.0
    
    T4:
    call bool CMain::RunTest4()
    brtrue.s T5
    ldc.i4.0
    stloc.0
    
    T5:
    call bool CMain::RunTest5()
    brtrue.s T6
    ldc.i4.0
    stloc.0
    
    T6:
    call bool CMain::RunTest6()
    brtrue.s DONE
    ldc.i4.0
    stloc.0
    
    DONE:
    ldloc.0
    brtrue.s   PASS

    ldstr      "Test FAILED"
    call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4.s   101
    ret
    
    PASS:
    ldstr      "Test PASSED"
    call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4.s   100
    ret

    ldc.i4.s   100
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
