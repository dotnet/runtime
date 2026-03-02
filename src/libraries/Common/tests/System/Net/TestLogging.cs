// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Test.Common
{
    public class TestLogging : ITestOutputHelper
    {
        private static readonly TestLogging s_instance = new TestLogging();

        private TestLogging()
        {
        }

        public string Output => throw new NotSupportedException();

        public static TestLogging GetInstance()
        {
            return s_instance;
        }

        public void Write(string message)
        {
            EventSourceTestLogging.Log.TestMessage(message);
        }

        public void Write(string format, params object[] args)
        {
            EventSourceTestLogging.Log.TestMessage(string.Format(format, args));
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
