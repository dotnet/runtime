// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit.Abstractions;

namespace System.Net.Test.Common
{
    public class VerboseTestLogging : ITestOutputHelper
    {
        private static readonly VerboseTestLogging s_instance = new VerboseTestLogging();

        private VerboseTestLogging()
        {
        }

        public static VerboseTestLogging GetInstance()
        {
            return s_instance;
        }

        public void WriteLine(string message)
        {
            EventSourceTestLogging.Log.TestVerboseMessage(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            EventSourceTestLogging.Log.TestVerboseMessage(string.Format(format, args));
        }
    }
}
