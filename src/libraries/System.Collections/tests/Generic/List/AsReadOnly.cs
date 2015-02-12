// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using TestSupport.Collections;
using TestSupport;
using Xunit;
using TestSupport.Common_TestSupport;
using List_ListUtils;
using List_AsReadOnly;

public class VerifyReadOnlyIList<T>
{
    public static bool Verify(IList<T> list, T[] items, GenerateItem<T> generateItem)
    {
        bool retValue = true;

        retValue &= VerifyAdd(list, items);
        retValue &= VerifyClear(list, items);
        retValue &= VerifyInsert(list, items);
        retValue &= VerifyRemove(list, items);
        retValue &= VerifyRemoveAt(list, items);
        retValue &= VerifyItem_Set(list, items);
        retValue &= VerifyIsReadOnly(list, items);
        retValue &= VerifyIndexOf(list, items);
        retValue &= VerifyContains(list, items);
        retValue &= VerifyItem_Get(list, items);
        retValue &= VerifyIList(list, items, generateItem);

        return retValue;
    }

    public static bool VerifyAdd(IList<T> list, T[] items)
    {
        bool retValue = true;
        int origCount = list.Count;

        //[]Try adding an item to the colleciton and verify Add throws NotSupportedException
        try
        {
            list.Add(default(T));
            retValue &= List_ListUtils.Test.Eval(false, "Err_27027ahbz!!! Not supported Exception should have been thrown when calling Add on a readonly collection");
        }
        catch (NotSupportedException)
        {
            retValue &= List_ListUtils.Test.Eval(origCount == list.Count,
                String.Format("Err_7072habpo!!! Expected Count={0} actual={1} after calling Add on a readonly collection", origCount, list.Count));
        }

        return retValue;
    }

    public static bool VerifyInsert(IList<T> list, T[] items)
    {
        bool retValue = true;
        int origCount = list.Count;

        //[]Verify Insert throws NotSupportedException
        try
        {
            list.Insert(0, default(T));
            retValue &= List_ListUtils.Test.Eval(false, "Err_558449ahpba!!! Not supported Exception should have been thrown when calling Insert on a readonly collection");
        }
        catch (NotSupportedException)
        {
            retValue &= List_ListUtils.Test.Eval(origCount == list.Count,
                String.Format("Err_21199apba!!! Expected Count={0} actual={1} after calling Insert on a readonly collection", origCount, list.Count));
        }

        return retValue;
    }

    public static bool VerifyClear(IList<T> list, T[] items)
    {
        bool retValue = true;
        int origCount = list.Count;

        //[]Verify Clear throws NotSupportedException
        try
        {
            list.Clear();
            retValue &= List_ListUtils.Test.Eval(false, "Err_7027qhpa!!! Not supported Exception should have been thrown when calling Clear on a readonly collection");
        }
        catch (NotSupportedException)
        {
            retValue &= List_ListUtils.Test.Eval(origCount == list.Count,
                String.Format("Err_727aahpb!!! Expected Count={0} actual={1} after calling Clear on a readonly collection", origCount, list.Count));
        }

        return retValue;
    }

    public static bool VerifyRemove(IList<T> list, T[] items)
    {
        bool retValue = true;
        int origCount = list.Count;

        //[]Verify Remove throws NotSupportedException
        try
        {
            if (null != items && items.Length != 0)
            {
                list.Remove(items[0]);
            }
            else
            {
                list.Remove(default(T));
            }

            retValue &= List_ListUtils.Test.Eval(false, "Err_8207aahpb!!! Not supported Exception should have been thrown when calling Remove on a readonly collection");
        }
        catch (NotSupportedException)
        {
            retValue &= List_ListUtils.Test.Eval(origCount == list.Count,
                String.Format("Err_7082hapba!!! Expected Count={0} actual={1} after calling Remove on a readonly collection", origCount, list.Count));
        }

        return retValue;
    }

    public static bool VerifyRemoveAt(IList<T> list, T[] items)
    {
        bool retValue = true;
        int origCount = list.Count;

        //[]Verify RemoveAt throws NotSupportedException
        try
        {
            list.RemoveAt(0);
            retValue &= List_ListUtils.Test.Eval(false, "Err_77894ahpba!!! Not supported Exception should have been thrown when calling RemoveAt on a readonly collection");
        }
        catch (NotSupportedException)
        {
            retValue &= List_ListUtils.Test.Eval(origCount == list.Count,
                String.Format("Err_111649ahpba!!! Expected Count={0} actual={1} after calling RemoveAt on a readonly collection", origCount, list.Count));
        }

        return retValue;
    }

