// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ParallelState.cs
//
//
// A non-generic and generic parallel state class, used by the Parallel helper class
// for parallel loop management.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Security.Permissions;
using System.Diagnostics.Contracts;

// Prevents compiler warnings/errors regarding the use of ref params in Interlocked methods
#pragma warning disable 0420

namespace System.Threading.Tasks
{

    /// <summary>
    /// Enables iterations of <see cref="T:System.Threading.Tasks.Parallel"/> loops to interact with
    /// other iterations.
    /// </summary>
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    [DebuggerDisplay("ShouldExitCurrentIteration = {ShouldExitCurrentIteration}")]
    public class ParallelLoopState
    {
        // Derived classes will track a ParallelStateFlags32 or ParallelStateFlags64.
        // So this is slightly redundant, but it enables us to implement some 
        // methods in this base class.
        private ParallelLoopStateFlags m_flagsBase;

        internal ParallelLoopState(ParallelLoopStateFlags fbase)
        {
            m_flagsBase = fbase;
        }

        /// <summary>
        /// Internal/virtual support for ShouldExitCurrentIteration.
        /// </summary>
        internal virtual bool InternalShouldExitCurrentIteration 
        { 
            get 
            { 
                Contract.Assert(false);
                throw new NotSupportedException(
                    Environment.GetResourceString("ParallelState_NotSupportedException_UnsupportedMethod"));
            } 
        }

        /// <summary>
        /// Gets whether the current iteration of the loop should exit based
        /// on requests made by this or other iterations.
        /// </summary>
        /// <remarks>
        /// When an iteration of a loop calls <see cref="Break()"/> or <see cref="Stop()"/>, or
        /// when one throws an exception, or when the loop is canceled, the <see cref="Parallel"/> class will proactively
        /// attempt to prohibit additional iterations of the loop from starting execution.
        /// However, there may be cases where it is unable to prevent additional iterations from starting.
        /// It may also be the case that a long-running iteration has already begun execution.  In such
        /// cases, iterations may explicitly check the <see cref="ShouldExitCurrentIteration"/> property and
        /// cease execution if the property returns true.
        /// </remarks>
        public bool ShouldExitCurrentIteration
        {
            get
            {
                return InternalShouldExitCurrentIteration;
            }
        }
    
        /// <summary>
        /// Gets whether any iteration of the loop has called <see cref="Stop()"/>.
        /// </summary>
        public bool IsStopped 
        { 
            get 
            {
                return ((m_flagsBase.LoopStateFlags & ParallelLoopStateFlags.PLS_STOPPED) != 0); 
            } 
        }

        /// <summary>
        /// Gets whether any iteration of the loop has thrown an exception that went unhandled by that
        /// iteration.
        /// </summary>
        public bool IsExceptional 
        { 
            get
            {
                return ((m_flagsBase.LoopStateFlags & ParallelLoopStateFlags.PLS_EXCEPTIONAL) != 0);
            }
        }

        /// <summary>
        /// Internal/virtual support for LowestBreakIteration.
        /// </summary>
        internal virtual long? InternalLowestBreakIteration
        {
            get
            {
                Contract.Assert(false);
                throw new NotSupportedException(
                    Environment.GetResourceString("ParallelState_NotSupportedException_UnsupportedMethod"));
            }
        }

        /// <summary>
        /// Gets the lowest iteration of the loop from which <see cref="Break()"/> was called.
        /// </summary>
        /// <remarks>
        /// If no iteration of the loop called <see cref="Break()"/>, this property will return null.
        /// </remarks>
        public long? LowestBreakIteration 
        { 
            get 
            {
                return InternalLowestBreakIteration;
            } 
        }
    
        /// <summary>
        /// Communicates that the <see cref="Parallel"/> loop should cease execution at the system's earliest
        /// convenience.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">
        /// The <see cref="Break()"/> method was previously called.  <see cref="Break()"/> and <see
        /// cref="Stop()"/> may not be used in combination by iterations of the same loop.
        /// </exception>
        /// <remarks>
        /// <para>
        /// <see cref="Stop()"/> may be used to communicate to the loop that no other iterations need be run.
        /// For long-running iterations that may already be executing, <see cref="Stop()"/> causes <see cref="IsStopped"/>
        /// to return true for all other iterations of the loop, such that another iteration may check <see
        /// cref="IsStopped"/> and exit early if it's observed to be true.
        /// </para>
        /// <para>
        /// <see cref="Stop()"/> is typically employed in search-based algorithms, where once a result is found,
        /// no other iterations need be executed.
        /// </para>
        /// </remarks>
        public void Stop()
        {
            m_flagsBase.Stop();
        }

