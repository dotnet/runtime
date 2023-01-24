// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;

public class AKey
{
    public override int GetHashCode()
    {
        Test.Eval(false, "Err_001 AKey.GetHashCode was invoked");
        return -1;
    }

    public override bool Equals(Object obj)
    {
        Test.Eval(false, "Err_002 AKey.Equals was invoked");

        AKey good = obj as AKey;
        if (obj == null)
            return false;

        return this == good;
    }

    public static bool operator ==(AKey a, AKey b)
    {
        Test.Eval(false, "Err_003 AKey.==operator was invoked");

        return (a == b);
    }

    public static bool operator !=(AKey a, AKey b)
    {
        Test.Eval(false, "Err_004 AKey.!=operator was invoked");

        return !(a == b);
    }
}

public class TestOverridesClass
{
    // this test ensures that while manipulating keys (through add/remove/lookup
    // in the dictionary the overridden GetHashCode(), Equals(), and ==operator do not get invoked.
    // Earlier implementation was using these functions virtually so overriding them would result in
    // the overridden functions being invoked. But later on Ati changed implementation to use
    // Runtime.GetHashCode and Object.ReferenceEquals so this test makes sure that overridden functions never get invoked.
    public static void TestOverrides()
    {
        string[] stringArr = new string[50];
        for (int i = 0; i < 50; i++)
        {
            stringArr[i] = "SomeTestString" + i.ToString();
        }

        ConditionalWeakTable<string, string> tbl = new ConditionalWeakTable<string, string>();

        string str;

        for (int i = 0; i < stringArr.Length; i++)
        {
            tbl.Add(stringArr[i], stringArr[i]);

            tbl.TryGetValue(stringArr[i], out str);

            tbl.Remove(stringArr[i]);
        }
    }

    public static int Main()
    {
        try
        {
            TestOverrides();

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
