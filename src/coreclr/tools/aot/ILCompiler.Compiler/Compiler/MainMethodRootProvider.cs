// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.IL.Stubs.StartupCode;

namespace ILCompiler
{
    /// <summary>
    /// Computes a compilation root based on the entrypoint of the assembly.
    /// </summary>
    public class MainMethodRootProvider : ICompilationRootProvider
    {
        /// <summary>
        /// Symbolic name under which the managed entrypoint is exported.
        /// </summary>
        public const string ManagedEntryPointMethodName = "__managed__Main";

        private readonly EcmaModule _module;
        private readonly string _entryPointName;
        private readonly IReadOnlyCollection<MethodDesc> _libraryInitializers;
        private readonly bool _generateLibraryAndModuleInitializers;

        public MainMethodRootProvider(EcmaModule module, IReadOnlyCollection<MethodDesc> libraryInitializers, bool generateLibraryAndModuleInitializers, string entryPointName = null)
        {
            _module = module;
            _entryPointName = entryPointName ?? ManagedEntryPointMethodName;
            _libraryInitializers = libraryInitializers;
            _generateLibraryAndModuleInitializers = generateLibraryAndModuleInitializers;
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            MethodDesc mainMethod = _module.EntryPoint;
            if (mainMethod == null)
                throw new Exception("No managed entrypoint defined for executable module");

            TypeDesc owningType = _module.GetGlobalModuleType();
            var startupCodeMain = new StartupCodeMainMethod(owningType, mainMethod, _libraryInitializers, _generateLibraryAndModuleInitializers);

            rootProvider.AddCompilationRoot(startupCodeMain, $"Startup Code Main Method for {_module.Assembly.GetName().Name}", new Utf8String(_entryPointName));
        }
    }
}
