// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;
using System.Text;
using Xunit.Abstractions;

namespace System.Net.Test.Common
{
    public class TestLogging : ITestOutputHelper
    {
        private static TestLogging s_instance = new TestLogging();

        private TestLogging()
        {
        }

        public static TestLogging GetInstance()
        {
            return s_instance;
        }

        public void WriteLine(string message)
        {
            EventSourceTestLogging.Log.TestMessage(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            EventSourceTestLogging.Log.TestMessage(string.Format(format, args));
        }
    }
}