    public static bool VerifyItem_Set(IList<T> list, T[] items)
    {
        bool retValue = true;
        int origCount = list.Count;

        //[]Verify Item_Set throws NotSupportedException
        try
        {
            list[0] = default(T);
            retValue &= List_ListUtils.Test.Eval(false, "Err_77894ahpba!!! Not supported Exception should have been thrown when calling Item_Set on a readonly collection");
        }
        catch (NotSupportedException) { }

        return retValue;
    }

    public static bool VerifyIsReadOnly(IList<T> list, T[] items)
    {
        return List_ListUtils.Test.Eval(list.IsReadOnly, "Err_44894phkni!!! Expected IsReadOnly to be false");
    }

    public static bool VerifyIndexOf(IList<T> list, T[] items)
    {
        int index;
        bool retValue = true;

        for (int i = 0; i < items.Length; ++i)
        {
            retValue &= List_ListUtils.Test.Eval(i == (index = list.IndexOf(items[i])) || items[i].Equals(items[index]),
                String.Format("Err_331697ahpba Expect IndexOf to return an index to item equal to={0} actual={1} IndexReturned={2} items Index={3}",
                    items[i], items[index], index, i));
        }

        return retValue;
    }

    public static bool VerifyContains(IList<T> list, T[] items)
    {
        bool retValue = true;

        for (int i = 0; i < items.Length; ++i)
        {
            retValue &= List_ListUtils.Test.Eval(list.Contains(items[i]),
                String.Format("Err_1568ahpa Expected Contains to return true with item={0} items index={1}",
                    items[i], i));
        }

        return retValue;
    }

    public static bool VerifyItem_Get(IList<T> list, T[] items)
    {
        bool retValue = true;

        for (int i = 0; i < items.Length; ++i)
        {
            retValue &= List_ListUtils.Test.Eval(items[i].Equals(list[i]),
                String.Format("Err_70717ahbpa Expected list[{0}]={1} actual={2}",
                    i, items[i], list[i]));
        }

        return retValue;
    }

    public static bool VerifyIList(IList<T> list, T[] items, GenerateItem<T> generateItem)
    {
        IList_T_Test<T> genericICollectionTest = new IList_T_Test<T>(list, generateItem, items, true, true);

        return genericICollectionTest.RunAllTests();
    }
}

namespace List_AsReadOnly
{
    public class Driver<T>
    {
        public void CheckType()
        {
            // VSWhidbey #378658
            List<T> list = new List<T>();
            ReadOnlyCollection<T> readOnlyList = list.AsReadOnly();
            List_ListUtils.Test.Eval(readOnlyList.GetType() == typeof(ReadOnlyCollection<T>), "Err_1703r38abhpx Read Only Collection Type Test FAILED");
        }

        public void EmptyCollection(GenerateItem<T> generateItem)
        {
            List<T> list = new List<T>();

            List_ListUtils.Test.Eval(VerifyReadOnlyIList<T>.Verify(list.AsReadOnly(), new T[0], generateItem), "Err_170718abhpx Empty Collection Test FAILED");
        }

        public void NonEmptyCollectionIEnumerableCtor(T[] items, GenerateItem<T> generateItem)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            List_ListUtils.Test.Eval(VerifyReadOnlyIList<T>.Verify(list.AsReadOnly(), items, generateItem), "Err_884964ahbz NON Empty Collection using the IEnumerable constructor to populate the list Test FAILED");
        }

