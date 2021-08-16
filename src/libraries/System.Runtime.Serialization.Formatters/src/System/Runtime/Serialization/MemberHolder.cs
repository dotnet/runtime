// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization
{
    internal sealed class MemberHolder
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        internal readonly Type _memberType;
        internal readonly StreamingContext _context;

        internal MemberHolder(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
            StreamingContext ctx)
        {
            _memberType = type;
            _context = ctx;
        }

        public override int GetHashCode() => _memberType.GetHashCode();

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is MemberHolder mh &&
                ReferenceEquals(mh._memberType, _memberType) &&
                mh._context.State == _context.State;
        }
    }
}
