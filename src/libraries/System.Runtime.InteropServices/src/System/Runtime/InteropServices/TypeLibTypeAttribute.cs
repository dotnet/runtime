// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Struct, Inherited = false)]
    public sealed class TypeLibTypeAttribute : Attribute
    {
        public TypeLibTypeAttribute(TypeLibTypeFlags flags)
        {
            Value = flags;
        }

        public TypeLibTypeAttribute(short flags)
        {
            Value = (TypeLibTypeFlags)flags;
        }

        public TypeLibTypeFlags Value { get; }
    }
}
