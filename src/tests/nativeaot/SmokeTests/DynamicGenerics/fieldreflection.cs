// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using RuntimeLibrariesTest;
using TypeOfRepo;


public class FieldReflectionTests
{
#if USC
    public struct Type1
    {
        public string _f1;
        public short _f2;
        public long _f3;
    }
    public struct Type2 
    {
        public float _f1;
        public double _f2;
        public byte _f3;
        public string _f4;
    }
    public struct SomeGenericType<T> { public T _myTField; }
#else
    public class Type1
    {
        public string _f1;
        public short _f2;
        public long _f3;
    }
    public class Type2 
    {
        public float _f1;
        public double _f2;
        public byte _f3;
        public string _f4;
    }
    public class SomeGenericType<T> { public T _myTField; }
#endif

    public class BaseType1
    {
        public float _f1;
        public string _f2;
    }
    public class BaseType2<T> : BaseType1
    {
        public T _f3;
        public double _f4;
        public byte _f5;
        public T _f6;
    }
    public class DerivedTypeWithVariousFields<T, U> : BaseType2<T>
    {
        public T InstanceFieldOfT;
        public SomeGenericType<U> InstanceFieldOfSomeGenericTypeU;
        public int InstanceIntField;
        public string InstanceStringField;
        public U InstanceFieldOfU;

        public object ReadInstance(int index)
        {
            switch (index)
            {
                case 0:
                    return InstanceFieldOfT;
                case 1:
                    return InstanceFieldOfSomeGenericTypeU;
                case 2:
                    return InstanceIntField;
                case 3:
                    return InstanceStringField;
                case 4:
                    return InstanceFieldOfU;
            }
            throw new InvalidOperationException();
        }
    }

    public class ReferenceTypeWithVariousFields<T, U>
    {
        public T InstanceFieldOfT;
        public SomeGenericType<U> InstanceFieldOfSomeGenericTypeU;
        public int InstanceIntField;
        public string InstanceStringField;
        public U InstanceFieldOfU;

        public object ReadInstance(int index)
        {
            switch (index)
            {
                case 0:
                    return InstanceFieldOfT;
                case 1:
                    return InstanceFieldOfSomeGenericTypeU;
                case 2:
                    return InstanceIntField;
                case 3:
                    return InstanceStringField;
                case 4:
                    return InstanceFieldOfU;
            }
            throw new InvalidOperationException();
        }

        public static T StaticFieldOfT;
        public static SomeGenericType<U> StaticFieldOfSomeGenericTypeU;
        public static int StaticIntField;
        public static string StaticStringField;
        public static U StaticFieldOfU;

        public static object ReadStatic(int index)
        {
            switch (index)
            {
                case 0:
                    return StaticFieldOfT;
                case 1:
                    return StaticFieldOfSomeGenericTypeU;
                case 2:
                    return StaticIntField;
                case 3:
                    return StaticStringField;
                case 4:
                    return StaticFieldOfU;
            }
            throw new InvalidOperationException();
        }
    }

    public struct ValueTypeWithVariousFields<T, U>
    {
        public T InstanceFieldOfT;
        public SomeGenericType<U> InstanceFieldOfSomeGenericTypeU;
        public int InstanceIntField;
        public string InstanceStringField;
        public U InstanceFieldOfU;

        public object ReadInstance(int index)
        {
            switch (index)
            {
                case 0:
                    return InstanceFieldOfT;
                case 1:
                    return InstanceFieldOfSomeGenericTypeU;
                case 2:
                    return InstanceIntField;
                case 3:
                    return InstanceStringField;
                case 4:
                    return InstanceFieldOfU;
            }
            throw new InvalidOperationException();
        }

        public static T StaticFieldOfT;
        public static SomeGenericType<U> StaticFieldOfSomeGenericTypeU;
        public static int StaticIntField;
        public static string StaticStringField;
        public static U StaticFieldOfU;

        public static object ReadStatic(int index)
        {
            switch (index)
            {
                case 0:
                    return StaticFieldOfT;
                case 1:
                    return StaticFieldOfSomeGenericTypeU;
                case 2:
                    return StaticIntField;
                case 3:
                    return StaticStringField;
                case 4:
                    return StaticFieldOfU;
            }
            throw new InvalidOperationException();
        }
    }

    public class ReferenceTypeWithCCtor<T>
    {
        public static int MaybeConvertedToPreinitBlob = 42;
        public static string InitializedFromCCtor;

