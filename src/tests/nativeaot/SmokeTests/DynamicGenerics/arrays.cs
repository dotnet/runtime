// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
#if INTERNAL_CONTRACTS
using Internal.Runtime.Augments;
#endif
using RuntimeLibrariesTest;
using TypeOfRepo;

namespace ArrayTests
{
    public class SomeClassForArrayTests
    {
        public string m_Member;
        public SomeClassForArrayTests() { }
        public override string ToString() { return m_Member; }
    }


    public struct GenericStruct<T>
    {
        public GenericStruct(T t)
        { _val = t; }

        T _val;
        public override string ToString() { return _val.ToString(); }
    }

    public class GenericClass<T>
    {
        public GenericClass(T t)
        { _val = t; }
        T _val;
        public override string ToString() { return _val.ToString(); }
    }

    public abstract class SetArrayValBase
    {
        public abstract void SetVal(object array, object val, params int[] indices);
    }

    public class SetArrayVal<T> : SetArrayValBase
    {
        public override void SetVal(object array, object val, params int[] indices)
        {
            if (indices.Length == 2)
            {
                ((T[,])array)[indices[0], indices[1]] = (T)Activator.CreateInstance(typeof(T), val);
            }
            else if (indices.Length == 1)
            {
                ((T[])array)[indices[0]] = (T)Activator.CreateInstance(typeof(T), val);
            }
        }
    }

    public abstract class IndexOfValBase
    {
        public abstract int IndexOf(object array, object val);
        public abstract void FillArray(object array, object val);
    }

    public class IndexOfVal<T> : IndexOfValBase where T:struct
    {
        public override void FillArray(object array, object val)
        {
            T?[] tArray = (T?[])array;
            for (int i = 0; i < tArray.Length; i++)
                tArray[i] = (T)val;
        }

        public override int IndexOf(object array, object val)
        {
            return Array.IndexOf<T?>((T?[])array, (T)val);
        }
    }

