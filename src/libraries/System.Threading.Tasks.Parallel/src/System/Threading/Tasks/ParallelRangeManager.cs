// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Implements the algorithm for distributing loop indices to parallel loop workers
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Runtime.InteropServices;

#pragma warning disable 0420
namespace System.Threading.Tasks
{
    /// <summary>
    /// Represents an index range
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    internal struct IndexRange
    {
        // the From and To values for this range. These do not change.
        internal long _nFromInclusive;
        internal long _nToExclusive;

        // The shared index, stored as the offset from nFromInclusive. Using an offset rather than the actual 
        // value saves us from overflows that can happen due to multiple workers racing to increment this.
        // All updates to this field need to be interlocked.  To avoid split interlockeds across cache-lines
        // in 32-bit processes, in 32-bit processes when the range fits in a 32-bit value, we prefer to use
        // a 32-bit field, and just use the first 32-bits of the long.  And to minimize false sharing, each
        // value is stored in its own heap-allocated object, which is lazily allocated by the thread using
        // that range, minimizing the chances it'll be near the objects from other threads.
        internal volatile Box<long> _nSharedCurrentIndexOffset;

        // to be set to 1 by the worker that finishes this range. It's OK to do a non-interlocked write here.
        internal int _bRangeFinished;
    }


    /// <summary>
    /// The RangeWorker struct wraps the state needed by a task that services the parallel loop
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    internal struct RangeWorker
    {
        // reference to the IndexRange array allocated by the range manager
        internal readonly IndexRange[] _indexRanges;

        // index of the current index range that this worker is grabbing chunks from
        internal int _nCurrentIndexRange;

        // the step for this loop. Duplicated here for quick access (rather than jumping to rangemanager)
        internal long _nStep;

        // increment value is the current amount that this worker will use 
        // to increment the shared index of the range it's working on
        internal long _nIncrementValue;

        // the increment value is doubled each time this worker finds work, and is capped at this value
        internal readonly long _nMaxIncrementValue;
        
        // whether to use 32-bits or 64-bits of current index in each range
        internal readonly bool _use32BitCurrentIndex;

        internal bool IsInitialized { get { return _indexRanges != null; } }

        /// <summary>
        /// Initializes a RangeWorker struct
        /// </summary>
        internal RangeWorker(IndexRange[] ranges, int nInitialRange, long nStep, bool use32BitCurrentIndex)
        {
            _indexRanges = ranges;
            _use32BitCurrentIndex = use32BitCurrentIndex;
            _nCurrentIndexRange = nInitialRange;
            _nStep = nStep;

            _nIncrementValue = nStep;

            _nMaxIncrementValue = Parallel.DEFAULT_LOOP_STRIDE * nStep;
        }

        /// <summary>
        /// Implements the core work search algorithm that will be used for this range worker. 
        /// </summary> 
        /// 
        /// Usage pattern is:
        ///    1) the thread associated with this rangeworker calls FindNewWork
        ///    2) if we return true, the worker uses the nFromInclusiveLocal and nToExclusiveLocal values
        ///       to execute the sequential loop
        ///    3) if we return false it means there is no more work left. It's time to quit.        
        ///    
        internal bool FindNewWork(out long nFromInclusiveLocal, out long nToExclusiveLocal)
        {
            // since we iterate over index ranges circularly, we will use the
            // count of visited ranges as our exit condition
            int numIndexRangesToVisit = _indexRanges.Length;

            do
            {
                // local snap to save array access bounds checks in places where we only read fields
                IndexRange currentRange = _indexRanges[_nCurrentIndexRange];

                if (currentRange._bRangeFinished == 0)
                {
                    if (_indexRanges[_nCurrentIndexRange]._nSharedCurrentIndexOffset == null)
                    {
                        Interlocked.CompareExchange(ref _indexRanges[_nCurrentIndexRange]._nSharedCurrentIndexOffset, new Box<long>(0), null);
                    }

                    long nMyOffset;
                    if (IntPtr.Size == 4 && _use32BitCurrentIndex)
                    {
                        // In 32-bit processes, we prefer to use 32-bit interlocked operations, to avoid the possibility of doing
                        // a 64-bit interlocked when the target value crosses a cache line, as that can be super expensive.
                        // We use the first 32 bits of the Int64 index in such cases.
                        unsafe
                        {
                            fixed (long* indexPtr = &_indexRanges[_nCurrentIndexRange]._nSharedCurrentIndexOffset.Value)
                            {
                                nMyOffset = Interlocked.Add(ref *(int*)indexPtr, (int)_nIncrementValue) - _nIncrementValue;
                            }
                        }
                    }
                    else
                    {
                        nMyOffset = Interlocked.Add(ref _indexRanges[_nCurrentIndexRange]._nSharedCurrentIndexOffset.Value, _nIncrementValue) - _nIncrementValue;
                    }

                    if (currentRange._nToExclusive - currentRange._nFromInclusive > nMyOffset)
                    {
                        // we found work

                        nFromInclusiveLocal = currentRange._nFromInclusive + nMyOffset;
                        nToExclusiveLocal = unchecked(nFromInclusiveLocal + _nIncrementValue);

                        // Check for going past end of range, or wrapping
                        if ((nToExclusiveLocal > currentRange._nToExclusive) || (nToExclusiveLocal < currentRange._nFromInclusive))
                        {
                            nToExclusiveLocal = currentRange._nToExclusive;
                        }

                        // We will double our unit of increment until it reaches the maximum.
                        if (_nIncrementValue < _nMaxIncrementValue)
                        {
                            _nIncrementValue *= 2;
                            if (_nIncrementValue > _nMaxIncrementValue)
                            {
                                _nIncrementValue = _nMaxIncrementValue;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        // this index range is completed, mark it so that others can skip it quickly
                        Interlocked.Exchange(ref _indexRanges[_nCurrentIndexRange]._bRangeFinished, 1);
                    }
                }

                // move on to the next index range, in circular order.
                _nCurrentIndexRange = (_nCurrentIndexRange + 1) % _indexRanges.Length;
                numIndexRangesToVisit--;
            } while (numIndexRangesToVisit > 0);
            // we've visited all index ranges possible => there's no work remaining

            nFromInclusiveLocal = 0;
            nToExclusiveLocal = 0;

            return false;
        }


        /// <summary>
        /// 32 bit integer version of FindNewWork. Assumes the ranges were initialized with 32 bit values.
        /// </summary> 
        internal bool FindNewWork32(out int nFromInclusiveLocal32, out int nToExclusiveLocal32)
        {
            long nFromInclusiveLocal;
            long nToExclusiveLocal;

            bool bRetVal = FindNewWork(out nFromInclusiveLocal, out nToExclusiveLocal);

            Debug.Assert((nFromInclusiveLocal <= Int32.MaxValue) && (nFromInclusiveLocal >= Int32.MinValue) &&
                            (nToExclusiveLocal <= Int32.MaxValue) && (nToExclusiveLocal >= Int32.MinValue));

            // convert to 32 bit before returning
            nFromInclusiveLocal32 = (int)nFromInclusiveLocal;
            nToExclusiveLocal32 = (int)nToExclusiveLocal;

            return bRetVal;
        }
    }


    /// <summary>
    /// Represents the entire loop operation, keeping track of workers and ranges.
    /// </summary>
    /// 
    /// The usage pattern is:
    ///    1) The Parallel loop entry function (ForWorker) creates an instance of this class
    ///    2) Every thread joining to service the parallel loop calls RegisterWorker to grab a 
    ///       RangeWorker struct to wrap the state it will need to find and execute work, 
    ///       and they keep interacting with that struct until the end of the loop
    internal class RangeManager
    {
        internal readonly IndexRange[] _indexRanges;
        internal readonly bool _use32BitCurrentIndex;

        internal int _nCurrentIndexRangeToAssign;
        internal long _nStep;

        /// <summary>
        /// Initializes a RangeManager with the given loop parameters, and the desired number of outer ranges
        /// </summary>
        internal RangeManager(long nFromInclusive, long nToExclusive, long nStep, int nNumExpectedWorkers)
        {
            _nCurrentIndexRangeToAssign = 0;
            _nStep = nStep;

            // Our signed math breaks down w/ nNumExpectedWorkers == 1.  So change it to 2.
            if (nNumExpectedWorkers == 1)
                nNumExpectedWorkers = 2;

            //
            // calculate the size of each index range
            //

            ulong uSpan = (ulong)(nToExclusive - nFromInclusive);
            ulong uRangeSize = uSpan / (ulong)nNumExpectedWorkers; // rough estimate first

            uRangeSize -= uRangeSize % (ulong)nStep; // snap to multiples of nStep 
                                                     // otherwise index range transitions will derail us from nStep

            if (uRangeSize == 0)
            {
                uRangeSize = (ulong)nStep;
            }

            //
            // find the actual number of index ranges we will need
            //
            Debug.Assert((uSpan / uRangeSize) < Int32.MaxValue);

            int nNumRanges = (int)(uSpan / uRangeSize);

            if (uSpan % uRangeSize != 0)
            {
                nNumRanges++;
            }


            // Convert to signed so the rest of the logic works.
            // Should be fine so long as uRangeSize < Int64.MaxValue, which we guaranteed by setting #workers >= 2. 
            long nRangeSize = (long)uRangeSize;
            _use32BitCurrentIndex = IntPtr.Size == 4 && nRangeSize <= int.MaxValue;

            // allocate the array of index ranges
            _indexRanges = new IndexRange[nNumRanges];

            long nCurrentIndex = nFromInclusive;
            for (int i = 0; i < nNumRanges; i++)
            {
                // the fromInclusive of the new index range is always on nCurrentIndex
                _indexRanges[i]._nFromInclusive = nCurrentIndex;
                _indexRanges[i]._nSharedCurrentIndexOffset = null;
                _indexRanges[i]._bRangeFinished = 0;

                // now increment it to find the toExclusive value for our range
                nCurrentIndex = unchecked(nCurrentIndex + nRangeSize);

                // detect integer overflow or range overage and snap to nToExclusive
                if (nCurrentIndex < unchecked(nCurrentIndex - nRangeSize) ||
                    nCurrentIndex > nToExclusive)
                {
                    // this should only happen at the last index
                    Debug.Assert(i == nNumRanges - 1);

                    nCurrentIndex = nToExclusive;
                }

                // now that the end point of the new range is calculated, assign it.
                _indexRanges[i]._nToExclusive = nCurrentIndex;
            }
        }

        /// <summary>
        /// The function that needs to be called by each new worker thread servicing the parallel loop
        /// in order to get a RangeWorker struct that wraps the state for finding and executing indices
        /// </summary>
        internal RangeWorker RegisterNewWorker()
        {
            Debug.Assert(_indexRanges != null && _indexRanges.Length != 0);

            int nInitialRange = (Interlocked.Increment(ref _nCurrentIndexRangeToAssign) - 1) % _indexRanges.Length;

            return new RangeWorker(_indexRanges, nInitialRange, _nStep, _use32BitCurrentIndex);
        }
    }
}
#pragma warning restore 0420
