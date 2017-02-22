// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Used to mark a class as being serializable
**
**
============================================================*/

using System;
using System.Reflection;

namespace System
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
    public sealed class SerializableAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeType type)
        {
            return (type.Attributes & TypeAttributes.Serializable) == TypeAttributes.Serializable ? new SerializableAttribute() : null;
        }
        internal static bool IsDefined(RuntimeType type)
        {
            return type.IsSerializable;
        }

        public SerializableAttribute()
        {
        }
    }
}