        // Internal/virtual support for Break().
        internal virtual void InternalBreak()
        {
            Contract.Assert(false);
            throw new NotSupportedException(
                    Environment.GetResourceString("ParallelState_NotSupportedException_UnsupportedMethod"));
        }

        /// <summary>
        /// Communicates that the <see cref="Parallel"/> loop should cease execution at the system's earliest
        /// convenience of iterations beyond the current iteration.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">
        /// The <see cref="Stop()"/> method was previously called. <see cref="Break()"/> and <see cref="Stop()"/>
        /// may not be used in combination by iterations of the same loop.
        /// </exception>
        /// <remarks>
        /// <para>
        /// <see cref="Break()"/> may be used to communicate to the loop that no other iterations after the
        /// current iteration need be run. For example, if <see cref="Break()"/> is called from the 100th
        /// iteration of a for loop iterating in parallel from 0 to 1000, all iterations less than 100 should
        /// still be run, but the iterations from 101 through to 1000 are not necessary.
        /// </para>
        /// <para>
        /// For long-running iterations that may already be executing, <see cref="Break()"/> causes <see
        /// cref="LowestBreakIteration"/>
        /// to be set to the current iteration's index if the current index is less than the current value of
        /// <see cref="LowestBreakIteration"/>.
        /// </para>
        /// <para>
        /// <see cref="Break()"/> is typically employed in search-based algorithms where an ordering is
        /// present in the data source.
        /// </para>
        /// </remarks>
        public void Break()
        {
            InternalBreak();
        }

        // Helper method to avoid repeating Break() logic between ParallelState32 and ParallelState32<TLocal>
        internal static void Break(int iteration, ParallelLoopStateFlags32 pflags)
        {
            int oldValue = ParallelLoopStateFlags.PLS_NONE;

            // Attempt to change state from "not stopped or broken or canceled or exceptional" to "broken".
            if (!pflags.AtomicLoopStateUpdate(ParallelLoopStateFlags.PLS_BROKEN,
                                             ParallelLoopStateFlags.PLS_STOPPED | ParallelLoopStateFlags.PLS_EXCEPTIONAL | ParallelLoopStateFlags.PLS_CANCELED,
                                             ref oldValue))
            {

                // If we were already stopped, we have a problem
                if ((oldValue & ParallelLoopStateFlags.PLS_STOPPED) != 0)
                {
                    throw new InvalidOperationException(
                        Environment.GetResourceString("ParallelState_Break_InvalidOperationException_BreakAfterStop"));
                }
                else
                {
                    // Apparently we previously got cancelled or became exceptional. No action necessary
                    return;
                }
            }

            // replace shared LowestBreakIteration with CurrentIteration, but only if CurrentIteration
            // is less than LowestBreakIteration.
            int oldLBI = pflags.m_lowestBreakIteration;
            if (iteration < oldLBI)
            {
                SpinWait wait = new SpinWait();
                while (Interlocked.CompareExchange(
                    ref pflags.m_lowestBreakIteration,
                        iteration,
                        oldLBI) != oldLBI)
                {
                    wait.SpinOnce();
                    oldLBI = pflags.m_lowestBreakIteration;
                    if (iteration > oldLBI) break;
                }
            }

        }

        // Helper method to avoid repeating Break() logic between ParallelState64 and ParallelState64<TLocal>
        internal static void Break(long iteration, ParallelLoopStateFlags64 pflags)
        {
            int oldValue = ParallelLoopStateFlags.PLS_NONE;

            // Attempt to change state from "not stopped or broken or canceled or exceptional" to "broken".
            if (!pflags.AtomicLoopStateUpdate(ParallelLoopStateFlags.PLS_BROKEN,
                                             ParallelLoopStateFlags.PLS_STOPPED | ParallelLoopStateFlags.PLS_EXCEPTIONAL | ParallelLoopStateFlags.PLS_CANCELED,
                                             ref oldValue))
            {

                // If we were already stopped, we have a problem
                if ((oldValue & ParallelLoopStateFlags.PLS_STOPPED) != 0)
                {
                    throw new InvalidOperationException(
                        Environment.GetResourceString("ParallelState_Break_InvalidOperationException_BreakAfterStop"));
                }
                else
                {
                    // Apparently we previously got cancelled or became exceptional. No action necessary
                    return;
                }
            }

            // replace shared LowestBreakIteration with CurrentIteration, but only if CurrentIteration
            // is less than LowestBreakIteration.
            long oldLBI = pflags.LowestBreakIteration;
            if (iteration < oldLBI)
            {
                SpinWait wait = new SpinWait();
                while (Interlocked.CompareExchange(
                    ref pflags.m_lowestBreakIteration,
                        iteration,
                        oldLBI) != oldLBI)
                {
                    wait.SpinOnce();
                    oldLBI = pflags.LowestBreakIteration;
                    if (iteration > oldLBI) break;
                }
            }

        }
    }

