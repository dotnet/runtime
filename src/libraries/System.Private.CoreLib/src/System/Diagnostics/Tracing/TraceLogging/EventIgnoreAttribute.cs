// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Tracing
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
