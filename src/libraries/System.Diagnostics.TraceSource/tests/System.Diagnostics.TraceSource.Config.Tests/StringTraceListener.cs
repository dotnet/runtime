// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Diagnostics.TraceSourceConfigTests
{
    public class StringTraceListener : DefaultTraceListener
    {
        private StringWriter _writer;
        public bool ShouldOverrideWriteLine { get; set; } = true;

        public StringTraceListener()
        {
            _writer = new StringWriter();
            AssertUiEnabled = false;
        }

        public void Clear()
        {
            _writer = new StringWriter();
        }

        public string Output
        {
            get { return _writer.ToString(); }
        }

        public override void Write(string message)
        {
            _writer.Write(message);
        }

        public override void WriteLine(string message)
        {
            if (ShouldOverrideWriteLine)
            {
                _writer.WriteLine(message);
            }
            else
            {
                base.WriteLine(message);
            }
        }
    }
}
