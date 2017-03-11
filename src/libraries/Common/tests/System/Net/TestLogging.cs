// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit.Abstractions;

namespace System.Net.Test.Common
{
    public class TestLogging : ITestOutputHelper
    {
        private readonly static TestLogging s_instance = new TestLogging();

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