    internal class ParallelLoopState32 : ParallelLoopState
    {
        private ParallelLoopStateFlags32 m_sharedParallelStateFlags;
        private int m_currentIteration = 0;
        
        /// <summary>
        /// Internal constructor to ensure an instance isn't created by users.
        /// </summary>
        /// <param name="sharedParallelStateFlags">A flag shared among all threads participating
        /// in the execution of a certain loop.</param>
        internal ParallelLoopState32(ParallelLoopStateFlags32 sharedParallelStateFlags)
            : base(sharedParallelStateFlags)
        {
            m_sharedParallelStateFlags = sharedParallelStateFlags;
        }

        /// <summary>
        /// Tracks the current loop iteration for the owning task.
        /// This is used to compute whether or not the task should
        /// terminate early due to a Break() call.
        /// </summary>
        internal int CurrentIteration {
            get { return m_currentIteration; }
            set { m_currentIteration = value; }
        }

        /// <summary>
        /// Returns true if we should be exiting from the current iteration
        /// due to Stop(), Break() or exception.
        /// </summary>
        internal override bool InternalShouldExitCurrentIteration
        {
            get { return m_sharedParallelStateFlags.ShouldExitLoop(CurrentIteration); }
        }

        /// <summary>
        /// Returns the lowest iteration at which Break() has been called, or
        /// null if Break() has not yet been called.
        /// </summary>
        internal override long? InternalLowestBreakIteration
        {
            get {return m_sharedParallelStateFlags.NullableLowestBreakIteration; }
        }

        /// <summary>
        /// Communicates that parallel tasks should stop when they reach a specified iteration element.
        /// (which is CurrentIteration of the caller).
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">Break() called after Stop().</exception>
        /// <remarks>
        /// This is shared with all other concurrent threads in the system which are participating in the
        /// loop's execution. After calling Break(), no additional iterations will be executed on
        /// the current thread, and other worker threads will execute once they get beyond the calling iteration.
        /// </remarks>
        internal override void InternalBreak()
        {
            ParallelLoopState.Break(CurrentIteration, m_sharedParallelStateFlags);
        }
    }

    /// <summary>
    /// Allows independent iterations of a parallel loop to interact with other iterations.
    /// </summary>
    internal class ParallelLoopState64 : ParallelLoopState
    {
        private ParallelLoopStateFlags64 m_sharedParallelStateFlags;
        private long m_currentIteration = 0;
        
        /// <summary>
        /// Internal constructor to ensure an instance isn't created by users.
        /// </summary>
        /// <param name="sharedParallelStateFlags">A flag shared among all threads participating
        /// in the execution of a certain loop.</param>
        internal ParallelLoopState64(ParallelLoopStateFlags64 sharedParallelStateFlags)
            : base(sharedParallelStateFlags)
        {
            m_sharedParallelStateFlags = sharedParallelStateFlags;
        }

        /// <summary>
        /// Tracks the current loop iteration for the owning task.
        /// This is used to compute whether or not the task should
        /// terminate early due to a Break() call.
        /// </summary>
        internal long CurrentIteration 
        {
            // No interlocks needed, because this value is only accessed in a single thread.
            get {return m_currentIteration;} 
            set {m_currentIteration = value; }
        }

        /// <summary>
        /// Returns true if we should be exiting from the current iteration
        /// due to Stop(), Break() or exception.
        /// </summary>
        internal override bool InternalShouldExitCurrentIteration
        {
            get { return m_sharedParallelStateFlags.ShouldExitLoop(CurrentIteration); }
        }

        /// <summary>
        /// Returns the lowest iteration at which Break() has been called, or
        /// null if Break() has not yet been called.
        /// </summary>
        internal override long? InternalLowestBreakIteration
        {
            // We don't need to worry about torn read/write here because
            // ParallelStateFlags64.LowestBreakIteration property is protected
            // by an Interlocked.Read().
            get { return m_sharedParallelStateFlags.NullableLowestBreakIteration; }
        }

