// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.DirectoryServices.ActiveDirectory
{
    public enum HourOfDay
    {
        Zero,
        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Eleven,
        Twelve,
        Thirteen,
        Fourteen,
        Fifteen,
        Sixteen,
        Seventeen,
        Eighteen,
        Nineteen,
        Twenty,
        TwentyOne,
        TwentyTwo,
        TwentyThree
    }

    public enum MinuteOfHour
    {
        Zero = 0,
        Fifteen = 15,
        Thirty = 30,
        FortyFive = 45
    }

    public class ActiveDirectorySchedule
    {
        // 24*7*4 = 672
        private readonly bool[] _scheduleArray = new bool[672];
        private readonly long _utcOffSet = 0;

        public ActiveDirectorySchedule()
        {
            // need to get the offset between local time and UTC time
#pragma warning disable 612, 618
            _utcOffSet = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Ticks / TimeSpan.TicksPerHour;
#pragma warning restore 612, 618
        }

        public ActiveDirectorySchedule(ActiveDirectorySchedule schedule) : this()
        {
            if (schedule == null)
                throw new ArgumentNullException();

            bool[] tmpSchedule = schedule._scheduleArray;
            for (int i = 0; i < 672; i++)
                _scheduleArray[i] = tmpSchedule[i];
        }

        internal ActiveDirectorySchedule(bool[] schedule) : this()
        {
            for (int i = 0; i < 672; i++)
                _scheduleArray[i] = schedule[i];
        }

        public bool[,,] RawSchedule
        {
            get
            {
                bool[,,] tmp = new bool[7, 24, 4];
                for (int i = 0; i < 7; i++)
                    for (int j = 0; j < 24; j++)
                        for (int k = 0; k < 4; k++)
                            tmp[i, j, k] = _scheduleArray[i * 24 * 4 + j * 4 + k];
                return tmp;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                ValidateRawArray(value);

                for (int i = 0; i < 7; i++)
                    for (int j = 0; j < 24; j++)
                        for (int k = 0; k < 4; k++)
                            _scheduleArray[i * 24 * 4 + j * 4 + k] = value[i, j, k];
            }
        }

        public void SetSchedule(DayOfWeek day, HourOfDay fromHour, MinuteOfHour fromMinute, HourOfDay toHour, MinuteOfHour toMinute)
        {
            if (day < DayOfWeek.Sunday || day > DayOfWeek.Saturday)
                throw new InvalidEnumArgumentException(nameof(day), (int)day, typeof(DayOfWeek));

            if (fromHour < HourOfDay.Zero || fromHour > HourOfDay.TwentyThree)
                throw new InvalidEnumArgumentException(nameof(fromHour), (int)fromHour, typeof(HourOfDay));

            if (fromMinute != MinuteOfHour.Zero && fromMinute != MinuteOfHour.Fifteen && fromMinute != MinuteOfHour.Thirty && fromMinute != MinuteOfHour.FortyFive)
                throw new InvalidEnumArgumentException(nameof(fromMinute), (int)fromMinute, typeof(MinuteOfHour));

            if (toHour < HourOfDay.Zero || toHour > HourOfDay.TwentyThree)
                throw new InvalidEnumArgumentException(nameof(toHour), (int)toHour, typeof(HourOfDay));

            if (toMinute != MinuteOfHour.Zero && toMinute != MinuteOfHour.Fifteen && toMinute != MinuteOfHour.Thirty && toMinute != MinuteOfHour.FortyFive)
                throw new InvalidEnumArgumentException(nameof(toMinute), (int)toMinute, typeof(MinuteOfHour));

            // end time should be later than the start time
            if ((int)fromHour * 60 + (int)fromMinute > (int)toHour * 60 + (int)toMinute)
                throw new ArgumentException(SR.InvalidTime);

            // set the availability            
            int startPoint = (int)day * 24 * 4 + (int)fromHour * 4 + (int)fromMinute / 15;
            int endPoint = (int)day * 24 * 4 + (int)toHour * 4 + (int)toMinute / 15;
            for (int i = startPoint; i <= endPoint; i++)
                _scheduleArray[i] = true;
        }

        public void SetSchedule(DayOfWeek[] days, HourOfDay fromHour, MinuteOfHour fromMinute, HourOfDay toHour, MinuteOfHour toMinute)
        {
            if (days == null)
                throw new ArgumentNullException(nameof(days));

            for (int i = 0; i < days.Length; i++)
            {
                if (days[i] < DayOfWeek.Sunday || days[i] > DayOfWeek.Saturday)
                    throw new InvalidEnumArgumentException(nameof(days), (int)days[i], typeof(DayOfWeek));
            }

            for (int i = 0; i < days.Length; i++)
                SetSchedule(days[i], fromHour, fromMinute, toHour, toMinute);
        }

        public void SetDailySchedule(HourOfDay fromHour, MinuteOfHour fromMinute, HourOfDay toHour, MinuteOfHour toMinute)
        {
            for (int i = 0; i < 7; i++)
            {
                SetSchedule((DayOfWeek)i, fromHour, fromMinute, toHour, toMinute);
            }
        }

        public void ResetSchedule()
        {
            for (int i = 0; i < 672; i++)
                _scheduleArray[i] = false;
        }

        private void ValidateRawArray(bool[,,] array)
        {
            if (array.Length != 672)
                throw new ArgumentException("value");

            int len1 = array.GetLength(0);
            int len2 = array.GetLength(1);
            int len3 = array.GetLength(2);

            if (len1 != 7 || len2 != 24 || len3 != 4)
                throw new ArgumentException("value");
        }

        internal byte[] GetUnmanagedSchedule()
        {
            byte val = 0;
            int index = 0;
            byte[] unmanagedSchedule = new byte[188];
            int unmanagedScheduleIndex = 0;

            // set size
            unmanagedSchedule[0] = 188;
            // set number of schedule
            unmanagedSchedule[8] = 1;
            // set offset
            unmanagedSchedule[16] = 20;

            // 20 is the offset in the unmanaged structure where the actual schedule begins
            for (int i = 20; i < 188; i++)
            {
                val = 0;
                index = (i - 20) * 4;

                if (_scheduleArray[index])
                    val |= 1;
                if (_scheduleArray[index + 1])
                    val |= 2;
                if (_scheduleArray[index + 2])
                    val |= 4;
                if (_scheduleArray[index + 3])
                    val |= 8;

                //recalculate index position taking utc offset into account
                //ensure circular array in both directions (with index from 20 to 187)
                unmanagedScheduleIndex = i - (int)_utcOffSet;
                if (unmanagedScheduleIndex >= 188)
                {
                    // falling off higher end (move back)
                    unmanagedScheduleIndex = unmanagedScheduleIndex - 188 + 20;
                }
                else if (unmanagedScheduleIndex < 20)
                {
                    // falling off lower end (move forward)
                    unmanagedScheduleIndex = 188 - (20 - unmanagedScheduleIndex);
                }
                unmanagedSchedule[unmanagedScheduleIndex] = val;
            }

            return unmanagedSchedule;
        }

        internal void SetUnmanagedSchedule(byte[] unmanagedSchedule)
        {
            int val = 0;
            int index = 0;
            int unmanagedScheduleIndex = 0;

            // 20 is the offset in the unmanaged structure where the actual schedule begins
            for (int i = 20; i < 188; i++)
            {
                val = 0;
                index = (i - 20) * 4;

                //recalculate index position taking utc offset into account
                //ensure circular array in both directions (with index from 20 to 187)
                unmanagedScheduleIndex = i - (int)_utcOffSet;
                if (unmanagedScheduleIndex >= 188)
                {
                    // falling off higher end (move back)
                    unmanagedScheduleIndex = unmanagedScheduleIndex - 188 + 20;
                }
                else if (unmanagedScheduleIndex < 20)
                {
                    // falling off lower end (move forward)
                    unmanagedScheduleIndex = 188 - (20 - unmanagedScheduleIndex);
                }
                val = unmanagedSchedule[unmanagedScheduleIndex];
                if ((val & 1) != 0)
                    _scheduleArray[index] = true;

                if ((val & 2) != 0)
                    _scheduleArray[index + 1] = true;

                if ((val & 4) != 0)
                    _scheduleArray[index + 2] = true;

                if ((val & 8) != 0)
                    _scheduleArray[index + 3] = true;
            }
        }
    }
}
