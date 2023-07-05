// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Diagnostics
{
    public sealed class EventLogTraceListener : TraceListener
    {
        private bool _nameSet;

        public EventLogTraceListener()
        {
        }

        public EventLogTraceListener(EventLog eventLog)
            : base(eventLog != null ? eventLog.Source : string.Empty)
        {
            EventLog = eventLog;
        }

        public EventLogTraceListener(string source)
        {
            EventLog = new EventLog
            {
                Source = source
            };
        }

        public EventLog EventLog
        {
            get;
            set;
        }

        public override string Name
        {
            get
            {
                if (_nameSet == false && EventLog != null)
                {
                    _nameSet = true;
                    base.Name = EventLog.Source;
                }

                return base.Name;
            }

            set
            {
                _nameSet = true;
                base.Name = value;
            }
        }

        public override void Close() => EventLog?.Close();

        protected override void Dispose(bool disposing)
        {
            try
            {
                Close();
                if (!disposing)
                {
                    EventLog = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void Write(string message) => EventLog?.WriteEntry(message);

        public override void WriteLine(string message) => Write(message);

        [ComVisible(false)]
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType severity, int id, string format, params object[] args)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, severity, id, format, args, null, null))
                return;

            EventInstance data = CreateEventInstance(severity, id);

            if (args == null || args.Length == 0)
            {
                EventLog.WriteEvent(data, format);
            }
            else if (string.IsNullOrEmpty(format))
            {
                string[] strings = new string[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    strings[i] = args[i].ToString();
                }

                EventLog.WriteEvent(data, strings);
            }
            else
            {
                EventLog.WriteEvent(data, string.Format(CultureInfo.InvariantCulture, format, args));
            }

        }

        [ComVisible(false)]
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType severity, int id, string message)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, severity, id, message, null, null, null))
                return;

            EventLog.WriteEvent(CreateEventInstance(severity, id), message);
        }

        [ComVisible(false)]
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType severity, int id, object data)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, severity, id, null, null, data, null))
                return;

            EventLog.WriteEvent(CreateEventInstance(severity, id), new object[] { data });
        }

        [ComVisible(false)]
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType severity, int id, params object[] data)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, severity, id, null, null, null, data))
                return;

            EventInstance inst = CreateEventInstance(severity, id);

            var sb = new StringBuilder();
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (i != 0)
                        sb.Append(", ");

                    if (data[i] != null)
                        sb.Append(data[i].ToString());
                }
            }

            EventLog.WriteEvent(inst, new object[] { sb.ToString() });
        }

        private static EventInstance CreateEventInstance(TraceEventType severity, int id)
        {
            // Win32 EventLog has an implicit cap at ushort.MaxValue
            // We need to cap this explicitly to prevent larger value
            // being wrongly casted
            if (id > ushort.MaxValue)
                id = ushort.MaxValue;

            // Ideally we need to pick a value other than '0' as zero is
            // a commonly used EventId by most applications
            if (id < ushort.MinValue)
                id = ushort.MinValue;

            EventInstance data = new EventInstance(id, 0);

            if (severity == TraceEventType.Error || severity == TraceEventType.Critical)
                data.EntryType = EventLogEntryType.Error;
            else if (severity == TraceEventType.Warning)
                data.EntryType = EventLogEntryType.Warning;
            else
                data.EntryType = EventLogEntryType.Information;

            return data;
        }

    }
}