        public void NonEmptyCollectionAdd(T[] items, GenerateItem<T> generateItem)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < items.Length; ++i)
            {
                list.Add(items[i]);
            }

            List_ListUtils.Test.Eval(VerifyReadOnlyIList<T>.Verify(list.AsReadOnly(), items, generateItem), "Err_58941ahpas NON Empty Collection Test using Add to populate the list FAILED");
        }

        public void AddRemoveSome(T[] items, T[] itemsToAdd, T[] itemsToRemove, GenerateItem<T> generateItem)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < itemsToAdd.Length; ++i)
            {
                list.Add(itemsToAdd[i]);
            }

            for (int i = 0; i < itemsToRemove.Length; ++i)
            {
                list.Remove(itemsToRemove[i]);
            }

            List_ListUtils.Test.Eval(VerifyReadOnlyIList<T>.Verify(list.AsReadOnly(), items, generateItem), "Err_70712bas Add then Remove some of the items Test FAILED");
        }

        public void AddRemoveAll(T[] items, GenerateItem<T> generateItem)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < items.Length; ++i)
            {
                list.Add(items[i]);
            }

            for (int i = 0; i < items.Length; ++i)
            {
                list.RemoveAt(0);
            }

            List_ListUtils.Test.Eval(VerifyReadOnlyIList<T>.Verify(list.AsReadOnly(), new T[0], generateItem), "Err_56498ahpba Add then Remove all of the items Test FAILED");
        }

        public void AddClear(T[] items, GenerateItem<T> generateItem)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < items.Length; ++i)
            {
                list.Add(items[i]);
            }

            list.Clear();

            List_ListUtils.Test.Eval(VerifyReadOnlyIList<T>.Verify(list.AsReadOnly(), new T[0], generateItem), "Err_46598ahpas Add then Clear Test FAILED");
        }
    }
}
public class RefX1IntGenerator
{
    private int _index;

    public RefX1IntGenerator()
    {
        _index = 1;
    }

    public RefX1<int> NextValue()
    {
        return new RefX1<int>(_index++);
    }

    public Object NextValueObject()
    {
        return (Object)NextValue();
    }
}

public class ValX1StringGenerator
{
    private int _index;

    public ValX1StringGenerator()
    {
        _index = 1;
    }

    public ValX1<String> NextValue()
    {
        return new ValX1<string>((_index++).ToString());
    }

    public Object NextValueObject()
    {
        return (Object)NextValue();
    }
}

public class AsReadOnly
{
    [Fact]
    public static void RunTests()
    {
        Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
        RefX1<int>[] intArr = new RefX1<int>[100];
        RefX1<int>[] intArrToRemove = new RefX1<int>[50];
        RefX1<int>[] intArrAfterRemove = new RefX1<int>[50];
        RefX1IntGenerator refX1IntGenerator = new RefX1IntGenerator();

        for (int i = 0; i < 100; i++)
        {
            intArr[i] = refX1IntGenerator.NextValue();

            if ((i & 1) != 0)
            {
                intArrToRemove[i / 2] = intArr[i];
            }
            else
            {
                intArrAfterRemove[i / 2] = intArr[i];
            }
        }

        IntDriver.CheckType();
        IntDriver.EmptyCollection(refX1IntGenerator.NextValue);
        IntDriver.NonEmptyCollectionIEnumerableCtor(intArr, refX1IntGenerator.NextValue);
        IntDriver.NonEmptyCollectionAdd(intArr, refX1IntGenerator.NextValue);
        IntDriver.AddRemoveSome(intArrAfterRemove, intArr, intArrToRemove, refX1IntGenerator.NextValue);
        IntDriver.AddRemoveAll(intArr, refX1IntGenerator.NextValue);
        IntDriver.AddClear(intArr, refX1IntGenerator.NextValue);


        Driver<ValX1<String>> StringDriver = new Driver<ValX1<String>>();
        ValX1<String>[] StringArr = new ValX1<String>[100];
        ValX1<String>[] StringArrToRemove = new ValX1<String>[50];
        ValX1<String>[] StringArrAfterRemove = new ValX1<String>[50];
        ValX1StringGenerator valX1StringGenerator = new ValX1StringGenerator();

        for (int i = 0; i < 100; i++)
        {
            StringArr[i] = valX1StringGenerator.NextValue();

            if ((i & 1) != 0)
            {
                StringArrToRemove[i / 2] = StringArr[i];
            }
            else
            {
                StringArrAfterRemove[i / 2] = StringArr[i];
            }
        }

        StringDriver.CheckType();
        StringDriver.EmptyCollection(valX1StringGenerator.NextValue);
        StringDriver.NonEmptyCollectionIEnumerableCtor(StringArr, valX1StringGenerator.NextValue);
        StringDriver.NonEmptyCollectionAdd(StringArr, valX1StringGenerator.NextValue);
        StringDriver.AddRemoveSome(StringArrAfterRemove, StringArr, StringArrToRemove, valX1StringGenerator.NextValue);
        StringDriver.AddRemoveAll(StringArr, valX1StringGenerator.NextValue);
        StringDriver.AddClear(StringArr, valX1StringGenerator.NextValue);

        Assert.True(List_ListUtils.Test.result);
    }
}
