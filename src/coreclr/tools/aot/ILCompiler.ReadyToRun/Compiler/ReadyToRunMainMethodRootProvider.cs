// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// Computes a compilation root based on the entrypoint of the assembly.
    /// </summary>
    public class ReadyToRunMainMethodRootProvider : ICompilationRootProvider
    {
        private readonly EcmaModule _module;

        public ReadyToRunMainMethodRootProvider(EcmaModule module)
        {
            _module = module;
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            MethodDesc mainMethod = _module.EntryPoint;
            if (mainMethod is not null)
            {
                rootProvider.AddCompilationRoot(mainMethod, rootMinimalDependencies: false, $"{_module.Assembly.GetName()} Main Method");
            }
        }
    }
}
