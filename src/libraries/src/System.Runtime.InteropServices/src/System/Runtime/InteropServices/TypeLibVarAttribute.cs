// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class TypeLibVarAttribute : Attribute
    {
        public TypeLibVarAttribute(TypeLibVarFlags flags)
        {
            Value = flags;
        }

        public TypeLibVarAttribute(short flags)
        {
            Value = (TypeLibVarFlags)flags;
        }

        public TypeLibVarFlags Value { get; }
    }
}
