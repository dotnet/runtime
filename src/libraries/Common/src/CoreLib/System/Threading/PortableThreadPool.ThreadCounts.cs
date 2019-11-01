// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal partial class PortableThreadPool
    {
        /// <summary>
        /// Tracks information on the number of threads we want/have in different states in our thread pool.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct ThreadCounts
        {
            /// <summary>
            /// Max possible thread pool threads we want to have.
            /// </summary>
            [FieldOffset(0)]
            public short numThreadsGoal;

            /// <summary>
            /// Number of thread pool threads that currently exist.
            /// </summary>
            [FieldOffset(2)]
            public short numExistingThreads;

            /// <summary>
            /// Number of threads processing work items.
            /// </summary>
            [FieldOffset(4)]
            public short numProcessingWork;

            [FieldOffset(0)]
            private long _asLong;

            public static ThreadCounts VolatileReadCounts(ref ThreadCounts counts)
            {
                return new ThreadCounts
                {
                    _asLong = Volatile.Read(ref counts._asLong)
                };
            }

            public static ThreadCounts CompareExchangeCounts(ref ThreadCounts location, ThreadCounts newCounts, ThreadCounts oldCounts)
            {
                ThreadCounts result = new ThreadCounts
                {
                    _asLong = Interlocked.CompareExchange(ref location._asLong, newCounts._asLong, oldCounts._asLong)
                };

                if (result == oldCounts)
                {
                    result.Validate();
                    newCounts.Validate();
                }
                return result;
            }

            public static bool operator ==(ThreadCounts lhs, ThreadCounts rhs) => lhs._asLong == rhs._asLong;

            public static bool operator !=(ThreadCounts lhs, ThreadCounts rhs) => lhs._asLong != rhs._asLong;

            public override bool Equals(object? obj)
            {
                return obj is ThreadCounts counts && this._asLong == counts._asLong;
            }

            public override int GetHashCode()
            {
                return (int)(_asLong >> 8) + numThreadsGoal;
            }

            private void Validate()
            {
                Debug.Assert(numThreadsGoal > 0, "Goal must be positive");
                Debug.Assert(numExistingThreads >= 0, "Number of existing threads must be non-zero");
                Debug.Assert(numProcessingWork >= 0, "Number of threads processing work must be non-zero");
                Debug.Assert(numProcessingWork <= numExistingThreads, $"Num processing work ({numProcessingWork}) must be less than or equal to Num existing threads ({numExistingThreads})");
            }
        }
    }
}
