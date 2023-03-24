// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Compilation roots necessary to enable universal shared generics thats
    /// are not encompassed in other root providers
    /// </summary>
    public class UniversalGenericsRootProvider : ICompilationRootProvider
    {
        private TypeSystemContext _context;

        public UniversalGenericsRootProvider(TypeSystemContext context)
        {
            _context = context;
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            if (_context.SupportsUniversalCanon)
                rootProvider.AddCompilationRoot(_context.UniversalCanonType.MakeArrayType(), "Universal generic array support");
        }
    }
}
