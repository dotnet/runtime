// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Runtime.InteropServices.WindowsRuntime
{
    // Event registration tokens are 64 bit opaque structures returned from WinRT style event adders, in order
    // to signify a registration of a particular delegate to an event.  The token's only real use is to
    // unregister the same delgate from the event at a later time.
    public struct EventRegistrationToken : IEquatable<EventRegistrationToken>
    {
        private readonly ulong _value;

        [CLSCompliant(false)]
        public EventRegistrationToken(ulong value) => _value = value;

        [CLSCompliant(false)]
        public ulong Value => _value;

        public static bool operator ==(EventRegistrationToken left, EventRegistrationToken right) =>
            left.Equals(right);

        public static bool operator !=(EventRegistrationToken left, EventRegistrationToken right) =>
            !left.Equals(right);

        public override bool Equals(object? obj) =>
            obj is EventRegistrationToken &&
            ((EventRegistrationToken)obj)._value == _value;

        public override int GetHashCode() => _value.GetHashCode();

        public bool Equals(EventRegistrationToken other) => other._value == _value;
    }
}
