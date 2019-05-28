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
        public void AddMetadata(string key, string value)
        {
            lock (MyLock)
            {
                _metadata = _metadata ?? new Dictionary<string, string>();
                _metadata.Add(key, value);
            }
        }

        public string? DisplayName { get; set; }

        public string Name { get; }

        public EventSource EventSource { get; }

        #region private implementation

        private CounterGroup _group;
        private Dictionary<string, string>? _metadata;

        internal abstract void WritePayload(float intervalSec, int pollingIntervalMillisec);

        // arbitrarily we use name as the lock object.  
        internal object MyLock { get { return Name; } }

        internal void ReportOutOfBandMessage(string message)
        {
            EventSource.ReportOutOfBandMessage(message, true);
        }

        internal string GetMetadataString()
        {
            if (_metadata == null)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder("");
            foreach(KeyValuePair<string, string> kvPair in _metadata)
            {
                sb.Append($"{kvPair.Key}:{kvPair.Value},");
            }
            return sb.Length == 0 ? "" : sb.ToString(0, sb.Length - 1); // Get rid of the last ","
        }

        #endregion // private implementation
    }
}
