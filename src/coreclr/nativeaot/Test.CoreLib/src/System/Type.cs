// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// System.Type is only defined to support C# typeof. We shouldn't have it here since the semantic
// is not very compatible.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public class Type
    {
        private readonly RuntimeTypeHandle _typeHandle;

        private Type(RuntimeTypeHandle typeHandle)
        {
            _typeHandle = typeHandle;
        }

        public RuntimeTypeHandle TypeHandle => _typeHandle;

        public static Type GetTypeFromHandle(RuntimeTypeHandle rh)
        {
            return new Type(rh);
        }

        [Intrinsic]
        public static bool operator ==(Type left, Type right)
        {
            return RuntimeTypeHandle.GetValueInternal(left._typeHandle) == RuntimeTypeHandle.GetValueInternal(right._typeHandle);
        }

        [Intrinsic]
        public static bool operator !=(Type left, Type right) => !(left == right);

        public override bool Equals(object o) => o is Type && this == (Type)o;

        public override int GetHashCode() => 0;
    }
}
