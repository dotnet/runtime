// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceProcess
{
    public readonly struct SessionChangeDescription
    {
        private readonly SessionChangeReason _reason;
        private readonly int _id;

        internal SessionChangeDescription(SessionChangeReason reason, int id)
        {
            _reason = reason;
            _id = id;
        }

        public SessionChangeReason Reason
        {
            get
            {
                return _reason;
            }
        }

        public int SessionId
        {
            get
            {
                return _id;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is SessionChangeDescription))
            {
                return false;
            }
            else
            {
                return Equals((SessionChangeDescription)obj);
            }
        }

        public override int GetHashCode()
        {
            return (int)_reason ^ _id;
        }

        public bool Equals(SessionChangeDescription changeDescription)
        {
            return (_reason == changeDescription._reason) && (_id == changeDescription._id);
        }

        public static bool operator ==(SessionChangeDescription a, SessionChangeDescription b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(SessionChangeDescription a, SessionChangeDescription b)
        {
            return !a.Equals(b);
        }
    }
}

