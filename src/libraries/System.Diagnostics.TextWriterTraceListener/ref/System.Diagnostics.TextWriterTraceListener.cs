// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics
{
    public partial class ConsoleTraceListener : System.Diagnostics.TextWriterTraceListener
    {
        public ConsoleTraceListener() { }
        public ConsoleTraceListener(bool useErrorStream) { }
        public override void Close() { }
    }
    public partial class DelimitedListTraceListener : System.Diagnostics.TextWriterTraceListener
    {
        public DelimitedListTraceListener(System.IO.Stream stream) { }
        public DelimitedListTraceListener(System.IO.Stream stream, string? name) { }
        public DelimitedListTraceListener(System.IO.TextWriter writer) { }
        public DelimitedListTraceListener(System.IO.TextWriter writer, string? name) { }
        public DelimitedListTraceListener(string? fileName) { }
        public DelimitedListTraceListener(string? fileName, string? name) { }
        public string Delimiter { get { throw null; } set { } }
        protected override string[] GetSupportedAttributes() { throw null; }
        public override void TraceData(System.Diagnostics.TraceEventCache? eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, object? data) { }
        public override void TraceData(System.Diagnostics.TraceEventCache? eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, params object?[]? data) { }
        public override void TraceEvent(System.Diagnostics.TraceEventCache? eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, string? message) { }
        public override void TraceEvent(System.Diagnostics.TraceEventCache? eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, string? format, params object?[]? args) { }
    }
    public partial class TextWriterTraceListener : System.Diagnostics.TraceListener
    {
        public TextWriterTraceListener() { }
        public TextWriterTraceListener(System.IO.Stream stream) { }
        public TextWriterTraceListener(System.IO.Stream stream, string? name) { }
        public TextWriterTraceListener(System.IO.TextWriter writer) { }
        public TextWriterTraceListener(System.IO.TextWriter writer, string? name) { }
        public TextWriterTraceListener(string? fileName) { }
        public TextWriterTraceListener(string? fileName, string? name) { }
        public System.IO.TextWriter? Writer { get { throw null; } set { } }
        public override void Close() { }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override void Write(string? message) { }
        public override void WriteLine(string? message) { }
    }
    public partial class XmlWriterTraceListener : System.Diagnostics.TextWriterTraceListener
    {
        public XmlWriterTraceListener(System.IO.Stream stream) { }
        public XmlWriterTraceListener(System.IO.Stream stream, string? name) { }
        public XmlWriterTraceListener(System.IO.TextWriter writer) { }
        public XmlWriterTraceListener(System.IO.TextWriter writer, string? name) { }
        public XmlWriterTraceListener(string? filename) { }
        public XmlWriterTraceListener(string? filename, string? name) { }
        public override void Close() { }
        public override void Fail(string? message, string? detailMessage) { }
        public override void TraceData(System.Diagnostics.TraceEventCache? eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, object? data) { }
        public override void TraceData(System.Diagnostics.TraceEventCache? eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, params object?[]? data) { }
        public override void TraceEvent(System.Diagnostics.TraceEventCache? eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, string? message) { }
        public override void TraceEvent(System.Diagnostics.TraceEventCache? eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, string? format, params object?[]? args) { }
        public override void TraceTransfer(System.Diagnostics.TraceEventCache? eventCache, string source, int id, string? message, System.Guid relatedActivityId) { }
        public override void Write(string? message) { }
        public override void WriteLine(string? message) { }
    }
}
