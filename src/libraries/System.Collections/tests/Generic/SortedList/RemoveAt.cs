// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections;
using Xunit;
using SortedList_SortedListUtils;

public class Driver<K, V, R, S> where K : IPublicValue<R> where V : IPublicValue<S>
{
    public void BasicRemoveAt(K[] keys, V[] values)
    {
        SortedList<K, V> tbl = new SortedList<K, V>();
        try
        {
            tbl.RemoveAt(0);
            Test.Eval(false, "BasicRemoveAt0: Expected trying to reference an Index with no value to generate an ArgumentOutOfRangeException, but it did not.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }

        for (int i = 0; i < keys.Length; i++)
        {
            tbl.Add(keys[i], values[i]);
            try
            {
                tbl.RemoveAt(i + 1);
                Test.Eval(false, "BasicRemoveAt1: Expected trying to reference an Index with no value to generate an ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }
        Test.Eval(tbl.Count == keys.Length);

        for (int i = keys.Length - 1; i >= 0; i--)
        {
            tbl.RemoveAt(i);
            Test.Eval(!tbl.ContainsKey(keys[i]), "BasicRemoveAt3: Expected RemoveAt to remove items but found one item still in existance: " + keys[i].publicVal);
        }
        Test.Eval(tbl.Count == 0, "BasicRemoveAt2: Expected RemoveAt to clear the sorted Dictionary, but it did not and count is still " + tbl.Count);
    }

    public void RemoveAtNegative(K[] keys, V[] values, V[] missingvalues)
    {
        SortedList<K, V> tbl = new SortedList<K, V>();
        for (int i = 0; i < keys.Length; i++)
        {
            tbl.Add(keys[i], values[i]);
        }
        Test.Eval(tbl.Count == keys.Length);

        try
        {
            tbl.RemoveAt(keys.Length);
            Test.Eval(false, "RemoveAtNegative0: Expected trying to reference an Index with no value to generate an ArgumentOutOfRangeException, but it did not.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }

        try
        {
            tbl.RemoveAt(-1);
            Test.Eval(false, "RemoveAtNegative1: Expected trying to reference an Index with no value to generate an ArgumentOutOfRangeException, but it did not.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }

        for (int i = 0; i < missingvalues.Length; i++)
        {
            Test.Eval(tbl.IndexOfValue(missingvalues[i]) == -1, "RemoveAtNegative2: Expected IndexofValue to return -1, but it returned" + tbl.IndexOfValue(missingvalues[i]) + " for index of " + missingvalues[i].publicVal);
        }
    }

    public void AddRemoveKeyRemoveAt(K[] keys, V[] values, int index)
    {
        SortedList<K, V> tbl = new SortedList<K, V>();
        for (int i = 0; i < keys.Length; i++)
        {
            tbl.Add(keys[i], values[i]);
        }
        tbl.Remove(keys[index]);
        try
        {
            tbl.RemoveAt(keys.Length - 1);
            Test.Eval(false, "AddRemoveKeyRemoveAt: Expected trying to reference an Index that has been removed to throw an exception, but it did not.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }
}

public class RemoveAt
{
    [Fact]
    public static void RemoveAtMain()
    {
        Driver<RefX1<int>, ValX1<string>, int, string> IntDriver = new Driver<RefX1<int>, ValX1<string>, int, string>();
        RefX1<int>[] intArr1 = new RefX1<int>[100];
        for (int i = 0; i < 100; i++)
        {
            intArr1[i] = new RefX1<int>(i + 100);
        }

        int[] intArr2 = new int[15];
        for (int i = 0; i < 10; i++)
        {
            intArr2[i] = i + 200;
        }
        for (int i = 10; i < 15; i++)
        {
            intArr2[i] = i + 195;
        }

        Driver<ValX1<string>, RefX1<int>, string, int> StringDriver = new Driver<ValX1<string>, RefX1<int>, string, int>();
        ValX1<string>[] stringArr1 = new ValX1<string>[100];
        for (int i = 0; i < 100; i++)
        {
            stringArr1[i] = new ValX1<string>("SomeTestString" + ((i + 100).ToString()));
        }

        ValX1<string>[] stringArr2 = new ValX1<string>[15];
        for (int i = 0; i < 10; i++)
        {
            stringArr2[i] = new ValX1<string>("SomeTestString" + (i + 200).ToString());
        }

        //Ref<val>,Val<Ref> 		
        IntDriver.BasicRemoveAt(intArr1, stringArr1);
        IntDriver.RemoveAtNegative(intArr1, stringArr1, stringArr2);
        IntDriver.RemoveAtNegative(new RefX1<int>[0], new ValX1<string>[0], stringArr2);
        IntDriver.AddRemoveKeyRemoveAt(intArr1, stringArr1, 0);
        IntDriver.AddRemoveKeyRemoveAt(intArr1, stringArr1, 50);
        IntDriver.AddRemoveKeyRemoveAt(intArr1, stringArr1, 99);

        //Val<Ref>,Ref<Val> 		
        StringDriver.BasicRemoveAt(stringArr1, intArr1);
        StringDriver.AddRemoveKeyRemoveAt(stringArr1, intArr1, 0);
        StringDriver.AddRemoveKeyRemoveAt(stringArr1, intArr1, 50);
        StringDriver.AddRemoveKeyRemoveAt(stringArr1, intArr1, 99);

        intArr1 = new RefX1<int>[105];
        for (int i = 0; i < 105; i++)
        {
            intArr1[i] = new RefX1<int>(i);
        }

        RefX1<int>[] intArr3 = new RefX1<int>[15];
        for (int i = 0; i < 10; i++)
        {
            intArr3[i] = new RefX1<int>(i + 100);
        }
        for (int i = 10; i < 15; i++)
        {
            intArr3[i] = new RefX1<int>(101);
        }

        stringArr1 = new ValX1<string>[105];
        for (int i = 0; i < 100; i++)
        {
            stringArr1[i] = new ValX1<string>("SomeTestString" + i.ToString());
        }
        for (int i = 100; i < 105; i++)
        {
            stringArr1[i] = new ValX1<string>("SomeTestString11");
        }

        stringArr2 = new ValX1<string>[15];
        for (int i = 0; i < 15; i++)
        {
            stringArr2[i] = new ValX1<string>("SomeTestString" + (i + 100).ToString());
        }

        IntDriver.BasicRemoveAt(intArr1, stringArr1);
        StringDriver.BasicRemoveAt(stringArr2, intArr3);

        stringArr1 = new ValX1<string>[100];
        for (int i = 0; i < 100; i++)
        {
            stringArr1[i] = new ValX1<string>("SomeTestString" + ((i + 100).ToString()));
        }

        StringDriver.RemoveAtNegative(stringArr1, intArr1, intArr3);

        Assert.True(Test.result);
    }
}

