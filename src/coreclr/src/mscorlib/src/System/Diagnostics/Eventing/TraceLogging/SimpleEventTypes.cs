// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
    internal class SimpleEventTypes<T>
        : TraceLoggingEventTypes
    {
        private static SimpleEventTypes<T> instance;

        internal readonly TraceLoggingTypeInfo<T> typeInfo;

        private SimpleEventTypes(TraceLoggingTypeInfo<T> typeInfo)
            : base(
                typeInfo.Name,
                typeInfo.Tags,
                new TraceLoggingTypeInfo[] { typeInfo })
        {
            this.typeInfo = typeInfo;
        }

        public static SimpleEventTypes<T> Instance
        {
            get { return instance ?? InitInstance(); }
        }

        private static SimpleEventTypes<T> InitInstance()
        {
            var newInstance = new SimpleEventTypes<T>(TraceLoggingTypeInfo<T>.Instance);
            Interlocked.CompareExchange(ref instance, newInstance, null);
            return instance;
        }
    }
}
