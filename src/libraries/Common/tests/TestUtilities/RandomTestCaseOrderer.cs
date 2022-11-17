// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace TestUtilities;

// Based on https://github.com/xunit/xunit/blob/v2/src/xunit.execution/Sdk/DefaultTestCaseOrderer.cs

public class RandomTestCaseOrderer : ITestCaseOrderer
{
    public const string RandomSeedEnvironmentVariableName = "XUNIT_RANDOM_ORDER_SEED";

    public static readonly Lazy<int> LazySeed = new (GetSeed, LazyThreadSafetyMode.ExecutionAndPublication);
    private readonly IMessageSink _diagnosticMessageSink;

    private static int GetSeed()
    {
        string? seedEnvVar = Environment.GetEnvironmentVariable(RandomSeedEnvironmentVariableName);
        if (string.IsNullOrEmpty(seedEnvVar) || !int.TryParse(seedEnvVar, out int seed))
        {
            seed = new Random().Next();
        }

        return seed;
    }

    public RandomTestCaseOrderer(IMessageSink diagnosticMessageSink)
    {
        diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Using random seed for test cases: {LazySeed.Value}"));
        _diagnosticMessageSink = diagnosticMessageSink;
    }

    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
        => TryRandomize(testCases.ToList(), _diagnosticMessageSink, out List<TTestCase>? randomizedTests)
                    ? randomizedTests
                    : testCases;

    public static bool TryRandomize<T>(List<T> tests, IMessageSink messageSink, [NotNullWhen(true)] out List<T>? randomizedTests)
    {
        randomizedTests = null;
        try
        {
            randomizedTests = Randomize(tests.ToList());
            return true;
        }
        catch (Exception ex)
        {
            messageSink.OnMessage(new DiagnosticMessage($"Failed to randomize test cases: {ex}"));
            return false;
        }

        static List<T> Randomize(List<T> tests)
        {
            var result = new List<T>(tests.Count);

            var randomizer = new Random(LazySeed.Value);

            while (tests.Count > 0)
            {
                int next = randomizer.Next(tests.Count);
                result.Add(tests[next]);
                tests.RemoveAt(next);
            }

            return result;
        }
    }
}
