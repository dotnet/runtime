// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// CDSCollectionETWBCLProvider.cs
//
// A helper class for firing ETW events related to the Coordination Data Structure
// collection types. This provider is used by CDS collections in both mscorlib.dll
// and System.dll. The purpose of sharing the provider class is to be able to enable
// ETW tracing on all CDS collection with a single ETW provider GUID, and to minimize
// the number of providers in use.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Collections.Concurrent
{
    /// <summary>Provides an event source for tracing CDS collection information.</summary>
    [EventSource(
        Name = EventSourceName,
        Guid = "35167F8E-49B2-4b96-AB86-435B59336B5E"
        )]
    internal sealed class CDSCollectionETWBCLProvider : EventSource
    {
        private const string EventSourceName = "System.Collections.Concurrent.ConcurrentCollectionsEventSource";
        /// <summary>
        /// Defines the singleton instance for the collection ETW provider.
        /// The collection provider GUID is {35167F8E-49B2-4b96-AB86-435B59336B5E}.
        /// </summary>
        public static readonly CDSCollectionETWBCLProvider Log = CreateInstance();

        private static CDSCollectionETWBCLProvider CreateInstance()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = ".ctor")]
            static extern void BaseConstructor(EventSource eventSource, Guid eventSourceGuid, string eventSourceName, EventSourceSettings settings, string[]? traits = null);

            var instance = (CDSCollectionETWBCLProvider)RuntimeHelpers.GetUninitializedObject(typeof(CDSCollectionETWBCLProvider));

            BaseConstructor(instance,
                new Guid(0x35167F8E, 0x49B2, 0x4b96, 0xAB, 0x86, 0x43, 0x5B, 0x59, 0x33, 0x6B, 0x5E),
                EventSourceName,
                EventSourceSettings.EtwManifestEventFormat);

            return instance;
        }

        /// <summary>Enabled for all keywords.</summary>
        private const EventKeywords ALL_KEYWORDS = (EventKeywords)(-1);

        //-----------------------------------------------------------------------------------
        //
        // CDS Collection Event IDs (must be unique)
        //

        private const int CONCURRENTSTACK_FASTPUSHFAILED_ID = 1;
        private const int CONCURRENTSTACK_FASTPOPFAILED_ID = 2;
        private const int CONCURRENTDICTIONARY_ACQUIRINGALLLOCKS_ID = 3;
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

        /////////////////////////////////////////////////////////////////////////////////////
        //
        // ConcurrentDictionary Events
        //

        [Event(CONCURRENTDICTIONARY_ACQUIRINGALLLOCKS_ID, Level = EventLevel.Warning)]
        public void ConcurrentDictionary_AcquiringAllLocks(int numOfBuckets)
        {
            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                WriteEvent(CONCURRENTDICTIONARY_ACQUIRINGALLLOCKS_ID, numOfBuckets);
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