        static ReferenceTypeWithCCtor()
        {
            InitializedFromCCtor = typeof(T).Name;
        }
    }

    public struct ValueTypeWithCCtor<T>
    {
        public static int MaybeConvertedToPreinitBlob = 42;
        public static string InitializedFromCCtor;

        static ValueTypeWithCCtor()
        {
            InitializedFromCCtor = typeof(T).Name;
        }
    }


    [TestMethod]
    public static void TestInstanceFieldsOnDerivedType()
    {
        Type1 srt1 = new Type1 { _f1 = "111", _f2 = 0x222, _f3 = 0x333 };
        Type2 srt2 = new Type2 { _f1 = 11.11f, _f2 = 22.22, _f3 = 0x33, _f4 = "444" };

        TestInstanceFieldsOnDerivedType_Inner<Type1, Type2>(srt1, srt2);
        TestInstanceFieldsOnDerivedType_Inner<CommonType1, CommonType2>(new CommonType1(), new CommonType2());
    }
    public static void TestInstanceFieldsOnDerivedType_Inner<T1, T2>(T1 srt1, T2 srt2)
    {
        Type t = TypeOf.FRT_DerivedTypeWithVariousFields.MakeGenericType(typeof(T1), typeof(T2));
        TypeInfo ti = t.GetTypeInfo();
        object o = Activator.CreateInstance(t);

        TypeInfo ti_base1 = typeof(BaseType1).GetTypeInfo();
        TypeInfo ti_base2 = TypeOf.FRT_BaseType2.MakeGenericType(typeof(T1)).GetTypeInfo();
        FieldInfo base1_f1 = ti_base1.GetDeclaredField("_f1");
        FieldInfo base1_f2 = ti_base1.GetDeclaredField("_f2");
        FieldInfo base2_f3 = ti_base2.GetDeclaredField("_f3");
        FieldInfo base2_f4 = ti_base2.GetDeclaredField("_f4");
        FieldInfo base2_f5 = ti_base2.GetDeclaredField("_f5");
        FieldInfo base2_f6 = ti_base2.GetDeclaredField("_f6");
        base1_f1.SetValue(o, 3.4f);
        base1_f2.SetValue(o, "555");
        base2_f3.SetValue(o, srt1);
        base2_f4.SetValue(o, 66.77);
        base2_f5.SetValue(o, (byte)0x99);
        base2_f6.SetValue(o, srt1);

        TestInstanceFields_Inner<T1, T2>(TypeOf.FRT_DerivedTypeWithVariousFields, srt1, srt2, ti, o);

        float f1 = (float)base1_f1.GetValue(o);
        string f2 = (string)base1_f2.GetValue(o);
        T1 f3 = (T1)base2_f3.GetValue(o);
        double f4 = (double)base2_f4.GetValue(o);
        byte f5 = (byte)base2_f5.GetValue(o);
        T1 f6 = (T1)base2_f6.GetValue(o);
        Assert.AreEqual(f1, 3.4f);
        Assert.AreEqual(f2, "555");
        Assert.AreEqual(f3, srt1);
        Assert.AreEqual(f4, 66.77);
        Assert.AreEqual(f5, (byte)0x99);
        Assert.AreEqual(f6, srt1);
    }

