// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// This file is auto-generated and any changes to it will be lost.
// ------------------------------------------------------------------------------

namespace Microsoft.DotNet.PlatformAbstractions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("HashCodeCombiner has been deprecated. Use System.HashCode instead.")]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct HashCodeCombiner
    {
        private int _dummyPrimitive;
        public int CombinedHash { [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]get { throw null; } }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Add(int i) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Add(object? o) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Add(string? s) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Add<TValue>(TValue? value, System.Collections.Generic.IEqualityComparer<TValue> comparer) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static Microsoft.DotNet.PlatformAbstractions.HashCodeCombiner Start() { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyModel
{
    public partial class CompilationLibrary : Microsoft.Extensions.DependencyModel.Library
    {
        public CompilationLibrary(string type, string name, string version, string? hash, System.Collections.Generic.IEnumerable<string> assemblies, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.Dependency> dependencies, bool serviceable) : base (default(string), default(string), default(string), default(string), default(System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.Dependency>), default(bool)) { }
        public CompilationLibrary(string type, string name, string version, string? hash, System.Collections.Generic.IEnumerable<string> assemblies, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.Dependency> dependencies, bool serviceable, string? path, string? hashPath) : base (default(string), default(string), default(string), default(string), default(System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.Dependency>), default(bool)) { }
        public System.Collections.Generic.IReadOnlyList<string> Assemblies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> ResolveReferencePaths() { throw null; }
        public System.Collections.Generic.IEnumerable<string> ResolveReferencePaths(params Microsoft.Extensions.DependencyModel.Resolution.ICompilationAssemblyResolver[] customResolvers) { throw null; }
    }
    public partial class CompilationOptions
    {
        public CompilationOptions(System.Collections.Generic.IEnumerable<string?> defines, string? languageVersion, string? platform, bool? allowUnsafe, bool? warningsAsErrors, bool? optimize, string? keyFile, bool? delaySign, bool? publicSign, string? debugType, bool? emitEntryPoint, bool? generateXmlDocumentation) { }
        public bool? AllowUnsafe { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string? DebugType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.Extensions.DependencyModel.CompilationOptions Default { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<string?> Defines { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool? DelaySign { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool? EmitEntryPoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool? GenerateXmlDocumentation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string? KeyFile { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string? LanguageVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool? Optimize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string? Platform { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool? PublicSign { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool? WarningsAsErrors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct Dependency : System.IEquatable<Microsoft.Extensions.DependencyModel.Dependency>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public Dependency(string name, string version) { throw null; }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Equals(Microsoft.Extensions.DependencyModel.Dependency other) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public partial class DependencyContext
    {
        public DependencyContext(Microsoft.Extensions.DependencyModel.TargetInfo target, Microsoft.Extensions.DependencyModel.CompilationOptions compilationOptions, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.CompilationLibrary> compileLibraries, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeLibrary> runtimeLibraries, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeFallbacks> runtimeGraph) { }
        public Microsoft.Extensions.DependencyModel.CompilationOptions CompilationOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.CompilationLibrary> CompileLibraries { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.CodeAnalysis.RequiresAssemblyFilesAttribute("DependencyContext for an assembly from a application published as single-file is not supported. The method will return null. Make sure the calling code can handle this case.")]
        public static Microsoft.Extensions.DependencyModel.DependencyContext? Default { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.RuntimeFallbacks> RuntimeGraph { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.RuntimeLibrary> RuntimeLibraries { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.Extensions.DependencyModel.TargetInfo Target { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.CodeAnalysis.RequiresAssemblyFilesAttribute("DependencyContext for an assembly from a application published as single-file is not supported. The method will return null. Make sure the calling code can handle this case.")]
        public static Microsoft.Extensions.DependencyModel.DependencyContext? Load(System.Reflection.Assembly assembly) { throw null; }
        public Microsoft.Extensions.DependencyModel.DependencyContext Merge(Microsoft.Extensions.DependencyModel.DependencyContext other) { throw null; }
    }
    public static partial class DependencyContextExtensions
    {
        public static System.Collections.Generic.IEnumerable<System.Reflection.AssemblyName> GetDefaultAssemblyNames(this Microsoft.Extensions.DependencyModel.DependencyContext self) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Reflection.AssemblyName> GetDefaultAssemblyNames(this Microsoft.Extensions.DependencyModel.RuntimeLibrary self, Microsoft.Extensions.DependencyModel.DependencyContext context) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> GetDefaultNativeAssets(this Microsoft.Extensions.DependencyModel.DependencyContext self) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> GetDefaultNativeAssets(this Microsoft.Extensions.DependencyModel.RuntimeLibrary self, Microsoft.Extensions.DependencyModel.DependencyContext context) { throw null; }
        public static System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeFile> GetDefaultNativeRuntimeFileAssets(this Microsoft.Extensions.DependencyModel.DependencyContext self) { throw null; }
        public static System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeFile> GetDefaultNativeRuntimeFileAssets(this Microsoft.Extensions.DependencyModel.RuntimeLibrary self, Microsoft.Extensions.DependencyModel.DependencyContext context) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Reflection.AssemblyName> GetRuntimeAssemblyNames(this Microsoft.Extensions.DependencyModel.DependencyContext self, string runtimeIdentifier) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Reflection.AssemblyName> GetRuntimeAssemblyNames(this Microsoft.Extensions.DependencyModel.RuntimeLibrary self, Microsoft.Extensions.DependencyModel.DependencyContext context, string runtimeIdentifier) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> GetRuntimeNativeAssets(this Microsoft.Extensions.DependencyModel.DependencyContext self, string runtimeIdentifier) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> GetRuntimeNativeAssets(this Microsoft.Extensions.DependencyModel.RuntimeLibrary self, Microsoft.Extensions.DependencyModel.DependencyContext context, string runtimeIdentifier) { throw null; }
        public static System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeFile> GetRuntimeNativeRuntimeFileAssets(this Microsoft.Extensions.DependencyModel.DependencyContext self, string runtimeIdentifier) { throw null; }
        public static System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeFile> GetRuntimeNativeRuntimeFileAssets(this Microsoft.Extensions.DependencyModel.RuntimeLibrary self, Microsoft.Extensions.DependencyModel.DependencyContext context, string runtimeIdentifier) { throw null; }
    }
    public partial class DependencyContextJsonReader : Microsoft.Extensions.DependencyModel.IDependencyContextReader, System.IDisposable
    {
        public DependencyContextJsonReader() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public Microsoft.Extensions.DependencyModel.DependencyContext Read(System.IO.Stream stream) { throw null; }
    }
    public partial class DependencyContextLoader
    {
        public DependencyContextLoader() { }
        public static Microsoft.Extensions.DependencyModel.DependencyContextLoader Default { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.CodeAnalysis.RequiresAssemblyFilesAttribute("DependencyContext for an assembly from a application published as single-file is not supported. The method will return null. Make sure the calling code can handle this case.")]
        public Microsoft.Extensions.DependencyModel.DependencyContext? Load(System.Reflection.Assembly assembly) { throw null; }
    }
    public partial class DependencyContextWriter
    {
        public DependencyContextWriter() { }
        public void Write(Microsoft.Extensions.DependencyModel.DependencyContext context, System.IO.Stream stream) { }
    }
    public partial interface IDependencyContextReader : System.IDisposable
    {
        Microsoft.Extensions.DependencyModel.DependencyContext Read(System.IO.Stream stream);
    }
    public partial class Library
    {
        public Library(string type, string name, string version, string? hash, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.Dependency> dependencies, bool serviceable) { }
        public Library(string type, string name, string version, string? hash, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.Dependency> dependencies, bool serviceable, string? path, string? hashPath) { }
        public Library(string type, string name, string version, string? hash, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.Dependency> dependencies, bool serviceable, string? path, string? hashPath, string? runtimeStoreManifestName = null) { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.Dependency> Dependencies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string? Hash { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string? HashPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string? Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string? RuntimeStoreManifestName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Serviceable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ResourceAssembly
    {
        public ResourceAssembly(string path, string locale) { }
        public string Locale { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class RuntimeAssembly
    {
        public RuntimeAssembly(string assemblyName, string path) { }
        public System.Reflection.AssemblyName Name { get { throw null; } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.Extensions.DependencyModel.RuntimeAssembly Create(string path) { throw null; }
    }
    public partial class RuntimeAssetGroup
    {
        public RuntimeAssetGroup(string? runtime, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeFile> runtimeFiles) { }
        public RuntimeAssetGroup(string? runtime, System.Collections.Generic.IEnumerable<string> assetPaths) { }
        public RuntimeAssetGroup(string? runtime, params string[] assetPaths) { }
        public System.Collections.Generic.IReadOnlyList<string> AssetPaths { get { throw null; } }
        public string? Runtime { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.RuntimeFile> RuntimeFiles { get { throw null; } }
    }
    public partial class RuntimeFallbacks
    {
        public RuntimeFallbacks(string runtime, System.Collections.Generic.IEnumerable<string?> fallbacks) { }
        public RuntimeFallbacks(string runtime, params string?[] fallbacks) { }
        public System.Collections.Generic.IReadOnlyList<string?> Fallbacks { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Runtime { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class RuntimeFile
    {
        public RuntimeFile(string path, string? assemblyVersion, string? fileVersion) { }
        public string? AssemblyVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string? FileVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class RuntimeLibrary : Microsoft.Extensions.DependencyModel.Library
    {
        public RuntimeLibrary(string type, string name, string version, string? hash, System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> runtimeAssemblyGroups, System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> nativeLibraryGroups, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.ResourceAssembly> resourceAssemblies, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.Dependency> dependencies, bool serviceable) : base (default(string), default(string), default(string), default(string), default(System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.Dependency>), default(bool)) { }
        public RuntimeLibrary(string type, string name, string version, string? hash, System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> runtimeAssemblyGroups, System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> nativeLibraryGroups, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.ResourceAssembly> resourceAssemblies, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.Dependency> dependencies, bool serviceable, string? path, string? hashPath) : base (default(string), default(string), default(string), default(string), default(System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.Dependency>), default(bool)) { }
        public RuntimeLibrary(string type, string name, string version, string? hash, System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> runtimeAssemblyGroups, System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> nativeLibraryGroups, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.ResourceAssembly> resourceAssemblies, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.Dependency> dependencies, bool serviceable, string? path, string? hashPath, string? runtimeStoreManifestName) : base (default(string), default(string), default(string), default(string), default(System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.Dependency>), default(bool)) { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> NativeLibraryGroups { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.ResourceAssembly> ResourceAssemblies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> RuntimeAssemblyGroups { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class TargetInfo
    {
        public TargetInfo(string framework, string? runtime, string? runtimeSignature, bool isPortable) { }
        public string Framework { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsPortable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string? Runtime { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string? RuntimeSignature { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public partial class AppBaseCompilationAssemblyResolver : Microsoft.Extensions.DependencyModel.Resolution.ICompilationAssemblyResolver
    {
        public AppBaseCompilationAssemblyResolver() { }
        public AppBaseCompilationAssemblyResolver(string basePath) { }
        public bool TryResolveAssemblyPaths(Microsoft.Extensions.DependencyModel.CompilationLibrary library, System.Collections.Generic.List<string>? assemblies) { throw null; }
    }
    public partial class CompositeCompilationAssemblyResolver : Microsoft.Extensions.DependencyModel.Resolution.ICompilationAssemblyResolver
    {
        public CompositeCompilationAssemblyResolver(Microsoft.Extensions.DependencyModel.Resolution.ICompilationAssemblyResolver[] resolvers) { }
        public bool TryResolveAssemblyPaths(Microsoft.Extensions.DependencyModel.CompilationLibrary library, System.Collections.Generic.List<string>? assemblies) { throw null; }
    }
    public partial class DotNetReferenceAssembliesPathResolver
    {
        public static readonly string DotNetReferenceAssembliesPathEnv;
        public DotNetReferenceAssembliesPathResolver() { }
        public static string? Resolve() { throw null; }
    }
    public partial interface ICompilationAssemblyResolver
    {
        bool TryResolveAssemblyPaths(Microsoft.Extensions.DependencyModel.CompilationLibrary library, System.Collections.Generic.List<string>? assemblies);
    }
    public partial class PackageCompilationAssemblyResolver : Microsoft.Extensions.DependencyModel.Resolution.ICompilationAssemblyResolver
    {
        public PackageCompilationAssemblyResolver() { }
        public PackageCompilationAssemblyResolver(string nugetPackageDirectory) { }
        public bool TryResolveAssemblyPaths(Microsoft.Extensions.DependencyModel.CompilationLibrary library, System.Collections.Generic.List<string>? assemblies) { throw null; }
    }
    public partial class ReferenceAssemblyPathResolver : Microsoft.Extensions.DependencyModel.Resolution.ICompilationAssemblyResolver
    {
        public ReferenceAssemblyPathResolver() { }
        public ReferenceAssemblyPathResolver(string? defaultReferenceAssembliesPath, string[] fallbackSearchPaths) { }
        public bool TryResolveAssemblyPaths(Microsoft.Extensions.DependencyModel.CompilationLibrary library, System.Collections.Generic.List<string>? assemblies) { throw null; }
    }
}
namespace System.Collections.Generic
{
    public static partial class CollectionExtensions
    {
        public static System.Collections.Generic.IEnumerable<string> GetDefaultAssets(this System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> self) { throw null; }
        public static Microsoft.Extensions.DependencyModel.RuntimeAssetGroup? GetDefaultGroup(this System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> self) { throw null; }
        public static System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeFile> GetDefaultRuntimeFileAssets(this System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> self) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> GetRuntimeAssets(this System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> self, string runtime) { throw null; }
        public static System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeFile> GetRuntimeFileAssets(this System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> self, string runtime) { throw null; }
        public static Microsoft.Extensions.DependencyModel.RuntimeAssetGroup? GetRuntimeGroup(this System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyModel.RuntimeAssetGroup> self, string runtime) { throw null; }
    }
}
