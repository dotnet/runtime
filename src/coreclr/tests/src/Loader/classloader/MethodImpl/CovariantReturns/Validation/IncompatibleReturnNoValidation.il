// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
.assembly extern System.Private.CoreLib { }
.assembly extern System.Runtime { }
.assembly extern mscorlib { }
.assembly IncompatibleReturnNoValidation { }

.class public auto ansi beforefieldinit A { }
.class public auto ansi beforefieldinit B extends A { }
.class public auto ansi beforefieldinit C extends B { }

.class public auto ansi beforefieldinit GenRetType<X,Y> { }
.class public auto ansi beforefieldinit Dictionary<KEY,VAL> { }

.class public auto ansi beforefieldinit GenDerive1<V1,V2> extends class GenRetType<!V1,!V2> { }
.class public auto ansi beforefieldinit GenDerive2<U1,U2,U3> extends class GenDerive1<!U1, class Dictionary<!U3,!U2>> { }
.class public auto ansi beforefieldinit GenDerive3<T1,T2> extends class GenDerive2<!T1,!T2,string> { }

.class public auto ansi beforefieldinit NonGenericDerived1<ARG1,ARG2> extends class GenRetType<!ARG1,!ARG2> { }
.class public auto ansi beforefieldinit NonGenericDerived2<T> extends class NonGenericDerived1<!T,object> { }
.class public auto ansi beforefieldinit NonGenericDerived3 extends class NonGenericDerived2<int32> { }
.class public auto ansi beforefieldinit NonGenericDerived4 extends NonGenericDerived3 { }

.class public auto ansi beforefieldinit GenToNonGen1<V1,V2> extends C { }
.class public auto ansi beforefieldinit GenToNonGen2<U1,U2,U3> extends class GenToNonGen1<!U1, class Dictionary<!U3,!U2>> { }
.class public auto ansi beforefieldinit GenToNonGen3<T1,T2> extends class GenToNonGen2<!T1,!T2,string> { }


.class public auto ansi beforefieldinit NonGenThroughGen1<V1,V2> extends C { }
.class public auto ansi beforefieldinit NonGenThroughGen2<U1,U2,U3> extends class NonGenThroughGen1<!U1, class Dictionary<!U3,!U2>> { }
.class public auto ansi beforefieldinit NonGenThroughGen3 extends class NonGenThroughGen2<object,int32,string> { }
.class public auto ansi beforefieldinit NonGenThroughGen4 extends NonGenThroughGen3 { }

