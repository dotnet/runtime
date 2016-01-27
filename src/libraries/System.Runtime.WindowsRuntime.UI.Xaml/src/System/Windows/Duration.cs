// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//

using System;
using System.Runtime.InteropServices;

#pragma warning disable 436   // Redefining types from Windows.Foundation

namespace Windows.UI.Xaml
{
    //
    // Duration is the managed projection of Windows.UI.Xaml.Duration.  Any changes to the layout
    // of this type must be exactly mirrored on the native WinRT side as well.
    //
    // DurationType is the managed projection of Windows.UI.Xaml.DurationType. Any changes to this
    // enumeration must be exactly mirrored on the native WinRT side as well.
    //
    // Note that these types are owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //

    public enum DurationType
    {
        Automatic,
        TimeSpan,
        Forever
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Duration
    {
        private TimeSpan _timeSpan;
        private DurationType _durationType;

        public Duration(TimeSpan timeSpan)
        {
            _durationType = DurationType.TimeSpan;
            _timeSpan = timeSpan;
        }

        public static implicit operator Duration(TimeSpan timeSpan)
        {
            return new Duration(timeSpan);
        }

        public static Duration operator +(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return new Duration(t1._timeSpan + t2._timeSpan);
            }
            else if (t1._durationType != DurationType.Automatic
                     && t2._durationType != DurationType.Automatic)
            {
                return Duration.Forever;
            }
            else
            {
                // Automatic + anything is Automatic
                return Duration.Automatic;
            }
        }

        public static Duration operator -(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return new Duration(t1._timeSpan - t2._timeSpan);
            }
            else if (t1._durationType == DurationType.Forever
                     && t2.HasTimeSpan)
            {
                return Duration.Forever;
            }
            else
            {
                return Duration.Automatic;
            }
        }

        public static bool operator ==(Duration t1, Duration t2)
        {
            return t1.Equals(t2);
        }

        public static bool operator !=(Duration t1, Duration t2)
        {
            return !(t1.Equals(t2));
        }

        public static bool operator >(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return t1._timeSpan > t2._timeSpan;
            }
            else if (t1.HasTimeSpan && t2._durationType == DurationType.Forever)
            {
                return false;
            }
            else if (t1._durationType == DurationType.Forever && t2.HasTimeSpan)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool operator >=(Duration t1, Duration t2)
        {
            if (t1._durationType == DurationType.Automatic && t2._durationType == DurationType.Automatic)
            {
                return true;
            }
            else if (t1._durationType == DurationType.Automatic || t2._durationType == DurationType.Automatic)
            {
                return false;
            }
            else
            {
                return !(t1 < t2);
            }
        }

        public static bool operator <(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return t1._timeSpan < t2._timeSpan;
            }
            else if (t1.HasTimeSpan && t2._durationType == DurationType.Forever)
            {
                return true;
            }
            else if (t1._durationType == DurationType.Forever && t2.HasTimeSpan)
            {
                return false;
            }
            else
            {
                return false;
            }
        }

        public static bool operator <=(Duration t1, Duration t2)
        {
            if (t1._durationType == DurationType.Automatic && t2._durationType == DurationType.Automatic)
            {
                return true;
            }
            else if (t1._durationType == DurationType.Automatic || t2._durationType == DurationType.Automatic)
            {
                return false;
            }
            else
            {
                return !(t1 > t2);
            }
        }

        public static int Compare(Duration t1, Duration t2)
        {
            if (t1._durationType == DurationType.Automatic)
            {
                if (t2._durationType == DurationType.Automatic)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else if (t2._durationType == DurationType.Automatic)
            {
                return 1;
            }
            else
            {
                if (t1 < t2)
                {
                    return -1;
                }
                else if (t1 > t2)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public static Duration operator +(Duration duration)
        {
            return duration;
        }

        public bool HasTimeSpan
        {
            get
            {
                return (_durationType == DurationType.TimeSpan);
            }
        }

        public static Duration Automatic
        {
            get
            {
                Duration duration = new Duration();
                duration._durationType = DurationType.Automatic;

                return duration;
            }
        }

        public static Duration Forever
        {
            get
            {
                Duration duration = new Duration();
                duration._durationType = DurationType.Forever;

                return duration;
            }
        }

        public TimeSpan TimeSpan
        {
            get
            {
                if (HasTimeSpan)
                {
                    return _timeSpan;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public Duration Add(Duration duration)
        {
            return this + duration;
        }

        public override bool Equals(Object value)
        {
            return value is Duration && Equals((Duration)value);
        }

        public bool Equals(Duration duration)
        {
            if (HasTimeSpan)
            {
                if (duration.HasTimeSpan)
                {
                    return _timeSpan == duration._timeSpan;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return _durationType == duration._durationType;
            }
        }

        public static bool Equals(Duration t1, Duration t2)
        {
            return t1.Equals(t2);
        }

        public override int GetHashCode()
        {
            if (HasTimeSpan)
            {
                return _timeSpan.GetHashCode();
            }
            else
            {
                return _durationType.GetHashCode() + 17;
            }
        }

        public Duration Subtract(Duration duration)
        {
            return this - duration;
        }

        public override string ToString()
        {
            if (HasTimeSpan)
            {
                return _timeSpan.ToString(); // "00"; //TypeDescriptor.GetConverter(_timeSpan).ConvertToString(_timeSpan);
            }
            else if (_durationType == DurationType.Forever)
            {
                return "Forever";
            }
            else // IsAutomatic
            {
                return "Automatic";
            }
        }
    }
}

#pragma warning restore 436
