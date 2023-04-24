// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization
{
    [Obsolete(Obsoletions.LegacyFormatterMessage, DiagnosticId = Obsoletions.LegacyFormatterDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    public sealed class SerializationObjectManager
    {
        private const string SerializationObjectManagerUnreferencedCodeMessage = "SerializationObjectManager is not trim compatible because the type of objects being managed cannot be statically discovered.";

        private readonly Dictionary<object, object> _objectSeenTable; // Table to keep track of objects [OnSerializing] has been called on
        private readonly StreamingContext _context;
        private SerializationEventHandler? _onSerializedHandler;

        public SerializationObjectManager(StreamingContext context)
        {
            _context = context;
            _objectSeenTable = new Dictionary<object, object>();
        }

        [RequiresUnreferencedCode(SerializationObjectManagerUnreferencedCodeMessage)]
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

        [RequiresUnreferencedCode(SerializationObjectManagerUnreferencedCodeMessage)]
        private void AddOnSerialized(object obj)
        {
            SerializationEvents cache = SerializationEventsCache.GetSerializationEventsForType(obj.GetType());
            _onSerializedHandler = cache.AddOnSerialized(obj, _onSerializedHandler);
        }
    }
}
