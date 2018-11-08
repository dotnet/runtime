// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Tests
{
    public class FuncMetadataAssemblyResolver : MetadataAssemblyResolver
    {
        System.Func<System.Reflection.MetadataLoadContext, System.Reflection.AssemblyName, System.Reflection.Assembly> func;
        public FuncMetadataAssemblyResolver(System.Func<System.Reflection.MetadataLoadContext, System.Reflection.AssemblyName, System.Reflection.Assembly> func) 
        {
            this.func = func ?? throw new ArgumentException("", nameof(func));
        }

        public override Assembly Resolve(System.Reflection.MetadataLoadContext context, AssemblyName assemblyName)
        {
            Debug.Assert(assemblyName != null);

            Assembly assembly = func.Invoke(context, assemblyName);
            return assembly;
        }
    }
}
