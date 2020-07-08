// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly FtnTests
{
}

// Generic test class with class constraint
.class public auto ansi beforefieldinit GenericTestClass`1<class T>
       extends [System.Runtime]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
    {
        ldarg.0
        call      instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig instance void InstanceMethod() cil managed 
    {
        ret
    }

    .method public hidebysig static void StaticMethod() cil managed 
    {
        ret
    }
}

// Generic test class with default ctor constraint
.class public auto ansi beforefieldinit GenericCtor`1<.ctor T>
       extends [System.Runtime]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
    {
        ldarg.0
        call      instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

// Generic test class with default valuetype constraint
.class public auto ansi beforefieldinit GenericValueType`1<valuetype T>
       extends [System.Runtime]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
    {
        ldarg.0
        call      instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

// Test class with generic methods with constraints and private default ctor
.class public auto ansi beforefieldinit TestClass
       extends [System.Runtime]System.Object
{
    .method private hidebysig specialname rtspecialname instance void .ctor() cil managed 
    {
        ldarg.0
        call      instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method private hidebysig specialname rtspecialname instance void .ctor(int32 i) cil managed 
    {
        ldarg.0
        call      instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig instance void InstanceMethodRefConstr<class T>() cil managed 
    {
        ret
    }

    .method public hidebysig instance void InstanceMethod() cil managed 
    {
        ret
    }

    .method public hidebysig newslot virtual instance void VirtInstanceMethod() cil managed 
    {
        ret
    }

    .method public hidebysig static void StaticMethod() cil managed 
    {
        ret
    }

    .method public hidebysig static int32 StaticIntMethod() cil managed
    {
        ldc.i4.0
        ret
    }

    .method public hidebysig static void StaticMethodRefConstr<class T>() cil managed 
    {
        ret
    }
}

// Type containing test methods for delegate-assignment
.class public auto ansi beforefieldinit TestMethodsType
    extends [System.Runtime]System.Object
{
    .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
    {
        ldarg.0
        call       instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig static uint8 ByteReturnMethod() cil managed
    {
        ldc.i4.0
        ret
    }

    .method public hidebysig static valuetype ByteEnum ByteEnumReturnMethod() cil managed
    {
        ldc.i4.0
        box     ByteEnum
        ret
    }

    .method public hidebysig static int32 IntReturnMethod() cil managed
    {
        ldc.i4.0
        ret
    }

    .method public hidebysig static string StringReturnMethod() cil managed
    {
        ldnull
        ret
    }

    .method public hidebysig static object ObjectReturnMethod() cil managed
    {
        ldnull
        ret
    }

    .method public hidebysig static void ByteParamMethod(uint8 param) cil managed
    {
        ret
    }

    .method public hidebysig static void ByteEnumParamMethod(valuetype ByteEnum param) cil managed
    {
        ret
    }
}

.class public auto ansi sealed ByteEnum
       extends [System.Runtime]System.Enum
{
    .field public specialname rtspecialname uint8 value__
    .field public static literal valuetype ByteEnum A = uint8(0)
    .field public static literal valuetype ByteEnum B = uint8(0)
}

.class public auto ansi beforefieldinit FtnTestsType
       extends [System.Runtime]System.Object
{
    .method static public hidebysig void LdFtn.StaticClassRefConstraint_Valid() cil managed
    {
        // var a = new System.Action(GenericTestClass<TestClass>.StaticMethod);
        // a();

        ldnull
        ldftn       void class GenericTestClass`1<class TestClass>::StaticMethod()
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        callvirt    instance void [System.Runtime]System.Action::Invoke()
        ret
    }

    .method static public hidebysig void LdFtn.InstanceClassRefConstraint_Valid(class GenericTestClass`1<class TestClass> c) cil managed
    {
        // (GenericTestClass<TestClass> c)
        // var a = new System.Action(c.InstanceMethod);
        // a();

        ldarg.0
        ldftn       instance void class GenericTestClass`1<class TestClass>::InstanceMethod()
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        callvirt    instance void [System.Runtime]System.Action::Invoke()
        ret
    }

    .method static public hidebysig void LdFtn.IntForStaticClassRefConstraint_Invalid_UnsatisfiedMethodParentInst() cil managed
    {
        // var a = new System.Action(GenericTestClass<int>.StaticMethod);
        // a();

        ldnull
        ldftn       void class GenericTestClass`1<int32>::StaticMethod()
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        callvirt    instance void [System.Runtime]System.Action::Invoke()
        ret
    }

    .method static public hidebysig void LdFtn.IntForStaticMethodRefConstraint_Invalid_UnsatisfiedMethodInst() cil managed
    {
        // var a = new System.Action(TestClass.StaticMethod<int>);
        // a();

        ldnull
        ldftn       void TestClass::StaticMethodRefConstr<int32>()
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        callvirt    instance void [System.Runtime]System.Action::Invoke()
        ret
    }

    .method static public hidebysig void LdvirtFtn.InstanceMethod_Valid(class TestClass c) cil managed
    {
        // (TestClass c)
        // var a = new System.Action(c.InstanceMethod);
        // a();

        ldarg.0
        dup
        ldvirtftn   instance void TestClass::InstanceMethod()
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        callvirt    instance void [System.Runtime]System.Action::Invoke()
        ret
    }

    .method static public hidebysig void LdvirtFtn.VirtInstanceMethod_Valid(class TestClass c) cil managed
    {
        // (TestClass c)
        // var a = new System.Action(c.InstanceMethod);
        // a();

        ldarg.0
        dup
        ldvirtftn   instance void TestClass::VirtInstanceMethod()
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        callvirt    instance void [System.Runtime]System.Action::Invoke()
        ret
    }

    .method static public hidebysig void LdvirtFtn.ValueTypeInstanceMethod_Valid(int32 i) cil managed
    {
        // (int i)
        // var f = new System.Func<int, int>(i.CompareTo);
        // f(0);

        ldarg.0
        box         [System.Runtime]System.Int32
        dup
        ldvirtftn   instance int32 [System.Runtime]System.Int32::CompareTo(int32)
        newobj      instance void class [System.Runtime]System.Func`2<int32, int32>::.ctor(object, native int)
        ldc.i4.0
        callvirt    instance !1 class [System.Runtime]System.Func`2<int32, int32>::Invoke(!0)
        pop
        ret
    }

    .method static public hidebysig void LdvirtFtn.StaticMethod_Invalid_LdvirtftnOnStatic() cil managed
    {
        ldnull
        dup
        ldvirtftn   void TestClass::StaticMethod()
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        callvirt    instance void [System.Runtime]System.Action::Invoke()
        ret
    }

    .method static public hidebysig void LdvirtFtn.ValueTypeNoBox_Invalid_StackObjRef.StackUnexpected.StackUnexpected.DelegateCtorSigO(int32 i) cil managed
    {
        // (int i)
        // var f = new System.Func<int, int>(i.CompareTo);
        // f(0);

        ldarg.0
        dup
        ldvirtftn   instance int32 [System.Runtime]System.Int32::CompareTo(int32)
        newobj      instance void class [System.Runtime]System.Func`2<int32, int32>::.ctor(object, native int)
        ldc.i4.0
        callvirt    instance !1 class [System.Runtime]System.Func`2<int32, int32>::Invoke(!0)
        pop
        ret
    }

    .method static public hidebysig void LdvirtFtn.ValueTypeWrongBox_Invalid_StackUnexpected.DelegateCtorSigO.DelegatePattern(int32 i) cil managed
    {
        // (int i)
        // var f = new System.Func<int, int>(i.CompareTo);
        // f(0);

        ldarg.0
        dup
        box         [System.Runtime]System.Int32
        ldvirtftn   instance int32 [System.Runtime]System.Int32::CompareTo(int32)
        newobj      instance void class [System.Runtime]System.Func`2<int32, int32>::.ctor(object, native int)
        ldc.i4.0
        callvirt    instance !1 class [System.Runtime]System.Func`2<int32, int32>::Invoke(!0)
        pop
        ret
    }

    .method static public hidebysig void LdvirtFtn.ObjectForTestClassInstance_Invalid_StackUnexpected(object c) cil managed
    {
        // (object c)
        // var a = new System.Action(c.InstanceMethod);

        ldarg.0
        dup
        ldvirtftn   instance void TestClass::InstanceMethod()
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        pop
        ret
    }

    .method static public hidebysig void NewObj.RefGenericForRefConstraint_Valid<class T>() cil managed
    {
        // var c = new GenericTestClass`1<T>();

        newobj      instance void class GenericTestClass`1<!!T>::.ctor()
        pop
        ret
    }

    .method static public hidebysig void NewObj.IntForRefConstraint_Invalid_UnsatisfiedMethodParentInst() cil managed
    {
        // GenericTestClass<int> c = new GenericTestClass<int>();

        newobj      instance void class GenericTestClass`1<int32>::.ctor()
        pop
        ret
    }

    .method static public hidebysig void LdFtn.NopInDelegatePattern_Invalid_DelegatePattern() cil managed
    {
        // var a = new System.Action(TestClass.StaticMethod);
        // a();

        ldnull
        ldftn       void TestClass::StaticMethod()
        nop
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        callvirt    instance void [System.Runtime]System.Action::Invoke()
        ret
    }

    .method static public hidebysig void LdvirtFtn.NopInDelegatePattern_Invalid_DelegatePattern(class TestClass c) cil managed
    {
        // (TestClass c)
        // var a = new System.Action(c.InstanceMethod);
        // a();

        ldarg.0
        dup
        nop
        ldvirtftn   instance void TestClass::InstanceMethod()
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        callvirt    instance void [System.Runtime]System.Action::Invoke()
        ret
    }

    .method static public hidebysig void LdFtn.BranchIntoDelegatePattern_Invalid_DelegatePattern() cil managed
    {
        ldnull
        ldftn       void TestClass::StaticMethod()
        br          lbl_newobj

        ldnull
        ldftn       void TestClass::StaticMethod()

    lbl_newobj:
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        callvirt    instance void [System.Runtime]System.Action::Invoke()
        ret
    }

    .method static public hidebysig void LdvirtFtn.BranchIntoDelegatePattern_Invalid_DelegatePattern(class TestClass t) cil managed
    {
        ldarg.0
        dup
        br          lbl_ldvirt

        ldarg.0
        dup

    lbl_ldvirt:
        ldvirtftn   instance void TestClass::InstanceMethod()
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        callvirt    instance void [System.Runtime]System.Action::Invoke()
        ret
    }

    .method static public hidebysig void LdFtn.DelegateMissingArgument_Invalid_DelegateCtor() cil managed
    {
        ldnull
        ldftn       void TestClass::StaticMethod()
        newobj      instance void class [System.Runtime]System.Action`1<int32>::.ctor(object, native int)
        ldc.i4.0
        callvirt    instance void class [System.Runtime]System.Action`1<int32>::Invoke(!0)
        ret
    }

    .method static public hidebysig void LdFtn.DelegateWrongReturnType_Invalid_DelegateCtor() cil managed
    {
        ldnull
        ldftn       int32 TestClass::StaticIntMethod()
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        callvirt    instance void [System.Runtime]System.Action::Invoke()
        ret
    }

    .method static public hidebysig void LdFtn.VirtualMethodNonThisPtr_Invalid_LdftnNonFinalVirtual(class TestClass c) cil managed
    {
        ldarg.0
        ldftn       instance void TestClass::VirtInstanceMethod()
        newobj      instance void [System.Runtime]System.Action::.ctor(object, native int)
        callvirt    instance void [System.Runtime]System.Action::Invoke()
        ret
    }

    // creating a Func<object> from a string return method is valid
    .method private hidebysig instance void ObjectFuncFromStringMethod_Valid() cil managed
    {
        ldnull
        ldftn   string TestMethodsType::StringReturnMethod()
        newobj  instance void class [System.Runtime]System.Func`1<object>::.ctor(object, native int)
        pop
        ret
    }

    // creating a Func<string> from a object return method is invalid
    .method private hidebysig instance void StringFuncFromObjectMethod_Invalid_DelegateCtor() cil managed
    {
        ldnull
        ldftn   object TestMethodsType::ObjectReturnMethod()
        newobj  instance void class [System.Runtime]System.Func`1<string>::.ctor(object, native int)
        pop
        ret
    }

    // creating a Func<byte> from an int return method is invalid
    .method private hidebysig instance void ByteFuncFromIntMethod_Invalid_DelegateCtor() cil managed
    {
        ldnull
        ldftn   int32 TestMethodsType::IntReturnMethod()
        newobj  instance void class [System.Runtime]System.Func`1<uint8>::.ctor(object, native int)
        pop
        ret
    }

    // creating a Func<int> from a byte return method is invalid
    .method private hidebysig instance void IntFuncFromByteMethod_Invalid_DelegateCtor() cil managed
    {
        ldnull
        ldftn   uint8 TestMethodsType::ByteReturnMethod()
        newobj  instance void class [System.Runtime]System.Func`1<int32>::.ctor(object, native int)
        pop
        ret
    }

    // creating a Func<byte> from a ByteEnum return method is invalid
    .method private hidebysig instance void IntFuncFromByteEnumMethod_Invalid_DelegateCtor() cil managed
    {
        ldnull
        ldftn   valuetype ByteEnum TestMethodsType::ByteEnumReturnMethod()
        newobj  instance void class [System.Runtime]System.Func`1<int32>::.ctor(object, native int)
        pop
        ret
    }

    // creating an Action<int> from a method with byte parameter is invalid
    .method private hidebysig instance void IntActionFromByteMethod_Invalid_DelegateCtor() cil managed
    {
        ldnull
        ldftn   void TestMethodsType::ByteParamMethod(uint8)
        newobj  instance void class [System.Runtime]System.Action`1<int32>::.ctor(object, native int)
        pop
        ret
    }

    // creating an Action<byte> from a method with an enum (with underlying type byte) as parameter is invalid
    .method private hidebysig instance void ByteActionFromByteEnum_Invalid_DelegateCtor() cil managed
    {
        ldnull
        ldftn   void TestMethodsType::ByteEnumParamMethod(valuetype ByteEnum)
        newobj  instance void class [System.Runtime]System.Action`1<uint8>::.ctor(object, native int)
        pop
        ret
    }
}
