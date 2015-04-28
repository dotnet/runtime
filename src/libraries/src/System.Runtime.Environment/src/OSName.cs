// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.InteropServices
{
    public struct OSName : IEquatable<OSName>
    {
        private readonly string _name;

        private const string WindowsName = "WINDOWS";
        private const string LinuxName = "LINUX";
        private const string OSXName = "OSX";

        public static readonly OSName Windows = new OSName(WindowsName);
        public static readonly OSName Linux = new OSName(LinuxName);
        public static readonly OSName OSX = new OSName(OSXName);

        public OSName(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name.Length == 0) throw new ArgumentException(SR.Argument_EmptyValue, "name");

            _name = name;
        }

        public bool Equals(OSName other)
        {
            return string.Equals(other._name, _name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (obj is OSName)
            {
                return Equals((OSName)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _name == null ? 0 : _name.GetHashCode();
        }

        public override string ToString()
        {
            return _name ?? string.Empty;
        }

        public static bool operator ==(OSName left, OSName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OSName left, OSName right)
        {
            return !(left == right);
        }
    }
}
