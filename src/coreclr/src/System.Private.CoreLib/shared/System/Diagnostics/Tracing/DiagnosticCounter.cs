// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
#if ES_BUILD_PCL
    using System.Threading.Tasks;
#endif

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

            _group = CounterGroup.GetCounterGroup(eventSource);
            _group.Add(this);
            Name = name;
            DisplayUnits = string.Empty;
            EventSource = eventSource;
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
                _group = null!; // TODO-NULLABLE: Avoid nulling out in Dispose
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
            set
            {
                if (value == null)
                    throw new ArgumentException("Cannot set null as DisplayName");
                _displayName = value;
            }
            get { return _displayName; }
        }

        private string _displayUnits = "";
        public string DisplayUnits
        {
            set
            {
                if (value == null)
                    throw new ArgumentException("Cannot set null as DisplayUnits");
                _displayUnits = value;
            }
            get { return _displayUnits; }
        }

        public string Name { get; }

        public EventSource EventSource { get; }

        #region private implementation

        private CounterGroup _group;
        private Dictionary<string, string?>? _metadata;

        internal abstract void WritePayload(float intervalSec, int pollingIntervalMillisec);

        internal void ReportOutOfBandMessage(string message)
        {
            EventSource.ReportOutOfBandMessage(message, true);
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
            var sb = new StringBuilder().Append(current.Key).Append(':').Append(current.Value);
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
