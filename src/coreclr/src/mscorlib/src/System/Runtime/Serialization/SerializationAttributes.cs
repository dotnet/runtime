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
namespace System.Runtime.Serialization
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Field, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class OptionalFieldAttribute : Attribute 
    {
        int versionAdded = 1;
        public OptionalFieldAttribute() { }
        
        public int VersionAdded 
        {
            get {
                return this.versionAdded;
            }
            set {
                if (value < 1)
                    throw new ArgumentException(Environment.GetResourceString("Serialization_OptionalFieldVersionValue"));
                Contract.EndContractBlock();
                this.versionAdded = value;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class OnSerializingAttribute : Attribute 
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class OnSerializedAttribute : Attribute 
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class OnDeserializingAttribute : Attribute 
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class OnDeserializedAttribute : Attribute 
    {
    }

}
