// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Specialized;

namespace System.Diagnostics
{
    /// <devdoc>
    /// <para>Provides the <see langword='abstract '/>base class for the listeners who
    ///    monitor trace and debug output.</para>
    /// </devdoc>
    public abstract class TraceListener : MarshalByRefObject, IDisposable
    {
        private int _indentLevel;
        private int _indentSize = 4;
        private TraceOptions _traceOptions = TraceOptions.None;
        private bool _needIndent = true;
        private StringDictionary? _attributes;

        private string? _listenerName;
        private TraceFilter? _filter;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Diagnostics.TraceListener'/> class.</para>
        /// </devdoc>
        protected TraceListener()
        {
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Diagnostics.TraceListener'/> class using the specified name as the
        ///    listener.</para>
        /// </devdoc>
        protected TraceListener(string? name)
        {
            _listenerName = name;
        }

        public StringDictionary Attributes
        {
            get
            {
                if (_attributes == null)
                    _attributes = new StringDictionary();
                return _attributes;
            }
        }

        /// <devdoc>
        /// <para> Gets or sets a name for this <see cref='System.Diagnostics.TraceListener'/>.</para>
        /// </devdoc>
        [AllowNull]
        public virtual string Name
        {
            get { return _listenerName ?? ""; }

            set { _listenerName = value; }
        }

        public virtual bool IsThreadSafe
        {
            get { return false; }
        }

        /// <devdoc>
        /// </devdoc>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <devdoc>
        /// </devdoc>
        protected virtual void Dispose(bool disposing)
        {
            return;
        }

        /// <devdoc>
        ///    <para>When overridden in a derived class, flushes the output buffer.</para>
        /// </devdoc>
        public virtual void Flush()
        {
            return;
        }

        /// <devdoc>
        ///    <para>Gets or sets the indent level.</para>
        /// </devdoc>
        public int IndentLevel
        {
            get
            {
                return _indentLevel;
            }

            set
            {
                _indentLevel = (value < 0) ? 0 : value;
            }
        }

        /// <devdoc>
        ///    <para>Gets or sets the number of spaces in an indent.</para>
        /// </devdoc>
        public int IndentSize
        {
            get
            {
                return _indentSize;
            }

            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(IndentSize), value, SR.TraceListenerIndentSize);
                _indentSize = value;
            }
        }

        public TraceFilter? Filter
        {
            get
            {
                return _filter;
            }
            set
            {
                _filter = value;
            }
        }


        /// <devdoc>
        ///    <para>Gets or sets a value indicating whether an indent is needed.</para>
        /// </devdoc>
        protected bool NeedIndent
        {
            get
            {
                return _needIndent;
            }

            set
            {
                _needIndent = value;
            }
        }

        public TraceOptions TraceOutputOptions
        {
            get { return _traceOptions; }
            set
            {
                if (((int)value >> 6) != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _traceOptions = value;
            }
        }

        /// <devdoc>
        ///    <para>When overridden in a derived class, closes the output stream
        ///       so that it no longer receives tracing or debugging output.</para>
        /// </devdoc>
        public virtual void Close()
        {
            return;
        }

        protected internal virtual string[]? GetSupportedAttributes()
        {
            return null;
        }

        public virtual void TraceTransfer(TraceEventCache? eventCache, string source, int id, string? message, Guid relatedActivityId)
        {
            TraceEvent(eventCache, source, TraceEventType.Transfer, id, string.Create(null, stackalloc char[256], $"{message}, relatedActivityId={relatedActivityId}"));
        }

        /// <devdoc>
        ///    <para>Emits or displays a message for an assertion that always fails.</para>
        /// </devdoc>
        public virtual void Fail(string? message)
        {
            Fail(message, null);
        }

        /// <devdoc>
        ///    <para>Emits or displays messages for an assertion that always fails.</para>
        /// </devdoc>
        public virtual void Fail(string? message, string? detailMessage)
        {
            WriteLine(detailMessage is null ?
                $"{SR.TraceListenerFail} {message}" :
                $"{SR.TraceListenerFail} {message} {detailMessage}");
        }

        /// <devdoc>
        ///    <para>When overridden in a derived class, writes the specified
        ///       message to the listener you specify in the derived class.</para>
        /// </devdoc>
        public abstract void Write(string? message);

        /// <devdoc>
        /// <para>Writes the name of the <paramref name="o"/> parameter to the listener you specify when you inherit from the <see cref='System.Diagnostics.TraceListener'/>
        /// class.</para>
        /// </devdoc>
        public virtual void Write(object? o)
        {
            if (Filter != null && !Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, null, null, o))
                return;

            if (o == null) return;
            Write(o.ToString());
        }

        /// <devdoc>
        ///    <para>Writes a category name and a message to the listener you specify when you
        ///       inherit from the <see cref='System.Diagnostics.TraceListener'/>
        ///       class.</para>
        /// </devdoc>
        public virtual void Write(string? message, string? category)
        {
            if (Filter != null && !Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, message))
                return;

