// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Globalization.Tests
{
    public class JapaneseCalendarTests : CalendarTestBase
    {
        public override Calendar Calendar => new JapaneseCalendar();

        public override DateTime MinSupportedDateTime => new DateTime(1868, 10, 23);

        public override bool SkipErasTest => true;

        [Fact]
        public void Eras_Get_ReturnsAtLeastFour()
        {
            int[] eras = Calendar.Eras;
            Assert.True(eras.Length >= 4);

            // eras should be [ noOfEras, noOfEras - 1, ..., 1 ]
            Assert.Equal(eras.Length, eras[0]);
            for (int i = 0; i < eras.Length; i++)
            {
                Assert.Equal(eras.Length - i, eras[i]);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.IsIcuGlobalization))]
        public void IcuEraData_IsNotDiscarded()
        {
            const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
            const int MeijiIcuEra = 232;
            Assembly coreLib = typeof(JapaneseCalendar).Assembly;
            Type calendarData = coreLib.GetType("System.Globalization.CalendarData");
            Type calendarId = coreLib.GetType("System.Globalization.CalendarId");
            Type dataType = coreLib.GetType("System.Globalization.CalendarDataType");
            MethodInfo enumNames = calendarData.GetMethod("EnumCalendarInfo", Flags, null,
                [typeof(string), calendarId, dataType, typeof(string[]).MakeByRefType()], null);
            object[] nameArgs = ["ja-JP", Enum.Parse(calendarId, "JAPAN"), Enum.Parse(dataType, "EraNames"), null];
            if (!(bool)enumNames.Invoke(null, nameArgs))
            {
                throw new SkipTestException("ICU Japanese era names are unavailable.");
            }

            string[] names = (string[])nameArgs[3];
            Type interop = coreLib.GetType("Interop+Globalization");
            int latestEra = (int)interop.GetMethod("GetLatestJapaneseEra", Flags).Invoke(null, null);
            MethodInfo getStartDate = typeof(JapaneseCalendar).GetMethod("GetJapaneseEraStartDate", Flags);
            if (latestEra < MeijiIcuEra || latestEra >= names.Length)
            {
                throw new SkipTestException("ICU Japanese era data is incomplete.");
            }

            for (int era = MeijiIcuEra; era <= latestEra; era++)
            {
                object[] startArgs = [era, default(DateTime)];
                if (string.IsNullOrEmpty(names[era]) || !(bool)getStartDate.Invoke(null, startArgs))
                {
                    throw new SkipTestException("ICU Japanese era data is incomplete.");
                }

                DateTime start = (DateTime)startArgs[1];
                if (era == MeijiIcuEra ? start.Year != MinSupportedDateTime.Year : start < MinSupportedDateTime)
                {
                    throw new SkipTestException("ICU Japanese era data does not cover the supported range.");
                }
            }

            // Until ICU adds another era, the public era list matches the fallback.
            MethodInfo getIcuEras = typeof(JapaneseCalendar).GetMethod("IcuGetJapaneseEras", Flags);
            Assert.NotNull(getIcuEras);
            Assert.NotNull(getIcuEras.Invoke(null, null));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void FallbackEraData_RespectsMinimumDate()
        {
            var startInfo = new ProcessStartInfo();
            TestEnvironment.ClearGlobalizationEnvironmentVars(startInfo.Environment);
            startInfo.Environment["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
            startInfo.Environment["DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY"] = "0";

            RemoteExecutor.Invoke(static () =>
            {
                Assert.True(PlatformDetection.IsInvariantGlobalization);
                var calendar = new JapaneseCalendar();
                DateTime minimum = calendar.MinSupportedDateTime;
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.GetEra(minimum.AddTicks(-1)));
                Assert.Equal(1, calendar.GetEra(minimum));
                Assert.Equal(1, calendar.GetEra(new DateTime(1912, 7, 29)));
                Assert.Equal(2, calendar.GetEra(new DateTime(1912, 7, 30)));
            }, new RemoteInvokeOptions { StartInfo = startInfo }).Dispose();
        }
    }
}
