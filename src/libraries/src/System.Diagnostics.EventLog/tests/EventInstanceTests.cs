// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.even

using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventInstanceTests
    {
        private long instanceId = 57;
        private int categoryId = 657;
        EventInstance eventInstance = null;

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void EventInstanceCreation()
        {
            eventInstance = new EventInstance(instanceId, categoryId);

            Assert.Equal(categoryId, eventInstance.CategoryId);
            Assert.Equal(instanceId, eventInstance.InstanceId);
            Assert.Equal(EventLogEntryType.Information, eventInstance.EntryType);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void EventInstanceOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EventInstance(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EventInstance(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EventInstance(-1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EventInstance(0, int.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EventInstance(long.MaxValue, 0));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void EventInstanceCreationWithType()
        {
            eventInstance = new EventInstance(instanceId, categoryId, EventLogEntryType.Warning);

            Assert.Equal(categoryId, eventInstance.CategoryId);
            Assert.Equal(instanceId, eventInstance.InstanceId);
            Assert.Equal(EventLogEntryType.Warning, eventInstance.EntryType);
        }
    }
}
