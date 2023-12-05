// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Xunit;

public class Driver<K, V>
    where K : class
    where V : class
{
    public void BasicAdd(K[] keys, V[] values)
    {
        ConditionalWeakTable<K,V> tbl = new ConditionalWeakTable<K,V>();

        for (int i = 0; i < keys.Length; i++)
        {
            tbl.Add(keys[i], values[i]);
        }

        for (int i = 0; i < keys.Length; i++)
        {
            V val;

            // make sure TryGetValues return true, since the key should be in the table
            Test.Eval(tbl.TryGetValue(keys[i], out val), "Err_001 Expected TryGetValue to return true");

            if ( val == null && values[i] == null )
            {
                Test.Eval(true);
            }
            else if (val != null && values[i] != null && val.Equals(values[i]))
            {
                Test.Eval(true);
            }
            else
            {
                // only one of the values is null or the values don't match
                Test.Eval(false, "Err_002 The value returned by TryGetValue doesn't match the expected value");
            }
        }
    }

    public void AddSameKey //Same Value - Different Value should not matter
        (K[] keys, V[] values, int index, int repeat)
    {
        ConditionalWeakTable<K,V> tbl = new ConditionalWeakTable<K,V>();

        for (int i = 0; i < keys.Length; i++)
        {
            tbl.Add(keys[i], values[i]);
        }

        for (int i = 0; i < repeat; i++)
        {
            try
            {
                tbl.Add(keys[index], values[index]);
                Test.Eval(false, "Err_003 Expected to get ArgumentException when invoking Add() on an already existing key");
            }
            catch (ArgumentException)
            {
                Test.Eval(true);
            }
        }
    }

    public void AddValidations(K[] keys, V[] values, V value)
    {
        ConditionalWeakTable<K,V> tbl = new ConditionalWeakTable<K,V>();

        for (int i = 0; i < keys.Length; i++)
        {
            tbl.Add(keys[i], values[i]);
        }

        //try to add null key
        try
        {
            tbl.Add(null, value);
            Test.Eval(false, "Err_004 Expected to get ArgumentNullException when invoking Add() on a null key");
        }
        catch (ArgumentNullException)
        {
            Test.Eval(true);
        }
    }

    public void RemoveValidations(K[] keys, V[] values, K key, V value)
    {
        ConditionalWeakTable<K,V> tbl = new ConditionalWeakTable<K,V>();

        // Try to remove key from an empty dictionary
        // Remove should return false
        Test.Eval(!tbl.Remove(keys[0]), "Err_005 Expected Remove to return false");


        for (int i = 0; i < keys.Length; i++)
        {
            tbl.Add(keys[i], values[i]);
        }

        //try to remove null key
        try
        {
            tbl.Remove(null);
            Test.Eval(false, "Err_006 Expected to get ArgumentNullException when invoking Remove() on a null key");
        }
        catch (ArgumentNullException)
        {
            Test.Eval(true);
        }

        // Remove non existing key
        // Remove should return false
        Test.Eval(!tbl.Remove(key), "Err_007 Expected Remove to return false");
    }

    public void TryGetValueValidations(K[] keys, V[] values, K key, V value)
    {
        ConditionalWeakTable<K,V> tbl = new ConditionalWeakTable<K,V>();

        V val;

        // Try to get key from an empty dictionary
        // TryGetValue should return false and value should contian default(TValue)
        Test.Eval(!tbl.TryGetValue(keys[0], out val), "Err_008 Expected TryGetValue to return false");
        Test.Eval(val == null, "Err_009 Expected val to be null");

        for (int i = 0; i < keys.Length; i++)
        {
            tbl.Add(keys[i], values[i]);
        }

        //try to get null key
        try
        {
            tbl.TryGetValue(null, out val);
            Test.Eval(false, "Err_010 Expected to get ArgumentNullException when invoking TryGetValue() on a null key");
        }
        catch (ArgumentNullException)
        {
            Test.Eval(true);
        }

        // Try to get non existing key
        // TryGetValue should return false and value should contian default(TValue)
        Test.Eval(!tbl.TryGetValue(key, out val), "Err_011 Expected TryGetValue to return false");
        Test.Eval(val == null, "Err_012 Expected val to be null");
    }


    public Dictionary<string, string> g_stringDict = new Dictionary<string, string>();
    public Dictionary<RefX1<int>, string> g_refIntDict = new Dictionary<RefX1<int>, string>();
    public Dictionary<RefX1<string>, string> g_refStringDict = new Dictionary<RefX1<string>, string>();


    public void GenerateValuesForStringKeys(string[] stringArr)
    {
        for (int i = 0; i < 100; ++i)
        {
            g_stringDict.Add(stringArr[i], stringArr[i]);
        }
    }

    public void GenerateValuesForIntRefKeys(RefX1<int>[] refIntArr, string[] stringArr)
    {
        for (int i = 0; i < 100; ++i)
        {
            g_refIntDict.Add(refIntArr[i], stringArr[i]);
        }
    }

    public void GenerateValuesForStringRefKeys(RefX1<string>[] refStringArr, string[] stringArr)
    {
        for (int i = 0; i < 100; ++i)
        {
            g_refStringDict.Add(refStringArr[i], stringArr[i]);
        }
    }


    //This method is used for the mscorlib defined delegate
    // public delegate V CreateValueCallback(K key);
    public V CreateValue(K key)
    {
        if (key is string)
        {
            return g_stringDict[key as string] as V;
        }
        else if (key is RefX1<int>)
        {
            return g_refIntDict[key as RefX1<int>] as V;
        }
        else if (key is RefX1<string>)
        {
            return g_refStringDict[key as RefX1<string>] as V;
        }

        Test.Eval(false, "Err_12a Unknown type of key provided to CreateValue()");
        return null;

    }

    public void VerifyValue(K key, V val)
    {
        V expectedVal;

        if (key is string)
        {
            expectedVal = g_stringDict[key as string] as V;
        }
        else if (key is RefX1<int>)
        {
            expectedVal = g_refIntDict[key as RefX1<int>] as V;
        }
        else if (key is RefX1<string>)
        {
            expectedVal = g_refStringDict[key as RefX1<string>] as V;
        }
        else
        {
            Test.Eval(false, "Err_12e Incorrect key type supplied");
            return;
        }

        if (!val.Equals(expectedVal))
        {
            Test.Eval(false, "Err_12b The value returned by TryGetValue doesn't match the expected value for key: " + key +
                                "\nExpected value: " + expectedVal + "; Actual: " + val);
        }
    }

    public void GetValueValidations(K[] keys, V[] values)
    {
        ConditionalWeakTable<K,V>.CreateValueCallback valueCallBack =
            new ConditionalWeakTable<K,V>.CreateValueCallback(CreateValue);

        ConditionalWeakTable<K,V> tbl = new ConditionalWeakTable<K,V>();

        K key = keys[0];

        // Get key from an empty dictionary
        // GetValue should return the new value generated from CreateValue()
        tbl.GetValue(key, valueCallBack);

        // check that this operation added the (key,value) pair to the dictionary
        V val;

        Test.Eval(tbl.TryGetValue(key, out val));
        VerifyValue(key, val);

        // now add values to the table
        for (int i = 1; i < keys.Length; i++)
        {
            try
            {
                tbl.Add(keys[i], values[i]);
            }
            catch (ArgumentException) { }
        }

        // try to get value for a key that already exists in the table
        tbl.GetValue(keys[55], valueCallBack);

        Test.Eval(tbl.TryGetValue(keys[55], out val));
        VerifyValue(keys[55], val);


        //try to get null key
        try
        {
            tbl.GetValue(null, valueCallBack);
            Test.Eval(false, "Err_010 Expected to get ArgumentNullException when invoking TryGetValue() on a null key");
        }
        catch (ArgumentNullException)
        {
            Test.Eval(true);
        }

        // try to use null callback
        try
        {
            valueCallBack = null;
            tbl.GetValue(key, valueCallBack);
            Test.Eval(false, "Err_010 Expected to get ArgumentNullException when invoking TryGetValue() on a null callback");
        }
        catch (ArgumentNullException)
        {
            Test.Eval(true);
        }
    }

    // The method first adds some keys to the table
    // Then removes a key, checks that it was removed, adds the same key and verifies that it was added.
    public void AddRemoveKeyValPair(K[] keys, V[] values, int index, int repeat)
    {
        ConditionalWeakTable<K,V> tbl = new ConditionalWeakTable<K,V>();

        for (int i = 0; i < keys.Length; i++)
        {
            tbl.Add(keys[i], values[i]);
        }

        for (int i = 0; i < repeat; i++)
        {
            // remove existing key and ensure method return true
            Test.Eval(tbl.Remove(keys[index]), "Err_013 Expected Remove to return true");

            V val;
            // since we removed the key, TryGetValue should return false
            Test.Eval(!tbl.TryGetValue(keys[index], out val), "Err_014 Expected TryGetValue to return false");

            Test.Eval(val == null, "Err_015 Expected val to be null");

            // next add the same key
            tbl.Add(keys[index], values[index]);

            // since we added the key, TryGetValue should return true
            Test.Eval(tbl.TryGetValue(keys[index], out val), "Err_016 Expected TryGetValue to return true");

            if (val == null && values[i] == null)
            {
                Test.Eval(true);
            }
            else if (val != null && values[index] != null && val.Equals(values[index]))
            {
                Test.Eval(true);
            }
            else
            {
                // only one of the values is null or the values don't match
                Test.Eval(false, "Err_017 The value returned by TryGetValue doesn't match the expected value");
            }
        }
    }

    public void BasicGetOrCreateValue(K[] keys)
    {
	V[] values = new V[keys.Length];

        ConditionalWeakTable<K,V> tbl = new ConditionalWeakTable<K,V>();

        // assume additions for all values
        for (int i = 0; i < keys.Length; i++)
        {
            values[i] = tbl.GetOrCreateValue(keys[i]);
        }

        for (int i = 0; i < keys.Length; i++)
        {
            V val;

            // make sure TryGetValues return true, since the key should be in the table
            Test.Eval(tbl.TryGetValue(keys[i], out val), "Err_018 Expected TryGetValue to return true");

            if (val == null || !val.Equals(values[i]))
            {
                // only one of the values is null or the values don't match
                Test.Eval(false, "Err_019 The value returned by TryGetValue doesn't match the object created via the default constructor");
            }
        }
    }


    public void BasicAddThenGetOrCreateValue(K[] keys, V[] values)
    {
        ConditionalWeakTable<K,V> tbl = new ConditionalWeakTable<K,V>();

        // assume additions for all values
        for (int i = 0; i < keys.Length; i++)
        {
            tbl.Add(keys[i], values[i]);
        }

        for (int i = 0; i < keys.Length; i++)
        {
            V val;

            // make sure GetOrCreateValues the value added (and not a new object)
            val = tbl.GetOrCreateValue(keys[i]);

            if (val == null || !val.Equals(values[i]))
            {
                // only one of the values is null or the values don't match
                Test.Eval(false, "Err_020 The value returned by GetOrCreateValue doesn't match the object added");
            }
        }
    }
}