        /// <summary>
        /// Communicates that parallel tasks should stop when they reach a specified iteration element.
        /// (which is CurrentIteration of the caller).
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">Break() called after Stop().</exception>
        /// <remarks>
        /// Atomically sets shared StoppedBroken flag to BROKEN, then atomically sets shared 
        /// LowestBreakIteration to CurrentIteration, but only if CurrentIteration is less than 
        /// LowestBreakIteration.
        /// </remarks>
        internal override void InternalBreak()
        {
            ParallelLoopState.Break(CurrentIteration, m_sharedParallelStateFlags);
        }

    }

    /// <summary>
    /// State information that is common between ParallelStateFlags class
    /// and ParallelStateFlags64 class.
    /// </summary>
    internal class ParallelLoopStateFlags
    {
        internal static int PLS_NONE;
        internal static int PLS_EXCEPTIONAL = 1;
        internal static int PLS_BROKEN = 2;
        internal static int PLS_STOPPED = 4;
        internal static int PLS_CANCELED = 8;
        
        private volatile int m_LoopStateFlags = PLS_NONE;
        
        internal int LoopStateFlags
        {
            get { return m_LoopStateFlags; }
        }

        internal bool AtomicLoopStateUpdate(int newState, int illegalStates)
        {
            int oldState = 0;
            return AtomicLoopStateUpdate(newState, illegalStates, ref oldState);
        }
        
        internal bool AtomicLoopStateUpdate(int newState, int illegalStates, ref int oldState)
        {
            SpinWait sw = new SpinWait();

            do
            {
                oldState = m_LoopStateFlags;
                if ((oldState & illegalStates) != 0) return false;
                if (Interlocked.CompareExchange(ref m_LoopStateFlags, oldState | newState, oldState) == oldState)
                {
                    return true;
                }
                sw.SpinOnce();
            } while (true);

        }

        internal void SetExceptional()
        {
            // we can set the exceptional flag regardless of the state of other bits.
            AtomicLoopStateUpdate(PLS_EXCEPTIONAL, PLS_NONE);
        }

        internal void Stop()
        {
            // disallow setting of PLS_STOPPED bit only if PLS_BROKEN was already set
            if (!AtomicLoopStateUpdate(PLS_STOPPED, PLS_BROKEN))
            {
                throw new InvalidOperationException(
    Environment.GetResourceString("ParallelState_Stop_InvalidOperationException_StopAfterBreak"));
            }
        }

        // Returns true if StoppedBroken is updated to PLS_CANCELED.
        internal bool Cancel()
        {
            // we can set the canceled flag regardless of the state of other bits.
            return (AtomicLoopStateUpdate(PLS_CANCELED, PLS_NONE));
        }
    }

    /// <summary>
    /// An internal class used to share accounting information in 32-bit versions
    /// of For()/ForEach() loops.
    /// </summary>
    internal class ParallelLoopStateFlags32 : ParallelLoopStateFlags
    {
        // Records the lowest iteration at which a Break() has been called,
        // or Int32.MaxValue if no break has been called.  Used directly
        // by Break().
        internal volatile int m_lowestBreakIteration = Int32.MaxValue;

        // Not strictly necessary, but maintains consistency with ParallelStateFlags64
        internal int LowestBreakIteration
        {
            get { return m_lowestBreakIteration; }
        }

        // Does some processing to convert m_lowestBreakIteration to a long?.
        internal long? NullableLowestBreakIteration
        {
            get 
            {
                if (m_lowestBreakIteration == Int32.MaxValue) return null;
                else
                {
                    // protect against torn read of 64-bit value
                    long rval = m_lowestBreakIteration;
                    if (IntPtr.Size >= 8) return rval;
                    else return Interlocked.Read(ref rval);
                }
            }
        }

        
        /// <summary>
        /// Lets the caller know whether or not to prematurely exit the For/ForEach loop.
        /// If this returns true, then exit the loop.  Otherwise, keep going.
        /// </summary>
        /// <param name="CallerIteration">The caller's current iteration point
        /// in the loop.</param>
        /// <remarks>
        /// The loop should exit on any one of the following conditions:
        ///   (1) Stop() has been called by one or more tasks.
        ///   (2) An exception has been raised by one or more tasks.
        ///   (3) Break() has been called by one or more tasks, and
        ///       CallerIteration exceeds the (lowest) iteration at which 
        ///       Break() was called.
        ///   (4) The loop was canceled.
        /// </remarks>
        internal bool ShouldExitLoop(int CallerIteration)
        {
            int flags = LoopStateFlags;
            return (flags != PLS_NONE && ( 
                            ((flags & (PLS_EXCEPTIONAL | PLS_STOPPED | PLS_CANCELED)) != 0) ||
                            (((flags & PLS_BROKEN) != 0) && (CallerIteration > LowestBreakIteration))));
        }

