// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ES_BUILD_STANDALONE
using System;
using System.Diagnostics;
#endif
using System.Threading;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
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
#if !ES_BUILD_STANDALONE
            [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource WriteEvent will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
#endif
            get { return instance ??= InitInstance(); }
        }

#if !ES_BUILD_STANDALONE
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource WriteEvent will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
#endif
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
