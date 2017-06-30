// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Runtime.Serialization
{
    internal sealed class MemberHolder
    {
        internal readonly MemberInfo[] _members = null;
        internal readonly Type _memberType;
        internal readonly StreamingContext _context;

        internal MemberHolder(Type type, StreamingContext ctx)
        {
            _memberType = type;
            _context = ctx;
        }

        public override int GetHashCode() => _memberType.GetHashCode();

        public override bool Equals(object obj)
        {
            var mh = obj as MemberHolder;
            return
                mh != null && 
                ReferenceEquals(mh._memberType, _memberType) && 
                mh._context.State == _context.State;
        }
    }
}
