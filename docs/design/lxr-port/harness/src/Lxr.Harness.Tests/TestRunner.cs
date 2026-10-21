// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Lxr.Harness.Tests;

/// <summary>
/// A minimal assertion runner.
///
/// The harness has no <c>PackageReference</c> of any kind, deliberately: it has to restore and build
/// with no feed, and it has to run on CoreRun, where there is no package resolution at all. Taking
/// xunit for the tests alone would have put a package back in the tree and split the harness into a
/// part that runs anywhere and a part that does not. The assertions here are simple enough that the
/// runner is a few dozen lines.
/// </summary>
public static class TestRunner
{
    private static readonly List<string> s_failures = [];
    private static int s_passed;

    public static void Test(string name, Action body)
    {
        ArgumentNullException.ThrowIfNull(body);
        try
        {
            body();
            s_passed++;
            Console.WriteLine($"  pass  {name}");
        }
        catch (Exception ex)
        {
            s_failures.Add($"{name}: {ex.Message}");
            Console.WriteLine($"  FAIL  {name}");
            Console.WriteLine($"        {ex.Message}");
        }
    }

    public static int Summarize()
    {
        Console.WriteLine();
        Console.WriteLine($"{s_passed} passed, {s_failures.Count} failed");
        foreach (string failure in s_failures)
        {
            Console.WriteLine($"  FAILED: {failure}");
        }

        return s_failures.Count == 0 ? 0 : 1;
    }

    public static void True(bool condition, string because)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"expected true: {because}");
        }
    }

    public static void False(bool condition, string because) => True(!condition, because);

    public static void Equal<T>(T expected, T actual, string because)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"expected {expected}, got {actual}: {because}");
        }
    }

    public static void Close(double expected, double actual, double tolerance, string because)
    {
        if (double.IsNaN(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException($"expected {expected} +/- {tolerance}, got {actual}: {because}");
        }
    }

    public static void Throws<T>(Action body, string because)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(body);
        try
        {
            body();
        }
        catch (T)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"expected {typeof(T).Name}, got {ex.GetType().Name}: {because}");
        }

        throw new InvalidOperationException($"expected {typeof(T).Name}, nothing was thrown: {because}");
    }
}
