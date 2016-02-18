// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//

using System;
using System.Runtime.InteropServices;

#pragma warning disable 436   // Redefining types from Windows.Foundation

namespace Windows.UI.Xaml.Media.Animation
{
    //
    // KeyTime is the managed projection of Windows.UI.Xaml.Media.Animation.KeyTime.
    // Any changes to the layout of this type must be exactly mirrored on the native WinRT side as well.
    //
    // Note that these types are owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //

    [StructLayout(LayoutKind.Sequential)]
    public struct KeyTime
    {
        private TimeSpan _timeSpan;

        public static KeyTime FromTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeSpan));
            }

            KeyTime keyTime = new KeyTime();

            keyTime._timeSpan = timeSpan;

            return keyTime;
        }

        public static bool Equals(KeyTime keyTime1, KeyTime keyTime2)
        {
            return (keyTime1._timeSpan == keyTime2._timeSpan);
        }

        public static bool operator ==(KeyTime keyTime1, KeyTime keyTime2)
        {
            return KeyTime.Equals(keyTime1, keyTime2);
        }

        public static bool operator !=(KeyTime keyTime1, KeyTime keyTime2)
        {
            return !KeyTime.Equals(keyTime1, keyTime2);
        }

        public bool Equals(KeyTime value)
        {
            return KeyTime.Equals(this, value);
        }

        public override bool Equals(object value)
        {
            return value is KeyTime && this == (KeyTime)value;
        }

        public override int GetHashCode()
        {
            return _timeSpan.GetHashCode();
        }

        public override string ToString()
        {
            return _timeSpan.ToString();
        }

        public static implicit operator KeyTime(TimeSpan timeSpan)
        {
            return KeyTime.FromTimeSpan(timeSpan);
        }

        public TimeSpan TimeSpan
        {
            get
            {
                return _timeSpan;
            }
        }
    }
}

#pragma warning restore 436