    [TestMethod]
    public static void TestInstanceFields()
    {
        Type1 srt1 = new Type1 { _f1 = "111", _f2 = 0x222, _f3 = 0x333 };
        Type2 srt2 = new Type2 { _f1 = 11.11f, _f2 = 22.22, _f3 = 0x33, _f4 = "444" };

        TestInstanceFields_Inner<Type1, Type2>(TypeOf.FRT_ReferenceTypeWithVariousFields, srt1, srt2);
        TestInstanceFields_Inner<CommonType1, CommonType2>(TypeOf.FRT_ReferenceTypeWithVariousFields, new CommonType1(), new CommonType2());

        TestInstanceFields_Inner<Type1, Type2>(TypeOf.FRT_ValueTypeWithVariousFields, srt1, srt2);
        TestInstanceFields_Inner<CommonType1, CommonType2>(TypeOf.FRT_ValueTypeWithVariousFields, new CommonType1(), new CommonType2());
    }
    public static void TestInstanceFields_Inner<T1, T2>(Type genTypeToUse, T1 srt1, T2 srt2)
    {
        Type t = genTypeToUse.MakeGenericType(typeof(T1), typeof(T2));
        TypeInfo ti = t.GetTypeInfo();
        object o = Activator.CreateInstance(t);

        TestInstanceFields_Inner<T1, T2>(genTypeToUse, srt1, srt2, ti, o);
    }
    public static void TestInstanceFields_Inner<T1, T2>(Type genTypeToUse, T1 srt1, T2 srt2, TypeInfo ti, object o)
    {
        Func<int, object> ReadValue = (Func<int, object>)ti.GetDeclaredMethod("ReadInstance").CreateDelegate(typeof(Func<int, object>), o);

        {
            FieldInfo fi_T = ti.GetDeclaredField("InstanceFieldOfT");
            fi_T.SetValue(o, srt1);
            Assert.AreEqual((T1)srt1, ReadValue(0));
            Assert.AreEqual((T1)srt1, fi_T.GetValue(o));

            FieldInfo fi_U = ti.GetDeclaredField("InstanceFieldOfU");
            fi_U.SetValue(o, srt2);
            Assert.AreEqual((T2)srt2, ReadValue(4));
            Assert.AreEqual((T2)srt2, fi_U.GetValue(o));
        }

        {
            FieldInfo fi = ti.GetDeclaredField("InstanceFieldOfSomeGenericTypeU");
            SomeGenericType<T2> srt = new SomeGenericType<T2> { _myTField = srt2 };
            fi.SetValue(o, srt);
            Assert.AreEqual(srt, ReadValue(1));
            Assert.AreEqual(srt, fi.GetValue(o));
        }

        {
            FieldInfo fi = ti.GetDeclaredField("InstanceIntField");
            fi.SetValue(o, 42);
            Assert.AreEqual(42, (int)ReadValue(2));
            Assert.AreEqual(42, (int)fi.GetValue(o));
        }

        {
            FieldInfo fi = ti.GetDeclaredField("InstanceStringField");
            fi.SetValue(o, "hello");
            Assert.AreEqual("hello", (string)ReadValue(3));
            Assert.AreEqual("hello", (string)fi.GetValue(o));
        }
    }

    [TestMethod]
    public static void TestStaticFields()
    {
        Type1 srt1 = new Type1 { _f1 = "111", _f2 = 0x222, _f3 = 0x333 };
        Type2 srt2 = new Type2 { _f1 = 11.11f, _f2 = 22.22, _f3 = 0x33, _f4 = "444" };

        TestStaticFields_Inner<Type1, Type2>(TypeOf.FRT_ReferenceTypeWithVariousFields, srt1, srt2);
        TestStaticFields_Inner<Type1, Type2>(TypeOf.FRT_ValueTypeWithVariousFields, srt1, srt2);

        TestStaticFields_Inner<CommonType1, CommonType2>(TypeOf.FRT_ReferenceTypeWithVariousFields, new CommonType1(), new CommonType2());
        TestStaticFields_Inner<CommonType1, CommonType2>(TypeOf.FRT_ValueTypeWithVariousFields, new CommonType1(), new CommonType2());
    }
    public static void TestStaticFields_Inner<T1, T2>(Type genTypeToUse, T1 srt1, T2 srt2)
    {
        {
            Type t = genTypeToUse.MakeGenericType(typeof(T1), typeof(T2));
            TypeInfo ti = t.GetTypeInfo();
            Func<int, object> ReadValue = (Func<int, object>)ti.GetDeclaredMethod("ReadStatic").CreateDelegate(typeof(Func<int, object>));

            {
                FieldInfo fi_T = ti.GetDeclaredField("StaticFieldOfT");
                fi_T.SetValue(null, srt1);
                Assert.AreEqual(srt1, ReadValue(0));
                Assert.AreEqual(srt1, fi_T.GetValue(null));

                FieldInfo fi_U = ti.GetDeclaredField("StaticFieldOfU");
                fi_U.SetValue(null, srt2);
                Assert.AreEqual(srt2, ReadValue(4));
                Assert.AreEqual(srt2, fi_U.GetValue(null));
            }

            {
                FieldInfo fi = ti.GetDeclaredField("StaticFieldOfSomeGenericTypeU");
                SomeGenericType<T2> srt = new SomeGenericType<T2> { _myTField = srt2 };
                fi.SetValue(null, srt);
                Assert.AreEqual(srt, ReadValue(1));
                Assert.AreEqual(srt, fi.GetValue(null));
            }

            {
                FieldInfo fi = ti.GetDeclaredField("StaticIntField");
                fi.SetValue(null, 42);
                Assert.AreEqual(42, (int)ReadValue(2));
                Assert.AreEqual(42, (int)fi.GetValue(null));
            }

            {
                FieldInfo fi = ti.GetDeclaredField("StaticStringField");
                fi.SetValue(null, "hello");
                Assert.AreEqual("hello", (string)ReadValue(3));
                Assert.AreEqual("hello", (string)fi.GetValue(null));
            }
        }
    }

