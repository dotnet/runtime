// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//------------------------------------------------------------------------------

using System.Globalization;

namespace System.Data.Common
{
    /// <summary>
    /// This class defines the data structure for ActvitiyId used for correlated tracing between client (bid trace event) and server (XEvent).
    /// It also includes all the APIs used to access the ActivityId. Note: ActivityId is thread based which is stored in TLS.
    /// </summary>

    internal static class ActivityCorrelator
    {
        internal class ActivityId
        {
            internal Guid Id { get; private set; }
            internal UInt32 Sequence { get; private set; }

            internal ActivityId()
            {
                this.Id = Guid.NewGuid();
                this.Sequence = 0; // the first event will start 1
            }

            // copy-constructor
            internal ActivityId(ActivityId activity)
            {
                this.Id = activity.Id;
                this.Sequence = activity.Sequence;
            }

            internal void Increment()
            {
                unchecked
                {
                    ++this.Sequence;
                }
            }

            public override string ToString()
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", this.Id, this.Sequence);
            }
        }

        // Declare the ActivityId which will be stored in TLS. The Id is unique for each thread.
        // The Sequence number will be incremented when each event happens.
        // Correlation along threads is consistent with the current XEvent mechanism at server.
        [ThreadStatic]
        private static ActivityId t_tlsActivity;

        /// <summary>
        /// Get the current ActivityId
        /// </summary>
        internal static ActivityId Current
        {
            get
            {
                if (t_tlsActivity == null)
                {
                    t_tlsActivity = new ActivityId();
                }

                return new ActivityId(t_tlsActivity);
            }
        }

        /// <summary>
        /// Increment the sequence number and generate the new ActivityId
        /// </summary>
        /// <returns>ActivityId</returns>
        internal static ActivityId Next()
        {
            if (t_tlsActivity == null)
            {
                t_tlsActivity = new ActivityId();
            }

            t_tlsActivity.Increment();

            return new ActivityId(t_tlsActivity);
        }
    }
}
