// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Various Attributes for Serialization 
**
**
============================================================*/

using System;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace System.Runtime.Serialization
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class OptionalFieldAttribute : Attribute
    {
        private int versionAdded = 1;
        public OptionalFieldAttribute() { }

        public int VersionAdded
        {
            get
            {
                return versionAdded;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentException(SR.Serialization_OptionalFieldVersionValue);
                Contract.EndContractBlock();
                versionAdded = value;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class OnSerializingAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class OnSerializedAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class OnDeserializingAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class OnDeserializedAttribute : Attribute
    {
    }
}
