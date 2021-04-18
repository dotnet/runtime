// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace System.Xml.Serialization
{
    [EventSource(
        Name = "System.Xml.Serialzation.XmlSerialization",
        LocalizationResources = "FxResources.System.Private.Xml.SR")]
    internal sealed class XmlSerializationEventSource : EventSource
    {
        internal static XmlSerializationEventSource Log = new XmlSerializationEventSource();

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
