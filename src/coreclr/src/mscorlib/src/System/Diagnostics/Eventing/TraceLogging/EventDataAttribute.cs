// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// Used when authoring types that will be passed to EventSource.Write.
    /// EventSource.Write&lt;T> only works when T is either an anonymous type
    /// or a type with an [EventData] attribute. In addition, the properties
    /// of T must be supported property types. Supported property types include
    /// simple built-in types (int, string, Guid, DateTime, DateTimeOffset,
    /// KeyValuePair, etc.), anonymous types that only contain supported types,
    /// types with an [EventData] attribute, arrays of the above, and IEnumerable
    /// of the above.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class EventDataAttribute
        : Attribute
    {
        private EventLevel level = (EventLevel)(-1);
        private EventOpcode opcode = (EventOpcode)(-1);

        /// <summary>
        /// Gets or sets the name to use if this type is used for an
        /// implicitly-named event or an implicitly-named property.
        /// 
        /// Example 1:
        /// 
        ///     EventSource.Write(null, new T()); // implicitly-named event
        ///     
        /// The name of the event will be determined as follows:
        /// 
        /// if (T has an EventData attribute and attribute.Name != null)
        ///     eventName = attribute.Name;
        /// else
        ///     eventName = typeof(T).Name;
        ///     
        /// Example 2:
        /// 
        ///     EventSource.Write(name, new { _1 = new T() }); // implicitly-named field
        ///     
        /// The name of the field will be determined as follows:
        /// 
        /// if (T has an EventData attribute and attribute.Name != null)
        ///     fieldName = attribute.Name;
        /// else
        ///     fieldName = typeof(T).Name;
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the level to use for the event.
        /// Invalid levels (outside the range 0..255) are treated as unset.
        /// Note that the Level attribute can bubble-up, i.e. if a type contains
        /// a sub-object (a field or property), and the sub-object's type has a
        /// TraceLoggingEvent attribute, the Level from the sub-object's attribute
        /// can affect the event's level.
        /// 
        /// Example: for EventSource.Write(name, options, data), the level of the
        /// event will be determined as follows:
        /// 
        /// if (options.Level has been set)
        ///     eventLevel = options.Level;
        /// else if (data.GetType() has a TraceLoggingEvent attribute and attribute.Level has been set)
        ///     eventLevel = attribute.Level;
        /// else if (a field/property contained in data has a TraceLoggingEvent attribute and attribute.Level has been set)
        ///     eventLevel = attribute.Level;
        /// else
        ///     eventLevel = EventLevel.LogAlways;
        /// </summary>
        internal EventLevel Level
        {
            get { return this.level; }
            set { this.level = value; }
        }

        /// <summary>
        /// Gets or sets the opcode to use for the event.
        /// Invalid opcodes (outside the range 0..255) are treated as unset.
        /// Note that the Opcode attribute can bubble-up, i.e. if a type contains
        /// a sub-object (a field or property), and the sub-object's type has a
        /// TraceLoggingEvent attribute, the Opcode from the sub-object's attribute
        /// can affect the event's opcode.
        /// 
        /// Example: for EventSource.Write(name, options, data), the opcode of the
        /// event will be determined as follows:
        /// 
        /// if (options.Opcode has been set)
        ///     eventOpcode = options.Opcode;
        /// else if (data.GetType() has a TraceLoggingEvent attribute and attribute.Opcode has been set)
        ///     eventOpcode = attribute.Opcode;
        /// else if (a field/property contained in data has a TraceLoggingEvent attribute and attribute.Opcode has been set)
        ///     eventOpcode = attribute.Opcode;
        /// else
        ///     eventOpcode = EventOpcode.Info;
        /// </summary>
        internal EventOpcode Opcode
        {
            get { return this.opcode; }
            set { this.opcode = value; }
        }

        /// <summary>
        /// Gets or sets the keywords to use for the event.
        /// Note that the Keywords attribute can bubble-up, i.e. if a type contains
        /// a sub-object (a field or property), and the sub-object's type has a
        /// TraceLoggingEvent attribute, the Keywords from the sub-object's attribute
        /// can affect the event's keywords.
        /// 
        /// Example: for EventSource.Write(name, options, data), the keywords of the
        /// event will be determined as follows:
        /// 
        /// eventKeywords = options.Keywords;
        /// if (data.GetType() has a TraceLoggingEvent attribute)
        ///     eventKeywords |= attribute.Keywords;
        /// if (a field/property contained in data has a TraceLoggingEvent attribute)
        ///     eventKeywords |= attribute.Keywords;
        /// </summary>
        internal EventKeywords Keywords
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the flags for an event. These flags are ignored by ETW,
        /// but can have meaning to the event consumer.
        /// </summary>
        internal EventTags Tags
        {
            get;
            set;
        }
    }
}
