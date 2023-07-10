// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly extern System.Runtime { }
.assembly extern mscorlib { }
.assembly UnitTest { }

// ========================================================================================
// Types that will be used as return types on the various methods
// ========================================================================================

.class interface public auto ansi abstract IUnused1 { }
.class interface public auto ansi abstract IUnused2 { }

.class interface public auto ansi abstract IA implements IUnused1, IUnused2 { }
.class interface public auto ansi abstract IB implements IUnused1, IUnused2, IA  { }
.class interface public auto ansi abstract IC implements IUnused1, IUnused2, IB  { }

.class interface public auto ansi abstract IGenRetType<X,Y>  { }
.class interface public auto ansi abstract IDictionary<KEY,VAL>  { }

.class public auto ansi abstract CA {}
.class public auto ansi abstract CB extends CA {}
.class public auto ansi abstract CC extends CB {}

.class interface public auto ansi abstract ICovariant<+ T>  { }
.class interface public auto ansi abstract IContravariant<- T>  { }

.class interface public auto ansi abstract IGenDerive1<V1,V2> implements IUnused1, IUnused2, class IGenRetType<!V1,!V2>  { }
.class interface public auto ansi abstract IGenDerive2<U1,U2,U3> implements IUnused1, IUnused2, class IGenDerive1<!U1, class IDictionary<!U3,!U2>>  { }
.class interface public auto ansi abstract IGenDerive3<T1,T2> implements IUnused1, IUnused2, class IGenDerive2<!T1,!T2,string>  { }

.class interface public auto ansi abstract INonGenericDerived1<ARG1,ARG2> implements IUnused1, IUnused2, class IGenRetType<!ARG1,!ARG2>  { }
.class interface public auto ansi abstract INonGenericDerived2<T> implements IUnused1, IUnused2, class INonGenericDerived1<!T,object>  { }
.class interface public auto ansi abstract INonGenericDerived3 implements IUnused1, IUnused2, class INonGenericDerived2<int32>  { }
.class interface public auto ansi abstract INonGenericDerived4 implements IUnused1, IUnused2, INonGenericDerived3  { }

.class interface public auto ansi abstract IGenToNonGen1<V1,V2> implements IUnused1, IUnused2, IC  { }
.class interface public auto ansi abstract IGenToNonGen2<U1,U2,U3> implements IUnused1, IUnused2, class IGenToNonGen1<!U1, class IDictionary<!U3,!U2>>  { }
.class interface public auto ansi abstract IGenToNonGen3<T1,T2> implements IUnused1, IUnused2, class IGenToNonGen2<!T1,!T2,string>  { }

.class interface public auto ansi abstract INonGenThroughGen1<V1,V2> implements IUnused1, IUnused2, IC  { }
.class interface public auto ansi abstract INonGenThroughGen2<U1,U2,U3> implements IUnused1, IUnused2, class INonGenThroughGen1<!U1, class IDictionary<!U3,!U2>>  { }
.class interface public auto ansi abstract INonGenThroughGen3 implements IUnused1, IUnused2, class INonGenThroughGen2<object,int32,string>  { }
.class interface public auto ansi abstract INonGenThroughGen4 implements IUnused1, IUnused2, INonGenThroughGen3  { }

// class implementing the interfaces
.class public auto ansi beforefieldinit NonGenThroughGen4   implements IUnused1, IUnused2, INonGenThroughGen4 { }
.class public auto ansi beforefieldinit GenToNonGen3<T1,T2> implements IUnused1, IUnused2, class IGenToNonGen3<!T1,!T2> { }
.class public auto ansi beforefieldinit NonGenericDerived4  implements IUnused1, IUnused2, INonGenericDerived4 { }
.class public auto ansi beforefieldinit GenDerive3<T1,T2>   implements IUnused1, IUnused2, class IGenDerive3<!T1,!T2> { }
.class public auto ansi beforefieldinit C                   implements IUnused1, IUnused2, IC { }
.class public auto ansi beforefieldinit GenRetType<X,Y>     implements IUnused1, IUnused2, class IGenRetType<!X,!Y> { }

.class public auto ansi beforefieldinit Base<T> {}
.class public auto ansi beforefieldinit Derived extends class Base<class Derived> {}
.class public auto ansi beforefieldinit Derived2 extends class Base<class Derived2> {}

