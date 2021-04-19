// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ES_BUILD_STANDALONE
using System;
using System.Diagnostics;
#endif
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// DiagnosticCounter is an abstract class that serves as the parent class for various Counter* classes,
    /// namely EventCounter, PollingCounter, IncrementingEventCounter, and IncrementingPollingCounter.
    /// </summary>
#if NETCOREAPP
    [UnsupportedOSPlatform("browser")]
#endif
    public abstract class DiagnosticCounter : IDisposable
    {
        /// <summary>
        /// All Counters live as long as the EventSource that they are attached to unless they are
        /// explicitly Disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="eventSource">The event source.</param>
        internal DiagnosticCounter(string name, EventSource eventSource)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(Name));
            }

            if (eventSource == null)
            {
                throw new ArgumentNullException(nameof(EventSource));
            }

            Name = name;
            EventSource = eventSource;
        }

        /// <summary>Adds the counter to the set that the EventSource will report on.</summary>
        /// <remarks>
        /// Must only be invoked once, and only after the instance has been fully initialized.
        /// This should be invoked by a derived type's ctor as the last thing it does.
        /// </remarks>
        private protected void Publish()
        {
            Debug.Assert(_group is null);
            Debug.Assert(Name != null);
            Debug.Assert(EventSource != null);

            _group = CounterGroup.GetCounterGroup(EventSource);
            _group.Add(this);
        }

        /// <summary>
        /// Removes the counter from set that the EventSource will report on.  After being disposed, this
        /// counter will do nothing and its resource will be reclaimed if all references to it are removed.
        /// If an EventCounter is not explicitly disposed it will be cleaned up automatically when the
        /// EventSource it is attached to dies.
        /// </summary>
        public void Dispose()
        {
            if (_group != null)
            {
                _group.Remove(this);
                _group = null;
            }
        }

        /// <summary>
        /// Adds a key-value metadata to the EventCounter that will be included as a part of the payload
        /// </summary>
        public void AddMetadata(string key, string? value)
        {
            lock (this)
            {
                _metadata ??= new Dictionary<string, string?>();
                _metadata.Add(key, value);
            }
        }

        private string _displayName = "";
        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(DisplayName));
                _displayName = value;
            }
        }

        private string _displayUnits = "";
        public string DisplayUnits
        {
            get => _displayUnits;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(DisplayUnits));
                _displayUnits = value;
            }
        }

        public string Name { get; }

        public EventSource EventSource { get; }

        #region private implementation

        private CounterGroup? _group;
        private Dictionary<string, string?>? _metadata;

        internal abstract void WritePayload(float intervalSec, int pollingIntervalMillisec);

        internal void ReportOutOfBandMessage(string message)
        {
            EventSource.ReportOutOfBandMessage(message);
        }

        internal string GetMetadataString()
        {
            Debug.Assert(Monitor.IsEntered(this));

            if (_metadata == null)
            {
                return "";
            }

            // The dictionary is only initialized to non-null when there's metadata to add, and no items
            // are ever removed, so if the dictionary is non-null, there must also be at least one element.
            Dictionary<string, string?>.Enumerator enumerator = _metadata.GetEnumerator();
            Debug.Assert(_metadata.Count > 0);
            bool gotOne = enumerator.MoveNext();
            Debug.Assert(gotOne);

            // If there's only one element, just concat a string for it.
            KeyValuePair<string, string?> current = enumerator.Current;
            if (!enumerator.MoveNext())
            {
                return current.Key + ":" + current.Value;
            }

            // Otherwise, append it, then append the element we moved to, and then
            // iterate through the remainder of the elements, appending each.
            StringBuilder sb = new StringBuilder().Append(current.Key).Append(':').Append(current.Value);
            do
            {
                current = enumerator.Current;
                sb.Append(',').Append(current.Key).Append(':').Append(current.Value);
            }
            while (enumerator.MoveNext());

            // Return the final string.
            return sb.ToString();
        }

        #endregion // private implementation
    }
}
