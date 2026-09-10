// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

namespace Microsoft.Diagnostics.NETCore.Client
{
    /// <summary>
    /// An optional per-provider filter on Event IDs, applied by the runtime after the keyword/level
    /// filter. Requires a target runtime that supports CollectTracing5 (.NET 10+).
    /// </summary>
    public sealed class EventPipeProviderEventFilter
    {
        /// <param name="enable">
        /// When true, <paramref name="eventIds"/> is an allow-list: only those Event IDs are enabled.
        /// When false, it is a deny-list: every Event ID except those is enabled (so an empty list with
        /// enable=false enables all events).
        /// </param>
        /// <param name="eventIds">The Event IDs to enable or disable, per <paramref name="enable"/>.</param>
        public EventPipeProviderEventFilter(bool enable, IReadOnlyList<uint> eventIds)
        {
            Enable = enable;
            EventIds = eventIds ?? (IReadOnlyList<uint>)System.Array.Empty<uint>();
        }

        public bool Enable { get; }

        public IReadOnlyList<uint> EventIds { get; }
    }

    public sealed class EventPipeProvider
    {
        public EventPipeProvider(string name, EventLevel eventLevel, long keywords = 0xF00000000000, IDictionary<string, string> arguments = null)
            : this(name, eventLevel, keywords, arguments, eventFilter: null)
        {
        }

        /// <summary>
        /// Creates a provider that additionally filters which Event IDs are enabled. Using this overload
        /// starts the session with CollectTracing5 (requires a .NET 10+ target runtime).
        /// </summary>
        /// <param name="name">The provider name.</param>
        /// <param name="eventLevel">The verbosity level to enable.</param>
        /// <param name="keywords">A bitmask of keywords to enable.</param>
        /// <param name="arguments">Optional provider arguments, or null.</param>
        /// <param name="eventFilter">The per-provider Event ID filter applied after the keyword/level filter.</param>
        public EventPipeProvider(string name, EventLevel eventLevel, long keywords, IDictionary<string, string> arguments, EventPipeProviderEventFilter eventFilter)
        {
            Name = name;
            EventLevel = eventLevel;
            Keywords = keywords;
            Arguments = arguments;
            EventFilter = eventFilter;
        }

        public long Keywords { get; }

        public EventLevel EventLevel { get; }

        public string Name { get; }

        public IDictionary<string, string> Arguments { get; }

        /// <summary>
        /// An optional filter on this provider's Event IDs, applied after the keyword/level filter.
        /// Setting it causes the session to be started with CollectTracing5 (requires a .NET 10+ target).
        /// </summary>
        public EventPipeProviderEventFilter EventFilter { get; }

        public override string ToString()
        {
            return $"{Name}:0x{Keywords:X16}:{(uint)EventLevel}{(Arguments == null ? "" : $":{GetArgumentString()}")}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return this == (EventPipeProvider)obj;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            hash ^= Name.GetHashCode();
            hash ^= Keywords.GetHashCode();
            hash ^= EventLevel.GetHashCode();
            hash ^= GetArgumentString().GetHashCode();
            return hash;
        }

        public static bool operator ==(EventPipeProvider left, EventPipeProvider right)
        {
            return left.ToString() == right.ToString();
        }

        public static bool operator !=(EventPipeProvider left, EventPipeProvider right)
        {
            return !(left == right);
        }

        internal string GetArgumentString()
        {
            if (Arguments == null)
            {
                return "";
            }
            return string.Join(";", Arguments.Select(a => {
                string escapedKey = a.Key.Contains(';') || a.Key.Contains('=') ? $"\"{a.Key}\"" : a.Key;
                string escapedValue = a.Value.Contains(';') || a.Value.Contains('=') ? $"\"{a.Value}\"" : a.Value;
                return $"{escapedKey}={escapedValue}";
            }));
        }

    }
}
