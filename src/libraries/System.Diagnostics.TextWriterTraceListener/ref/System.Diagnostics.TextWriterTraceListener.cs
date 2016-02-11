// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Diagnostics
{
    public partial class DelimitedListTraceListener : System.Diagnostics.TextWriterTraceListener
    {
        public DelimitedListTraceListener(System.IO.Stream stream) { }
        public DelimitedListTraceListener(System.IO.Stream stream, string name) { }
        public DelimitedListTraceListener(System.IO.TextWriter writer) { }
        public DelimitedListTraceListener(System.IO.TextWriter writer, string name) { }
        public string Delimiter { get { return default(string); } set { } }
        public override void TraceData(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, object data) { }
        public override void TraceData(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, params object[] data) { }
        public override void TraceEvent(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, string message) { }
        public override void TraceEvent(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, string format, params object[] args) { }
    }
    public partial class TextWriterTraceListener : System.Diagnostics.TraceListener
    {
        public TextWriterTraceListener() { }
        public TextWriterTraceListener(System.IO.Stream stream) { }
        public TextWriterTraceListener(System.IO.Stream stream, string name) { }
        public TextWriterTraceListener(System.IO.TextWriter writer) { }
        public TextWriterTraceListener(System.IO.TextWriter writer, string name) { }
        public System.IO.TextWriter Writer { get { return default(System.IO.TextWriter); } set { } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override void Write(string message) { }
        public override void WriteLine(string message) { }
    }
}
