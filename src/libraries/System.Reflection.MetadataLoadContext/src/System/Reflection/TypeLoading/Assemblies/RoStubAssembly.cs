// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    // A convenience base class for implementing special-purpose RoAssemblies such as sentinels. It exists primarily to reduce the number
    // of files that have to be edited whenever RoAssembly adds or removes an abstract method rather than to imply any meaningful commonality.
    internal abstract class RoStubAssembly : RoAssembly
    {
        internal RoStubAssembly() : base(null!, 0) { }
        public sealed override string Location => throw null!;
        public sealed override MethodInfo EntryPoint => throw null!;
        public sealed override string ImageRuntimeVersion => throw null!;
        public sealed override bool IsDynamic => throw null!;
        public sealed override event ModuleResolveEventHandler? ModuleResolve { add { throw null!; } remove { throw null!; } }
        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => throw null!;
        public sealed override ManifestResourceInfo? GetManifestResourceInfo(string resourceName) => throw null!;
        public sealed override string[] GetManifestResourceNames() => throw null!;
        public sealed override Stream GetManifestResourceStream(string name) => throw null!;
        protected sealed override AssemblyNameData[] ComputeAssemblyReferences() => throw null!;
        protected sealed override AssemblyNameData ComputeNameData() => throw null!;
        internal sealed override RoModule GetRoManifestModule() => throw null!;
        protected sealed override void IterateTypeForwards(TypeForwardHandler handler) => throw null!;
        protected sealed override RoModule LoadModule(string moduleName, bool containsMetadata) => throw null!;
        protected sealed override IEnumerable<AssemblyFileInfo> GetAssemblyFileInfosFromManifest(bool includeManifestModule, bool includeResourceModules) => throw null!;
        protected sealed override RoModule CreateModule(Stream peStream, bool containsMetadata) => throw null!;
    }
}
