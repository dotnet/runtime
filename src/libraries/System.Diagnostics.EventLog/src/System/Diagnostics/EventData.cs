// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Diagnostics
{
    public class EventInstance
    {
        private int _categoryNumber;
        private EventLogEntryType _entryType = EventLogEntryType.Information;
        private long _instanceId;

        public EventInstance(long instanceId, int categoryId)
        {
            CategoryId = categoryId;
            InstanceId = instanceId;
        }

        public EventInstance(long instanceId, int categoryId, EventLogEntryType entryType) : this(instanceId, categoryId)
        {
            EntryType = entryType;
        }

        public int CategoryId
        {
            get => _categoryNumber;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNotBetween(value, 0, ushort.MaxValue);

                _categoryNumber = value;
            }
        }

        public EventLogEntryType EntryType
        {
            get => _entryType;
            set
            {
                if (!Enum.IsDefined(typeof(EventLogEntryType), value))
                    throw new InvalidEnumArgumentException(nameof(EntryType), (int)value, typeof(EventLogEntryType));

                _entryType = value;
            }
        }

        public long InstanceId
        {
            get => _instanceId;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNotBetween(value, 0, uint.MaxValue);

                _instanceId = value;
            }
        }
    }
}
