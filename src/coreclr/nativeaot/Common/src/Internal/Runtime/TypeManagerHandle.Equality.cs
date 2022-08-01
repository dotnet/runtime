// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.Runtime
{
    public unsafe partial struct TypeManagerHandle : IEquatable<TypeManagerHandle>
    {
        public static bool operator ==(TypeManagerHandle left, TypeManagerHandle right)
        {
            return left._handleValue == right._handleValue;
        }

        public static bool operator !=(TypeManagerHandle left, TypeManagerHandle right)
        {
            return left._handleValue != right._handleValue;
        }

        public override int GetHashCode()
        {
            return ((IntPtr)_handleValue).GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (!(o is TypeManagerHandle))
                return false;

            return _handleValue == ((TypeManagerHandle)o)._handleValue;
        }

        public bool Equals(TypeManagerHandle other)
        {
            return _handleValue == other._handleValue;
        }

        public string LowLevelToString()
        {
            return ((IntPtr)_handleValue).LowLevelToString();
        }
    }
}
