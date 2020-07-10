// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;

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

        [EditorBrowsable(EditorBrowsableState.Never)] // https://github.com/dotnet/runtime/issues/33331#issuecomment-650326500
        public static OSPlatform OSX { get; } = macOS;

        public static OSPlatform iOS { get; } = new OSPlatform("IOS");

        public static OSPlatform tvOS { get; } = new OSPlatform("TVOS");

        public static OSPlatform watchOS { get; } = new OSPlatform("WATCHOS");

        public static OSPlatform Windows { get; } = new OSPlatform("WINDOWS");

        private OSPlatform(string osPlatform)
        {
            if (osPlatform == null) throw new ArgumentNullException(nameof(osPlatform));
            if (osPlatform.Length == 0) throw new ArgumentException(SR.Argument_EmptyValue, nameof(osPlatform));

            _osPlatform = osPlatform.Equals("OSX", StringComparison.OrdinalIgnoreCase) ? "MACOS" : osPlatform;
        }

        public static OSPlatform Create(string osPlatform)
        {
            return new OSPlatform(osPlatform);
        }

        public bool Equals(OSPlatform other)
        {
            return string.Equals(_osPlatform, other._osPlatform, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            Debug.Fail("The non generic method should not be used by BCL");

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
