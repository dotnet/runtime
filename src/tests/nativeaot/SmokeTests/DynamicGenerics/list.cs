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


public class DynamicListTests
{
    public class NonGenericBase
    {
        public int IntValue { get; set; }
    }

    public struct StructWrapper<T> where T: NonGenericBase
    {
        public T Reference;
    }

    public struct StructWrapperWithEquals<T> where T : NonGenericBase
    {
        public T Reference;

        public override bool Equals(object obj)
        {
            return (obj is StructWrapperWithEquals<T>) && ((StructWrapperWithEquals<T>)obj).Reference.Equals(Reference);
        }
        public override int GetHashCode()
        {
            return Reference.GetHashCode();
        }
    }

    public class DummyForRdXml : NonGenericBase, IEquatable<DummyForRdXml>, IComparable<DummyForRdXml>
    {
        public bool Equals(DummyForRdXml other)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(DummyForRdXml other)
        {
            throw new NotImplementedException();
        }
    }

    public struct EquatableStructWrapper<T> : IEquatable<EquatableStructWrapper<T>> where T: IEquatable<T>
    {
        public T Reference;

        public bool Equals(EquatableStructWrapper<T> other)
        {
            return Reference.Equals(other.Reference);
        }
    }

    public struct ComparableStructWrapper<T> : IComparable<ComparableStructWrapper<T>> where T: IComparable<T>
    {
        public T Reference;

        public int CompareTo(ComparableStructWrapper<T> other)
        {
            return Reference.CompareTo(other.Reference);
        }
    }

    public class NonGenericElement1 : NonGenericBase { }
    public class NonGenericElement2 : NonGenericBase { }
    public class NonGenericElement3 : NonGenericBase { }
    public class NonGenericElement4 : NonGenericBase { }
    public class NonGenericElement5 : NonGenericBase { }
    public class NonGenericElement6 : NonGenericBase { }
    public class NonGenericElement7 : NonGenericBase { }
    public class NonGenericElement8 : NonGenericBase { }
    public class NonGenericElement9 : NonGenericBase { }
    public class NonGenericElement10 : NonGenericBase { }
    public class NonGenericElement11 : NonGenericBase { }
    public class NonGenericElement12 : NonGenericBase { }


    public class EquatableElement1 : NonGenericBase, IEquatable<EquatableElement1>
    {
        public bool Equals(EquatableElement1 other)
        {
            return IntValue == other.IntValue;
        }
    }

    public class ComparableElement1 : NonGenericBase, IComparable<ComparableElement1>
    {
        public int CompareTo(ComparableElement1 other)
        {
            return IntValue.CompareTo(other.IntValue);
        }
    }

    public class NonGenericElementWithEquals1 : NonGenericBase
    {
        public override bool Equals(object obj)
        {
            return (obj is NonGenericElementWithEquals1) && ((NonGenericElementWithEquals1)obj).IntValue == IntValue;
        }
        public override int GetHashCode()
        {
            return IntValue;
        }
    }

    public class NonGenericBaseComparer : IComparer<NonGenericBase>
    {
        public int Compare(NonGenericBase x, NonGenericBase y)
        {
            return x.IntValue.CompareTo(y.IntValue);
        }
    }

    public class NonGenericWrappedBaseComparer<T> : IComparer<StructWrapper<T>> where T : NonGenericBase
    {
        public int Compare(StructWrapper<T> x, StructWrapper<T> y)
        {
            return x.Reference.IntValue.CompareTo(y.Reference.IntValue);
        }
    }

    public static class Producer
    {
        public static IEnumerable<T> Produce<T>(int count) where T : NonGenericBase, new()
        {
            for (int i = 0; i < count; i++)
            {
                T element = new T();
                element.IntValue = i;
                yield return element;
            }
        }

        public static IEnumerable<StructWrapper<T>> WrapInStructWrapper<T>(IEnumerable<T> enumeration) where T: NonGenericBase
        {
            foreach (var e in enumeration)
                yield return new StructWrapper<T> { Reference = e };
        }

        public static IEnumerable<StructWrapperWithEquals<T>> WrapInStructWrapperWithEquals<T>(IEnumerable<T> enumeration) where T : NonGenericBase
        {
            foreach (var e in enumeration)
                yield return new StructWrapperWithEquals<T> { Reference = e };
        }

