// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace OLEDB.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // CTestSkippedException
    //
    // Since this is a very common exception and makes it so you
    // can catch only the skipped exceptions this is quite useful
    //
    ////////////////////////////////////////////////////////////////
    public class CTestSkippedException : CTestException
    {
        //Constructor
        public CTestSkippedException(string message)
            : this(message, false, true, null)
        {
        }

        public CTestSkippedException(string message, object actual, object expected, Exception inner)
            : base(CTestBase.TEST_SKIPPED, message, actual, expected, inner)
        {
        }
    }

    ////////////////////////////////////////////////////////////////
    // CTestFailedException
    //
    ////////////////////////////////////////////////////////////////
    public class CTestFailedException : CTestException
    {
        //Constructor
        public CTestFailedException(string message)
            : this(message, false, true, null)
        {
        }

        public CTestFailedException(string message, object actual, object expected, Exception inner)
            : base(CTestBase.TEST_FAIL, message, actual, expected, inner)
        {
        }
    }

    ////////////////////////////////////////////////////////////////
    // CTestException
    //
    // For all other cases, you can just setup the status directly
    ////////////////////////////////////////////////////////////////
    public class CTestException : Exception
    {
        //Data
        public int Result;
        public object Actual;
        public object Expected;

        //Constructor
        public CTestException(string message)
            : this(CTestBase.TEST_FAIL, message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
        }

        public CTestException(int result, string message)
            : this(result, message, false, true, null)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
        }

        public CTestException(int result, string message, object actual, object expected, Exception inner)
            : base(message, inner)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            //Note: iResult is the variation result (i.e.: TEST_PASS, TEST_FAIL, etc...)
            //Setup the exception
            Result = result;
            Actual = actual ?? "";
            Expected = expected ?? "";
        }

        public override string Message
        {
            get
            {
                StringBuilder text = new StringBuilder();
                text.AppendLine(base.Message);
                text.AppendLine($"Expected: `{Expected.ToString()}`");
                text.AppendLine($"Actual: `{Actual.ToString()}`");
                text.AppendLine($"Result: {Result}");
                return text.ToString();
            }
        }
    }
}
