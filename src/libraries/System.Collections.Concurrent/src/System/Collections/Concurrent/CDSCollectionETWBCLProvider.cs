// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// CDSCollectionETWBCLProvider.cs
//
// A helper class for firing ETW events related to the Coordination Data Structure
// collection types.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics.Tracing;

namespace System.Collections.Concurrent
{
    /// <summary>Provides an event source for tracing CDS collection information.</summary>
    [EventSource(
        Name = "System.Collections.Concurrent.ConcurrentCollectionsEventSource",
        Guid = "35167F8E-49B2-4b96-AB86-435B59336B5E"
        )]
    internal sealed class CDSCollectionETWBCLProvider : EventSource
    {
        /// <summary>
        /// Defines the singleton instance for the collection ETW provider.
        /// The collection provider GUID is {35167F8E-49B2-4b96-AB86-435B59336B5E}.
        /// </summary>
        public static readonly CDSCollectionETWBCLProvider Log = new CDSCollectionETWBCLProvider();
        /// <summary>Prevent external instantiation.  All logging should go through the Log instance.</summary>
        private CDSCollectionETWBCLProvider() { }

        /// <summary>Enabled for all keywords.</summary>
        private const EventKeywords ALL_KEYWORDS = (EventKeywords)(-1);

        //-----------------------------------------------------------------------------------
        //
        // CDS Collection Event IDs (must be unique)
        //

        private const int CONCURRENTSTACK_FASTPUSHFAILED_ID = 1;
        private const int CONCURRENTSTACK_FASTPOPFAILED_ID = 2;
        private const int CONCURRENTBAG_TRYTAKESTEALS_ID = 4;
        private const int CONCURRENTBAG_TRYPEEKSTEALS_ID = 5;

        /////////////////////////////////////////////////////////////////////////////////////
        //
        // ConcurrentStack Events
        //

        [Event(CONCURRENTSTACK_FASTPUSHFAILED_ID, Level = EventLevel.Warning)]
        public void ConcurrentStack_FastPushFailed(int spinCount)
        {
            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                WriteEvent(CONCURRENTSTACK_FASTPUSHFAILED_ID, spinCount);
            }
        }

        [Event(CONCURRENTSTACK_FASTPOPFAILED_ID, Level = EventLevel.Warning)]
        public void ConcurrentStack_FastPopFailed(int spinCount)
        {
            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                WriteEvent(CONCURRENTSTACK_FASTPOPFAILED_ID, spinCount);
            }
        }

        //
        // Events below this point are used by the CDS types in System.DLL
        //

        /////////////////////////////////////////////////////////////////////////////////////
        //
        // ConcurrentBag Events
        //

        [Event(CONCURRENTBAG_TRYTAKESTEALS_ID, Level = EventLevel.Verbose)]
        public void ConcurrentBag_TryTakeSteals()
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                WriteEvent(CONCURRENTBAG_TRYTAKESTEALS_ID);
            }
        }

        [Event(CONCURRENTBAG_TRYPEEKSTEALS_ID, Level = EventLevel.Verbose)]
        public void ConcurrentBag_TryPeekSteals()
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                WriteEvent(CONCURRENTBAG_TRYPEEKSTEALS_ID);
            }
        }
    }
}
