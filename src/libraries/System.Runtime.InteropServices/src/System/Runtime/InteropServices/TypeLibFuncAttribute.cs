// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class TypeLibFuncAttribute : Attribute
    {
        public TypeLibFuncAttribute(TypeLibFuncFlags flags)
        {
            Value = flags;
        }

        public TypeLibFuncAttribute(short flags)
        {
            Value = (TypeLibFuncFlags)flags;
        }

        public TypeLibFuncFlags Value { get; }
    }
}
