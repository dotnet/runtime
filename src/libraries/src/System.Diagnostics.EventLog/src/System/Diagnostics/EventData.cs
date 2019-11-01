// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                if (value > ushort.MaxValue || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(CategoryId));

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
                if (value > uint.MaxValue || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(InstanceId));

                _instanceId = value;
            }
        }
    }
}