.class public auto ansi beforefieldinit GenBaseType<A,B>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed 
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } 

  .method public hidebysig newslot virtual instance object MyFunc(string& res) 
  {
    ldarg.0
    ldstr "object GenBaseType.MyFunc()"
    stind.ref
    newobj instance void A::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B MyFunc(string& res) 
  {
    ldarg.0
    ldstr "B GenBaseType.MyFunc()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B GenToNonGen(string& res) 
  {
    ldarg.0
    ldstr "B GenBaseType.GenToNonGen()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B NonGenThroughGenFunc(string& res) 
  {
    ldarg.0
    ldstr "B GenBaseType.NonGenThroughGenFunc()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class GenRetType<int32,object> MyGenFunc(string& res) 
  {
    ldarg.0
    ldstr "GenRetType<int32,object> GenBaseType.MyGenFunc()"
    stind.ref
    newobj instance void class GenRetType<int32,object>::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class GenRetType<!A, class Dictionary<string,!B>> MyGenFunc(string& res) 
  {
    ldarg.0
    ldstr "GenRetType<!A, class Dictionary<string,!B>> GenBaseType.MyGenFunc()"
    stind.ref
    newobj instance void class GenRetType<!A, class Dictionary<string,!B>>::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class GenRetType<!A,!B> TestNonGenericDerived(string& res) 
  {
    ldarg.0
    ldstr "GenRetType<!A,!B> GenBaseType.TestNonGenericDerived()"
    stind.ref
    newobj instance void class GenRetType<!A,!B>::.ctor()
    ret
  }
}


// ========================================================================================

.class public auto ansi beforefieldinit GenTestType<U,V> extends class GenBaseType<!U,!V>
{
  .method public hidebysig newslot virtual instance class NonGenThroughGen4 NonGenThroughGenOverride(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::NonGenThroughGenFunc(string& res)
    ldarg.1
    ldstr "NonGenThroughGen4 TestType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenToNonGen3<!V,object> GenToNonGenOverride(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::GenToNonGen(string& res)
    ldarg.1
    ldstr "GenToNonGen3<!V,object> TestType.GenToNonGenOverride()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class NonGenericDerived4 NewGenFunc1(string& res)
  {
    .override method instance class GenRetType<int32,object> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "NonGenericDerived4 TestType.NewGenFunc1()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenDerive3<!U,!V> NewGenFunc2(string& res)
  {
    .override method instance class GenRetType<!0, class Dictionary<string,!1>> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "GenDerive3<!U,!V> TestType.NewGenFunc2()"
    stind.ref
    ldnull
    ret
  }
  
  .method public hidebysig newslot virtual instance class GenRetType<!U,!V> NewFunc1(string& res)
  {
    .override method instance object class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "GenRetType<!U,!V> TestType.NewFunc1()"
    stind.ref
    ldnull
    ret
  }
  
  .method public hidebysig newslot virtual instance class C NewFunc2(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "C TestType.NewFunc2()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void class GenBaseType<!U,!V>::.ctor()
    ret
  }
}

// ========================================================================================

// ========================================================================================
// The following types are overriding base type methods that have already been overridden
// with covariant return types, but the return type here is not compatible (return type is 
// a base type of the covariant return type of existing overrides). Validation is only
// triggered by the presense of the ValidateMethodImplRemainsInEffect attribute, so the 
// following types should successfully load since they do not have the attribute.
// ========================================================================================
.class public auto ansi beforefieldinit IncompatibleButValid1<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig newslot virtual instance class B NonGenThroughGenOverride_IncompatibleButValid(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::NonGenThroughGenFunc(string& res)
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void class GenTestType<!U,!V>::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit IncompatibleButValid2<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig newslot virtual instance class B GenToNonGenOverride_IncompatibleButValid(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::GenToNonGen(string& res)
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void class GenTestType<!U,!V>::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit IncompatibleButValid3<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig newslot virtual instance class GenRetType<int32,object> NewGenFunc1_IncompatibleButValid(string& res)
  {
    .override method instance class GenRetType<int32,object> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void class GenTestType<!U,!V>::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit IncompatibleButValid4<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig newslot virtual instance class GenRetType<!U, class Dictionary<string,!V>> NewGenFunc2_IncompatibleButValid(string& res)
  {
    .override method instance class GenRetType<!0, class Dictionary<string,!1>> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void class GenTestType<!U,!V>::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit IncompatibleButValid5<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig newslot virtual instance object NewFunc1_IncompatibleButValid(string& res)
  {
    .override method instance object class GenBaseType<!U,!V>::MyFunc(string& res)
    ldnull
    ret
  }
  
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void class GenTestType<!U,!V>::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit IncompatibleButValid6<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig newslot virtual instance class B NewFunc2_IncompatibleButValid(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::MyFunc(string& res)
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void class GenTestType<!U,!V>::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit CMain extends [mscorlib]System.Object
{
  .method public static void RunTest1() noinlining 
  {
    newobj     instance void class IncompatibleButValid1<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest2() noinlining 
  {
    newobj     instance void class IncompatibleButValid2<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest3() noinlining 
  {
    newobj     instance void class IncompatibleButValid3<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest4() noinlining 
  {
    newobj     instance void class IncompatibleButValid4<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest5() noinlining 
  {
    newobj     instance void class IncompatibleButValid5<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest6() noinlining 
  {
    newobj     instance void class IncompatibleButValid6<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public hidebysig static int32  Main( string[] args) cil managed
  {
    .entrypoint
    .maxstack  2

    // No exception should be thrown here.
    call void CMain::RunTest1()
    call void CMain::RunTest2()
    call void CMain::RunTest3()
    call void CMain::RunTest4()
    call void CMain::RunTest5()
    call void CMain::RunTest6()

    ldstr      "Test PASSED"
    call       void [System.Console]System.Console::WriteLine(string)
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