        public static IEnumerable<EquatableStructWrapper<T>> WrapInEquatableStructWrapper<T>(IEnumerable<T> enumeration) where T : NonGenericBase, IEquatable<T>
        {
            foreach (var e in enumeration)
                yield return new EquatableStructWrapper<T> { Reference = e };
        }

        public static IEnumerable<T> MakeEnumerable<T>(T item)
        {
            yield return item;
        }

        public static IEnumerable<T> MakeEnumerable<T>(T item1, T item2)
        {
            yield return item1;
            yield return item2;
        }

        public static IEnumerable<T> MakeEnumerable<T>(T item1, T item2, T item3)
        {
            yield return item1;
            yield return item2;
            yield return item3;
        }
    }

    public class ListAccessor<T>
    {
        public readonly Type ListType;
        public readonly TypeInfo ListTypeInfo;

        public ListAccessor()
        {
            ListType = TypeOf.List.MakeGenericType(typeof(T));
            ListTypeInfo = ListType.GetTypeInfo();
        }

        public object Construct()
        {
            return Activator.CreateInstance(ListType);
        }

        public object Construct(IEnumerable<T> elements)
        {
            return Activator.CreateInstance(ListType, new object[] { elements });
        }

        public object Construct(int capacity)
        {
            return Activator.CreateInstance(ListType, new object[] { capacity });
        }

        public int GetCapacity(object list)
        {
            return (int)ListTypeInfo.GetDeclaredProperty("Capacity").GetValue(list);
        }

        public void SetCapacity(object list, int value)
        {
            ListTypeInfo.GetDeclaredProperty("Capacity").SetValue(list, value);
        }

        public int GetCount(object list)
        {
            return (int)ListTypeInfo.GetDeclaredProperty("Count").GetValue(list);
        }

        public T GetItem(object list, int index)
        {
            return (T)ListTypeInfo.GetDeclaredProperty("Item").GetValue(list, new object[] { index });
        }

        public void CallAdd(object list, T item)
        {
            ListTypeInfo.GetDeclaredMethod("Add").Invoke(list, new object[] { item });
        }

        public bool CallRemove(object list, T item)
        {
            return (bool)ListTypeInfo.GetDeclaredMethod("Remove").Invoke(list, new object[] { item });
        }

        public int CallRemoveAll(object list, Predicate<T> match)
        {
            return (int)ListTypeInfo.GetDeclaredMethod("RemoveAll").Invoke(list, new object[] { match });
        }

        public void CallAddRange(object list, IEnumerable<T> collection)
        {
            ListTypeInfo.GetDeclaredMethod("AddRange").Invoke(list, new object[] { collection });
        }

        public object CallGetRange(object list, int index, int count)
        {
            // This method returns object on purpose so that it doesn't force a static reference to List<T>
            return ListTypeInfo.GetDeclaredMethod("GetRange").Invoke(list, new object[] { index, count });
        }

        public bool CallContains(object list, T item)
        {
            return (bool)ListTypeInfo.GetDeclaredMethod("Contains").Invoke(list, new object[] { item });
        }

        public void CallSort(object list, Comparison<T> comparison)
        {
            MethodInfo sortMethod = null;
            foreach (MethodInfo mi in ListTypeInfo.GetDeclaredMethods("Sort"))
            {
                ParameterInfo[] pars = mi.GetParameters();
                if (pars.Length == 1 && pars[0].ParameterType == typeof(Comparison<T>))
                {
                    sortMethod = mi;
                    break;
                }
            }
            sortMethod.Invoke(list, new object[] { comparison });
        }

        public void CallSort(object list, IComparer<T> comparer)
        {
            MethodInfo sortMethod = null;
            foreach (MethodInfo mi in ListTypeInfo.GetDeclaredMethods("Sort"))
            {
                ParameterInfo[] pars = mi.GetParameters();
                if (pars.Length == 1 && pars[0].ParameterType == typeof(IComparer<T>))
                {
                    sortMethod = mi;
                    break;
                }
            }
            sortMethod.Invoke(list, new object[] { comparer });
        }
    }

