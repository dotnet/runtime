// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Internal.TypeSystem;
using Internal.IL.Stubs.StartupCode;

namespace ILCompiler
{
    /// <summary>
    /// Provides compilation group for a native library that compiles the initialize method.
    /// </summary>
    public class NativeLibraryInitializerRootProvider : ICompilationRootProvider
    {
        /// <summary>
        /// Symbolic name under which the managed initializer is exported.
        /// </summary>
        public const string ManagedEntryPointMethodName = "__managed__Startup";

        private ModuleDesc _module;
        private IReadOnlyCollection<MethodDesc> _libraryInitializers;

        public NativeLibraryInitializerRootProvider(ModuleDesc module, IReadOnlyCollection<MethodDesc> libraryInitializers)
        {
            _module = module;
            _libraryInitializers = libraryInitializers;
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            TypeDesc owningType = _module.GetGlobalModuleType();
            NativeLibraryStartupMethod nativeLibStartupCode = new NativeLibraryStartupMethod(owningType, _libraryInitializers);
            rootProvider.AddCompilationRoot(nativeLibStartupCode, "Startup Code Main Method", ManagedEntryPointMethodName);
        }
    }
}