    [TestMethod]
    public static void TestInitializedStaticFields()
    {
        // If this field was converted to a preinitialized static blob, additionally test
        // that the dynamically created type doesn't share the blob with the template
        ReferenceTypeWithCCtor<CommonType5>.MaybeConvertedToPreinitBlob = 0xBADF00D;
        ValueTypeWithCCtor<CommonType6>.MaybeConvertedToPreinitBlob = 0xBADF00D;

        Type ref_t = TypeOf.FRT_ReferenceTypeWithCCtor.MakeGenericType(TypeOf.CommonType1);
        Type val_t = TypeOf.FRT_ValueTypeWithCCtor.MakeGenericType(TypeOf.CommonType2);
        TypeInfo ref_ti = ref_t.GetTypeInfo();
        TypeInfo val_ti = val_t.GetTypeInfo();

        {
            FieldInfo fi = ref_ti.GetDeclaredField("MaybeConvertedToPreinitBlob");
            Assert.AreEqual(42, (int)fi.GetValue(null));

            fi = val_ti.GetDeclaredField("MaybeConvertedToPreinitBlob");
            Assert.AreEqual(42, (int)fi.GetValue(null));
        }

        {
            FieldInfo fi = ref_ti.GetDeclaredField("InitializedFromCCtor");
            Assert.AreEqual("CommonType1", (string)fi.GetValue(null));

            fi = val_ti.GetDeclaredField("InitializedFromCCtor");
            Assert.AreEqual("CommonType2", (string)fi.GetValue(null));
        }
    }

// Bug 253515 - FieldInfo.SetValue on some instantiations throws MissingRuntimeArtifactException
    public class MyGenericClass<T>
    {
        [ThreadStatic]
        public static T MyThreadStaticField;
        public static T MyGenericField;
        public static void SetField(T someParam)
        {
            MyGenericField = someParam;
        }
    }

    public class MyOtherGenericClass<T>
    {
        [ThreadStatic]
        public static T MyThreadStaticField;
        public static T MyGenericField;
        public static void SetField(T someParam)
        {
            MyGenericField = someParam;
        }
    }