            if (category == null)
                Write(message);
            else
                Write(category + ": " + (message ?? string.Empty));
        }

        /// <devdoc>
        /// <para>Writes a category name and the name of the <paramref name="o"/> parameter to the listener you
        ///    specify when you inherit from the <see cref='System.Diagnostics.TraceListener'/>
        ///    class.</para>
        /// </devdoc>
        public virtual void Write(object? o, string? category)
        {
            if (Filter != null && !Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, category, null, o))
                return;

            if (category == null)
                Write(o);
            else
                Write(o == null ? "" : o.ToString(), category);
        }

        /// <devdoc>
        ///    <para>Writes the indent to the listener you specify when you
        ///       inherit from the <see cref='System.Diagnostics.TraceListener'/>
        ///       class, and resets the <see cref='TraceListener.NeedIndent'/> property to <see langword='false'/>.</para>
        /// </devdoc>
        protected virtual void WriteIndent()
        {
            NeedIndent = false;
            for (int i = 0; i < _indentLevel; i++)
            {
                if (_indentSize == 4)
                    Write("    ");
                else
                {
                    for (int j = 0; j < _indentSize; j++)
                    {
                        Write(" ");
                    }
                }
            }
        }

        /// <devdoc>
        ///    <para>When overridden in a derived class, writes a message to the listener you specify in
        ///       the derived class, followed by a line terminator. The default line terminator is a carriage return followed
        ///       by a line feed (\r\n).</para>
        /// </devdoc>
        public abstract void WriteLine(string? message);

        /// <devdoc>
        /// <para>Writes the name of the <paramref name="o"/> parameter to the listener you specify when you inherit from the <see cref='System.Diagnostics.TraceListener'/> class, followed by a line terminator. The default line terminator is a
        ///    carriage return followed by a line feed
        ///    (\r\n).</para>
        /// </devdoc>
        public virtual void WriteLine(object? o)
        {
            if (Filter != null && !Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, null, null, o))
                return;

            WriteLine(o == null ? "" : o.ToString());
        }

        /// <devdoc>
        ///    <para>Writes a category name and a message to the listener you specify when you
        ///       inherit from the <see cref='System.Diagnostics.TraceListener'/> class,
        ///       followed by a line terminator. The default line terminator is a carriage return followed by a line feed (\r\n).</para>
        /// </devdoc>
        public virtual void WriteLine(string? message, string? category)
        {
            if (Filter != null && !Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, message))
                return;
            if (category == null)
                WriteLine(message);
            else
                WriteLine(category + ": " + (message ?? string.Empty));
        }

        /// <devdoc>
        ///    <para>Writes a category
        ///       name and the name of the <paramref name="o"/>parameter to the listener you
        ///       specify when you inherit from the <see cref='System.Diagnostics.TraceListener'/>
        ///       class, followed by a line terminator. The default line terminator is a carriage
        ///       return followed by a line feed (\r\n).</para>
        /// </devdoc>
        public virtual void WriteLine(object? o, string? category)
        {
            if (Filter != null && !Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, category, null, o))
                return;

            WriteLine(o == null ? "" : o.ToString(), category);
        }


        // new write methods used by TraceSource

        public virtual void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, object? data)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data))
                return;

            WriteHeader(source, eventType, id);
            string? datastring = string.Empty;
            if (data != null)
                datastring = data.ToString();

            WriteLine(datastring);
            WriteFooter(eventCache);
        }

        public virtual void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, params object?[]? data)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
                return;

            WriteHeader(source, eventType, id);

            WriteLine(data != null ? string.Join(", ", data) : string.Empty);

            WriteFooter(eventCache);
        }

        public virtual void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id)
        {
            TraceEvent(eventCache, source, eventType, id, string.Empty);
        }

        // All other TraceEvent methods come through this one.
        public virtual void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, message))
                return;

            WriteHeader(source, eventType, id);
            WriteLine(message);

            WriteFooter(eventCache);
        }

        public virtual void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? format, params object?[]? args)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, format, args))
                return;

            WriteHeader(source, eventType, id);
            if (args != null)
                WriteLine(string.Format(CultureInfo.InvariantCulture, format!, args));
            else
                WriteLine(format);

            WriteFooter(eventCache);
        }

        private void WriteHeader(string source, TraceEventType eventType, int id)
        {
            Write(string.Create(CultureInfo.InvariantCulture, stackalloc char[256], $"{source} {eventType}: {id} : "));
        }

        private void WriteFooter(TraceEventCache? eventCache)
        {
            if (eventCache == null)
                return;

            _indentLevel++;

            if (IsEnabled(TraceOptions.ProcessId))
                WriteLine("ProcessId=" + eventCache.ProcessId);

            if (IsEnabled(TraceOptions.LogicalOperationStack))
            {
                Write("LogicalOperationStack=");
                Stack operationStack = eventCache.LogicalOperationStack;
                bool first = true;
                foreach (object obj in operationStack)
                {
                    if (!first)
                    {
                        Write(", ");
                    }
                    else
                    {
                        first = false;
                    }

                    Write(obj.ToString());
                }

                WriteLine(string.Empty);
            }

            Span<char> stackBuffer = stackalloc char[128];

            if (IsEnabled(TraceOptions.ThreadId))
                WriteLine("ThreadId=" + eventCache.ThreadId);

            if (IsEnabled(TraceOptions.DateTime))
                WriteLine(string.Create(null, stackBuffer, $"DateTime={eventCache.DateTime:o}"));

            if (IsEnabled(TraceOptions.Timestamp))
                WriteLine(string.Create(null, stackBuffer, $"Timestamp={eventCache.Timestamp}"));

            if (IsEnabled(TraceOptions.Callstack))
                WriteLine("Callstack=" + eventCache.Callstack);

            _indentLevel--;
        }

        internal bool IsEnabled(TraceOptions opts)
        {
            return (opts & TraceOutputOptions) != 0;
        }
    }
}
