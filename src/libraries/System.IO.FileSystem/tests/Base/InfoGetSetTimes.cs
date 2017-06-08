// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public abstract class InfoGetSetTimes<T> : BaseGetSetTimes<T> where T : FileSystemInfo
    {
        public abstract void InvokeCreate(T item);

        public void DoesntExistThenCreate_ReturnsDefaultValues()
        {
            T item = GetMissingItem();
            InvokeCreate(item);

            Assert.All(TimeFunctions(), (function) =>
            {
                Assert.Equal(
                    function.Kind == DateTimeKind.Local
                        ? DateTime.FromFileTime(0).Ticks
                        : DateTime.FromFileTimeUtc(0).Ticks,
                    function.Getter(item).Ticks);
            });
        }

        public void ExistsThenDelete_ReturnsDefaultValues()
        {
            // Because we haven't hit the property the item should be in
            // an uninitialized state, hence still 0.

            T item = GetExistingItem();
            item.Delete();

            Assert.All(TimeFunctions(), (function) =>
            {
                Assert.Equal(
                    function.Kind == DateTimeKind.Local
                        ? DateTime.FromFileTime(0).Ticks
                        : DateTime.FromFileTimeUtc(0).Ticks,
                    function.Getter(item).Ticks);
            });
        }

        [Fact]
        public void TimesStillSetAfterDelete()
        {
            DateTime beforeTime = DateTime.UtcNow.AddSeconds(-1);
            T item = GetExistingItem();

            // Refresh to fill state
            item.Refresh();
            DateTime afterTime = DateTime.UtcNow.AddSeconds(1);

            // Deleting doesn't change any info state
            item.Delete();
            ValidateSetTimes(item, beforeTime, afterTime);
        }
    }
}
