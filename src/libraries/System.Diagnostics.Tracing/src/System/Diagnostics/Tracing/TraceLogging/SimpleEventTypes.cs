// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Interlocked = System.Threading.Interlocked;

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
        private static TraceLoggingEventTypes instance;

        public static TraceLoggingEventTypes Instance
        {
            get { return instance ?? InitInstance(); }
        }

        private static TraceLoggingEventTypes InitInstance()
        {
            var info = TraceLoggingTypeInfo.GetInstance(typeof(T), null);
            var newInstance = new TraceLoggingEventTypes(info.Name, info.Tags, new TraceLoggingTypeInfo[] { info });
            Interlocked.CompareExchange(ref instance, newInstance, null);
            return instance;
        }
    }
}
