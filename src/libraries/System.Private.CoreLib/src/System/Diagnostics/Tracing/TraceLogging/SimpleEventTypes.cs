// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// TraceLogging: Contains the metadata needed to emit an event, optimized
    /// for events with one top-level compile-time-typed payload object.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the top-level payload object. Should be EmptyStruct if the
    /// event has no payload.
    /// </typeparam>
    internal static class SimpleEventTypes<T>
    {
        private static TraceLoggingEventTypes? instance;

        public static TraceLoggingEventTypes Instance
        {
            [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource WriteEvent will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
            get { return instance ??= InitInstance(); }
        }

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource WriteEvent will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
        private static TraceLoggingEventTypes InitInstance()
        {
            var info = TraceLoggingTypeInfo.GetInstance(typeof(T), null);
            var newInstance = new TraceLoggingEventTypes(info.Name, info.Tags, new TraceLoggingTypeInfo[] { info });
            Interlocked.CompareExchange(ref instance, newInstance, null);
            Debug.Assert(instance != null);
            return instance;
        }
    }
}
