// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Runtime.Serialization
{
    public sealed class SerializationObjectManager
    {
        private readonly Dictionary<object, object> _objectSeenTable; // Table to keep track of objects [OnSerializing] has been called on
        private readonly StreamingContext _context;
        private SerializationEventHandler _onSerializedHandler;

        public SerializationObjectManager(StreamingContext context)
        {
            _context = context;
            _objectSeenTable = new Dictionary<object, object>();
        }

        public void RegisterObject(object obj)
        {
            // Invoke OnSerializing for this object
            SerializationEvents cache = SerializationEventsCache.GetSerializationEventsForType(obj.GetType());

            // Check to make sure type has serializing events
            if (cache.HasOnSerializingEvents)
            {
                // Check to see if we have invoked the events on the object
                if (_objectSeenTable.TryAdd(obj, true))
                {
                    // Invoke the events
                    cache.InvokeOnSerializing(obj, _context);
                    // Register for OnSerialized event
                    AddOnSerialized(obj);
                }
            }
        }

        public void RaiseOnSerializedEvent() => _onSerializedHandler?.Invoke(_context);

        private void AddOnSerialized(object obj)
        {
            SerializationEvents cache = SerializationEventsCache.GetSerializationEventsForType(obj.GetType());
            _onSerializedHandler = cache.AddOnSerialized(obj, _onSerializedHandler);
        }
    }
}
