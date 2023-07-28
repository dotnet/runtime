// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// A non-generic and generic parallel state class, used by the Parallel helper class
// for parallel loop management.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

// Prevents compiler warnings/errors regarding the use of ref params in Interlocked methods

namespace System.Threading.Tasks
{
    /// <summary>
    /// Enables iterations of <see cref="System.Threading.Tasks.Parallel"/> loops to interact with
    /// other iterations.
    /// </summary>
    [DebuggerDisplay("ShouldExitCurrentIteration = {ShouldExitCurrentIteration}")]
    public class ParallelLoopState
    {
        // Derived classes will track a ParallelLoopStateFlags<Int>.
        // So this is slightly redundant, but it enables us to implement some
        // methods in this base class.
        private readonly ParallelLoopStateFlags _flagsBase;

        internal ParallelLoopState(ParallelLoopStateFlags fbase)
        {
            _flagsBase = fbase;
        }

        /// <summary>
        /// Internal/virtual support for ShouldExitCurrentIteration.
        /// </summary>
        internal virtual bool InternalShouldExitCurrentIteration
        {
            get
            {
                Debug.Fail(SR.ParallelState_NotSupportedException_UnsupportedMethod);
                throw new NotSupportedException(
                    SR.ParallelState_NotSupportedException_UnsupportedMethod);
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
                return ((_flagsBase.LoopStateFlags & ParallelLoopStateFlags.ParallelLoopStateStopped) != 0);
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
                return ((_flagsBase.LoopStateFlags & ParallelLoopStateFlags.ParallelLoopStateExceptional) != 0);
            }
        }

        /// <summary>
        /// Internal/virtual support for LowestBreakIteration.
        /// </summary>
        internal virtual long? InternalLowestBreakIteration
        {
            get
            {
                Debug.Fail(SR.ParallelState_NotSupportedException_UnsupportedMethod);
                throw new NotSupportedException(
                    SR.ParallelState_NotSupportedException_UnsupportedMethod);
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
        /// <exception cref="System.InvalidOperationException">
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
            _flagsBase.Stop();
        }

        // Internal/virtual support for Break().
        internal virtual void InternalBreak()
        {
            Debug.Fail(SR.ParallelState_NotSupportedException_UnsupportedMethod);
            throw new NotSupportedException(
                    SR.ParallelState_NotSupportedException_UnsupportedMethod);
        }

        /// <summary>
        /// Communicates that the <see cref="Parallel"/> loop should cease execution at the system's earliest
        /// convenience of iterations beyond the current iteration.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
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

        internal static void Break<TInt>(TInt iteration, ParallelLoopStateFlags<TInt> pflags) where TInt : struct, IBinaryInteger<TInt>, IMinMaxValue<TInt>
        {
            Debug.Assert(typeof(TInt) == typeof(int) || typeof(TInt) == typeof(long));

            int oldValue = ParallelLoopStateFlags.ParallelLoopStateNone;

            // Attempt to change state from "not stopped or broken or canceled or exceptional" to "broken".
            if (!pflags.AtomicLoopStateUpdate(ParallelLoopStateFlags.ParallelLoopStateBroken,
                                             ParallelLoopStateFlags.ParallelLoopStateStopped | ParallelLoopStateFlags.ParallelLoopStateExceptional | ParallelLoopStateFlags.ParallelLoopStateCanceled,
                                             ref oldValue))
            {
                // If we were already stopped, we have a problem
                if ((oldValue & ParallelLoopStateFlags.ParallelLoopStateStopped) != 0)
                {
                    throw new InvalidOperationException(
                        SR.ParallelState_Break_InvalidOperationException_BreakAfterStop);
                }
                else
                {
                    // Apparently we previously got cancelled or became exceptional. No action necessary
                    return;
                }
            }

            // replace shared LowestBreakIteration with CurrentIteration, but only if CurrentIteration
            // is less than LowestBreakIteration.
            TInt oldLBI = pflags.LowestBreakIteration;
            if (iteration < oldLBI)
            {
                SpinWait wait = default;
                while (typeof(TInt) == typeof(int) ?
                    Interlocked.CompareExchange(ref Unsafe.As<TInt, int>(ref pflags._lowestBreakIteration), Unsafe.As<TInt, int>(ref iteration), Unsafe.As<TInt, int>(ref oldLBI)) != Unsafe.As<TInt, int>(ref oldLBI) :
                    Interlocked.CompareExchange(ref Unsafe.As<TInt, long>(ref pflags._lowestBreakIteration), Unsafe.As<TInt, long>(ref iteration), Unsafe.As<TInt, long>(ref oldLBI)) != Unsafe.As<TInt, long>(ref oldLBI))
                {
                    wait.SpinOnce();
                    oldLBI = pflags.LowestBreakIteration;
                    if (iteration > oldLBI) break;
                }
            }
        }
    }

