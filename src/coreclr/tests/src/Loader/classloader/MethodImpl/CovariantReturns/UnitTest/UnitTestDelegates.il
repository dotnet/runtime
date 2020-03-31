// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
.assembly extern System.Runtime { }
.assembly extern mscorlib { }
.assembly UnitTestDelegates { }

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
    .override method instance class GenRetType<!U, class Dictionary<string,!V>> class GenBaseType<!U,!V>::MyGenFunc(string& res)
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




.class public auto ansi sealed ReturnObjDel extends [System.Runtime]System.MulticastDelegate
{
    // We can use a delegate with signature 'object Invoke(string&)' for all methods here since they all return a reference type
    
	.method public hidebysig specialname rtspecialname instance void .ctor (object 'object', native int 'method') runtime managed { }
	.method public hidebysig newslot virtual instance class [System.Runtime]System.IAsyncResult BeginInvoke (string& res, class [System.Runtime]System.AsyncCallback callback, object 'object') runtime managed { }
	.method public hidebysig newslot virtual instance object Invoke (string& res) runtime managed { }
	.method public hidebysig newslot virtual instance object EndInvoke (string& res, class [System.Runtime]System.IAsyncResult result) runtime managed { }
}

.class public auto ansi beforefieldinit CMain extends [mscorlib]System.Object
{
  .method public static bool RunTest1() noinlining 
  {
    .locals init (string res, bool flag)
    
    ldstr "EXPECTED: GenRetType<!U,!V> TestType.NewFunc1()"
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class GenTestType<int32,object>::.ctor()
    dup
	ldvirtftn  instance object class GenBaseType<int32,object>::MyFunc(string&)
	newobj     instance void class ReturnObjDel::.ctor(object, native int)
    
    ldloca.s   0
    callvirt   instance object class ReturnObjDel::Invoke(string&)
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
    
    newobj     instance void class GenTestType<int32,object>::.ctor()
    dup
	ldvirtftn  instance class B class GenBaseType<int32,object>::MyFunc(string&)
	newobj     instance void class ReturnObjDel::.ctor(object, native int)
    
    ldloca.s   0
    callvirt   instance object class ReturnObjDel::Invoke(string&)
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
    
    newobj     instance void class GenTestType<int32,object>::.ctor()
    dup
	ldvirtftn  instance class GenRetType<!0, class Dictionary<string,!1>> class GenBaseType<int32,object>::MyGenFunc(string&)
	newobj     instance void class ReturnObjDel::.ctor(object, native int)
    
    ldloca.s   0
    callvirt   instance object class ReturnObjDel::Invoke(string&)
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
    
    newobj     instance void class GenTestType<int32,object>::.ctor()
    dup
	ldvirtftn  instance class GenRetType<int32,object> class GenBaseType<int32,object>::MyGenFunc(string&)
	newobj     instance void class ReturnObjDel::.ctor(object, native int)
    
    ldloca.s   0
    callvirt   instance object class ReturnObjDel::Invoke(string&)
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
    
    newobj     instance void class GenTestType<int32,object>::.ctor()
    dup
	ldvirtftn  instance class B class GenBaseType<int32,object>::GenToNonGen(string&)
	newobj     instance void class ReturnObjDel::.ctor(object, native int)
    
    ldloca.s   0
    callvirt   instance object class ReturnObjDel::Invoke(string&)
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
    
    newobj     instance void class GenTestType<int32,object>::.ctor()
    dup
	ldvirtftn  instance class B class GenBaseType<int32,object>::NonGenThroughGenFunc(string& res)
	newobj     instance void class ReturnObjDel::.ctor(object, native int)
    
    ldloca.s   0
    callvirt   instance object class ReturnObjDel::Invoke(string&)
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
