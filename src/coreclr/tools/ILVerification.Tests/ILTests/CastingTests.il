// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly CastingTests
{
}

// Provides tests for casting arrays
.class public auto ansi beforefieldinit ArrayCastingTestsType
       extends [System.Runtime]System.Object
{
    // Array<T> tests
    .method static public hidebysig void Casting.ArrayOfTToEnumerableOfT_Valid(int32[] test) cil managed
    {
        .locals init (
            class [System.Runtime]System.Collections.Generic.IEnumerable`1<int32> V_0
        )
        ldarg.0
        stloc.0
        ret
    }

    .method static public hidebysig void Casting.ArrayOfTToEnumerable_Valid(int32[] test) cil managed
    {
        .locals init (
            class [System.Runtime]System.Collections.IEnumerable V_0
        )
        ldarg.0
        stloc.0
        ret
    }

    .method static public hidebysig void Casting.ArrayOfTToICollectionOfT_Valid(int32[] test) cil managed
    {
        .locals init (
            class [System.Runtime]System.Collections.Generic.ICollection`1<int32> V_0
        )
        ldarg.0
        stloc.0
        ret
    }

    .method static public hidebysig void Casting.ArrayOfTToICollection_Valid(int32[] test) cil managed
    {
        .locals init (
            class [System.Runtime]System.Collections.ICollection V_0
        )
        ldarg.0
        stloc.0
        ret
    }

    .method static public hidebysig void Casting.ArrayOfTToIListOfT_Valid(int32[] test) cil managed
    {
        .locals init (
            class [System.Runtime]System.Collections.Generic.IList`1<int32> V_0
        )
        ldarg.0
        stloc.0
        ret
    }

    .method static public hidebysig void Casting.ArrayOfTToIList_Valid(int32[] test) cil managed
    {
        .locals init (
            class [System.Runtime]System.Collections.IList V_0
        )
        ldarg.0
        stloc.0
        ret
    }

    .method static public hidebysig void Casting.ArrayOfTToIReadOnlyCollectionOfT_Valid(int32[] test) cil managed
    {
        .locals init (
            class [System.Runtime]System.Collections.Generic.IReadOnlyCollection`1<int32> V_0
        )
        ldarg.0
        stloc.0
        ret
    }

    .method static public hidebysig void Casting.ArrayOfTToIReadOnlyListOfT_Valid(int32[] test) cil managed
    {
        .locals init (
            class [System.Runtime]System.Collections.Generic.IReadOnlyList`1<int32> V_0
        )
        ldarg.0
        stloc.0
        ret
    }

    .method static public hidebysig void Casting.ArrayOfTToArray_Valid(int32[] test) cil managed
    {
        .locals init (
            class [System.Runtime]System.Array V_0
        )
        ldarg.0
        stloc.0
        ret
    }

    .method static public hidebysig void Assign.ConstrainedGenericToComparable_Valid<(class [System.Runtime]System.IComparable`1<!!T>) T>(!!T t)
    {
        .locals init (
            class [System.Runtime]System.IComparable`1<!!T> c
        )

        ldarg.0
        box     !!T
        stloc.0
        ret
    }

    .method static public hidebysig void Assign.ComplexConstrainedGenericToComparable_Valid<(!!S) T, (class [System.Runtime]System.IComparable`1<!!T>) S>(!!T t)
    {
        .locals init (
            class [System.Runtime]System.IComparable`1<!!T> c
        )

        ldarg.0
        box     !!T
        stloc.0
        ret
    }
}

.class public sequential ansi sealed beforefieldinit GenericOtherFieldsType`1<T>
       extends [System.Runtime]System.ValueType
{
    .field public !T GenericField;
    .field int32 IntField;
}

.class public sequential ansi beforefieldinit GenericTypeWithConstrained<class ([System.Runtime]System.IComparable) TValueType>
        extends [System.Runtime]System.Object
{
    .property !TValueType Value()
    {
        .get instance !0 GenericTypeWithConstrained::get_Value()
    }
    .method public !TValueType get_Value()
    {
        ldc.i4 0x00000000
        ret
    }
}

// Provides casting logic tests for stfld, ldfld, call, callvirt
.class public auto ansi beforefieldinit GenericCastingTestsType`1<T>
       extends [System.Runtime]System.Object
{
    .field private int32 ThisIntField
    .method public hidebysig  instance void Casting.StorePlainFieldInThisGenericType_Valid(int32 v) cil managed
    {
        ldarg.0
        ldarg.1
        stfld int32 class GenericCastingTestsType`1<!T>::ThisIntField
        ret
    }

    .method public hidebysig  instance void Casting.StorePlainFieldInOtherGenericType_Valid(int32 v) cil managed
    {
        .locals init (
            class GenericOtherFieldsType`1<!T> V_0
        )

        ldloc.0
        ldarg.1
        stfld !0 class GenericOtherFieldsType`1<!T>::IntField
        ret
    }

    .field private !T ThisGenericField
    .method public hidebysig  instance void Casting.StoreGenericFieldInThisGenericType_Valid(!T v) cil managed
    {
        ldarg.0
        ldarg.1
        stfld !0 class GenericCastingTestsType`1<!T>::ThisGenericField
        ret
    }

    .method public hidebysig  instance void Casting.StoreGenericFieldInOtherGenericType_Valid(!T v) cil managed
    {
        .locals init (
            class GenericOtherFieldsType`1<!T> V_0
        )
        ldloc.0
        ldarg.1
        stfld !0 class GenericOtherFieldsType`1<!T>::GenericField
        ret
    }

    .method public hidebysig  instance void Casting.CallPlainFunctionFromThisGenericType_Valid(int32 v) cil managed
    {
        ldarg.0
        ldarg.1
        callvirt instance void class GenericCastingTestsType`1<!T>::Casting.StorePlainFieldInThisGenericType_Valid(int32)
        ret
    }

    .method public hidebysig  instance void Casting.CallGenericFunctionFromThisGenericType_Valid(!T v) cil managed
    {
        ldarg.0
        ldarg.1
        callvirt instance void class GenericCastingTestsType`1<!T>::Casting.CallGenericFunctionFromOtherGenericType_Valid(!0)
        ret
    }

    .method public hidebysig  instance void Casting.CallGenericFunctionFromOtherGenericType_Valid(!T v) cil managed
    {
        .locals init (
            class [System.Runtime]System.Func`1<!T> V_0
        )

        ldloc.0
        callvirt  instance !0 class [System.Runtime]System.Func`1<!T>::Invoke()
        pop
        ret
    }

    .method public hidebysig instance void Casting.AssignVariantInterfaceToObject_Valid(class [System.Runtime]System.Collections.Generic.IEnumerable`1<string> e) cil managed
    {
        .locals init (
            object o
        )

        ldarg.1
        stloc.0
        ret
    }

    .method public hidebysig instance void Casting.AssignThisToSameTypeWithOtherGenericArgs_Invalid_StackUnexpected() cil managed
    {
        .locals init (
            class GenericCastingTestsType`1<int32> V_0
        )
        
        ldarg.0
        stloc.0
        ret
    }

    .method public hidebysig instance void Casting.AssignGenericToObject_Valid(!T v) cil managed
    {
        .locals init (
            object
        )

        ldarg.1
        box     !T
        stloc.0
        ret
    }

    .method public hidebysig static void Casting.AssignToSameTypeWithOtherGenericArgs_Invalid_StackUnexpected() cil managed
    {
        .locals init (
            class GenericCastingTestsType`1<int32> V_0,
            class GenericCastingTestsType`1<string> V_1
        )
        
        ldloc.0
        stloc.1
        ret
    }

    .method public static void Casting.AssignReturnValueFromConstrainedProperty_Valid<class ([System.Runtime]System.IComparable) TValueType>(class GenericTypeWithConstrained<!!TValueType> wrapper) cil managed
    {
        .locals init (
            class [System.Runtime]System.IComparable V_0
        )

        ldarg.0
        callvirt instance !0 class GenericTypeWithConstrained<!!TValueType>::get_Value()
        box      !!TValueType
        stloc.0
        ret
    }
}

// Class with private nested class
.class public auto ansi beforefieldinit ContainingClass
       extends [System.Runtime]System.Object
{
    .class nested private auto ansi beforefieldinit PrivateNestedClass
        extends [System.Runtime]System.Object
    {
    }
}


// Test enum
.class public auto ansi sealed Enum
       extends [System.Runtime]System.Enum
{
    .field public static literal valuetype Enum EnumValue1 = int32(0)
    .field public static literal valuetype Enum EnumValue2 = int32(1)

    .field public specialname rtspecialname int32 value__
}

.class public auto ansi beforefieldinit CastingTestsType
       extends [System.Runtime]System.Object
{
    .method public static void Casting.AssignPrimitiveToIComparable_Valid(int32 arg) cil managed
    {
        .locals init (
            class [System.Runtime]System.IComparable
        )

        ldarg.0
        box     [System.Runtime]System.Int32
        stloc.0
        ret
    }

    .method public static void Casting.CastToInaccessibleType_Invalid_TypeAccess() cil managed
    {
        ldnull
        castclass ContainingClass/PrivateNestedClass
        pop
        ret
    }

    .method public static void Casting.CastClassInt_Invalid_StackObjRef() cil managed
    {
        ldc.i4.0
        castclass CastingTestsType
        pop
        ret
    }

    .method public static void Casting.BoxInaccesibleType_Invalid_TypeAccess(class ContainingClass/PrivateNestedClass c) cil managed
    {
        ldarg.0
        box     ContainingClass/PrivateNestedClass
        pop
        ret
    }

    .method public static void Casting.BoxByRefInt_Invalid_BoxByRef.ExpectedValClassObjRefVariable(int32& i) cil managed
    {
        ldarg.0
        box     int32&
        pop
        ret
    }

    .method public static void Casting.BoxToUnsatisfiedConstraint_Invalid_UnsatisfiedBoxOperand(class GenericTypeWithConstrained<class [System.Runtime]System.Object> c) cil managed
    {
        ldarg.0
        box     class GenericTypeWithConstrained<class [System.Runtime]System.Object>
        pop
        ret
    }

    .method public static void Casting.BoxEnum_Valid(valuetype Enum e) cil managed
    {
        ldarg.0
        box     Enum
        pop
        ret
    }
}
