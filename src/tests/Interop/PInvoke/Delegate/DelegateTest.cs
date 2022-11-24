// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using Xunit;

using static DelegateTestNative;

class DelegateTest
{
    private static void TestFunctionPointer()
    {
        int expectedValue = 987654;
        int TestFunction() => expectedValue;

        Assert.True(ValidateDelegateReturnsExpected(expectedValue, TestFunction));

        {
            TestDelegate localDelegate = TestFunction;
            Assert.True(ReplaceDelegate(expectedValue, ref localDelegate, out int newExpectedValue));
            Assert.Equal(newExpectedValue, localDelegate());
        }

        {
            GetNativeTestFunction(out TestDelegate test, out int value);
            Assert.Equal(value, test());
        }

        {
            var returned = GetNativeTestFunctionReturned(out int value);
            Assert.Equal(value, returned());
        }

        {
            CallbackWithExpectedValue cb = new CallbackWithExpectedValue
            {
                expectedValue = expectedValue,
                del = TestFunction
            };

            Assert.True(ValidateCallbackWithValue(cb));
        }

        {
            CallbackWithExpectedValue cb = new CallbackWithExpectedValue
            {
                expectedValue = expectedValue,
                del = TestFunction
            };

            Assert.True(ValidateAndUpdateCallbackWithValue(ref cb));
            Assert.Equal(cb.expectedValue, cb.del());
        }

        {
            GetNativeCallbackAndValue(out CallbackWithExpectedValue cb);
            Assert.Equal(cb.expectedValue, cb.del());
        }
    }

    private static void TestIDispatch()
    {
        int expectedValue = 987654;
        int TestFunction() => expectedValue;

        Assert.True(ValidateDelegateValueMatchesExpected(expectedValue, TestFunction));

        {
            TestDelegate localDelegate = TestFunction;
            Assert.True(ValidateDelegateValueMatchesExpectedAndClear(expectedValue, ref localDelegate));
            Assert.Equal(null, localDelegate);
        }

        {
            TestDelegate localDelegate = TestFunction;
            Assert.True(DuplicateDelegate(expectedValue, localDelegate, out var outDelegate));
            Assert.Equal(localDelegate, outDelegate);
        }

        {
            TestDelegate localDelegate = TestFunction;
            Assert.Equal(localDelegate, DuplicateDelegateReturned(localDelegate));
        }

        {
            var cb = new DispatchDelegateWithExpectedValue
            {
                expectedValue = expectedValue,
                del = TestFunction
            };

            Assert.True(ValidateStructDelegateValueMatchesExpected(cb));
        }

        {
            var cb = new DispatchDelegateWithExpectedValue
            {
                expectedValue = expectedValue,
                del = TestFunction
            };

            Assert.True(ValidateDelegateValueMatchesExpectedAndClearStruct(ref cb));
            Assert.Equal(null, cb.del);
        }

        {
            var cb = new DispatchDelegateWithExpectedValue
            {
                expectedValue = expectedValue,
                del = TestFunction
            };

            Assert.True(DuplicateStruct(cb, out var cbOut));
            Assert.Equal(cbOut.expectedValue, cbOut.del());
        }

        Assert.Throws<MarshalDirectiveException>(() => MarshalDelegateAsInterface(TestFunction));
    }

    static int Main()
    {
        try
        {
            TestFunctionPointer();
            if (OperatingSystem.IsWindows())
            {
                TestIDispatch();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
        return 100;
    }
}
