// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//

using System;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable 436   // Redefining types from Windows.Foundation

namespace Windows.UI.Xaml.Media.Animation
{
    //
    // RepeatBehavior is the managed projection of Windows.UI.Xaml.Media.Animation.RepeatBehavior.
    // Any changes to the layout of this type must be exactly mirrored on the native WinRT side as well.
    //
    // RepeatBehaviorType is the managed projection of Windows.UI.Xaml.Media.Animation.RepeatBehaviorType.
    // Any changes to this enumeration must be exactly mirrored on the native WinRT side as well.
    //
    // Note that these types are owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //

    public enum RepeatBehaviorType
    {
        Count,
        Duration,
        Forever
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RepeatBehavior : IFormattable
    {
        private double _Count;
        private TimeSpan _Duration;
        private RepeatBehaviorType _Type;

        public RepeatBehavior(double count)
        {
            if (!double.IsFinite(count) || count < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            _Duration = new TimeSpan(0);
            _Count = count;
            _Type = RepeatBehaviorType.Count;
        }

        public RepeatBehavior(TimeSpan duration)
        {
            if (duration < new TimeSpan(0))
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            _Duration = duration;
            _Count = 0.0;
            _Type = RepeatBehaviorType.Duration;
        }

        public static RepeatBehavior Forever
        {
            get
            {
                RepeatBehavior forever = new RepeatBehavior();
                forever.Type = RepeatBehaviorType.Forever;

                return forever;
            }
        }

        public bool HasCount
        {
            get
            {
                return Type == RepeatBehaviorType.Count;
            }
        }

        public bool HasDuration
        {
            get
            {
                return Type == RepeatBehaviorType.Duration;
            }
        }

        public double Count
        {
            get { return _Count; }
            set { _Count = value; }
        }

        public TimeSpan Duration
        {
            get { return _Duration; }
            set { _Duration = value; }
        }

        public RepeatBehaviorType Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        public override string ToString()
        {
            return InternalToString(null, null);
        }

        public string ToString(IFormatProvider formatProvider)
        {
            return InternalToString(null, formatProvider);
        }

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return InternalToString(format, formatProvider);
        }

        internal string InternalToString(string format, IFormatProvider formatProvider)
        {
            switch (_Type)
            {
                case RepeatBehaviorType.Forever:

                    return "Forever";

                case RepeatBehaviorType.Count:

                    StringBuilder sb = new StringBuilder();

                    sb.AppendFormat(
                        formatProvider,
                        "{0:" + format + "}x",
                        _Count);

                    return sb.ToString();

                case RepeatBehaviorType.Duration:

                    return _Duration.ToString();

                default:
                    return null;
            }
        }

        public override bool Equals(Object value)
        {
            if (value is RepeatBehavior)
            {
                return this.Equals((RepeatBehavior)value);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(RepeatBehavior repeatBehavior)
        {
            if (_Type == repeatBehavior._Type)
            {
                switch (_Type)
                {
                    case RepeatBehaviorType.Forever:

                        return true;

                    case RepeatBehaviorType.Count:

                        return _Count == repeatBehavior._Count;

                    case RepeatBehaviorType.Duration:

                        return _Duration == repeatBehavior._Duration;

                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool Equals(RepeatBehavior repeatBehavior1, RepeatBehavior repeatBehavior2)
        {
            return repeatBehavior1.Equals(repeatBehavior2);
        }

        public override int GetHashCode()
        {
            switch (_Type)
            {
                case RepeatBehaviorType.Count:

                    return _Count.GetHashCode();

                case RepeatBehaviorType.Duration:

                    return _Duration.GetHashCode();

                case RepeatBehaviorType.Forever:

                    // We try to choose an unlikely hash code value for Forever.
                    // All Forevers need to return the same hash code value.
                    return int.MaxValue - 42;

                default:
                    return base.GetHashCode();
            }
        }

        public static bool operator ==(RepeatBehavior repeatBehavior1, RepeatBehavior repeatBehavior2)
        {
            return repeatBehavior1.Equals(repeatBehavior2);
        }

        public static bool operator !=(RepeatBehavior repeatBehavior1, RepeatBehavior repeatBehavior2)
        {
            return !repeatBehavior1.Equals(repeatBehavior2);
        }
    }
}

#pragma warning restore 436
