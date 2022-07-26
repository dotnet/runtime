// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly extern System.Runtime { }
.assembly extern mscorlib { }
.assembly UnitTest_GVM { }

// ========================================================================================
// Types that will be used as return types on the various methods
// ========================================================================================

.class public auto ansi beforefieldinit A
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit B extends A
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit C extends B
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit GenRetType<X,Y>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit Dictionary<KEY,VAL>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit GenDerive1<V1,V2> extends class GenRetType<!V1,!V2>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenDerive2<U1,U2,U3> extends class GenDerive1<!U1, class Dictionary<!U3,!U2>>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenDerive3<T1,T2> extends class GenDerive2<!T1,!T2,string>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit NonGenericDerived1<ARG1,ARG2> extends class GenRetType<!ARG1,!ARG2>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenericDerived2<T> extends class NonGenericDerived1<!T,object>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenericDerived3 extends class NonGenericDerived2<int32>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenericDerived4 extends NonGenericDerived3
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenericDerived5<T> extends class NonGenericDerived1<int32,!T>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit GenToNonGen1<V1,V2> extends C
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenToNonGen2<U1,U2,U3> extends class GenToNonGen1<!U1, class Dictionary<!U3,!U2>>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenToNonGen3<T1,T2> extends class GenToNonGen2<!T1,!T2,string>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}


.class public auto ansi beforefieldinit NonGenThroughGen1<V1,V2> extends C
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenThroughGen2<U1,U2,U3> extends class NonGenThroughGen1<!U1, class Dictionary<!U3,!U2>>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenThroughGen3 extends class NonGenThroughGen2<object,int32,string>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenThroughGen4 extends NonGenThroughGen3
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenThroughGen5<T> extends class NonGenThroughGen2<object,int32,!T>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}


// ========================================================================================
// Main base type with various virtual methods that will be overridden later
// ========================================================================================

