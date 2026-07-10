// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TestLibrary;
using Xunit;

public static class IteratorConditionalEscape
{
    private const int Iterations = 10_000;

    private static readonly List<int> s_list = Enumerable.Range(0, 20).ToList();
    private static readonly int[] s_array = Enumerable.Range(0, 20).ToArray();
    private static readonly IEnumerable<int> s_enumerable = s_list;
    private static readonly List<int> s_emptyList = [];
    private static readonly IEnumerable<int> s_emptyEnumerable = s_emptyList;
    private static readonly Func<int, bool> s_even = static x => (x & 1) == 0;
    private static readonly Func<int, bool> s_gt3 = static x => x > 3;
    private static readonly Func<int, int> s_times2 = static x => x * 2;

    private delegate int Test();

    private sealed class Case(
        string name,
        Test test,
        int expectedResult,
        double maxBytesPerIteration,
        bool warmUp = false,
        int iterations = Iterations,
        bool checkAllocations = true)
    {
        public string Name { get; } = name;
        public Test Test { get; } = test;
        public int ExpectedResult { get; } = expectedResult;
        public double MaxBytesPerIteration { get; } = maxBytesPerIteration;
        public bool WarmUp { get; } = warmUp;
        public int Iterations { get; } = iterations;
        public bool CheckAllocations { get; } = checkAllocations;
    }

    private static readonly Case[] s_cases =
    [
        new(nameof(WhereList), WhereList, 90, 8, warmUp: true),
        new(nameof(WhereArray), WhereArray, 90, 8, warmUp: true),
        new(nameof(WhereEnumerable), WhereEnumerable, 184, 8, warmUp: true),
        new(nameof(SelectList), SelectList, 380, 8, warmUp: true),
        new(nameof(SelectArray), SelectArray, 380, 8, warmUp: true),
        new(nameof(SelectEnumerable), SelectEnumerable, 380, 8, warmUp: true),
        new(nameof(YieldIterator), YieldIterator, 190, 40, warmUp: true),
        new(nameof(DefaultIfEmptyEmptyList), DefaultIfEmptyEmptyList, 0, 8, warmUp: true),
        new(nameof(DefaultIfEmptyEmptyArray), DefaultIfEmptyEmptyArray, 0, 8, warmUp: true),
        new(nameof(DefaultIfEmptyEmptyEnumerable), DefaultIfEmptyEmptyEnumerable, 0, 8, warmUp: true),
        new(nameof(TwoWhereListSerial), TwoWhereListSerial, 274, 8, warmUp: true),
        new(nameof(WhereListThenSelectListSerial), WhereListThenSelectListSerial, 470, 8, warmUp: true),
        new(nameof(TwoWhereDifferentParallel), TwoWhereDifferentParallel, 175, 160, iterations: 1, checkAllocations: false),
        new(nameof(IfForeachDifferentTypesHotList), IfForeachDifferentTypesHotList, 90, 160),
        new(nameof(IfForeachDifferentTypesHotArray), IfForeachDifferentTypesHotArray, 184, 160),
        new(nameof(NestedForeachDifferentTypes), NestedForeachDifferentTypes, 3_280, 1_000),
        new(nameof(SkipList), SkipList, 187, 96),
        new(nameof(TakeArray), TakeArray, 45, 96),
    ];

    [ActiveIssue("needs triage", TestRuntimes.Mono)]
    [Fact]
    public static int TestEntryPoint()
    {
        string? selectedCaseName = Environment.GetEnvironmentVariable("ITERATOR_CEA_CASE");
        if (selectedCaseName is not null)
        {
            foreach (Case testCase in s_cases)
            {
                if (testCase.Name == selectedCaseName)
                {
                    return RunInCurrentProcess(testCase);
                }
            }

            Console.WriteLine($"Unknown case '{selectedCaseName}'");
            return -1;
        }

        int result = 100;

        foreach (Case testCase in s_cases)
        {
            if (!RunInChildProcess(testCase))
            {
                result = -1;
            }
        }

        return result;
    }

    private static int RunInCurrentProcess(Case testCase)
    {
        if (testCase.WarmUp)
        {
            WarmUp(testCase.Test);
        }

        Thread.Sleep(100);

        bool checkAllocations = Environment.GetEnvironmentVariable("DOTNET_GCStress") is null;
        return RunAndValidate(testCase, checkAllocations) ? 100 : -1;
    }

    private static bool RunInChildProcess(Case testCase)
    {
        string? processPath = Environment.ProcessPath;
        if (processPath is null)
        {
            Console.WriteLine("Unable to locate current process path");
            return false;
        }

        ProcessStartInfo processInfo = new(processPath)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        processInfo.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        processInfo.Environment["ITERATOR_CEA_CASE"] = testCase.Name;

        using Process process = Process.Start(processInfo)!;
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        string output = outputTask.GetAwaiter().GetResult();
        string error = errorTask.GetAwaiter().GetResult();

        Console.Write(output);
        Console.Error.Write(error);

        if (process.ExitCode == 100)
        {
            return true;
        }

        Console.WriteLine($"FAILURE ({testCase.Name}): child process exited with {process.ExitCode}");
        return false;
    }

