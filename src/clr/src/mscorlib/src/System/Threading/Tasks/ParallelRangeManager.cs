// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// Implements the algorithm for distributing loop indices to parallel loop workers
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Threading;
using System.Diagnostics.Contracts;

#pragma warning disable 0420

namespace System.Threading.Tasks
{
    /// <summary>
    /// Represents an index range
    /// </summary>
    internal struct IndexRange
    {
        // the From and To values for this range. These do not change.
        internal long m_nFromInclusive;
        internal long m_nToExclusive;

        // The shared index, stored as the offset from nFromInclusive. Using an offset rather than the actual 
        // value saves us from overflows that can happen due to multiple workers racing to increment this.
        // All updates to this field need to be interlocked.
        internal volatile Shared<long> m_nSharedCurrentIndexOffset;

        // to be set to 1 by the worker that finishes this range. It's OK to do a non-interlocked write here.
        internal int m_bRangeFinished;
    }


    /// <summary>
    /// The RangeWorker struct wraps the state needed by a task that services the parallel loop
    /// </summary>
    internal struct RangeWorker
    {
        // reference to the IndexRange array allocated by the range manager
        internal readonly IndexRange[] m_indexRanges;

        // index of the current index range that this worker is grabbing chunks from
        internal int m_nCurrentIndexRange;        

        // the step for this loop. Duplicated here for quick access (rather than jumping to rangemanager)
        internal long m_nStep;

        // increment value is the current amount that this worker will use 
        // to increment the shared index of the range it's working on
        internal long m_nIncrementValue;

        // the increment value is doubled each time this worker finds work, and is capped at this value
        internal readonly long m_nMaxIncrementValue;

