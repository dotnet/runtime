// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.InteropServices
{
    public readonly struct OSPlatform : IEquatable<OSPlatform>
    {
        public static OSPlatform FreeBSD { get; } = new OSPlatform("FREEBSD");

        public static OSPlatform Linux { get; } = new OSPlatform("LINUX");

        public static OSPlatform OSX { get; } = new OSPlatform("OSX");

        public static OSPlatform Windows { get; } = new OSPlatform("WINDOWS");

        public static OSPlatform Android { get; } = new OSPlatform("ANDROID");

        public static OSPlatform iOS { get; } = new OSPlatform("IOS");

        public static OSPlatform tvOS { get; } = new OSPlatform("TVOS");

        public static OSPlatform Browser { get; } = new OSPlatform("BROWSER");

        public static OSPlatform MacCatalyst { get; } = new OSPlatform("MACCATALYST");

        public static OSPlatform Wasi { get; } = new OSPlatform("WASI");

        public static OSPlatform OpenBSD { get; } = new OSPlatform("OPENBSD");

        public static OSPlatform Haiku { get; } = new OSPlatform("HAIKU");
        internal string Name { get; }

        private OSPlatform(string osPlatform)
        {
            ArgumentException.ThrowIfNullOrEmpty(osPlatform);
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

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is OSPlatform osPlatform && Equals(osPlatform);
        }

        public override int GetHashCode()
        {
            return Name == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
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
