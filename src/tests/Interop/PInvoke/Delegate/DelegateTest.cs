// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;

using static DelegateTestNative;

class DelegateTest
{
    private static void TestFunctionPointer()
    {
        int expectedValue = 987654;
        int TestFunction() => expectedValue;

        Assert.IsTrue(ValidateDelegateReturnsExpected(expectedValue, TestFunction));
        
        {
            TestDelegate localDelegate = TestFunction;
            Assert.IsTrue(ReplaceDelegate(expectedValue, ref localDelegate, out int newExpectedValue));
            Assert.AreEqual(newExpectedValue, localDelegate());
        }

        {
            GetNativeTestFunction(out TestDelegate test, out int value);
            Assert.AreEqual(value, test());
        }

        {
            var returned = GetNativeTestFunctionReturned(out int value);
            Assert.AreEqual(value, returned());
        }

        {
            CallbackWithExpectedValue cb = new CallbackWithExpectedValue
            {
                expectedValue = expectedValue,
                del = TestFunction
            };

            Assert.IsTrue(ValidateCallbackWithValue(cb));
        }

        {
            CallbackWithExpectedValue cb = new CallbackWithExpectedValue
            {
                expectedValue = expectedValue,
                del = TestFunction
            };

            Assert.IsTrue(ValidateAndUpdateCallbackWithValue(ref cb));
            Assert.AreEqual(cb.expectedValue, cb.del());
        }

        {
            GetNativeCallbackAndValue(out CallbackWithExpectedValue cb);
            Assert.AreEqual(cb.expectedValue, cb.del());
        }
    }

    private static void TestIDispatch()
    {
        int expectedValue = 987654;
        int TestFunction() => expectedValue;

        Assert.IsTrue(ValidateDelegateValueMatchesExpected(expectedValue, TestFunction));
        
        {
            TestDelegate localDelegate = TestFunction;
            Assert.IsTrue(ValidateDelegateValueMatchesExpectedAndClear(expectedValue, ref localDelegate));
            Assert.AreEqual(null, localDelegate);
        }

        {
            TestDelegate localDelegate = TestFunction;
            Assert.IsTrue(DuplicateDelegate(expectedValue, localDelegate, out var outDelegate));
            Assert.AreEqual(localDelegate, outDelegate);
        }

        {
            TestDelegate localDelegate = TestFunction;
            Assert.AreEqual(localDelegate, DuplicateDelegateReturned(localDelegate));
        }

        {
            var cb = new DispatchDelegateWithExpectedValue
            {
                expectedValue = expectedValue,
                del = TestFunction
            };

            Assert.IsTrue(ValidateStructDelegateValueMatchesExpected(cb));
        }

        {
            var cb = new DispatchDelegateWithExpectedValue
            {
                expectedValue = expectedValue,
                del = TestFunction
            };

            Assert.IsTrue(ValidateDelegateValueMatchesExpectedAndClearStruct(ref cb));
            Assert.AreEqual(null, cb.del);
        }

        {
            var cb = new DispatchDelegateWithExpectedValue
            {
                expectedValue = expectedValue,
                del = TestFunction
            };

            Assert.IsTrue(DuplicateStruct(cb, out var cbOut));
            Assert.AreEqual(cbOut.expectedValue, cbOut.del());
        }

        Assert.Throws<MarshalDirectiveException>(() => MarshalDelegateAsInterface(TestFunction));
    }

    static int Main(string[] args)
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
