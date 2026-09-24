// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    public sealed class ReadyToRunDelegateCtorRootProvider(ReadyToRunCompilerContext context) : ICompilationRootProvider
    {
        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            MetadataType delegateType = context.SystemModule.GetType("System"u8, "Delegate"u8);

            foreach (MethodDesc method in delegateType.GetMethods())
            {
                string methodName = method.GetName();
                if (methodName is "CtorClosed" or "CtorClosedStatic" or "CtorOpen" or "DelegateConstruct")
                {
                    rootProvider.AddCompilationRoot(
                        method,
                        rootMinimalDependencies: false,
                        $"Wasm ReadyToRun delegate constructor {delegateType}.{methodName}");
                }
            }
        }
    }
}
