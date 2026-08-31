// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public abstract class InfoGetSetTimes<T> : BaseGetSetTimes<T> where T : FileSystemInfo
    {
        protected abstract void InvokeCreate(T item);

        [Fact]
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
        public void TimesResetAfterDelete()
        {
            T item = GetExistingItem();

            // Refresh to fill state
            item.Refresh();

            // Get our initial times
            var times = new Dictionary<TimeFunction, DateTime>();
            foreach (var timeFunction in TimeFunctions())
            {
                times.Add(timeFunction, timeFunction.Getter(item));
            }

            // Deleting should refresh state
            item.Delete();

            Assert.All(times, time =>
            {
                // We check that all the file times have been refreshed
                Assert.NotEqual(time.Key.Getter(item), time.Value);
            });
        }
    }
}
