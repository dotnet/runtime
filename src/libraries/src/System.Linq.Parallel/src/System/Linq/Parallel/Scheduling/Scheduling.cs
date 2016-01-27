// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Scheduling.cs
//
// Infrastructure for setting up concurrent work, marshaling exceptions, determining
// the recommended degree-of-parallelism, and so forth.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Security;

namespace System.Linq.Parallel
{
    //-----------------------------------------------------------------------------------
    // A simple helper class that offers common task scheduling functionality.
    //

    internal static class Scheduling
    {
        // Whether to preserve order by default, when neither AsOrdered nor AsUnordered is used.
        internal const bool DefaultPreserveOrder = false;

        // The default degree of parallelism, or -1 if unspecified. Dev unit tests set this value
        // to change the default DOP.
        internal static int DefaultDegreeOfParallelism = Math.Min(Environment.ProcessorCount, MAX_SUPPORTED_DOP);

        // The size to use for bounded buffers. 
        internal const int DEFAULT_BOUNDED_BUFFER_CAPACITY = 512;

        // The number of bytes we want "chunks" to be, when partitioning, etc. We choose 4 cache
        // lines worth, assuming 128b cache line.  Most (popular) architectures use 64b cache lines,
        // but choosing 128b works for 64b too whereas a multiple of 64b isn't necessarily sufficient
        // for 128b cache systems.  So 128b it is.
        internal const int DEFAULT_BYTES_PER_CHUNK = 128 * 4;

        // The number of milliseconds before we assume a producer has been zombied.
        internal const int ZOMBIED_PRODUCER_TIMEOUT = Timeout.Infinite;

        // The largest number of partitions that PLINQ supports.
        internal const int MAX_SUPPORTED_DOP = 512;


        //-----------------------------------------------------------------------------------
        // Calculates the proper amount of DOP.  This takes into consideration dynamic nesting.
        //

        internal static int GetDefaultDegreeOfParallelism()
        {
            return DefaultDegreeOfParallelism;
        }

        //-----------------------------------------------------------------------------------
        // Gets the recommended "chunk size" for a particular CLR type.
        //
        // Notes:
        //     We try to recommend some reasonable "chunk size" for the data, but this is
        //     clearly a tradeoff, and requires a bit of experimentation. A larger chunk size
        //     can help locality, but if it's too big we may end up either stalling another
        //     partition (if enumerators are calculating data on demand) or skewing the
        //     distribution of data among the partitions.
        //

        internal static int GetDefaultChunkSize<T>()
        {
            int chunkSize;

            // Because of the lack of typeof(T).IsValueType we need two pieces of information
            // to determine this. default(T) will return a non null for Value Types, except those
            // using Nullable<>, that is why we need a second condition.
            if (default(T) != null || Nullable.GetUnderlyingType(typeof(T)) != null)
            {
                // Marshal.SizeOf fails for value types that don't have explicit layouts. We
                // just fall back to some arbitrary constant in that case. Is there a better way?
                {
                    // We choose '128' because this ensures, no matter the actual size of the value type,
                    // the total bytes used will be a multiple of 128. This ensures it's cache aligned.
                    chunkSize = 128;
                }
            }
            else
            {
                Debug.Assert((DEFAULT_BYTES_PER_CHUNK % IntPtr.Size) == 0, "bytes per chunk should be a multiple of pointer size");
                chunkSize = (DEFAULT_BYTES_PER_CHUNK / IntPtr.Size);
            }

            TraceHelpers.TraceInfo("Scheduling::GetDefaultChunkSize({0}) -- returning {1}", typeof(T), chunkSize);

            return chunkSize;
        }
    }
}
