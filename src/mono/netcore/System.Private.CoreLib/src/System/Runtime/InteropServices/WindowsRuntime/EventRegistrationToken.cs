// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.WindowsRuntime
{
    public struct EventRegistrationToken : IEquatable<EventRegistrationToken>
    {
        [CLSCompliant(false)]
        public EventRegistrationToken(ulong value) => throw new PlatformNotSupportedException();

        [CLSCompliant(false)]
        public ulong Value => throw new PlatformNotSupportedException();

        public static bool operator ==(EventRegistrationToken left, EventRegistrationToken right) =>
            throw new PlatformNotSupportedException();

        public static bool operator !=(EventRegistrationToken left, EventRegistrationToken right) =>
            throw new PlatformNotSupportedException();

        public override bool Equals(object? obj) => throw new PlatformNotSupportedException();

        public override int GetHashCode() => throw new PlatformNotSupportedException();

        public bool Equals(EventRegistrationToken other) => throw new PlatformNotSupportedException();
    }
}
