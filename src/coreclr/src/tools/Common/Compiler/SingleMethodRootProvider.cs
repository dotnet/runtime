// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Compilation root that is a single method.
    /// </summary>
    public class SingleMethodRootProvider : ICompilationRootProvider
    {
        private IEnumerable<MethodDesc> _methods;

        public SingleMethodRootProvider(IEnumerable<MethodDesc> methods)
        {
            _methods = methods;
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            foreach (var method in _methods)
                rootProvider.AddCompilationRoot(method, "Single method root");
        }
    }
}