    private static void WarmUp(Test test)
    {
        for (int i = 0; i < 30; i++)
        {
            for (int j = 0; j < 2_000; j++)
            {
                test();
            }

            Thread.Sleep(15);
        }

        Thread.Sleep(50);

        for (int i = 0; i < 1_000; i++)
        {
            test();
        }
    }

    private static bool RunAndValidate(Case testCase, bool checkAllocations)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
        int sum = 0;
        for (int i = 0; i < testCase.Iterations; i++)
        {
            sum += testCase.Test();
        }
        long allocatedBytesAfter = GC.GetAllocatedBytesForCurrentThread();

        int expectedSum = testCase.ExpectedResult * testCase.Iterations;
        if (sum != expectedSum)
        {
            Console.WriteLine($"FAILURE ({testCase.Name}): expected {expectedSum}, got {sum}");
            return false;
        }

        double bytesPerIteration = (double)(allocatedBytesAfter - allocatedBytesBefore) / testCase.Iterations;
        if (checkAllocations && testCase.CheckAllocations && (bytesPerIteration > testCase.MaxBytesPerIteration))
        {
            Console.WriteLine(
                $"FAILURE ({testCase.Name}): allocated {bytesPerIteration:F3} bytes/iteration; expected <= {testCase.MaxBytesPerIteration:F3}");
            return false;
        }

        Console.WriteLine($"SUCCESS ({testCase.Name}): allocated {bytesPerIteration:F3} bytes/iteration");
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int WhereList() => SumWhereList(s_list.Where(s_even));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int WhereArray() => SumWhereArray(s_array.Where(s_even));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int WhereEnumerable() => SumWhereEnumerable(s_enumerable.Where(s_gt3));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SelectList() => SumSelectList(s_list.Select(s_times2));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SelectArray() => SumSelectArray(s_array.Select(s_times2));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SelectEnumerable() => SumSelectEnumerable(s_enumerable.Select(s_times2));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int YieldIterator() => SumYieldIterator(YieldRange());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DefaultIfEmptyEmptyList() => SumDefaultIfEmptyEmptyList(s_emptyList.DefaultIfEmpty());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DefaultIfEmptyEmptyArray() => SumDefaultIfEmptyEmptyArray(Array.Empty<int>().DefaultIfEmpty());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DefaultIfEmptyEmptyEnumerable() => SumDefaultIfEmptyEmptyEnumerable(s_emptyEnumerable.DefaultIfEmpty());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int TwoWhereListSerial()
    {
        int sum = 0;
        foreach (int value in s_list.Where(s_even))
        {
            sum += value;
        }

        foreach (int value in s_list.Where(s_gt3))
        {
            sum += value;
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int WhereListThenSelectListSerial()
    {
        int sum = 0;
        foreach (int value in s_list.Where(s_even))
        {
            sum += value;
        }

        foreach (int value in s_list.Select(s_times2))
        {
            sum += value;
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int TwoWhereDifferentParallel()
    {
        using IEnumerator<int> e1 = s_list.Where(s_even).GetEnumerator();
        using IEnumerator<int> e2 = s_array.Where(s_gt3).GetEnumerator();

        int sum = 0;
        while (e1.MoveNext() & e2.MoveNext())
        {
            sum += e1.Current + e2.Current;
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int IfForeachDifferentTypesHotList() => IfForeachDifferentTypes(useList: true);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int IfForeachDifferentTypesHotArray() => IfForeachDifferentTypes(useList: false);

    private static int IfForeachDifferentTypes(bool useList)
    {
        int sum = 0;
        if (useList)
        {
            foreach (int value in s_list.Where(s_even))
            {
                sum += value;
            }
        }
        else
        {
            foreach (int value in s_array.Where(s_gt3))
            {
                sum += value;
            }
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NestedForeachDifferentTypes()
    {
        int sum = 0;
        foreach (int x in s_list.Where(s_even))
        {
            foreach (int y in s_array.Where(s_gt3))
            {
                sum += x + y;
            }
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SkipList() => SumSkipList(s_list.Skip(3));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int TakeArray() => SumTakeArray(s_array.Take(10));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IEnumerable<int> YieldRange()
    {
        for (int i = 0; i < 20; i++)
        {
            yield return i;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumWhereList(IEnumerable<int> source) => Sum(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumWhereArray(IEnumerable<int> source) => Sum(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumWhereEnumerable(IEnumerable<int> source) => Sum(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumSelectList(IEnumerable<int> source) => Sum(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumSelectArray(IEnumerable<int> source) => Sum(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumSelectEnumerable(IEnumerable<int> source) => Sum(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumYieldIterator(IEnumerable<int> source) => Sum(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumDefaultIfEmptyEmptyList(IEnumerable<int> source) => Sum(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumDefaultIfEmptyEmptyArray(IEnumerable<int> source) => Sum(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumDefaultIfEmptyEmptyEnumerable(IEnumerable<int> source) => Sum(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumSkipList(IEnumerable<int> source) => Sum(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumTakeArray(IEnumerable<int> source) => Sum(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Sum(IEnumerable<int> source)
    {
        int sum = 0;
        foreach (int value in source)
        {
            sum += value;
        }

        return sum;
    }
}