    [TestMethod]
    public static void TestFieldSetValueOnInstantiationsThatAlreadyExistButAreNotKnownToReflection()
    {
#if UNIVERSAL_GENERICS
        // The int instantiation is visible to both nutc and analysis, MyGenericClass<int>.MyGenericField appears in RequiredGenericFields.
        // This works.
        MyGenericClass<int>.SetField(3);
        FieldInfo intField = typeof(MyGenericClass<int>).GetTypeInfo().GetDeclaredField("MyGenericField");
        intField.SetValue(null, 4);

        // The object instantiation is visible to both nutc and analysis, MyGenericClass<object>.MyGenericField appears in RequiredGenericFields.
        // This works.
        MyOtherGenericClass<object>.SetField(3);
        FieldInfo objectField = typeof(MyGenericClass<object>).GetTypeInfo().GetDeclaredField("MyGenericField");
        objectField.SetValue(null, 4);


        // The double instantiation isn't visible to either nutc or analysis. Confirmed that SetField uses USG code.
        // This works.
        Type obfuscatedDoubleType = TypeOf.Double;
        Type doubleInstantiation = typeof(MyGenericClass<>).MakeGenericType(obfuscatedDoubleType);
        MethodInfo doubleSetterMethod = doubleInstantiation.GetTypeInfo().GetDeclaredMethod("SetField");
        doubleSetterMethod.Invoke(null, new object[] { 1.0 });
        FieldInfo doubleField = doubleInstantiation.GetTypeInfo().GetDeclaredField("MyGenericField");
        doubleField.SetValue(null, 2.0);


        // The string instantiation isn't visible to either nutc or analysis. Confirmed that SetField uses USG (__UniversalCanon, not __Canon).
        Type obfuscatedStringType = TypeOf.String;
        Type stringInstantiation = typeof(MyGenericClass<>).MakeGenericType(obfuscatedStringType);
        MethodInfo stringSetterMethod = stringInstantiation.GetTypeInfo().GetDeclaredMethod("SetField");
        stringSetterMethod.Invoke(null, new object[] { "bar" });
        FieldInfo stringField = stringInstantiation.GetTypeInfo().GetDeclaredField("MyGenericField");
        stringField.SetValue(null, "foo");

        // The stringbuilder instantiation is visible to nutc, but analysis doesn't know it needs reflection. Even though the type has compiled code (__Canon shared generic),
        // the reflection method invoke calls into __UniversalCanon USG. The field invoke throws.
        MyGenericClass<StringBuilder>.SetField(new StringBuilder("baz")); // Uses __Canon implementation
        string obfuscatedSbName = "System.Text.StringBuildery";
        obfuscatedSbName = obfuscatedSbName.Remove(obfuscatedSbName.Length - 1);
        Type obfuscatedSbType = Type.GetType(obfuscatedSbName);
        Type sbInstantiation = typeof(MyGenericClass<>).MakeGenericType(obfuscatedSbType);
        MethodInfo sbSetterMethod = sbInstantiation.GetTypeInfo().GetDeclaredMethod("SetField");
        sbSetterMethod.Invoke(null, new object[] { new StringBuilder("bar") }); // Uses __UniversalCanon implementation
        FieldInfo uriField = sbInstantiation.GetTypeInfo().GetDeclaredField("MyGenericField");

        StringBuilder newStringBuilder = new StringBuilder("foo");
        uriField.SetValue(null, newStringBuilder); // Throws a MissingRuntimeArtifactException
        Assert.AreEqual(newStringBuilder, MyGenericClass<StringBuilder>.MyGenericField);

        // The float instantiation is visible to nutc, but analysis doesn't know it needs reflection. Even though the type has compiled code (specialized to float),
        // the reflection method invoke calls into __UniversalCanon USG. The field invoke throws.
        MyGenericClass<float>.SetField(1.0f); // Uses float implementation (even uses vector registers!)
        string obfuscatedFloatName = "System.Singley";
        obfuscatedFloatName = obfuscatedFloatName.Remove(obfuscatedFloatName.Length - 1);
        Type obfuscatedFloatType = Type.GetType(obfuscatedFloatName);
        Type floatInstantiation = typeof(MyGenericClass<>).MakeGenericType(obfuscatedFloatType);
        MethodInfo floatSetterMethod = floatInstantiation.GetTypeInfo().GetDeclaredMethod("SetField");
        floatSetterMethod.Invoke(null, new object[] { 3.0f }); // Uses __UniversalCanon implementation
        FieldInfo floatField = floatInstantiation.GetTypeInfo().GetDeclaredField("MyGenericField");

        floatField.SetValue(null, 2.0f); // Throws a MissingRuntimeArtifactException
        Assert.AreEqual(2.0f, MyGenericClass<float>.MyGenericField);

        FieldInfo threadStaticFloatField = floatInstantiation.GetTypeInfo().GetDeclaredField("MyThreadStaticField");

        threadStaticFloatField.SetValue(null, 6.0f); // Throws a MissingRuntimeArtifactException
        Assert.AreEqual(6.0f, MyGenericClass<float>.MyThreadStaticField);


        // The stringbuilder instantiation is visible to nutc, but analysis doesn't know it needs reflection.
        MyOtherGenericClass<StringBuilder>.SetField(new StringBuilder("baz")); // Uses __Canon implementation
        Type sbInstantiationOther = typeof(MyOtherGenericClass<>).MakeGenericType(obfuscatedSbType);
        MethodInfo sbOtherSetterMethod = sbInstantiationOther.GetTypeInfo().GetDeclaredMethod("SetField");
        sbOtherSetterMethod.Invoke(null, new object[] { new StringBuilder("bar") }); // Uses __Canon implementation
        FieldInfo otherField = sbInstantiationOther.GetTypeInfo().GetDeclaredField("MyGenericField");

        otherField.SetValue(null, newStringBuilder); // Throws a MissingRuntimeArtifactException
        Assert.AreEqual(newStringBuilder, MyOtherGenericClass<StringBuilder>.MyGenericField);

        FieldInfo threadStaticSbOtherField = sbInstantiationOther.GetTypeInfo().GetDeclaredField("MyThreadStaticField");

        threadStaticSbOtherField.SetValue(null, newStringBuilder); // Throws a MissingRuntimeArtifactException
        Assert.AreEqual(newStringBuilder, MyOtherGenericClass<StringBuilder>.MyThreadStaticField);
#endif
    }
}
