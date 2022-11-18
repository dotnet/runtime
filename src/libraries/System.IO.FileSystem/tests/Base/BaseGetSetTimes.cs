// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public abstract class BaseGetSetTimes<T> : FileSystemTest
    {
        protected const string HFS = "hfs";
        public delegate void SetTime(T item, DateTime time);
        public delegate DateTime GetTime(T item);
        // AppContainer restricts access to DriveFormat (::GetVolumeInformation)
        private static string driveFormat = PlatformDetection.IsInAppContainer ? string.Empty : new DriveInfo(Path.GetTempPath()).DriveFormat;

        protected static bool isHFS => driveFormat != null && driveFormat.Equals(HFS, StringComparison.InvariantCultureIgnoreCase);

        protected static bool LowTemporalResolution => PlatformDetection.IsBrowser || isHFS;
        protected static bool HighTemporalResolution => !LowTemporalResolution;

        protected abstract bool CanBeReadOnly { get; }

        protected abstract T GetExistingItem(bool readOnly = false);
        protected abstract T GetMissingItem();

        protected abstract T CreateSymlink(string path, string pathToTarget);

        // When the item is a link, indicates whether the .NET API will get/set the link itself, or its target. 
        protected virtual bool ApiTargetsLink => true;

        protected T CreateSymlinkToItem(T item)
        {
            // Creates a Symlink to 'item' (target may or may not exist)
            string itemPath = GetItemPath(item);
            return CreateSymlink(path: itemPath + ".link", pathToTarget: itemPath);
        }

        protected abstract string GetItemPath(T item);

        // requiresRoundtripping defines whether to convert DateTimeFormat 'a' to 'b' and then back to 'a' to verify the DateTimeFormat-conversion
        public abstract IEnumerable<TimeFunction> TimeFunctions(bool requiresRoundtripping = false);

        public class TimeFunction : Tuple<SetTime, GetTime, DateTimeKind>
        {
            public TimeFunction(SetTime setter, GetTime getter, DateTimeKind kind)
                : base(item1: setter, item2: getter, item3: kind)
            {
            }

            public static TimeFunction Create(SetTime setter, GetTime getter, DateTimeKind kind)
                => new TimeFunction(setter, getter, kind);

            public SetTime Setter => Item1;
            public GetTime Getter => Item2;
            public DateTimeKind Kind => Item3;

            public override string ToString()
            {
                return $"TimeFunction DateTimeKind.{Kind} Setter: {Setter.Method.Name} Getter: {Getter.Method.Name}";
            }
        }

        private void SettingUpdatesPropertiesCore(T item, T? linkTarget = default)
        {
            Assert.All(TimeFunctions(requiresRoundtripping: true), (function) =>
            {
                bool isLink = linkTarget is not null;

                // Checking that milliseconds are not dropped after setter.
                // Emscripten drops milliseconds in Browser
                DateTime dt = new DateTime(2014, 12, 1, 12, 3, 3, LowTemporalResolution ? 0 : 321, function.Kind);
                function.Setter(item, dt);

                T getTarget = !isLink || ApiTargetsLink ? item : linkTarget;
                DateTime result = function.Getter(getTarget);

                Assert.Equal(dt, result);
                Assert.Equal(dt.ToLocalTime(), result.ToLocalTime());

                // File and Directory UTC APIs treat a DateTimeKind.Unspecified as UTC whereas
                // ToUniversalTime treats it as local.
                if (function.Kind == DateTimeKind.Unspecified)
                {
                    Assert.Equal(dt, result.ToUniversalTime());
                }
                else
                {
                    Assert.Equal(dt.ToUniversalTime(), result.ToUniversalTime());
                }
            });
        }

        [Fact]
        public void SettingUpdatesProperties()
        {
            T item = GetExistingItem();
            SettingUpdatesPropertiesCore(item);
        }

        [Fact]
        public void SettingUpdatesPropertiesWhenReadOnly()
        {
            if (!CanBeReadOnly)
            {
                return; // directories can't be read only, so automatic pass
            }

            T item = GetExistingItem(readOnly: true);
            SettingUpdatesPropertiesCore(item);
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [PlatformSpecific(~TestPlatforms.Browser)] // Browser is excluded as it doesn't support symlinks
        [InlineData(false)]
        [InlineData(true)]
        public void SettingPropertiesOnSymlink(bool targetExists)
        {
            // This test is in this class since it needs all of the time functions.
            // This test makes sure that the times are set on the symlink itself.
            // It is needed as on OSX for example, the default for most APIs is
            // to follow the symlink to completion and set the time on that entry
            // instead (eg. the setattrlist will do this without the flag set).
            // It is also the same case on unix, with the utimensat function.
            // It is a theory since we test both the target existing and missing.

            T target = targetExists ? GetExistingItem() : GetMissingItem();

            // When the target exists, we want to verify that its times don't change.

            T link = CreateSymlinkToItem(target);
            if (!targetExists)
            {
                // Don't check when settings update the target.
                if (ApiTargetsLink)
                {
                    SettingUpdatesPropertiesCore(link, target);
                }
            }
            else
            {
                // When properties update link, verify the target properties don't change.
                IEnumerable<TimeFunction>? timeFunctions = null;
                DateTime[]? initialTimes = null;
                if (ApiTargetsLink)
                {
                    timeFunctions = TimeFunctions(requiresRoundtripping: true);
                    initialTimes = timeFunctions.Select((funcs) => funcs.Getter(target)).ToArray();
                }

                SettingUpdatesPropertiesCore(link, target);

                // Ensure target properties haven't changed.
                if (ApiTargetsLink)
                {
                    // Ensure that we have the latest times.
                    if (target is FileSystemInfo fsi)
                    {
                        fsi.Refresh();
                    }

                    DateTime[] updatedTimes = timeFunctions.Select((funcs) => funcs.Getter(target)).ToArray();
                    Assert.Equal(initialTimes, updatedTimes);
                }
            }
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.Browser)] // Browser is excluded as there is only 1 effective time store.
        public void SettingUpdatesPropertiesAfterAnother()
        {
            T item = GetExistingItem();

            // These linq calls make an IEnumerable of pairs of functions that are not identical
            // (eg. not (creationtime, creationtime)), includes both orders as separate entries
            // as they it have different behavior in reverse order (of functions), in addition
            // to the pairs of functions, there is a reverse bool that allows a test for both
            // increasing and decreasing timestamps as to not limit the test unnecessarily.
            // Only testing with utc because it would be hard to check if lastwrite utc was the
            // same type of method as lastwrite local since their .Getter fields are different.
            // This test is required as some apis change more dates than would be desired (eg.
            // utimes()/utimensat() set the write and access times, but as a side effect of
            // the implementation, it sets creation time too when the write time is less than
            // the creation time). There were issues related to the order in which the dates are
            // set, so this test should almost fully eliminate any possibilities of that in the
            // future by having a proper test for it. Also, it should be noted that the
            // combination (A, B, false) is not the same as (B, A, true).

            // The order that these LINQ expression creates is (when all 3 are available):
            // [0] = (creation, access, False), [1] = (creation, access, True),  [2] = (creation, write, False),
            // [3] = (creation, write, True),   [4] = (access, creation, False), [5] = (access, creation, True),
            // [6] = (access, write, False),    [7] = (access, write, True),     [8] = (write, creation, False),
            // [9] = (write, creation, True),  [10] = (write, access, False),   [11] = (write, access, True)
            // Or, when creation time setting is not available:
            // [0] = (access, write, False),    [1] = (access, write, True),
            // [2] = (write, access, False),    [3] = (write, access, True)

            IEnumerable<TimeFunction> timeFunctionsUtc = TimeFunctions(requiresRoundtripping: true).Where((f) => f.Kind == DateTimeKind.Utc);
            bool[] booleanArray = new bool[] { false, true };
            Assert.All(timeFunctionsUtc.SelectMany((x) => timeFunctionsUtc.SelectMany((y) => booleanArray.Select((reverse) => (x, y, reverse)))).Where((fs) => fs.x.Getter != fs.y.Getter), (functions) =>
            {
                TimeFunction function1 = functions.x;
                TimeFunction function2 = functions.y;
                bool reverse = functions.reverse;

                // Checking that milliseconds are not dropped after setter.
                DateTime dt1 = new DateTime(2002, 12, 1, 12, 3, 3, LowTemporalResolution ? 0 : 321, DateTimeKind.Utc);
                DateTime dt2 = new DateTime(2001, 12, 1, 12, 3, 3, LowTemporalResolution ? 0 : 321, DateTimeKind.Utc);
                DateTime dt3 = new DateTime(2000, 12, 1, 12, 3, 3, LowTemporalResolution ? 0 : 321, DateTimeKind.Utc);
                if (reverse) //reverse the order of setting dates
                {
                    (dt1, dt3) = (dt3, dt1);
                }
                function1.Setter(item, dt1);
                function2.Setter(item, dt2);
                function1.Setter(item, dt3);
                DateTime result1 = function1.Getter(item);
                DateTime result2 = function2.Getter(item);
                Assert.Equal(dt3, result1);
                Assert.Equal(dt2, result2);
            });
        }

        [Fact]
        public void CanGetAllTimesAfterCreation()
        {
            DateTime beforeTime = DateTime.UtcNow.AddSeconds(-3);
            T item = GetExistingItem();
            DateTime afterTime = DateTime.UtcNow.AddSeconds(3);
            ValidateSetTimes(item, beforeTime, afterTime);
        }

        [ConditionalFact(nameof(HighTemporalResolution))] // OSX HFS driver format and Browser platform do not support millisec granularity
        public void TimesIncludeMillisecondPart()
        {
            T item = GetExistingItem();
            Assert.All(TimeFunctions(), (function) =>
            {
                var msec = 0;
                for (int i = 0; i < 5; i++)
                {
                    DateTime time = function.Getter(item);
                    msec = time.Millisecond;
                    if (msec != 0)
                        break;

                    // This case should only happen 1/1000 times, unless the OS/Filesystem does
                    // not support millisecond granularity.

                    // If it's 1/1000, or low granularity, this may help:
                    Thread.Sleep(1234);

                    // If it's the OS/Filesystem often returns 0 for the millisecond part, this may
                    // help prove it. This should only be written 1/1000 runs, unless the test is going to
                    // fail.
                    Console.WriteLine($"## TimesIncludeMillisecondPart got a file time of {time.ToString("o")} on {driveFormat}");

                    item = GetExistingItem(); // try a new file/directory
                }

                Assert.NotEqual(0, msec);
            });
        }

        [ConditionalFact(nameof(LowTemporalResolution))]
        public void TimesIncludeMillisecondPart_LowTempRes()
        {
            T item = GetExistingItem();
            // OSX HFS driver format and Browser do not support millisec granularity
            Assert.All(TimeFunctions(), (function) =>
            {
                DateTime time = function.Getter(item);
                Assert.Equal(0, time.Millisecond);
            });
        }

        protected void ValidateSetTimes(T item, DateTime beforeTime, DateTime afterTime)
        {
            Assert.All(TimeFunctions(), (function) =>
            {
                // We want to test all possible DateTimeKind conversions to ensure they function as expected
                if (function.Kind == DateTimeKind.Local)
                    Assert.InRange(function.Getter(item).Ticks, beforeTime.ToLocalTime().Ticks, afterTime.ToLocalTime().Ticks);
                else
                    Assert.InRange(function.Getter(item).Ticks, beforeTime.Ticks, afterTime.Ticks);
                Assert.InRange(function.Getter(item).ToLocalTime().Ticks, beforeTime.ToLocalTime().Ticks, afterTime.ToLocalTime().Ticks);
                Assert.InRange(function.Getter(item).ToUniversalTime().Ticks, beforeTime.Ticks, afterTime.Ticks);
            });
        }

        public void DoesntExist_ReturnsDefaultValues()
        {
            T item = GetMissingItem();

            Assert.All(TimeFunctions(), (function) =>
            {
                Assert.Equal(
                    function.Kind == DateTimeKind.Local
                        ? DateTime.FromFileTime(0).Ticks
                        : DateTime.FromFileTimeUtc(0).Ticks,
                    function.Getter(item).Ticks);
            });
        }
    }
}