.class interface public auto ansi abstract IVariant<+ V>
{
  .method public hidebysig newslot virtual instance void Test()
  {
    ret
  }
}

// ========================================================================================
// Main base type with various virtual methods that will be overridden later
// ========================================================================================

.class public auto ansi beforefieldinit GenBaseType<A,B>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed  { ret }

  .method public hidebysig newslot virtual instance object MyFunc(string& res)
  {
    ldarg.1
    ldstr "object GenBaseType.MyFunc()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IB MyFunc(string& res)
  {
    ldarg.1
    ldstr "IB GenBaseType.MyFunc()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class ICovariant<class CB> MyFuncCovariant(string& res)
  {
    ldarg.1
    ldstr "ICovariant<CB> GenBaseType.MyFuncCovariant()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IContravariant<class CB> MyFuncContravariant(string& res)
  {
    ldarg.1
    ldstr "IContravariant<CB> GenBaseType.MyFuncContravariant()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IB GenToNonGen(string& res)
  {
    ldarg.1
    ldstr "IB GenBaseType.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IB NonGenThroughGenFunc(string& res)
  {
    ldarg.1
    ldstr "IB GenBaseType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IGenRetType<int32,object> MyGenFunc(string& res)
  {
    ldarg.1
    ldstr "IGenRetType<int32,object> GenBaseType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IGenRetType<!A, class IDictionary<string,!B>> MyGenFunc(string& res)
  {
    ldarg.1
    ldstr "IGenRetType<!A, class IDictionary<string,!B>> GenBaseType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IVariant<class Base<class Derived>> MultiLevelGenericVariantFunc(string&)
  {
    ldarg.1
    ldstr "IVariant<class Base<class Derived>> GenBaseType.MultiLevelGenericVariantFunc()"
    stind.ref
    ldnull
    ret
  }
}

// ========================================================================================
// SECOND LAYER type: overrides *SOME* virtuals on GenBaseType using MethodImpls with
// covariant return types (more derived return types)
// ========================================================================================

.class public auto ansi beforefieldinit GenMiddleType<U,V> extends class GenBaseType<!V,!U>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class INonGenThroughGen2<object,int32,string> NonGenThroughGenFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IB class GenBaseType<!V,!U>::NonGenThroughGenFunc(string& res)
    ldarg.1
    ldstr "INonGenThroughGen2<object,int32,string> GenMiddleType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IGenToNonGen1<!V, class IDictionary<string,object>> GenToNonGen(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IB class GenBaseType<!V,!U>::GenToNonGen(string& res)
    ldarg.1
    ldstr "IGenToNonGen1<!V, class IDictionary<string,object>> GenMiddleType.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class INonGenericDerived1<int32,object> MyGenFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IGenRetType<int32,object> class GenBaseType<!V,!U>::MyGenFunc(string& res)
    ldarg.1
    ldstr "INonGenericDerived1<int32,object> GenMiddleType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IGenDerive1<!V, class IDictionary<string,!U>> MyGenFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IGenRetType<!0, class IDictionary<string,!1>> class GenBaseType<!V,!U>::MyGenFunc(string& res)
    ldarg.1
    ldstr "IGenDerive1<!U, class IDictionary<string,!V>> GenMiddleType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }
}


// ========================================================================================
// THIRD LAYER type: overrides all virtuals from GenBaseType using MethodImpls with
// covariant return types (more derived return types than the ones used in GenMiddleType)
// ========================================================================================

.class public auto ansi beforefieldinit GenTestType<UNUSED1,UNUSED2,U,V> extends class GenMiddleType<!V,!U>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class INonGenThroughGen4 NonGenThroughGenFunc(string& res)
  {
    .override method instance class IB class GenBaseType<!U,!V>::NonGenThroughGenFunc(string& res)
    ldarg.1
    ldstr "INonGenThroughGen4 TestType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IGenToNonGen3<!U,object> GenToNonGen(string& res)
  {
    .override method instance class IB class GenBaseType<!U,!V>::GenToNonGen(string& res)
    ldarg.1
    ldstr "IGenToNonGen3<!U,object> TestType.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class INonGenericDerived4 MyGenFunc(string& res)
  {
    .override method instance class IGenRetType<int32,object> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "INonGenericDerived4 TestType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IGenDerive3<!U,!V> MyGenFunc(string& res)
  {
    .override method instance class IGenRetType<!0, class IDictionary<string,!1>> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "IGenDerive3<!U,!V> TestType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IGenRetType<!U,!V> MyFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance object class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "IGenRetType<!U,!V> TestType.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IC MyFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IB class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "IC TestType.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class ICovariant<class CC> MyFuncCovariant(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class ICovariant<class CB> class GenBaseType<!U,!V>::MyFuncCovariant(string& res)
    ldarg.1
    ldstr "ICovariant<CC> TestType.MyFuncCovariant()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IContravariant<class CA> MyFuncContravariant(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IContravariant<class CB> class GenBaseType<!U,!V>::MyFuncContravariant(string& res)
    ldarg.1
    ldstr "IContravariant<CA> TestType.MyFuncContravariant()"
    stind.ref
    ldnull
    ret
  }

  // ========================================================================================
  // Set of implicit overrides that should be ignored given there are explicit overrides from the MethodImpls
  // ========================================================================================
  .method public hidebysig virtual instance class IB NonGenThroughGenFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class IB GenToNonGen(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class IGenRetType<int32,object> MyGenFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class IGenRetType<!2, class IDictionary<string,!3>> MyGenFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance object MyFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class IB MyFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class ICovariant<class CB> MyFuncCovariant(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class IContravariant<class CB> MyFuncContravariant(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }
}

// ========================================================================================
// FOURTH LAYER type: overrides all virtuals from GenBaseType using MethodImpls with
// covariant return types (classes that implement the interfaces used as return types)
// ========================================================================================

.class public auto ansi beforefieldinit GenMoreDerived<UNUSED1,UNUSED2,U,V> extends class GenTestType<!UNUSED2,!UNUSED1,!U,!V>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class NonGenThroughGen4 NonGenThroughGenFunc(string& res)
  {
    .override method instance class IB class GenBaseType<!U,!V>::NonGenThroughGenFunc(string& res)
    ldarg.1
    ldstr "class NonGenThroughGen4 GenMoreDerived.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenToNonGen3<!U,object> GenToNonGen(string& res)
  {
    .override method instance class IB class GenBaseType<!U,!V>::GenToNonGen(string& res)
    ldarg.1
    ldstr "class GenToNonGen3<!U,object> GenMoreDerived.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class NonGenericDerived4 MyGenFunc(string& res)
  {
    .override method instance class IGenRetType<int32,object> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "class NonGenericDerived4 GenMoreDerived.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenDerive3<!U,!V> MyGenFunc(string& res)
  {
    .override method instance class IGenRetType<!0, class IDictionary<string,!1>> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "class GenDerive3<!U,!V> GenMoreDerived.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenRetType<!U,!V> MyFunc(string& res)
  {
    .override method instance object class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "class GenRetType<!U,!V> GenMoreDerived.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class C MyFunc(string& res)
  {
    .override method instance class IB class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "class C GenMoreDerived.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IVariant<class Derived> MultiLevelGenericVariantFunc(string&)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IVariant<class Base<class Derived>> class GenBaseType<!U,!V>::MultiLevelGenericVariantFunc(string&)
    ldarg.1
    ldstr "class IVariant<class Derived> GenMoreDerived.MultiLevelGenericVariantFunc()"
    stind.ref
    ldnull
    ret
  }
}


// ========================================================================================
// FIFTH LAYER INVALID type: Used to verify we can't override the method using a compatible interface
// if it has been already overridden using a class that implements the interface (i.e. the new
// interface return type won't be compatible with the class return type on the previous override
// ========================================================================================

.class public auto ansi beforefieldinit Invalid1<Q,W> extends class GenMoreDerived<!Q,!W,!Q,!W>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class INonGenThroughGen4 NonGenThroughGenFunc(string& res)
  {
    .override method instance class IB class GenBaseType<!Q,!W>::NonGenThroughGenFunc(string& res)
    ldnull
    ret
  }
}

// ========================================================================================
// FIFTH LAYER INVALID type: Used to verify we can't override the method using a less derived interface
// than one that has already been used in a previous override
// ========================================================================================

.class public auto ansi beforefieldinit Invalid2<Q,W> extends class GenTestType<!Q,!W,!Q,!W>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class INonGenThroughGen2<object,int32,string> NonGenThroughGenFunc(string& res)
  {
    .override method instance class IB class GenBaseType<!Q,!W>::NonGenThroughGenFunc(string& res)
    ldnull
    ret
  }
}

.class public auto ansi beforefieldinit Invalid3<Q,W> extends class GenTestType<!Q,!W,!Q,!W>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class IVariant<class Derived2> MultiLevelGenericVariantFunc(string&)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IVariant<class Base<class Derived>> class GenBaseType<!Q,!W>::MultiLevelGenericVariantFunc(string&)
    ldnull
    ret
  }
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

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance object class GenBaseType<int32,object>::MyFunc(string&)
    pop

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class IGenRetType<!2,!3> class GenTestType<int32,object,int32,object>::MyFunc(string&)
    pop

    ldstr "IGenRetType<!U,!V> TestType.MyFunc()"
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

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class IB class GenBaseType<int32,object>::MyFunc(string&)
    pop

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class IC class GenTestType<int32,object,int32,object>::MyFunc(string&)
    pop

    ldstr "IC TestType.MyFunc()"
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

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class IGenRetType<!0, class IDictionary<string,!1>> class GenBaseType<int32,object>::MyGenFunc(string&)
    pop

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class IGenDerive1<!1, class IDictionary<string,!0>> class GenMiddleType<object,int32>::MyGenFunc(string&)
    pop

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class IGenDerive3<!2,!3> class GenTestType<int32,object,int32,object>::MyGenFunc(string&)
    pop

    ldstr "IGenDerive3<!U,!V> TestType.MyGenFunc()"
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

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class IGenRetType<int32,object> class GenBaseType<int32,object>::MyGenFunc(string&)
    pop

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class INonGenericDerived1<int32,object> class GenMiddleType<object,int32>::MyGenFunc(string&)
    pop

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class INonGenericDerived4 class GenTestType<int32,object,int32,object>::MyGenFunc(string&)
    pop

    ldstr "INonGenericDerived4 TestType.MyGenFunc()"
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

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class IB class GenBaseType<int32,object>::GenToNonGen(string&)
    pop

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class IGenToNonGen1<!1, class IDictionary<string,object>> class GenMiddleType<object,int32>::GenToNonGen(string&)
    pop

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class IGenToNonGen3<!2,object> class GenTestType<int32,object,int32,object>::GenToNonGen(string&)
    pop

    ldstr "IGenToNonGen3<!U,object> TestType.GenToNonGen()"
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

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class IB class GenBaseType<int32,object>::NonGenThroughGenFunc(string&)
    pop

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class INonGenThroughGen2<object,int32,string> class GenMiddleType<object,int32>::NonGenThroughGenFunc(string&)
    pop

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class INonGenThroughGen4 class GenTestType<int32,object,int32,object>::NonGenThroughGenFunc(string&)
    pop

    ldstr "INonGenThroughGen4 TestType.NonGenThroughGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest7() noinlining
  {
    .locals init (string res1, string res2, string res3)

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class ICovariant<class CB> class GenBaseType<int32,object>::MyFuncCovariant(string&)
    pop

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class ICovariant<class CC> class GenTestType<int32,object,int32,object>::MyFuncCovariant(string&)
    pop

    ldstr "ICovariant<CC> TestType.MyFuncCovariant()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest8() noinlining
  {
    .locals init (string res1, string res2, string res3)

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class IContravariant<class CB> class GenBaseType<int32,object>::MyFuncContravariant(string&)
    pop

    newobj     instance void class GenTestType<int32,object,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class IContravariant<class CA> class GenTestType<int32,object,int32,object>::MyFuncContravariant(string&)
    pop

    ldstr "IContravariant<CA> TestType.MyFuncContravariant()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest9() noinlining
  {
    .locals init (string res1)
    newobj     instance void class GenMoreDerived<int32,object,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class IVariant<class Derived> class GenMoreDerived<int32,object,int32,object>::MultiLevelGenericVariantFunc(string&)
    pop

    ldstr "class IVariant<class Derived> GenMoreDerived.MultiLevelGenericVariantFunc()"
    ldloc.0
    ldnull
    ldnull
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

    newobj     instance void class GenMiddleType<object,int32>::.ctor()
    ldloca.s   0
    callvirt   instance class IGenRetType<!0, class IDictionary<string,!1>> class GenBaseType<int32,object>::MyGenFunc(string&)
    pop

    newobj     instance void class GenMiddleType<object,int32>::.ctor()
    ldloca.s   1
    callvirt   instance class IGenDerive1<!1, class IDictionary<string,!0>> class GenMiddleType<object,int32>::MyGenFunc(string&)
    pop

    ldstr "IGenDerive1<!U, class IDictionary<string,!V>> GenMiddleType.MyGenFunc()"
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

    newobj     instance void class GenMiddleType<object,int32>::.ctor()
    ldloca.s   0
    callvirt   instance class IGenRetType<int32,object> class GenBaseType<int32,object>::MyGenFunc(string&)
    pop

    newobj     instance void class GenMiddleType<object,int32>::.ctor()
    ldloca.s   1
    callvirt   instance class INonGenericDerived1<int32,object> class GenMiddleType<object,int32>::MyGenFunc(string&)
    pop

    ldstr "INonGenericDerived1<int32,object> GenMiddleType.MyGenFunc()"
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

    newobj     instance void class GenMiddleType<object,int32>::.ctor()
    ldloca.s   0
    callvirt   instance class IB class GenBaseType<int32,object>::GenToNonGen(string&)
    pop

    newobj     instance void class GenMiddleType<object,int32>::.ctor()
    ldloca.s   1
    callvirt   instance class IGenToNonGen1<!1, class IDictionary<string,object>> class GenMiddleType<object,int32>::GenToNonGen(string&)
    pop

    ldstr "IGenToNonGen1<!V, class IDictionary<string,object>> GenMiddleType.GenToNonGen()"
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

    newobj     instance void class GenMiddleType<object,int32>::.ctor()
    ldloca.s   0
    callvirt   instance class IB class GenBaseType<int32,object>::NonGenThroughGenFunc(string&)
    pop

    newobj     instance void class GenMiddleType<object,int32>::.ctor()
    ldloca.s   1
    callvirt   instance class INonGenThroughGen2<object,int32,string> class GenMiddleType<object,int32>::NonGenThroughGenFunc(string&)
    pop

    ldstr "INonGenThroughGen2<object,int32,string> GenMiddleType.NonGenThroughGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  // ============== Test using GenMoreDerived ============== //
  // These test methods will callvirt each method using:
  //    1) The signature from GetBaseType
  //    2) The signature from GenMiddleType with covariant returns (when applicable)
  //    3) The signature from GenTestType with covariant returns
  //    4) The signature from GenMoreDerived with covariant returns
  // And verify that the override on GenMoreDerived is the one that executes

  .method public static bool RunTest_MoreDerived1() noinlining
  {
    .locals init (string res1, string res2, string res3, string res4)

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance object class GenBaseType<int32,object>::MyFunc(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class IGenRetType<!2,!3> class GenTestType<int32,object,int32,object>::MyFunc(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class GenRetType<!2,!3> class GenMoreDerived<object,int32,int32,object>::MyFunc(string&)
    pop

    ldstr "class GenRetType<!U,!V> GenMoreDerived.MyFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldloc.3
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest_MoreDerived2() noinlining
  {
    .locals init (string res1, string res2, string res3, string res4)

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class IB class GenBaseType<int32,object>::MyFunc(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class IC class GenTestType<int32,object,int32,object>::MyFunc(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class C class GenMoreDerived<object,int32,int32,object>::MyFunc(string&)
    pop

    ldstr "class C GenMoreDerived.MyFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldloc.3
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest_MoreDerived3() noinlining
  {
    .locals init (string res1, string res2, string res3, string res4)

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class IGenRetType<!0, class IDictionary<string,!1>> class GenBaseType<int32,object>::MyGenFunc(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class IGenDerive1<!1, class IDictionary<string,!0>> class GenMiddleType<object,int32>::MyGenFunc(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class IGenDerive3<!2,!3> class GenTestType<int32,object,int32,object>::MyGenFunc(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   3
    callvirt   instance class GenDerive3<!2,!3> class GenMoreDerived<object,int32,int32,object>::MyGenFunc(string&)
    pop

    ldstr "class GenDerive3<!U,!V> GenMoreDerived.MyGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldloc.3
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest_MoreDerived4() noinlining
  {
    .locals init (string res1, string res2, string res3, string res4)

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class IGenRetType<int32,object> class GenBaseType<int32,object>::MyGenFunc(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class INonGenericDerived1<int32,object> class GenMiddleType<object,int32>::MyGenFunc(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class INonGenericDerived4 class GenTestType<int32,object,int32,object>::MyGenFunc(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   3
    callvirt   instance class NonGenericDerived4 class GenMoreDerived<object,int32,int32,object>::MyGenFunc(string&)
    pop

    ldstr "class NonGenericDerived4 GenMoreDerived.MyGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldloc.3
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest_MoreDerived5() noinlining
  {
    .locals init (string res1, string res2, string res3, string res4)

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class IB class GenBaseType<int32,object>::GenToNonGen(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class IGenToNonGen1<!1, class IDictionary<string,object>> class GenMiddleType<object,int32>::GenToNonGen(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class IGenToNonGen3<!2,object> class GenTestType<int32,object,int32,object>::GenToNonGen(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   3
    callvirt   instance class GenToNonGen3<!2,object> class GenMoreDerived<object,int32,int32,object>::GenToNonGen(string&)
    pop

    ldstr "class GenToNonGen3<!U,object> GenMoreDerived.GenToNonGen()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldloc.3
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public static bool RunTest_MoreDerived6() noinlining
  {
    .locals init (string res1, string res2, string res3, string res4)

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   0
    callvirt   instance class IB class GenBaseType<int32,object>::NonGenThroughGenFunc(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   1
    callvirt   instance class INonGenThroughGen2<object,int32,string> class GenMiddleType<object,int32>::NonGenThroughGenFunc(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   2
    callvirt   instance class INonGenThroughGen4 class GenTestType<int32,object,int32,object>::NonGenThroughGenFunc(string&)
    pop

    newobj     instance void class GenMoreDerived<object,int32,int32,object>::.ctor()
    ldloca.s   3
    callvirt   instance class NonGenThroughGen4 class GenMoreDerived<object,int32,int32,object>::NonGenThroughGenFunc(string&)
    pop

    ldstr "class NonGenThroughGen4 GenMoreDerived.NonGenThroughGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldloc.3
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }


  // ===================================================================================== //

  .method public static void RunTest_Invalid1() noinlining
  {
    newobj     instance void class Invalid1<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest_Invalid2() noinlining
  {
    newobj     instance void class Invalid2<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest_Invalid3() noinlining
  {
    newobj     instance void class Invalid3<int32,object>::.ctor()
    callvirt   instance class IVariant<class Derived2> class Invalid3<int32,object>::MultiLevelGenericVariantFunc()
    pop
    ret
  }
  // ===================================================================================== //

  .method public hidebysig static int32  Main() cil managed
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
    brtrue.s T7
    ldc.i4.0
    stloc.0

    T7:
    call bool CMain::RunTest7()
    brtrue.s T8
    ldc.i4.0
    stloc.0

    T8:
    call bool CMain::RunTest8()
    brtrue.s T9
    ldc.i4.0
    stloc.0

    T9:
    call bool CMain::RunTest9()
    brtrue.s M1
    ldc.i4.0
    stloc.0

    // ===================================================================================== //

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
    brtrue.s MOREDERIVED1
    ldc.i4.0
    stloc.0

    // ===================================================================================== //

    MOREDERIVED1:
    call bool CMain::RunTest_MoreDerived1()
    brtrue.s MOREDERIVED2
    ldc.i4.0
    stloc.0

    MOREDERIVED2:
    call bool CMain::RunTest_MoreDerived2()
    brtrue.s MOREDERIVED3
    ldc.i4.0
    stloc.0

    MOREDERIVED3:
    call bool CMain::RunTest_MoreDerived3()
    brtrue.s MOREDERIVED4
    ldc.i4.0
    stloc.0

    MOREDERIVED4:
    call bool CMain::RunTest_MoreDerived4()
    brtrue.s MOREDERIVED5
    ldc.i4.0
    stloc.0

    MOREDERIVED5:
    call bool CMain::RunTest_MoreDerived5()
    brtrue.s MOREDERIVED6
    ldc.i4.0
    stloc.0

    MOREDERIVED6:
    call bool CMain::RunTest_MoreDerived6()
    brtrue.s INVALID1
    ldc.i4.0
    stloc.0

    // ===================================================================================== //

    INVALID1:
    .try
    {
        call void CMain::RunTest_Invalid1()
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
        call void CMain::RunTest_Invalid2()
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
        call void CMain::RunTest_Invalid3()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading DerivedClassFail1."
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

    // ===================================================================================== //

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
