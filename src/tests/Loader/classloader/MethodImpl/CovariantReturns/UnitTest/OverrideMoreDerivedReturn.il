// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly extern System.Runtime { }
.assembly extern mscorlib { }
.assembly 'OverrideMoreDerivedReturn' { }

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
.class public auto ansi beforefieldinit D extends C
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit GenRetType<X,Y>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenDerivedRetType<X,Y> extends class GenRetType<!X,!Y>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenDerivedRetType2<X,Y> extends class GenDerivedRetType<!X,!Y>
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
.class public auto ansi beforefieldinit GenDerive4<T1,T2> extends class GenDerive3<!T1,!T2>
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
.class public auto ansi beforefieldinit NonGenericDerived5 extends NonGenericDerived4
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
.class public auto ansi beforefieldinit GenToNonGen4<T1,T2> extends class GenToNonGen3<!T1,!T2>
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
.class public auto ansi beforefieldinit NonGenThroughGen5 extends NonGenThroughGen4
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

// ========================================================================================
// Main base type with various virtual methods that will be overridden later
// ========================================================================================

.class public auto ansi beforefieldinit GenBaseType<A,B>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

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
// SECOND LAYER type: overrides all virtuals on GenBaseType using MethodImpls with
// covariant return types (more derived return types)
// ========================================================================================

.class public auto ansi beforefieldinit GenTestType<U,V> extends class GenBaseType<!U,!V>
{
  .method public hidebysig newslot virtual instance class NonGenThroughGen4 NonGenThroughGenFunc(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::NonGenThroughGenFunc(string& res)
    ldarg.1
    ldstr "NonGenThroughGen4 TestType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenToNonGen3<!V,object> GenToNonGen(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::GenToNonGen(string& res)
    ldarg.1
    ldstr "GenToNonGen3<!V,object> TestType.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class NonGenericDerived4 MyGenFunc(string& res)
  {
    .override method instance class GenRetType<int32,object> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "NonGenericDerived4 TestType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenDerive3<!U,!V> MyGenFunc(string& res)
  {
    .override method instance class GenRetType<!0, class Dictionary<string,!1>> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "GenDerive3<!U,!V> TestType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenDerivedRetType<!U,!V> MyFunc(string& res)
  {
    .override method instance object class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "GenDerivedRetType<!U,!V> TestType.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class C MyFunc(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "C TestType.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

// ========================================================================================
// THIRD LAYER types: explicitly override the virtual methods on GenBaseType, using
// more derived return types than the ones in the MethodImpls on GenTestType
// ========================================================================================

.class public auto ansi beforefieldinit GenDerivedTestType<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig newslot virtual instance class NonGenThroughGen5 NonGenThroughGenFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class B class GenBaseType<!U,!V>::NonGenThroughGenFunc(string& res)
    ldarg.1
    ldstr "NonGenThroughGen5 DerivedTestType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenToNonGen4<!V,object> GenToNonGen(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class B class GenBaseType<!U,!V>::GenToNonGen(string& res)
    ldarg.1
    ldstr "GenToNonGen4<!V,object> DerivedTestType.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class NonGenericDerived5 MyGenFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class GenRetType<int32,object> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "NonGenericDerived5 DerivedTestType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenDerive4<!U,!V> MyGenFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class GenRetType<!0, class Dictionary<string,!1>> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "GenDerive4<!U,!V> DerivedTestType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenDerivedRetType2<!U,!V> MyFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance object class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "GenDerivedRetType2<!U,!V> DerivedTestType.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class D MyFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class B class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "D DerivedTestType.MyFunc()"
    stind.ref
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

  // ============== Test using GenDerivedTestType ============== //
  // These test methods will callvirt each method using:
  //    1) The signature from GetBaseType
  //    2) The signature from GenTestType with covariant returns
  //    3) The signature from GenDerivedTestType with covariant returns
  // And verify that the override on GenDerivedTestType is the one that executes

  .method public static bool RunTest1() noinlining
  {
    .locals init (string res1, string res2, string res3)

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance object class GenBaseType<int32,object>::MyFunc(string&)
    pop

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class GenDerivedRetType<!0,!1> class GenTestType<int32,object>::MyFunc(string&)
    pop

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class GenDerivedRetType2<!0,!1> class GenDerivedTestType<int32,object>::MyFunc(string&)
    pop

    ldstr "GenDerivedRetType2<!U,!V> DerivedTestType.MyFunc()"
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

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class B class GenBaseType<int32,object>::MyFunc(string&)
    pop

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class C class GenTestType<int32,object>::MyFunc(string&)
    pop

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class D class GenDerivedTestType<int32,object>::MyFunc(string&)
    pop

    ldstr "D DerivedTestType.MyFunc()"
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

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class GenRetType<!0, class Dictionary<string,!1>> class GenBaseType<int32,object>::MyGenFunc(string&)
    pop

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class GenDerive3<!0,!1> class GenTestType<object,int32>::MyGenFunc(string&)
    pop

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class GenDerive4<!0,!1> class GenDerivedTestType<int32,object>::MyGenFunc(string&)
    pop

    ldstr "GenDerive4<!U,!V> DerivedTestType.MyGenFunc()"
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

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class GenRetType<int32,object> class GenBaseType<int32,object>::MyGenFunc(string&)
    pop

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class NonGenericDerived4 class GenTestType<object,int32>::MyGenFunc(string&)
    pop

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class NonGenericDerived5 class GenDerivedTestType<int32,object>::MyGenFunc(string&)
    pop

    ldstr "NonGenericDerived5 DerivedTestType.MyGenFunc()"
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

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class B class GenBaseType<int32,object>::GenToNonGen(string&)
    pop

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class GenToNonGen3<!1,object> class GenTestType<object,int32>::GenToNonGen(string&)
    pop

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class GenToNonGen4<!1,object> class GenDerivedTestType<int32,object>::GenToNonGen(string&)
    pop

    ldstr "GenToNonGen4<!V,object> DerivedTestType.GenToNonGen()"
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

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class B class GenBaseType<int32,object>::NonGenThroughGenFunc(string&)
    pop

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class NonGenThroughGen4 class GenTestType<object,int32>::NonGenThroughGenFunc(string&)
    pop

    newobj     instance void class GenDerivedTestType<int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class NonGenThroughGen5 class GenDerivedTestType<int32,object>::NonGenThroughGenFunc(string&)
    pop

    ldstr "NonGenThroughGen5 DerivedTestType.NonGenThroughGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
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
