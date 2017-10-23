// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.even

using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventInstanceTests
    {
        // These ids can be any non-negative numbers
        private const long instanceId = 57;
        private const int categoryId = 657;

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void EventInstanceCreation()
        {
            EventInstance eventInstance = new EventInstance(instanceId, categoryId);

            Assert.Equal(categoryId, eventInstance.CategoryId);
            Assert.Equal(instanceId, eventInstance.InstanceId);
            Assert.Equal(EventLogEntryType.Information, eventInstance.EntryType);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void EventInstanceOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EventInstance(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EventInstance(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EventInstance(-1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EventInstance(0, int.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EventInstance(long.MaxValue, 0));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void EventInstanceCreationWithType()
        {
            EventInstance eventInstance = new EventInstance(instanceId, categoryId, EventLogEntryType.Warning);

            Assert.Equal(categoryId, eventInstance.CategoryId);
            Assert.Equal(instanceId, eventInstance.InstanceId);
            Assert.Equal(EventLogEntryType.Warning, eventInstance.EntryType);
        }
    }
}