    public struct GenStructImplementsIEquatable<T> : IEquatable<GenStructImplementsIEquatable<T>>
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public bool Equals(GenStructImplementsIEquatable<T> other) { return true; }
        public override bool Equals(object other) { return false; }
        public override int GetHashCode() { return 0; }
    }

    public struct GenStructImplementsIEquatable2<T> : IEquatable<GenStructImplementsIEquatable2<T>>
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public bool Equals(GenStructImplementsIEquatable2<T> other) { return true; }
        public override bool Equals(object other) { return false; }
        public override int GetHashCode() { return 0; }
    }

    public class ArrayTests
    {
        static void TestForEach(object[] array)
        {
            foreach(var item1 in array)
            {
                foreach (var item2 in (object[])item1)
                {
                    Console.WriteLine(item2);
                }
            }
        }

        static void AddItemsToArray(object o)
        {
            Array array = (Array)o;
            array.SetValue(new SomeClassForArrayTests[]
                { 
                    new SomeClassForArrayTests{ m_Member = "a1" },
                    new SomeClassForArrayTests{ m_Member = "a2" },
                    new SomeClassForArrayTests{ m_Member = "a3" },
                }, 0);
            array.SetValue(array.GetValue(0), 1);
            array.SetValue(new SomeClassForArrayTests[]
                { 
                    new SomeClassForArrayTests{ m_Member = "a4" },
                    new SomeClassForArrayTests{ m_Member = "a5" },
                }, 2);
        }
        
        public static void DynamicMDArrayTest<T>(Func<T,T> getValue)
        {
            T lastValue = default(T);

            T[,,] array = new T[1, 2, 3];
            for (int i = 0; i < 1; i++)
                for (int j = 0; j < 2; j++)
                    for (int k = 0; k < 3; k++)
                        array[i, j, k] = lastValue = getValue(lastValue);

            int index = 0;
            string[] expected = new string[] { "10", "20", "30", "40", "50", "60" };
            foreach (var item in (IEnumerable)array)
                Assert.AreEqual(expected[index++], item.ToString());
        }
        
        [TestMethod]
        public static void TestArrays()
        {
            TypeInfo objArray = TypeOf.Object.MakeArrayType().GetTypeInfo();
            Assert.AreEqual("System.Object[]", objArray.ToString());

            TypeInfo myArray = TypeOf.AT_ArrayTests.MakeArrayType().GetTypeInfo();
            Assert.AreEqual("ArrayTests.ArrayTests[]", myArray.ToString());

            TypeInfo listArray = TypeOf.List.MakeGenericType(TypeOf.AT_ArrayTests).MakeArrayType().GetTypeInfo();
            Assert.AreEqual("System.Collections.Generic.List`1[ArrayTests.ArrayTests][]", listArray.ToString());

            // Array types can have multiple EETypes representing them. 
            // Verify that at the end of the day, all of them are "equal" and castable to each other.
            // Same array types created twice using MakeArrayType() to test the caching logic in the runtime.
            Type t1 = TypeOf.AT_SomeClassForArrayTests.MakeArrayType().MakeArrayType();
            Type t2 = TypeOf.AT_SomeClassForArrayTests.MakeArrayType().MakeArrayType();
            Type t3 = TypeOf.AT_SomeClassForArrayTests2;
            Type t4 = (new SomeClassForArrayTests[6][]).GetType();
            Type t5 = TypeOf.AT_SomeClassForArrayTests1.MakeArrayType();
            Type t6 = TypeOf.AT_SomeClassForArrayTests1.MakeArrayType();
            Console.WriteLine("{0} == {1}? : {2}", t1, t2, t1 == t2);
            Console.WriteLine("{0} == {1}? : {2}", t2, t3, t2 == t3);
            Console.WriteLine("{0} == {1}? : {2}", t3, t4, t3 == t4);
            Console.WriteLine("{0} == {1}? : {2}", t4, t5, t4 == t5);
            Console.WriteLine("{0} == {1}? : {2}", t5, t6, t5 == t6);
            Assert.AreEqual(t1, t2);
            Assert.AreEqual(t2, t3);
            Assert.AreEqual(t3, t4);
            Assert.AreEqual(t4, t5);
            Assert.AreEqual(t5, t6);

            Assert.AreEqual(Activator.CreateInstance(t1, 3).GetType().ToString(), t1.ToString());
            Assert.AreEqual(Activator.CreateInstance(t2, 3).GetType().ToString(), t2.ToString());
            Assert.AreEqual(Activator.CreateInstance(t3, 3).GetType().ToString(), t3.ToString());
            Assert.AreEqual(Activator.CreateInstance(t4, 3).GetType().ToString(), t4.ToString());
            Assert.AreEqual(Activator.CreateInstance(t5, 3).GetType().ToString(), t5.ToString());
            Assert.AreEqual(Activator.CreateInstance(t6, 3).GetType().ToString(), t6.ToString());

            var array = Activator.CreateInstance(t2, 3);
            AddItemsToArray(array);

            TestForEach((object[])array);
        }

        [TestMethod]
        public static void TestDynamicArrays()
        {
#if UNIVERSAL_GENERICS
            var typeofGenStructOfString = typeof(GenericStruct<>).MakeGenericType(TypeOf.String);
            var typeofSetArrayValGenStructOfString = typeof(SetArrayVal<>).MakeGenericType(typeofGenStructOfString);
            var typeofGenStructOfShort = typeof(GenericStruct<>).MakeGenericType(TypeOf.Short);
            var typeofSetArrayValGenStructOfShort = typeof(SetArrayVal<>).MakeGenericType(typeofGenStructOfShort);
            var typeofGenClassOfString = typeof(GenericClass<>).MakeGenericType(TypeOf.String);
            var typeofSetArrayValGenClassOfString = typeof(SetArrayVal<>).MakeGenericType(typeofGenClassOfString);
            var typeofGenClassOfShort = typeof(GenericClass<>).MakeGenericType(TypeOf.Short);
            var typeofSetArrayValGenClassOfShort = typeof(SetArrayVal<>).MakeGenericType(typeofGenClassOfShort);

#if INTERNAL_CONTRACTS
            Assert.IsTrue(RuntimeAugments.IsDynamicType(typeofSetArrayValGenStructOfString.TypeHandle));
            Assert.IsTrue(RuntimeAugments.IsDynamicType(typeofSetArrayValGenStructOfShort.TypeHandle));
            Assert.IsTrue(RuntimeAugments.IsDynamicType(typeofSetArrayValGenClassOfString.TypeHandle));
            Assert.IsTrue(RuntimeAugments.IsDynamicType(typeofSetArrayValGenClassOfShort.TypeHandle));
#endif

            Array array_GS_String = Array.CreateInstance(typeofGenStructOfString, 1);
            var mdArrayRank2_GS_String = Array.CreateInstance(typeofGenStructOfString, 2, 3);
            SetArrayValBase setVal_GS_String = (SetArrayValBase)Activator.CreateInstance(typeofSetArrayValGenStructOfString);

            var array_GS_Short = Array.CreateInstance(typeofGenStructOfShort, 4);
            var mdArrayRank2_GS_Short = Array.CreateInstance(typeofGenStructOfShort, 5, 6);
            SetArrayValBase setVal_GS_Short = (SetArrayValBase)Activator.CreateInstance(typeofSetArrayValGenStructOfShort);

            var array_GC_String = Array.CreateInstance(typeofGenClassOfString, 7);
            var mdArrayRank2_GC_String = Array.CreateInstance(typeofGenClassOfString, 8, 9);
            SetArrayValBase setVal_GC_String = (SetArrayValBase)Activator.CreateInstance(typeofSetArrayValGenClassOfString);

            var array_GC_Short = Array.CreateInstance(typeofGenClassOfShort, 10);
            var mdArrayRank2_GC_Short = Array.CreateInstance(typeofGenClassOfShort, 11, 12);
            SetArrayValBase setVal_GC_Short = (SetArrayValBase)Activator.CreateInstance(typeofSetArrayValGenClassOfShort);

            setVal_GS_String.SetVal(array_GS_String, "TestStr", 0);
            setVal_GS_String.SetVal(mdArrayRank2_GS_String, "TestStr", 1, 2);
            Assert.AreEqual("TestStr", array_GS_String.GetValue(0).ToString());
            Assert.AreEqual("TestStr", mdArrayRank2_GS_String.GetValue(1,2).ToString());

            setVal_GS_Short.SetVal(array_GS_Short, (short)123, 0);
            setVal_GS_Short.SetVal(mdArrayRank2_GS_Short, (short)123, 1, 2);
            Assert.AreEqual("123", array_GS_Short.GetValue(0).ToString());
            Assert.AreEqual("123", mdArrayRank2_GS_Short.GetValue(1,2).ToString());

            setVal_GC_String.SetVal(array_GC_String, "TestStr", 0);
            setVal_GC_String.SetVal(mdArrayRank2_GC_String, "TestStr", 1, 2);
            Assert.AreEqual("TestStr", array_GC_String.GetValue(0).ToString());
            Assert.AreEqual("TestStr", mdArrayRank2_GC_String.GetValue(1,2).ToString());

            setVal_GC_Short.SetVal(array_GC_Short, (short)123, 0);
            setVal_GC_Short.SetVal(mdArrayRank2_GC_Short, (short)123, 1, 2);
            Assert.AreEqual("123", array_GC_Short.GetValue(0).ToString());
            Assert.AreEqual("123", mdArrayRank2_GC_Short.GetValue(1,2).ToString());
#endif
        } 

        [TestMethod]
        public static void TestMDArrays()
        {
#if UNIVERSAL_GENERICS
            int[,,] array = new int[1, 2, 3];
            int value = 1;
            for (int i = 0; i < 1; i++)
                for (int j = 0; j < 2; j++)
                    for (int k = 0; k < 3; k++)
                        array[i, j, k] = value++;

            int index = 0;
            string[] expected = new string[] { "1", "2", "3", "4", "5", "6" };
            foreach (var item in (IEnumerable)array)
                Assert.AreEqual(expected[index++], item.ToString());

            MethodInfo mi = typeof(ArrayTests).GetTypeInfo().GetDeclaredMethod("DynamicMDArrayTest").MakeGenericMethod(TypeOf.Short);
            mi.Invoke(null, new object[] { new Func<short, short>((val) => (short)(val + 10)) });
#endif
        }

        [TestMethod]
        public static void TestArrayIndexOfNullableStructOfCanon_USG()
        {
#if UNIVERSAL_GENERICS
            // Test USG Scenario
            var typeofGenStructOfString = typeof(GenStructImplementsIEquatable<>).MakeGenericType(TypeOf.String);
            var typeofIndexOfValGenStructOfString = typeof(IndexOfVal<>).MakeGenericType(typeofGenStructOfString);
#if INTERNAL_CONTRACTS
            Assert.IsTrue(RuntimeAugments.IsDynamicType(typeofGenStructOfString.TypeHandle));
#endif
            var typeofNullableGenStructOfString = typeof(Nullable<>).MakeGenericType(typeofGenStructOfString);

            Array array_GS_String = Array.CreateInstance(typeofNullableGenStructOfString, 1);
            IndexOfValBase indexOf_GS_String = (IndexOfValBase)Activator.CreateInstance(typeofIndexOfValGenStructOfString);
            indexOf_GS_String.FillArray(array_GS_String, Activator.CreateInstance(typeofGenStructOfString));
            Assert.AreEqual(0, indexOf_GS_String.IndexOf(array_GS_String, Activator.CreateInstance(typeofGenStructOfString)));
#endif
        }

        [TestMethod]
        public static void TestArrayIndexOfNullableStructOfCanon_Canon()
        {
            // Force canonical implementation Array.IndexOf<GenStructImplementsIEquatable2<__Canon>?>() to be generated and used
            GenStructImplementsIEquatable2<object>?[] arr = new GenStructImplementsIEquatable2<object>?[10];
            IndexOfValBase indexOfValCanonForcer = new IndexOfVal<GenStructImplementsIEquatable2<object>>();
            indexOfValCanonForcer.FillArray(arr, default(GenStructImplementsIEquatable2<object>));
            Console.WriteLine(indexOfValCanonForcer.IndexOf(arr, default(GenStructImplementsIEquatable2<object>)));

            // Just as in the above USG logic
            var typeofGenStructOfString = typeof(GenStructImplementsIEquatable2<>).MakeGenericType(TypeOf.String);
            var typeofIndexOfValGenStructOfString = typeof(IndexOfVal<>).MakeGenericType(typeofGenStructOfString);
#if INTERNAL_CONTRACTS
            Assert.IsTrue(RuntimeAugments.IsDynamicType(typeofGenStructOfString.TypeHandle));
#endif
            var typeofNullableGenStructOfString = typeof(Nullable<>).MakeGenericType(typeofGenStructOfString);

            Array array_GS_String = Array.CreateInstance(typeofNullableGenStructOfString, 1);
            IndexOfValBase indexOf_GS_String = (IndexOfValBase)Activator.CreateInstance(typeofIndexOfValGenStructOfString);
            indexOf_GS_String.FillArray(array_GS_String, Activator.CreateInstance(typeofGenStructOfString));
            Assert.AreEqual(0, indexOf_GS_String.IndexOf(array_GS_String, Activator.CreateInstance(typeofGenStructOfString)));
        }
    }
}