    internal sealed class ParallelLoopState<TInt> : ParallelLoopState where TInt : struct, IBinaryInteger<TInt>, IMinMaxValue<TInt>
    {
        private readonly ParallelLoopStateFlags<TInt> _sharedParallelStateFlags;
        private TInt _currentIteration;

        /// <summary>
        /// Internal constructor to ensure an instance isn't created by users.
        /// </summary>
        /// <param name="sharedParallelStateFlags">A flag shared among all threads participating
        /// in the execution of a certain loop.</param>
        internal ParallelLoopState(ParallelLoopStateFlags<TInt> sharedParallelStateFlags)
            : base(sharedParallelStateFlags)
        {
            Debug.Assert(typeof(TInt) == typeof(int) || typeof(TInt) == typeof(long));
            _sharedParallelStateFlags = sharedParallelStateFlags;
        }

        /// <summary>
        /// Tracks the current loop iteration for the owning task.
        /// This is used to compute whether or not the task should
        /// terminate early due to a Break() call.
        /// </summary>
        internal TInt CurrentIteration
        {
            get { return _currentIteration; }
            set { _currentIteration = value; }
        }

        /// <summary>
        /// Returns true if we should be exiting from the current iteration
        /// due to Stop(), Break() or exception.
        /// </summary>
        internal override bool InternalShouldExitCurrentIteration
        {
            get { return _sharedParallelStateFlags.ShouldExitLoop(CurrentIteration); }
        }

        /// <summary>
        /// Returns the lowest iteration at which Break() has been called, or
        /// null if Break() has not yet been called.
        /// </summary>
        internal override long? InternalLowestBreakIteration
        {
            get { return _sharedParallelStateFlags.NullableLowestBreakIteration; }
        }

        /// <summary>
        /// Communicates that parallel tasks should stop when they reach a specified iteration element.
        /// (which is CurrentIteration of the caller).
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Break() called after Stop().</exception>
        /// <remarks>
        /// This is shared with all other concurrent threads in the system which are participating in the
        /// loop's execution. After calling Break(), no additional iterations will be executed on
        /// the current thread, and other worker threads will execute once they get beyond the calling iteration.
        /// </remarks>
        internal override void InternalBreak()
        {
            Break(CurrentIteration, _sharedParallelStateFlags);
        }
    }

    /// <summary>
    /// State information that is common between ParallelLoopStateFlags{TInt} types.
    /// </summary>
    internal class ParallelLoopStateFlags
    {
        internal const int ParallelLoopStateNone = 0;
        internal const int ParallelLoopStateExceptional = 1;
        internal const int ParallelLoopStateBroken = 2;
        internal const int ParallelLoopStateStopped = 4;
        internal const int ParallelLoopStateCanceled = 8;

        private volatile int _loopStateFlags = ParallelLoopStateNone;

        internal int LoopStateFlags
        {
            get { return _loopStateFlags; }
        }

        internal bool AtomicLoopStateUpdate(int newState, int illegalStates)
        {
            int oldState = 0;
            return AtomicLoopStateUpdate(newState, illegalStates, ref oldState);
        }