public class NoDefaultConstructor
{
   public NoDefaultConstructor(string str)
   {
   }
}

public class WithDefaultConstructor
{
   private string str;

   public WithDefaultConstructor()
   {
   }

   public WithDefaultConstructor(string s)
   {
       str = s;
   }

   public new bool Equals(object obj)
   {
       WithDefaultConstructor wdc = (WithDefaultConstructor)obj;

       return (wdc.str.Equals(str));
   }

}

public class NegativeTestCases
{
    public static void NoDefaulConstructor()
    {
        ConditionalWeakTable<string,NoDefaultConstructor> tbl = new ConditionalWeakTable<string,NoDefaultConstructor>();

        try
        {
            tbl.GetOrCreateValue("string1");

            Test.Eval(false, "Err_021 MissingMethodException execpted");
        }
        catch (Exception e)
        {
            Test.Eval(typeof(MissingMethodException) == e.GetType(), "Err_022 Incorrect exception thrown: " + e);
        }
    }
}

public class TestAPIs
{
    [Fact]
    public static int TestEntryPoint()
    {
        Random r = new Random();

        try
        {
            // test for ConditionalWeakTable<string>
            Driver<string,string> stringDriver = new Driver<string,string>();

            string[] stringArr = new string[100];
            for (int i = 0; i < 100; i++)
            {
                stringArr[i] = "SomeTestString" + i.ToString();
            }

            // test with generic object
            // test for ConditionalWeakTable<RefX1<int>>
            Driver<string,RefX1<int>> refIntDriver = new Driver<string,RefX1<int>>();

            RefX1<int>[] refIntArr = new RefX1<int>[100];
            for (int i = 0; i < 100; i++)
            {
                refIntArr[i] = new RefX1<int>(i);
            }

            // test with generic object
            // test for ConditionalWeakTable<RefX1<string>>
            Driver<string, RefX1<string>> refStringDriver = new Driver<string,RefX1<string>>();

            RefX1<string>[] refStringArr = new RefX1<string>[100];
            for (int i = 0; i < 100; i++)
            {
                refStringArr[i] = new RefX1<string>("SomeTestString" + i.ToString());
            }


            stringDriver.BasicAdd(stringArr, stringArr);
            refIntDriver.BasicAdd(stringArr, refIntArr);
            refStringDriver.BasicAdd(stringArr, refStringArr);

            //===============================================================
            // test various boundary conditions
            // - add/remove/lookup of null key
            // - remove/lookup of non-existing key in an empty dictionary and a non-empty dictionary
            stringDriver.AddValidations(stringArr, stringArr, stringArr[0]);
            refIntDriver.AddValidations(stringArr, refIntArr, refIntArr[0]);
            refStringDriver.AddValidations(stringArr, refStringArr, refStringArr[0]);

            //===============================================================
            stringDriver.RemoveValidations(stringArr, stringArr, r.Next().ToString(), stringArr[0]);
            refIntDriver.RemoveValidations(stringArr, refIntArr, r.Next().ToString(), refIntArr[0]);
            refStringDriver.RemoveValidations(stringArr, refStringArr, r.Next().ToString(), refStringArr[0]);

            //===============================================================
            stringDriver.TryGetValueValidations(stringArr, stringArr, r.Next().ToString(), stringArr[0]);
            refIntDriver.TryGetValueValidations(stringArr, refIntArr, r.Next().ToString(), refIntArr[0]);
            refStringDriver.TryGetValueValidations(stringArr, refStringArr, r.Next().ToString(), refStringArr[0]);

            //===============================================================
            // this method generates a dictionary with keys and values to be used for GetValue() method testing
            stringDriver.GenerateValuesForStringKeys(stringArr);
            stringDriver.GetValueValidations(stringArr, stringArr);

            Driver<RefX1<int>, string> refIntDriver2 = new Driver<RefX1<int>, string>();
            refIntDriver2.GenerateValuesForIntRefKeys(refIntArr, stringArr);
            refIntDriver2.GetValueValidations(refIntArr,stringArr);

            Driver<RefX1<string>, string> refStringDriver2 = new Driver<RefX1<string>, string>();
            refStringDriver2.GenerateValuesForStringRefKeys(refStringArr, stringArr);
            refStringDriver2.GetValueValidations(refStringArr, stringArr);

            //===============================================================
            stringDriver.AddSameKey(stringArr, stringArr, 0, 2);
            stringDriver.AddSameKey(stringArr, stringArr, 99, 3);
            stringDriver.AddSameKey(stringArr, stringArr, 50, 4);
            stringDriver.AddSameKey(stringArr, stringArr, 1, 5);
            stringDriver.AddSameKey(stringArr, stringArr, 98, 6);

            refIntDriver.AddSameKey(stringArr, refIntArr, 0, 2);
            refIntDriver.AddSameKey(stringArr, refIntArr, 99, 3);
            refIntDriver.AddSameKey(stringArr, refIntArr, 50, 4);
            refIntDriver.AddSameKey(stringArr, refIntArr, 1, 5);
            refIntDriver.AddSameKey(stringArr, refIntArr, 98, 6);

            refStringDriver.AddSameKey(stringArr, refStringArr, 0, 2);
            refStringDriver.AddSameKey(stringArr, refStringArr, 99, 3);
            refStringDriver.AddSameKey(stringArr, refStringArr, 50, 4);
            refStringDriver.AddSameKey(stringArr, refStringArr, 1, 5);
            refStringDriver.AddSameKey(stringArr, refStringArr, 98, 6);

            //===============================================================
            stringDriver.AddRemoveKeyValPair(stringArr, stringArr, 0, 2);
            stringDriver.AddRemoveKeyValPair(stringArr, stringArr, 99, 3);
            stringDriver.AddRemoveKeyValPair(stringArr, stringArr, 50, 4);
            stringDriver.AddRemoveKeyValPair(stringArr, stringArr, 1, 5);
            stringDriver.AddRemoveKeyValPair(stringArr, stringArr, 98, 6);

            refIntDriver.AddRemoveKeyValPair(stringArr, refIntArr, 0, 2);
            refIntDriver.AddRemoveKeyValPair(stringArr, refIntArr, 99, 3);
            refIntDriver.AddRemoveKeyValPair(stringArr, refIntArr, 50, 4);
            refIntDriver.AddRemoveKeyValPair(stringArr, refIntArr, 1, 5);
            refIntDriver.AddRemoveKeyValPair(stringArr, refIntArr, 98, 6);

            refStringDriver.AddRemoveKeyValPair(stringArr, refStringArr, 0, 2);
            refStringDriver.AddRemoveKeyValPair(stringArr, refStringArr, 99, 3);
            refStringDriver.AddRemoveKeyValPair(stringArr, refStringArr, 50, 4);
            refStringDriver.AddRemoveKeyValPair(stringArr, refStringArr, 1, 5);
            refStringDriver.AddRemoveKeyValPair(stringArr, refStringArr, 98, 6);

            //==============================================================
            // new tests for GetOrCreateValue
            (new Driver<string, WithDefaultConstructor>()).BasicGetOrCreateValue(stringArr);
            WithDefaultConstructor[] wvalues = new WithDefaultConstructor[stringArr.Length];
            for (int i=0; i<wvalues.Length; i++) wvalues[i] = new WithDefaultConstructor(stringArr[i]);
            (new Driver<string, WithDefaultConstructor>()).BasicAddThenGetOrCreateValue(stringArr, wvalues);

            NegativeTestCases.NoDefaulConstructor();

            //===============================================================
            if (Test.result)
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Test Failed");
                return 101;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Test threw unexpected exception:\n{0}", e);
            return 102;
        }
    }
}


