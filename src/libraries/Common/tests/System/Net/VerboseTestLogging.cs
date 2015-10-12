// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;
using System.Text;
using Xunit.Abstractions;

namespace System.Net.Test.Common
{
    public class VerboseTestLogging : ITestOutputHelper
    {
        private static VerboseTestLogging s_instance = new VerboseTestLogging();

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
