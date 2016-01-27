// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface: SerializationBinder
**
**
** Purpose: The base class of serialization binders.
**
**
===========================================================*/
namespace System.Runtime.Serialization {
    using System;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class SerializationBinder {

        public virtual void BindToName(Type serializedType, out String assemblyName, out String typeName)
        {
            assemblyName = null;
            typeName = null;
        }

        public abstract Type BindToType(String assemblyName, String typeName);
    }
}
