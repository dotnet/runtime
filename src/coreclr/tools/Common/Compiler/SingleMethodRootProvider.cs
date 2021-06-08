// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Compilation root that is a single method.
    /// </summary>
    public class SingleMethodRootProvider : ICompilationRootProvider
    {
        private MethodDesc _method;

        public SingleMethodRootProvider(MethodDesc method)
        {
            _method = method;
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            rootProvider.AddCompilationRoot(_method,
#if READYTORUN
                rootMinimalDependencies: false,
#endif
                reason: "Single method root");
        }
    }
}
