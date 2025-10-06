// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace System.Xml.Serialization
{
    [EventSource(Name = XmlSerializationEventSourceName)]
    internal sealed class XmlSerializationEventSource : EventSource
    {
        private const string XmlSerializationEventSourceName = "System.Xml.Serialzation.XmlSerialization";

        public XmlSerializationEventSource()
            : base(XmlSerializationEventSourceName, EventSourceSettings.EtwSelfDescribingEventFormat)
        {
        }

        internal static readonly XmlSerializationEventSource Log = new XmlSerializationEventSource();

        [Event(EventIds.XmlSerializerExpired, Level = EventLevel.Informational)]
        internal void XmlSerializerExpired(string serializerName, string type)
        {
            WriteEvent(EventIds.XmlSerializerExpired, serializerName, type);
        }

        public static class EventIds
        {
            public const int XmlSerializerExpired = 1;
        }
    }
}
