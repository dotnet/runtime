// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** Purpose: Used to mark a class as being serializable
**
**
============================================================*/
namespace System {

    using System;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
[System.Runtime.InteropServices.ComVisible(true)]
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

        public SerializableAttribute() {
        }
    }
}