        // This lighter version of ShouldExitLoop will be used when the body type doesn't contain a state.
        // Since simpler bodies cannot stop or break, we can safely skip checks for those flags here.
        internal bool ShouldExitLoop()
        {
            int flags = LoopStateFlags;
            return ((flags != PLS_NONE) && ((flags & (PLS_EXCEPTIONAL | PLS_CANCELED)) != 0));
        }
    }

    /// <summary>
    /// An internal class used to share accounting information in 64-bit versions
    /// of For()/ForEach() loops.
    /// </summary>
    internal class ParallelLoopStateFlags64 : ParallelLoopStateFlags
    {
        // Records the lowest iteration at which a Break() has been called,
        // or Int64.MaxValue if no break has been called.  Used directly
        // by Break().
        internal long m_lowestBreakIteration = Int64.MaxValue;

        // Performs a conditionally interlocked read of m_lowestBreakIteration.
        internal long LowestBreakIteration
        {
            get
            {
                if (IntPtr.Size >= 8) return m_lowestBreakIteration;
                else return Interlocked.Read(ref m_lowestBreakIteration);
            }
        }

        // Does some processing to convert m_lowestBreakIteration to a long?.
        internal long? NullableLowestBreakIteration
        {
            get
            {
                if (m_lowestBreakIteration == Int64.MaxValue) return null;
                else
                {
                    if (IntPtr.Size >= 8) return m_lowestBreakIteration;
                    else return Interlocked.Read(ref m_lowestBreakIteration);
                }
            }
        }

        /// <summary>
        /// Lets the caller know whether or not to prematurely exit the For/ForEach loop.
        /// If this returns true, then exit the loop.  Otherwise, keep going.
        /// </summary>
        /// <param name="CallerIteration">The caller's current iteration point
        /// in the loop.</param>
        /// <remarks>
        /// The loop should exit on any one of the following conditions:
        ///   (1) Stop() has been called by one or more tasks.
        ///   (2) An exception has been raised by one or more tasks.
        ///   (3) Break() has been called by one or more tasks, and
        ///       CallerIteration exceeds the (lowest) iteration at which 
        ///       Break() was called.
        ///   (4) The loop has been canceled.
        /// </remarks>
        internal bool ShouldExitLoop(long CallerIteration)
        {
            int flags = LoopStateFlags;
            return (flags != PLS_NONE && (
                            ((flags & (PLS_EXCEPTIONAL | PLS_STOPPED | PLS_CANCELED)) != 0) ||
                            (((flags & PLS_BROKEN) != 0) && (CallerIteration > LowestBreakIteration))));
        }

        // This lighter version of ShouldExitLoop will be used when the body type doesn't contain a state.
        // Since simpler bodies cannot stop or break, we can safely skip checks for those flags here.
        internal bool ShouldExitLoop()
        {
            int flags = LoopStateFlags;
            return ((flags != PLS_NONE) && ((flags & (PLS_EXCEPTIONAL | PLS_CANCELED)) != 0));
        }
    }

    /// <summary>
    /// Provides completion status on the execution of a <see cref="Parallel"/> loop.
    /// </summary>
    /// <remarks>
    /// If <see cref="IsCompleted"/> returns true, then the loop ran to completion, such that all iterations
    /// of the loop were executed. If <see cref="IsCompleted"/> returns false and <see
    /// cref="LowestBreakIteration"/> returns null, a call to <see
    /// cref="System.Threading.Tasks.ParallelLoopState.Stop"/> was used to end the loop prematurely. If <see
    /// cref="IsCompleted"/> returns false and <see cref="LowestBreakIteration"/> returns a non-null integral
    /// value, <see cref="System.Threading.Tasks.ParallelLoopState.Break()"/> was used to end the loop prematurely.
    /// </remarks>
    public struct ParallelLoopResult
    {
        internal bool m_completed;
        internal long? m_lowestBreakIteration;

        /// <summary>
        /// Gets whether the loop ran to completion, such that all iterations of the loop were executed
        /// and the loop didn't receive a request to end prematurely.
        /// </summary>
        public bool IsCompleted { get { return m_completed; } }

        /// <summary>
        /// Gets the index of the lowest iteration from which <see
        /// cref="System.Threading.Tasks.ParallelLoopState.Break()"/>
        /// was called.
        /// </summary>
        /// <remarks>
        /// If <see cref="System.Threading.Tasks.ParallelLoopState.Break()"/> was not employed, this property will
        /// return null.
        /// </remarks>
        public long? LowestBreakIteration { get { return m_lowestBreakIteration; } }
    }

}

#pragma warning restore 0420
