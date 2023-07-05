// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Reflection.Runtime.General
{
    internal struct RuntimeTypeHandleKey : IEquatable<RuntimeTypeHandleKey>
    {
        public RuntimeTypeHandleKey(RuntimeTypeHandle typeHandle)
        {
            TypeHandle = typeHandle;
        }

        public RuntimeTypeHandle TypeHandle { get; }

        public override bool Equals(object obj)
        {
            if (!(obj is RuntimeTypeHandleKey other))
                return false;
            return Equals(other);
        }

        public bool Equals(RuntimeTypeHandleKey other)
        {
            return TypeHandle.Equals(other.TypeHandle);
        }

        public override int GetHashCode()
        {
            return TypeHandle.GetHashCode();
        }
    }
}
