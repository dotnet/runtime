// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    public readonly struct OSPlatform : IEquatable<OSPlatform>
    {
        private readonly string _osPlatform;

        public static OSPlatform Android { get; } = new OSPlatform("ANDROID");

        public static OSPlatform Browser { get; } = new OSPlatform("BROWSER");

        public static OSPlatform FreeBSD { get; } = new OSPlatform("FREEBSD");

        public static OSPlatform Linux { get; } = new OSPlatform("LINUX");

        public static OSPlatform macOS { get; } = new OSPlatform("MACOS");

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] // superseded by macOS
        public static OSPlatform OSX { get; } = new OSPlatform("OSX");

        public static OSPlatform iOS { get; } = new OSPlatform("IOS");

        public static OSPlatform tvOS { get; } = new OSPlatform("TVOS");

        public static OSPlatform watchOS { get; } = new OSPlatform("WATCHOS");

        public static OSPlatform Windows { get; } = new OSPlatform("WINDOWS");

        internal bool IsCurrent { get; } // this information is cached because it's frequently used

        private OSPlatform(string osPlatform)
        {
            if (osPlatform == null) throw new ArgumentNullException(nameof(osPlatform));
            if (osPlatform.Length == 0) throw new ArgumentException(SR.Argument_EmptyValue, nameof(osPlatform));

            _osPlatform = osPlatform;
            IsCurrent = RuntimeInformation.IsCurrentOSPlatform(osPlatform);
        }

        /// <summary>
        /// Creates a new OSPlatform instance.
        /// </summary>
        /// <remarks>If you plan to call this method frequently, please consider caching its result.</remarks>
        public static OSPlatform Create(string osPlatform)
        {
            return new OSPlatform(osPlatform);
        }

        public bool Equals(OSPlatform other)
        {
            return Equals(other._osPlatform);
        }

        internal bool Equals(string? other)
        {
            return string.Equals(_osPlatform, other, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is OSPlatform osPlatform && Equals(osPlatform);
        }

        public override int GetHashCode()
        {
            return _osPlatform == null ? 0 : _osPlatform.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return _osPlatform ?? string.Empty;
        }

        public static bool operator ==(OSPlatform left, OSPlatform right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OSPlatform left, OSPlatform right)
        {
            return !(left == right);
        }
    }
}
