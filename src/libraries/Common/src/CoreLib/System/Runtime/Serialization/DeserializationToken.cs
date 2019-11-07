// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    // Tracks whether deserialization is currently in progress
    public readonly struct DeserializationToken : IDisposable
    {
        private readonly DeserializationTracker? _tracker;

        internal DeserializationToken(DeserializationTracker? tracker)
        {
            _tracker = tracker;
        }

        // If this token owned the DeserializationTracker, turn off DeserializationInProgress tracking
        public void Dispose()
        {
            if (_tracker != null && _tracker.DeserializationInProgress)
            {
                lock (_tracker)
                {
                    if (_tracker.DeserializationInProgress)
                    {
                        _tracker.DeserializationInProgress = false;
                        SerializationInfo.AsyncDeserializationInProgress.Value = false;
                    }
                }
            }
        }
    }
}
