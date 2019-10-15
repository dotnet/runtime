// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Reflection
{
    public abstract partial class MetadataAssemblyResolver
    {
        protected MetadataAssemblyResolver() { }
        public abstract System.Reflection.Assembly Resolve(System.Reflection.MetadataLoadContext context, System.Reflection.AssemblyName assemblyName);
    }
    public sealed partial class MetadataLoadContext : System.IDisposable
    {
        public MetadataLoadContext(System.Reflection.MetadataAssemblyResolver resolver, string coreAssemblyName = null) { }
        public System.Reflection.Assembly CoreAssembly { get { throw null; } }
        public void Dispose() { }
        public System.Collections.Generic.IEnumerable<System.Reflection.Assembly> GetAssemblies() { throw null; }
        public System.Reflection.Assembly LoadFromAssemblyName(System.Reflection.AssemblyName assemblyName) { throw null; }
        public System.Reflection.Assembly LoadFromAssemblyName(string assemblyName) { throw null; }
        public System.Reflection.Assembly LoadFromAssemblyPath(string assemblyPath) { throw null; }
        public System.Reflection.Assembly LoadFromByteArray(byte[] assembly) { throw null; }
        public System.Reflection.Assembly LoadFromStream(System.IO.Stream assembly) { throw null; }
    }
    public partial class PathAssemblyResolver : System.Reflection.MetadataAssemblyResolver
    {
        public PathAssemblyResolver(System.Collections.Generic.IEnumerable<string> assemblyPaths) { }
        public override System.Reflection.Assembly Resolve(System.Reflection.MetadataLoadContext context, System.Reflection.AssemblyName assemblyName) { throw null; }
    }
}