    public static class Driver<T> where T : new()
    {
        private static ListAccessor<T> accessor = new ListAccessor<T>();
        
        public static void TestGetRange()
        {
            object list = accessor.Construct();
            T item = new T();
            accessor.CallAdd(list, item);

            object range = accessor.CallGetRange(list, 0, 1);
            Assert.AreEqual(list.GetType(), range.GetType());
            Assert.AreEqual(1, accessor.GetCount(range));
            // BUGBUG Assert.IsTrue(Object.ReferenceEquals(item, accessor.GetItem(range, 0)));
            Assert.AreEqual(item, accessor.GetItem(range, 0));
        }

        public static void TestAddRange(IEnumerable<T> testData)
        {
            {
                object list = accessor.Construct();
                accessor.CallAddRange(list, testData);

                Assert.AreEqual(testData.Count(), accessor.GetCount(list));

                IEnumerator<T> srcItem = testData.GetEnumerator();
                for (int i = 0; i < accessor.GetCount(list); i++)
                {
                    srcItem.MoveNext();
                    Assert.AreEqual(srcItem.Current, accessor.GetItem(list, i));
                    Assert.IsTrue(accessor.GetItem(list, i) is T);
                }
            }

            // This goes through the ICollection implementation
            {
                object list = accessor.Construct();
                object source = accessor.Construct(testData);
                accessor.CallAddRange(list, (IEnumerable<T>)source);

                Assert.AreEqual(testData.Count(), accessor.GetCount(list));

                for (int i = 0; i < accessor.GetCount(list); i++)
                {
                    Assert.AreEqual(accessor.GetItem(source, i), accessor.GetItem(list, i));
                }
            }
        }

        public static void TestAddRemoveAndCapacity()
        {
            object list = accessor.Construct(1);
            Assert.AreEqual(1, accessor.GetCapacity(list));
            Assert.AreEqual(0, accessor.GetCount(list));

            T item1 = new T();
            T item2 = new T();
            
            accessor.CallAdd(list, item1);
            Assert.AreEqual(1, accessor.GetCount(list));

            accessor.CallAdd(list, item2);
            Assert.AreEqual(2, accessor.GetCount(list));

            accessor.CallAdd(list, item1);
            Assert.AreEqual(3, accessor.GetCount(list));

            accessor.CallAdd(list, item1);
            Assert.AreEqual(4, accessor.GetCount(list));

            if (!typeof(T).GetTypeInfo().IsValueType)
            {
                Assert.AreEqual(item1, accessor.GetItem(list, 0));
                Assert.AreEqual(item2, accessor.GetItem(list, 1));
                Assert.AreEqual(item1, accessor.GetItem(list, 2));
                Assert.AreEqual(item1, accessor.GetItem(list, 3));
                Assert.AreNotEqual(item2, accessor.GetItem(list, 0));

                bool removed = accessor.CallRemove(list, item1);
                Assert.IsTrue(removed);
                Assert.AreEqual(3, accessor.GetCount(list));
                Assert.AreEqual(item2, accessor.GetItem(list, 0));
                Assert.AreEqual(item1, accessor.GetItem(list, 1));
                Assert.AreEqual(item1, accessor.GetItem(list, 2));

                int numRemoved = accessor.CallRemoveAll(list, x => item1.GetHashCode() == x.GetHashCode() && Object.ReferenceEquals(item1, x));
                Assert.AreEqual(2, numRemoved);
                Assert.AreEqual(1, accessor.GetCount(list));
                Assert.AreEqual(item2, accessor.GetItem(list, 0));
            }

            accessor.SetCapacity(list, 500);
            Assert.AreEqual(500, accessor.GetCapacity(list));
        }

        public static void TestIListOfT()
        {
            IList<T> list = (IList<T>)accessor.Construct();
            Assert.AreEqual(0, list.Count);
            Assert.IsFalse(list.IsReadOnly);

            list.Add(new T());
            Assert.AreEqual(1, list.Count);

            foreach (T e in list)
                Assert.AreEqual(typeof(T), e.GetType());

            list.RemoveAt(0);
            Assert.AreEqual(0, list.Count);
        }

