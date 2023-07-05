// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

enum TestResult
{
    Success,
    ReturnFailure,
    ReturnNull,
    IncorrectEvaluation,
    ArgumentNull,
    ArgumentBad,
    DllNotFound,
    BadImage,
    InvalidOperation,
    EntryPointNotFound,
    GenericException
};

static class TestHelpers
{
    public static void EXPECT(TestResult actualValue, TestResult expectedValue = TestResult.Success)
    {
        Assert.Equal(expectedValue, actualValue);
    }

    public static TestResult Run (Func<TestResult> test)
    {
        TestResult result;

        try
        {
            result = test();
        }
        catch (ArgumentNullException)
        {
            return TestResult.ArgumentNull;
        }
        catch (ArgumentException)
        {
            return TestResult.ArgumentBad;
        }
        catch (DllNotFoundException)
        {
            return TestResult.DllNotFound;
        }
        catch (BadImageFormatException)
        {
            return TestResult.BadImage;
        }
        catch (InvalidOperationException)
        {
            return TestResult.InvalidOperation;
        }
        catch (EntryPointNotFoundException)
        {
            return TestResult.EntryPointNotFound;
        }
        catch (Exception)
        {
            return TestResult.GenericException;
        }

        return result;
    }
}