.class public auto ansi beforefieldinit GenBaseType<A,B>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance object MyFunc<Z>(string& res)
  {
    ldarg.1
    ldstr "object GenBaseType.MyFunc()"
    stind.ref
    newobj instance void A::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B MyFunc<Z>(string& res)
  {
    ldarg.1
    ldstr "B GenBaseType.MyFunc()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B GenToNonGen<Z>(string& res)
  {
    ldarg.1
    ldstr "B GenBaseType.GenToNonGen()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B NonGenThroughGenFunc<Z>(string& res)
  {
    ldarg.1
    ldstr "B GenBaseType.NonGenThroughGenFunc()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class GenRetType<int32,!!Z> MyGenFunc<Z>(string& res)
  {
    ldarg.1
    ldstr "GenRetType<int32,!!Y> GenBaseType.MyGenFunc()"
    stind.ref
    newobj instance void class GenRetType<int32,!!Z>::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class GenRetType<!A, class Dictionary<!!Z,!B>> MyGenFunc<Z>(string& res)
  {
    ldarg.1
    ldstr "GenRetType<!A, class Dictionary<!!Y,!B>> GenBaseType.MyGenFunc()"
    stind.ref
    newobj instance void class GenRetType<!A, class Dictionary<!!Z,!B>>::.ctor()
    ret
  }
}


// ========================================================================================
// SECOND LAYER type: overrides *SOME* virtuals on GenBaseType using MethodImpls with
// covariant return types (more derived return types)
// ========================================================================================

.class public auto ansi beforefieldinit GenMiddleType<U,V> extends class GenBaseType<!U,!V>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class NonGenThroughGen2<object,int32,!!Y> NonGenThroughGenFunc<Y>(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class B class GenBaseType<!U,!V>::NonGenThroughGenFunc<[1]>(string& res)
    ldarg.1
    ldstr "NonGenThroughGen2<object,int32,!!Y> GenMiddleType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenToNonGen1<!V, class Dictionary<string,!!Y>> GenToNonGen<Y>(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class B class GenBaseType<!U,!V>::GenToNonGen<[1]>(string& res)
    ldarg.1
    ldstr "GenToNonGen1<!V, class Dictionary<string,!!Y>> GenMiddleType.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class NonGenericDerived1<int32,!!Y> MyGenFunc<Y>(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class GenRetType<int32,!!0> class GenBaseType<!U,!V>::MyGenFunc<[1]>(string& res)
    ldarg.1
    ldstr "NonGenericDerived1<int32,!!Y> GenMiddleType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenDerive1<!U, class Dictionary<!!Y,!V>> MyGenFunc<Y>(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class GenRetType<!0, class Dictionary<!!0,!1>> class GenBaseType<!U,!V>::MyGenFunc<[1]>(string& res)
    ldarg.1
    ldstr "GenDerive1<!U, class Dictionary<!!Y,!V>> GenMiddleType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }
}

// ========================================================================================
// THIRD LAYER type: overrides all virtuals from GenBaseType using MethodImpls with
// covariant return types (more derived return types than the ones used in GenMiddleType)
// ========================================================================================

.class public auto ansi beforefieldinit GenTestType<U,V> extends class GenMiddleType<!U,!V>
{
  .method public hidebysig newslot virtual instance class NonGenThroughGen5<!!Y> NonGenThroughGenFunc<Y>(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::NonGenThroughGenFunc<[1]>(string& res)
    ldarg.1
    ldstr "NonGenThroughGen5<!!Y> TestType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenToNonGen3<!V,!!Y> GenToNonGen<Y>(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::GenToNonGen<[1]>(string& res)
    ldarg.1
    ldstr "GenToNonGen3<!V,!!Y> TestType.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class NonGenericDerived5<!!Y> MyGenFunc<Y>(string& res)
  {
    .override method instance class GenRetType<int32,!!0> class GenBaseType<!U,!V>::MyGenFunc<[1]>(string& res)
    ldarg.1
    ldstr "NonGenericDerived5<!!Y> TestType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenDerive2<!U,!V,!!Y> MyGenFunc<Y>(string& res)
  {
    .override method instance class GenRetType<!0, class Dictionary<!!0,!1>> class GenBaseType<!U,!V>::MyGenFunc<[1]>(string& res)
    ldarg.1
    ldstr "GenDerive2<!U,!V,!!Y> TestType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenRetType<!U,!V> MyFunc<Y>(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance object class GenBaseType<!U,!V>::MyFunc<[1]>(string& res)
    ldarg.1
    ldstr "GenRetType<!U,!V> TestType.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class C MyFunc<Y>(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class B class GenBaseType<!U,!V>::MyFunc<[1]>(string& res)
    ldarg.1
    ldstr "C TestType.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

// ========================================================================================
// THIRD LAYER INVALID types: Couple of types that override some method from GenBaseType
// using a return type signature that would be incompatible with the return type used in
// GenMiddle. Loading these types will throw a TypeLoadException.
// ========================================================================================

.class public auto ansi beforefieldinit Invalid1<U,V> extends class GenMiddleType<!U,!V>
{
  // Invalid: when comparing 'object' with !!Y in the instantiation of NonGenericDerived1 in the base type chain
  .method public hidebysig newslot virtual instance class NonGenThroughGen4 NonGenThroughGenOverride<Y>(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::NonGenThroughGenFunc<[1]>(string& res)
    ldnull
    ret
  }
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit Invalid2<U,V> extends class GenMiddleType<!U,!V>
{
   // Invalid: when comparing 'object' with !!Y in the instantiation
  .method public hidebysig newslot virtual instance class GenToNonGen3<!V,object> GenToNonGenOverride<Y>(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::GenToNonGen<[1]>(string& res)
    ldnull
    ret
  }
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit Invalid3<U,V> extends class GenMiddleType<!U,!V>
{
  // Invalid when comparing !!Y and 'object' in instantiation of GenRetType
  .method public hidebysig newslot virtual instance class NonGenericDerived4 NewGenFunc1<Y>(string& res)
  {
    .override method instance class GenRetType<int32,!!0> class GenBaseType<!U,!V>::MyGenFunc<[1]>(string& res)
    ldnull
    ret
  }
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit Invalid4<U,V> extends class GenMiddleType<!U,!V>
{
  // Invalid when comparing !!Y and 'string' in instantiation of Dictionary
  .method public hidebysig newslot virtual instance class GenDerive3<!U,!V> NewGenFunc2<Y>(string& res)
  {
    .override method instance class GenRetType<!0, class Dictionary<!!0,!1>> class GenBaseType<!U,!V>::MyGenFunc<[1]>(string& res)
    ldnull
    ret
  }
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

// ========================================================================================

.class public auto ansi beforefieldinit CMain extends [mscorlib]System.Object
{
  .method private hidebysig static bool CheckResults (
        string expected,
        string a,
        [opt] string b,
        [opt] string c,
        [opt] string d) cil managed
  {
    .param [3] = nullref
    .param [4] = nullref
    .param [5] = nullref
    // Method begins at RVA 0x20a0
    // Code size 164 (0xa4)
    .maxstack 2
    .locals /* 11000002 */ init (
        [0] bool
    )

    IL_0000: ldarg.1
    IL_0001: ldarg.0
    IL_0002: call bool [System.Runtime]System.String::op_Equality(string, string) /* 0A000012 */
    IL_0007: stloc.0
    IL_0008: ldstr "EXPECTED: " /* 70000001 */
    IL_000d: ldarg.0
    IL_000e: call string [System.Runtime]System.String::Concat(string, string) /* 0A000013 */
    IL_0013: call void [System.Console]System.Console::WriteLine(string) /* 0A000014 */
    IL_0018: ldstr "ACTUAL1 : " /* 70000017 */
    IL_001d: ldarg.1
    IL_001e: call string [System.Runtime]System.String::Concat(string, string) /* 0A000013 */
    IL_0023: call void [System.Console]System.Console::WriteLine(string) /* 0A000014 */
    IL_0028: ldarg.2
    IL_0029: call bool [System.Runtime]System.String::IsNullOrEmpty(string) /* 0A000015 */
    IL_002e: brtrue.s IL_004e

    IL_0030: ldstr "ACTUAL2 : " /* 7000002D */
    IL_0035: ldarg.2
    IL_0036: call string [System.Runtime]System.String::Concat(string, string) /* 0A000013 */
    IL_003b: call void [System.Console]System.Console::WriteLine(string) /* 0A000014 */
    IL_0040: ldloc.0
    IL_0041: brfalse.s IL_004c

    IL_0043: ldarg.2
    IL_0044: ldarg.0
    IL_0045: call bool [System.Runtime]System.String::op_Equality(string, string) /* 0A000012 */
    IL_004a: br.s IL_004d

    IL_004c: ldc.i4.0

    IL_004d: stloc.0

    IL_004e: ldarg.3
    IL_004f: call bool [System.Runtime]System.String::IsNullOrEmpty(string) /* 0A000015 */
    IL_0054: brtrue.s IL_0074

    IL_0056: ldstr "ACTUAL3 : " /* 70000043 */
    IL_005b: ldarg.3
    IL_005c: call string [System.Runtime]System.String::Concat(string, string) /* 0A000013 */
    IL_0061: call void [System.Console]System.Console::WriteLine(string) /* 0A000014 */
    IL_0066: ldloc.0
    IL_0067: brfalse.s IL_0072

    IL_0069: ldarg.3
    IL_006a: ldarg.0
    IL_006b: call bool [System.Runtime]System.String::op_Equality(string, string) /* 0A000012 */
    IL_0070: br.s IL_0073

    IL_0072: ldc.i4.0

    IL_0073: stloc.0

    IL_0074: ldarg.s d
    IL_0076: call bool [System.Runtime]System.String::IsNullOrEmpty(string) /* 0A000015 */
    IL_007b: brtrue.s IL_009d

    IL_007d: ldstr "ACTUAL4 : " /* 70000059 */
    IL_0082: ldarg.s d
    IL_0084: call string [System.Runtime]System.String::Concat(string, string) /* 0A000013 */
    IL_0089: call void [System.Console]System.Console::WriteLine(string) /* 0A000014 */
    IL_008e: ldloc.0
    IL_008f: brfalse.s IL_009b

    IL_0091: ldarg.s d
    IL_0093: ldarg.0
    IL_0094: call bool [System.Runtime]System.String::op_Equality(string, string) /* 0A000012 */
    IL_0099: br.s IL_009c

    IL_009b: ldc.i4.0

    IL_009c: stloc.0

    IL_009d: call void [System.Console]System.Console::WriteLine() /* 0A000016 */
    IL_00a2: ldloc.0
    IL_00a3: ret
  } // end of method Program::CheckResults

  // ============== Test using GenTestType ============== //
  // These test methods will callvirt each method using:
  //    1) The signature from GetBaseType
  //    2) The signature from GenMiddleType with covariant returns (when applicable)
  //    3) The signature from GenTestType with covariant returns
  // And verify that the override on GetTestType is the one that executes

  .method public static bool RunTest1() noinlining
  {
    .locals init (string res1, string res2, string res3)

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance object class GenBaseType<int32,object>::MyFunc<class CMain>(string&)
    pop

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class GenRetType<!0,!1> class GenTestType<int32,object>::MyFunc<class CMain>(string&)
    pop

    ldstr "GenRetType<!U,!V> TestType.MyFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest2() noinlining
  {
    .locals init (string res1, string res2, string res3)

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class B class GenBaseType<int32,object>::MyFunc<class CMain>(string&)
    pop

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class C class GenTestType<int32,object>::MyFunc<class CMain>(string&)
    pop

    ldstr "C TestType.MyFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest3() noinlining
  {
    .locals init (string res1, string res2, string res3)

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class GenRetType<!0, class Dictionary<!!0,!1>> class GenBaseType<int32,object>::MyGenFunc<class CMain>(string&)
    pop

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class GenDerive1<!0, class Dictionary<!!0,!1>> class GenMiddleType<int32,object>::MyGenFunc<class CMain>(string&)
    pop

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class GenDerive2<!0,!1,!!0> class GenTestType<int32,object>::MyGenFunc<class CMain>(string&)
    pop

    ldstr "GenDerive2<!U,!V,!!Y> TestType.MyGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest4() noinlining
  {
    .locals init (string res1, string res2, string res3)

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class GenRetType<int32,!!0> class GenBaseType<int32,object>::MyGenFunc<class CMain>(string&)
    pop

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class NonGenericDerived1<int32,!!0> class GenMiddleType<int32,object>::MyGenFunc<class CMain>(string&)
    pop

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class NonGenericDerived5<!!0> class GenTestType<int32,object>::MyGenFunc<class CMain>(string&)
    pop

    ldstr "NonGenericDerived5<!!Y> TestType.MyGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest5() noinlining
  {
    .locals init (string res1, string res2, string res3)

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class B class GenBaseType<int32,object>::GenToNonGen<class CMain>(string&)
    pop

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class GenToNonGen1<!1, class Dictionary<string,!!0>> class GenMiddleType<int32,object>::GenToNonGen<class CMain>(string&)
    pop

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class GenToNonGen3<!1,!!0> class GenTestType<int32,object>::GenToNonGen<class CMain>(string&)
    pop

    ldstr "GenToNonGen3<!V,!!Y> TestType.GenToNonGen()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest6() noinlining
  {
    .locals init (string res1, string res2, string res3)

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class B class GenBaseType<int32,object>::NonGenThroughGenFunc<class CMain>(string&)
    pop

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class NonGenThroughGen2<object,int32,!!0> class GenMiddleType<int32,object>::NonGenThroughGenFunc<class CMain>(string&)
    pop

    newobj     instance void class GenTestType<int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class NonGenThroughGen5<!!0> class GenTestType<int32,object>::NonGenThroughGenFunc<class CMain>(string&)
    pop

    ldstr "NonGenThroughGen5<!!Y> TestType.NonGenThroughGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  // ============== Test using GenMiddleType ============== //
  // These test methods will callvirt each method using:
  //    1) The signature from GetBaseType
  //    2) The signature from GenMiddleType with covariant returns
  // And verify that the override on GenMiddleType is the one that executes

  .method public static bool RunTest_Middle1() noinlining
  {
    .locals init (string res1, string res2, string res3)

    newobj     instance void class GenMiddleType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class GenRetType<!0, class Dictionary<!!0,!1>> class GenBaseType<int32,object>::MyGenFunc<class CMain>(string&)
    pop

    newobj     instance void class GenMiddleType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class GenDerive1<!0, class Dictionary<!!0,!1>> class GenMiddleType<int32,object>::MyGenFunc<class CMain>(string&)
    pop

    ldstr "GenDerive1<!U, class Dictionary<!!Y,!V>> GenMiddleType.MyGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest_Middle2() noinlining
  {
    .locals init (string res1, string res2, string res3)

    newobj     instance void class GenMiddleType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class GenRetType<int32,!!0> class GenBaseType<int32,object>::MyGenFunc<class CMain>(string&)
    pop

    newobj     instance void class GenMiddleType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class NonGenericDerived1<int32,!!0> class GenMiddleType<int32,object>::MyGenFunc<class CMain>(string&)
    pop

    ldstr "NonGenericDerived1<int32,!!Y> GenMiddleType.MyGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest_Middle3() noinlining
  {
    .locals init (string res1, string res2, string res3)

    newobj     instance void class GenMiddleType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class B class GenBaseType<int32,object>::GenToNonGen<class CMain>(string&)
    pop

    newobj     instance void class GenMiddleType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class GenToNonGen1<!1, class Dictionary<string,!!0>> class GenMiddleType<int32,object>::GenToNonGen<class CMain>(string&)
    pop

    ldstr "GenToNonGen1<!V, class Dictionary<string,!!Y>> GenMiddleType.GenToNonGen()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest_Middle4() noinlining
  {
    .locals init (string res1, string res2, string res3)

    newobj     instance void class GenMiddleType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class B class GenBaseType<int32,object>::NonGenThroughGenFunc<class CMain>(string&)
    pop

    newobj     instance void class GenMiddleType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class NonGenThroughGen2<object,int32,!!0> class GenMiddleType<int32,object>::NonGenThroughGenFunc<class CMain>(string&)
    pop

    ldstr "NonGenThroughGen2<object,int32,!!Y> GenMiddleType.NonGenThroughGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  // ============== Test using GenMiddleType ============== //
  // These test methods attempt to load the invalid types, and are
  // expected to throw a TypeLoadException (caught by Main())

  .method public static void RunInvalidTest1() noinlining
  {
    newobj     instance void class Invalid1<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunInvalidTest2() noinlining
  {
    newobj     instance void class Invalid2<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunInvalidTest3() noinlining
  {
    newobj     instance void class Invalid3<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunInvalidTest4() noinlining
  {
    newobj     instance void class Invalid4<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  // ===================================================================================== //
  .method public hidebysig static int32  Main( string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
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
    brtrue.s M1
    ldc.i4.0
    stloc.0

    M1:
    call bool CMain::RunTest_Middle1()
    brtrue.s M2
    ldc.i4.0
    stloc.0

    M2:
    call bool CMain::RunTest_Middle2()
    brtrue.s M3
    ldc.i4.0
    stloc.0

    M3:
    call bool CMain::RunTest_Middle3()
    brtrue.s M4
    ldc.i4.0
    stloc.0

    M4:
    call bool CMain::RunTest_Middle4()
    brtrue.s INVALID1
    ldc.i4.0
    stloc.0

    INVALID1:
    .try
    {
        call void CMain::RunInvalidTest1()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Invalid1."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s INVALID2
    }
    catch [mscorlib]System.TypeLoadException
    {
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)
        leave.s INVALID2
    }

    INVALID2:
    .try
    {
        call void CMain::RunInvalidTest2()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Invalid2."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s INVALID3
    }
    catch [mscorlib]System.TypeLoadException
    {
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)
        leave.s INVALID3
    }

    INVALID3:
    .try
    {
        call void CMain::RunInvalidTest3()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Invalid3."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s INVALID4
    }
    catch [mscorlib]System.TypeLoadException
    {
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)
        leave.s INVALID4
    }

    INVALID4:
    .try
    {
        call void CMain::RunInvalidTest4()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Invalid4."
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
