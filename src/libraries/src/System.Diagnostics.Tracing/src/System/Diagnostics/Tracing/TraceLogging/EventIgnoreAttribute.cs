using System;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// Used when authoring types that will be passed to EventSource.Write.
    /// By default, EventSource.Write will write all of an object's public
    /// properties to the event payload. Apply [EventIgnore] to a public
    /// property to prevent EventSource.Write from including the property in
    /// the event.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class EventIgnoreAttribute
        : Attribute
    {
    }
}
