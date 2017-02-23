// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Used to mark a member as being not-serialized
**
**
============================================================*/

using System.Reflection;

namespace System
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class NonSerializedAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeFieldInfo field)
        {
            if ((field.Attributes & FieldAttributes.NotSerialized) == 0)
                return null;

            return new NonSerializedAttribute();
        }

        internal static bool IsDefined(RuntimeFieldInfo field)
        {
            return (field.Attributes & FieldAttributes.NotSerialized) != 0;
        }

        public NonSerializedAttribute() { }
    }
}