        public static void TestICollectionOfT()
        {
            ICollection<T> list = (ICollection<T>)accessor.Construct();
            Assert.AreEqual(0, list.Count);
            Assert.IsFalse(list.IsReadOnly);

            list.Add(new T());
            Assert.AreEqual(1, list.Count);

            foreach (T e in list)
                Assert.AreEqual(typeof(T), e.GetType());
        }

        public static void TestIList()
        {
            IList list = (IList)accessor.Construct();
            Assert.AreEqual(0, list.Count);
            Assert.IsFalse(list.IsReadOnly);

            list.Add(new T());
            Assert.AreEqual(1, list.Count);

            foreach (object e in list)
                Assert.AreEqual(typeof(T), e.GetType());

            list.RemoveAt(0);
            Assert.AreEqual(0, list.Count);
        }

        public static void TestICollection()
        {
            ICollection list = (ICollection)accessor.Construct();
            Assert.AreEqual(0, list.Count);
            Assert.IsFalse(list.IsSynchronized);

            T[] arr = new T[0];
            list.CopyTo(arr, 0);
        }

        public static void TestIReadOnlyListOfT()
        {
            IReadOnlyList<T> list = (IReadOnlyList<T>)accessor.Construct();
            Assert.AreEqual(0, list.Count);
        }

        public static void TestIReadOnlyCollectionOfT()
        {
            IReadOnlyCollection<T> list = (IReadOnlyCollection<T>)accessor.Construct();
            Assert.AreEqual(0, list.Count);
        }

        public static void TestToArray()
        {
            object list = accessor.Construct();
            MethodInfo toArrayMethod = accessor.ListTypeInfo.GetDeclaredMethod("ToArray");
            object array = toArrayMethod.Invoke(list, null);

            if (!typeof(T).GetTypeInfo().IsValueType)
            {
                IList<object> arrayAsIListOfObject = (IList<object>)array;
                Assert.AreEqual(0, arrayAsIListOfObject.Count);
            }

            // This is the empty array that is declared as a static in IList
            Type arrayType = typeof(T).MakeArrayType();
            Assert.AreEqual(arrayType, array.GetType());
            
            // Adding an element will make List allocate actual array
            T item = new T();
            accessor.CallAdd(list, item);
            array = toArrayMethod.Invoke(list, null);
            Assert.AreEqual(arrayType, array.GetType());

            if (!typeof(T).GetTypeInfo().IsValueType)
            {
                IList<object> arrayAsIListOfObject = (IList<object>)array;
                arrayAsIListOfObject = (IList<object>)array;
                Assert.AreEqual(1, arrayAsIListOfObject.Count);
                Assert.AreEqual(item, ((object[])array)[0]);
            }
        }

        public static void TestContains(IEnumerable<T> source, T elementToFind, bool expectedResult)
        {
            object list = accessor.Construct(source);
            Assert.AreEqual(expectedResult, accessor.CallContains(list, elementToFind));
        }

        public static void TestSortWithComparison(IEnumerable<T> enumToSort, Comparison<T> comparison, IEnumerable<T> expectedOrder)
        {
            object list = accessor.Construct(enumToSort);
            accessor.CallSort(list, comparison);

            object expectedList = accessor.Construct(expectedOrder);
            Assert.AreEqual(accessor.GetCount(expectedList), accessor.GetCount(list));

            for (int i = 0; i < accessor.GetCount(list); i++)
            {
                Assert.AreEqual(accessor.GetItem(expectedList, i), accessor.GetItem(list, i));
            }
        }

        public static void TestSortWithComparer(IEnumerable<T> enumToSort, IComparer<T> comparer, IEnumerable<T> expectedOrder)
        {
            object list = accessor.Construct(enumToSort);
            accessor.CallSort(list, comparer);

            object expectedList = accessor.Construct(expectedOrder);

            // Plot twist: construct it one more time exercising a path in constructor that uses ICollection<T>
            expectedList = accessor.Construct((IEnumerable<T>)expectedList);

            Assert.AreEqual(accessor.GetCount(expectedList), accessor.GetCount(list));

            for (int i = 0; i < accessor.GetCount(list); i++)
            {
                Assert.AreEqual(accessor.GetItem(expectedList, i), accessor.GetItem(list, i));
            }
        }
    }

