// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Collections.Generic;

namespace System.Reflection
{
    public abstract class MetadataAssemblyResolver
    {
        public abstract Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName);
    }

    public sealed partial class MetadataLoadContext : System.IDisposable
    {
        public MetadataLoadContext(MetadataAssemblyResolver resolver, string coreAssemblyName = null) { }
        public Assembly CoreAssembly { get { throw null; } }
        public void Dispose() { }
        public IEnumerable<Assembly> GetAssemblies() { throw null; }
        public Assembly LoadFromAssemblyName(AssemblyName assemblyName) { throw null; }
        public Assembly LoadFromAssemblyName(string assemblyName) { throw null; }
        public Assembly LoadFromAssemblyPath(string assemblyPath) { throw null; }
        public Assembly LoadFromByteArray(byte[] assembly) { throw null; }
        public Assembly LoadFromStream(System.IO.Stream assembly) { throw null; }
    }

    public class PathAssemblyResolver : MetadataAssemblyResolver
    {
        public PathAssemblyResolver(IEnumerable<string> assemblyPaths) { throw null; }
        public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName) { throw null; }
    }
}
