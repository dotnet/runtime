// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;
using List_ForEach_Action;
using List_ListUtils;

namespace List_ForEach_Action
{
    public class Driver<T>
    {
        public bool Verify(T[] items)
        {
            bool retValue = true;

            retValue &= Test.Eval(VerifyVanilla(items), "Err_1828ahiudioe VerifyVanilla FAILD\n");
            retValue &= Test.Eval(VerifyExceptions(items), "Err_848ajode VerifyExceptions FAILD\n");

            return retValue;
        }

        public bool VerifyVanilla(T[] items)
        {
            bool retValue = true;
            List<T> list = new List<T>();
            List<T> visitedItems = new List<T>();
            Action<T> action = delegate (T item) { visitedItems.Add(item); };
            bool typeNullable = default(T) == null;

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify ForEach looks at every item
            visitedItems.Clear();
            list.ForEach(action);
            retValue &= Test.Eval(VerifyList(list, visitedItems),
                    "Err_282308ahid Verify ForEach looks at every item FAILED\n");

            return retValue;
        }

        public bool VerifyExceptions(T[] items)
        {
            bool retValue = true;
            List<T> list = new List<T>();

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify Null match
            retValue &= Test.Eval(VerifyException(list, null, typeof(ArgumentNullException)),
                "Err_858ahia Expected null match to throw ArgumentNullException");

            return retValue;
        }

        private bool VerifyException(List<T> list, Action<T> action, Type expectedExceptionType)
        {
            bool printException = false;
            bool retValue = true;

            try
            {
                list.ForEach(action);
                retValue &= Test.Eval(false, "Err_2987298ahid Expected {0} exception to be thrown", expectedExceptionType);
            }
            catch (Exception e)
            {
                retValue &= Test.Eval(e.GetType() == expectedExceptionType,
                    "Err_1282haid Expected {0} to be thrown and the following was thrown:\n{1}", expectedExceptionType, e);

                if (printException)
                    Console.WriteLine("INFO:\n" + e);
            }

            return retValue;
        }

        private bool VerifyList(List<T> list, List<T> expectedItems)
        {
            bool retValue = true;

            retValue &= Test.Eval(list.Count == expectedItems.Count, "Err_2828ahid Expected Count={0} actual={1}", expectedItems.Count, list.Count);

            // Do not continue if the expected and actual lengths differ
            if (!retValue)
                return false;

            //Only verify the indexer. List should be in a good enough state that we
            //do not have to verify consistancy with any other method.
            for (int i = 0; i < list.Count; ++i)
            {
                retValue &= Test.Eval(list[i] == null ? expectedItems[i] == null : list[i].Equals(expectedItems[i]),
                    "Err_19818ayiadb Expceted List[{0}]={1} actual={2}", i, expectedItems[i], list[i]);
            }

            return retValue;
        }

        public void CallForEach(T[] items, Action<T> action)
        {
            List<T> list = new List<T>();
            List<T> visitedItems = new List<T>();

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            list.ForEach(action);
        }
    }
}
public class TestCase
{
    [Fact]
    public static void RunTests()
    {
        Driver<int> intDriver = new Driver<int>();
        Driver<string> stringDriver = new Driver<string>();
        int[] intArray;
        string[] stringArray;
        int arraySize = 16;
        int sum = 0;


        intArray = new int[arraySize];
        stringArray = new string[arraySize];

        for (int i = 0; i < arraySize; ++i)
        {
            intArray[i] = i + 1;
            stringArray[i] = (i + 1).ToString();
        }

        sum = 0;
        Test.Eval(intDriver.Verify(new int[0]), "Err_2829ahdi Empty List<int> FAILED\n");
        Test.Eval(intDriver.Verify(new int[] { 1 }), "Err_8488ahoid List<int> with 1 item FAILED\n");
        Test.Eval(intDriver.Verify(intArray), "Err_8948ajod List<int> with more then 1 item FAILED\n");

        intDriver.CallForEach(intArray, delegate (int item) { sum += item; });
        Test.Eval(sum == ((arraySize * (arraySize + 1)) / 2), "Err_2790ahid List<int> Expected Sum to return:{0} atcual={1} FAILED\n", (arraySize * (arraySize + 1)) / 2, sum);

        sum = 0;
        Test.Eval(stringDriver.Verify(new string[0]), "Err_5088ahisa Empty List<string> FAILED\n");
        Test.Eval(stringDriver.Verify(new string[] { "1" }), "Err_3684ahieaz List<string> with 1 item FAILED\n");
        Test.Eval(stringDriver.Verify(stringArray), "Err_30250aiwg List<string> with more then 1 item FAILED\n");

        stringDriver.CallForEach(stringArray, delegate (string item) { sum += Convert.ToInt32(item); });
        Test.Eval(sum == ((arraySize * (arraySize + 1)) / 2), "Err_2790ahid List<string> Expected Sum to return:{0} atcual={1} FAILED\n", (arraySize * (arraySize + 1)) / 2, sum);

        Assert.True(Test.result);
    }
}