    [TestMethod]
    public static void TestGetRange()
    {
        Driver<NonGenericElement1>.TestGetRange();
        Driver<StructWrapper<NonGenericElement1>>.TestGetRange();
    }

    [TestMethod]
    public static void TestAddRange()
    {
        Driver<NonGenericElementWithEquals1>.TestAddRange(Producer.Produce<NonGenericElementWithEquals1>(10));
        
        Driver<StructWrapperWithEquals<NonGenericElementWithEquals1>>.TestAddRange(
            Producer.WrapInStructWrapperWithEquals(Producer.Produce<NonGenericElementWithEquals1>(10)));
    }

    [TestMethod]
    public static void TestAddRemove()
    {
        Driver<NonGenericElement2>.TestAddRemoveAndCapacity();
        Driver<StructWrapper<NonGenericElement2>>.TestAddRemoveAndCapacity();
    }

    [TestMethod]
    public static void TestIListOfT()
    {
        Driver<NonGenericElement3>.TestIListOfT();
        Driver<StructWrapper<NonGenericElement3>>.TestIListOfT();
    }

    [TestMethod]
    public static void TestICollectionOfT()
    {
        Driver<NonGenericElement4>.TestICollectionOfT();
        Driver<StructWrapper<NonGenericElement4>>.TestICollectionOfT();
    }

    [TestMethod]
    public static void TestIList()
    {
        Driver<NonGenericElement5>.TestIList();
        Driver<StructWrapper<NonGenericElement5>>.TestIList();
    }

    [TestMethod]
    public static void TestICollection()
    {
        Driver<NonGenericElement6>.TestICollection();
        Driver<StructWrapper<NonGenericElement6>>.TestICollection();
    }

    [TestMethod]
    public static void TestIReadOnlyListOfT()
    {
        Driver<NonGenericElement7>.TestIReadOnlyListOfT();
        Driver<StructWrapper<NonGenericElement7>>.TestIReadOnlyListOfT();
    }

    [TestMethod]
    public static void TestIReadOnlyCollectionOfT()
    {
        Driver<NonGenericElement8>.TestIReadOnlyCollectionOfT();
        Driver<StructWrapper<NonGenericElement8>>.TestIReadOnlyCollectionOfT();
    }

    [TestMethod]
    public static void TestToArray()
    {
        Driver<NonGenericElement9>.TestToArray();
        Driver<StructWrapper<NonGenericElement9>>.TestToArray();
    }

    [TestMethod]
    public static void TestContains()
    {
        
        {
            // Should be compared with IEquatable
            Driver<EquatableElement1>.TestContains(Producer.Produce<EquatableElement1>(5), new EquatableElement1 { IntValue = 1 }, true);
            Driver<EquatableElement1>.TestContains(Producer.Produce<EquatableElement1>(5), new EquatableElement1 { IntValue = 5 }, false);
        
            // Should be compared by calling Equals(object other)
            NonGenericElement10 item = new NonGenericElement10();
            Driver<NonGenericElement10>.TestContains(Producer.MakeEnumerable(item), item, true);
            Driver<NonGenericElement10>.TestContains(Producer.MakeEnumerable(item), new NonGenericElement10(), false);
        }

        // Struct version of the above two
        {
            // Should be compared with IEquatable
            Driver<EquatableStructWrapper<EquatableElement1>>.TestContains(
                Producer.WrapInEquatableStructWrapper(Producer.Produce<EquatableElement1>(5)),
                new EquatableStructWrapper<EquatableElement1> { Reference = new EquatableElement1 { IntValue = 1 } },
                true);
            Driver<EquatableStructWrapper<EquatableElement1>>.TestContains(
                Producer.WrapInEquatableStructWrapper(Producer.Produce<EquatableElement1>(5)),
                new EquatableStructWrapper<EquatableElement1> { Reference = new EquatableElement1 { IntValue = 5 } },
                false);

            // Should be compared by calling Equals(object other)
            StructWrapper<NonGenericElement10> item = new StructWrapper<NonGenericElement10> { Reference = new NonGenericElement10() };
            Driver<StructWrapper<NonGenericElement10>>.TestContains(Producer.MakeEnumerable(item), item, true);
            Driver<StructWrapper<NonGenericElement10>>.TestContains(Producer.MakeEnumerable(item), new StructWrapper<NonGenericElement10> { Reference = new NonGenericElement10() }, false);
        }
    }

