// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RuntimeLibrariesTest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using TypeOfRepo;
using System.Runtime.CompilerServices;


public class FieldLayoutTests
{
    public abstract class BaseType<T>
    {
        public T _field;
        public abstract void SetField(T value);
    }
    
    public class DerivedType<T> : BaseType<T>
    {
        public override void SetField(T value)
        {
            _field = value;
        }
    }

    public interface IIntegerable
    {
        int AsInt();
    }
    public struct StructTypeNotUsedAsANullable : IIntegerable
    {
        public int IntegerValue;
        public int AsInt() { return IntegerValue; }
    }

    public abstract class NonGenericBaseType
    {
        public abstract object BoxField();
        public abstract int IntegerifyField();
        public abstract void SetFieldFromObject(object obj);
    }

    public class DerivedTypeNullableThing<T> : NonGenericBaseType where T : struct, IIntegerable
    {
        public T? _field;

        public override int IntegerifyField() { return _field.Value.AsInt(); }
        public override object BoxField() { return _field; }
        public override void SetFieldFromObject(object obj) { _field = (T?)obj; }
    }

    [TestMethod]
    public static void TestFieldLayoutMatchesBetweenStaticAndDynamic_Long()
    {
        // In this test, the code used for DerivedType<long>.SetField will be universal canon, thus using the runtime
        // calculation of field layout, and the value will then be read by using the compiler definition of field layout
        // (in the use of bt._field). This allows us to see that variables of type long will be properly aligned on 
        // x86 platforms (this is a regression test)
        Type derivedType = typeof(DerivedType<>).MakeGenericType(TypeOf.Long);
        BaseType<long> bt = (BaseType<long>)Activator.CreateInstance(derivedType);

        long compareVal = 0x12345;
        bt.SetField(compareVal);
        Assert.AreEqual(compareVal, bt._field);
    }

    [TestMethod]
    public static void TestFieldLayoutMatchesBetweenStaticAndDynamic_Int64Enum()
    {
        // In this test, the code used for DerivedType<long>.SetField will be universal canon, thus using the runtime
        // calculation of field layout, and the value will then be read by using the compiler definition of field layout
        // (in the use of bt._field). This allows us to see that variables of type long will be properly aligned on 
        // x86 platforms (this is a regression test)
        Type derivedType = typeof(DerivedType<>).MakeGenericType(TypeOf.Int64Enum);
        BaseType<Int64Enum> bt = (BaseType<Int64Enum>)Activator.CreateInstance(derivedType);

        Int64Enum compareVal = (Int64Enum)0x12345;
        bt.SetField(compareVal);
        Assert.AreEqual(compareVal, bt._field);
    }

    [TestMethod]
    public static void TestBoxingUSGCreatedNullable()
    {
        StructTypeNotUsedAsANullable someStruct = new StructTypeNotUsedAsANullable();
        someStruct.IntegerValue = 0x12345678;
        
        Type derivedType = typeof(DerivedTypeNullableThing<>).MakeGenericType(TypeOf.FieldLayout_StructTypeNotUsedAsANullable);
        NonGenericBaseType baseType = (NonGenericBaseType)Activator.CreateInstance(derivedType);
        baseType.SetFieldFromObject(someStruct);
        Assert.AreEqual(someStruct.IntegerValue, baseType.IntegerifyField());
        StructTypeNotUsedAsANullable someStruct2 = (StructTypeNotUsedAsANullable)baseType.BoxField();
        Assert.AreEqual(someStruct.IntegerValue, someStruct2.IntegerValue);
    }
}
