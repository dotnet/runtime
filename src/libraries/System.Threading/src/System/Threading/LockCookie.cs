// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Threading
{
    /// <summary>
    /// Stores the lock state of a <see cref="ReaderWriterLock"/> before its lock state is changed, such that the lock state may
    /// later be restored.
    /// </summary>
    public struct LockCookie : IEquatable<LockCookie>
    {
        internal LockCookieFlags _flags;
        internal ushort _readerLevel;
        internal ushort _writerLevel;
        internal int _threadID;

        public override int GetHashCode() =>
            (int)_flags + _readerLevel + _writerLevel + _threadID;

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is LockCookie other && Equals(other);

        public bool Equals(LockCookie obj) =>
            _flags == obj._flags &&
            _readerLevel == obj._readerLevel &&
            _writerLevel == obj._writerLevel &&
            _threadID == obj._threadID;

        public static bool operator ==(LockCookie a, LockCookie b) => a.Equals(b);

        public static bool operator !=(LockCookie a, LockCookie b) => !a.Equals(b);
    }
}
