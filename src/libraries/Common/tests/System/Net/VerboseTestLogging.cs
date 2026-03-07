// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace System.Net.Test.Common
{
    public class VerboseTestLogging : ITestOutputHelper
    {
        private static readonly VerboseTestLogging s_instance = new VerboseTestLogging();

        private VerboseTestLogging()
        {
        }

        public string Output => throw new NotSupportedException();

        public static VerboseTestLogging GetInstance()
        {
            return s_instance;
        }

        public void Write(string message)
        {
            EventSourceTestLogging.Log.TestVerboseMessage(message);
            Debug.Write(message);
        }

        public void Write(string message, params object[] args)
        {
            string formattedMessage = string.Format(message, args);
            EventSourceTestLogging.Log.TestVerboseMessage(formattedMessage);
            Debug.Write(formattedMessage);
        }

        public void WriteLine(string message)
        {
            EventSourceTestLogging.Log.TestVerboseMessage(message);
            Debug.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            string message = string.Format(format, args);
            EventSourceTestLogging.Log.TestVerboseMessage(message);
            Debug.WriteLine(message);
        }
    }
}
