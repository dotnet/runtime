// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // Event registration tokens are 64 bit opaque structures returned from WinRT style event adders, in order
    // to signify a registration of a particular delegate to an event.  The token's only real use is to
    // unregister the same delgate from the event at a later time.
    public struct EventRegistrationToken : IEquatable<EventRegistrationToken>
    {
        internal ulong m_value;

        internal EventRegistrationToken(ulong value)
        {
            m_value = value;
        }

        internal ulong Value
        {
            get { return m_value; }
        }

        public static bool operator ==(EventRegistrationToken left, EventRegistrationToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EventRegistrationToken left, EventRegistrationToken right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EventRegistrationToken))
            {
                return false;
            }

            return ((EventRegistrationToken)obj).Value == Value;
        }

        public override int GetHashCode()
        {
            return m_value.GetHashCode();
        }

        bool IEquatable<EventRegistrationToken>.Equals(EventRegistrationToken other)
        {
            return Equals(other);
        }

        private bool Equals(EventRegistrationToken other)
        {
            return other.m_value == m_value;
        }
    }
}
