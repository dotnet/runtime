// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.InteropServices
{
    public struct OSPlatform : IEquatable<OSPlatform>
    {
        private readonly string _osPlatform;

        private const string WindowsName = "WINDOWS";
        private const string LinuxName = "LINUX";
        private const string OSXName = "OSX";

        private static readonly OSPlatform s_windows = new OSPlatform(WindowsName);
        private static readonly OSPlatform s_linux = new OSPlatform(LinuxName);
        private static readonly OSPlatform s_osx = new OSPlatform(OSXName);

        public static OSPlatform Windows
        {
            get
            {
                return s_windows;
            }
        }

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

        private OSPlatform(string osPlatform)
        {
            if (osPlatform == null) throw new ArgumentNullException("name");
            if (osPlatform.Length == 0) throw new ArgumentException(SR.Argument_EmptyValue, "name");
            
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
