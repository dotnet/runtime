// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Represents an event obtained from an EventReader.
    /// </summary>
    public abstract class EventRecord : IDisposable
    {
        public abstract int Id { get; }
        public abstract byte? Version { get; }
        public abstract byte? Level { get; }
        public abstract int? Task { get; }

        public abstract short? Opcode { get; }
        public abstract long? Keywords { get; }

        public abstract long? RecordId { get; }

        public abstract string ProviderName { get; }
        public abstract Guid? ProviderId { get; }
        public abstract string LogName { get; }

        public abstract int? ProcessId { get; }
        public abstract int? ThreadId { get; }
        public abstract string MachineName { get; }
        public abstract SecurityIdentifier UserId { get; }
        public abstract DateTime? TimeCreated { get; }

        public abstract Guid? ActivityId { get; }
        public abstract Guid? RelatedActivityId { get; }
        public abstract int? Qualifiers { get; }

        public abstract string FormatDescription();
        public abstract string FormatDescription(IEnumerable<object> values);

        public abstract string LevelDisplayName { get; }

        public abstract string OpcodeDisplayName { get; }
        public abstract string TaskDisplayName { get; }
        public abstract IEnumerable<string> KeywordsDisplayNames { get; }

        public abstract EventBookmark Bookmark { get; }

        public abstract IList<EventProperty> Properties { get; }

        public abstract string ToXml();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing) { }
    }
}
