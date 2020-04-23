// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
.assembly extern System.Private.CoreLib { }
.assembly extern System.Runtime { }
.assembly extern mscorlib { }
.assembly ImplicitOverrideSameSigAsDecl { }

.class public auto ansi beforefieldinit A {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit B extends A {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit C extends B {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit GenRetType<X,Y> {
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


.class public auto ansi beforefieldinit NonGenThroughGen1<V1,V2> extends C {
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
    ldarg.1
    ldstr "object GenBaseType.MyFunc()"
    stind.ref
    newobj instance void A::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B MyFunc(string& res) 
  {
    ldarg.1
    ldstr "B GenBaseType.MyFunc()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B GenToNonGen(string& res) 
  {
    ldarg.1
    ldstr "B GenBaseType.GenToNonGen()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B NonGenThroughGenFunc(string& res) 
  {
    ldarg.1
    ldstr "B GenBaseType.NonGenThroughGenFunc()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class GenRetType<int32,object> MyGenFunc(string& res) 
  {
    ldarg.1
    ldstr "GenRetType<int32,object> GenBaseType.MyGenFunc()"
    stind.ref
    newobj instance void class GenRetType<int32,object>::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class GenRetType<!A, class Dictionary<string,!B>> MyGenFunc(string& res) 
  {
    ldarg.1
    ldstr "GenRetType<!A, class Dictionary<string,!B>> GenBaseType.MyGenFunc()"
    stind.ref
    newobj instance void class GenRetType<!A, class Dictionary<string,!B>>::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class GenRetType<!A,!B> TestNonGenericDerived(string& res) 
  {
    ldarg.1
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

.class public auto ansi beforefieldinit DerivedTestType<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig virtual instance object MyFunc(string& res) 
  {
    // Attribute ignored since this is not a MethodImpl
    .custom instance void [System.Private.CoreLib]System.Runtime.CompilerServices.ValidateMethodImplRemainsInEffectAttribute::.ctor() = (01 00 00 00)
    ldarg.1
    ldstr "object DerivedTestType.MyFunc()"
    stind.ref
    newobj instance void A::.ctor()
    ret
  }

  .method public hidebysig virtual instance class B MyFunc(string& res) 
  {
    // Attribute ignored since this is not a MethodImpl
    .custom instance void [System.Private.CoreLib]System.Runtime.CompilerServices.ValidateMethodImplRemainsInEffectAttribute::.ctor() = (01 00 00 00)
    ldarg.1
    ldstr "B DerivedTestType.MyFunc()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  
  .method public hidebysig virtual instance class B GenToNonGen(string& res) 
  {
    // Attribute ignored since this is not a MethodImpl
    .custom instance void [System.Private.CoreLib]System.Runtime.CompilerServices.ValidateMethodImplRemainsInEffectAttribute::.ctor() = (01 00 00 00)
    ldarg.1
    ldstr "B DerivedTestType.GenToNonGen()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }

  .method public hidebysig virtual instance class B NonGenThroughGenFunc(string& res) 
  {
    // Attribute ignored since this is not a MethodImpl
    .custom instance void [System.Private.CoreLib]System.Runtime.CompilerServices.ValidateMethodImplRemainsInEffectAttribute::.ctor() = (01 00 00 00)
    ldarg.1
    ldstr "B DerivedTestType.NonGenThroughGenFunc()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }

  .method public hidebysig virtual instance class GenRetType<int32,object> MyGenFunc(string& res) 
  {
    // Attribute ignored since this is not a MethodImpl
    .custom instance void [System.Private.CoreLib]System.Runtime.CompilerServices.ValidateMethodImplRemainsInEffectAttribute::.ctor() = (01 00 00 00)
    ldarg.1
    ldstr "GenRetType<int32,object> DerivedTestType.MyGenFunc()"
    stind.ref
    newobj instance void class GenRetType<int32,object>::.ctor()
    ret
  }
  
  .method public hidebysig virtual instance class GenRetType<!U, class Dictionary<string,!V>> MyGenFunc(string& res) 
  {
    // Attribute ignored since this is not a MethodImpl
    .custom instance void [System.Private.CoreLib]System.Runtime.CompilerServices.ValidateMethodImplRemainsInEffectAttribute::.ctor() = (01 00 00 00)
    ldarg.1
    ldstr "GenRetType<!U, class Dictionary<string,!V>> DerivedTestType.MyGenFunc()"
    stind.ref
    newobj instance void class GenRetType<!U, class Dictionary<string,!V>>::.ctor()
    ret
  }

  .method public hidebysig virtual instance class GenRetType<!U,!V> TestNonGenericDerived(string& res) 
  {
    // Attribute ignored since this is not a MethodImpl
    .custom instance void [System.Private.CoreLib]System.Runtime.CompilerServices.ValidateMethodImplRemainsInEffectAttribute::.ctor() = (01 00 00 00)
    ldarg.1
    ldstr "GenRetType<!U,!V> DerivedTestType.TestNonGenericDerived()"
    stind.ref
    newobj instance void class GenRetType<!U,!V>::.ctor()
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

// ========================================================================================

.class public auto ansi beforefieldinit CMain extends [mscorlib]System.Object
{
  .method public static bool RunTest1() noinlining 
  {
    .locals init (string res, bool flag, string expected)
        
    ldstr "object DerivedTestType.MyFunc()"
    stloc.2

    ldstr "EXPECTED: "
    call       void [System.Console]System.Console::Write(object)    
    ldloc.2
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class DerivedTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance object class GenBaseType<int32,object>::MyFunc(string&)
    pop
    
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(object)
    
	ldloc.0
    ldloc.2
	call bool [System.Runtime]System.String::op_Equality(string, string)
    ret
  }

  .method public static bool RunTest2() noinlining 
  {
    .locals init (string res, bool flag, string expected)
        
    ldstr "B DerivedTestType.MyFunc()"
    stloc.2

    ldstr "EXPECTED: "
    call       void [System.Console]System.Console::Write(object)    
    ldloc.2
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class DerivedTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class B class GenBaseType<int32,object>::MyFunc(string&)
    pop
    
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(object)
    
	ldloc.0
    ldloc.2
	call bool [System.Runtime]System.String::op_Equality(string, string)
    ret
  }

  .method public static bool RunTest3() noinlining 
  {
    .locals init (string res, bool flag, string expected)
        
    ldstr "B DerivedTestType.GenToNonGen()"
    stloc.2

    ldstr "EXPECTED: "
    call       void [System.Console]System.Console::Write(object)    
    ldloc.2
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class DerivedTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class B class GenBaseType<int32,object>::GenToNonGen(string&)
    pop
    
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(object)
    
	ldloc.0
    ldloc.2
	call bool [System.Runtime]System.String::op_Equality(string, string)
    ret
  }

  .method public static bool RunTest4() noinlining 
  {
    .locals init (string res, bool flag, string expected)
        
    ldstr "B DerivedTestType.NonGenThroughGenFunc()"
    stloc.2

    ldstr "EXPECTED: "
    call       void [System.Console]System.Console::Write(object)    
    ldloc.2
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class DerivedTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class B class GenBaseType<int32,object>::NonGenThroughGenFunc(string&)
    pop
    
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(object)
    
	ldloc.0
    ldloc.2
	call bool [System.Runtime]System.String::op_Equality(string, string)
    ret
  }

  .method public static bool RunTest5() noinlining 
  {
    .locals init (string res, bool flag, string expected)
        
    ldstr "GenRetType<int32,object> DerivedTestType.MyGenFunc()"
    stloc.2

    ldstr "EXPECTED: "
    call       void [System.Console]System.Console::Write(object)    
    ldloc.2
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class DerivedTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class GenRetType<int32,object> class GenBaseType<int32,object>::MyGenFunc(string&)
    pop
    
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(object)
    
	ldloc.0
    ldloc.2
	call bool [System.Runtime]System.String::op_Equality(string, string)
    ret
  }

  .method public static bool RunTest6() noinlining 
  {
    .locals init (string res, bool flag, string expected)
        
    ldstr "GenRetType<!U, class Dictionary<string,!V>> DerivedTestType.MyGenFunc()"
    stloc.2

    ldstr "EXPECTED: "
    call       void [System.Console]System.Console::Write(object)    
    ldloc.2
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class DerivedTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class GenRetType<!0, class Dictionary<string,!1>> class GenBaseType<int32,object>::MyGenFunc(string&)
    pop
    
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(object)
    
	ldloc.0
    ldloc.2
	call bool [System.Runtime]System.String::op_Equality(string, string)
    ret
  }

  .method public static bool RunTest7() noinlining 
  {
    .locals init (string res, bool flag, string expected)
        
    ldstr "GenRetType<!U,!V> DerivedTestType.TestNonGenericDerived()"
    stloc.2

    ldstr "EXPECTED: "
    call       void [System.Console]System.Console::Write(object)    
    ldloc.2
    call       void [System.Console]System.Console::WriteLine(object)
    ldstr "ACTUAL  : "
    call       void [System.Console]System.Console::Write(object)
    
    newobj     instance void class DerivedTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class GenRetType<!0,!1> class GenBaseType<int32,object>::TestNonGenericDerived(string&)
    pop
    
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(object)
    
	ldloc.0
    ldloc.2
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
    brtrue.s T7
    ldc.i4.0
    stloc.0

    T7:
    call bool CMain::RunTest7()
    brtrue.s DONE
    ldc.i4.0
    stloc.0

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
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
