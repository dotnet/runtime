// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;

namespace System.Runtime.Serialization.Formatters.Binary
{
    [EventSource(
        Name = "System.Runtime.Serialization.Formatters.Binary.BinaryFormatterEventSource",
        LocalizationResources = "FxResources.System.Runtime.Serialization.Formatters.SR")]
    internal sealed class BinaryFormatterEventSource : EventSource
    {
        private const int EventId_SerializationStarted = 10;
        private const int EventId_SerializationEnded = 11;
        private const int EventId_SerializingObject = 12;
        private const int EventId_DeserializationStarted = 20;
        private const int EventId_DeserializationEnded = 21;
        private const int EventId_DeserializingObject = 22;

        // Used to keep track of whether a write operation is in progress. It's
        // possible the listener itself uses BinaryFormatter to write to a log,
        // and if this is the case we suppress our own logging events so that we
        // enter an infinite recursion scenario.
        private static readonly AsyncLocal<bool> _writeInProgress = new AsyncLocal<bool>();

        public static readonly BinaryFormatterEventSource Log = new BinaryFormatterEventSource();

        private BinaryFormatterEventSource()
        {
        }

        [Event(EventId_SerializationStarted, Opcode = EventOpcode.Start, Keywords = Keywords.Serialization, Level = EventLevel.Informational)]
        public void SerializationStarted()
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Serialization) && !_writeInProgress.Value)
            {
                try
                {
                    _writeInProgress.Value = true;
                    WriteEvent(EventId_SerializationStarted);
                }
                finally
                {
                    _writeInProgress.Value = false;
                }
            }
        }

        [Event(EventId_SerializationEnded, Opcode = EventOpcode.Stop, Keywords = Keywords.Serialization, Level = EventLevel.Informational)]
        public void SerializationEnded()
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Serialization) && !_writeInProgress.Value)
            {
                try
                {
                    _writeInProgress.Value = true;
                    WriteEvent(EventId_SerializationEnded);
                }
                finally
                {
                    _writeInProgress.Value = false;
                }
            }
        }

        [NonEvent]
        public void SerializingObject(Type type)
        {
            Debug.Assert(type != null);

            if (IsEnabled(EventLevel.Informational, Keywords.Serialization) && !_writeInProgress.Value)
            {
                try
                {
                    _writeInProgress.Value = true;
                    SerializingObject(type.AssemblyQualifiedName);
                }
                finally
                {
                    _writeInProgress.Value = false;
                }
            }
        }

        [Event(EventId_SerializingObject, Keywords = Keywords.Serialization, Level = EventLevel.Informational)]
        private void SerializingObject(string? typeName)
        {
            WriteEvent(EventId_SerializingObject, typeName);
        }

        [Event(EventId_DeserializationStarted, Opcode = EventOpcode.Start, Keywords = Keywords.Deserialization, Level = EventLevel.Informational)]
        public void DeserializationStarted()
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Deserialization) && !_writeInProgress.Value)
            {
                try
                {
                    _writeInProgress.Value = true;
                    WriteEvent(EventId_DeserializationStarted);
                }
                finally
                {
                    _writeInProgress.Value = false;
                }
            }
        }

        [Event(EventId_DeserializationEnded, Opcode = EventOpcode.Stop, Keywords = Keywords.Deserialization, Level = EventLevel.Informational)]
        public void DeserializationEnded()
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Deserialization) && !_writeInProgress.Value)
            {
                try
                {
                    _writeInProgress.Value = true;
                    WriteEvent(EventId_DeserializationEnded);
                }
                finally
                {
                    _writeInProgress.Value = false;
                }
            }
        }

        [NonEvent]
        public void DeserializingObject(Type type)
        {
            Debug.Assert(type != null);

            if (IsEnabled(EventLevel.Informational, Keywords.Deserialization) && !_writeInProgress.Value)
            {
                try
                {
                    _writeInProgress.Value = true;
                    DeserializingObject(type.AssemblyQualifiedName);
                }
                finally
                {
                    _writeInProgress.Value = false;
                }
            }
        }

        [Event(EventId_DeserializingObject, Keywords = Keywords.Deserialization, Level = EventLevel.Informational)]
        private void DeserializingObject(string? typeName)
        {
            WriteEvent(EventId_DeserializingObject, typeName);
        }

        public class Keywords
        {
            public const EventKeywords Serialization = (EventKeywords)1;
            public const EventKeywords Deserialization = (EventKeywords)2;
        }
    }
}
