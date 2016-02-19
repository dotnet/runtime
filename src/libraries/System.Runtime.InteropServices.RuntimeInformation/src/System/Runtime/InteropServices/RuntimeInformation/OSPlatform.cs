// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    public struct OSPlatform : IEquatable<OSPlatform>
    {
        private readonly string _osPlatform;

        private const string FreeBSDName = "FREEBSD";
        private const string LinuxName = "LINUX";
        private const string OSXName = "OSX";
        private const string WindowsName = "WINDOWS";

        private static readonly OSPlatform s_freebsd = new OSPlatform(FreeBSDName);
        private static readonly OSPlatform s_linux = new OSPlatform(LinuxName);
        private static readonly OSPlatform s_osx = new OSPlatform(OSXName);
        private static readonly OSPlatform s_windows = new OSPlatform(WindowsName);

        public static OSPlatform Linux
        {
            get
            {
                return s_linux;
            }
        }

        public static OSPlatform OSX
        {
            get
            {
                return s_osx;
            }
        }

        public static OSPlatform Windows
        {
            get
            {
                return s_windows;
            }
        }

        private OSPlatform(string osPlatform)
        {
            if (osPlatform == null) throw new ArgumentNullException(nameof(osPlatform));
            if (osPlatform.Length == 0) throw new ArgumentException(SR.Argument_EmptyValue, nameof(osPlatform));
            
            _osPlatform = osPlatform;
        }

        public static OSPlatform Create(string osPlatform)
        {
            return new OSPlatform(osPlatform);
        }

        public bool Equals(OSPlatform other)
        {
            return string.Equals(other._osPlatform, _osPlatform, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (obj is OSPlatform)
            {
                return Equals((OSPlatform)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _osPlatform == null ? 0 : _osPlatform.GetHashCode();
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
