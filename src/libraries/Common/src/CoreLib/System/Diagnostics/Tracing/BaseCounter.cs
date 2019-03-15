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
    /// BaseCounter is an abstract class that serves as the parent class for various Counter* classes, 
    /// namely EventCounter, PollingCounter, IncrementingEventCounter, and IncrementingPollingCounter.
    /// </summary>
    public abstract class BaseCounter : IDisposable
    {
        /// <summary>
        /// All Counters live as long as the EventSource that they are attached to unless they are
        /// explicitly Disposed.   
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="eventSource">The event source.</param>
        public BaseCounter(string name, EventSource eventSource)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(_name));
            }

            if (eventSource == null)
            {
                throw new ArgumentNullException(nameof(eventSource));
            }

            _group = CounterGroup.GetCounterGroup(eventSource);
            _group.Add(this);
            _eventSource = eventSource;
            _name = name;
            _metaData = new Dictionary<string, string>();
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
        internal void AddMetaData(string key, string value)
        {
            lock (MyLock)
            {
                _metaData.Add(key, value);
            }
        }

        internal string DisplayName { get; set; }

        #region private implementation

        internal readonly string _name;

        private CounterGroup _group;
        private Dictionary<string, string> _metaData;
        internal EventSource _eventSource;

        internal abstract void WritePayload(float intervalSec);

        // arbitrarily we use name as the lock object.  
        internal object MyLock { get { return _name; } }

        internal void ReportOutOfBandMessage(string message)
        {
            _eventSource.ReportOutOfBandMessage(message, true);
        }

        internal string GetMetaDataString()
        {
            StringBuilder sb = new StringBuilder("");
            foreach(KeyValuePair<string, string> kvPair in _metaData)
            {
                sb.Append($"{kvPair.Key}:{kvPair.Value},");
            }
            return sb.Length == 0 ? "" : sb.ToString(0, sb.Length - 1); // Get rid of the last ","
        }

        #endregion // private implementation
    }
}
