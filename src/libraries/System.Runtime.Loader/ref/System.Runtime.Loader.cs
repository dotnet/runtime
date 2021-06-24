// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Reflection.Metadata
{
    public static partial class AssemblyExtensions
    {
        [System.CLSCompliantAttribute(false)]
        public unsafe static bool TryGetRawMetadata(this System.Reflection.Assembly assembly, out byte* blob, out int length) { throw null; }
        public static void ApplyUpdate(Assembly assembly, ReadOnlySpan<byte> metadataDelta, ReadOnlySpan<byte> ilDelta, ReadOnlySpan<byte> pdbDelta) { throw null; }
    }
    public static partial class MetadataUpdater
    {
        public static void ApplyUpdate(Assembly assembly, ReadOnlySpan<byte> metadataDelta, ReadOnlySpan<byte> ilDelta, ReadOnlySpan<byte> pdbDelta) { throw null; }
        public static bool IsSupported { get { throw null; } }
    }
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class MetadataUpdateHandlerAttribute : System.Attribute
    {
        public MetadataUpdateHandlerAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type handlerType) { }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
        public System.Type HandlerType { get { throw null; } }
    }
}
namespace System.Runtime.Loader
{
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("maccatalyst")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
    public sealed partial class AssemblyDependencyResolver
    {
        public AssemblyDependencyResolver(string componentAssemblyPath) { }
        public string? ResolveAssemblyToPath(System.Reflection.AssemblyName assemblyName) { throw null; }
        public string? ResolveUnmanagedDllToPath(string unmanagedDllName) { throw null; }
    }
    public partial class AssemblyLoadContext
    {
        protected AssemblyLoadContext() { }
        protected AssemblyLoadContext(bool isCollectible) { }
        public AssemblyLoadContext(string? name, bool isCollectible = false) { }
        public static System.Collections.Generic.IEnumerable<System.Runtime.Loader.AssemblyLoadContext> All { get { throw null; } }
        public System.Collections.Generic.IEnumerable<System.Reflection.Assembly> Assemblies { get { throw null; } }
        public static System.Runtime.Loader.AssemblyLoadContext? CurrentContextualReflectionContext { get { throw null; } }
        public static System.Runtime.Loader.AssemblyLoadContext Default { get { throw null; } }
        public bool IsCollectible { get { throw null; } }
        public string? Name { get { throw null; } }
        public event System.Func<System.Runtime.Loader.AssemblyLoadContext, System.Reflection.AssemblyName, System.Reflection.Assembly?>? Resolving { add { } remove { } }
        public event System.Func<System.Reflection.Assembly, string, System.IntPtr>? ResolvingUnmanagedDll { add { } remove { } }
        public event System.Action<System.Runtime.Loader.AssemblyLoadContext>? Unloading { add { } remove { } }
        public System.Runtime.Loader.AssemblyLoadContext.ContextualReflectionScope EnterContextualReflection() { throw null; }
        public static System.Runtime.Loader.AssemblyLoadContext.ContextualReflectionScope EnterContextualReflection(System.Reflection.Assembly? activating) { throw null; }
        ~AssemblyLoadContext() { }
        public static System.Reflection.AssemblyName GetAssemblyName(string assemblyPath) { throw null; }
        public static System.Runtime.Loader.AssemblyLoadContext? GetLoadContext(System.Reflection.Assembly assembly) { throw null; }
        protected virtual System.Reflection.Assembly? Load(System.Reflection.AssemblyName assemblyName) { throw null; }
        public System.Reflection.Assembly LoadFromAssemblyName(System.Reflection.AssemblyName assemblyName) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded assembly depends on might be removed")]
        public System.Reflection.Assembly LoadFromAssemblyPath(string assemblyPath) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded assembly depends on might be removed")]
        public System.Reflection.Assembly LoadFromNativeImagePath(string nativeImagePath, string? assemblyPath) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded assembly depends on might be removed")]
        public System.Reflection.Assembly LoadFromStream(System.IO.Stream assembly) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded assembly depends on might be removed")]
        public System.Reflection.Assembly LoadFromStream(System.IO.Stream assembly, System.IO.Stream? assemblySymbols) { throw null; }
        protected virtual System.IntPtr LoadUnmanagedDll(string unmanagedDllName) { throw null; }
        protected System.IntPtr LoadUnmanagedDllFromPath(string unmanagedDllPath) { throw null; }
        public void SetProfileOptimizationRoot(string directoryPath) { }
        public void StartProfileOptimization(string? profile) { }
        public override string ToString() { throw null; }
        public void Unload() { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public partial struct ContextualReflectionScope : System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public void Dispose() { }
        }
    }
}
