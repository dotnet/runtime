// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    // Places KDF work limits on the current thread.
    internal static class KdfWorkLimiter
    {
        [ThreadStatic]
        private static State? t_state;

        // Entry point: sets the iteration limit to a new value.
        internal static void SetIterationLimit(ulong workLimit)
        {
            Debug.Assert(t_state == null, "This method is not intended to be called recursively.");
            State state = new State();
            state.RemainingAllowedWork = workLimit;
            t_state = state;
        }

        internal static bool WasWorkLimitExceeded()
        {
            Debug.Assert(t_state != null, "This method should only be called within a protected block.");
            return t_state.WorkLimitWasExceeded;
        }

        // Removes any iteration limit on the current thread.
        internal static void ResetIterationLimit()
        {
            t_state = null;
        }

        // Records that we're about to perform some amount of work.
        // Overflows if the work count is exceeded.
        internal static void RecordIterations(int workCount)
        {
            RecordIterations((long)workCount);
        }

        // Records that we're about to perform some amount of work.
        // Overflows if the work count is exceeded.
        internal static void RecordIterations(long workCount)
        {
            State? state = t_state;
            if (state == null)
            {
                return;
            }

            bool success = false;

            if (workCount < 0)
            {
                throw new CryptographicException();
            }

            try
            {
                if (!state.WorkLimitWasExceeded)
                {
                    state.RemainingAllowedWork = checked(state.RemainingAllowedWork - (ulong)workCount);
                    success = true;
                }
            }
            finally
            {
                // If for any reason we failed, mark the thread as "no further work allowed" and
                // normalize to CryptographicException.
                if (!success)
                {
                    state.RemainingAllowedWork = 0;
                    state.WorkLimitWasExceeded = true;
                    throw new CryptographicException();
                }
            }
        }

        private sealed class State
        {
            internal ulong RemainingAllowedWork;
            internal bool WorkLimitWasExceeded;
        }
    }
}
