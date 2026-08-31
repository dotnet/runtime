// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    public class AssemblyNameProxy : MarshalByRefObject
    {
        public AssemblyName GetAssemblyName(string assemblyFile)
        {
            return AssemblyName.GetAssemblyName(assemblyFile);
        }
    }
}
