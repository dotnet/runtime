// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading
{
    internal sealed partial class PortableThreadPool
    {
        private CountsOfThreadsProcessingUserCallbacks _countsOfThreadsProcessingUserCallbacks;

        public void ReportThreadStatus(bool isProcessingUserCallback)
        {
            CountsOfThreadsProcessingUserCallbacks counts = _countsOfThreadsProcessingUserCallbacks;
            while (true)
            {
                CountsOfThreadsProcessingUserCallbacks newCounts = counts;
                if (isProcessingUserCallback)
                {
                    newCounts.IncrementCurrent();
                }
                else
                {
                    newCounts.DecrementCurrent();
                }

                CountsOfThreadsProcessingUserCallbacks countsBeforeUpdate =
                    _countsOfThreadsProcessingUserCallbacks.InterlockedCompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts)
                {
                    break;
                }

                counts = countsBeforeUpdate;
            }
        }

        private short GetAndResetHighWatermarkCountOfThreadsProcessingUserCallbacks()
        {
            CountsOfThreadsProcessingUserCallbacks counts = _countsOfThreadsProcessingUserCallbacks;
            while (true)
            {
                CountsOfThreadsProcessingUserCallbacks newCounts = counts;
                newCounts.ResetHighWatermark();

                CountsOfThreadsProcessingUserCallbacks countsBeforeUpdate =
                    _countsOfThreadsProcessingUserCallbacks.InterlockedCompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts || countsBeforeUpdate.HighWatermark == countsBeforeUpdate.Current)
                {
                    return countsBeforeUpdate.HighWatermark;
                }

                counts = countsBeforeUpdate;
            }
        }

        /// <summary>
        /// Tracks thread count information that is used when the <code>EnableWorkerTracking</code> config option is enabled.
        /// </summary>
        private struct CountsOfThreadsProcessingUserCallbacks
        {
            private const byte CurrentShift = 0;
            private const byte HighWatermarkShift = 16;

            private uint _data;

            private CountsOfThreadsProcessingUserCallbacks(uint data) => _data = data;

            private short GetInt16Value(byte shift) => (short)(_data >> shift);
            private void SetInt16Value(short value, byte shift) =>
                _data = (_data & ~((uint)ushort.MaxValue << shift)) | ((uint)(ushort)value << shift);

            /// <summary>
            /// Number of threads currently processing user callbacks
            /// </summary>
            public short Current => GetInt16Value(CurrentShift);

            public void IncrementCurrent()
            {
                if (Current < HighWatermark)
                {
                    _data += (uint)1 << CurrentShift;
                }
                else
                {
                    Debug.Assert(Current == HighWatermark);
                    Debug.Assert(Current != short.MaxValue);
                    _data += ((uint)1 << CurrentShift) | ((uint)1 << HighWatermarkShift);
                }
            }

            public void DecrementCurrent()
            {
                Debug.Assert(Current > 0);
                _data -= (uint)1 << CurrentShift;
            }

            /// <summary>
            /// The high-warkmark of number of threads processing user callbacks since the high-watermark was last reset
            /// </summary>
            public short HighWatermark => GetInt16Value(HighWatermarkShift);

            public void ResetHighWatermark() => SetInt16Value(Current, HighWatermarkShift);

            public CountsOfThreadsProcessingUserCallbacks InterlockedCompareExchange(
                CountsOfThreadsProcessingUserCallbacks newCounts,
                CountsOfThreadsProcessingUserCallbacks oldCounts)
            {
                return
                    new CountsOfThreadsProcessingUserCallbacks(
                        Interlocked.CompareExchange(ref _data, newCounts._data, oldCounts._data));
            }

            public static bool operator ==(
                CountsOfThreadsProcessingUserCallbacks lhs,
                CountsOfThreadsProcessingUserCallbacks rhs) => lhs._data == rhs._data;
            public static bool operator !=(
                CountsOfThreadsProcessingUserCallbacks lhs,
                CountsOfThreadsProcessingUserCallbacks rhs) => lhs._data != rhs._data;

            public override bool Equals([NotNullWhen(true)] object? obj) =>
                obj is CountsOfThreadsProcessingUserCallbacks other && _data == other._data;
            public override int GetHashCode() => (int)_data;
        }
    }
}