        /// <summary>
        /// Initializes a RangeWorker struct
        /// </summary>
        internal RangeWorker(IndexRange[] ranges, int nInitialRange, long nStep)
        {
            m_indexRanges = ranges;
            m_nCurrentIndexRange = nInitialRange;
            m_nStep = nStep;

            m_nIncrementValue = nStep;

            m_nMaxIncrementValue = Parallel.DEFAULT_LOOP_STRIDE * nStep;
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
            int numIndexRangesToVisit = m_indexRanges.Length;

            do
            {
                // local snap to save array access bounds checks in places where we only read fields
                IndexRange currentRange = m_indexRanges[m_nCurrentIndexRange];

                if (currentRange.m_bRangeFinished == 0)
                {
                    if (m_indexRanges[m_nCurrentIndexRange].m_nSharedCurrentIndexOffset == null)
                    {
                        Interlocked.CompareExchange(ref m_indexRanges[m_nCurrentIndexRange].m_nSharedCurrentIndexOffset, new Shared<long>(0), null);
                    }

                    // this access needs to be on the array slot
                    long nMyOffset = Interlocked.Add(ref m_indexRanges[m_nCurrentIndexRange].m_nSharedCurrentIndexOffset.Value,
                                                    m_nIncrementValue) - m_nIncrementValue;

                    if (currentRange.m_nToExclusive - currentRange.m_nFromInclusive > nMyOffset)
                    {
                        // we found work

                        nFromInclusiveLocal = currentRange.m_nFromInclusive + nMyOffset;
                        nToExclusiveLocal = nFromInclusiveLocal + m_nIncrementValue;

                        // Check for going past end of range, or wrapping
                        if ( (nToExclusiveLocal > currentRange.m_nToExclusive) || (nToExclusiveLocal < currentRange.m_nFromInclusive) )
                        {
                            nToExclusiveLocal = currentRange.m_nToExclusive;
                        }

                        // We will double our unit of increment until it reaches the maximum.
                        if (m_nIncrementValue < m_nMaxIncrementValue)
                        {
                            m_nIncrementValue *= 2;
                            if (m_nIncrementValue > m_nMaxIncrementValue)
                            {
                                m_nIncrementValue = m_nMaxIncrementValue;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        // this index range is completed, mark it so that others can skip it quickly
                        Interlocked.Exchange(ref m_indexRanges[m_nCurrentIndexRange].m_bRangeFinished, 1);
                    }
                }

                // move on to the next index range, in circular order.
                m_nCurrentIndexRange = (m_nCurrentIndexRange + 1) % m_indexRanges.Length;
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

            Contract.Assert((nFromInclusiveLocal <= Int32.MaxValue) && (nFromInclusiveLocal >= Int32.MinValue) &&
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
        internal readonly IndexRange[] m_indexRanges;

        internal int m_nCurrentIndexRangeToAssign;
        internal long m_nStep;
        
        /// <summary>
        /// Initializes a RangeManager with the given loop parameters, and the desired number of outer ranges
        /// </summary>
        internal RangeManager(long nFromInclusive, long nToExclusive, long nStep, int nNumExpectedWorkers)
        {
            m_nCurrentIndexRangeToAssign = 0;
            m_nStep = nStep;

            // Our signed math breaks down w/ nNumExpectedWorkers == 1.  So change it to 2.
            if (nNumExpectedWorkers == 1)
                nNumExpectedWorkers = 2;

            //
            // calculate the size of each index range
            //

            ulong uSpan = (ulong)(nToExclusive - nFromInclusive);
            ulong uRangeSize = uSpan / (ulong) nNumExpectedWorkers; // rough estimate first
            
            uRangeSize -= uRangeSize % (ulong) nStep; // snap to multiples of nStep 
                                                      // otherwise index range transitions will derail us from nStep

            if (uRangeSize == 0)
            {
                uRangeSize = (ulong) nStep;
            }

            //
            // find the actual number of index ranges we will need
            //
            Contract.Assert((uSpan / uRangeSize) < Int32.MaxValue);

            int nNumRanges = (int)(uSpan / uRangeSize);
            
            if (uSpan % uRangeSize != 0)
            {
                nNumRanges++;
            }


            // Convert to signed so the rest of the logic works.
            // Should be fine so long as uRangeSize < Int64.MaxValue, which we guaranteed by setting #workers >= 2. 
            long nRangeSize = (long)uRangeSize; 

            // allocate the array of index ranges
            m_indexRanges = new IndexRange[nNumRanges];

            long nCurrentIndex = nFromInclusive;
            for (int i = 0; i < nNumRanges; i++)
            {
                // the fromInclusive of the new index range is always on nCurrentIndex
                m_indexRanges[i].m_nFromInclusive = nCurrentIndex;
                m_indexRanges[i].m_nSharedCurrentIndexOffset = null;
                m_indexRanges[i].m_bRangeFinished = 0;

                // now increment it to find the toExclusive value for our range
                nCurrentIndex += nRangeSize;

                // detect integer overflow or range overage and snap to nToExclusive
                if (nCurrentIndex < nCurrentIndex - nRangeSize ||
                    nCurrentIndex > nToExclusive)
                {
                    // this should only happen at the last index
                    Contract.Assert(i == nNumRanges - 1);

                    nCurrentIndex = nToExclusive;
                }

                // now that the end point of the new range is calculated, assign it.
                m_indexRanges[i].m_nToExclusive = nCurrentIndex;
            }
        }

        /// <summary>
        /// The function that needs to be called by each new worker thread servicing the parallel loop
        /// in order to get a RangeWorker struct that wraps the state for finding and executing indices
        /// </summary>
        internal RangeWorker RegisterNewWorker()
        {
            Contract.Assert(m_indexRanges != null && m_indexRanges.Length != 0);

            int nInitialRange = (Interlocked.Increment(ref m_nCurrentIndexRangeToAssign) - 1) % m_indexRanges.Length;

            return new RangeWorker(m_indexRanges, nInitialRange, m_nStep);
        }
    }
}
#pragma warning restore 0420
