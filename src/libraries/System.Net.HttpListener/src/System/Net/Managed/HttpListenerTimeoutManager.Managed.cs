// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Net
{
    public class HttpListenerTimeoutManager
    {
        private TimeSpan _drainEntityBody = TimeSpan.Zero;
        private TimeSpan _idleConnection = TimeSpan.Zero;

        internal HttpListenerTimeoutManager(HttpListener _) { }

        public TimeSpan DrainEntityBody
        {
            get => _drainEntityBody;
            set
            {
                // Managed implementation currently doesn't pool connections,
                // so this is a nop other than roundtripping the value.
                ValidateTimeout(value);
                _drainEntityBody = value;
            }
        }

        public TimeSpan IdleConnection
        {
            get => _idleConnection;
            set
            {
                // Managed implementation currently doesn't pool connections,
                // so this is a nop other than roundtripping the value.
                ValidateTimeout(value);
                _idleConnection = value;
            }
        }

        public TimeSpan EntityBody
        {
            get => TimeSpan.Zero;
            [SupportedOSPlatform("windows")]
            set
            {
                ValidateTimeout(value);
                throw new PlatformNotSupportedException(); // low usage, not currently implemented
            }
        }

        public TimeSpan HeaderWait
        {
            get => TimeSpan.Zero;
            [SupportedOSPlatform("windows")]
            set
            {
                ValidateTimeout(value);
                throw new PlatformNotSupportedException(); // low usage, not currently implemented
            }
        }

        public long MinSendBytesPerSecond
        {
            get => 0;
            [SupportedOSPlatform("windows")]
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, uint.MaxValue);
                throw new PlatformNotSupportedException(); // low usage, not currently implemented
            }
        }

        public TimeSpan RequestQueue
        {
            get => TimeSpan.Zero;
            [SupportedOSPlatform("windows")]
            set
            {
                ValidateTimeout(value);
                throw new PlatformNotSupportedException(); // low usage, not currently implemented
            }
        }

        private static void ValidateTimeout(TimeSpan value)
        {
            long timeoutValue = Convert.ToInt64(value.TotalSeconds);
            ArgumentOutOfRangeException.ThrowIfNegative(timeoutValue, nameof(value));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(timeoutValue, ushort.MaxValue, nameof(value));
        }
    }
}
