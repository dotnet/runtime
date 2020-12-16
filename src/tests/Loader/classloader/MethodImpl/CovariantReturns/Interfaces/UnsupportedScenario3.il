// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test to validate the current unsupported scenarios will throw a TypeLoadException.
// Scenario being tested here: covariant returns on MethodImpls to interface methods 
// implemented by structs

.assembly extern System.Console { }
.assembly extern mscorlib { }
.assembly UnsupportedScenario3 { }

.class public auto ansi beforefieldinit A { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
.class public auto ansi beforefieldinit B extends A { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
.class public auto ansi beforefieldinit C extends B { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}

.class public auto ansi beforefieldinit GenRetType<A,B> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}

.class public auto ansi beforefieldinit Dictionary<KEY,VAL> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}

.class public auto ansi beforefieldinit GenDerive1<V1,V2> extends class GenRetType<!V1,!V2> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
.class public auto ansi beforefieldinit GenDerive2<U1,U2,U3> extends class GenDerive1<!U1, class Dictionary<!U3,!U2>> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
.class public auto ansi beforefieldinit GenDerive3<T1,T2> extends class GenDerive2<!T1,!T2,string> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}

.class public auto ansi beforefieldinit NonGenericDerived1<ARG1,ARG2> extends class GenRetType<!ARG1,!ARG2> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
.class public auto ansi beforefieldinit NonGenericDerived2<T> extends class NonGenericDerived1<!T,object> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
.class public auto ansi beforefieldinit NonGenericDerived3 extends class NonGenericDerived2<int32> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
.class public auto ansi beforefieldinit NonGenericDerived4 extends NonGenericDerived3 { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}

.class public auto ansi beforefieldinit GenToNonGen1<V1,V2> extends C { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
.class public auto ansi beforefieldinit GenToNonGen2<U1,U2,U3> extends class GenToNonGen1<!U1, class Dictionary<!U3,!U2>> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
.class public auto ansi beforefieldinit GenToNonGen3<T1,T2> extends class GenToNonGen2<!T1,!T2,string> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}

.class public auto ansi beforefieldinit NonGenThroughGen1<V1,V2> extends class C { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
.class public auto ansi beforefieldinit NonGenThroughGen2<U1,U2,U3> extends class NonGenThroughGen1<!U1, class Dictionary<!U3,!U2>> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
.class public auto ansi beforefieldinit NonGenThroughGen3 extends class NonGenThroughGen2<object,int32,string> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
.class public auto ansi beforefieldinit NonGenThroughGen4 extends NonGenThroughGen3 { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}

.class interface public auto ansi abstract IBaseTest<U,V>
{
  .method public hidebysig newslot virtual instance class GenRetType<!U,!V> MyFunc() 
  {
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class GenToNonGen3<!V,object> GenToNonGenFunc() 
  {
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class NonGenThroughGen4 NonGenThroughGenFunc() 
  {
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class NonGenericDerived4 GenFunc() 
  {
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class GenDerive3<!U,!V> GenFunc() 
  {
    ldnull
    ret
  }
}

.class public sealed auto ansi Test1<U,V> 
        extends [mscorlib]System.ValueType 
        implements class IBaseTest<!U,!V>
{
  .method public hidebysig newslot virtual instance class B NonGenThroughGenOverride()
  {
    .override method instance class NonGenThroughGen4 class IBaseTest<!U,!V>::NonGenThroughGenFunc()
    ldnull
    ret
  }
}

.class public sealed auto ansi Test2<U,V> 
        extends [mscorlib]System.ValueType 
        implements class IBaseTest<!U,!V>
{
  .method public hidebysig newslot virtual instance class B GenToNonGenOverride()
  {
    .override method instance class GenToNonGen3<!1,object> class IBaseTest<!U,!V>::GenToNonGenFunc()
    ldnull
    ret
  }
}

.class public sealed auto ansi Test3<U,V> 
        extends [mscorlib]System.ValueType 
        implements class IBaseTest<!U,!V>
{
  .method public hidebysig newslot virtual instance class GenRetType<int32,object> NewGenFunc1()
  {
    .override method instance class NonGenericDerived4 class IBaseTest<!U,!V>::GenFunc()
    ldnull
    ret
  }
}

.class public sealed auto ansi Test4<U,V> 
        extends [mscorlib]System.ValueType 
        implements class IBaseTest<!U,!V>
{
  .method public hidebysig newslot virtual instance class GenRetType<!U, class Dictionary<string,!V>> NewGenFunc2()
  {
    .override method instance class GenDerive3<!0,!1> class IBaseTest<!U,!V>::GenFunc()
    ldnull
    ret
  }
}

.class public sealed auto ansi Test5<U,V> 
        extends [mscorlib]System.ValueType 
        implements class IBaseTest<!U,!V>
{
  .method public hidebysig newslot virtual instance object NewFunc1()
  {
    .override method instance class GenRetType<!0,!1> class IBaseTest<!U,!V>::MyFunc()
    ldnull
    ret
  }
}

// ========================================================================================

.class public auto ansi beforefieldinit CMain extends [mscorlib]System.Object
{
  .method public static void RunTest1() noinlining 
  {
    .locals init (valuetype Test1<int32,object>)
    ldloca.s 0
    initobj valuetype Test1<int32,object>
    ldloc.0
    box valuetype Test1<int32,object>
    call void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest2() noinlining 
  {
    .locals init (valuetype Test2<int32,object>)
    ldloca.s 0
    initobj valuetype Test2<int32,object>
    ldloc.0
    box valuetype Test2<int32,object>
    call void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest3() noinlining 
  {
    .locals init (valuetype Test3<int32,object>)
    ldloca.s 0
    initobj valuetype Test3<int32,object>
    ldloc.0
    box valuetype Test3<int32,object>
    call void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest4() noinlining 
  {
    .locals init (valuetype Test4<int32,object>)
    ldloca.s 0
    initobj valuetype Test4<int32,object>
    ldloc.0
    box valuetype Test4<int32,object>
    call void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest5() noinlining 
  {
    .locals init (valuetype Test5<int32,object>)
    ldloca.s 0
    initobj valuetype Test5<int32,object>
    ldloc.0
    box valuetype Test5<int32,object>
    call void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public hidebysig static int32  Main( string[] args) cil managed
  {
    .entrypoint
    .maxstack  2
    .locals init (bool V_0)    
    ldc.i4.1
    stloc.0
    
    T1:
    .try
    {
        call void CMain::RunTest1()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Test1."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s T2
    }
    catch [mscorlib]System.TypeLoadException
    {   
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)        
        leave.s T2
    }
    
    T2:
    .try
    {
        call void CMain::RunTest2()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Test2."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s T3
    }
    catch [mscorlib]System.TypeLoadException
    {   
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)        
        leave.s T3
    }

    T3:
    .try
    {
        call void CMain::RunTest3()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Test3."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s T4
    }
    catch [mscorlib]System.TypeLoadException
    {   
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)        
        leave.s T4
    }

    T4:
    .try
    {
        call void CMain::RunTest4()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Test4."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s T5
    }
    catch [mscorlib]System.TypeLoadException
    {   
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)        
        leave.s T5
    }

    T5:
    .try
    {
        call void CMain::RunTest5()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Test5."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s DONE
    }
    catch [mscorlib]System.TypeLoadException
    {   
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)        
        leave.s DONE
    }

    DONE:
    
    ldloc.0
    brtrue.s   PASS

    ldc.i4.s   101
    ret
    
    PASS:
    ldstr      "Test PASSED"
    call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4.s   100
    ret
  }
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CMain::.ctor

} // end of class CMain
