// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

public class UnrollEqualsStartsWIth
{
    [Fact]
    public static int TestEntryPoint()
    {
        var testTypes = typeof(UnrollEqualsStartsWIth).Assembly
            .GetTypes()
            .Where(t => t.Name.StartsWith("Tests_len"))
            .ToArray();

        int testCount = 0;
        foreach (var testType in testTypes)
            testCount += RunTests(testType);
        return testCount == 127512 ? 100 : 0;
    }

    public static int RunTests(Type type)
    {
        // List of "reference" (unoptimized) tests
        var refImpl = type
            .GetMethods()
            .Where(m => m.Name.StartsWith("Test_ref_"))
            .ToArray();

        // List of actual tests
        var tstImpl = type
            .GetMethods()
            .Where(m => m.Name.StartsWith("Test_tst_"))
            .ToArray();

        string[] testData =
        {
            "\u9244",
            "\u9244\u9244",
            "\u9244\u9244\u9244",
            "\uFFFF",
            "\uFFFF\uFFFF",
            "\uFFFF\uFFFF\uFFFF",
            "",
            string.Empty,
            "a",
            "A",
            "\0",
            "a-",
            "aa",
            "aAa",
            "aaA",
            "a-aa",
            "aaaa",
            "aaaaa",
            "aaaaa",
            "aaaaaa",
            "aaaaaa",
            "aaaaaaa",
            "aaaaaaa",
            "aaAAaaaa",
            "aaaaa-aa",
            "aaaaaaaaa",
            "aaaaaaaaa",
            "aaaaaaaaaa",
            "aaaaaaaaaa",
            "aaaaaaaaaaa",
            "aaaAAaaaaaa",
            "aaaaaa-aaaaa",
            "aaaaaaaaaaaa",
            "aaaaaaaaaaaaa",
            "aaaaaaaaa\u0436\u0436aaa",
            "aaaaaaaaaaaaaaa",
            "aaaAAAaaaaaazzz",
            "aaaaaaaaaaaaaaaa",
            "aaaaaaaaaaaa\u0436aaaa",
            "aaaaaaAAAAaaaaaaa",
            "aaaaaaaaaaaaaaaaaa",
            "aaaaaaaggggggggggaa",
            "aaaaaaaaaaaaaaaaaaaa",
            "aaaaaaaaaaaaaaaaaaaaa",
            "aaaaaaAAAAaaaaaaaaaaaa",
            "aaaaaaaaaaaaaaaaaaaaaa\0",
            "aaa\u0447\u0447\u0447\u0447\u0447\u0447\u0447\u0447\u0447\u0447aaaa\u0436\u0436aaaaa",
            "aaaaaaaaaaaaaaaaaaaaaaaaa",
            "gggggggggggaaaaaaaaaaaaaaa",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "aaaaa\\aaaaaaaaaaaNNNNNNaaaaaa",
            "aaaaaaaaaaaaaaaaaaaaaaaaaa-aaaa",
            "aaaaaaaaaaaaaa\0aaaaaaaaaaaaaaaaa",
            "aaaaaZZZZZZZaaaaaaaaaaaaaaaaaaaaa\0",
        };

        var testDataList = testData.ToList();
        foreach (var td in testData)
        {
            // Add more test input - uppercase, lowercase, replace some chars
            testDataList.Add(td.ToUpper());
            testDataList.Add(td.Replace('a', 'b'));
            testDataList.Add(td.Replace('a', '\u0424'));
            testDataList.Add(td.ToLower());
        }

        // Add null and check how various APIs react to it
        testDataList.Add(null);

        int testCasesCount = 0;
        foreach (var testStr in testDataList)
        {
            for (int i = 0; i < refImpl.Length; i++)
            {
                // Compare states for ref and tst (e.g. both should return the same value and the same exception if any)
                if (!GetInvokeResult(refImpl[i], testStr).Equals(GetInvokeResult(tstImpl[i], testStr)))
                    throw new InvalidOperationException($"Different states, type={type}, str={testStr}, mi={tstImpl[i]}");
                testCasesCount++;
            }
        }
        return testCasesCount;
    }

    // Invoke method and return its return value and exception if happened
    public static (bool, Type) GetInvokeResult(MethodInfo mi, string str)
    {
        bool eq = false;
        Type excType = null;
        try
        {
            eq = (bool)mi.Invoke(null, new object[] { str });
        }
        catch (TargetInvocationException e)
        {
            excType = e.InnerException.GetType();
        }
        catch (Exception e)
        {
            excType = e.GetType();
        }
        return (eq, excType);
    }
}

public class Utils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Const<T>(T t) => t;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Var<T>(T t) => t;

    public const MethodImplOptions Opt = MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization;
    public const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;
}
