// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Tests
{
    /// <summary>
    /// Resolves the core assembly with a dummy assembly that has no types.
    /// </summary>
    public class EmptyCoreMetadataAssemblyResolver : MetadataAssemblyResolver
    {
        public EmptyCoreMetadataAssemblyResolver() { }

        public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
        {
            if (assemblyName.Name == "mscorlib")
            {
                if (_coreAssembly == null)
                {
                    // This assembly has no types, so any access to core types will throw
                    _coreAssembly = context.LoadFromByteArray(TestData.s_SimpleNameOnlyImage);
                }

                return _coreAssembly;
            }

            return null;
        }

        private Assembly _coreAssembly;
    }
}
