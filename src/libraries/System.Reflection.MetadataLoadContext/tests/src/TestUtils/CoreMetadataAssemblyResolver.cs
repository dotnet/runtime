// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace System.Reflection.Tests
{
    /// <summary>
    /// Resolves the core assembly by creating a simulated core assembly.
    /// </summary>
    public class CoreMetadataAssemblyResolver : MetadataAssemblyResolver
    {
        public CoreMetadataAssemblyResolver() { }

        public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
        {
            string name = assemblyName.Name;

            if (name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("System.Runtime", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("netstandard", StringComparison.OrdinalIgnoreCase) ||
                // For interop attributes such as DllImport and Guid:
                name.Equals("System.Runtime.InteropServices", StringComparison.OrdinalIgnoreCase))
            {
                if (_coreAssembly == null)
                {
                    _coreAssembly = context.LoadFromStream(TestUtils.CreateStreamForCoreAssembly());
                }

                return _coreAssembly;
            }

            return null;
        }

        private Assembly _coreAssembly;
    }
}
