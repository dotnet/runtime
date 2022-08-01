// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Encapsulates a list of class constructors that must be run in a prescribed order during start-up
    /// </summary>
    public sealed class LibraryInitializers
    {
        private const string LibraryInitializerContainerNamespaceName = "Internal.Runtime.CompilerHelpers";
        private const string LibraryInitializerContainerTypeName = "LibraryInitializer";
        private const string LibraryInitializerMethodName = "InitializeLibrary";

        private List<MethodDesc> _libraryInitializerMethods;

        private readonly TypeSystemContext _context;
        private IReadOnlyCollection<ModuleDesc> _librariesWithInitializers;

        public LibraryInitializers(TypeSystemContext context, IEnumerable<ModuleDesc> librariesWithInitializers)
        {
            _context = context;
            _librariesWithInitializers = new List<ModuleDesc>(librariesWithInitializers);
        }

        public IReadOnlyCollection<MethodDesc> LibraryInitializerMethods
        {
            get
            {
                if (_libraryInitializerMethods == null)
                    InitLibraryInitializers();

                return _libraryInitializerMethods;
            }
        }

        private void InitLibraryInitializers()
        {
            Debug.Assert(_libraryInitializerMethods == null);

            _libraryInitializerMethods = new List<MethodDesc>();

            foreach (var assembly in _librariesWithInitializers)
            {
                TypeDesc containingType = assembly.GetType(LibraryInitializerContainerNamespaceName, LibraryInitializerContainerTypeName, throwIfNotFound: false);
                if (containingType == null)
                    continue;

                MethodDesc initializerMethod = containingType.GetMethod(LibraryInitializerMethodName, null);
                if (initializerMethod == null)
                    continue;

                _libraryInitializerMethods.Add(initializerMethod);
            }
        }
    }
}