        internal bool AtomicLoopStateUpdate(int newState, int illegalStates, ref int oldState)
        {
            SpinWait sw = default;

            do
            {
                oldState = _loopStateFlags;
                if ((oldState & illegalStates) != 0) return false;
                if (Interlocked.CompareExchange(ref _loopStateFlags, oldState | newState, oldState) == oldState)
                {
                    return true;
                }
                sw.SpinOnce();
            } while (true);
        }

        internal void SetExceptional()
        {
            // we can set the exceptional flag regardless of the state of other bits.
            AtomicLoopStateUpdate(ParallelLoopStateExceptional, ParallelLoopStateNone);
        }

        internal void Stop()
        {
            // disallow setting of ParallelLoopStateStopped bit only if ParallelLoopStateBroken was already set
            if (!AtomicLoopStateUpdate(ParallelLoopStateStopped, ParallelLoopStateBroken))
            {
                throw new InvalidOperationException(SR.ParallelState_Stop_InvalidOperationException_StopAfterBreak);
            }
        }

        // Returns true if StoppedBroken is updated to ParallelLoopStateCanceled.
        internal bool Cancel()
        {
            // we can set the canceled flag regardless of the state of other bits.
            return (AtomicLoopStateUpdate(ParallelLoopStateCanceled, ParallelLoopStateNone));
        }
    }

    internal sealed unsafe class ParallelLoopStateFlags<TInt> : ParallelLoopStateFlags
        where TInt : struct, IBinaryInteger<TInt>, IMinMaxValue<TInt>
    {
        // Records the lowest iteration at which a Break() has been called,
        // or TInt.MaxValue if no break has been called.
        internal TInt _lowestBreakIteration = TInt.MaxValue;

        internal TInt LowestBreakIteration
        {
            get
            {
                if (typeof(TInt) == typeof(int))
                {
                    return Unsafe.BitCast<int, TInt>(Volatile.Read(ref Unsafe.As<TInt, int>(ref _lowestBreakIteration)));
                }

                Debug.Assert(typeof(TInt) == typeof(long));
                return Unsafe.BitCast<long, TInt>(Volatile.Read(ref Unsafe.As<TInt, long>(ref _lowestBreakIteration)));
            }
        }

        // Does some processing to convert _lowestBreakIteration to a long?.
        internal long? NullableLowestBreakIteration
        {
            get
            {
                TInt lowestBreakIteration = LowestBreakIteration;
                return lowestBreakIteration == TInt.MaxValue ? null : long.CreateTruncating(lowestBreakIteration);
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
        internal bool ShouldExitLoop(TInt CallerIteration)
        {
            int flags = LoopStateFlags;
            return (flags != ParallelLoopStateNone && (
                            ((flags & (ParallelLoopStateExceptional | ParallelLoopStateStopped | ParallelLoopStateCanceled)) != 0) ||
                            (((flags & ParallelLoopStateBroken) != 0) && (CallerIteration > LowestBreakIteration))));
        }

        // This lighter version of ShouldExitLoop will be used when the body type doesn't contain a state.
        // Since simpler bodies cannot stop or break, we can safely skip checks for those flags here.
        internal bool ShouldExitLoop()
        {
            int flags = LoopStateFlags;
            return ((flags != ParallelLoopStateNone) && ((flags & (ParallelLoopStateExceptional | ParallelLoopStateCanceled)) != 0));
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
        internal bool _completed;
        internal long? _lowestBreakIteration;

        /// <summary>
        /// Gets whether the loop ran to completion, such that all iterations of the loop were executed
        /// and the loop didn't receive a request to end prematurely.
        /// </summary>
        public bool IsCompleted { get { return _completed; } }

        /// <summary>
        /// Gets the index of the lowest iteration from which <see
        /// cref="System.Threading.Tasks.ParallelLoopState.Break()"/>
        /// was called.
        /// </summary>
        /// <remarks>
        /// If <see cref="System.Threading.Tasks.ParallelLoopState.Break()"/> was not employed, this property will
        /// return null.
        /// </remarks>
        public long? LowestBreakIteration { get { return _lowestBreakIteration; } }
    }
}
