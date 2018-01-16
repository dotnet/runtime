// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Interlocked = System.Threading.Interlocked;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: Stores the metadata and event identifier corresponding
    /// to a tracelogging event type+name+tags combination.
    /// </summary>
    internal sealed class NameInfo
        : ConcurrentSetItem<KeyValuePair<string, EventTags>, NameInfo>
    {
        /// <summary>
        /// Insure that eventIds strictly less than 'eventId' will not be
        /// used by the SelfDescribing events.   
        /// </summary>
        internal static void ReserveEventIDsBelow(int eventId)
        {
            for(;;)
            {
                int snapshot = lastIdentity;
                int newIdentity = (lastIdentity & ~0xFFFFFF) + eventId;
                newIdentity = Math.Max(newIdentity, snapshot);      // Should be redundant.  as we only create descriptors once.  
                if (Interlocked.CompareExchange(ref lastIdentity, newIdentity, snapshot) == snapshot)
                    break;
            }
        }

        private static int lastIdentity = Statics.TraceLoggingChannel << 24;
        internal readonly string name;
        internal readonly EventTags tags;
        internal readonly int identity;
        internal readonly byte[] nameMetadata;

        public NameInfo(string name, EventTags tags, int typeMetadataSize)
        {
            this.name = name;
            this.tags = tags & Statics.EventTagsMask;
            this.identity = Interlocked.Increment(ref lastIdentity);

            int tagsPos = 0;
            Statics.EncodeTags((int)this.tags, ref tagsPos, null);

            this.nameMetadata = Statics.MetadataForString(name, tagsPos, 0, typeMetadataSize);

            tagsPos = 2;
            Statics.EncodeTags((int)this.tags, ref tagsPos, this.nameMetadata);
        }

        public override int Compare(NameInfo other)
        {
            return this.Compare(other.name, other.tags);
        }

        public override int Compare(KeyValuePair<string, EventTags> key)
        {
            return this.Compare(key.Key, key.Value & Statics.EventTagsMask);
        }

        private int Compare(string otherName, EventTags otherTags)
        {
            int result = StringComparer.Ordinal.Compare(this.name, otherName);
            if (result == 0 && this.tags != otherTags)
            {
                result = this.tags < otherTags ? -1 : 1;
            }
            return result;
        }
    }
}
