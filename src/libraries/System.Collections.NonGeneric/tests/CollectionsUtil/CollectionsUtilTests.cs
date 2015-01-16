// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Specialized;
using Xunit;

public class CollectionsUtilTests
{
    private int _iCountErrors = 0;
    private int _iCountTestcases = 0;

    public virtual bool runTest()
    {
        try
        {
            /////////////////////////  START TESTS ////////////////////////////

            // [] CollectionsUtil.CreateCaseInsensitiveHashtable()
            _iCountTestcases++;
            Hashtable hashtable = CollectionsUtil.CreateCaseInsensitiveHashtable();
            Eval<int>(hashtable.Count, 0, "Err_001a!  Hashtable Count was wrong.");
            TryAdd(hashtable, "key1", "value1", true);
            TryAdd(hashtable, "Key1", "value2", false);
            Eval<int>(hashtable.Count, 1, "Err_001b!  Hashtable Count was wrong.");

            // [] CollectionsUtil.CreateCaseInsensitiveHashtable(int)
            _iCountTestcases++;
            hashtable = CollectionsUtil.CreateCaseInsensitiveHashtable(15);
            Eval<int>(hashtable.Count, 0, "Err_002a!  Hashtable Count was wrong.");
            TryAdd(hashtable, "key1", "value1", true);
            TryAdd(hashtable, "Key1", "value2", false);
            Eval<int>(hashtable.Count, 1, "Err_002b!  Hashtable Count was wrong.");

            // [] CollectionsUtil.CreateCaseInsensitiveHashtable(IDictionary)
            _iCountTestcases++;
            hashtable = CollectionsUtil.CreateCaseInsensitiveHashtable(hashtable);
            Eval<int>(hashtable.Count, 1, "Err_003a!  Hashtable Count was wrong.");
            TryAdd(hashtable, "key1", "value1", false);
            TryAdd(hashtable, "Key2", "value2", true);
            Eval<int>(hashtable.Count, 2, "Err_003b!  Hashtable Count was wrong.");

            // [] CollectionsUtil.CreateCaseInsensitiveSortedList()
            _iCountTestcases++;
            SortedList sortedList = CollectionsUtil.CreateCaseInsensitiveSortedList();
            Eval<int>(sortedList.Count, 0, "Err_004a!  SortedList Count was wrong.");
            TryAdd(sortedList, "key1", "value1", true);
            TryAdd(sortedList, "Key1", "value2", false);
            Eval<int>(sortedList.Count, 1, "Err_004b!  SortedList Count was wrong.");
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            _iCountErrors++;
            Console.WriteLine(": Error Err_general!  exc_general==\n" + exc_general.ToString());
        }

        ////  Finish Diagnostics
        if (_iCountErrors == 0)
        {
            return true;
        }
        else
        {
            Console.WriteLine("Fail!  iCountErrors==" + _iCountErrors);
            return false;
        }
    }

    bool Eval<T>(T actual, T expected, String errorMsg)
    {
        bool retValue = expected == null ? actual == null : expected.Equals(actual);
        if (!retValue)
        {
            Eval(retValue, errorMsg +
                 " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                 " Actual:" + (null == actual ? "<null>" : actual.ToString()));
        }
        return retValue;
    }

    bool Eval(bool expression, String msg)
    {
        if (!expression)
        {
            _iCountErrors++;
            Console.WriteLine(msg);
        }
        return expression;
    }

    void TryAdd(IDictionary dict, Object key, Object value, Boolean shouldSucceed)
    {
        try
        {
            dict.Add(key, value);
            if (!shouldSucceed)
            {
                _iCountErrors++;
                Console.WriteLine("Err_101!  Add succeeded when it should have failed!");
            }
        }
        catch (ArgumentException ex)
        {
            if (shouldSucceed)
            {
                _iCountErrors++;
                Console.WriteLine("Err_102!  Add failed when it should have succeeded!  ex={0}", ex);
            }
        }
        catch (Exception ex)
        {
            _iCountErrors++;
            Console.WriteLine("Err_103!  Add failed with unexpected exception!  ex={0}", ex);
        }
    }

    [Fact]
    public static void ExecuteCollectionsUtilTests()
    {
        bool bResult = false;
        var test = new CollectionsUtilTests();

        try
        {
            bResult = test.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("Fail! Error Err_main! Uncaught Exception in main(), exc_main==" + exc_main);
        }

        Assert.True(bResult);
    }
}
