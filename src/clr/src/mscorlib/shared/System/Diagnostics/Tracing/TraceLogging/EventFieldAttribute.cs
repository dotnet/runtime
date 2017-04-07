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
    /// Tags are flags that are not interpreted by EventSource but are passed along
    /// to the EventListener. The EventListener determines the semantics of the flags.
    /// </summary>
    [Flags]
    public enum EventFieldTags
    {
        /// <summary>
        /// No special traits are added to the field.
        /// </summary>
        None = 0,

        /* Bits below 0x10000 are available for any use by the provider. */
        /* Bits at or above 0x10000 are reserved for definition by Microsoft. */
    }

    /// <summary>
    /// TraceLogging: used when authoring types that will be passed to EventSource.Write.
    /// Controls how a field or property is handled when it is written as a
    /// field in a TraceLogging event. Apply this attribute to a field or
    /// property if the default handling is not correct. (Apply the
    /// TraceLoggingIgnore attribute if the property should not be
    /// included as a field in the event.)
    /// The default for Name is null, which means that the name of the
    /// underlying field or property will be used as the event field's name.
    /// The default for PiiTag is 0, which means that the event field does not
    /// contain personally-identifiable information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class EventFieldAttribute
        : Attribute
    {
        /// <summary>
        /// User defined options for the field. These are not interpreted by the EventSource
        /// but are available to the Listener. See EventFieldSettings for details
        /// </summary>
        public EventFieldTags Tags
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name to use for the field. This defaults to null.
        /// If null, the name of the corresponding property will be used
        /// as the event field's name.
        /// TODO REMOVE
        /// </summary>
        internal string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a field formatting hint.
        /// </summary>
        public EventFieldFormat Format
        {
            get;
            set;
        }
    }
}