    [TestMethod]
    public static void TestSortWithComparison()
    {
        {
            NonGenericElement11 item1 = new NonGenericElement11 { IntValue = 2 };
            NonGenericElement11 item2 = new NonGenericElement11 { IntValue = 3 };
            NonGenericElement11 item3 = new NonGenericElement11 { IntValue = 1 };

            Driver<NonGenericElement11>.TestSortWithComparison(
                Producer.MakeEnumerable(item1, item2, item3),
                (x, y) => x.IntValue.CompareTo(y.IntValue),
                Producer.MakeEnumerable(item3, item1, item2));
        }

        {
            StructWrapper<NonGenericElement11> item1 = new StructWrapper<NonGenericElement11> { Reference = new NonGenericElement11 { IntValue = 2 } };
            StructWrapper<NonGenericElement11> item2 = new StructWrapper<NonGenericElement11> { Reference = new NonGenericElement11 { IntValue = 3 } };
            StructWrapper<NonGenericElement11> item3 = new StructWrapper<NonGenericElement11> { Reference = new NonGenericElement11 { IntValue = 1 } };

            Driver<StructWrapper<NonGenericElement11>>.TestSortWithComparison(
                Producer.MakeEnumerable(item1, item2, item3),
                (x, y) => x.Reference.IntValue.CompareTo(y.Reference.IntValue),
                Producer.MakeEnumerable(item3, item1, item2));
        }
    }

    [TestMethod]
    public static void TestSortWithComparer()
    {
        // Provide a comparer
        {
            NonGenericElement12 item1 = new NonGenericElement12 { IntValue = 2 };
            NonGenericElement12 item2 = new NonGenericElement12 { IntValue = 3 };
            NonGenericElement12 item3 = new NonGenericElement12 { IntValue = 1 };

            Driver<NonGenericElement12>.TestSortWithComparer(
                Producer.MakeEnumerable(item1, item2, item3),
                new NonGenericBaseComparer(),
                Producer.MakeEnumerable(item3, item1, item2));
        }

        // Rely on IComparable<T>
        {
            ComparableElement1 item1 = new ComparableElement1 { IntValue = 2 };
            ComparableElement1 item2 = new ComparableElement1 { IntValue = 3 };
            ComparableElement1 item3 = new ComparableElement1 { IntValue = 1 };

            Driver<ComparableElement1>.TestSortWithComparer(
                Producer.MakeEnumerable(item1, item2, item3),
                null,
                Producer.MakeEnumerable(item3, item1, item2));
        }

        // Provide a comparer
        {
            StructWrapper<NonGenericElement12> item1 = new StructWrapper<NonGenericElement12> { Reference = new NonGenericElement12 { IntValue = 2 } };
            StructWrapper<NonGenericElement12> item2 = new StructWrapper<NonGenericElement12> { Reference = new NonGenericElement12 { IntValue = 3 } };
            StructWrapper<NonGenericElement12> item3 = new StructWrapper<NonGenericElement12> { Reference = new NonGenericElement12 { IntValue = 1 } };

            Driver<StructWrapper<NonGenericElement12>>.TestSortWithComparer(
                Producer.MakeEnumerable(item1, item2, item3),
                new NonGenericWrappedBaseComparer<NonGenericElement12>(),
                Producer.MakeEnumerable(item3, item1, item2));
        }

        // Rely on IComparable<T>
        {
            ComparableStructWrapper<ComparableElement1> item1 = new ComparableStructWrapper<ComparableElement1> { Reference = new ComparableElement1 { IntValue = 2 } };
            ComparableStructWrapper<ComparableElement1> item2 = new ComparableStructWrapper<ComparableElement1> { Reference = new ComparableElement1 { IntValue = 3 } };
            ComparableStructWrapper<ComparableElement1> item3 = new ComparableStructWrapper<ComparableElement1> { Reference = new ComparableElement1 { IntValue = 1 } };

            Driver<ComparableStructWrapper<ComparableElement1>>.TestSortWithComparer(
                Producer.MakeEnumerable(item1, item2, item3),
                null,
                Producer.MakeEnumerable(item3, item1, item2));
        }
    }
}
