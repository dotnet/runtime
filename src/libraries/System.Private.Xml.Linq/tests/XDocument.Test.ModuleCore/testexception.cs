// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // TestSkippedException
    //
    // Since this is a very common exception and makes it so you
    // can catch only the skipped exceptions this is quite useful
    //
    ////////////////////////////////////////////////////////////////
    public class TestSkippedException : TestException
    {
        //Constructor
        public TestSkippedException(string message)
            : this(message, false, true, null)
        {
        }

        public TestSkippedException(string message, object actual, object expected, Exception inner)
            : base(TestResult.Skipped, message, actual, expected, inner)
        {
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestFailedException
    //
    ////////////////////////////////////////////////////////////////
    public class TestFailedException : TestException
    {
        //Constructor
        public TestFailedException(string message)
            : this(message, false, true, null)
        {
        }

        public TestFailedException(string message, object actual, object expected, Exception inner)
            : base(TestResult.Failed, message, actual, expected, inner)
        {
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestWarningException
    //
    ////////////////////////////////////////////////////////////////
    public class TestWarningException : TestException
    {
        //Constructor
        public TestWarningException(string message)
            : this(message, false, true, null)
        {
        }

        public TestWarningException(string message, object actual, object expected, Exception inner)
            : base(TestResult.Warning, message, actual, expected, inner)
        {
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestException
    //
    // For all other cases, you can just setup the status directly
    ////////////////////////////////////////////////////////////////
    public class TestException : Exception
    {
        //Data
        public TestResult Result;
        public object Actual;
        public object Expected;

        //Constructor
        public TestException(TestResult result, string message)
            : this(result, message, false, true, null)
        {
        }

        public TestException(TestResult result, string message, object actual, object expected, Exception inner)
            : base(message, inner)
        {
            //Note: iResult is the variation result (ie: TEST_PASS, TEST_FAIL, etc...)
            //Setup the exception
            Result = result;
            Actual = actual;
            Expected = expected;
        }

        public override string ToString()
        {
            var expected = "Expected: " + Expected + " (" + Expected?.GetType() + ")\n";
            var actual = "Actual  : " + Actual + " (" + Actual?.GetType() + ")\n";

            return expected + actual + "\n" + base.ToString();
        }
    }
}
