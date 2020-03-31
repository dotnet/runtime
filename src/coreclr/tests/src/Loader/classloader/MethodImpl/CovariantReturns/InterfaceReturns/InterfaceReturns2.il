// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
.assembly extern mscorlib { }
.assembly InterfaceReturns { }

.class interface public auto ansi abstract IA { }
.class interface public auto ansi abstract IB implements IA { }
.class interface public auto ansi abstract IC implements IB { }
.class interface public auto ansi abstract IGenRetType<A,B> { }

.class public auto ansi beforefieldinit Dictionary<KEY,VAL> { }

.class public auto ansi beforefieldinit GenDerive1<V1,V2> implements class IGenRetType<!V1,!V2> { }
.class public auto ansi beforefieldinit GenDerive2<U1,U2,U3> extends class GenDerive1<!U1, class Dictionary<!U3,!U2>> { }
.class public auto ansi beforefieldinit GenDerive3<T1,T2> extends class GenDerive2<!T1,!T2,string> { }

.class public auto ansi beforefieldinit NonGenericDerived1<ARG1,ARG2> implements class IGenRetType<!ARG1,!ARG2> { }
.class public auto ansi beforefieldinit NonGenericDerived2<T> extends class NonGenericDerived1<!T,object> { }
.class public auto ansi beforefieldinit NonGenericDerived3 extends class NonGenericDerived2<int32> { }
.class public auto ansi beforefieldinit NonGenericDerived4 extends NonGenericDerived3 { }

.class public auto ansi beforefieldinit GenToNonGen1<V1,V2> implements IC { }
.class public auto ansi beforefieldinit GenToNonGen2<U1,U2,U3> extends class GenToNonGen1<!U1, class Dictionary<!U3,!U2>> { }
.class public auto ansi beforefieldinit GenToNonGen3<T1,T2> extends class GenToNonGen2<!T1,!T2,string> { }

.class public auto ansi beforefieldinit NonGenThroughGen1<V1,V2> implements class IC { }
.class public auto ansi beforefieldinit NonGenThroughGen2<U1,U2,U3> extends class NonGenThroughGen1<!U1, class Dictionary<!U3,!U2>> { }
.class public auto ansi beforefieldinit NonGenThroughGen3 extends class NonGenThroughGen2<object,int32,string> { }
.class public auto ansi beforefieldinit NonGenThroughGen4 extends NonGenThroughGen3 { }

.class public auto ansi beforefieldinit BaseTest<U,V>
{
  .method public hidebysig newslot virtual instance class IGenRetType<!U,!V> MyFunc() 
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

.class public auto ansi beforefieldinit Test1<U,V> extends class BaseTest<!U,!V>
{
  .method public hidebysig newslot virtual instance class IB NonGenThroughGenOverride()
  {
    .override method instance class NonGenThroughGen4 class BaseTest<!U,!V>::NonGenThroughGenFunc()
    ldnull
    ret
  }
}

.class public auto ansi beforefieldinit Test2<U,V> extends class BaseTest<!U,!V>
{
  .method public hidebysig newslot virtual instance class IB GenToNonGenOverride()
  {
    .override method instance class GenToNonGen3<!V,object> class BaseTest<!U,!V>::GenToNonGenFunc()
    ldnull
    ret
  }
}

.class public auto ansi beforefieldinit Test3<U,V> extends class BaseTest<!U,!V>
{
  .method public hidebysig newslot virtual instance class IGenRetType<int32,object> NewGenFunc1()
  {
    .override method instance class NonGenericDerived4 class BaseTest<!U,!V>::GenFunc()
    ldnull
    ret
  }
}

.class public auto ansi beforefieldinit Test4<U,V> extends class BaseTest<!U,!V>
{
  .method public hidebysig newslot virtual instance class IGenRetType<!U, class Dictionary<string,!V>> NewGenFunc2()
  {
    .override method instance class GenDerive3<!U,!V> class BaseTest<!U,!V>::GenFunc()
    ldnull
    ret
  }
}

.class public auto ansi beforefieldinit Test5<U,V> extends class BaseTest<!U,!V>
{
  .method public hidebysig newslot virtual instance object NewFunc1()
  {
    .override method instance class IGenRetType<!U,!V> class BaseTest<!U,!V>::MyFunc()
    ldnull
    ret
  }
}

.class public auto ansi beforefieldinit CMain extends [mscorlib]System.Object
{
  .method public static void RunTest1() noinlining 
  {
    newobj     instance void class Test1<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest2() noinlining 
  {
    newobj     instance void class Test2<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest3() noinlining 
  {
    newobj     instance void class Test3<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest4() noinlining 
  {
    newobj     instance void class Test4<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest5() noinlining 
  {
    newobj     instance void class Test5<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
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
