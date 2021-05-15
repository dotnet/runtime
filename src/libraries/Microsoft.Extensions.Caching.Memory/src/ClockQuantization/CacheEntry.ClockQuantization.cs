// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.Extensions.Caching.Memory
{
    internal partial class CacheEntry : ICacheEntry
    {
        private long? _slidingExpirationClockOffsetUnits;


        private Internal.ClockQuantization.LazyClockOffsetSerialPosition _lastAccessedClockOffsetSerialPosition;
        internal Internal.ClockQuantization.LazyClockOffsetSerialPosition LastAccessedClockOffsetSerialPosition
        {
            get => _lastAccessedClockOffsetSerialPosition;
            set
            {
                _lastAccessedClockOffsetSerialPosition = value;
            }
        }


        private long? _absoluteExpirationClockOffset;
    }
}

#nullable restore
