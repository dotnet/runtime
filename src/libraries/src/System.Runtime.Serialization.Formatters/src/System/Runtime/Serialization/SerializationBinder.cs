// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    public abstract class SerializationBinder
    {
        public virtual void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = null;
        }

        public abstract Type BindToType(string assemblyName, string typeName);
    }
}
