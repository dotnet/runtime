// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler
{
    /// <summary>
    /// Provides a set of seeds from which compilation will start.
    /// </summary>
    public interface ICompilationRootProvider
    {
        /// <summary>
        /// When implemented in a class, uses <paramref name="rootProvider"/> to add compilation
        /// roots to the compilation.
        /// </summary>
        void AddCompilationRoots(IRootingServiceProvider rootProvider);
    }
}
