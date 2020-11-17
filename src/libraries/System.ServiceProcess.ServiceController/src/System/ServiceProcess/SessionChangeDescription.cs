// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ServiceProcess
{
    public readonly struct SessionChangeDescription
    {
        internal SessionChangeDescription(SessionChangeReason reason, int id)
        {
            Reason = reason;
            SessionId = id;
        }

        public SessionChangeReason Reason { get; }

        public int SessionId { get; }

        public override bool Equals(object? obj)
        {
            if (!(obj is SessionChangeDescription))
            {
                return false;
            }

            return Equals((SessionChangeDescription)obj);
        }

        public override int GetHashCode()
        {
            return (int)Reason ^ SessionId;
        }

        public bool Equals(SessionChangeDescription changeDescription)
        {
            return (Reason == changeDescription.Reason) && (SessionId == changeDescription.SessionId);
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
