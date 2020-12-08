// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    public readonly struct OSPlatform : IEquatable<OSPlatform>
    {
        public static OSPlatform FreeBSD { get; } = new OSPlatform("FREEBSD");

        public static OSPlatform Linux { get; } = new OSPlatform("LINUX");

        public static OSPlatform OSX { get; } = new OSPlatform("OSX");

        public static OSPlatform Windows { get; } = new OSPlatform("WINDOWS");

        internal string Name { get; }

        private OSPlatform(string osPlatform)
        {
            if (osPlatform == null) throw new ArgumentNullException(nameof(osPlatform));
            if (osPlatform.Length == 0) throw new ArgumentException(SR.Argument_EmptyValue, nameof(osPlatform));

            Name = osPlatform;
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
            return Equals(other.Name);
        }

        internal bool Equals(string? other)
        {
            return string.Equals(Name, other, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is OSPlatform osPlatform && Equals(osPlatform);
        }

        public override int GetHashCode()
        {
            return Name == null ? 0 : Name.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return Name ?? string.Empty;
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
