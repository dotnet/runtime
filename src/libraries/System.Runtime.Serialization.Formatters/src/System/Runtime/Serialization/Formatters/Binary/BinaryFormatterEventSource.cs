// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace System.Runtime.Serialization.Formatters.Binary
{
    [EventSource(
        Name = "System.Runtime.Serialization.Formatters.Binary.BinaryFormatterEventSource")]
    internal sealed class BinaryFormatterEventSource : EventSource
    {
        private const int EventId_SerializationStart = 10;
        private const int EventId_SerializationStop = 11;
        private const int EventId_SerializingObject = 12;
        private const int EventId_DeserializationStart = 20;
        private const int EventId_DeserializationStop = 21;
        private const int EventId_DeserializingObject = 22;

        public static readonly BinaryFormatterEventSource Log = new BinaryFormatterEventSource();

        private BinaryFormatterEventSource()
        {
        }

        [Event(EventId_SerializationStart, Opcode = EventOpcode.Start, Keywords = Keywords.Serialization, Level = EventLevel.Informational, ActivityOptions = EventActivityOptions.Recursive)]
        public void SerializationStart()
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Serialization))
            {
                WriteEvent(EventId_SerializationStart);
            }
        }

        [Event(EventId_SerializationStop, Opcode = EventOpcode.Stop, Keywords = Keywords.Serialization, Level = EventLevel.Informational)]
        public void SerializationStop()
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Serialization))
            {
                WriteEvent(EventId_SerializationStop);
            }
        }

        [NonEvent]
        public void SerializingObject(Type type)
        {
            Debug.Assert(type != null);

            if (IsEnabled(EventLevel.Informational, Keywords.Serialization))
            {
                SerializingObject(type.AssemblyQualifiedName);
            }
        }

        [Event(EventId_SerializingObject, Keywords = Keywords.Serialization, Level = EventLevel.Informational)]
        private void SerializingObject(string? typeName)
        {
            WriteEvent(EventId_SerializingObject, typeName);
        }

        [Event(EventId_DeserializationStart, Opcode = EventOpcode.Start, Keywords = Keywords.Deserialization, Level = EventLevel.Informational, ActivityOptions = EventActivityOptions.Recursive)]
        public void DeserializationStart()
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Deserialization))
            {
                WriteEvent(EventId_DeserializationStart);
            }
        }

        [Event(EventId_DeserializationStop, Opcode = EventOpcode.Stop, Keywords = Keywords.Deserialization, Level = EventLevel.Informational)]
        public void DeserializationStop()
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Deserialization))
            {
                WriteEvent(EventId_DeserializationStop);
            }
        }

        [NonEvent]
        public void DeserializingObject(Type type)
        {
            Debug.Assert(type != null);

            if (IsEnabled(EventLevel.Informational, Keywords.Deserialization))
            {
                DeserializingObject(type.AssemblyQualifiedName);
            }
        }

        [Event(EventId_DeserializingObject, Keywords = Keywords.Deserialization, Level = EventLevel.Informational)]
        private void DeserializingObject(string? typeName)
        {
            WriteEvent(EventId_DeserializingObject, typeName);
        }

        public static class Keywords
        {
            public const EventKeywords Serialization = (EventKeywords)1;
            public const EventKeywords Deserialization = (EventKeywords)2;
        }
    }
}
