// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.InteropServices;

using Internal.Reflection.Augments;

// This type is just stubbed out to be harmonious with CoreCLR
namespace System.Runtime.Loader
{
    public partial class AssemblyLoadContext
    {
        internal static Assembly[] GetLoadedAssemblies() => ReflectionAugments.GetLoadedAssemblies();

        public Assembly LoadFromAssemblyName(AssemblyName assemblyName)
        {
            return Assembly.Load(assemblyName);
        }

#pragma warning disable IDE0060
        private static IntPtr InitializeAssemblyLoadContext(IntPtr ptrAssemblyLoadContext, bool fRepresentsTPALoadContext, bool isCollectible)
        {
            return IntPtr.Zero;
        }

        private static void PrepareForAssemblyLoadContextRelease(IntPtr ptrNativeAssemblyLoadContext, IntPtr ptrAssemblyLoadContextStrong)
        {
        }

        public static AssemblyLoadContext? GetLoadContext(Assembly assembly)
        {
            return Default;
        }

        public void SetProfileOptimizationRoot(string directoryPath)
        {
        }

        public void StartProfileOptimization(string profile)
        {
        }

        private static Assembly InternalLoadFromPath(string? assemblyPath, string? nativeImagePath)
        {
            ArgumentNullException.ThrowIfNull(assemblyPath);

            throw new PlatformNotSupportedException();
        }
#pragma warning restore IDE0060

#pragma warning disable CA1822, IDE0060
        internal Assembly InternalLoad(ReadOnlySpan<byte> rawAssembly, ReadOnlySpan<byte> rawSymbols)
        {
            if (rawAssembly.IsEmpty)
                throw new ArgumentNullException(nameof(rawAssembly));

            throw new PlatformNotSupportedException();
        }
#pragma warning restore CA1822, IDE0060

        private void ReferenceUnreferencedEvents()
        {
            // Dummy method to avoid CS0067 "Event is never used" warning.
            // These are defined in the shared partition and it's not worth the ifdeffing.
            _ = AssemblyLoad;
            _ = ResourceResolve;
            _ = _resolving;
            _ = TypeResolve;
            _ = AssemblyResolve;
        }
    }
}
