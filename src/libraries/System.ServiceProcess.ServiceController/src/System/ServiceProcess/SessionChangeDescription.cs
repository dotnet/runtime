// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ServiceProcess
{
    public readonly struct SessionChangeDescription
#if NETCOREAPP
        : IEquatable<SessionChangeDescription>
#endif
    {
        internal SessionChangeDescription(SessionChangeReason reason, int id)
        {
            Reason = reason;
            SessionId = id;
        }

        public SessionChangeReason Reason { get; }

        public int SessionId { get; }

        public override int GetHashCode() =>
            (int)Reason ^ SessionId;

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is SessionChangeDescription other && Equals(other);

        public bool Equals(SessionChangeDescription changeDescription) =>
            (Reason == changeDescription.Reason) &&
            (SessionId == changeDescription.SessionId);

        public static bool operator ==(SessionChangeDescription a, SessionChangeDescription b) =>
            a.Equals(b);

        public static bool operator !=(SessionChangeDescription a, SessionChangeDescription b) =>
            !a.Equals(b);
    }
}
