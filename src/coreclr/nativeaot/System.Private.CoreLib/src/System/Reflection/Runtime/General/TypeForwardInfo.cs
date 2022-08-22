// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection.Runtime.General
{
    internal struct TypeForwardInfo
    {
        public TypeForwardInfo(RuntimeAssemblyName redirectedAssemblyName, string namespaceName, string typeName)
        {
            Debug.Assert(redirectedAssemblyName != null);
            Debug.Assert(namespaceName != null);
            Debug.Assert(typeName != null);

            RedirectedAssemblyName = redirectedAssemblyName;
            NamespaceName = namespaceName;
            TypeName = typeName;
        }

        public RuntimeAssemblyName RedirectedAssemblyName { get; }
        public string NamespaceName { get; }
        public string TypeName { get; }
    }
}
